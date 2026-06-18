using Gccs.Application.Tenancy;
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
        var entity = new CuiSupportEscalationEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SourceWorkflow = Normalize(request.SourceWorkflow),
            AffectedEntityType = Normalize(request.AffectedEntityType),
            AffectedEntityId = Normalize(request.AffectedEntityId),
            Category = request.Category,
            Severity = request.Severity,
            Status = CuiSupportEscalationStatus.Submitted,
            Description = Normalize(request.Description),
            IsAffectedContentBlocked = request.Category is CuiSupportEscalationCategory.ProhibitedData,
            CreatedAt = createdAt,
            CreatedByUserId = actorUserId
        };

        dbContext.CuiSupportEscalations.Add(entity);
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
        var entity = await Query(tenantId).SingleOrDefaultAsync(
            escalation => escalation.TenantId == tenantId && escalation.Id == escalationId,
            cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Owner = Normalize(request.Owner);
        entity.Severity = request.Severity;
        entity.Status = request.Status;
        entity.IsAffectedContentBlocked = IsBlocked(entity.Category, entity.Status);
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
        var entity = await Query(tenantId).SingleOrDefaultAsync(escalation => escalation.Id == escalationId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Status = request.Status;
        entity.StatusNote = Normalize(request.Note);
        entity.StatusChangedAt = changedAt;
        entity.StatusChangedByUserId = actorUserId;
        entity.IsAffectedContentBlocked = IsBlocked(entity.Category, entity.Status);
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
        var entity = await Query(tenantId).SingleOrDefaultAsync(escalation => escalation.Id == escalationId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Status = CuiSupportEscalationStatus.Resolved;
        entity.StatusNote = Normalize(request.Summary);
        entity.StatusChangedAt = resolvedAt;
        entity.StatusChangedByUserId = actorUserId;
        entity.IsAffectedContentBlocked = false;
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

    private static CuiSupportEscalationResolutionDto ToResolutionDto(CuiSupportEscalationResolutionEntity entity) =>
        new(
            entity.Id,
            entity.EscalationId,
            entity.ResolutionType,
            entity.Summary,
            entity.ResolvedAt,
            entity.ResolvedByUserId);

    private static bool IsBlocked(CuiSupportEscalationCategory category, CuiSupportEscalationStatus status) =>
        category is CuiSupportEscalationCategory.ProhibitedData &&
        status is CuiSupportEscalationStatus.Submitted or CuiSupportEscalationStatus.Triage or CuiSupportEscalationStatus.Contained;

    private static string Normalize(string value) => value.Trim();
}
