using Gccs.Application.Tenancy;
using Gccs.Domain.Common;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Tenancy;

public sealed class EfCuiSupportEscalationRepository(GccsDbContext dbContext) : ICuiSupportEscalationRepository
{
    public async Task<IReadOnlyList<CuiSupportEscalationDto>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var escalations = await dbContext.CuiSupportEscalations
            .AsNoTracking()
            .Include(escalation => escalation.Resolutions)
            .Where(escalation => escalation.TenantId == tenantId)
            .OrderByDescending(escalation => escalation.CreatedAt)
            .ToArrayAsync(cancellationToken);

        return escalations.Select(ToDto).ToArray();
    }

    public async Task<CuiSupportEscalationDto> CreateAsync(
        Guid tenantId,
        CreateCuiSupportEscalationRequest request,
        Guid actorUserId,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(request.AffectedEntityId, out var affectedId) || affectedId == Guid.Empty ||
            !await ContentExistsAsync(tenantId, request.AffectedEntityType.Trim(), affectedId, cancellationToken))
            throw new CuiSupportEscalationValidationException("The affected content reference is unavailable in this tenant.");
        var entity = new CuiSupportEscalationEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SourceWorkflow = Normalize(request.SourceWorkflow),
            AffectedEntityType = Normalize(request.AffectedEntityType),
            AffectedEntityId = Guid.TryParse(request.AffectedEntityId, out var contentId) ? contentId.ToString() : Normalize(request.AffectedEntityId),
            Category = request.Category,
            Severity = request.Severity,
            Status = CuiSupportEscalationStatus.Submitted,
            Description = Normalize(request.Description),
            IsAffectedContentBlocked = true,
            CreatedAt = createdAt,
            CreatedByUserId = actorUserId
        };

        dbContext.CuiSupportEscalations.Add(entity);
        await SetContentBlockedAsync(tenantId, entity.AffectedEntityType, affectedId, true, createdAt, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<CuiSupportEscalationDto?> UpdateSupportFieldsAsync(
        Guid tenantId,
        Guid escalationId,
        UpdateCuiSupportEscalationRequest request,
        Guid actorUserId,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken = default)
    {
        var entity = await FindForUpdateAsync(tenantId, escalationId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Owner = Normalize(request.Owner);
        entity.Severity = request.Severity;
        entity.Status = request.Status;
        entity.IsAffectedContentBlocked = IsBlocked(entity.Category, entity.Status);
        if (Guid.TryParse(entity.AffectedEntityId, out var affectedId))
            await SetContentBlockedAsync(tenantId, entity.AffectedEntityType, affectedId, entity.IsAffectedContentBlocked, updatedAt, cancellationToken, entity.Id);
        entity.UpdatedAt = updatedAt;
        entity.UpdatedByUserId = actorUserId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<CuiSupportEscalationDto?> ChangeStatusAsync(
        Guid tenantId,
        Guid escalationId,
        ChangeCuiSupportEscalationStatusRequest request,
        Guid actorUserId,
        DateTimeOffset changedAt,
        CancellationToken cancellationToken = default)
    {
        var entity = await FindForUpdateAsync(tenantId, escalationId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Status = request.Status;
        entity.StatusNote = Normalize(request.Note);
        entity.StatusChangedAt = changedAt;
        entity.StatusChangedByUserId = actorUserId;
        entity.IsAffectedContentBlocked = IsBlocked(entity.Category, entity.Status);
        if (Guid.TryParse(entity.AffectedEntityId, out var affectedId))
            await SetContentBlockedAsync(tenantId, entity.AffectedEntityType, affectedId, entity.IsAffectedContentBlocked, changedAt, cancellationToken, entity.Id);
        entity.UpdatedAt = changedAt;
        entity.UpdatedByUserId = actorUserId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<CuiSupportEscalationDto?> ResolveAsync(
        Guid tenantId,
        Guid escalationId,
        ResolveCuiSupportEscalationRequest request,
        Guid actorUserId,
        DateTimeOffset resolvedAt,
        CancellationToken cancellationToken = default)
    {
        var entity = await FindForUpdateAsync(tenantId, escalationId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (entity.Status == CuiSupportEscalationStatus.Resolved)
            throw new CuiSupportEscalationValidationException("This escalation has already been resolved.");
        // Referral is not a release decision. Keep it contained until removal or a reviewed false positive.
        if (request.ResolutionType == CuiSupportEscalationResolutionType.ReferredToCustomer)
            throw new CuiSupportEscalationValidationException("Customer referral does not release content. Keep the escalation contained.");
        if (!await CanReleaseAsync(entity, request.ResolutionType, cancellationToken))
            throw new CuiSupportEscalationValidationException("Release requires reviewed safe content, or completed content removal and private object cleanup.");
        entity.Status = CuiSupportEscalationStatus.Resolved;
        entity.StatusNote = Normalize(request.Summary);
        entity.StatusChangedAt = resolvedAt;
        entity.StatusChangedByUserId = actorUserId;
        entity.IsAffectedContentBlocked = false;
        if (Guid.TryParse(entity.AffectedEntityId, out var affectedId))
            await SetContentBlockedAsync(tenantId, entity.AffectedEntityType, affectedId, false, resolvedAt, cancellationToken, entity.Id);
        entity.UpdatedAt = resolvedAt;
        entity.UpdatedByUserId = actorUserId;
        dbContext.CuiSupportEscalationResolutions.Add(new CuiSupportEscalationResolutionEntity
        {
            Id = Guid.NewGuid(),
            EscalationId = entity.Id,
            ResolutionType = request.ResolutionType,
            Summary = Normalize(request.Summary),
            ResolvedAt = resolvedAt,
            ResolvedByUserId = actorUserId
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    private Task<bool> ContentExistsAsync(Guid tenantId, string type, Guid id, CancellationToken ct) => type switch
    {
        "EvidenceItem" => dbContext.EvidenceItems.AnyAsync(e => e.TenantId == tenantId && e.Id == id, ct),
        "EvidenceFileVersion" => dbContext.EvidenceFileVersions.AnyAsync(e => e.Id == id && e.EvidenceItem!.TenantId == tenantId, ct),
        "ClassifiedNote" => dbContext.Set<ClassifiedNoteEntity>().AnyAsync(e => e.Id == id && e.TenantId == tenantId, ct),
        "ExtractionJob" => dbContext.Set<ExtractionJobEntity>().AnyAsync(e => e.Id == id && e.TenantId == tenantId, ct),
        "ContractDocument" => dbContext.Set<ContractDocumentEntity>().AnyAsync(e => e.Id == id && e.Contract!.TenantId == tenantId, ct),
        "Report" => dbContext.Set<ReportEntity>().AnyAsync(e => e.Id == id && e.TenantId == tenantId, ct),
        _ => Task.FromResult(false)
    };

    private async Task SetContentBlockedAsync(
        Guid tenantId,
        string type,
        Guid id,
        bool blocked,
        DateTimeOffset changedAt,
        CancellationToken ct,
        Guid? currentEscalationId = null)
    {
        if (!blocked && await dbContext.CuiSupportEscalations.AnyAsync(e =>
                e.TenantId == tenantId && e.Id != currentEscalationId &&
                e.AffectedEntityType == type && e.AffectedEntityId == id.ToString() &&
                e.IsAffectedContentBlocked, ct))
            blocked = true;

        IContainableContentEntity? content = type switch
        {
            "EvidenceItem" => await dbContext.EvidenceItems.SingleOrDefaultAsync(e => e.TenantId == tenantId && e.Id == id, ct),
            "EvidenceFileVersion" => await dbContext.EvidenceFileVersions.SingleOrDefaultAsync(e => e.Id == id && e.EvidenceItem!.TenantId == tenantId, ct),
            "ClassifiedNote" => await dbContext.Set<ClassifiedNoteEntity>().SingleOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, ct),
            "ExtractionJob" => await dbContext.Set<ExtractionJobEntity>().SingleOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, ct),
            "ContractDocument" => await dbContext.Set<ContractDocumentEntity>().SingleOrDefaultAsync(e => e.Id == id && e.Contract!.TenantId == tenantId, ct),
            "Report" => await dbContext.Reports.SingleOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, ct),
            _ => null
        };
        if (content is null)
            throw new CuiSupportEscalationValidationException("The affected content reference is unavailable in this tenant.");
        content.IsUseBlocked = blocked;
        content.UseBlockedAt = blocked ? content.UseBlockedAt ?? changedAt : null;
    }

    private async Task<bool> CanReleaseAsync(CuiSupportEscalationEntity escalation, CuiSupportEscalationResolutionType resolution, CancellationToken ct)
    {
        if (!Guid.TryParse(escalation.AffectedEntityId, out var id)) return false;
        var tenantId = escalation.TenantId;
        if (resolution == CuiSupportEscalationResolutionType.ContentRemoved)
        {
            // Never interpret archive as deletion of an immutable report artifact.
            if (escalation.AffectedEntityType == "Report") return false;
            if (escalation.AffectedEntityType == "EvidenceItem")
            {
                // Evidence metadata itself is still retained; a file tombstone is not complete removal.
                if (await dbContext.EvidenceItems.AnyAsync(e => e.Id == id && e.TenantId == tenantId, ct)) return false;
            }
            else if (escalation.AffectedEntityType != "ContractDocument" ||
                await ContentExistsAsync(tenantId, escalation.AffectedEntityType, id, ct)) return false;
            // Conservative: no removal claim while any tenant cleanup remains incomplete.
            return !await dbContext.Set<ObjectCleanupEntity>().AnyAsync(e => e.TenantId == tenantId && e.CompletedAt == null, ct);
        }
        if (escalation.AffectedEntityType == "EvidenceItem")
        {
            var evidence = await dbContext.EvidenceItems.AsNoTracking().SingleOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, ct);
            if (evidence is null || !Safe(evidence.Classification) ||
                evidence.ClassificationSource != ContentClassificationSource.AdminReviewed ||
                evidence.ClassificationReviewedByUserId is null || evidence.ClassificationReviewedByUserId == Guid.Empty ||
                evidence.ClassificationReviewedAt < escalation.CreatedAt || evidence.ClassificationReviewedAt is null) return false;
            return !await dbContext.EvidenceFileVersions.AnyAsync(v => v.EvidenceItemId == id && v.DeletedAt == null &&
                v.Classification != ContentClassification.Unclassified && v.Classification != ContentClassification.Fci, ct);
        }
        IClassifiedContentEntity? reviewed = escalation.AffectedEntityType switch
        {
            "EvidenceFileVersion" => await dbContext.EvidenceFileVersions.AsNoTracking().SingleOrDefaultAsync(e => e.Id == id && e.EvidenceItem!.TenantId == tenantId, ct),
            "ContractDocument" => await dbContext.Set<ContractDocumentEntity>().AsNoTracking().SingleOrDefaultAsync(e => e.Id == id && e.Contract!.TenantId == tenantId, ct),
            "ClassifiedNote" => await dbContext.Set<ClassifiedNoteEntity>().AsNoTracking().SingleOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, ct),
            "ExtractionJob" => await dbContext.Set<ExtractionJobEntity>().AsNoTracking().SingleOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, ct),
            "Report" => (await dbContext.Reports.AsNoTracking().Include(e => e.CurrentClassification).SingleOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, ct))?.CurrentClassification,
            _ => null
        };
        return reviewed is not null && reviewed.ClassificationRevision > 0 && Safe(reviewed.Classification) &&
            reviewed.ClassificationSource == ContentClassificationSource.AdminReviewed &&
            reviewed.ClassificationReviewedByUserId is not null && reviewed.ClassificationReviewedByUserId != Guid.Empty &&
            reviewed.ClassificationReviewedAt >= escalation.CreatedAt;
    }

    private static bool Safe(ContentClassification classification) =>
        classification is ContentClassification.Unclassified or ContentClassification.Fci;

    private static CuiSupportEscalationDto ToDto(CuiSupportEscalationEntity entity) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.SourceWorkflow,
            entity.AffectedEntityType,
            entity.AffectedEntityId,
            entity.Category,
            entity.Severity,
            entity.Status,
            entity.Owner,
            entity.Description,
            entity.IsAffectedContentBlocked,
            entity.StatusNote,
            entity.StatusChangedAt,
            entity.StatusChangedByUserId,
            entity.CreatedAt,
            entity.CreatedByUserId ?? Guid.Empty,
            entity.UpdatedAt,
            entity.UpdatedByUserId,
            entity.Resolutions.OrderByDescending(resolution => resolution.ResolvedAt).Select(ToResolutionDto).ToArray());

    private IQueryable<CuiSupportEscalationEntity> Query(Guid tenantId) =>
        dbContext.CuiSupportEscalations
            .Include(escalation => escalation.Resolutions)
            .Where(escalation => escalation.TenantId == tenantId);

    private async Task<CuiSupportEscalationEntity?> FindForUpdateAsync(Guid tenantId, Guid id, CancellationToken ct)
    {
        if (!dbContext.Database.IsNpgsql()) return await Query(tenantId).SingleOrDefaultAsync(e => e.Id == id, ct);
        if (dbContext.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Containment transitions require an application transaction.");
        var items = await dbContext.CuiSupportEscalations.FromSqlInterpolated($"""
            SELECT * FROM gccs.cui_support_escalations WHERE tenant_id = {tenantId} AND id = {id} FOR UPDATE
            """).ToArrayAsync(ct);
        var item = items.SingleOrDefault();
        if (item is not null) await dbContext.Entry(item).Collection(e => e.Resolutions).LoadAsync(ct);
        return item;
    }

    private static CuiSupportEscalationResolutionDto ToResolutionDto(CuiSupportEscalationResolutionEntity entity) =>
        new(
            entity.Id,
            entity.EscalationId,
            entity.ResolutionType,
            entity.Summary,
            entity.ResolvedAt,
            entity.ResolvedByUserId);

    private static bool IsBlocked(CuiSupportEscalationCategory category, CuiSupportEscalationStatus status) =>
        status is CuiSupportEscalationStatus.Submitted or CuiSupportEscalationStatus.Triage or CuiSupportEscalationStatus.Contained;

    private static string Normalize(string value) => value.Trim();
}
