using Gccs.Domain.Tenancy;

namespace Gccs.Application.Tenancy;

public interface ITenantRepository
{
    Task<Tenant?> FindInCurrentTenantScopeAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default);

    Task<Tenant?> UpdateStatusInCurrentTenantScopeAsync(Guid tenantId, TenantStatus status, CancellationToken cancellationToken = default);
}
