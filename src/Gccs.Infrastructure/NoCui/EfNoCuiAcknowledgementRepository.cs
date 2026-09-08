using Gccs.Application.NoCui;
using Gccs.Application.Security;
using Gccs.Application.Common;
using Gccs.Domain.Common;
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

    public Task<bool> CurrentTenantEvidenceItemExistsAsync(
        Guid evidenceItemId,
        CancellationToken cancellationToken = default) =>
        dbContext.EvidenceItems.AnyAsync(
            candidate => candidate.Id == evidenceItemId && candidate.TenantId == tenantContext.TenantId,
            cancellationToken);

    public async Task<EvidenceFileVersionDto> RecordAcceptedEvidenceUploadIntentAsync(
        EvidenceUploadIntentDto uploadIntent,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = dbContext.Database.IsNpgsql() && dbContext.Database.CurrentTransaction is null
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var evidenceItem = !dbContext.Database.IsNpgsql()
            ? await FindCurrentTenantEvidenceItemAsync(uploadIntent.EvidenceItemId, cancellationToken)
            : await dbContext.EvidenceItems
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM gccs.evidence_items
                    WHERE id = {uploadIntent.EvidenceItemId}
                      AND tenant_id = {tenantContext.TenantId}
                    FOR UPDATE
                    """)
                .SingleOrDefaultAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        if (evidenceItem is null)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw new EvidenceItemNotFoundException(uploadIntent.EvidenceItemId);
        }

        evidenceItem.UpdatedAt = now;
        evidenceItem.UpdatedByUserId = uploadIntent.CreatedByUserId;

        evidenceItem.OriginalFileName = uploadIntent.FileName;
        evidenceItem.ContentType = uploadIntent.ContentType;
        evidenceItem.SizeBytes = uploadIntent.SizeBytes;
        evidenceItem.UploadValidationStatus = uploadIntent.ValidationStatus;
        evidenceItem.MalwareScanStatus = uploadIntent.MalwareScanStatus;
        evidenceItem.StorageUri = uploadIntent.StorageObjectName;
        evidenceItem.FileHash = null;
        // A replacement cannot erase a review, downgrade its parent, or release quarantined content.
        ContentClassificationPolicy.EnsureProcessable(evidenceItem.Classification, "Evidence replacement");
        if (evidenceItem.Classification == ContentClassification.SyntheticCui)
            throw new ContentClassificationValidationException("Imported synthetic seed content cannot be replaced through customer uploads.");
        var previous = Gccs.Infrastructure.Common.ClassificationMetadata.Read(evidenceItem);
        var incoming = uploadIntent.Classification;
        var promote = incoming.Classification == ContentClassification.Unknown ||
            (incoming.Classification == ContentClassification.Cui && evidenceItem.Classification != ContentClassification.Cui) ||
            (incoming.Classification == ContentClassification.Fci && evidenceItem.Classification == ContentClassification.Unclassified);
        if (promote)
        {
            evidenceItem.Classification = incoming.Classification;
            evidenceItem.ClassificationSource = ContentClassificationSource.SystemSuggested;
            evidenceItem.ClassificationConfidence = null;
            evidenceItem.ClassificationReviewedByUserId = null;
            evidenceItem.ClassificationReviewedAt = null;
            evidenceItem.ClassificationReason = "Handling classification raised from an accepted file version; reviewer confirmation remains separate.";
            evidenceItem.ClassificationIsApprovedDemoContent = false;
            evidenceItem.ClassificationRevision++;
            dbContext.ContentClassificationHistory.Add(Gccs.Infrastructure.Common.ClassificationMetadata.History(
                evidenceItem, tenantContext.TenantId, "EvidenceItem", uploadIntent.CreatedByUserId, now, previous));
        }

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
            StorageUri = uploadIntent.StorageObjectName,
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
        dbContext.ContentClassificationHistory.Add(Gccs.Infrastructure.Common.ClassificationMetadata.History(
            version, tenantContext.TenantId, "EvidenceFileVersion", uploadIntent.CreatedByUserId, now));

        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return ToDto(version);
    }

    public async Task<ContentClassificationDto?> FindCurrentTenantEvidenceClassificationAsync(
        Guid evidenceItemId, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.EvidenceItems.AsNoTracking().SingleOrDefaultAsync(
            e => e.Id == evidenceItemId && e.TenantId == tenantContext.TenantId, cancellationToken);
        return item is null ? null : new ContentClassificationDto(item.Classification, item.ClassificationSource,
            item.ClassificationConfidence, item.ClassificationReviewedByUserId, item.ClassificationReviewedAt,
            item.ClassificationReason, item.ClassificationIsApprovedDemoContent);
    }

    private Task<EvidenceItemEntity?> FindCurrentTenantEvidenceItemAsync(
        Guid evidenceItemId,
        CancellationToken cancellationToken) =>
        dbContext.EvidenceItems.SingleOrDefaultAsync(
            candidate => candidate.Id == evidenceItemId && candidate.TenantId == tenantContext.TenantId,
            cancellationToken);

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
        // Serialize deletion with replacements until the service commits its audit event.
        if (dbContext.Database.IsNpgsql())
        {
            await dbContext.EvidenceItems.FromSqlInterpolated($"""
                SELECT * FROM gccs.evidence_items
                WHERE id = {evidenceItemId} AND tenant_id = {tenantContext.TenantId}
                FOR UPDATE
                """).ToListAsync(cancellationToken);
        }
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
            version.StorageUri,
            version.UploadedAt,
            version.DeletedAt);

    private static bool IsUsable(string validationStatus, string malwareScanStatus) =>
        string.Equals(validationStatus, EvidenceUploadGuardrails.AcceptedValidationStatus, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(malwareScanStatus, "clean", StringComparison.OrdinalIgnoreCase);
}
