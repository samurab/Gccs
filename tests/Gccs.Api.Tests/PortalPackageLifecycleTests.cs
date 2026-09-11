using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Portals;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Infrastructure.Portals;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class PortalPackageLifecycleTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task TC_34_3_1_Shared_packages_enforce_allowed_lifecycle_states()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out _, out _);
        var shared = await service.ShareAsync(CreateRequest(ids), ids.TenantId, ids.ActorUserId);

        var expired = await service.ExpireAsync(shared.Id, ids.TenantId, ids.ActorUserId);

        Assert.Equal(SharedPortalPackageState.Expired, expired?.State);
        await Assert.ThrowsAsync<PortalPackageLifecycleException>(() =>
            service.RevokeAsync(shared.Id, ids.TenantId, "Too late.", ids.ActorUserId));
        var archived = await service.ArchiveAsync(shared.Id, ids.TenantId, ids.ActorUserId);
        Assert.Equal(SharedPortalPackageState.Archived, archived?.State);
    }

    [Fact]
    public async Task TC_34_3_2_Revocation_and_expiration_cut_off_portal_access_immediately()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out _, out _);
        var shared = await service.ShareAsync(CreateRequest(ids), ids.TenantId, ids.ActorUserId);

        Assert.True(await service.CanAccessAsync(shared.Id, ids.TenantId, ids.InvitationId, ids.PackageId, Now));
        Assert.False(await service.CanAccessAsync(shared.Id, ids.OtherTenantId, ids.InvitationId, ids.PackageId, Now));
        Assert.False(await service.CanAccessAsync(shared.Id, ids.TenantId, ids.OtherInvitationId, ids.PackageId, Now));
        Assert.False(await service.CanAccessAsync(shared.Id, ids.TenantId, ids.InvitationId, ids.OtherPackageId, Now));

        await service.RevokeAsync(shared.Id, ids.TenantId, "Customer requested immediate cutoff.", ids.ActorUserId);

        Assert.False(await service.CanAccessAsync(shared.Id, ids.TenantId, ids.InvitationId, ids.PackageId, Now));
        await Assert.ThrowsAsync<PortalPackageAccessDeniedException>(() =>
            service.RecordActivityAsync(shared.Id, ids.TenantId, ids.InvitationId, ids.PackageId,
                PortalPackageActivityType.Download, ids.PortalUserId));
    }

    [Fact]
    public async Task TC_34_3_3_Reissue_creates_new_version_and_bidirectional_lineage()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out _, out _);
        var original = await service.ShareAsync(CreateRequest(ids), ids.TenantId, ids.ActorUserId);

        var replacement = await service.ReissueAsync(
            original.Id,
            ids.TenantId,
            new ReissueSharedPortalPackageRequest(ids.OtherPackageId, Now.AddDays(60)),
            ids.ActorUserId);
        var old = Assert.Single(await service.ListAsync(ids.TenantId), package => package.Id == original.Id);

        Assert.Equal(original.Version + 1, replacement?.Version);
        Assert.Equal(original.Id, replacement?.SupersedesSharedPackageId);
        Assert.Equal(replacement?.Id, old.ReplacementSharedPackageId);
        Assert.Equal(ids.OtherPackageId, old.ReplacementPackageId);
        Assert.Equal(SharedPortalPackageState.Superseded, old.State);
        Assert.False(await service.CanAccessAsync(original.Id, ids.TenantId, ids.InvitationId, ids.PackageId, Now));
        Assert.True(await service.CanAccessAsync(replacement!.Id, ids.TenantId, ids.InvitationId, ids.OtherPackageId, Now));
        await Assert.ThrowsAsync<PortalPackageLifecycleConflictException>(() =>
            service.ReissueAsync(original.Id, ids.TenantId,
                new ReissueSharedPortalPackageRequest(Guid.NewGuid(), Now.AddDays(90)), ids.ActorUserId));
    }

    [Fact]
    public async Task TC_34_3_4_Activity_report_is_complete_and_tenant_scoped()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out _, out _);
        var shared = await service.ShareAsync(CreateRequest(ids), ids.TenantId, ids.ActorUserId);
        await service.RecordActivityAsync(shared.Id, ids.TenantId, ids.InvitationId, ids.PackageId, PortalPackageActivityType.Access, ids.PortalUserId);
        await service.RecordActivityAsync(shared.Id, ids.TenantId, ids.InvitationId, ids.PackageId, PortalPackageActivityType.Comment, ids.PortalUserId);
        await service.RecordActivityAsync(shared.Id, ids.TenantId, ids.InvitationId, ids.PackageId, PortalPackageActivityType.Download, ids.PortalUserId);
        await service.RevokeAsync(shared.Id, ids.TenantId, "Scope reduced.", ids.ActorUserId);

        var report = await service.GenerateActivityReportAsync(ids.TenantId);
        var otherTenantReport = await service.GenerateActivityReportAsync(ids.OtherTenantId);

        Assert.Equal(
            [PortalPackageActivityType.Access, PortalPackageActivityType.Comment, PortalPackageActivityType.Download, PortalPackageActivityType.Revocation],
            report.Activities.Select(activity => activity.ActivityType).Order());
        Assert.All(report.Activities, activity => Assert.Equal(ids.TenantId, activity.TenantId));
        Assert.Empty(otherTenantReport.Activities);
    }

    [Fact]
    public async Task TC_34_3_5_Maintenance_records_reminder_and_automatic_expiration_with_audit()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out var auditWriter, out _);
        var shared = await service.ShareAsync(
            new SharedPortalPackageRequest(ids.PackageId, ids.InvitationId, Now.AddDays(2), 7),
            ids.TenantId,
            ids.ActorUserId);

        var reminderResult = await service.ProcessDueAsync(Now);
        var expirationResult = await service.ProcessDueAsync(Now.AddDays(3));
        var repeatedResult = await service.ProcessDueAsync(Now.AddDays(3));
        var stored = Assert.Single(await service.ListAsync(ids.TenantId));
        var report = await service.GenerateActivityReportAsync(ids.TenantId);

        Assert.Equal(1, reminderResult.RemindersCreated);
        Assert.Equal(1, expirationResult.PackagesExpired);
        Assert.Equal(0, repeatedResult.PackagesExpired);
        Assert.Equal(SharedPortalPackageState.Expired, stored.State);
        Assert.Contains(report.Activities, activity => activity.ActivityType == PortalPackageActivityType.ExpirationReminder);
        Assert.Contains(report.Activities, activity => activity.ActivityType == PortalPackageActivityType.Expiration);
        Assert.Contains(auditWriter.Events, audit =>
            audit.Action == AuditAction.Expired &&
            audit.Metadata["systemInitiated"] == bool.TrueString);
        Assert.False(await service.CanAccessAsync(shared.Id, ids.TenantId, ids.InvitationId, ids.PackageId, Now.AddDays(3)));
    }

    [Fact]
    public async Task Cross_tenant_mutation_returns_not_found_and_leaves_state_and_audit_unchanged()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out var auditWriter, out _);
        var shared = await service.ShareAsync(CreateRequest(ids), ids.TenantId, ids.ActorUserId);
        var auditCount = auditWriter.Events.Count;

        var result = await service.RevokeAsync(
            shared.Id, ids.OtherTenantId, "Cross-tenant attempt.", ids.ActorUserId);

        Assert.Null(result);
        Assert.Equal(auditCount, auditWriter.Events.Count);
        Assert.True(await service.CanAccessAsync(shared.Id, ids.TenantId, ids.InvitationId, ids.PackageId, Now));
    }

    [Fact]
    public async Task Share_eligibility_rejects_CUI_and_cross_tenant_source_packages()
    {
        var ids = StoryIds.Create();
        var invitations = new InMemoryExternalPortalAccessRepository();
        var invitation = Assert.IsType<ExternalPortalInvitationDto>(
            await invitations.FindInvitationAsync((await invitations.CreateInvitationAsync(
                new ExternalPortalInvitationRequest(
                    "reviewer@example.test", ExternalPortalRole.Auditor, [ids.PackageId],
                    [], Now.AddDays(30), true, true),
                ids.TenantId,
                ids.ActorUserId)).Id));
        var packages = new InMemoryPortalPackageRepository();
        packages.SeedPackages(new PortalPackageDto(
            ids.PackageId, ids.TenantId, null, "CUI package", 1, PortalPackageStatus.Approved,
            ContentClassification.Cui, false, [], Now));
        var validator = new PortalPackageShareEligibilityValidator(invitations, packages);

        await Assert.ThrowsAsync<PortalPackageLifecycleException>(() =>
            validator.ValidateAsync(ids.PackageId, invitation.Id, ids.TenantId, Now));
        await Assert.ThrowsAsync<PortalPackageLifecycleException>(() =>
            validator.ValidateAsync(ids.PackageId, invitation.Id, ids.OtherTenantId, Now));
    }

    [Fact]
    public async Task TC_34_3_5_All_lifecycle_actions_are_audited_and_reported()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out var auditWriter, out _);
        async Task<SharedPortalPackageDto> Share(Guid packageId) => await service.ShareAsync(
            new SharedPortalPackageRequest(packageId, ids.InvitationId, Now.AddDays(30)),
            ids.TenantId,
            ids.ActorUserId);

        var superseded = await Share(Guid.NewGuid());
        var replacement = await Share(Guid.NewGuid());
        await service.SupersedeAsync(superseded.Id, ids.TenantId, replacement.Id, ids.ActorUserId);
        await service.ExpireAsync((await Share(Guid.NewGuid())).Id, ids.TenantId, ids.ActorUserId);
        await service.RevokeAsync((await Share(Guid.NewGuid())).Id, ids.TenantId, "Scope reduced.", ids.ActorUserId);
        await service.ReissueAsync((await Share(Guid.NewGuid())).Id, ids.TenantId,
            new ReissueSharedPortalPackageRequest(Guid.NewGuid(), Now.AddDays(60)), ids.ActorUserId);
        await service.ArchiveAsync((await Share(Guid.NewGuid())).Id, ids.TenantId, ids.ActorUserId);

        var reportTypes = (await service.GenerateActivityReportAsync(ids.TenantId)).Activities
            .Select(activity => activity.ActivityType).ToHashSet();
        Assert.Subset(new HashSet<PortalPackageActivityType>
        {
            PortalPackageActivityType.Expiration,
            PortalPackageActivityType.Supersede,
            PortalPackageActivityType.Revocation,
            PortalPackageActivityType.Reissue,
            PortalPackageActivityType.Archive
        }, reportTypes);
        Assert.Contains(auditWriter.Events, audit => audit.Action == AuditAction.Expired);
        Assert.Contains(auditWriter.Events, audit => audit.Action == AuditAction.PermissionChanged);
        Assert.Contains(auditWriter.Events, audit => audit.Action == AuditAction.Updated);
        Assert.Contains(auditWriter.Events, audit => audit.Action == AuditAction.Archived);
        Assert.All(auditWriter.Events, audit => Assert.Equal(ids.TenantId, audit.TenantId));
    }

    private static PortalPackageLifecycleService CreateService(
        out CapturingAuditEventWriter auditWriter,
        out InMemoryPortalPackageLifecycleRepository repository)
    {
        auditWriter = new CapturingAuditEventWriter();
        repository = new InMemoryPortalPackageLifecycleRepository();
        return new PortalPackageLifecycleService(
            repository,
            new AllowAllEligibilityValidator(),
            auditWriter,
            new PassThroughTransaction(),
            new FixedTimeProvider(Now));
    }

    private static SharedPortalPackageRequest CreateRequest(StoryIds ids) =>
        new(ids.PackageId, ids.InvitationId, Now.AddDays(30));

    private sealed class PassThroughTransaction : IApplicationTransaction
    {
        public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
            operation(cancellationToken);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class AllowAllEligibilityValidator : IPortalPackageShareEligibilityValidator
    {
        public Task ValidateAsync(Guid packageId, Guid invitationId, Guid tenantId, DateTimeOffset asOf, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

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
            Events.Add(new(tenantId, actorUserId, action, entityType, entityId, summary, metadata?.ToDictionary() ?? []));
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
        Guid OtherTenantId,
        Guid PackageId,
        Guid OtherPackageId,
        Guid InvitationId,
        Guid OtherInvitationId,
        Guid ActorUserId,
        Guid PortalUserId)
    {
        public static StoryIds Create() => new(
            Guid.Parse("34334334-4334-3343-3433-4334334334aa"),
            Guid.Parse("34334334-4334-3343-3433-4334334334ab"),
            Guid.Parse("34334334-4334-3343-3433-4334334334bb"),
            Guid.Parse("34334334-4334-3343-3433-4334334334bc"),
            Guid.Parse("34334334-4334-3343-3433-4334334334cc"),
            Guid.Parse("34334334-4334-3343-3433-4334334334cd"),
            Guid.Parse("34334334-4334-3343-3433-4334334334dd"),
            Guid.Parse("34334334-4334-3343-3433-4334334334de"));
    }
}
