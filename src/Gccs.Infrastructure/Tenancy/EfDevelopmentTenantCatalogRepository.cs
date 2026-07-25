using Gccs.Application.Tenancy;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Tenancy;

public sealed class EfDevelopmentTenantCatalogRepository(GccsDbContext dbContext)
    : IDevelopmentTenantCatalogRepository
{
    public async Task<IReadOnlyList<DevelopmentTenantOptionDto>> ListAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Tenants
            .AsNoTracking()
            .OrderBy(tenant => tenant.Name)
            .ThenBy(tenant => tenant.Id)
            .Select(tenant => new DevelopmentTenantOptionDto(
                tenant.Id,
                tenant.Name,
                tenant.Status.ToString(),
                tenant.DataPosture.ToString(),
                tenant.Status == TenantStatus.Active ||
                    tenant.Status == TenantStatus.Trialing,
                tenant.Status == TenantStatus.Active ||
                    tenant.Status == TenantStatus.Trialing
                    ? null
                    : "The tenant is not operational."))
            .ToListAsync(cancellationToken);
}
