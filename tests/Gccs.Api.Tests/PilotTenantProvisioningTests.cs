using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class PilotTenantProvisioningTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public PilotTenantProvisioningTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Admin_can_provision_no_cui_pilot_tenant_with_owner_membership_roles_and_audit()
    {
        var activeTenantId = Guid.Parse("70707070-7070-7070-7070-7070707070a1");
        var actorUserId = Guid.Parse("70707070-7070-7070-7070-7070707070b1");
        var ownerUserId = Guid.Parse("70707070-7070-7070-7070-7070707070c1");
        await using var factory = CreateFactory("pilot-provision-success", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(activeTenantId, "Platform Admin Tenant"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();

        using var request = CreateRequest(
            HttpMethod.Post,
            "/api/admin/pilot-tenants",
            new PilotTenantProvisioningRequest(
                "Aegis Pilot Workspace",
                ownerUserId,
                "pilot.owner@example.com",
                "Pilot Owner",
                RoleCatalog.Owner,
                new DateOnly(2026, 8, 31),
                "Provision No-CUI workspace for first pilot."),
            activeTenantId,
            actorUserId,
            Permission.ManageTenant);

        var response = await client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<PilotTenantProvisioningResultDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal("Aegis Pilot Workspace", result.Tenant.DisplayName);
        Assert.Equal(TenantStatus.Active, result.Tenant.Status);
        Assert.Equal(TenantDataPosture.NoCui, result.Tenant.DataHandlingMode);
        Assert.Equal(ownerUserId, result.Owner.UserId);
        Assert.Equal(result.Tenant.Id, result.Owner.TenantId);
        Assert.Equal("pilot.owner@example.com", result.Owner.Email);
        Assert.Equal(RoleCatalog.Owner, result.Owner.RoleName);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var tenant = await dbContext.Tenants.SingleAsync(candidate => candidate.Id == result.Tenant.Id);
        var ownerMembership = await dbContext.TenantMemberships.SingleAsync(candidate =>
            candidate.TenantId == result.Tenant.Id &&
            candidate.UserId == ownerUserId);
        var ownerRole = await dbContext.Roles.SingleAsync(candidate =>
            candidate.TenantId == result.Tenant.Id &&
            candidate.Name == RoleCatalog.Owner);
        var permissionCount = await dbContext.Set<RolePermissionEntity>().CountAsync(candidate => candidate.RoleId == ownerRole.Id);
        var userRoleExists = await dbContext.Set<UserRoleEntity>().AnyAsync(candidate =>
            candidate.UserId == ownerUserId &&
            candidate.RoleId == ownerRole.Id);
        var modeHistory = await dbContext.TenantDataHandlingModeHistory.SingleAsync(candidate => candidate.TenantId == result.Tenant.Id);
        var auditEvents = await dbContext.AuditLogEntries
            .Where(candidate => candidate.TenantId == result.Tenant.Id)
            .OrderBy(candidate => candidate.EntityType)
            .ToArrayAsync();

        Assert.Equal(TenantDataPosture.NoCui, tenant.DataPosture);
        Assert.Equal(MembershipStatus.Active, ownerMembership.Status);
        Assert.True(permissionCount > 0);
        Assert.True(userRoleExists);
        Assert.Null(modeHistory.PreviousMode);
        Assert.Equal(TenantDataPosture.NoCui, modeHistory.NewMode);
        Assert.Equal("Provision No-CUI workspace for first pilot.", modeHistory.Reason);
        Assert.Contains(auditEvents, candidate => candidate.EntityType == "Tenant" && candidate.Action == AuditAction.Created);
        Assert.Contains(auditEvents, candidate => candidate.EntityType == "TenantMembership" && candidate.Action == AuditAction.Created);
    }

    [Fact]
    public async Task Pilot_tenant_provisioning_requires_manage_tenant_permission()
    {
        var activeTenantId = Guid.Parse("70707070-7070-7070-7070-7070707070a2");
        await using var factory = CreateFactory("pilot-provision-rbac", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(activeTenantId, "Platform Admin Tenant"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();

        using var request = CreateRequest(
            HttpMethod.Post,
            "/api/admin/pilot-tenants",
            new PilotTenantProvisioningRequest(
                "Blocked Pilot Workspace",
                Guid.NewGuid(),
                "blocked.owner@example.com",
                "Blocked Owner"),
            activeTenantId,
            Guid.NewGuid(),
            Permission.ViewReports);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Pilot_tenant_provisioning_rejects_non_owner_or_admin_roles()
    {
        var activeTenantId = Guid.Parse("70707070-7070-7070-7070-7070707070a3");
        await using var factory = CreateFactory("pilot-provision-validation", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(activeTenantId, "Platform Admin Tenant"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();

        using var request = CreateRequest(
            HttpMethod.Post,
            "/api/admin/pilot-tenants",
            new PilotTenantProvisioningRequest(
                "Invalid Pilot Workspace",
                Guid.NewGuid(),
                "invalid.owner@example.com",
                "Invalid Owner",
                RoleCatalog.Contributor),
            activeTenantId,
            Guid.NewGuid(),
            Permission.ManageTenant);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Pilot owner role must be Owner or Admin", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Existing_user_can_be_attached_as_owner_of_new_pilot_tenant_without_cross_tenant_data_leakage()
    {
        var activeTenantId = Guid.Parse("70707070-7070-7070-7070-7070707070a4");
        var existingUserId = Guid.Parse("70707070-7070-7070-7070-7070707070b4");
        await using var factory = CreateFactory("pilot-provision-existing-user", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(activeTenantId, "Platform Admin Tenant"));
            dbContext.Users.Add(new UserEntity
            {
                Id = existingUserId,
                TenantId = activeTenantId,
                Email = "existing@example.com",
                DisplayName = "Existing User",
                Status = UserStatus.Active,
                MfaEnabled = true,
                CreatedAt = DateTimeOffset.UtcNow
            });
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();

        using var request = CreateRequest(
            HttpMethod.Post,
            "/api/admin/pilot-tenants",
            new PilotTenantProvisioningRequest(
                "Existing User Pilot",
                existingUserId,
                "existing.pilot@example.com",
                "Existing Pilot Owner",
                RoleCatalog.Admin),
            activeTenantId,
            Guid.NewGuid(),
            Permission.ManageTenant);

        var response = await client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<PilotTenantProvisioningResultDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(result);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();

        Assert.Equal(1, await dbContext.Users.CountAsync(candidate => candidate.Id == existingUserId));
        Assert.True(await dbContext.TenantMemberships.AnyAsync(candidate =>
            candidate.TenantId == result.Tenant.Id &&
            candidate.UserId == existingUserId &&
            candidate.RoleName == RoleCatalog.Admin));
        Assert.False(await dbContext.TenantMemberships.AnyAsync(candidate =>
            candidate.TenantId == activeTenantId &&
            candidate.RoleName == RoleCatalog.Admin));
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
                services.AddDbContext<GccsDbContext>(options => options
                    .UseInMemoryDatabase(databaseName)
                    .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
                services.AddScoped<PilotTenantProvisioningService>();
                services.AddScoped<IPilotTenantProvisioningRepository, EfPilotTenantProvisioningRepository>();

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
                seed?.Invoke(dbContext);
            });
        });

    private static HttpRequestMessage CreateRequest<TContent>(
        HttpMethod method,
        string requestUri,
        TContent content,
        Guid tenantId,
        Guid userId,
        Permission permission)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", permission.ToString());
        request.Content = JsonContent.Create(content, options: JsonOptions);

        return request;
    }

    private static TenantEntity CreateTenant(Guid tenantId, string name) =>
        new()
        {
            Id = tenantId,
            Name = name,
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        };
}
