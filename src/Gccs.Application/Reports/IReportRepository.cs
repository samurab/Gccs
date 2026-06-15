using Gccs.Domain.Reports;

namespace Gccs.Application.Reports;

public interface IReportRepository
{
    Task<IReadOnlyList<ApprovedEvidencePackageDto>> ListApprovedEvidencePackagesAsync(
        CancellationToken cancellationToken = default);

    Task<ComplianceStatusReportDto> GenerateComplianceStatusReportAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed record ComplianceStatusReportDto(
    Guid Id,
    Guid TenantId,
    ReportType Type,
    ReportStatus Status,
    string Title,
    DateTimeOffset GeneratedAt,
    Guid GeneratedByUserId,
    ComplianceStatusReportSnapshotDto Snapshot,
    string ExportHtml);

public sealed record ComplianceStatusReportSnapshotDto(
    DateTimeOffset GeneratedAt,
    int TotalObligations,
    int HighRiskObligations,
    int OverdueTasks,
    IReadOnlyDictionary<string, int> ObligationStatusCounts,
    IReadOnlyDictionary<string, int> EvidenceStatusCounts,
    int CmmcAssessments,
    int CmmcControlsImplemented,
    int CmmcControlsTotal,
    int SubcontractorGaps,
    IReadOnlyList<string> HighRiskItems);
