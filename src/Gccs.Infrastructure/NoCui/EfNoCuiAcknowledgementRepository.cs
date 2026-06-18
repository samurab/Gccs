using Gccs.Application.NoCui;
using Gccs.Application.Security;
using Gccs.Application.Common;
using Gccs.Domain.Evidence;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.NoCui;

public sealed class EfNoCuiAcknowledgementRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : INoCuiAcknowledgementRepository
{
    public async Task<NoCuiAcknowledgementStatusDto?> FindCurrentUserAcknowledgementAsync(
        string noticeVersion,
        CancellationToken cancellationToken = default)
    {
        var acknowledgement = await dbContext.NoCuiAcknowledgements
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.TenantId == tenantContext.TenantId &&
                    candidate.UserId == tenantContext.UserId &&
                    candidate.NoticeVersion == noticeVersion,
                cancellationToken);

        return acknowledgement is null ? null : ToDto(acknowledgement);
    }

    public async Task<NoCuiAcknowledgementStatusDto> AddCurrentUserAcknowledgementAsync(
        string noticeVersion,
        string noticeCopy,
        Guid actorUserId,
        DateTimeOffset acknowledgedAt,
        CancellationToken cancellationToken = default)
    {
        var acknowledgement = new NoCuiAcknowledgementEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            UserId = tenantContext.UserId,
            NoticeVersion = noticeVersion,
            NoticeCopy = noticeCopy,
            AcknowledgedAt = acknowledgedAt,
            CreatedAt = acknowledgedAt,
            CreatedByUserId = actorUserId
        };

        dbContext.NoCuiAcknowledgements.Add(acknowledgement);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(acknowledgement);
    }

    public async Task<EvidenceFileVersionDto> RecordAcceptedEvidenceUploadIntentAsync(
        EvidenceUploadIntentDto uploadIntent,
        CancellationToken cancellationToken = default)
    {
        var evidenceItem = await dbContext.EvidenceItems.SingleOrDefaultAsync(
            candidate =>
                candidate.Id == uploadIntent.EvidenceItemId &&
                candidate.TenantId == tenantContext.TenantId,
            cancellationToken);
        var now = DateTimeOffset.UtcNow;

        if (evidenceItem is null)
        {
            evidenceItem = new EvidenceItemEntity
            {
                Id = uploadIntent.EvidenceItemId,
                TenantId = tenantContext.TenantId,
                Name = uploadIntent.FileName,
                Description = "Upload intent metadata captured by No-CUI guardrails. File storage is not enabled for CUI in the MVP.",
                Type = EvidenceType.Other,
                Status = EvidenceStatus.InReview,
                OwnerFunction = "Compliance",
                TagsJson = "[\"no-cui\",\"upload-intent\"]",
                Classification = uploadIntent.Classification.Classification,
                ClassificationSource = uploadIntent.Classification.Source,
                ClassificationConfidence = uploadIntent.Classification.Confidence,
                ClassificationReviewedByUserId = uploadIntent.Classification.ReviewedByUserId,
                ClassificationReviewedAt = uploadIntent.Classification.ReviewedAt,
                ClassificationReason = uploadIntent.Classification.Reason,
                ClassificationIsApprovedDemoContent = uploadIntent.Classification.IsApprovedDemoContent,
                CreatedAt = now,
                CreatedByUserId = uploadIntent.CreatedByUserId
            };

            dbContext.EvidenceItems.Add(evidenceItem);
        }
        else
        {
            evidenceItem.UpdatedAt = now;
            evidenceItem.UpdatedByUserId = uploadIntent.CreatedByUserId;
        }

        evidenceItem.OriginalFileName = uploadIntent.FileName;
        evidenceItem.ContentType = uploadIntent.ContentType;
        evidenceItem.SizeBytes = uploadIntent.SizeBytes;
        evidenceItem.UploadValidationStatus = uploadIntent.ValidationStatus;
        evidenceItem.MalwareScanStatus = uploadIntent.MalwareScanStatus;
        evidenceItem.StorageUri = null;
        evidenceItem.FileHash = null;
        evidenceItem.Classification = uploadIntent.Classification.Classification;
        evidenceItem.ClassificationSource = uploadIntent.Classification.Source;
        evidenceItem.ClassificationConfidence = uploadIntent.Classification.Confidence;
        evidenceItem.ClassificationReviewedByUserId = uploadIntent.Classification.ReviewedByUserId;
        evidenceItem.ClassificationReviewedAt = uploadIntent.Classification.ReviewedAt;
        evidenceItem.ClassificationReason = uploadIntent.Classification.Reason;
        evidenceItem.ClassificationIsApprovedDemoContent = uploadIntent.Classification.IsApprovedDemoContent;

        var nextVersionNumber = await dbContext.EvidenceFileVersions
            .Where(version => version.EvidenceItemId == evidenceItem.Id)
            .Select(version => (int?)version.VersionNumber)
            .MaxAsync(cancellationToken) ?? 0;
        var version = new EvidenceFileVersionEntity
        {
            Id = uploadIntent.Id,
            EvidenceItemId = evidenceItem.Id,
            VersionNumber = nextVersionNumber + 1,
            FileName = uploadIntent.FileName,
            ContentType = uploadIntent.ContentType,
            SizeBytes = uploadIntent.SizeBytes,
            ValidationStatus = uploadIntent.ValidationStatus,
            MalwareScanStatus = uploadIntent.MalwareScanStatus,
            StorageUri = null,
            FileHash = null,
            UploadedAt = now,
            UploadedByUserId = uploadIntent.CreatedByUserId,
            Classification = uploadIntent.Classification.Classification,
            ClassificationSource = uploadIntent.Classification.Source,
            ClassificationConfidence = uploadIntent.Classification.Confidence,
            ClassificationReviewedByUserId = uploadIntent.Classification.ReviewedByUserId,
            ClassificationReviewedAt = uploadIntent.Classification.ReviewedAt,
            ClassificationReason = uploadIntent.Classification.Reason,
            ClassificationIsApprovedDemoContent = uploadIntent.Classification.IsApprovedDemoContent
        };
        dbContext.EvidenceFileVersions.Add(version);

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(version);
    }

    public async Task<EvidenceFileVersionDto?> FindLatestCurrentTenantFileVersionAsync(
        Guid evidenceItemId,
        CancellationToken cancellationToken = default)
    {
        var version = await QueryCurrentTenantVersions(evidenceItemId)
            .AsNoTracking()
            .Where(candidate => candidate.DeletedAt == null)
            .OrderByDescending(candidate => candidate.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        return version is null ? null : ToDto(version);
    }

    public async Task<EvidenceFileVersionDto?> MarkLatestCurrentTenantFileVersionDeletedAsync(
        Guid evidenceItemId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var version = await QueryCurrentTenantVersions(evidenceItemId)
            .Where(candidate => candidate.DeletedAt == null)
            .OrderByDescending(candidate => candidate.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (version is null)
        {
            return null;
        }

        version.DeletedAt = DateTimeOffset.UtcNow;
        version.DeletedByUserId = actorUserId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(version);
    }

    private IQueryable<EvidenceFileVersionEntity> QueryCurrentTenantVersions(Guid evidenceItemId) =>
        dbContext.EvidenceFileVersions
            .Include(version => version.EvidenceItem)
            .Where(version =>
                version.EvidenceItemId == evidenceItemId &&
                version.EvidenceItem != null &&
                version.EvidenceItem.TenantId == tenantContext.TenantId);

    private static NoCuiAcknowledgementStatusDto ToDto(NoCuiAcknowledgementEntity acknowledgement) =>
        new(
            true,
            acknowledgement.NoticeVersion,
            acknowledgement.NoticeCopy,
            acknowledgement.TenantId,
            acknowledgement.UserId,
            acknowledgement.AcknowledgedAt);

    private static EvidenceFileVersionDto ToDto(EvidenceFileVersionEntity version) =>
        new(
            version.Id,
            version.EvidenceItemId,
            version.VersionNumber,
            version.FileName,
            version.ContentType,
            version.SizeBytes,
            version.ValidationStatus,
            version.MalwareScanStatus,
            IsUsable(version.ValidationStatus, version.MalwareScanStatus),
            new ContentClassificationDto(
                version.Classification,
                version.ClassificationSource,
                version.ClassificationConfidence,
                version.ClassificationReviewedByUserId,
                version.ClassificationReviewedAt,
                version.ClassificationReason,
                version.ClassificationIsApprovedDemoContent),
            version.UploadedAt,
            version.DeletedAt);

    private static bool IsUsable(string validationStatus, string malwareScanStatus) =>
        string.Equals(validationStatus, EvidenceUploadGuardrails.AcceptedValidationStatus, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(malwareScanStatus, "clean", StringComparison.OrdinalIgnoreCase);
}
