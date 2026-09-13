using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Portals;
using Gccs.Domain.Audit;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Portals;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class PortalPackageLifecycleEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly WebApplicationFactory<Program> _factory;

    public PortalPackageLifecycleEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Tenant_admin_can_revoke_and_cross_tenant_access_returns_not_found_without_mutation()
    {
        var ids = TestIds.Create();
        await using var factory = CreateFactory(nameof(Tenant_admin_can_revoke_and_cross_tenant_access_returns_not_found_without_mutation), ids);
        using var client = factory.CreateClient();
        var created = await CreateShareAsync(client, ids, ids.TenantId);

        var crossTenant = await client.SendAsync(Request(
            HttpMethod.Post,
            $"/api/portal/shared-packages/{created.Id}/revoke",
            new RevokeSharedPortalPackageRequest("Cross-tenant attempt."),
            ids,
            ids.OtherTenantId,
            Permission.ManageUsers));
        Assert.Equal(HttpStatusCode.NotFound, crossTenant.StatusCode);

        var revoked = await client.SendAsync(Request(
            HttpMethod.Post,
            $"/api/portal/shared-packages/{created.Id}/revoke",
            new RevokeSharedPortalPackageRequest("Over-shared outside the intended review scope."),
            ids,
            ids.TenantId,
            Permission.ManageUsers));
        Assert.Equal(HttpStatusCode.OK, revoked.StatusCode);
        var dto = await revoked.Content.ReadFromJsonAsync<SharedPortalPackageDto>(JsonOptions);
        Assert.Equal(SharedPortalPackageState.Revoked, dto?.State);
        Assert.Equal("Over-shared outside the intended review scope.", dto?.RevocationReason);

        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPortalPackageLifecycleRepository>();
        Assert.False(await repository.CanAccessAsync(created.Id, ids.TenantId, ids.InvitationId, ids.PackageId, DateTimeOffset.UtcNow));
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Single(await db.AuditLogEntries.Where(entry =>
            entry.TenantId == ids.TenantId &&
            entry.EntityType == "SharedPortalPackage" &&
            entry.Action == AuditAction.PermissionChanged).ToArrayAsync());
        Assert.Empty(await db.AuditLogEntries.Where(entry => entry.TenantId == ids.OtherTenantId).ToArrayAsync());
    }

    [Fact]
    public async Task Lifecycle_routes_require_admin_permission_and_activity_report_requires_audit_permission()
    {
        var ids = TestIds.Create();
        await using var factory = CreateFactory(nameof(Lifecycle_routes_require_admin_permission_and_activity_report_requires_audit_permission), ids);
        using var client = factory.CreateClient();

        var deniedCreate = await client.SendAsync(Request(
            HttpMethod.Post,
            "/api/portal/shared-packages",
            new SharedPortalPackageRequest(ids.PackageId, ids.InvitationId, DateTimeOffset.UtcNow.AddDays(30)),
            ids,
            ids.TenantId,
            Permission.ViewReports));
        Assert.Equal(HttpStatusCode.Forbidden, deniedCreate.StatusCode);

        await CreateShareAsync(client, ids, ids.TenantId);
        var deniedReport = await client.SendAsync(Request(
            HttpMethod.Get,
            "/api/portal/shared-packages/activity-report",
            ids,
            ids.TenantId,
            Permission.ManageUsers));
        Assert.Equal(HttpStatusCode.Forbidden, deniedReport.StatusCode);

        var allowedReport = await client.SendAsync(Request(
            HttpMethod.Get,
            "/api/portal/shared-packages/activity-report",
            ids,
            ids.TenantId,
            Permission.ViewAuditLog));
        Assert.Equal(HttpStatusCode.OK, allowedReport.StatusCode);
    }

    [Fact]
    public async Task Invalid_transition_returns_standard_validation_error_and_does_not_append_false_audit()
    {
        var ids = TestIds.Create();
        await using var factory = CreateFactory(nameof(Invalid_transition_returns_standard_validation_error_and_does_not_append_false_audit), ids);
        using var client = factory.CreateClient();
        var created = await CreateShareAsync(client, ids, ids.TenantId);
        var expire = await client.SendAsync(Request(HttpMethod.Post,
            $"/api/portal/shared-packages/{created.Id}/expire", ids, ids.TenantId, Permission.ManageUsers));
        Assert.Equal(HttpStatusCode.OK, expire.StatusCode);

        var rejected = await client.SendAsync(Request(
            HttpMethod.Post,
            $"/api/portal/shared-packages/{created.Id}/revoke",
            new RevokeSharedPortalPackageRequest("Invalid second terminal action."),
            ids,
            ids.TenantId,
            Permission.ManageUsers));
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Equal("application/problem+json", rejected.Content.Headers.ContentType?.MediaType);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Empty(await db.AuditLogEntries.Where(entry =>
            entry.EntityId == created.Id.ToString() && entry.Action == AuditAction.PermissionChanged).ToArrayAsync());
    }

    [Fact]
    public async Task Package_preparation_requires_manage_reports_and_source_specific_permission()
    {
        var ids = TestIds.Create();
        await using var factory = CreateFactory(nameof(Package_preparation_requires_manage_reports_and_source_specific_permission), ids);
        using var client = factory.CreateClient();
        var request = new PreparePortalReviewPackageRequest(
            PortalReviewPreparationSource.AuditLogExport, null, "Auditor review package",
            Gccs.Domain.Common.ContentClassification.Unclassified);

        var missingSourcePermission = await client.SendAsync(Request(
            HttpMethod.Post, "/api/portal/review-packages/prepare", request,
            ids, ids.TenantId, Permission.ManageReports));
        Assert.Equal(HttpStatusCode.Forbidden, missingSourcePermission.StatusCode);

        var allowed = await client.SendAsync(Request(
            HttpMethod.Post, "/api/portal/review-packages/prepare", request,
            ids, ids.TenantId, Permission.ManageReports, Permission.ViewAuditLog));
        Assert.Equal(HttpStatusCode.Created, allowed.StatusCode);
        var package = await allowed.Content.ReadFromJsonAsync<PreparedPortalReviewPackageDto>(JsonOptions);
        Assert.Equal(PortalReviewPreparationSource.AuditLogExport, package?.SourceType);
    }

    private async Task<SharedPortalPackageDto> CreateShareAsync(HttpClient client, TestIds ids, Guid tenantId)
    {
        var response = await client.SendAsync(Request(
            HttpMethod.Post,
            "/api/portal/shared-packages",
            new SharedPortalPackageRequest(ids.PackageId, ids.InvitationId, DateTimeOffset.UtcNow.AddDays(30)),
            ids,
            tenantId,
            Permission.ManageUsers));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<SharedPortalPackageDto>(
            await response.Content.ReadFromJsonAsync<SharedPortalPackageDto>(JsonOptions));
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, TestIds ids) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPortalPackageLifecycleRepository>();
                services.RemoveAll<IPortalPackageShareEligibilityValidator>();
                services.RemoveAll<TimeProvider>();
                services.AddSingleton(TimeProvider.System);
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<IPortalPackageLifecycleRepository, EfPortalPackageLifecycleRepository>();
                services.AddSingleton<IPortalPackageShareEligibilityValidator, AllowAllEligibilityValidator>();
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();
                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                db.Tenants.AddRange(
                    Tenant(ids.TenantId, "Alpha"),
                    Tenant(ids.OtherTenantId, "Bravo"));
                db.SaveChanges();
            });
        });

    private static TenantEntity Tenant(Guid id, string name) => new()
    {
        Id = id,
        Name = name,
        Status = TenantStatus.Active,
        DataPosture = TenantDataPosture.NoCui,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static HttpRequestMessage Request<T>(
        HttpMethod method,
        string uri,
        T body,
        TestIds ids,
        Guid tenantId,
        params Permission[] permissions)
    {
        var request = Request(method, uri, ids, tenantId, permissions);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        return request;
    }

    private static HttpRequestMessage Request(
        HttpMethod method,
        string uri,
        TestIds ids,
        Guid tenantId,
        params Permission[] permissions)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", ids.ActorUserId.ToString());
        request.Headers.Add("X-Gccs-Dev-Email", "admin@example.test");
        request.Headers.Add("X-Gccs-Dev-Permissions", string.Join(',', permissions));
        return request;
    }

    private sealed record TestIds(
        Guid TenantId,
        Guid OtherTenantId,
        Guid ActorUserId,
        Guid PackageId,
        Guid InvitationId)
    {
        public static TestIds Create() => new(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
    }

    private sealed class AllowAllEligibilityValidator : IPortalPackageShareEligibilityValidator
    {
        public Task<PortalPackageApprovalMetadataDto> ValidateAsync(Guid packageId, Guid invitationId, Guid tenantId, DateTimeOffset asOf, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PortalPackageApprovalMetadataDto(1, new string('a', 64)));
    }
}
