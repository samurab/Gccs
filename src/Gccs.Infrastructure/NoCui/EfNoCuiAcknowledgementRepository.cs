using Gccs.Application.NoCui;
using Gccs.Application.Security;
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

    public async Task RecordAcceptedEvidenceUploadIntentAsync(
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
                TagsJson = "[\"no-cui\",\"upload-intent\"]",
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

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static NoCuiAcknowledgementStatusDto ToDto(NoCuiAcknowledgementEntity acknowledgement) =>
        new(
            true,
            acknowledgement.NoticeVersion,
            acknowledgement.NoticeCopy,
            acknowledgement.TenantId,
            acknowledgement.UserId,
            acknowledgement.AcknowledgedAt);
}
