using Gccs.Application.Tenancy;
using Gccs.Domain.Common;
using Gccs.Domain.Identity;
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
            .Include(escalation => escalation.Events)
            .Where(escalation => escalation.TenantId == tenantId)
            .OrderByDescending(escalation => escalation.CreatedAt)
            .ToArrayAsync(cancellationToken);

        return escalations.Select(ToDto).ToArray();
    }

    public async Task<CuiSupportEscalationReportDto> GetReportAsync(Guid tenantId, DateTimeOffset asOf, CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.CuiSupportEscalations.AsNoTracking().Where(e => e.TenantId == tenantId)
            .Select(e => new { e.Status, e.Severity, e.SlaDueAt }).ToArrayAsync(cancellationToken);
        static bool Complete(CuiSupportEscalationStatus status) => status is CuiSupportEscalationStatus.Resolved or CuiSupportEscalationStatus.Closed;
        return new CuiSupportEscalationReportDto(rows.Count(e => !Complete(e.Status)), rows.Count(e => Complete(e.Status)),
            rows.Count(e => !Complete(e.Status) && e.SlaDueAt < asOf),
            rows.GroupBy(e => e.Status.ToString()).ToDictionary(g => g.Key, g => g.Count()),
            rows.GroupBy(e => e.Severity.ToString()).ToDictionary(g => g.Key, g => g.Count()));
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
            CreatedByUserId = actorUserId,
            SlaDueAt = createdAt + SlaWindow(request.Severity)
        };

        dbContext.CuiSupportEscalations.Add(entity);
        AddEvent(entity, CuiSupportEscalationStatus.Submitted, "Escalation submitted.", actorUserId, createdAt);
        await SetContentBlockedAsync(tenantId, entity.AffectedEntityType, affectedId, true, createdAt, cancellationToken);
        await AddNotificationsAsync(entity, actorUserId, "A new CUI support escalation requires triage.", createdAt, cancellationToken);
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

        var statusChanged = entity.Status != request.Status;
        if (statusChanged && string.IsNullOrWhiteSpace(request.Note))
            throw new CuiSupportEscalationValidationException("Status change note is required.");
        ValidateTransition(entity.Status, request.Status);
        entity.Owner = Normalize(request.Owner);
        entity.Severity = request.Severity;
        entity.Status = request.Status;
        entity.IsAffectedContentBlocked = entity.Status == CuiSupportEscalationStatus.Closed ? entity.IsAffectedContentBlocked : IsBlocked(entity.Category, entity.Status);
        if (Guid.TryParse(entity.AffectedEntityId, out var affectedId))
            await SetContentBlockedAsync(tenantId, entity.AffectedEntityType, affectedId, entity.IsAffectedContentBlocked, updatedAt, cancellationToken, entity.Id);
        entity.UpdatedAt = updatedAt;
        entity.UpdatedByUserId = actorUserId;
        AddEvent(entity, entity.Status, statusChanged ? Normalize(request.Note!) : $"Assigned to {entity.Owner}; severity set to {entity.Severity}.", actorUserId, updatedAt);
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

        ValidateTransition(entity.Status, request.Status);

        entity.Status = request.Status;
        entity.StatusNote = Normalize(request.Note);
        entity.StatusChangedAt = changedAt;
        entity.StatusChangedByUserId = actorUserId;
        entity.IsAffectedContentBlocked = entity.Status == CuiSupportEscalationStatus.Closed ? entity.IsAffectedContentBlocked : IsBlocked(entity.Category, entity.Status);
        if (Guid.TryParse(entity.AffectedEntityId, out var affectedId))
            await SetContentBlockedAsync(tenantId, entity.AffectedEntityType, affectedId, entity.IsAffectedContentBlocked, changedAt, cancellationToken, entity.Id);
        entity.UpdatedAt = changedAt;
        entity.UpdatedByUserId = actorUserId;
        AddEvent(entity, request.Status, entity.StatusNote, actorUserId, changedAt);
        await AddNotificationsAsync(entity, actorUserId, $"CUI escalation status changed to {request.Status}.", changedAt, cancellationToken);
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

        if (entity.Status is CuiSupportEscalationStatus.Resolved or CuiSupportEscalationStatus.Closed)
            throw new CuiSupportEscalationValidationException("This escalation has already been resolved.");
        // Referral is not a release decision. Keep it contained until removal or a reviewed false positive.
        var referral = request.ResolutionType is CuiSupportEscalationResolutionType.ReferredToCustomer or CuiSupportEscalationResolutionType.ReferredToLegalOrSecurity;
        if (!referral && !await CanReleaseAsync(entity, request.ResolutionType, cancellationToken))
            throw new CuiSupportEscalationValidationException("Release requires reviewed safe content, or completed content removal and private object cleanup.");
        entity.Status = CuiSupportEscalationStatus.Resolved;
        entity.StatusNote = Normalize(request.Summary);
        entity.StatusChangedAt = resolvedAt;
        entity.StatusChangedByUserId = actorUserId;
        entity.IsAffectedContentBlocked = referral;
        if (Guid.TryParse(entity.AffectedEntityId, out var affectedId))
            await SetContentBlockedAsync(tenantId, entity.AffectedEntityType, affectedId, entity.IsAffectedContentBlocked, resolvedAt, cancellationToken, entity.Id);
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
        AddEvent(entity, CuiSupportEscalationStatus.Resolved, entity.StatusNote, actorUserId, resolvedAt);
        await AddNotificationsAsync(entity, actorUserId, referral ? "CUI escalation was referred and remains contained." : "CUI escalation was resolved.", resolvedAt, cancellationToken);

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
        if (resolution is CuiSupportEscalationResolutionType.ContentRemoved or CuiSupportEscalationResolutionType.Deleted)
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
            entity.SlaDueAt,
            entity.Status is CuiSupportEscalationStatus.Resolved or CuiSupportEscalationStatus.Closed ? "Completed" : entity.SlaDueAt < DateTimeOffset.UtcNow ? "Breached" : "Open",
            entity.Resolutions.OrderByDescending(resolution => resolution.ResolvedAt).Select(ToResolutionDto).ToArray(),
            entity.Events.OrderBy(e => e.OccurredAt).Select(e => new CuiSupportEscalationEventDto(e.Id, e.Status, e.Note, e.OccurredAt, e.ActorUserId)).ToArray());

    private IQueryable<CuiSupportEscalationEntity> Query(Guid tenantId) =>
        dbContext.CuiSupportEscalations
            .Include(escalation => escalation.Resolutions)
            .Include(escalation => escalation.Events)
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
        if (item is not null) await dbContext.Entry(item).Collection(e => e.Events).LoadAsync(ct);
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
        status is CuiSupportEscalationStatus.Submitted or CuiSupportEscalationStatus.Triage or CuiSupportEscalationStatus.Contained or CuiSupportEscalationStatus.CustomerActionRequired or CuiSupportEscalationStatus.Reopened;

    private static void ValidateTransition(CuiSupportEscalationStatus current, CuiSupportEscalationStatus next)
    {
        if (next == CuiSupportEscalationStatus.Reopened && current is not (CuiSupportEscalationStatus.Resolved or CuiSupportEscalationStatus.Closed))
            throw new CuiSupportEscalationValidationException("Only a resolved or closed escalation can be reopened.");
        if (next == CuiSupportEscalationStatus.Closed && current != CuiSupportEscalationStatus.Resolved)
            throw new CuiSupportEscalationValidationException("Only a resolved escalation can be closed.");
        if (current is CuiSupportEscalationStatus.Resolved or CuiSupportEscalationStatus.Closed && next is not (CuiSupportEscalationStatus.Reopened or CuiSupportEscalationStatus.Closed))
            throw new CuiSupportEscalationValidationException("Use Reopened before resuming a completed escalation.");
    }

    private static TimeSpan SlaWindow(CuiSupportEscalationSeverity severity) => severity switch
    {
        CuiSupportEscalationSeverity.Critical => TimeSpan.FromHours(1), CuiSupportEscalationSeverity.High => TimeSpan.FromHours(4),
        CuiSupportEscalationSeverity.Medium => TimeSpan.FromHours(24), _ => TimeSpan.FromHours(72)
    };

    private void AddEvent(CuiSupportEscalationEntity escalation, CuiSupportEscalationStatus status, string note, Guid actor, DateTimeOffset at) =>
        dbContext.CuiSupportEscalationEvents.Add(new CuiSupportEscalationEventEntity { Id = Guid.NewGuid(), EscalationId = escalation.Id, Status = status, Note = Normalize(note), ActorUserId = actor, OccurredAt = at });

    private async Task AddNotificationsAsync(CuiSupportEscalationEntity escalation, Guid actor, string message, DateTimeOffset at, CancellationToken ct)
    {
        var recipients = await dbContext.TenantMemberships.AsNoTracking()
            .Where(m => m.TenantId == escalation.TenantId && m.Status == MembershipStatus.Active &&
                (m.RoleName == RoleCatalog.Owner || m.RoleName == RoleCatalog.Admin || m.RoleName == RoleCatalog.Advisor))
            .Select(m => m.UserId).Distinct().ToArrayAsync(ct);
        var existing = await dbContext.NotificationDeliveries.AsNoTracking().Where(n => n.TenantId == escalation.TenantId &&
            n.SourceTaskId == escalation.Id && n.Category == $"cui_escalation_{escalation.Status}").Select(n => n.UserId).ToArrayAsync(ct);
        foreach (var userId in recipients.Except(existing))
            dbContext.NotificationDeliveries.Add(new NotificationDeliveryEntity { Id = Guid.NewGuid(), TenantId = escalation.TenantId, UserId = userId,
                SourceTaskId = escalation.Id, SourceType = "CuiSupportEscalation", LinkUrl = "/app#/evidence", Category = $"cui_escalation_{escalation.Status}",
                Status = "Delivered", Placeholder = message, AttemptedAt = at, CreatedAt = at, CreatedByUserId = actor });
    }

    private static string Normalize(string value) => value.Trim();
}
