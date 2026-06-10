using Gccs.Domain.Common;

namespace Gccs.Domain.Reports;

public sealed record Report(
    Guid Id,
    Guid TenantId,
    ReportType Type,
    string Title,
    ReportStatus Status,
    DateTimeOffset GeneratedAt,
    Guid GeneratedByUserId,
    Uri? StorageUri,
    IReadOnlyList<Guid> ContractIds,
    IReadOnlyList<string> ObligationIds,
    IReadOnlyList<Guid> EvidenceItemIds,
    EntityAudit Audit);

public enum ReportType
{
    ComplianceStatus,
    ContractObligationMatrix,
    CmmcReadiness,
    PrimeEvidencePackage,
    SamSbaProfile,
    SubcontractorCompliance,
    AuditTrail,
    ExecutiveRiskDashboard
}

public enum ReportStatus
{
    Queued,
    Generating,
    Complete,
    Failed,
    Archived
}
