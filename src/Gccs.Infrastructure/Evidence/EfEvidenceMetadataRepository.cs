using System.Text.Json;
using Gccs.Application.Common;
using Gccs.Application.Evidence;
using Gccs.Application.Security;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Domain.Evidence;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Evidence;

public sealed class EfEvidenceMetadataRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : IEvidenceMetadataRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<EvidenceMetadataDto>> ListCurrentTenantAsync(
        EvidenceMetadataQuery query,
        CancellationToken cancellationToken = default)
    {
        var entities = await QueryCurrentTenant()
            .OrderBy(evidence => evidence.ExpiresAt ?? DateOnly.MaxValue)
            .ThenBy(evidence => evidence.Name)
            .ToArrayAsync(cancellationToken);
        var mapped = entities.Select(ToDto).ToArray();

        if (!string.IsNullOrWhiteSpace(query.Tag))
        {
            mapped = mapped
                .Where(evidence => evidence.Tags.Any(tag => string.Equals(tag, query.Tag.Trim(), StringComparison.OrdinalIgnoreCase)))
                .ToArray();
        }

        return mapped;
    }

    public async Task<EvidenceMetadataDto?> FindCurrentTenantAsync(
        Guid evidenceItemId,
        CancellationToken cancellationToken = default)
    {
        var entity = await QueryCurrentTenant()
            .SingleOrDefaultAsync(evidence => evidence.Id == evidenceItemId, cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<EvidenceMetadataDto> CreateCurrentTenantAsync(
        UpsertEvidenceMetadataRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new EvidenceItemEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            Name = request.Title,
            Description = request.Description,
            Type = request.Type,
            OwnerFunction = request.OwnerFunction,
            Status = request.Status,
            EffectiveAt = request.EffectiveAt,
            ExpiresAt = request.ExpiresAt,
            TagsJson = JsonSerializer.Serialize(request.Tags, JsonOptions),
            Classification = request.Classification?.Classification ?? ContentClassification.Unclassified,
            ClassificationSource = request.Classification?.Source ?? ContentClassificationSource.UserSelected,
            ClassificationConfidence = request.Classification?.Confidence,
            ClassificationReviewedByUserId = request.Classification?.ReviewedByUserId,
            ClassificationReviewedAt = request.Classification?.ReviewedAt,
            ClassificationReason = request.Classification?.Reason,
            ClassificationIsApprovedDemoContent = request.Classification?.IsApprovedDemoContent ?? false,
            CreatedAt = now,
            CreatedByUserId = actorUserId
        };

        dbContext.EvidenceItems.Add(entity);
        SyncLinks(entity.Id, request);
        AddClassificationHistory(entity.TenantId, entity.Id.ToString(), null, entity, actorUserId, now);
        await SyncExpirationTaskAsync(entity, actorUserId, now, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await FindCurrentTenantAsync(entity.Id, cancellationToken))!;
    }

    public async Task<EvidenceMetadataDto?> UpdateCurrentTenantAsync(
        Guid evidenceItemId,
        UpsertEvidenceMetadataRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = await QueryCurrentTenant()
            .SingleOrDefaultAsync(evidence => evidence.Id == evidenceItemId, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var previousClassification = entity.Classification;
        entity.Name = request.Title;
        entity.Description = request.Description;
        entity.Type = request.Type;
        entity.OwnerFunction = request.OwnerFunction;
        entity.Status = request.Status;
        entity.EffectiveAt = request.EffectiveAt;
        entity.ExpiresAt = request.ExpiresAt;
        entity.TagsJson = JsonSerializer.Serialize(request.Tags, JsonOptions);
        if (request.Classification is not null)
        {
            ApplyClassification(entity, request.Classification);
        }
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = actorUserId;

        SyncLinks(entity.Id, request);
        if (previousClassification != entity.Classification)
        {
            AddClassificationHistory(entity.TenantId, entity.Id.ToString(), previousClassification, entity, actorUserId, now);
        }
        await SyncExpirationTaskAsync(entity, actorUserId, now, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<IReadOnlyList<string>> FindMissingControlIdsAsync(
        IReadOnlyList<string> controlIds,
        CancellationToken cancellationToken = default)
    {
        var requested = controlIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id)
            .ToArray();

        if (requested.Length == 0)
        {
            return [];
        }

        var existing = await dbContext.Controls
            .Where(control => requested.Contains(control.Id))
            .Select(control => control.Id)
            .ToArrayAsync(cancellationToken);

        return requested
            .Except(existing, StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id)
            .ToArray();
    }

    public async Task<EvidenceReviewDto?> ApplyCurrentTenantReviewAsync(
        Guid evidenceItemId,
        EvidenceReviewDecision decision,
        string? comment,
        Guid actorUserId,
        DateTimeOffset reviewedAt,
        CancellationToken cancellationToken = default)
    {
        var entity = await QueryCurrentTenant()
            .SingleOrDefaultAsync(evidence => evidence.Id == evidenceItemId, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.Status = ToStatus(decision);
        entity.UpdatedAt = reviewedAt;
        entity.UpdatedByUserId = actorUserId;
        if (entity.Status == EvidenceStatus.Approved)
        {
            entity.ApprovedAt = reviewedAt;
            entity.ApprovedByUserId = actorUserId;
        }
        else
        {
            entity.ApprovedAt = null;
            entity.ApprovedByUserId = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new EvidenceReviewDto(
            Guid.NewGuid(),
            entity.Id,
            entity.TenantId,
            decision,
            entity.Status,
            comment,
            actorUserId,
            reviewedAt,
            entity.Status == EvidenceStatus.Approved);
    }

    private IQueryable<EvidenceItemEntity> QueryCurrentTenant() =>
        dbContext.EvidenceItems
            .Include(evidence => evidence.Obligations)
            .Include(evidence => evidence.Controls)
            .Include(evidence => evidence.Contracts)
            .Include(evidence => evidence.Vendors)
            .Include(evidence => evidence.Employees)
            .Where(evidence => evidence.TenantId == tenantContext.TenantId);

    private void SyncLinks(Guid evidenceItemId, UpsertEvidenceMetadataRequest request)
    {
        dbContext.Set<EvidenceObligationEntity>().RemoveRange(
            dbContext.Set<EvidenceObligationEntity>().Where(link => link.EvidenceItemId == evidenceItemId));
        dbContext.Set<EvidenceControlEntity>().RemoveRange(
            dbContext.Set<EvidenceControlEntity>().Where(link => link.EvidenceItemId == evidenceItemId));
        dbContext.Set<EvidenceContractEntity>().RemoveRange(
            dbContext.Set<EvidenceContractEntity>().Where(link => link.EvidenceItemId == evidenceItemId));
        dbContext.Set<EvidenceVendorEntity>().RemoveRange(
            dbContext.Set<EvidenceVendorEntity>().Where(link => link.EvidenceItemId == evidenceItemId));
        dbContext.Set<EvidenceEmployeeEntity>().RemoveRange(
            dbContext.Set<EvidenceEmployeeEntity>().Where(link => link.EvidenceItemId == evidenceItemId));
        dbContext.Set<SubcontractorEvidenceEntity>().RemoveRange(
            dbContext.Set<SubcontractorEvidenceEntity>().Where(link => link.EvidenceItemId == evidenceItemId));
        dbContext.Set<ReportEvidenceEntity>().RemoveRange(
            dbContext.Set<ReportEvidenceEntity>().Where(link => link.EvidenceItemId == evidenceItemId));

        dbContext.Set<EvidenceObligationEntity>().AddRange(request.ObligationIds.Select(id => new EvidenceObligationEntity
        {
            EvidenceItemId = evidenceItemId,
            ObligationId = id
        }));
        dbContext.Set<EvidenceControlEntity>().AddRange(request.ControlIds.Select(id => new EvidenceControlEntity
        {
            EvidenceItemId = evidenceItemId,
            ControlId = id
        }));
        dbContext.Set<EvidenceContractEntity>().AddRange(request.ContractIds.Select(id => new EvidenceContractEntity
        {
            EvidenceItemId = evidenceItemId,
            ContractId = id
        }));
        dbContext.Set<EvidenceVendorEntity>().AddRange(request.VendorIds.Select(id => new EvidenceVendorEntity
        {
            EvidenceItemId = evidenceItemId,
            VendorId = id
        }));
        dbContext.Set<EvidenceEmployeeEntity>().AddRange(request.EmployeeIds.Select(id => new EvidenceEmployeeEntity
        {
            EvidenceItemId = evidenceItemId,
            EmployeeId = id
        }));
        dbContext.Set<SubcontractorEvidenceEntity>().AddRange(request.SubcontractorIds.Select(id => new SubcontractorEvidenceEntity
        {
            EvidenceItemId = evidenceItemId,
            SubcontractorId = id
        }));
        dbContext.Set<ReportEvidenceEntity>().AddRange(request.ReportIds.Select(id => new ReportEvidenceEntity
        {
            EvidenceItemId = evidenceItemId,
            ReportId = id
        }));
    }

    private static EvidenceStatus ToStatus(EvidenceReviewDecision decision) =>
        decision switch
        {
            EvidenceReviewDecision.Approve => EvidenceStatus.Approved,
            EvidenceReviewDecision.Reject or EvidenceReviewDecision.RequestChanges => EvidenceStatus.Rejected,
            EvidenceReviewDecision.Archive => EvidenceStatus.Archived,
            EvidenceReviewDecision.Expire => EvidenceStatus.Expired,
            _ => EvidenceStatus.InReview
        };

    private async Task SyncExpirationTaskAsync(
        EvidenceItemEntity entity,
        Guid actorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (entity.ExpiresAt is not { } expiresAt)
        {
            return;
        }

        var reminderDueAt = expiresAt.AddDays(-30);
        var exists = await dbContext.ComplianceTasks.AnyAsync(
            task =>
                task.TenantId == tenantContext.TenantId &&
                task.Type == ComplianceTaskType.Renewal &&
                task.EvidenceItemId == entity.Id &&
                task.DueAt == reminderDueAt,
            cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.ComplianceTasks.Add(new ComplianceTaskEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            Title = $"Renew evidence: {entity.Name}",
            Description = $"{entity.Name} expires on {expiresAt:yyyy-MM-dd}.",
            Type = ComplianceTaskType.Renewal,
            Status = ComplianceTaskStatus.Open,
            RiskLevel = RiskLevel.Medium,
            OwnerFunction = entity.OwnerFunction,
            DueAt = reminderDueAt,
            EvidenceItemId = entity.Id,
            CreatedAt = now,
            CreatedByUserId = actorUserId
        });
    }

    private EvidenceMetadataDto ToDto(EvidenceItemEntity entity) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.Name,
            entity.Type,
            entity.OwnerFunction,
            entity.Status,
            entity.EffectiveAt,
            entity.ExpiresAt,
            ReadTags(entity.TagsJson),
            entity.Description,
            entity.Obligations.Select(link => link.ObligationId).OrderBy(id => id).ToArray(),
            entity.Controls.Select(link => link.ControlId).OrderBy(id => id).ToArray(),
            entity.Contracts.Select(link => link.ContractId).OrderBy(id => id).ToArray(),
            entity.Vendors.Select(link => link.VendorId).OrderBy(id => id).ToArray(),
            ReadSubcontractorIds(entity.Id),
            entity.Employees.Select(link => link.EmployeeId).OrderBy(id => id).ToArray(),
            ReadReportIds(entity.Id),
            ToClassificationDto(entity),
            entity.CreatedAt,
            entity.UpdatedAt);

    private static void ApplyClassification(EvidenceItemEntity entity, ContentClassificationRequest classification)
    {
        entity.Classification = classification.Classification;
        entity.ClassificationSource = classification.Source;
        entity.ClassificationConfidence = classification.Confidence;
        entity.ClassificationReviewedByUserId = classification.ReviewedByUserId;
        entity.ClassificationReviewedAt = classification.ReviewedAt;
        entity.ClassificationReason = classification.Reason;
        entity.ClassificationIsApprovedDemoContent = classification.IsApprovedDemoContent;
    }

    private void AddClassificationHistory(
        Guid tenantId,
        string entityId,
        ContentClassification? previousClassification,
        EvidenceItemEntity entity,
        Guid actorUserId,
        DateTimeOffset changedAt)
    {
        dbContext.ContentClassificationHistory.Add(new ContentClassificationHistoryEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EntityType = "EvidenceItem",
            EntityId = entityId,
            PreviousClassification = previousClassification,
            NewClassification = entity.Classification,
            Source = entity.ClassificationSource,
            Confidence = entity.ClassificationConfidence,
            ReviewedByUserId = entity.ClassificationReviewedByUserId,
            ReviewedAt = entity.ClassificationReviewedAt,
            Reason = entity.ClassificationReason,
            ChangedByUserId = actorUserId,
            ChangedAt = changedAt
        });
    }

    private static ContentClassificationDto ToClassificationDto(EvidenceItemEntity entity) =>
        new(
            entity.Classification,
            entity.ClassificationSource,
            entity.ClassificationConfidence,
            entity.ClassificationReviewedByUserId,
            entity.ClassificationReviewedAt,
            entity.ClassificationReason,
            entity.ClassificationIsApprovedDemoContent);

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

    private static IReadOnlyList<string> ReadTags(string tagsJson)
    {
        try
        {
            return JsonSerializer.Deserialize<string[]>(tagsJson, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
