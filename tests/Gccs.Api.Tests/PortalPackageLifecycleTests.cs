using Gccs.Application.Audit;
using Gccs.Application.Portals;
using Gccs.Domain.Audit;
using Gccs.Infrastructure.Portals;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class PortalPackageLifecycleTests
{
    [Fact]
    public async Task TC_34_3_1_Shared_packages_move_through_allowed_lifecycle_states()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out _);
        var shared = await service.ShareAsync(CreateRequest(ids), ids.TenantId, ids.ActorUserId);

        var superseded = await service.SupersedeAsync(shared.Id, ids.ReplacementPackageId, ids.ActorUserId);
        var reissued = await service.ReissueAsync(shared.Id, DateTimeOffset.UtcNow.AddDays(60), ids.ActorUserId);
        var expired = await service.ExpireAsync(shared.Id, ids.ActorUserId);
        var revoked = await service.RevokeAsync(shared.Id, "Over-shared package.", ids.ActorUserId);
        var archived = await service.ArchiveAsync(shared.Id, ids.ActorUserId);

        Assert.Equal(SharedPortalPackageState.Superseded, superseded?.State);
        Assert.Equal(SharedPortalPackageState.Active, reissued?.State);
        Assert.Equal(SharedPortalPackageState.Expired, expired?.State);
        Assert.Equal(SharedPortalPackageState.Revoked, revoked?.State);
        Assert.Equal(SharedPortalPackageState.Archived, archived?.State);
    }

    [Fact]
    public async Task TC_34_3_2_Revoked_active_package_loses_access_immediately()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out _);
        var shared = await service.ShareAsync(CreateRequest(ids), ids.TenantId, ids.ActorUserId);

        Assert.True(await service.CanAccessAsync(shared.Id, DateTimeOffset.UtcNow));
        await service.RevokeAsync(shared.Id, "Customer request.", ids.ActorUserId);

        Assert.False(await service.CanAccessAsync(shared.Id, DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task TC_34_3_3_Superseded_package_links_to_replacement_version()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out _);
        var shared = await service.ShareAsync(CreateRequest(ids), ids.TenantId, ids.ActorUserId);

        var superseded = await service.SupersedeAsync(shared.Id, ids.ReplacementPackageId, ids.ActorUserId);

        Assert.Equal(ids.ReplacementPackageId, superseded?.ReplacementPackageId);
        Assert.Equal(SharedPortalPackageState.Superseded, superseded?.State);
    }

    [Fact]
    public async Task TC_34_3_4_Activity_report_includes_access_comments_downloads_expiration_supersede_and_revocation()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out _);
        var shared = await service.ShareAsync(CreateRequest(ids), ids.TenantId, ids.ActorUserId);
        await service.RecordActivityAsync(shared.Id, PortalPackageActivityType.Comment, ids.ActorUserId);
        await service.RecordActivityAsync(shared.Id, PortalPackageActivityType.Download, ids.ActorUserId);
        await service.ExpireAsync(shared.Id, ids.ActorUserId);
        await service.SupersedeAsync(shared.Id, ids.ReplacementPackageId, ids.ActorUserId);
        await service.RevokeAsync(shared.Id, "Revoked.", ids.ActorUserId);

        var report = await service.GenerateActivityReportAsync(ids.TenantId);

        Assert.Contains(report.Activities, activity => activity.ActivityType == PortalPackageActivityType.Access);
        Assert.Contains(report.Activities, activity => activity.ActivityType == PortalPackageActivityType.Comment);
        Assert.Contains(report.Activities, activity => activity.ActivityType == PortalPackageActivityType.Download);
        Assert.Contains(report.Activities, activity => activity.ActivityType == PortalPackageActivityType.Expiration);
        Assert.Contains(report.Activities, activity => activity.ActivityType == PortalPackageActivityType.Supersede);
        Assert.Contains(report.Activities, activity => activity.ActivityType == PortalPackageActivityType.Revocation);
    }

    [Fact]
    public async Task TC_34_3_5_Lifecycle_actions_are_audit_logged()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out var auditWriter);
        var shared = await service.ShareAsync(CreateRequest(ids), ids.TenantId, ids.ActorUserId);
        await service.ExpireAsync(shared.Id, ids.ActorUserId);
        await service.RevokeAsync(shared.Id, "Revoked.", ids.ActorUserId);
        await service.SupersedeAsync(shared.Id, ids.ReplacementPackageId, ids.ActorUserId);
        await service.ReissueAsync(shared.Id, DateTimeOffset.UtcNow.AddDays(45), ids.ActorUserId);
        await service.ArchiveAsync(shared.Id, ids.ActorUserId);

        var events = auditWriter.Events.Where(auditEvent => auditEvent.EntityType == "SharedPortalPackage").ToArray();
        Assert.Equal(6, events.Length);
        Assert.Contains(events, audit => audit.Summary.Contains("expired", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(events, audit => audit.Summary.Contains("revoked", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(events, audit => audit.Summary.Contains("superseded", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(events, audit => audit.Summary.Contains("reissued", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(events, audit => audit.Summary.Contains("archived", StringComparison.OrdinalIgnoreCase));
    }

    private static PortalPackageLifecycleService CreateService(out CapturingAuditEventWriter auditWriter)
    {
        auditWriter = new CapturingAuditEventWriter();
        return new PortalPackageLifecycleService(new InMemoryPortalPackageLifecycleRepository(), auditWriter);
    }

    private static SharedPortalPackageRequest CreateRequest(StoryIds ids) =>
        new(ids.PackageId, ids.InvitationId, DateTimeOffset.UtcNow.AddDays(30));

    private sealed class CapturingAuditEventWriter : IAuditEventWriter
    {
        public List<CapturedAuditEvent> Events { get; } = [];

        public Task WriteAsync(Guid tenantId, Guid actorUserId, AuditAction action, string entityType, string entityId, string summary, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        {
            Events.Add(new CapturedAuditEvent(tenantId, actorUserId, action, entityType, entityId, summary, metadata?.ToDictionary() ?? []));
            return Task.CompletedTask;
        }
    }

    private sealed record CapturedAuditEvent(Guid TenantId, Guid ActorUserId, AuditAction Action, string EntityType, string EntityId, string Summary, IReadOnlyDictionary<string, string> Metadata);

    private sealed record StoryIds(Guid TenantId, Guid PackageId, Guid ReplacementPackageId, Guid InvitationId, Guid ActorUserId)
    {
        public static StoryIds Create() =>
            new(
                Guid.Parse("34334334-4334-3343-3433-4334334334aa"),
                Guid.Parse("34334334-4334-3343-3433-4334334334bb"),
                Guid.Parse("34334334-4334-3343-3433-4334334334bc"),
                Guid.Parse("34334334-4334-3343-3433-4334334334cc"),
                Guid.Parse("34334334-4334-3343-3433-4334334334dd"));
    }
}
