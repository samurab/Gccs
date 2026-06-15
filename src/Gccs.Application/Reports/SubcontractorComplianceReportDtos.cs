using Gccs.Domain.Reports;
using Gccs.Domain.Vendors;

namespace Gccs.Application.Reports;

public sealed record SubcontractorComplianceReportDto(
    Guid Id,
    Guid TenantId,
    ReportType Type,
    ReportStatus Status,
    string Title,
    DateTimeOffset GeneratedAt,
    Guid GeneratedByUserId,
    SubcontractorComplianceSnapshotDto Snapshot,
    string ExportCsv);

public sealed record SubcontractorComplianceSnapshotDto(
    Guid? ContractId,
    DateTimeOffset GeneratedAt,
    int TotalSubcontractors,
    int MissingEvidenceRequests,
    int OverdueEvidenceRequests,
    int OpenFlowDowns,
    IReadOnlyList<SubcontractorComplianceRowDto> Rows);

public sealed record SubcontractorComplianceRowDto(
    Guid SubcontractorId,
    string Name,
    SubcontractorStatus Status,
    string CmmcStatus,
    DateOnly? InsuranceExpiresAt,
    string NdaStatus,
    IReadOnlyList<SubcontractorReportFlowDownDto> FlowDowns,
    IReadOnlyList<SubcontractorReportEvidenceRequestDto> EvidenceRequests,
    bool HasMissingEvidence,
    bool HasOverdueEvidence);

public sealed record SubcontractorReportFlowDownDto(
    Guid Id,
    Guid? ContractId,
    string ClauseNumber,
    string Title,
    FlowDownStatus Status);

public sealed record SubcontractorReportEvidenceRequestDto(
    Guid Id,
    string RequestedItem,
    DateOnly DueDate,
    SubcontractorEvidenceRequestStatus Status,
    Guid? RelatedFlowDownClauseId,
    bool IsMissing,
    bool IsOverdue);
