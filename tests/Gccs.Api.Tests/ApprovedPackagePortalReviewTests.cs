using Gccs.Application.Audit;
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

        var packages = await harness.ReviewService.ListPackagesAsync(harness.Invitation.Id, DateTimeOffset.UtcNow, ids.ActorUserId);

        var package = Assert.Single(packages);
        Assert.Equal(ids.ApprovedPackageId, package.Id);
    }

    [Fact]
    public async Task TC_34_2_2_Drafts_internal_unsafe_and_unrelated_records_are_hidden()
    {
        var ids = StoryIds.Create();
        var harness = await CreateHarnessAsync(ids);

        var packages = await harness.ReviewService.ListPackagesAsync(harness.Invitation.Id, DateTimeOffset.UtcNow, ids.ActorUserId);

        Assert.DoesNotContain(packages, package => package.Id == ids.DraftPackageId);
        Assert.DoesNotContain(packages, package => package.Id == ids.InternalPackageId);
        Assert.DoesNotContain(packages, package => package.Id == ids.ProhibitedPackageId);
        Assert.DoesNotContain(packages, package => package.Id == ids.UnknownPackageId);
        Assert.DoesNotContain(packages, package => package.Id == ids.UnrelatedPackageId);
    }

    [Fact]
    public async Task TC_34_2_3_Comments_and_questions_do_not_modify_source_package_records()
    {
        var ids = StoryIds.Create();
        var harness = await CreateHarnessAsync(ids);
        var before = await harness.PackageRepository.FindPackageAsync(ids.ApprovedPackageId);

        var comment = await harness.ReviewService.AddCommentAsync(new PortalPackageCommentRequest(ids.ApprovedPackageId, PortalCommentKind.Comment, "Looks good."), ids.TenantId, ids.ActorUserId);
        var question = await harness.ReviewService.AddQuestionAsync(new PortalPackageCommentRequest(ids.ApprovedPackageId, PortalCommentKind.Comment, "Where is evidence?"), ids.TenantId, ids.ActorUserId);
        var after = await harness.PackageRepository.FindPackageAsync(ids.ApprovedPackageId);

        Assert.Equal(ids.ApprovedPackageId, comment.PackageId);
        Assert.Equal(PortalCommentKind.Question, question.Kind);
        Assert.Equal(before, after);
        Assert.Equal(2, harness.PackageRepository.Comments.Count);
    }

    [Fact]
    public async Task TC_34_2_4_Download_includes_metadata_and_watermark_when_configured()
    {
        var ids = StoryIds.Create();
        var harness = await CreateHarnessAsync(ids);

        var download = await harness.ReviewService.DownloadAsync(ids.ApprovedPackageId, watermark: true, ids.TenantId, ids.ActorUserId);

        Assert.Equal(ids.ApprovedPackageId, download.PackageId);
        Assert.Equal("Prime evidence package", download.Title);
        Assert.Equal(3, download.Version);
        Assert.Contains(ids.TenantId.ToString(), download.Watermark, StringComparison.Ordinal);
        Assert.NotEqual(default, download.GeneratedAt);
    }

    [Fact]
    public async Task TC_34_2_5_View_comment_question_and_download_are_audit_logged()
    {
        var ids = StoryIds.Create();
        var harness = await CreateHarnessAsync(ids);
        await harness.ReviewService.ListPackagesAsync(harness.Invitation.Id, DateTimeOffset.UtcNow, ids.ActorUserId);
        await harness.ReviewService.AddCommentAsync(new PortalPackageCommentRequest(ids.ApprovedPackageId, PortalCommentKind.Comment, "Comment."), ids.TenantId, ids.ActorUserId);
        await harness.ReviewService.AddQuestionAsync(new PortalPackageCommentRequest(ids.ApprovedPackageId, PortalCommentKind.Comment, "Question?"), ids.TenantId, ids.ActorUserId);
        await harness.ReviewService.DownloadAsync(ids.ApprovedPackageId, watermark: true, ids.TenantId, ids.ActorUserId);

        Assert.Contains(harness.AuditWriter.Events, audit => audit.Summary.Contains("access was granted", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(harness.AuditWriter.Events, audit => audit.EntityType == "PortalPackageComment" && audit.Action == AuditAction.Created);
        Assert.Contains(harness.AuditWriter.Events, audit => audit.EntityType == "PortalPackage" && audit.Action == AuditAction.Downloaded);
    }

    private static async Task<StoryHarness> CreateHarnessAsync(StoryIds ids)
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
        return new StoryHarness(new ApprovedPackagePortalReviewService(accessService, packageRepository, auditWriter), packageRepository, invitation, auditWriter);
    }

    private static PortalPackageDto Package(Guid id, Guid tenantId, Guid contractId, PortalPackageStatus status, ContentClassification classification, bool internalNotes) =>
        new(id, tenantId, contractId, "Prime evidence package", 3, status, classification, internalNotes, [Guid.NewGuid()], DateTimeOffset.UtcNow);

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

    private sealed record StoryHarness(ApprovedPackagePortalReviewService ReviewService, InMemoryPortalPackageRepository PackageRepository, ExternalPortalInvitationDto Invitation, CapturingAuditEventWriter AuditWriter);

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
