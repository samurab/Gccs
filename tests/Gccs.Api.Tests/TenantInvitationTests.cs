using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using System.Text;
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

public sealed class TenantInvitationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public TenantInvitationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_2_3_1_Admin_creates_invitation_with_token_expiration_pending_status_and_notification_placeholder()
    {
        var tenantId = Guid.Parse("23232323-2323-2323-2323-2323232323a1");
        var actorUserId = Guid.Parse("23232323-2323-2323-2323-2323232323b1");
        await using var factory = CreateFactory("tc-2-3-1", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-2.3.1 Tenant"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            HttpMethod.Post,
            "/api/tenant-invitations",
            new CreateTenantInvitationRequest("Invited.User@Example.com", "Compliance Manager", 5),
            tenantId,
            actorUserId,
            "admin@example.com",
            Permission.ManageUsers);

        var response = await client.SendAsync(request);
        var invitation = await response.Content.ReadFromJsonAsync<TenantInvitationDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(invitation);
        Assert.Equal(tenantId, invitation.TenantId);
        Assert.Equal("invited.user@example.com", invitation.Email);
        Assert.Equal("Compliance Manager", invitation.RoleName);
        Assert.Equal(TenantInvitationStatus.Pending, invitation.Status);
        Assert.True(invitation.ExpiresAt > DateTimeOffset.UtcNow.AddDays(4));
        Assert.Null(invitation.NotificationSentAt);
        Assert.Equal(InvitationDeliveryStatus.Queued, invitation.DeliveryStatus);
        Assert.DoesNotContain("token", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var persistedInvitation = await dbContext.TenantInvitations.SingleAsync(candidate => candidate.Id == invitation.InvitationId);
        var tenant = await dbContext.Tenants.SingleAsync(candidate => candidate.Id == tenantId);
        var auditEvent = await dbContext.AuditLogEntries.SingleAsync(candidate =>
            candidate.TenantId == tenantId &&
            candidate.EntityType == "TenantInvitation" &&
            candidate.EntityId == invitation.InvitationId.ToString());

        Assert.Equal(TenantDataPosture.NoCui, tenant.DataPosture);
        Assert.Equal(TenantInvitationStatus.Pending, persistedInvitation.Status);
        Assert.Equal(actorUserId, persistedInvitation.CreatedByUserId);
        Assert.Equal(actorUserId, auditEvent.ActorUserId);
        Assert.Equal(AuditAction.Created, auditEvent.Action);
        Assert.Contains("created", auditEvent.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Compliance Manager", auditEvent.MetadataJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Existing_tenant_member_cannot_receive_another_invitation_for_a_different_role()
    {
        var tenantId = Guid.Parse("23232323-2323-2323-2323-2323232323a7");
        var ownerUserId = Guid.Parse("23232323-2323-2323-2323-2323232323b7");
        await using var factory = CreateFactory("tenant-invitation-existing-member", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "Existing Member Tenant"));
            AddMember(dbContext, tenantId, ownerUserId, "owner@example.com", RoleCatalog.Owner);
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            HttpMethod.Post,
            "/api/tenant-invitations",
            new CreateTenantInvitationRequest("OWNER@example.com", RoleCatalog.ComplianceManager),
            tenantId,
            ownerUserId,
            "owner@example.com",
            Permission.ManageUsers);

        var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("already a member of the current tenant", responseBody, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Empty(await dbContext.TenantInvitations.ToListAsync());
        Assert.Single(await dbContext.TenantMemberships.ToListAsync());
    }

    [Fact]
    public async Task Existing_membership_in_another_tenant_does_not_block_an_invitation()
    {
        var tenantAId = Guid.Parse("23232323-2323-2323-2323-2323232323a8");
        var tenantBId = Guid.Parse("23232323-2323-2323-2323-2323232323b8");
        var userId = Guid.Parse("23232323-2323-2323-2323-2323232323c8");
        await using var factory = CreateFactory("tenant-invitation-other-tenant-member", dbContext =>
        {
            dbContext.Tenants.AddRange(
                CreateTenant(tenantAId, "Inviting Tenant"),
                CreateTenant(tenantBId, "Existing Membership Tenant"));
            AddMember(dbContext, tenantBId, userId, "multi.tenant@example.com", RoleCatalog.Owner);
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            HttpMethod.Post,
            "/api/tenant-invitations",
            new CreateTenantInvitationRequest("multi.tenant@example.com", RoleCatalog.Auditor),
            tenantAId,
            Guid.NewGuid(),
            "admin@example.com",
            Permission.ManageUsers);

        var response = await client.SendAsync(request);
        var invitation = await response.Content.ReadFromJsonAsync<TenantInvitationDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(tenantAId, Assert.IsType<TenantInvitationDto>(invitation).TenantId);
    }

    [Fact]
    public async Task Existing_tenant_member_cannot_accept_a_stale_invitation()
    {
        var tenantId = Guid.Parse("23232323-2323-2323-2323-2323232323a9");
        var ownerUserId = Guid.Parse("23232323-2323-2323-2323-2323232323b9");
        var invitationId = Guid.Parse("23232323-2323-2323-2323-2323232323c9");
        const string invitationCode = "stale-existing-member-token";
        await using var factory = CreateFactory("tenant-invitation-stale-existing-member", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "Stale Invitation Tenant"));
            AddMember(dbContext, tenantId, ownerUserId, "owner@example.com", RoleCatalog.Owner);
            dbContext.TenantInvitations.Add(CreateInvitation(
                invitationId,
                tenantId,
                "owner@example.com",
                RoleCatalog.ComplianceManager,
                invitationCode,
                TenantInvitationStatus.Pending));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            HttpMethod.Post,
            $"/api/invitations/{invitationCode}/accept",
            new AcceptTenantInvitationRequest("Tenant Owner"),
            tenantId,
            ownerUserId,
            "owner@example.com",
            Permission.AuditorReadOnly);

        var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("already a member of this tenant", responseBody, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var invitation = await dbContext.TenantInvitations.SingleAsync(candidate => candidate.Id == invitationId);
        Assert.Equal(TenantInvitationStatus.Pending, invitation.Status);
        Assert.NotNull(invitation.InvitationTokenHash);
        Assert.Single(await dbContext.TenantMemberships.ToListAsync());
    }

    [Theory]
    [InlineData(Permission.ManageEvidence, "contributor@example.com")]
    [InlineData(Permission.AuditorReadOnly, "auditor@example.com")]
    public async Task TC_2_3_2_Contributor_and_auditor_cannot_manage_invitations(
        Permission permission,
        string actorEmail)
    {
        var tenantId = Guid.Parse("23232323-2323-2323-2323-2323232323a2");
        var invitationId = Guid.Parse("23232323-2323-2323-2323-2323232323b2");
        var actorUserId = Guid.NewGuid();
        await using var factory = CreateFactory("tc-2-3-2", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-2.3.2 Tenant"));
            dbContext.TenantInvitations.Add(CreateInvitation(
                invitationId,
                tenantId,
                "pending.invite@example.com",
                "Contributor",
                "pending-token",
                TenantInvitationStatus.Pending));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        using var listRequest = CreateRequest(
            HttpMethod.Get,
            "/api/tenant-invitations",
            tenantId,
            actorUserId,
            actorEmail,
            permission);
        using var createRequest = CreateRequest(
            HttpMethod.Post,
            "/api/tenant-invitations",
            new CreateTenantInvitationRequest("blocked@example.com", "Contributor"),
            tenantId,
            actorUserId,
            actorEmail,
            permission);
        using var expireRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/tenant-invitations/{invitationId}/expire",
            tenantId,
            actorUserId,
            actorEmail,
            permission);
        using var revokeRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/tenant-invitations/{invitationId}/revoke",
            new RevokeTenantInvitationRequest("Authorization regression test."),
            tenantId,
            actorUserId,
            actorEmail,
            permission);

        var listResponse = await client.SendAsync(listRequest);
        var createResponse = await client.SendAsync(createRequest);
        var expireResponse = await client.SendAsync(expireRequest);
        var revokeResponse = await client.SendAsync(revokeRequest);

        Assert.Equal(HttpStatusCode.Forbidden, listResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, expireResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, revokeResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var invitations = await dbContext.TenantInvitations
            .Where(candidate => candidate.TenantId == tenantId)
            .ToListAsync();

        var invitation = Assert.Single(invitations);
        Assert.Equal(invitationId, invitation.Id);
        Assert.Equal(TenantInvitationStatus.Pending, invitation.Status);
        Assert.Null(invitation.RevokedAt);
        Assert.Null(invitation.UpdatedAt);
        var auditEvents = await dbContext.AuditLogEntries
            .Where(candidate => candidate.TenantId == tenantId)
            .ToListAsync();
        Assert.All(auditEvents, audit =>
        {
            Assert.Equal("Authorization", audit.EntityType);
            Assert.Equal(AuditAction.Rejected, audit.Action);
            Assert.Equal(actorUserId, audit.ActorUserId);
        });
        Assert.DoesNotContain(auditEvents, audit => audit.EntityType == "TenantInvitation");
    }

    [Fact]
    public async Task TC_2_3_3_Expired_or_revoked_invitations_cannot_be_accepted()
    {
        var tenantId = Guid.Parse("23232323-2323-2323-2323-2323232323a3");
        var expiredInvitationId = Guid.Parse("23232323-2323-2323-2323-2323232323b3");
        var revokedInvitationId = Guid.Parse("23232323-2323-2323-2323-2323232323c3");
        var expiredUserId = Guid.Parse("23232323-2323-2323-2323-2323232323d3");
        var revokedUserId = Guid.Parse("23232323-2323-2323-2323-2323232323e3");
        await using var factory = CreateFactory("tc-2-3-3", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-2.3.3 Tenant"));
            dbContext.TenantInvitations.AddRange(
                CreateInvitation(expiredInvitationId, tenantId, "expired@example.com", "Auditor", "expired-token", TenantInvitationStatus.Expired),
                CreateInvitation(revokedInvitationId, tenantId, "revoked@example.com", "Auditor", "revoked-token", TenantInvitationStatus.Revoked));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        using var expiredRequest = CreateRequest(
            HttpMethod.Post,
            "/api/invitations/expired-token/accept",
            new AcceptTenantInvitationRequest("Expired User"),
            tenantId,
            expiredUserId,
            "expired@example.com",
            Permission.AuditorReadOnly);
        using var revokedRequest = CreateRequest(
            HttpMethod.Post,
            "/api/invitations/revoked-token/accept",
            new AcceptTenantInvitationRequest("Revoked User"),
            tenantId,
            revokedUserId,
            "revoked@example.com",
            Permission.AuditorReadOnly);

        var expiredResponse = await client.SendAsync(expiredRequest);
        var revokedResponse = await client.SendAsync(revokedRequest);
        var expiredResponseBody = await expiredResponse.Content.ReadAsStringAsync();
        var revokedResponseBody = await revokedResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, expiredResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, revokedResponse.StatusCode);
        Assert.Equal("application/problem+json", expiredResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal("application/problem+json", revokedResponse.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Expired invitations cannot be accepted", expiredResponseBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Revoked invitations cannot be accepted", revokedResponseBody, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var tenant = await dbContext.Tenants.SingleAsync(candidate => candidate.Id == tenantId);
        Assert.Equal(TenantDataPosture.NoCui, tenant.DataPosture);
        Assert.False(await dbContext.TenantMemberships.AnyAsync(candidate => candidate.UserId == expiredUserId || candidate.UserId == revokedUserId));
        Assert.Equal(TenantInvitationStatus.Expired, (await dbContext.TenantInvitations.SingleAsync(candidate => candidate.Id == expiredInvitationId)).Status);
        Assert.Equal(TenantInvitationStatus.Revoked, (await dbContext.TenantInvitations.SingleAsync(candidate => candidate.Id == revokedInvitationId)).Status);
    }

    [Fact]
    public async Task TC_2_3_4_Invitation_create_accept_expire_and_revoke_actions_are_audit_logged()
    {
        var tenantId = Guid.Parse("23232323-2323-2323-2323-2323232323a4");
        var adminUserId = Guid.Parse("23232323-2323-2323-2323-2323232323b4");
        var invitedUserId = Guid.Parse("23232323-2323-2323-2323-2323232323c4");
        await using var factory = CreateFactory("tc-2-3-4", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-2.3.4 Tenant"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();

        var acceptedInvitation = await CreateInvitationAsync(client, tenantId, adminUserId, "accepted.audit@example.com", "Admin");
        var expiredInvitation = await CreateInvitationAsync(client, tenantId, adminUserId, "expired.audit@example.com", "Auditor");
        var revokedInvitation = await CreateInvitationAsync(client, tenantId, adminUserId, "revoked.audit@example.com", "Contributor");
        const string acceptedCode = "accepted-audit-token";
        using (var tokenScope = factory.Services.CreateScope())
        {
            var tokenDbContext = tokenScope.ServiceProvider.GetRequiredService<GccsDbContext>();
            var invitation = await tokenDbContext.TenantInvitations.SingleAsync(candidate => candidate.Id == acceptedInvitation.InvitationId);
            invitation.InvitationTokenHash = HashToken(acceptedCode);
            var invitationToRevoke = await tokenDbContext.TenantInvitations.SingleAsync(candidate =>
                candidate.Id == revokedInvitation.InvitationId);
            invitationToRevoke.InvitationTokenHash = HashToken("revoked-audit-token");
            invitationToRevoke.DeliveryStatus = InvitationDeliveryStatus.Sent;
            invitationToRevoke.NotificationSentAt = DateTimeOffset.UtcNow.AddMinutes(-5);
            invitationToRevoke.NotificationPlaceholder = "Owner invitation email was sent.";
            await tokenDbContext.SaveChangesAsync();
        }

        using var acceptRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/invitations/{acceptedCode}/accept",
            new AcceptTenantInvitationRequest("Accepted Audit"),
            tenantId,
            invitedUserId,
            acceptedInvitation.Email,
            Permission.AuditorReadOnly);
        using var expireRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/tenant-invitations/{expiredInvitation.InvitationId}/expire",
            tenantId,
            adminUserId,
            "admin@example.com",
            Permission.ManageUsers);
        using var revokeRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/tenant-invitations/{revokedInvitation.InvitationId}/revoke",
            new RevokeTenantInvitationRequest("Role assignment is no longer required."),
            tenantId,
            adminUserId,
            "admin@example.com",
            Permission.ManageUsers);

        var acceptResponse = await client.SendAsync(acceptRequest);
        var expireResponse = await client.SendAsync(expireRequest);
        var revokeResponse = await client.SendAsync(revokeRequest);

        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, expireResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, revokeResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var invitationIds = new[]
        {
            acceptedInvitation.InvitationId.ToString(),
            expiredInvitation.InvitationId.ToString(),
            revokedInvitation.InvitationId.ToString()
        };
        var tenant = await dbContext.Tenants.SingleAsync(candidate => candidate.Id == tenantId);
        var auditEvents = await dbContext.AuditLogEntries
            .Where(candidate => candidate.TenantId == tenantId && candidate.EntityType == "TenantInvitation" && invitationIds.Contains(candidate.EntityId))
            .ToListAsync();
        var membership = await dbContext.TenantMemberships.SingleAsync(candidate =>
            candidate.TenantId == tenantId &&
            candidate.UserId == invitedUserId);

        Assert.Equal(6, auditEvents.Count);
        Assert.Equal(TenantDataPosture.NoCui, tenant.DataPosture);
        Assert.Equal(3, auditEvents.Count(candidate => candidate.Action == AuditAction.Created));
        Assert.Contains(auditEvents, candidate => candidate.Action == AuditAction.Updated && candidate.Summary.Contains("accepted", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(auditEvents, candidate => candidate.Action == AuditAction.Updated && candidate.Summary.Contains("expired", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(auditEvents, candidate => candidate.Action == AuditAction.Updated && candidate.Summary.Contains("revoked", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            auditEvents,
            candidate =>
                candidate.Action == AuditAction.Updated &&
                candidate.Summary.Contains("revoked", StringComparison.OrdinalIgnoreCase) &&
                candidate.MetadataJson.Contains("Role assignment is no longer required.", StringComparison.Ordinal));
        Assert.All(auditEvents, candidate => Assert.Contains("roleName", candidate.MetadataJson, StringComparison.OrdinalIgnoreCase));
        var persistedRevokedInvitation = await dbContext.TenantInvitations.SingleAsync(candidate =>
            candidate.Id == revokedInvitation.InvitationId);
        Assert.Null(persistedRevokedInvitation.InvitationTokenHash);
        Assert.Equal(InvitationDeliveryStatus.Sent, persistedRevokedInvitation.DeliveryStatus);
        Assert.Equal("Admin", membership.RoleName);
        Assert.Equal(MembershipStatus.Active, membership.Status);
    }

    [Fact]
    public async Task Revoke_requires_a_pending_invitation_and_cancels_queued_delivery()
    {
        var tenantId = Guid.Parse("23232323-2323-2323-2323-2323232323a7");
        var actorUserId = Guid.Parse("23232323-2323-2323-2323-2323232323b7");
        var acceptedInvitationId = Guid.Parse("23232323-2323-2323-2323-2323232323c7");
        var pendingInvitationId = Guid.Parse("23232323-2323-2323-2323-2323232323d7");
        await using var factory = CreateFactory("tenant-invitation-revoke-state", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "Invitation Revoke State Tenant"));
            var accepted = CreateInvitation(
                acceptedInvitationId,
                tenantId,
                "accepted@example.com",
                "Admin",
                "accepted-token",
                TenantInvitationStatus.Accepted);
            var pending = CreateInvitation(
                pendingInvitationId,
                tenantId,
                "pending@example.com",
                "Contributor",
                "pending-token",
                TenantInvitationStatus.Pending);
            pending.DeliveryStatus = InvitationDeliveryStatus.Queued;
            pending.NotificationSentAt = null;
            pending.NotificationPlaceholder = "Invitation is queued for delivery.";
            dbContext.TenantInvitations.AddRange(accepted, pending);
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        using var acceptedRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/tenant-invitations/{acceptedInvitationId}/revoke",
            new RevokeTenantInvitationRequest("This must not overwrite accepted state."),
            tenantId,
            actorUserId,
            "admin@example.com",
            Permission.ManageUsers);
        using var pendingRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/tenant-invitations/{pendingInvitationId}/revoke",
            new RevokeTenantInvitationRequest("The invited user no longer needs access."),
            tenantId,
            actorUserId,
            "admin@example.com",
            Permission.ManageUsers);
        using var missingReasonRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/tenant-invitations/{pendingInvitationId}/revoke",
            new RevokeTenantInvitationRequest(" "),
            tenantId,
            actorUserId,
            "admin@example.com",
            Permission.ManageUsers);

        var acceptedResponse = await client.SendAsync(acceptedRequest);
        var missingReasonResponse = await client.SendAsync(missingReasonRequest);
        var pendingResponse = await client.SendAsync(pendingRequest);

        Assert.Equal(HttpStatusCode.BadRequest, acceptedResponse.StatusCode);
        Assert.Contains(
            "Only pending invitations can be revoked.",
            await acceptedResponse.Content.ReadAsStringAsync(),
            StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.BadRequest, missingReasonResponse.StatusCode);
        Assert.Contains(
            "A revocation reason is required",
            await missingReasonResponse.Content.ReadAsStringAsync(),
            StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, pendingResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var accepted = await dbContext.TenantInvitations.SingleAsync(candidate => candidate.Id == acceptedInvitationId);
        var revoked = await dbContext.TenantInvitations.SingleAsync(candidate => candidate.Id == pendingInvitationId);
        Assert.Equal(TenantInvitationStatus.Accepted, accepted.Status);
        Assert.NotNull(accepted.InvitationTokenHash);
        Assert.Equal(TenantInvitationStatus.Revoked, revoked.Status);
        Assert.Equal(InvitationDeliveryStatus.Cancelled, revoked.DeliveryStatus);
        Assert.Null(revoked.InvitationTokenHash);
        Assert.Equal(actorUserId, revoked.RevokedByUserId);
        Assert.Null(revoked.NextDeliveryAttemptAt);
        Assert.Null(revoked.DeliveryLeaseUntil);
    }

    [Fact]
    public async Task Tenant_invitation_list_and_state_changes_are_scoped_to_current_tenant()
    {
        var tenantAId = Guid.Parse("23232323-2323-2323-2323-2323232323a5");
        var tenantBId = Guid.Parse("23232323-2323-2323-2323-2323232323b5");
        var tenantAInvitationId = Guid.Parse("23232323-2323-2323-2323-2323232323c5");
        var tenantBInvitationId = Guid.Parse("23232323-2323-2323-2323-2323232323d5");
        await using var factory = CreateFactory("tc-2-3-tenant-scope", dbContext =>
        {
            dbContext.Tenants.AddRange(
                CreateTenant(tenantAId, "TC-2.3 Tenant A"),
                CreateTenant(tenantBId, "TC-2.3 Tenant B"));
            dbContext.TenantInvitations.AddRange(
                CreateInvitation(tenantAInvitationId, tenantAId, "tenant-a.invite@example.com", "Admin", "tenant-a-token", TenantInvitationStatus.Pending),
                CreateInvitation(tenantBInvitationId, tenantBId, "tenant-b.invite@example.com", "Admin", "tenant-b-token", TenantInvitationStatus.Pending));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        using var tenantAListRequest = CreateRequest(HttpMethod.Get, "/api/tenant-invitations", tenantAId, Guid.NewGuid(), "admin-a@example.com", Permission.ManageUsers);
        using var tenantBListRequest = CreateRequest(HttpMethod.Get, "/api/tenant-invitations", tenantBId, Guid.NewGuid(), "admin-b@example.com", Permission.ManageUsers);
        using var crossTenantRevokeRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/tenant-invitations/{tenantBInvitationId}/revoke",
            new RevokeTenantInvitationRequest("Cross-tenant isolation test."),
            tenantAId,
            Guid.NewGuid(),
            "admin-a@example.com",
            Permission.ManageUsers);

        var tenantAListResponse = await client.SendAsync(tenantAListRequest);
        var tenantBListResponse = await client.SendAsync(tenantBListRequest);
        var crossTenantRevokeResponse = await client.SendAsync(crossTenantRevokeRequest);
        var tenantAInvitations = await tenantAListResponse.Content.ReadFromJsonAsync<TenantInvitationDto[]>(JsonOptions);
        var tenantBInvitations = await tenantBListResponse.Content.ReadFromJsonAsync<TenantInvitationDto[]>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, tenantAListResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, tenantBListResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, crossTenantRevokeResponse.StatusCode);
        var tenantAInvitation = Assert.Single(tenantAInvitations ?? []);
        var tenantBInvitation = Assert.Single(tenantBInvitations ?? []);
        Assert.Equal(tenantAInvitationId, tenantAInvitation.InvitationId);
        Assert.Equal(tenantBInvitationId, tenantBInvitation.InvitationId);
        Assert.DoesNotContain("tenant-b.invite@example.com", JsonSerializer.Serialize(tenantAInvitations, JsonOptions), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tenant-a.invite@example.com", JsonSerializer.Serialize(tenantBInvitations, JsonOptions), StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var tenantPostures = await dbContext.Tenants
            .Where(candidate => candidate.Id == tenantAId || candidate.Id == tenantBId)
            .Select(candidate => candidate.DataPosture)
            .ToListAsync();
        var tenantBInvitationStatus = await dbContext.TenantInvitations
            .Where(candidate => candidate.Id == tenantBInvitationId)
            .Select(candidate => candidate.Status)
            .SingleAsync();

        Assert.All(tenantPostures, posture => Assert.Equal(TenantDataPosture.NoCui, posture));
        Assert.Equal(TenantInvitationStatus.Pending, tenantBInvitationStatus);
    }

    [Fact]
    public async Task Acceptance_context_requires_matching_email_and_token_cannot_be_replayed()
    {
        var tenantId = Guid.Parse("23232323-2323-2323-2323-2323232323a6");
        const string invitationCode = "single-use-owner-token";
        await using var factory = CreateFactory("tc-2-3-acceptance-security", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "Acceptance Security Tenant"));
            dbContext.TenantInvitations.Add(CreateInvitation(
                Guid.NewGuid(), tenantId, "owner@example.com", "Owner", invitationCode, TenantInvitationStatus.Pending));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        using var wrongEmailRequest = CreateRequest(
            HttpMethod.Get, $"/api/invitations/{invitationCode}", tenantId, Guid.NewGuid(), "attacker@example.com", Permission.AuditorReadOnly);
        using var acceptRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/invitations/{invitationCode}/accept",
            new AcceptTenantInvitationRequest("Tenant Owner"),
            tenantId,
            Guid.Parse("23232323-2323-2323-2323-2323232323b6"),
            "owner@example.com",
            Permission.AuditorReadOnly);
        using var replayRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/invitations/{invitationCode}/accept",
            new AcceptTenantInvitationRequest("Tenant Owner"),
            tenantId,
            Guid.Parse("23232323-2323-2323-2323-2323232323b6"),
            "owner@example.com",
            Permission.AuditorReadOnly);

        var wrongEmailResponse = await client.SendAsync(wrongEmailRequest);
        var acceptResponse = await client.SendAsync(acceptRequest);
        var replayResponse = await client.SendAsync(replayRequest);

        Assert.Equal(HttpStatusCode.BadRequest, wrongEmailResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, replayResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var persistedInvitation = await scope.ServiceProvider.GetRequiredService<GccsDbContext>().TenantInvitations.SingleAsync();
        Assert.Null(persistedInvitation.InvitationTokenHash);
    }

    private async Task<TenantInvitationDto> CreateInvitationAsync(
        HttpClient client,
        Guid tenantId,
        Guid actorUserId,
        string email,
        string roleName)
    {
        using var request = CreateRequest(
            HttpMethod.Post,
            "/api/tenant-invitations",
            new CreateTenantInvitationRequest(email, roleName),
            tenantId,
            actorUserId,
            "admin@example.com",
            Permission.ManageUsers);

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var invitation = await response.Content.ReadFromJsonAsync<TenantInvitationDto>(JsonOptions);
        return Assert.IsType<TenantInvitationDto>(invitation);
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
                services.AddScoped<TenantInvitationService>();
                services.AddScoped<ITenantInvitationRepository, EfTenantInvitationRepository>();
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
        string email,
        Permission permission)
    {
        var request = CreateRequest(method, requestUri, tenantId, userId, email, permission);
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
        string email,
        Permission permission)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        request.Headers.Add("X-Gccs-Dev-Email", email);
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
            CreatedAt = DateTimeOffset.Parse("2026-06-13T12:00:00Z")
        };

    private static void AddMember(
        GccsDbContext dbContext,
        Guid tenantId,
        Guid userId,
        string email,
        string roleName)
    {
        var createdAt = DateTimeOffset.Parse("2026-06-13T12:00:00Z");
        dbContext.Users.Add(new UserEntity
        {
            Id = userId,
            TenantId = tenantId,
            Email = email,
            DisplayName = "Existing Member",
            Status = UserStatus.Active,
            MfaEnabled = true,
            CreatedAt = createdAt
        });
        dbContext.TenantMemberships.Add(new TenantMembershipEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            RoleName = roleName,
            Status = MembershipStatus.Active,
            CreatedAt = createdAt
        });
    }

    private static TenantInvitationEntity CreateInvitation(
        Guid invitationId,
        Guid tenantId,
        string email,
        string roleName,
        string invitationToken,
        TenantInvitationStatus status) =>
        new()
        {
            Id = invitationId,
            TenantId = tenantId,
            Email = email,
            RoleName = roleName,
            InvitationTokenHash = HashToken(invitationToken),
            Status = status,
            ExpiresAt = status == TenantInvitationStatus.Expired
                ? DateTimeOffset.UtcNow.AddDays(-1)
                : DateTimeOffset.UtcNow.AddDays(3),
            NotificationSentAt = DateTimeOffset.UtcNow.AddDays(-2),
            NotificationPlaceholder = "Owner invitation email was sent.",
            DeliveryStatus = InvitationDeliveryStatus.Sent,
            DeliveryAttemptCount = 1,
            LastDeliveryAttemptAt = DateTimeOffset.UtcNow.AddDays(-2),
            CreatedAt = DateTimeOffset.Parse("2026-06-13T12:00:00Z")
        };

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
