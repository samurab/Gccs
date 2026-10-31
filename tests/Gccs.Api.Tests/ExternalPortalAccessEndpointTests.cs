using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Identity;
using Gccs.Application.Portals;
using Gccs.Domain.Audit;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Identity;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Portals;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ExternalPortalAccessEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly WebApplicationFactory<Program> _factory;

    public ExternalPortalAccessEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Tenant_admin_manages_durable_scoped_invitation_and_cross_tenant_mutation_is_hidden()
    {
        var ids = TestIds.Create();
        await using var factory = CreateFactory(nameof(Tenant_admin_manages_durable_scoped_invitation_and_cross_tenant_mutation_is_hidden), ids);
        using var client = factory.CreateClient();
        var invitation = await CreateInvitationAsync(client, ids);

        var listed = await client.SendAsync(AdminRequest(HttpMethod.Get, "/api/portal/invitations/", ids, ids.TenantId));
        Assert.Equal(HttpStatusCode.OK, listed.StatusCode);
        Assert.Single(Assert.IsType<ExternalPortalInvitationDto[]>(
            await listed.Content.ReadFromJsonAsync<ExternalPortalInvitationDto[]>(JsonOptions)));

        var crossTenant = await client.SendAsync(AdminRequest(
            HttpMethod.Post, $"/api/portal/invitations/{invitation.Id}/revoke", ids, ids.OtherTenantId,
            new { reason = "Cross-tenant attempt." }));
        Assert.Equal(HttpStatusCode.NotFound, crossTenant.StatusCode);

        var crossTenantHistory = await client.SendAsync(AdminRequest(
            HttpMethod.Get, $"/api/portal/invitations/{invitation.Id}/access-history", ids, ids.OtherTenantId));
        Assert.Equal(HttpStatusCode.NotFound, crossTenantHistory.StatusCode);

        var revoked = await client.SendAsync(AdminRequest(
            HttpMethod.Post, $"/api/portal/invitations/{invitation.Id}/revoke", ids, ids.TenantId,
            new { reason = "External review completed." }));
        Assert.Equal(HttpStatusCode.OK, revoked.StatusCode);
        var dto = await revoked.Content.ReadFromJsonAsync<ExternalPortalInvitationDto>(JsonOptions);
        Assert.Equal(ExternalPortalInvitationStatus.Revoked, dto?.Status);
        Assert.Equal("External review completed.", dto?.RevocationReason);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(2, await db.AuditLogEntries.CountAsync(entry =>
            entry.TenantId == ids.TenantId && entry.EntityType == "ExternalPortalInvitation"));
        Assert.Empty(await db.AuditLogEntries.Where(entry => entry.TenantId == ids.OtherTenantId).ToArrayAsync());
    }

    [Fact]
    public async Task Portal_access_binds_verified_identity_enforces_mfa_and_scope_and_records_history()
    {
        var ids = TestIds.Create();
        await using var factory = CreateFactory(nameof(Portal_access_binds_verified_identity_enforces_mfa_and_scope_and_records_history), ids);
        using var client = factory.CreateClient();
        var invitation = await CreateInvitationAsync(client, ids);

        var withoutMfa = await client.SendAsync(PortalRequest(invitation.Id, ids.PackageId, ids.ContractId, ids, ids.ExternalUserId));
        Assert.Equal(HttpStatusCode.Forbidden, withoutMfa.StatusCode);

        var allowed = await client.SendAsync(PortalRequest(
            invitation.Id, ids.PackageId, ids.ContractId, ids, ids.ExternalUserId, "reviewer@example.test", mfa: true));
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        var access = await allowed.Content.ReadFromJsonAsync<ExternalPortalAccessResultDto>(JsonOptions);
        Assert.True(access?.Allowed);
        Assert.Equal(ids.ExternalUserId, access?.Invitation?.ExternalUserId);
        Assert.NotNull(access?.Invitation?.LastAccessedAt);

        var wrongPackage = await client.SendAsync(PortalRequest(
            invitation.Id, Guid.NewGuid(), ids.ContractId, ids, ids.ExternalUserId, "reviewer@example.test", mfa: true));
        var differentSubject = await client.SendAsync(PortalRequest(
            invitation.Id, ids.PackageId, ids.ContractId, ids, Guid.NewGuid(), "reviewer@example.test", mfa: true));
        Assert.Equal(HttpStatusCode.Forbidden, wrongPackage.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, differentSubject.StatusCode);

        var history = await client.SendAsync(AdminRequest(
            HttpMethod.Get, $"/api/portal/invitations/{invitation.Id}/access-history", ids, ids.TenantId));
        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        var entries = Assert.IsType<ExternalPortalAccessHistoryDto[]>(
            await history.Content.ReadFromJsonAsync<ExternalPortalAccessHistoryDto[]>(JsonOptions));
        Assert.Equal(4, entries.Length);
        Assert.Single(entries, entry => entry.Allowed);
        Assert.Contains(entries, entry => entry.ResultCode == "strong_authentication_required");
        Assert.Contains(entries, entry => entry.ResultCode == "package_out_of_scope");
        Assert.Contains(entries, entry => entry.ResultCode == "identity_subject_mismatch");
    }

    [Fact]
    public async Task Portal_identity_has_no_workspace_write_authority_and_revocation_cuts_off_access()
    {
        var ids = TestIds.Create();
        await using var factory = CreateFactory(nameof(Portal_identity_has_no_workspace_write_authority_and_revocation_cuts_off_access), ids);
        using var client = factory.CreateClient();
        var invitation = await CreateInvitationAsync(client, ids);

        var directWrite = new HttpRequestMessage(HttpMethod.Post, "/api/contracts")
        {
            Content = JsonContent.Create(new { })
        };
        AddPortalAuth(directWrite, ids, ids.ExternalUserId, "reviewer@example.test", mfa: true);
        directWrite.Headers.Add("X-Gccs-Dev-Tenant", ids.TenantId.ToString());
        var deniedWrite = await client.SendAsync(directWrite);
        Assert.Equal(HttpStatusCode.Forbidden, deniedWrite.StatusCode);

        var revoke = await client.SendAsync(AdminRequest(
            HttpMethod.Post, $"/api/portal/invitations/{invitation.Id}/revoke", ids, ids.TenantId,
            new { reason = "Review access withdrawn." }));
        Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);
        var deniedAccess = await client.SendAsync(PortalRequest(
            invitation.Id, ids.PackageId, ids.ContractId, ids, ids.ExternalUserId, "reviewer@example.test", mfa: true));
        Assert.Equal(HttpStatusCode.Forbidden, deniedAccess.StatusCode);
    }

    [Fact]
    public async Task Portal_invitation_management_requires_tenant_admin_permission_without_business_side_effects()
    {
        var ids = TestIds.Create();
        await using var factory = CreateFactory(nameof(Portal_invitation_management_requires_tenant_admin_permission_without_business_side_effects), ids);
        using var client = factory.CreateClient();
        var request = AdminRequest(HttpMethod.Post, "/api/portal/invitations/", ids, ids.TenantId,
            new ExternalPortalInvitationRequest("reviewer@example.test", ExternalPortalRole.AuditorReviewer,
                [ids.PackageId], [ids.ContractId], DateTimeOffset.UtcNow.AddDays(30), false, true));
        request.Headers.Remove("X-Gccs-Dev-User");
        request.Headers.Remove("X-Gccs-Dev-Email");
        request.Headers.Add("X-Gccs-Dev-User", ids.ViewerUserId.ToString());
        request.Headers.Add("X-Gccs-Dev-Email", "auditor@example.test");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Empty(await db.ExternalPortalInvitations.ToArrayAsync());
        var audit = Assert.Single(await db.AuditLogEntries.ToArrayAsync());
        Assert.Equal(AuditAction.Rejected, audit.Action);
        Assert.DoesNotContain("ExternalPortalInvitation", audit.EntityType, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<ExternalPortalInvitationDto> CreateInvitationAsync(HttpClient client, TestIds ids)
    {
        var response = await client.SendAsync(AdminRequest(HttpMethod.Post, "/api/portal/invitations/", ids, ids.TenantId,
            new ExternalPortalInvitationRequest("reviewer@example.test", ExternalPortalRole.PrimeReviewer,
                [ids.PackageId], [ids.ContractId], DateTimeOffset.UtcNow.AddDays(30), true, true)));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<ExternalPortalInvitationDto>(
            await response.Content.ReadFromJsonAsync<ExternalPortalInvitationDto>(JsonOptions));
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, TestIds ids) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.UseSetting("Security:MembershipAuthorization:Enforce", "true");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IExternalPortalAccessRepository>();
                services.RemoveAll<IExternalPortalScopeValidator>();
                services.RemoveAll<IAuditEventWriter>();
                services.RemoveAll<ITenantMembershipRepository>();
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<IExternalPortalAccessRepository, EfExternalPortalAccessRepository>();
                services.AddSingleton<IExternalPortalScopeValidator, AllowAllScopeValidator>();
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();
                services.AddScoped<ITenantMembershipRepository, EfTenantMembershipRepository>();

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                db.Tenants.AddRange(Tenant(ids.TenantId, "Alpha"), Tenant(ids.OtherTenantId, "Bravo"));
                db.Users.Add(new UserEntity
                {
                    Id = ids.AdminUserId, TenantId = ids.TenantId, Email = "admin@example.test",
                    DisplayName = "Admin", Status = UserStatus.Active, MfaEnabled = true, CreatedAt = DateTimeOffset.UtcNow
                });
                db.Users.Add(new UserEntity
                {
                    Id = ids.ViewerUserId, TenantId = ids.TenantId, Email = "auditor@example.test",
                    DisplayName = "Auditor", Status = UserStatus.Active, MfaEnabled = true, CreatedAt = DateTimeOffset.UtcNow
                });
                db.TenantMemberships.Add(new TenantMembershipEntity
                {
                    Id = Guid.NewGuid(), TenantId = ids.TenantId, UserId = ids.AdminUserId,
                    Status = MembershipStatus.Active, RoleName = "Admin", CreatedAt = DateTimeOffset.UtcNow
                });
                db.TenantMemberships.Add(new TenantMembershipEntity
                {
                    Id = Guid.NewGuid(), TenantId = ids.TenantId, UserId = ids.ViewerUserId,
                    Status = MembershipStatus.Active, RoleName = "Auditor", CreatedAt = DateTimeOffset.UtcNow
                });
                db.TenantMemberships.Add(new TenantMembershipEntity
                {
                    Id = Guid.NewGuid(), TenantId = ids.OtherTenantId, UserId = ids.AdminUserId,
                    Status = MembershipStatus.Active, RoleName = "Admin", CreatedAt = DateTimeOffset.UtcNow
                });
                db.SaveChanges();
            });
        });

    private static TenantEntity Tenant(Guid id, string name) => new()
    {
        Id = id, Name = name, Status = TenantStatus.Active,
        DataPosture = TenantDataPosture.NoCui, CreatedAt = DateTimeOffset.UtcNow
    };

    private static HttpRequestMessage AdminRequest<T>(HttpMethod method, string uri, TestIds ids, Guid tenantId, T body)
    {
        var request = AdminRequest(method, uri, ids, tenantId);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        return request;
    }

    private static HttpRequestMessage AdminRequest(HttpMethod method, string uri, TestIds ids, Guid tenantId)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", ids.AdminUserId.ToString());
        request.Headers.Add("X-Gccs-Dev-Email", "admin@example.test");
        return request;
    }

    private static HttpRequestMessage PortalRequest(
        Guid invitationId, Guid packageId, Guid? contractId, TestIds ids, Guid userId,
        string email = "reviewer@example.test", bool mfa = false)
    {
        var uri = $"/api/external-portal/invitations/{invitationId}/access?packageId={packageId}" +
            (contractId is null ? string.Empty : $"&contractId={contractId}");
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        AddPortalAuth(request, ids, userId, email, mfa);
        return request;
    }

    private static void AddPortalAuth(HttpRequestMessage request, TestIds ids, Guid userId, string email, bool mfa)
    {
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", ids.TenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        request.Headers.Add("X-Gccs-Dev-Email", email);
        if (mfa) request.Headers.Add("X-Gccs-Dev-Amr", "mfa");
    }

    private sealed class AllowAllScopeValidator : IExternalPortalScopeValidator
    {
        public Task ValidateAsync(Guid tenantId, IReadOnlyList<Guid> packageIds, IReadOnlyList<Guid> contractIds,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed record TestIds(
        Guid TenantId, Guid OtherTenantId, Guid AdminUserId, Guid ViewerUserId, Guid ExternalUserId, Guid PackageId, Guid ContractId)
    {
        public static TestIds Create() => new(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
    }
}
