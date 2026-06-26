using Gccs.Application.Compliance;
using Gccs.Application.Security;

namespace Gccs.Infrastructure.Compliance;

public sealed class EmptyComplianceOverviewRepository(ICurrentTenantContext tenantContext) : IComplianceOverviewRepository
{
    public Task<ComplianceOverviewDto> GetCurrentTenantOverviewAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new ComplianceOverviewDto(
            tenantContext.TenantId,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            []));
}
