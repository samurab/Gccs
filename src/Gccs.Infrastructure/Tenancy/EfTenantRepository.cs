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

    private static Tenant ToDomain(TenantEntity entity) =>
        new(
            entity.Id,
            entity.Name,
            entity.Status,
            entity.DataPosture,
            entity.TrialEndsAt,
            new EntityAudit(entity.CreatedAt, entity.CreatedByUserId, entity.UpdatedAt, entity.UpdatedByUserId));
}
