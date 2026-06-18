using Gccs.Application.Security;
using Gccs.Application.Tenancy;
using Gccs.Domain.Common;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Tenancy;

public sealed class EfTenantRepository(GccsDbContext dbContext, ICurrentTenantContext tenantContext) : ITenantRepository
{
    public async Task<Tenant?> FindInCurrentTenantScopeAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId != tenantContext.TenantId)
        {
            return null;
        }

        var entity = await dbContext.Tenants
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == tenantId, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<TenantDataPosture?> FindCurrentTenantDataHandlingModeAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Tenants
            .AsNoTracking()
            .Where(candidate => candidate.Id == tenantContext.TenantId)
            .Select(candidate => (TenantDataPosture?)candidate.DataPosture)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<TenantDataHandlingModeHistoryDto>> ListDataHandlingModeHistoryInCurrentTenantScopeAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (tenantId != tenantContext.TenantId)
        {
            return [];
        }

        return await dbContext.TenantDataHandlingModeHistory
            .AsNoTracking()
            .Where(candidate => candidate.TenantId == tenantId)
            .OrderByDescending(candidate => candidate.ChangedAt)
            .ThenByDescending(candidate => candidate.Id)
            .Select(candidate => new TenantDataHandlingModeHistoryDto(
                candidate.Id,
                candidate.TenantId,
                candidate.PreviousMode,
                candidate.NewMode,
                candidate.ActorUserId,
                candidate.ChangedAt,
                candidate.Reason,
                candidate.ApprovalRecordReference))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Status = tenant.Status,
            DataPosture = tenant.DataPosture,
            TrialEndsAt = tenant.TrialEndsAt,
            CreatedAt = tenant.Audit.CreatedAt,
            CreatedByUserId = tenant.Audit.CreatedByUserId,
            UpdatedAt = tenant.Audit.UpdatedAt,
            UpdatedByUserId = tenant.Audit.UpdatedByUserId
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddDataHandlingModeHistoryAsync(
        Guid tenantId,
        TenantDataPosture? previousMode,
        TenantDataPosture newMode,
        Guid actorUserId,
        string reason,
        string? approvalRecordReference,
        CancellationToken cancellationToken = default)
    {
        dbContext.TenantDataHandlingModeHistory.Add(new TenantDataHandlingModeHistoryEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PreviousMode = previousMode,
            NewMode = newMode,
            ActorUserId = actorUserId,
            ChangedAt = DateTimeOffset.UtcNow,
            Reason = reason,
            ApprovalRecordReference = string.IsNullOrWhiteSpace(approvalRecordReference)
                ? null
                : approvalRecordReference.Trim()
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Tenant?> UpdateStatusInCurrentTenantScopeAsync(
        Guid tenantId,
        TenantStatus status,
        CancellationToken cancellationToken = default)
    {
        if (tenantId != tenantContext.TenantId)
        {
            return null;
        }

        var entity = await dbContext.Tenants
            .SingleOrDefaultAsync(candidate => candidate.Id == tenantId, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.Status = status;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedByUserId = tenantContext.UserId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDomain(entity);
    }

    public async Task<Tenant?> UpdateDataHandlingModeInCurrentTenantScopeAsync(
        Guid tenantId,
        TenantDataPosture dataHandlingMode,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (tenantId != tenantContext.TenantId)
        {
            return null;
        }

        var entity = await dbContext.Tenants
            .SingleOrDefaultAsync(candidate => candidate.Id == tenantId, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.DataPosture = dataHandlingMode;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedByUserId = actorUserId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDomain(entity);
    }

    private static Tenant ToDomain(TenantEntity entity) =>
        new(
            entity.Id,
            entity.Name,
            entity.Status,
            entity.DataPosture,
            entity.TrialEndsAt,
            new EntityAudit(entity.CreatedAt, entity.CreatedByUserId, entity.UpdatedAt, entity.UpdatedByUserId));
}
