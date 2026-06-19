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

public sealed class SsoSignInEnforcementTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public SsoSignInEnforcementTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_35_2_1_Admin_sets_each_sso_enforcement_mode_with_confirmation_and_permission_checks()
    {
        var tenantId = Guid.Parse("35235235-2352-3523-5235-2352352352a1");
        var actorUserId = Guid.Parse("35235235-2352-3523-5235-2352352352b1");
        await using var factory = CreateFactory("tc-35-2-1", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-35.2.1 Tenant"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();

        foreach (var mode in Enum.GetValues<SsoEnforcementMode>())
        {
            var response = await client.SendAsync(CreateRequest(
                HttpMethod.Put,
                "/api/enterprise/sso-policy",
                new UpdateTenantSsoPolicyRequest(mode, "CONFIRM SSO POLICY", RequiredEmailDomain: "example.com"),
                tenantId,
                actorUserId,
                Permission.ManageUsers));

            var policy = await response.Content.ReadFromJsonAsync<TenantSsoPolicyDto>(JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(policy);
            Assert.Equal(mode, policy.Mode);
            Assert.Equal("example.com", policy.RequiredEmailDomain);
        }

        using var missingConfirmation = CreateRequest(
            HttpMethod.Put,
            "/api/enterprise/sso-policy",
            new UpdateTenantSsoPolicyRequest(SsoEnforcementMode.RequiredForMembers, "wrong"),
            tenantId,
            actorUserId,
            Permission.ManageUsers);
        using var forbidden = CreateRequest(
            HttpMethod.Put,
            "/api/enterprise/sso-policy",
            new UpdateTenantSsoPolicyRequest(SsoEnforcementMode.Optional, "CONFIRM SSO POLICY"),
            tenantId,
            actorUserId,
            Permission.ViewEvidence);

        var missingConfirmationResponse = await client.SendAsync(missingConfirmation);
        var forbiddenResponse = await client.SendAsync(forbidden);
        var persistedResponse = await client.SendAsync(CreateRequest(HttpMethod.Get, "/api/enterprise/sso-policy", tenantId, actorUserId, Permission.ManageUsers));
        var persisted = await persistedResponse.Content.ReadFromJsonAsync<TenantSsoPolicyDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, missingConfirmationResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
        Assert.Equal(SsoEnforcementMode.Disabled, persisted?.Mode);
    }

    [Fact]
    public async Task TC_35_2_2_Saml_sign_in_links_matching_existing_member()
    {
        var tenantId = Guid.Parse("35235235-2352-3523-5235-2352352352a2");
        var actorUserId = Guid.Parse("35235235-2352-3523-5235-2352352352b2");
        var memberUserId = Guid.Parse("35235235-2352-3523-5235-2352352352c2");
        await using var factory = CreateFactory("tc-35-2-2", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-35.2.2 Tenant"));
            AddMember(dbContext, tenantId, memberUserId, "member@example.com", MembershipStatus.Active, UserStatus.Active);
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        await PutPolicyAsync(client, tenantId, actorUserId, SsoEnforcementMode.RequiredForMembers);

        var response = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            "/api/enterprise/sso/sign-in-evaluations",
            new SsoSignInEvaluationRequest("saml|member-1", "member@example.com"),
            tenantId,
            actorUserId,
            Permission.ManageUsers));
        var result = await response.Content.ReadFromJsonAsync<SsoSignInEvaluationResult>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(result?.Allowed);
        Assert.Equal(memberUserId, result?.UserId);
        Assert.NotNull(result?.SamlAccountLinkId);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var link = await dbContext.SamlAccountLinks.SingleAsync(candidate => candidate.TenantId == tenantId);
        Assert.Equal("saml|member-1", link.SamlSubject);
        Assert.Equal("member@example.com", link.Email);
        Assert.NotNull(link.LastSuccessfulSignInAt);
    }

    [Fact]
    public async Task TC_35_2_3_Invalid_sso_attempts_are_denied()
    {
        var tenantId = Guid.Parse("35235235-2352-3523-5235-2352352352a3");
        var otherTenantId = Guid.Parse("35235235-2352-3523-5235-2352352352b3");
        var actorUserId = Guid.Parse("35235235-2352-3523-5235-2352352352c3");
        await using var factory = CreateFactory("tc-35-2-3", dbContext =>
        {
            dbContext.Tenants.AddRange(
                CreateTenant(tenantId, "TC-35.2.3 Tenant A"),
                CreateTenant(otherTenantId, "TC-35.2.3 Tenant B"));
            AddMember(dbContext, tenantId, Guid.Parse("35235235-2352-3523-5235-2352352352d3"), "inactive@example.com", MembershipStatus.Suspended, UserStatus.Active);
            AddMember(dbContext, otherTenantId, Guid.Parse("35235235-2352-3523-5235-2352352352e3"), "other@example.com", MembershipStatus.Active, UserStatus.Active);
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        await PutPolicyAsync(
            client,
            tenantId,
            actorUserId,
            SsoEnforcementMode.RequiredForMembers,
            new Dictionary<string, string> { ["department"] = "contracts" });

        var unmapped = await EvaluateAsync(client, tenantId, actorUserId, "saml|unmapped", "missing@example.com", new Dictionary<string, string> { ["department"] = "contracts" });
        var inactive = await EvaluateAsync(client, tenantId, actorUserId, "saml|inactive", "inactive@example.com", new Dictionary<string, string> { ["department"] = "contracts" });
        var crossTenant = await EvaluateAsync(client, tenantId, actorUserId, "saml|other", "other@example.com", new Dictionary<string, string> { ["department"] = "contracts" });
        var missingAttribute = await EvaluateAsync(client, tenantId, actorUserId, "saml|missing-attribute", "missing@example.com", new Dictionary<string, string>());

        Assert.False(unmapped.Allowed);
        Assert.Equal("unmapped_saml_account", unmapped.ReasonCode);
        Assert.False(inactive.Allowed);
        Assert.Equal("inactive_member", inactive.ReasonCode);
        Assert.False(crossTenant.Allowed);
        Assert.Equal("unmapped_saml_account", crossTenant.ReasonCode);
        Assert.False(missingAttribute.Allowed);
        Assert.Equal("missing_required_attribute", missingAttribute.ReasonCode);
    }

    [Fact]
    public async Task TC_35_2_4_Break_glass_requires_approval_reason_and_expiration_and_expires()
    {
        var tenantId = Guid.Parse("35235235-2352-3523-5235-2352352352a4");
        var actorUserId = Guid.Parse("35235235-2352-3523-5235-2352352352b4");
        var memberUserId = Guid.Parse("35235235-2352-3523-5235-2352352352c4");
        await using var factory = CreateFactory("tc-35-2-4", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-35.2.4 Tenant"));
            AddMember(dbContext, tenantId, memberUserId, "breakglass@example.com", MembershipStatus.Active, UserStatus.Active);
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();

        var invalid = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            "/api/enterprise/sso/break-glass",
            new CreateBreakGlassAccessRequest(memberUserId, "", actorUserId, "INC-100", DateTimeOffset.UtcNow.AddMinutes(10)),
            tenantId,
            actorUserId,
            Permission.ManageUsers));
        var createdResponse = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            "/api/enterprise/sso/break-glass",
            new CreateBreakGlassAccessRequest(memberUserId, "Emergency IdP outage", actorUserId, "INC-101", DateTimeOffset.UtcNow.AddMinutes(10)),
            tenantId,
            actorUserId,
            Permission.ManageUsers));
        var grant = await createdResponse.Content.ReadFromJsonAsync<BreakGlassAccessGrantDto>(JsonOptions);
        var useResponse = await client.SendAsync(CreateRequest(HttpMethod.Post, $"/api/enterprise/sso/break-glass/{grant!.Id}/use", tenantId, actorUserId, Permission.ManageUsers));
        var useResult = await useResponse.Content.ReadFromJsonAsync<SsoSignInEvaluationResult>(JsonOptions);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            var entity = await dbContext.BreakGlassAccessGrants.SingleAsync(candidate => candidate.Id == grant.Id);
            entity.Status = BreakGlassGrantStatus.Active;
            entity.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            await dbContext.SaveChangesAsync();
        }

        var expiredResponse = await client.SendAsync(CreateRequest(HttpMethod.Post, $"/api/enterprise/sso/break-glass/{grant.Id}/use", tenantId, actorUserId, Permission.ManageUsers));
        var expired = await expiredResponse.Content.ReadFromJsonAsync<SsoSignInEvaluationResult>(JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        Assert.Equal(BreakGlassGrantStatus.Active, grant.Status);
        Assert.True(useResult?.Allowed);
        Assert.True(useResult?.UsedBreakGlass);
        Assert.False(expired?.Allowed);
        Assert.Equal("break_glass_expired", expired?.ReasonCode);
    }

    [Fact]
    public async Task TC_35_2_5_Sso_and_break_glass_actions_create_audit_events()
    {
        var tenantId = Guid.Parse("35235235-2352-3523-5235-2352352352a5");
        var actorUserId = Guid.Parse("35235235-2352-3523-5235-2352352352b5");
        var memberUserId = Guid.Parse("35235235-2352-3523-5235-2352352352c5");
        await using var factory = CreateFactory("tc-35-2-5", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-35.2.5 Tenant"));
            AddMember(dbContext, tenantId, memberUserId, "audit-member@example.com", MembershipStatus.Active, UserStatus.Active);
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();

        await PutPolicyAsync(client, tenantId, actorUserId, SsoEnforcementMode.RequiredForMembers);
        await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            "/api/enterprise/sso/sign-in-evaluations",
            new SsoSignInEvaluationRequest("saml|audit-member", "audit-member@example.com"),
            tenantId,
            actorUserId,
            Permission.ManageUsers));
        await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            "/api/enterprise/sso/sign-in-evaluations",
            new SsoSignInEvaluationRequest("saml|audit-failed", "nobody@example.com"),
            tenantId,
            actorUserId,
            Permission.ManageUsers));
        var grantResponse = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            "/api/enterprise/sso/break-glass",
            new CreateBreakGlassAccessRequest(memberUserId, "Audit trail verification", actorUserId, "INC-200", DateTimeOffset.UtcNow.AddMinutes(10)),
            tenantId,
            actorUserId,
            Permission.ManageUsers));
        var grant = await grantResponse.Content.ReadFromJsonAsync<BreakGlassAccessGrantDto>(JsonOptions);
        await client.SendAsync(CreateRequest(HttpMethod.Post, $"/api/enterprise/sso/break-glass/{grant!.Id}/use", tenantId, actorUserId, Permission.ManageUsers));

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var audits = await dbContext.AuditLogEntries
            .Where(audit => audit.TenantId == tenantId)
            .OrderBy(audit => audit.OccurredAt)
            .ToListAsync();

        Assert.Contains(audits, audit => audit.EntityType == "TenantSsoPolicy" && audit.Action == AuditAction.Updated);
        Assert.Contains(audits, audit => audit.EntityType == "SamlAccountLink" && audit.Action == AuditAction.Created);
        Assert.Contains(audits, audit => audit.EntityType == "SamlAccountLink" && audit.Action == AuditAction.SignedIn);
        Assert.Contains(audits, audit => audit.EntityType == "SsoSignInAttempt" && audit.Action == AuditAction.Rejected);
        Assert.Contains(audits, audit => audit.EntityType == "BreakGlassAccessGrant" && audit.Action == AuditAction.Approved);
        Assert.Contains(audits, audit => audit.EntityType == "BreakGlassAccessGrant" && audit.Action == AuditAction.SignedIn);
    }

    private static async Task PutPolicyAsync(
        HttpClient client,
        Guid tenantId,
        Guid actorUserId,
        SsoEnforcementMode mode,
        IReadOnlyDictionary<string, string>? requiredAttributes = null)
    {
        var response = await client.SendAsync(CreateRequest(
            HttpMethod.Put,
            "/api/enterprise/sso-policy",
            new UpdateTenantSsoPolicyRequest(mode, "CONFIRM SSO POLICY", RequiredAttributes: requiredAttributes),
            tenantId,
            actorUserId,
            Permission.ManageUsers));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<SsoSignInEvaluationResult> EvaluateAsync(
        HttpClient client,
        Guid tenantId,
        Guid actorUserId,
        string samlSubject,
        string email,
        IReadOnlyDictionary<string, string> attributes)
    {
        var response = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            "/api/enterprise/sso/sign-in-evaluations",
            new SsoSignInEvaluationRequest(samlSubject, email, Attributes: attributes),
            tenantId,
            actorUserId,
            Permission.ManageUsers));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Assert.IsType<SsoSignInEvaluationResult>(await response.Content.ReadFromJsonAsync<SsoSignInEvaluationResult>(JsonOptions));
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
                services.AddScoped<SsoSignInEnforcementService>();
                services.AddScoped<ISsoSignInEnforcementRepository, EfSsoSignInEnforcementRepository>();
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

    private static void AddMember(
        GccsDbContext dbContext,
        Guid tenantId,
        Guid userId,
        string email,
        MembershipStatus membershipStatus,
        UserStatus userStatus)
    {
        dbContext.Users.Add(new UserEntity
        {
            Id = userId,
            TenantId = tenantId,
            Email = email,
            DisplayName = email,
            Status = userStatus,
            MfaEnabled = true,
            CreatedAt = DateTimeOffset.Parse("2026-06-19T12:00:00Z")
        });
        dbContext.TenantMemberships.Add(new TenantMembershipEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Status = membershipStatus,
            RoleName = RoleCatalog.Admin,
            CreatedAt = DateTimeOffset.Parse("2026-06-19T12:00:00Z")
        });
    }
}
