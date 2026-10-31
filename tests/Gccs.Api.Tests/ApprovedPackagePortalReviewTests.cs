using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Portals;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Infrastructure.Portals;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ApprovedPackagePortalReviewTests
{
    [Fact]
    public async Task TC_34_2_1_Reviewer_sees_only_approved_explicitly_assigned_packages()
    {
        var ids = StoryIds.Create();
        var harness = await CreateHarnessAsync(ids);

        var packages = await harness.ReviewService.ListPackagesAsync(harness.Invitation.Id, Identity(ids));

        var package = Assert.Single(packages);
        Assert.Equal(ids.ApprovedPackageId, package.PackageId);
    }

    [Fact]
    public async Task TC_34_2_2_Drafts_internal_unsafe_and_unrelated_records_are_hidden()
    {
        var ids = StoryIds.Create();
        var harness = await CreateHarnessAsync(ids);

        var packages = await harness.ReviewService.ListPackagesAsync(harness.Invitation.Id, Identity(ids));

        Assert.DoesNotContain(packages, package => package.PackageId == ids.DraftPackageId);
        Assert.DoesNotContain(packages, package => package.PackageId == ids.InternalPackageId);
        Assert.DoesNotContain(packages, package => package.PackageId == ids.ProhibitedPackageId);
        Assert.DoesNotContain(packages, package => package.PackageId == ids.UnknownPackageId);
        Assert.DoesNotContain(packages, package => package.PackageId == ids.UnrelatedPackageId);
    }

    [Fact]
    public async Task TC_34_2_3_Comments_and_questions_do_not_modify_source_package_records()
    {
        var ids = StoryIds.Create();
        var harness = await CreateHarnessAsync(ids);
        var before = await harness.PackageRepository.FindPackageAsync(ids.TenantId, ids.ApprovedPackageId);

        var comment = await harness.ReviewService.AddMessageAsync(harness.Invitation.Id, harness.ApprovedShareId, new PortalPackageReviewMessageRequest(PortalCommentKind.Comment, "Looks good."), Identity(ids));
        var question = await harness.ReviewService.AddMessageAsync(harness.Invitation.Id, harness.ApprovedShareId, new PortalPackageReviewMessageRequest(PortalCommentKind.Question, "Where is evidence?"), Identity(ids));
        var after = await harness.PackageRepository.FindPackageAsync(ids.TenantId, ids.ApprovedPackageId);

        Assert.Equal(PortalCommentKind.Comment, comment.Kind);
        Assert.Equal(PortalCommentKind.Question, question.Kind);
        Assert.Equal(before, after);
        Assert.Equal(2, harness.PackageRepository.Messages.Count);
    }

    [Fact]
    public async Task Reviewer_message_with_prohibited_data_marking_is_rejected_without_mutation_or_audit()
    {
        var ids = StoryIds.Create();
        var harness = await CreateHarnessAsync(ids);
        var auditCount = harness.AuditWriter.Events.Count;

        await Assert.ThrowsAsync<PortalPackageValidationException>(() =>
            harness.ReviewService.AddMessageAsync(harness.Invitation.Id, harness.ApprovedShareId,
                new PortalPackageReviewMessageRequest(PortalCommentKind.Comment, "CUI//SP-PROPIN"),
                Identity(ids)));

        Assert.Empty(harness.PackageRepository.Messages);
        Assert.Equal(auditCount, harness.AuditWriter.Events.Count);
    }

    [Fact]
    public async Task TC_34_2_4_Download_includes_metadata_and_watermark_when_configured()
    {
        var ids = StoryIds.Create();
        var harness = await CreateHarnessAsync(ids);

        var download = await harness.ReviewService.DownloadAsync(harness.Invitation.Id, harness.ApprovedShareId, Identity(ids));

        Assert.Equal(ids.ApprovedPackageId, download.Metadata.PackageId);
        Assert.Equal("Prime evidence package", download.Metadata.Title);
        Assert.Equal(3, download.Metadata.Version);
        Assert.NotNull(download.Metadata.Watermark);
        Assert.Contains("data-portal-watermark", System.Text.Encoding.UTF8.GetString(download.Content));
        Assert.NotEqual(default, download.Metadata.GeneratedAt);
    }

    [Fact]
    public async Task Download_keeps_metadata_and_omits_watermark_when_disabled_by_server_policy()
    {
        var ids = StoryIds.Create();
        var harness = await CreateHarnessAsync(ids, watermarkDownloads: false);

        var download = await harness.ReviewService.DownloadAsync(
            harness.Invitation.Id, harness.ApprovedShareId, Identity(ids));

        Assert.Null(download.Metadata.Watermark);
        var content = System.Text.Encoding.UTF8.GetString(download.Content);
        Assert.Contains($"package={ids.ApprovedPackageId}", content);
        Assert.DoesNotContain("data-portal-watermark", content);
    }

    [Fact]
    public async Task TC_34_2_5_View_comment_question_and_download_are_audit_logged()
    {
        var ids = StoryIds.Create();
        var harness = await CreateHarnessAsync(ids);
        await harness.ReviewService.ListPackagesAsync(harness.Invitation.Id, Identity(ids));
        await harness.ReviewService.AddMessageAsync(harness.Invitation.Id, harness.ApprovedShareId, new PortalPackageReviewMessageRequest(PortalCommentKind.Comment, "Comment."), Identity(ids));
        await harness.ReviewService.AddMessageAsync(harness.Invitation.Id, harness.ApprovedShareId, new PortalPackageReviewMessageRequest(PortalCommentKind.Question, "Question?"), Identity(ids));
        await harness.ReviewService.DownloadAsync(harness.Invitation.Id, harness.ApprovedShareId, Identity(ids));

        Assert.Contains(harness.AuditWriter.Events, audit => audit.Summary.Contains("access was granted", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(harness.AuditWriter.Events, audit => audit.EntityType == "PortalPackageReviewMessage" && audit.Action == AuditAction.Created);
        Assert.Contains(harness.AuditWriter.Events, audit => audit.EntityType == "PortalPackage" && audit.Action == AuditAction.Downloaded);
    }

    [Fact]
    public async Task TC_34_3_2_Revoked_share_is_removed_from_review_and_download_paths()
    {
        var ids = StoryIds.Create();
        var harness = await CreateHarnessAsync(ids);
        Assert.Single(await harness.ReviewService.ListPackagesAsync(
            harness.Invitation.Id, Identity(ids)));

        await harness.LifecycleService.RevokeAsync(
            harness.ApprovedShareId, ids.TenantId, "Review access withdrawn.", ids.ActorUserId);

        Assert.Empty(await harness.ReviewService.ListPackagesAsync(
            harness.Invitation.Id, Identity(ids)));
        await Assert.ThrowsAsync<PortalPackageAccessDeniedException>(() =>
            harness.ReviewService.DownloadAsync(
                harness.Invitation.Id, harness.ApprovedShareId, Identity(ids)));
    }

    [Fact]
    public async Task Source_version_change_after_approval_fails_closed_until_reissued()
    {
        var ids = StoryIds.Create();
        var harness = await CreateHarnessAsync(ids);
        harness.PackageRepository.ReplacePackage(
            Package(ids.ApprovedPackageId, ids.TenantId, ids.ContractId,
                PortalPackageStatus.Approved, ContentClassification.Fci, internalNotes: false) with { Version = 4 });

        Assert.Empty(await harness.ReviewService.ListPackagesAsync(harness.Invitation.Id, Identity(ids)));
        await Assert.ThrowsAsync<PortalPackageAccessDeniedException>(() =>
            harness.ReviewService.DownloadAsync(harness.Invitation.Id, harness.ApprovedShareId, Identity(ids)));
    }

    private static async Task<StoryHarness> CreateHarnessAsync(StoryIds ids, bool watermarkDownloads = true)
    {
        var auditWriter = new CapturingAuditEventWriter();
        var accessRepository = new InMemoryExternalPortalAccessRepository();
        var accessService = new ExternalPortalAccessService(accessRepository, auditWriter);
        var invitation = await accessService.InviteAsync(
            new ExternalPortalInvitationRequest(
                "reviewer@example.test",
                ExternalPortalRole.Auditor,
                [ids.ApprovedPackageId, ids.DraftPackageId, ids.InternalPackageId, ids.ProhibitedPackageId, ids.UnknownPackageId],
                [ids.ContractId],
                DateTimeOffset.UtcNow.AddDays(30),
                CanDownload: true,
                StrongAuthenticationRequired: true),
            ids.TenantId,
            ids.ActorUserId);
        var packageRepository = new InMemoryPortalPackageRepository();
        packageRepository.SeedInvitation(invitation);
        packageRepository.SeedPackages(
            Package(ids.ApprovedPackageId, ids.TenantId, ids.ContractId, PortalPackageStatus.Approved, ContentClassification.Fci, internalNotes: false),
            Package(ids.DraftPackageId, ids.TenantId, ids.ContractId, PortalPackageStatus.Draft, ContentClassification.Fci, internalNotes: false),
            Package(ids.InternalPackageId, ids.TenantId, ids.ContractId, PortalPackageStatus.Approved, ContentClassification.Fci, internalNotes: true),
            Package(ids.ProhibitedPackageId, ids.TenantId, ids.ContractId, PortalPackageStatus.Approved, ContentClassification.Prohibited, internalNotes: false),
            Package(ids.UnknownPackageId, ids.TenantId, ids.ContractId, PortalPackageStatus.Approved, ContentClassification.Unknown, internalNotes: false),
            Package(ids.UnrelatedPackageId, ids.TenantId, ids.OtherContractId, PortalPackageStatus.Approved, ContentClassification.Fci, internalNotes: false));
        var lifecycleService = new PortalPackageLifecycleService(
            new InMemoryPortalPackageLifecycleRepository(),
            new PortalPackageShareEligibilityValidator(accessRepository, packageRepository),
            auditWriter,
            new PassThroughTransaction(),
            TimeProvider.System);
        var shares = new Dictionary<Guid, Guid>();
        foreach (var packageId in new[] { ids.ApprovedPackageId })
        {
            var share = await lifecycleService.ShareAsync(
                new SharedPortalPackageRequest(packageId, invitation.Id, DateTimeOffset.UtcNow.AddDays(30)),
                ids.TenantId,
                ids.ActorUserId);
            shares[packageId] = share.Id;
        }
        return new StoryHarness(
            new ApprovedPackagePortalReviewService(accessService, packageRepository, lifecycleService, auditWriter,
                new PassThroughTransaction(), TimeProvider.System, new PortalReviewDownloadPolicy(watermarkDownloads)),
            packageRepository,
            invitation,
            shares[ids.ApprovedPackageId],
            lifecycleService,
            auditWriter);
    }

    private static PortalPackageDto Package(Guid id, Guid tenantId, Guid contractId, PortalPackageStatus status, ContentClassification classification, bool internalNotes) =>
        new(id, tenantId, contractId, "Prime evidence package", 3, status, classification, internalNotes, [Guid.NewGuid()], DateTimeOffset.UtcNow);

    private static PortalReviewerIdentity Identity(StoryIds ids) =>
        new(ids.ActorUserId, "reviewer@example.test", true);

    private sealed class CapturingAuditEventWriter : IAuditEventWriter
    {
        public List<CapturedAuditEvent> Events { get; } = [];

        public Task WriteAsync(Guid tenantId, Guid actorUserId, AuditAction action, string entityType, string entityId, string summary, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        {
            Events.Add(new CapturedAuditEvent(tenantId, actorUserId, action, entityType, entityId, summary, metadata?.ToDictionary() ?? []));
            return Task.CompletedTask;
        }
    }

    private sealed class PassThroughTransaction : IApplicationTransaction
    {
        public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
            operation(cancellationToken);
    }

    private sealed record CapturedAuditEvent(Guid TenantId, Guid ActorUserId, AuditAction Action, string EntityType, string EntityId, string Summary, IReadOnlyDictionary<string, string> Metadata);

    private sealed record StoryHarness(ApprovedPackagePortalReviewService ReviewService, InMemoryPortalPackageRepository PackageRepository, ExternalPortalInvitationDto Invitation, Guid ApprovedShareId, PortalPackageLifecycleService LifecycleService, CapturingAuditEventWriter AuditWriter);

    private sealed record StoryIds(Guid TenantId, Guid ContractId, Guid OtherContractId, Guid ApprovedPackageId, Guid DraftPackageId, Guid InternalPackageId, Guid ProhibitedPackageId, Guid UnknownPackageId, Guid UnrelatedPackageId, Guid ActorUserId)
    {
        public static StoryIds Create() =>
            new(
                Guid.Parse("34234234-4234-2342-3423-4234234234aa"),
                Guid.Parse("34234234-4234-2342-3423-4234234234bb"),
                Guid.Parse("34234234-4234-2342-3423-4234234234bc"),
                Guid.Parse("34234234-4234-2342-3423-4234234234cc"),
                Guid.Parse("34234234-4234-2342-3423-4234234234cd"),
                Guid.Parse("34234234-4234-2342-3423-4234234234ce"),
                Guid.Parse("34234234-4234-2342-3423-4234234234cf"),
                Guid.Parse("34234234-4234-2342-3423-4234234234da"),
                Guid.Parse("34234234-4234-2342-3423-4234234234db"),
                Guid.Parse("34234234-4234-2342-3423-4234234234dd"));
    }
}
