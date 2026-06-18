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
        var entity = await dbContext.CuiSupportEscalations.SingleOrDefaultAsync(
            escalation => escalation.TenantId == tenantId && escalation.Id == escalationId,
            cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Owner = Normalize(request.Owner);
        entity.Severity = request.Severity;
        entity.Status = request.Status;
        entity.UpdatedAt = updatedAt;
        entity.UpdatedByUserId = actorUserId;
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
            entity.CreatedAt,
            entity.CreatedByUserId ?? Guid.Empty,
            entity.UpdatedAt,
            entity.UpdatedByUserId);

    private static string Normalize(string value) => value.Trim();
}
