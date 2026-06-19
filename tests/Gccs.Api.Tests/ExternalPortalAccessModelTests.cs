using Gccs.Application.Audit;
using Gccs.Application.Portals;
using Gccs.Domain.Audit;
using Gccs.Infrastructure.Portals;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ExternalPortalAccessModelTests
{
    [Fact]
    public async Task TC_34_1_1_Tenant_admin_invites_external_reviewer_with_role_scope_expiration_package_and_download()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out _);

        var invitation = await service.InviteAsync(CreateRequest(ids), ids.TenantId, ids.ActorUserId);

        Assert.Equal(ids.TenantId, invitation.TenantId);
        Assert.Equal("reviewer@example.test", invitation.Email);
        Assert.Equal(ExternalPortalRole.PrimeReviewer, invitation.Role);
        Assert.Equal([ids.PackageId], invitation.PackageIds);
        Assert.Equal([ids.ContractId], invitation.ContractIds);
        Assert.True(invitation.CanDownload);
        Assert.True(invitation.StrongAuthenticationRequired);
    }

    [Fact]
    public async Task TC_34_1_2_Expired_or_revoked_invitations_cannot_be_used()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out _);
        var invitation = await service.InviteAsync(CreateRequest(ids), ids.TenantId, ids.ActorUserId);

        var expired = await service.ValidateAccessAsync(invitation.Id, ids.PackageId, ids.ContractId, invitation.ExpiresAt.AddSeconds(1), ids.ActorUserId);
        await service.RevokeAsync(invitation.Id, ids.ActorUserId);
        var revoked = await service.ValidateAccessAsync(invitation.Id, ids.PackageId, ids.ContractId, DateTimeOffset.UtcNow, ids.ActorUserId);

        Assert.False(expired.Allowed);
        Assert.False(revoked.Allowed);
    }

    [Fact]
    public async Task TC_34_1_3_Portal_user_accesses_only_assigned_packages_and_scoped_records()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out _);
        var invitation = await service.InviteAsync(CreateRequest(ids), ids.TenantId, ids.ActorUserId);

        var allowed = await service.ValidateAccessAsync(invitation.Id, ids.PackageId, ids.ContractId, DateTimeOffset.UtcNow, ids.ActorUserId);
        var wrongPackage = await service.ValidateAccessAsync(invitation.Id, ids.OtherPackageId, ids.ContractId, DateTimeOffset.UtcNow, ids.ActorUserId);
        var wrongContract = await service.ValidateAccessAsync(invitation.Id, ids.PackageId, ids.OtherContractId, DateTimeOffset.UtcNow, ids.ActorUserId);

        Assert.True(allowed.Allowed);
        Assert.False(wrongPackage.Allowed);
        Assert.False(wrongContract.Allowed);
    }

    [Fact]
    public async Task TC_34_1_4_Portal_users_cannot_modify_workspace_data()
    {
        var service = CreateService(out _);

        await Assert.ThrowsAsync<ExternalPortalAccessException>(() => service.EnsureReadOnlyAsync());
    }

    [Fact]
    public async Task TC_34_1_5_Invitation_access_resend_extension_and_revocation_are_audit_logged()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out var auditWriter);
        var invitation = await service.InviteAsync(CreateRequest(ids), ids.TenantId, ids.ActorUserId);
        await service.ValidateAccessAsync(invitation.Id, ids.PackageId, ids.ContractId, DateTimeOffset.UtcNow, ids.ActorUserId);
        await service.ResendAsync(invitation.Id, ids.ActorUserId);
        await service.ExtendAsync(invitation.Id, DateTimeOffset.UtcNow.AddDays(60), ids.ActorUserId);
        await service.RevokeAsync(invitation.Id, ids.ActorUserId);

        var events = auditWriter.Events.Where(auditEvent => auditEvent.EntityType == "ExternalPortalInvitation").ToArray();
        Assert.Equal(5, events.Length);
        Assert.Contains(events, audit => audit.Summary.Contains("created", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(events, audit => audit.Summary.Contains("access was granted", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(events, audit => audit.Summary.Contains("resent", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(events, audit => audit.Summary.Contains("extended", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(events, audit => audit.Summary.Contains("revoked", StringComparison.OrdinalIgnoreCase));
    }

    private static ExternalPortalAccessService CreateService(out CapturingAuditEventWriter auditWriter)
    {
        auditWriter = new CapturingAuditEventWriter();
        return new ExternalPortalAccessService(new InMemoryExternalPortalAccessRepository(), auditWriter);
    }

    private static ExternalPortalInvitationRequest CreateRequest(StoryIds ids) =>
        new(
            "reviewer@example.test",
            ExternalPortalRole.PrimeReviewer,
            [ids.PackageId],
            [ids.ContractId],
            DateTimeOffset.UtcNow.AddDays(30),
            CanDownload: true,
            StrongAuthenticationRequired: true);

    private sealed class CapturingAuditEventWriter : IAuditEventWriter
    {
        public List<CapturedAuditEvent> Events { get; } = [];

        public Task WriteAsync(
            Guid tenantId,
            Guid actorUserId,
            AuditAction action,
            string entityType,
            string entityId,
            string summary,
            IReadOnlyDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            Events.Add(new CapturedAuditEvent(tenantId, actorUserId, action, entityType, entityId, summary, metadata?.ToDictionary() ?? []));
            return Task.CompletedTask;
        }
    }

    private sealed record CapturedAuditEvent(
        Guid TenantId,
        Guid ActorUserId,
        AuditAction Action,
        string EntityType,
        string EntityId,
        string Summary,
        IReadOnlyDictionary<string, string> Metadata);

    private sealed record StoryIds(
        Guid TenantId,
        Guid PackageId,
        Guid OtherPackageId,
        Guid ContractId,
        Guid OtherContractId,
        Guid ActorUserId)
    {
        public static StoryIds Create() =>
            new(
                Guid.Parse("34134134-4134-1341-3413-4134134134aa"),
                Guid.Parse("34134134-4134-1341-3413-4134134134bb"),
                Guid.Parse("34134134-4134-1341-3413-4134134134bc"),
                Guid.Parse("34134134-4134-1341-3413-4134134134cc"),
                Guid.Parse("34134134-4134-1341-3413-4134134134cd"),
                Guid.Parse("34134134-4134-1341-3413-4134134134dd"));
    }
}
