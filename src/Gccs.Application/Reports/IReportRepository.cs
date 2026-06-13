namespace Gccs.Application.Reports;

public interface IReportRepository
{
    Task<IReadOnlyList<ApprovedEvidencePackageDto>> ListApprovedEvidencePackagesAsync(
        CancellationToken cancellationToken = default);
}
