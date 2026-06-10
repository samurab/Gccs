using Gccs.Domain.Common;

namespace Gccs.Domain.Evidence;

public sealed record EvidenceItem(
    Guid Id,
    Guid TenantId,
    string Name,
    string Description,
    EvidenceType Type,
    EvidenceStatus Status,
    Uri? StorageUri,
    string? FileHash,
    DateOnly? EffectiveAt,
    DateOnly? ExpiresAt,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> ObligationIds,
    IReadOnlyList<Guid> ContractIds,
    IReadOnlyList<string> ControlIds,
    IReadOnlyList<Guid> VendorIds,
    IReadOnlyList<Guid> EmployeeIds,
    Guid? ApprovedByUserId,
    DateTimeOffset? ApprovedAt,
    EntityAudit Audit);

public enum EvidenceType
{
    Policy,
    TrainingRecord,
    Screenshot,
    SystemConfiguration,
    VendorAttestation,
    SubcontractorCertification,
    SignedFlowDown,
    PayrollRecord,
    IncidentRecord,
    AccessReview,
    RiskAssessment,
    MeetingNote,
    CorrectiveActionPlan,
    Other
}

public enum EvidenceStatus
{
    Requested,
    Uploaded,
    InReview,
    Approved,
    Rejected,
    Expired,
    Archived
}
