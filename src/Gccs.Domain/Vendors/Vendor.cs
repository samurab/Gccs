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
    string WorkshareDescription,
    decimal? WorksharePercentage,
    bool HasFciAccess,
    bool HasCuiAccess,
    string? RequiredCmmcLevel,
    IReadOnlyList<FlowDownClause> FlowDownClauses,
    IReadOnlyList<Guid> ContractIds,
    IReadOnlyList<Guid> EvidenceItemIds,
    PointOfContact? Contact,
    EntityAudit Audit);

public sealed record FlowDownClause(
    Guid Id,
    string ClauseNumber,
    string Title,
    FlowDownStatus Status,
    DateOnly? SentAt,
    DateOnly? SignedAt,
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
    Signed,
    Expired,
    Waived
}
