using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Identity;
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

public sealed class TenantMembershipTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public TenantMembershipTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_2_2_1_Assigned_user_is_visible_only_when_that_tenant_is_active()
    {
        var tenantAId = Guid.Parse("12121212-1212-1212-1212-1212121212a1");
        var tenantBId = Guid.Parse("12121212-1212-1212-1212-1212121212b1");
        var userId = Guid.Parse("12121212-1212-1212-1212-1212121212c1");
        await using var factory = CreateFactory("tc-2-2-1", dbContext =>
        {
            SeedTenants(dbContext, tenantAId, tenantBId);
            dbContext.Users.Add(CreateUser(userId, tenantAId, "shared.member@example.com", "Shared Member"));
            dbContext.TenantMemberships.AddRange(
                CreateMembership(Guid.Parse("12121212-1212-1212-1212-1212121212d1"), tenantAId, userId, "Owner"),
                CreateMembership(Guid.Parse("12121212-1212-1212-1212-1212121212d2"), tenantBId, userId, "Advisor"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();

        using var tenantARequest = CreateRequest(HttpMethod.Get, "/api/tenant-members", tenantAId, Guid.NewGuid(), Permission.ManageUsers);
        using var tenantBRequest = CreateRequest(HttpMethod.Get, "/api/tenant-members", tenantBId, Guid.NewGuid(), Permission.ManageUsers);

        var tenantAResponse = await client.SendAsync(tenantARequest);
        var tenantBResponse = await client.SendAsync(tenantBRequest);
        var tenantAResponseBody = await tenantAResponse.Content.ReadAsStringAsync();
        var tenantBResponseBody = await tenantBResponse.Content.ReadAsStringAsync();
        var tenantAMembers = JsonSerializer.Deserialize<TenantMemberDto[]>(tenantAResponseBody, JsonOptions);
        var tenantBMembers = JsonSerializer.Deserialize<TenantMemberDto[]>(tenantBResponseBody, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, tenantAResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, tenantBResponse.StatusCode);
        var tenantAMember = Assert.Single(tenantAMembers ?? []);
        var tenantBMember = Assert.Single(tenantBMembers ?? []);
        Assert.Equal(tenantAId, tenantAMember.TenantId);
        Assert.Equal("Owner", tenantAMember.RoleName);
        Assert.Equal(tenantBId, tenantBMember.TenantId);
        Assert.Equal("Advisor", tenantBMember.RoleName);
        Assert.Equal(userId, tenantAMember.UserId);
        Assert.Equal(userId, tenantBMember.UserId);
        Assert.DoesNotContain(tenantBId.ToString(), tenantAResponseBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Advisor", tenantAResponseBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(tenantAId.ToString(), tenantBResponseBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Owner", tenantBResponseBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_2_2_2_Tenant_member_list_excludes_users_from_other_tenants()
    {
        var tenantAId = Guid.Parse("22222222-2222-2222-2222-2222222222a1");
        var tenantBId = Guid.Parse("22222222-2222-2222-2222-2222222222b1");
        var tenantAUserId = Guid.Parse("22222222-2222-2222-2222-2222222222c1");
        var tenantBUserId = Guid.Parse("22222222-2222-2222-2222-2222222222c2");
        await using var factory = CreateFactory("tc-2-2-2", dbContext =>
        {
            SeedTenants(dbContext, tenantAId, tenantBId);
            dbContext.Users.AddRange(
                CreateUser(tenantAUserId, tenantAId, "tenant-a.member@example.com", "Tenant A Member"),
                CreateUser(tenantBUserId, tenantBId, "tenant-b.member@example.com", "Tenant B Member"));
            dbContext.TenantMemberships.AddRange(
                CreateMembership(Guid.Parse("22222222-2222-2222-2222-2222222222d1"), tenantAId, tenantAUserId, "Admin"),
                CreateMembership(Guid.Parse("22222222-2222-2222-2222-2222222222d2"), tenantBId, tenantBUserId, "Admin"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        using var tenantARequest = CreateRequest(HttpMethod.Get, "/api/tenant-members", tenantAId, Guid.NewGuid(), Permission.ManageUsers);
        using var tenantBRequest = CreateRequest(HttpMethod.Get, "/api/tenant-members", tenantBId, Guid.NewGuid(), Permission.ManageUsers);

        var tenantAResponse = await client.SendAsync(tenantARequest);
        var tenantBResponse = await client.SendAsync(tenantBRequest);
        var tenantAResponseBody = await tenantAResponse.Content.ReadAsStringAsync();
        var tenantBResponseBody = await tenantBResponse.Content.ReadAsStringAsync();
        var tenantAMembers = JsonSerializer.Deserialize<TenantMemberDto[]>(tenantAResponseBody, JsonOptions);
        var tenantBMembers = JsonSerializer.Deserialize<TenantMemberDto[]>(tenantBResponseBody, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, tenantAResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, tenantBResponse.StatusCode);
        var tenantAMember = Assert.Single(tenantAMembers ?? []);
        var tenantBMember = Assert.Single(tenantBMembers ?? []);
        Assert.Equal(tenantAUserId, tenantAMember.UserId);
        Assert.Equal(tenantBUserId, tenantBMember.UserId);
        Assert.DoesNotContain("tenant-b.member@example.com", tenantAResponseBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(tenantBId.ToString(), tenantAResponseBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tenant-a.member@example.com", tenantBResponseBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(tenantAId.ToString(), tenantBResponseBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_2_2_3_Duplicate_membership_creation_is_rejected()
    {
        var tenantId = Guid.Parse("33333333-3333-3333-3333-3333333333a1");
        var userId = Guid.Parse("33333333-3333-3333-3333-3333333333b1");
        await using var factory = CreateFactory("tc-2-2-3", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-2.2.3 Tenant"));
            dbContext.Users.Add(CreateUser(userId, tenantId, "duplicate.member@example.com", "Duplicate Member"));
            dbContext.TenantMemberships.Add(CreateMembership(Guid.Parse("33333333-3333-3333-3333-3333333333c1"), tenantId, userId, "Admin"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            HttpMethod.Post,
            "/api/tenant-members",
            new AssignTenantMemberRequest(userId, "duplicate.member@example.com", "Duplicate Member", "Admin"),
            tenantId,
            Guid.NewGuid(),
            Permission.ManageUsers);

        var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("already a member", responseBody, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(1, await dbContext.TenantMemberships.CountAsync(candidate => candidate.TenantId == tenantId && candidate.UserId == userId));
    }

    [Fact]
    public async Task TC_2_2_4_Membership_add_update_and_deactivate_actions_are_audit_logged()
    {
        var tenantId = Guid.Parse("44444444-4444-4444-4444-4444444444a1");
        var userId = Guid.Parse("44444444-4444-4444-4444-4444444444b1");
        var actorUserId = Guid.Parse("44444444-4444-4444-4444-4444444444c1");
        await using var factory = CreateFactory("tc-2-2-4", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-2.2.4 Tenant"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();

        using var createRequest = CreateRequest(
            HttpMethod.Post,
            "/api/tenant-members",
            new AssignTenantMemberRequest(userId, "audit.member@example.com", "Audit Member", "Compliance Manager"),
            tenantId,
            actorUserId,
            Permission.ManageUsers);
        var createResponse = await client.SendAsync(createRequest);
        var createdMember = await createResponse.Content.ReadFromJsonAsync<TenantMemberDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createdMember);

        using var suspendRequest = CreateRequest(
            HttpMethod.Patch,
            $"/api/tenant-members/{createdMember.MembershipId}/status",
            new UpdateTenantMembershipStatusRequest(MembershipStatus.Suspended),
            tenantId,
            actorUserId,
            Permission.ManageUsers);
        using var deactivateRequest = CreateRequest(
            HttpMethod.Patch,
            $"/api/tenant-members/{createdMember.MembershipId}/status",
            new UpdateTenantMembershipStatusRequest(MembershipStatus.Deactivated),
            tenantId,
            actorUserId,
            Permission.ManageUsers);

        var suspendResponse = await client.SendAsync(suspendRequest);
        var deactivateResponse = await client.SendAsync(deactivateRequest);

        Assert.Equal(HttpStatusCode.OK, suspendResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var auditEvents = await dbContext.AuditLogEntries
            .Where(candidate => candidate.TenantId == tenantId && candidate.EntityId == createdMember.MembershipId.ToString())
            .OrderBy(candidate => candidate.OccurredAt)
            .ToListAsync();

        Assert.Equal(3, auditEvents.Count);
        Assert.Contains(auditEvents, candidate => candidate.Action == AuditAction.Created && candidate.Summary.Contains("added", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(2, auditEvents.Count(candidate => candidate.Action == AuditAction.Updated));
        Assert.All(auditEvents, candidate =>
        {
            Assert.Equal(actorUserId, candidate.ActorUserId);
            Assert.Equal("TenantMembership", candidate.EntityType);
            Assert.Contains(userId.ToString(), candidate.MetadataJson, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task Tenant_membership_actions_require_manage_users_permission()
    {
        var tenantId = Guid.Parse("55555555-5555-5555-5555-5555555555a1");
        await using var factory = CreateFactory("story-2-2-rbac", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "Story 2.2 RBAC Tenant"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();

        using var listRequest = CreateRequest(HttpMethod.Get, "/api/tenant-members", tenantId, Guid.NewGuid(), Permission.AuditorReadOnly);
        using var createRequest = CreateRequest(
            HttpMethod.Post,
            "/api/tenant-members",
            new AssignTenantMemberRequest(Guid.NewGuid(), "unauthorized@example.com", "Unauthorized User", "Admin"),
            tenantId,
            Guid.NewGuid(),
            Permission.AuditorReadOnly);

        var listResponse = await client.SendAsync(listRequest);
        var createResponse = await client.SendAsync(createRequest);

        Assert.Equal(HttpStatusCode.Forbidden, listResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);
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
                services.AddScoped<TenantMembershipService>();
                services.AddScoped<ITenantMembershipRepository, EfTenantMembershipRepository>();
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();

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
        TContent? content,
        Guid tenantId,
        Guid userId,
        Permission permission)
    {
        var request = CreateRequest(method, requestUri, tenantId, userId, permission);
        if (content is not null)
        {
            request.Content = JsonContent.Create(content, options: JsonOptions);
        }

        return request;
    }

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        string requestUri,
        Guid tenantId,
        Guid userId,
        Permission permission)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", permission.ToString());

        return request;
    }

    private static void SeedTenants(GccsDbContext dbContext, Guid tenantAId, Guid tenantBId)
    {
        dbContext.Tenants.AddRange(
            CreateTenant(tenantAId, "Tenant A"),
            CreateTenant(tenantBId, "Tenant B"));
    }

    private static TenantEntity CreateTenant(Guid tenantId, string name) =>
        new()
        {
            Id = tenantId,
            Name = name,
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.Parse("2026-06-13T12:00:00Z")
        };

    private static UserEntity CreateUser(Guid userId, Guid tenantId, string email, string displayName) =>
        new()
        {
            Id = userId,
            TenantId = tenantId,
            Email = email,
            DisplayName = displayName,
            Status = UserStatus.Active,
            MfaEnabled = true,
            CreatedAt = DateTimeOffset.Parse("2026-06-13T12:00:00Z")
        };

    private static TenantMembershipEntity CreateMembership(Guid membershipId, Guid tenantId, Guid userId, string roleName) =>
        new()
        {
            Id = membershipId,
            TenantId = tenantId,
            UserId = userId,
            Status = MembershipStatus.Active,
            RoleName = roleName,
            CreatedAt = DateTimeOffset.Parse("2026-06-13T12:00:00Z")
        };
}
