using Gccs.Application.Security;

namespace Gccs.Application.Compliance;

public sealed class ComplianceOverviewService(
    IComplianceOverviewRepository repository,
    ICurrentTenantContext tenantContext)
{
    public async Task<ComplianceOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var overview = await repository.GetCurrentTenantOverviewAsync(cancellationToken);
        return overview with { TenantId = tenantContext.TenantId };
    }
}

public interface IComplianceOverviewRepository
{
    Task<ComplianceOverviewDto> GetCurrentTenantOverviewAsync(CancellationToken cancellationToken = default);
}
