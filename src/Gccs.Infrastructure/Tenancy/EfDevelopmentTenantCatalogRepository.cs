using Gccs.Application.Tenancy;
using Gccs.Domain.Identity;
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

    public async Task<IReadOnlyList<DevelopmentPersonaOptionDto>> ListPersonasAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.TenantMemberships
            .AsNoTracking()
            .Where(membership =>
                membership.Status == MembershipStatus.Active &&
                membership.User != null &&
                membership.User.Status == UserStatus.Active)
            .OrderBy(membership => membership.TenantId)
            .ThenBy(membership => membership.RoleName)
            .ThenBy(membership => membership.User!.DisplayName)
            .Select(membership => new DevelopmentPersonaOptionDto(
                membership.TenantId,
                membership.UserId,
                membership.User!.Email,
                string.IsNullOrWhiteSpace(membership.User.DisplayName)
                    ? membership.User.Email
                    : membership.User.DisplayName,
                membership.RoleName))
            .ToListAsync(cancellationToken);
}
