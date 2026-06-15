using Gccs.Domain.Common;

namespace Gccs.Domain.Vendors;

public sealed record Vendor(
    Guid Id,
    Guid TenantId,
    string Name,
    VendorType Type,
    VendorRiskLevel RiskLevel,
    PointOfContact? Contact,
    bool HasFciAccess,
    bool HasCuiAccess,
    IReadOnlyList<Guid> EvidenceItemIds,
    EntityAudit Audit);

public sealed record Subcontractor(
    Guid Id,
    Guid TenantId,
    string Name,
    string? Uei,
    string? CageCode,
    SubcontractorStatus Status,
    string RoleDescription,
    string SmallBusinessStatus,
    string CmmcStatus,
    DateOnly? InsuranceExpiresAt,
    string NdaStatus,
    string WorkshareDescription,
    decimal? WorksharePercentage,
    bool HasFciAccess,
    bool HasCuiAccess,
    bool HasExportControlledAccess,
    string? RequiredCmmcLevel,
    IReadOnlyList<FlowDownClause> FlowDownClauses,
    IReadOnlyList<Guid> ContractIds,
    IReadOnlyList<Guid> EvidenceItemIds,
    PointOfContact? Contact,
    EntityAudit Audit);

public sealed record FlowDownClause(
    Guid Id,
    Guid? ContractId,
    Guid? ContractClauseId,
    string? ObligationId,
    string ClauseNumber,
    string Title,
    FlowDownStatus Status,
    DateOnly? SentAt,
    DateOnly? AcknowledgedAt,
    DateOnly? SignedAt,
    DateOnly? WaivedAt,
    Guid? SignedEvidenceItemId);

public enum VendorType
{
    Supplier,
    SoftwareProvider,
    ExternalServiceProvider,
    Msp,
    Consultant,
    Subcontractor,
    Other
}

public enum VendorRiskLevel
{
    Low,
    Medium,
    High,
    Critical,
    Unknown
}

public enum SubcontractorStatus
{
    Prospective,
    Active,
    Suspended,
    Completed,
    Archived
}

public enum FlowDownStatus
{
    NotRequired,
    Required,
    Sent,
    Acknowledged,
    Signed,
    Expired,
    Waived,
    NotApplicable
}

public enum SubcontractorEvidenceRequestStatus
{
    Draft,
    Sent,
    Submitted,
    Satisfied,
    Overdue,
    Cancelled
}
