using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Gccs.Application.Audit;
using Gccs.Domain.Audit;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class AuditLogViewerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly WebApplicationFactory<Program> _factory;

    public AuditLogViewerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(RoleCatalog.Owner)]
    [InlineData(RoleCatalog.Admin)]
    [InlineData(RoleCatalog.Advisor)]
    public async Task TC_5_2_1_Admin_owner_or_advisor_sees_only_current_tenant_events(string roleName)
    {
        var tenantAId = Guid.Parse("52525252-5252-5252-5252-5252525252a1");
        var tenantBId = Guid.Parse("52525252-5252-5252-5252-5252525252b1");
        await using var factory = CreateFactory("tc-5-2-1", dbContext =>
        {
            dbContext.Tenants.AddRange(
                CreateTenant(tenantAId, "TC-5.2.1 Tenant A"),
                CreateTenant(tenantBId, "TC-5.2.1 Tenant B"));
            dbContext.AuditLogEntries.AddRange(
                CreateAuditEntry(tenantAId, 1, AuditAction.Created, "Tenant", "Tenant A event"),
                CreateAuditEntry(tenantAId, 2, AuditAction.Updated, "TenantMembership", "Tenant A membership event"),
                CreateAuditEntry(tenantBId, 3, AuditAction.Deleted, "Tenant", "Tenant B event"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        using var request = CreateRoleRequest(HttpMethod.Get, "/api/audit-logs?page=1&pageSize=25", tenantAId, roleName);

        var response = await client.SendAsync(request);
        var page = await response.Content.ReadFromJsonAsync<PagedResultDto<AuditLogEntryDto>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(page);
        Assert.Equal(2, page.TotalCount);
        Assert.All(page.Items, item => Assert.Equal(tenantAId, item.TenantId));
        Assert.DoesNotContain(page.Items, item => item.Summary.Contains("Tenant B", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(RoleCatalog.Contributor)]
    [InlineData(RoleCatalog.Auditor)]
    public async Task TC_5_2_2_Contributor_and_auditor_cannot_access_audit_logs(string roleName)
    {
        var tenantId = Guid.Parse("52525252-5252-5252-5252-5252525252a2");
        await using var factory = CreateFactory("tc-5-2-2", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-5.2.2 Tenant"));
            dbContext.AuditLogEntries.Add(CreateAuditEntry(tenantId, 1, AuditAction.Created, "Tenant", "Restricted event"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        using var request = CreateRoleRequest(HttpMethod.Get, "/api/audit-logs", tenantId, roleName);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task TC_5_2_3_Audit_log_pagination_uses_page_size_and_stable_ordering()
    {
        var tenantId = Guid.Parse("52525252-5252-5252-5252-5252525252a3");
        await using var factory = CreateFactory("tc-5-2-3", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-5.2.3 Tenant"));
            for (var index = 1; index <= 6; index++)
            {
                dbContext.AuditLogEntries.Add(CreateAuditEntry(tenantId, index, AuditAction.Created, "Tenant", $"Audit event {index}"));
            }

            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        using var firstPageRequest = CreatePermissionRequest(HttpMethod.Get, "/api/audit-logs?page=1&pageSize=2", tenantId, Permission.ViewAuditLog);
        using var secondPageRequest = CreatePermissionRequest(HttpMethod.Get, "/api/audit-logs?page=2&pageSize=2", tenantId, Permission.ViewAuditLog);

        var firstPageResponse = await client.SendAsync(firstPageRequest);
        var firstPage = await firstPageResponse.Content.ReadFromJsonAsync<PagedResultDto<AuditLogEntryDto>>(JsonOptions);
        var secondPageResponse = await client.SendAsync(secondPageRequest);
        var secondPage = await secondPageResponse.Content.ReadFromJsonAsync<PagedResultDto<AuditLogEntryDto>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, firstPageResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondPageResponse.StatusCode);
        Assert.NotNull(firstPage);
        Assert.NotNull(secondPage);
        Assert.Equal(6, firstPage.TotalCount);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.True(firstPage.HasNextPage);
        Assert.False(firstPage.HasPreviousPage);
        Assert.Equal(["Audit event 6", "Audit event 5"], firstPage.Items.Select(item => item.Summary).ToArray());
        Assert.Equal(["Audit event 4", "Audit event 3"], secondPage.Items.Select(item => item.Summary).ToArray());
        Assert.True(secondPage.HasNextPage);
        Assert.True(secondPage.HasPreviousPage);
    }

    [Fact]
    public async Task TC_5_2_4_Audit_log_filters_are_correct_and_tenant_scoped()
    {
        var tenantAId = Guid.Parse("52525252-5252-5252-5252-5252525252a4");
        var tenantBId = Guid.Parse("52525252-5252-5252-5252-5252525252b4");
        var actorUserId = Guid.Parse("52525252-5252-5252-5252-5252525252c4");
        await using var factory = CreateFactory("tc-5-2-4", dbContext =>
        {
            dbContext.Tenants.AddRange(
                CreateTenant(tenantAId, "TC-5.2.4 Tenant A"),
                CreateTenant(tenantBId, "TC-5.2.4 Tenant B"));
            dbContext.AuditLogEntries.AddRange(
                CreateAuditEntry(tenantAId, 1, AuditAction.Created, "Tenant", "Wrong action", actorUserId),
                CreateAuditEntry(tenantAId, 2, AuditAction.Updated, "TenantMembership", "Matching membership", actorUserId),
                CreateAuditEntry(tenantAId, 3, AuditAction.Updated, "Tenant", "Wrong entity", actorUserId),
                CreateAuditEntry(tenantAId, 4, AuditAction.Updated, "TenantMembership", "Wrong actor", Guid.NewGuid()),
                CreateAuditEntry(tenantBId, 2, AuditAction.Updated, "TenantMembership", "Wrong tenant", actorUserId));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        var from = Uri.EscapeDataString("2026-06-15T12:01:00Z");
        var to = Uri.EscapeDataString("2026-06-15T12:03:00Z");
        using var request = CreatePermissionRequest(
            HttpMethod.Get,
            $"/api/audit-logs?actorUserId={actorUserId}&action=updated&entityType=TenantMembership&from={from}&to={to}",
            tenantAId,
            Permission.ViewAuditLog);

        var response = await client.SendAsync(request);
        var page = await response.Content.ReadFromJsonAsync<PagedResultDto<AuditLogEntryDto>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(page);
        var item = Assert.Single(page.Items);
        Assert.Equal(tenantAId, item.TenantId);
        Assert.Equal(actorUserId, item.ActorUserId);
        Assert.Equal(AuditAction.Updated.ToString(), item.Action);
        Assert.Equal("TenantMembership", item.EntityType);
        Assert.Equal("Matching membership", item.Summary);
    }

    private WebApplicationFactory<Program> CreateFactory(
        string databaseName,
        Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<AuditLogService>();
                services.AddScoped<IAuditLogRepository, EfAuditLogRepository>();

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
                seed?.Invoke(dbContext);
            });
        });

    private static HttpRequestMessage CreateRoleRequest(HttpMethod method, string requestUri, Guid tenantId, string roleName)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-Role", roleName);
        request.Headers.Add("X-Gccs-Dev-User", Guid.NewGuid().ToString());
        return request;
    }

    private static HttpRequestMessage CreatePermissionRequest(HttpMethod method, string requestUri, Guid tenantId, Permission permission)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", permission.ToString());
        request.Headers.Add("X-Gccs-Dev-User", Guid.NewGuid().ToString());
        return request;
    }

    private static TenantEntity CreateTenant(Guid tenantId, string name) =>
        new()
        {
            Id = tenantId,
            Name = name,
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.Parse("2026-06-15T12:00:00Z")
        };

    private static AuditLogEntryEntity CreateAuditEntry(
        Guid tenantId,
        int sequence,
        AuditAction action,
        string entityType,
        string summary,
        Guid? actorUserId = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ActorUserId = actorUserId ?? Guid.NewGuid(),
            Action = action,
            EntityType = entityType,
            EntityId = Guid.NewGuid().ToString(),
            OccurredAt = DateTimeOffset.Parse("2026-06-15T12:00:00Z").AddMinutes(sequence),
            IpAddress = "203.0.113.10",
            UserAgent = "seed",
            CorrelationId = $"seed-correlation-{sequence}",
            Summary = summary,
            MetadataJson = "{}"
        };
}
