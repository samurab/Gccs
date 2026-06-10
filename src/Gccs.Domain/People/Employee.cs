using Gccs.Domain.Common;

namespace Gccs.Domain.People;

public sealed record Employee(
    Guid Id,
    Guid TenantId,
    string EmployeeNumber,
    string Name,
    string Email,
    EmploymentStatus Status,
    string JobTitle,
    string LaborCategory,
    bool HandlesFci,
    bool HandlesCui,
    IReadOnlyList<Guid> TrainingRecordIds,
    EntityAudit Audit);

public sealed record TrainingRecord(
    Guid Id,
    Guid TenantId,
    Guid EmployeeId,
    string TrainingName,
    TrainingType Type,
    TrainingStatus Status,
    DateOnly AssignedAt,
    DateOnly? CompletedAt,
    DateOnly? ExpiresAt,
    Guid? EvidenceItemId,
    EntityAudit Audit);

public enum EmploymentStatus
{
    Active,
    Leave,
    Terminated,
    Contractor
}

public enum TrainingType
{
    SecurityAwareness,
    CuiHandling,
    IncidentResponse,
    Ethics,
    LaborCompliance,
    RoleSpecific,
    Other
}

public enum TrainingStatus
{
    Assigned,
    Complete,
    Overdue,
    Expired,
    Waived
}
