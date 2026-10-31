using Gccs.Domain.Common;

namespace Gccs.Domain.Compliance;

public sealed record ComplianceTask(
    Guid Id,
    Guid TenantId,
    string Title,
    string Description,
    ComplianceTaskType Type,
    ComplianceTaskStatus Status,
    RiskLevel RiskLevel,
    Guid? AssignedToUserId,
    string OwnerFunction,
    DateOnly? DueAt,
    Guid? ContractId,
    string? ObligationId,
    string? ControlId,
    Guid? EvidenceItemId,
    EntityAudit Audit);

public enum ComplianceTaskType
{
    ObligationAction,
    CalendarReminder,
    EvidenceRequest,
    ControlAssessment,
    PolicyReview,
    Renewal,
    Report,
    CorrectiveAction
}

public enum ComplianceTaskStatus
{
    Open,
    InProgress,
    Blocked,
    WaitingForReview,
    Done,
    Canceled
}
