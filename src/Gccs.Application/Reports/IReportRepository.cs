using Gccs.Domain.Reports;

namespace Gccs.Application.Reports;

public interface IReportRepository
{
    Task<IReadOnlyList<ReportHistoryItemDto>> ListRecentReportsAsync(
        int limit,
        CancellationToken cancellationToken = default);

    Task<ReportArtifactDetailDto?> GetReportArtifactAsync(
        Guid reportId,
        CancellationToken cancellationToken = default);

    Task<ReportLifecycleTransitionDto?> SetArchiveStateAsync(
        Guid reportId,
        bool archived,
        Guid actorUserId,
        string reason,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApprovedEvidencePackageDto>> ListApprovedEvidencePackagesAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<ComplianceStatusReportDto> GenerateComplianceStatusReportAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default, Gccs.Application.Common.ContentClassificationRequest? classification = null);

    Task<CmmcReadinessReportDto?> GenerateCmmcReadinessReportAsync(
        Guid assessmentId,
        Guid actorUserId,
        bool includeEvidenceLinks,
        CancellationToken cancellationToken = default, Gccs.Application.Common.ContentClassificationRequest? classification = null);

    Task<EvidencePackageReportDto> GenerateEvidencePackageAsync(
        EvidencePackageGenerateRequest request,
        Guid actorUserId,
        bool includeDraftOrRejectedEvidence,
        CancellationToken cancellationToken = default, Gccs.Application.Common.ContentClassificationRequest? classification = null);

    Task<EvidencePackageReportDto?> GetEvidencePackageAsync(
        Guid reportId,
        CancellationToken cancellationToken = default);

    Task<SubcontractorComplianceReportDto> GenerateSubcontractorComplianceReportAsync(
        Guid? contractId,
        Guid actorUserId,
        CancellationToken cancellationToken = default, Gccs.Application.Common.ContentClassificationRequest? classification = null);
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
    string ExportHtml)
{
    public Gccs.Application.Common.ContentClassificationDto? Classification { get; init; }
    public string Disclaimer => ReportArtifactLanguage.WorkflowGuidanceDisclaimer;
}

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
