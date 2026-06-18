using Gccs.Application.Common;
using Gccs.Application.Evidence;
using Gccs.Application.Security;
using Gccs.Domain.Common;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Common;

public sealed class EfContentClassificationReviewRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : IContentClassificationReviewRepository
{
    public async Task<IReadOnlyList<ContentClassificationReviewItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var evidenceItems = await dbContext.EvidenceItems
            .AsNoTracking()
            .Where(item =>
                item.TenantId == tenantContext.TenantId &&
                (item.Classification == ContentClassification.Unknown || item.Classification == ContentClassification.Prohibited))
            .OrderBy(item => item.CreatedAt)
            .ToArrayAsync(cancellationToken);
        var evidence = evidenceItems.Select(item => new ContentClassificationReviewItemDto(
                item.TenantId,
                "EvidenceItem",
                item.Id.ToString(),
                item.Name,
                ToClassificationDto(item),
                item.CreatedAt,
                item.Classification == ContentClassification.Prohibited ? "escalation" : "review"))
            .ToArray();

        var documentItems = await dbContext.Set<ContractDocumentEntity>()
            .AsNoTracking()
            .Include(item => item.Contract)
            .Where(item =>
                item.Contract != null &&
                item.Contract.TenantId == tenantContext.TenantId &&
                (item.Classification == ContentClassification.Unknown || item.Classification == ContentClassification.Prohibited))
            .OrderBy(item => item.UploadedAt)
            .ToArrayAsync(cancellationToken);
        var documents = documentItems.Select(item => new ContentClassificationReviewItemDto(
                tenantContext.TenantId,
                "ContractDocument",
                item.Id.ToString(),
                item.FileName,
                ToClassificationDto(item),
                item.UploadedAt,
                item.Classification == ContentClassification.Prohibited ? "escalation" : "review"))
            .ToArray();

        return evidence.Concat(documents)
            .OrderBy(item => item.CreatedAt)
            .ToArray();
    }

    public async Task<EvidenceMetadataDto?> ReclassifyEvidenceAsync(
        Guid evidenceItemId,
        ReclassifyContentRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.EvidenceItems
            .Include(item => item.Obligations)
            .Include(item => item.Controls)
            .Include(item => item.Contracts)
            .Include(item => item.Vendors)
            .Include(item => item.Employees)
            .SingleOrDefaultAsync(item => item.Id == evidenceItemId && item.TenantId == tenantContext.TenantId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var previous = entity.Classification;
        var now = DateTimeOffset.UtcNow;
        entity.Classification = request.Classification.Classification;
        entity.ClassificationSource = request.Classification.Source;
        entity.ClassificationConfidence = request.Classification.Confidence;
        entity.ClassificationReviewedByUserId = request.Classification.ReviewedByUserId ?? actorUserId;
        entity.ClassificationReviewedAt = request.Classification.ReviewedAt ?? now;
        entity.ClassificationReason = request.Classification.Reason;
        entity.ClassificationIsApprovedDemoContent = request.Classification.IsApprovedDemoContent;
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = actorUserId;

        if (previous != entity.Classification)
        {
            dbContext.ContentClassificationHistory.Add(new ContentClassificationHistoryEntity
            {
                Id = Guid.NewGuid(),
                TenantId = entity.TenantId,
                EntityType = "EvidenceItem",
                EntityId = entity.Id.ToString(),
                PreviousClassification = previous,
                NewClassification = entity.Classification,
                Source = entity.ClassificationSource,
                Confidence = entity.ClassificationConfidence,
                ReviewedByUserId = entity.ClassificationReviewedByUserId,
                ReviewedAt = entity.ClassificationReviewedAt,
                Reason = entity.ClassificationReason,
                ChangedByUserId = actorUserId,
                ChangedAt = now
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToEvidenceDto(entity);
    }

    private IReadOnlyList<Guid> ReadSubcontractorIds(Guid evidenceItemId) =>
        dbContext.Set<SubcontractorEvidenceEntity>()
            .AsNoTracking()
            .Where(link => link.EvidenceItemId == evidenceItemId)
            .Select(link => link.SubcontractorId)
            .OrderBy(id => id)
            .ToArray();

    private IReadOnlyList<Guid> ReadReportIds(Guid evidenceItemId) =>
        dbContext.Set<ReportEvidenceEntity>()
            .AsNoTracking()
            .Where(link => link.EvidenceItemId == evidenceItemId)
            .Select(link => link.ReportId)
            .OrderBy(id => id)
            .ToArray();

    private EvidenceMetadataDto ToEvidenceDto(EvidenceItemEntity item) =>
        new(
            item.Id,
            item.TenantId,
            item.Name,
            item.Type,
            item.OwnerFunction,
            item.Status,
            item.EffectiveAt,
            item.ExpiresAt,
            ReadTags(item.TagsJson),
            item.Description,
            item.Obligations.Select(link => link.ObligationId).OrderBy(id => id).ToArray(),
            item.Controls.Select(link => link.ControlId).OrderBy(id => id).ToArray(),
            item.Contracts.Select(link => link.ContractId).OrderBy(id => id).ToArray(),
            item.Vendors.Select(link => link.VendorId).OrderBy(id => id).ToArray(),
            ReadSubcontractorIds(item.Id),
            item.Employees.Select(link => link.EmployeeId).OrderBy(id => id).ToArray(),
            ReadReportIds(item.Id),
            ToClassificationDto(item),
            item.CreatedAt,
            item.UpdatedAt);

    private static IReadOnlyList<string> ReadTags(string tagsJson)
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<string[]>(tagsJson, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)) ?? [];
        }
        catch (System.Text.Json.JsonException)
        {
            return [];
        }
    }

    private static ContentClassificationDto ToClassificationDto(EvidenceItemEntity item) =>
        new(item.Classification, item.ClassificationSource, item.ClassificationConfidence, item.ClassificationReviewedByUserId, item.ClassificationReviewedAt, item.ClassificationReason, item.ClassificationIsApprovedDemoContent);

    private static ContentClassificationDto ToClassificationDto(ContractDocumentEntity item) =>
        new(item.Classification, item.ClassificationSource, item.ClassificationConfidence, item.ClassificationReviewedByUserId, item.ClassificationReviewedAt, item.ClassificationReason, item.ClassificationIsApprovedDemoContent);
}
