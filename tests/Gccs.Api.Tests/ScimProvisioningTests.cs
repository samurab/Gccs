using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Identity;
using Gccs.Application.Security;
using Gccs.Domain.Audit;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Identity;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ScimProvisioningTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public ScimProvisioningTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_35_3_1_Enable_rotate_and_revoke_scim_tokens()
    {
        var tenantId = Guid.Parse("35335335-3353-3533-5335-3353353353a1");
        var actorUserId = Guid.Parse("35335335-3353-3533-5335-3353353353b1");
        await using var factory = CreateFactory("tc-35-3-1", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-35.3.1 Tenant"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        var enabled = await EnableAsync(client, tenantId, actorUserId);
        await PutMappingAsync(client, tenantId, actorUserId, "GCCS Admins", RoleCatalog.Admin);

        var firstTokenWorks = await ProvisionAsync(client, tenantId, actorUserId, enabled.Token, "external-1", "one@example.com", ["GCCS Admins"]);
        var rotatedResponse = await client.SendAsync(CreateRequest(HttpMethod.Post, "/api/enterprise/scim/token/rotate", tenantId, actorUserId, Permission.ManageUsers));
        var rotated = await rotatedResponse.Content.ReadFromJsonAsync<ScimTokenLifecycleResult>(JsonOptions);
        var oldTokenResponse = await client.SendAsync(CreateRequest(HttpMethod.Put, "/api/enterprise/scim/users", NewUser(enabled.Token, "external-old", "old@example.com", ["GCCS Admins"]), tenantId, actorUserId, Permission.ManageUsers));
        var newTokenWorks = await ProvisionAsync(client, tenantId, actorUserId, rotated!.Token, "external-2", "two@example.com", ["GCCS Admins"]);
        await client.SendAsync(CreateRequest(HttpMethod.Post, "/api/enterprise/scim/token/revoke", tenantId, actorUserId, Permission.ManageUsers));
        var revokedTokenResponse = await client.SendAsync(CreateRequest(HttpMethod.Put, "/api/enterprise/scim/users", NewUser(rotated.Token, "external-revoked", "revoked@example.com", ["GCCS Admins"]), tenantId, actorUserId, Permission.ManageUsers));

        Assert.True(firstTokenWorks.UserStatus == UserStatus.Active);
        Assert.Equal(HttpStatusCode.OK, rotatedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, oldTokenResponse.StatusCode);
        Assert.Equal(UserStatus.Active, newTokenWorks.UserStatus);
        Assert.Equal(HttpStatusCode.BadRequest, revokedTokenResponse.StatusCode);
    }

    [Fact]
    public async Task TC_35_3_2_Create_update_deactivate_and_reactivate_users_in_current_tenant_only()
    {
        var tenantId = Guid.Parse("35335335-3353-3533-5335-3353353353a2");
        var otherTenantId = Guid.Parse("35335335-3353-3533-5335-3353353353b2");
        var actorUserId = Guid.Parse("35335335-3353-3533-5335-3353353353c2");
        await using var factory = CreateFactory("tc-35-3-2", dbContext =>
        {
            dbContext.Tenants.AddRange(CreateTenant(tenantId, "TC-35.3.2 Tenant A"), CreateTenant(otherTenantId, "TC-35.3.2 Tenant B"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        var provisioningKey = (await EnableAsync(client, tenantId, actorUserId)).Token;
        await PutMappingAsync(client, tenantId, actorUserId, "GCCS Users", RoleCatalog.Contributor);

        var created = await ProvisionAsync(client, tenantId, actorUserId, provisioningKey, "external-2", "user@example.com", ["GCCS Users"]);
        var updated = await ProvisionAsync(client, tenantId, actorUserId, provisioningKey, "external-2", "user@example.com", ["GCCS Users"], "Updated User");
        var deactivated = await ProvisionAsync(client, tenantId, actorUserId, provisioningKey, "external-2", "user@example.com", ["GCCS Users"], "Updated User", active: false);
        var reactivateResponse = await client.SendAsync(CreateRequest(HttpMethod.Post, "/api/enterprise/scim/users/external-2/reactivate", new ScimTokenRequest(provisioningKey), tenantId, actorUserId, Permission.ManageUsers));
        var reactivated = await reactivateResponse.Content.ReadFromJsonAsync<ScimProvisionedUserDto>(JsonOptions);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();

        Assert.Equal("user@example.com", created.UserName);
        Assert.Equal("Updated User", updated.DisplayName);
        Assert.Equal(UserStatus.Disabled, deactivated.UserStatus);
        Assert.Equal(MembershipStatus.Deactivated, deactivated.MembershipStatus);
        Assert.Equal(HttpStatusCode.OK, reactivateResponse.StatusCode);
        Assert.Equal(UserStatus.Active, reactivated?.UserStatus);
        Assert.Empty(await dbContext.ScimProvisionedIdentities.Where(identity => identity.TenantId == otherTenantId).ToListAsync());
    }

    [Fact]
    public async Task TC_35_3_3_Group_assign_and_remove_persist_role_changes_with_conflict_validation()
    {
        var tenantId = Guid.Parse("35335335-3353-3533-5335-3353353353a3");
        var actorUserId = Guid.Parse("35335335-3353-3533-5335-3353353353b3");
        await using var factory = CreateFactory("tc-35-3-3", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-35.3.3 Tenant"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        var provisioningKey = (await EnableAsync(client, tenantId, actorUserId)).Token;
        await PutMappingAsync(client, tenantId, actorUserId, "GCCS Admins", RoleCatalog.Admin);
        await PutMappingAsync(client, tenantId, actorUserId, "GCCS Contributors", RoleCatalog.Contributor);

        var provisioned = await ProvisionAsync(client, tenantId, actorUserId, provisioningKey, "external-3", "grouped@example.com", ["GCCS Contributors"]);
        var assignedResponse = await client.SendAsync(CreateRequest(HttpMethod.Post, "/api/enterprise/scim/users/external-3/groups", new ScimGroupAssignmentRequest(provisioningKey, "GCCS Admins"), tenantId, actorUserId, Permission.ManageUsers));
        var assigned = await assignedResponse.Content.ReadFromJsonAsync<ScimProvisionedUserDto>(JsonOptions);
        var removedResponse = await client.SendAsync(CreateRequest(HttpMethod.Delete, "/api/enterprise/scim/users/external-3/groups", new ScimGroupAssignmentRequest(provisioningKey, "GCCS Admins"), tenantId, actorUserId, Permission.ManageUsers));
        var removed = await removedResponse.Content.ReadFromJsonAsync<ScimProvisionedUserDto>(JsonOptions);
        var conflictResponse = await client.SendAsync(CreateRequest(HttpMethod.Put, "/api/enterprise/scim/users", NewUser(provisioningKey, "external-conflict", "conflict@example.com", ["GCCS Admins", "GCCS Contributors"]), tenantId, actorUserId, Permission.ManageUsers));

        Assert.Equal(RoleCatalog.Contributor, provisioned.RoleName);
        Assert.Equal(HttpStatusCode.OK, assignedResponse.StatusCode);
        Assert.Equal(RoleCatalog.Admin, assigned?.RoleName);
        Assert.Equal(HttpStatusCode.OK, removedResponse.StatusCode);
        Assert.Equal(RoleCatalog.Contributor, removed?.RoleName);
        Assert.Equal(HttpStatusCode.BadRequest, conflictResponse.StatusCode);
    }

    [Fact]
    public async Task TC_35_3_4_Duplicate_invalid_and_cross_tenant_provisioning_are_rejected()
    {
        var tenantId = Guid.Parse("35335335-3353-3533-5335-3353353353a4");
        var otherTenantId = Guid.Parse("35335335-3353-3533-5335-3353353353b4");
        var actorUserId = Guid.Parse("35335335-3353-3533-5335-3353353353c4");
        await using var factory = CreateFactory("tc-35-3-4", dbContext =>
        {
            dbContext.Tenants.AddRange(CreateTenant(tenantId, "TC-35.3.4 Tenant A"), CreateTenant(otherTenantId, "TC-35.3.4 Tenant B"));
            AddScimIdentity(dbContext, otherTenantId, "external-cross", "other@example.com");
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        var provisioningKey = (await EnableAsync(client, tenantId, actorUserId)).Token;
        await PutMappingAsync(client, tenantId, actorUserId, "GCCS Admins", RoleCatalog.Admin);
        await ProvisionAsync(client, tenantId, actorUserId, provisioningKey, "external-dup", "duplicate@example.com", ["GCCS Admins"]);

        var duplicate = await client.SendAsync(CreateRequest(HttpMethod.Put, "/api/enterprise/scim/users", NewUser(provisioningKey, "external-dup", "changed@example.com", ["GCCS Admins"]), tenantId, actorUserId, Permission.ManageUsers));
        var invalidGroup = await client.SendAsync(CreateRequest(HttpMethod.Put, "/api/enterprise/scim/users", NewUser(provisioningKey, "external-invalid", "invalid@example.com", ["Unknown Group"]), tenantId, actorUserId, Permission.ManageUsers));
        var invalidMapping = await client.SendAsync(CreateRequest(HttpMethod.Put, "/api/enterprise/scim/group-mappings", new UpsertScimGroupMappingRequest("Bad", "Superuser"), tenantId, actorUserId, Permission.ManageUsers));
        var crossTenant = await client.SendAsync(CreateRequest(HttpMethod.Put, "/api/enterprise/scim/users", NewUser(provisioningKey, "external-cross", "cross@example.com", ["GCCS Admins"]), tenantId, actorUserId, Permission.ManageUsers));

        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidGroup.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidMapping.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, crossTenant.StatusCode);
    }

    [Fact]
    public async Task TC_35_3_5_Scim_events_and_token_lifecycle_are_audit_logged()
    {
        var tenantId = Guid.Parse("35335335-3353-3533-5335-3353353353a5");
        var actorUserId = Guid.Parse("35335335-3353-3533-5335-3353353353b5");
        await using var factory = CreateFactory("tc-35-3-5", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-35.3.5 Tenant"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        var enabled = await EnableAsync(client, tenantId, actorUserId);
        await PutMappingAsync(client, tenantId, actorUserId, "GCCS Admins", RoleCatalog.Admin);
        await PutMappingAsync(client, tenantId, actorUserId, "GCCS Contributors", RoleCatalog.Contributor);
        await ProvisionAsync(client, tenantId, actorUserId, enabled.Token, "external-audit", "audit@example.com", ["GCCS Admins"]);
        await client.SendAsync(CreateRequest(HttpMethod.Put, "/api/enterprise/scim/users", NewUser(enabled.Token, "external-audit", "changed@example.com", ["GCCS Admins"]), tenantId, actorUserId, Permission.ManageUsers));
        await client.SendAsync(CreateRequest(HttpMethod.Put, "/api/enterprise/scim/users", NewUser(enabled.Token, "external-conflict", "conflict@example.com", ["GCCS Admins", "GCCS Contributors"]), tenantId, actorUserId, Permission.ManageUsers));
        await client.SendAsync(CreateRequest(HttpMethod.Post, "/api/enterprise/scim/users/missing/reactivate", new ScimTokenRequest(enabled.Token), tenantId, actorUserId, Permission.ManageUsers));
        await client.SendAsync(CreateRequest(HttpMethod.Post, "/api/enterprise/scim/token/rotate", tenantId, actorUserId, Permission.ManageUsers));
        await client.SendAsync(CreateRequest(HttpMethod.Post, "/api/enterprise/scim/token/revoke", tenantId, actorUserId, Permission.ManageUsers));

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var audits = await dbContext.AuditLogEntries.Where(audit => audit.TenantId == tenantId).ToListAsync();

        Assert.Contains(audits, audit => audit.MetadataJson.Contains("enabled", StringComparison.Ordinal));
        Assert.Contains(audits, audit => audit.MetadataJson.Contains("user_upserted", StringComparison.Ordinal));
        Assert.Contains(audits, audit => audit.MetadataJson.Contains("duplicate_identity", StringComparison.Ordinal));
        Assert.Contains(audits, audit => audit.MetadataJson.Contains("conflict", StringComparison.Ordinal));
        Assert.Contains(audits, audit => audit.MetadataJson.Contains("skipped", StringComparison.Ordinal));
        Assert.Contains(audits, audit => audit.MetadataJson.Contains("token_rotated", StringComparison.Ordinal));
        Assert.Contains(audits, audit => audit.MetadataJson.Contains("token_revoked", StringComparison.Ordinal));
    }

    private static ScimProvisionUserRequest NewUser(string token, string externalId, string email, IReadOnlyList<string> groups, string displayName = "SCIM User", bool active = true) =>
        new(token, externalId, email, displayName, active, groups);

    private static async Task<ScimTokenLifecycleResult> EnableAsync(HttpClient client, Guid tenantId, Guid actorUserId)
    {
        var response = await client.SendAsync(CreateRequest(HttpMethod.Post, "/api/enterprise/scim/enable", new EnableScimProvisioningRequest("ENABLE SCIM", "Okta"), tenantId, actorUserId, Permission.ManageUsers));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Assert.IsType<ScimTokenLifecycleResult>(await response.Content.ReadFromJsonAsync<ScimTokenLifecycleResult>(JsonOptions));
    }

    private static async Task PutMappingAsync(HttpClient client, Guid tenantId, Guid actorUserId, string groupName, string roleName)
    {
        var response = await client.SendAsync(CreateRequest(HttpMethod.Put, "/api/enterprise/scim/group-mappings", new UpsertScimGroupMappingRequest(groupName, roleName), tenantId, actorUserId, Permission.ManageUsers));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<ScimProvisionedUserDto> ProvisionAsync(HttpClient client, Guid tenantId, Guid actorUserId, string token, string externalId, string email, IReadOnlyList<string> groups, string displayName = "SCIM User", bool active = true)
    {
        var response = await client.SendAsync(CreateRequest(HttpMethod.Put, "/api/enterprise/scim/users", NewUser(token, externalId, email, groups, displayName, active), tenantId, actorUserId, Permission.ManageUsers));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Assert.IsType<ScimProvisionedUserDto>(await response.Content.ReadFromJsonAsync<ScimProvisionedUserDto>(JsonOptions));
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<ScimProvisioningService>();
                services.AddScoped<IScimProvisioningRepository, EfScimProvisioningRepository>();
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
                seed?.Invoke(dbContext);
            });
        });

    private static HttpRequestMessage CreateRequest<TContent>(HttpMethod method, string requestUri, TContent? content, Guid tenantId, Guid userId, Permission permission)
    {
        var request = CreateRequest(method, requestUri, tenantId, userId, permission);
        if (content is not null)
        {
            request.Content = JsonContent.Create(content, options: JsonOptions);
        }

        return request;
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string requestUri, Guid tenantId, Guid userId, Permission permission)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        request.Headers.Add("X-Gccs-Dev-Email", "admin@example.com");
        request.Headers.Add("X-Gccs-Dev-Permissions", permission.ToString());
        return request;
    }

    private static TenantEntity CreateTenant(Guid tenantId, string name) =>
        new()
        {
            Id = tenantId,
            Name = name,
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.Parse("2026-06-19T12:00:00Z")
        };

    private static void AddScimIdentity(GccsDbContext dbContext, Guid tenantId, string externalId, string email)
    {
        var userId = Guid.NewGuid();
        var membershipId = Guid.NewGuid();
        dbContext.Users.Add(new UserEntity
        {
            Id = userId,
            TenantId = tenantId,
            Email = email,
            DisplayName = email,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.Parse("2026-06-19T12:00:00Z")
        });
        dbContext.TenantMemberships.Add(new TenantMembershipEntity
        {
            Id = membershipId,
            TenantId = tenantId,
            UserId = userId,
            Status = MembershipStatus.Active,
            RoleName = RoleCatalog.Contributor,
            CreatedAt = DateTimeOffset.Parse("2026-06-19T12:00:00Z")
        });
        dbContext.ScimProvisionedIdentities.Add(new ScimProvisionedIdentityEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ExternalId = externalId,
            UserName = email,
            UserId = userId,
            MembershipId = membershipId,
            CreatedAt = DateTimeOffset.Parse("2026-06-19T12:00:00Z")
        });
    }
}
