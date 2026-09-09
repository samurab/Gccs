namespace Gccs.Infrastructure.Persistence.Models;

public abstract class ReadinessRecordEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public int Version { get; set; }
    public string State { get; set; } = "Draft";
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string? ApprovalNotes { get; set; }
}
public sealed class SecurityReviewRecordEntity : ReadinessRecordEntity
{
    public ICollection<SecurityReviewChecklistItemRecordEntity> Items { get; set; } = [];
    public ICollection<SecurityReviewFindingRecordEntity> Findings { get; set; } = [];
    public ICollection<AcceptedSecurityRiskRecordEntity> AcceptedRisks { get; set; } = [];
}
public sealed class SecurityReviewChecklistItemRecordEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ReviewId { get; set; }
    public string Area { get; set; } = ""; public int Status { get; set; }
    public Guid? ReviewerUserId { get; set; }
    public DateOnly? ReviewedAt { get; set; }
    public string? EvidenceLink { get; set; }
    public string? Rationale { get; set; }
    public SecurityReviewRecordEntity? Review { get; set; }
}
public sealed class SecurityReviewFindingRecordEntity
{
    public Guid Id { get; set; }
    public Guid LogicalId { get; set; }
    public Guid TenantId { get; set; }
    public Guid ReviewId { get; set; }
    public string Area { get; set; } = ""; public int Severity { get; set; }
    public int Status { get; set; }
    public string Summary { get; set; } = ""; public string RemediationOwner { get; set; } = ""; public DateOnly? DueAt { get; set; }
    public string? ClosureNotes { get; set; }
    public SecurityReviewRecordEntity? Review { get; set; }
}
public sealed class AcceptedSecurityRiskRecordEntity
{
    public Guid Id { get; set; }
    public Guid LogicalId { get; set; }
    public Guid TenantId { get; set; }
    public Guid ReviewId { get; set; }
    public Guid? FindingId { get; set; }
    public Guid ApproverUserId { get; set; }
    public DateOnly AcceptedAt { get; set; }
    public string Scope { get; set; } = "";
    public DateOnly? ExpiresAt { get; set; }
    public DateOnly? ReviewAt { get; set; }
    public string MitigationNote { get; set; } = "";
    public SecurityReviewRecordEntity? Review { get; set; }
    public SecurityReviewFindingRecordEntity? Finding { get; set; }
}
public sealed class TechnicalReadinessRecordEntity : ReadinessRecordEntity
{
    public ICollection<ExecutedControlEvidenceEntity> Evidence { get; set; } = [];
}
public sealed class ExecutedControlEvidenceEntity
{
    public Guid Id { get; set; }
    public Guid LogicalId { get; set; }
    public Guid TenantId { get; set; }
    public Guid ReadinessId { get; set; }
    public string ControlType { get; set; } = ""; public DateOnly ExecutedAt { get; set; }
    public string Environment { get; set; } = "";
    public Guid ReviewerUserId { get; set; }
    public string Result { get; set; } = ""; public string EvidenceReference { get; set; } = "";
    public DateOnly? ExpiresAt { get; set; }
    public string Notes { get; set; } = ""; public TechnicalReadinessRecordEntity? Readiness { get; set; }
}
public sealed class IncidentReadinessRecordEntity : ReadinessRecordEntity
{
    public DateOnly ReviewDueAt { get; set; }
    public string ReviewBasis { get; set; } = "Annual";
    public ICollection<IncidentContactRecordEntity> Contacts { get; set; } = [];
    public ICollection<IncidentPlaybookRecordEntity> Playbooks { get; set; } = [];
    public ICollection<IncidentTabletopRecordEntity> Tabletops { get; set; } = [];
    public ICollection<IncidentFollowUpRecordEntity> FollowUps { get; set; } = [];
}
public sealed class IncidentContactRecordEntity
{
    public Guid Id { get; set; }
    public Guid LogicalId { get; set; }
    public Guid TenantId { get; set; }
    public Guid ReadinessId { get; set; }
    public string Function { get; set; } = ""; public string Contact { get; set; } = ""; public string EscalationRole { get; set; } = "";
    public IncidentReadinessRecordEntity? Readiness { get; set; }
}
public sealed class IncidentPlaybookRecordEntity
{
    public Guid Id { get; set; }
    public Guid LogicalId { get; set; }
    public Guid TenantId { get; set; }
    public Guid ReadinessId { get; set; }
    public string Key { get; set; } = "";
    public string Trigger { get; set; } = ""; public string ContainmentStepsJson { get; set; } = "[]"; public string NotificationPath { get; set; } = "";
    public string EvidenceToCollectJson { get; set; } = "[]"; public string Owner { get; set; } = ""; public string ClosureCriteria { get; set; } = "";
    public IncidentReadinessRecordEntity? Readiness { get; set; }
}
public sealed class IncidentTabletopRecordEntity
{
    public Guid Id { get; set; }
    public Guid LogicalId { get; set; }
    public Guid TenantId { get; set; }
    public Guid ReadinessId { get; set; }
    public DateOnly ExecutedAt { get; set; }
    public string Environment { get; set; } = ""; public string ParticipantsJson { get; set; } = "[]"; public string FindingsJson { get; set; } = "[]";
    public string EvidenceReference { get; set; } = ""; public Guid ReviewerUserId { get; set; }
    public IncidentReadinessRecordEntity? Readiness { get; set; }
}
public sealed class IncidentFollowUpRecordEntity
{
    public Guid Id { get; set; }
    public Guid LogicalId { get; set; }
    public Guid TenantId { get; set; }
    public Guid ReadinessId { get; set; }
    public Guid TabletopId { get; set; }
    public int Severity { get; set; }
    public int Status { get; set; }
    public string Summary { get; set; } = ""; public string Owner { get; set; } = "";
    public DateOnly DueAt { get; set; }
    public string? ClosureNotes { get; set; }
    public IncidentReadinessRecordEntity? Readiness { get; set; }
    public IncidentTabletopRecordEntity? Tabletop { get; set; }
}
public sealed class ReadinessApprovalEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string RecordType { get; set; } = ""; public Guid RecordId { get; set; }
    public int Version { get; set; }
    public Guid ApprovedByUserId { get; set; }
    public DateTimeOffset ApprovedAt { get; set; }
    public string Notes { get; set; } = "";
}
public sealed class ReadinessHistoryEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string RecordType { get; set; } = ""; public Guid RecordId { get; set; }
    public int Version { get; set; }
    public string Action { get; set; } = ""; public Guid ActorUserId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string Summary { get; set; } = "";
}
