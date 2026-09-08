using Gccs.Application.Tenancy;
using Gccs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Tenancy;

public sealed class EfContentContainmentRepository(GccsDbContext db) : IContentContainmentRepository
{
    public Task<bool> IsBlockedAsync(Guid tenantId, string entityType, string entityId, CancellationToken cancellationToken = default) =>
        db.CuiSupportEscalations.AsNoTracking().AnyAsync(e =>
            e.TenantId == tenantId && e.AffectedEntityType == entityType && e.AffectedEntityId == entityId &&
            e.Status != CuiSupportEscalationStatus.Resolved, cancellationToken);
}
