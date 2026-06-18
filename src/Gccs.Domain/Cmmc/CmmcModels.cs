using Gccs.Domain.Common;
using Gccs.Domain.Compliance;

namespace Gccs.Domain.Cmmc;

public sealed record Control(
    string Id,
    ControlFramework Framework,
    CmmcLevel CmmcLevel,
    string Family,
    string Title,
    string Requirement,
    string AssessmentObjective,
    IReadOnlyList<string> EvidenceExamples,
    ComplianceSource SourceReference);

public sealed record Assessment(
    Guid Id,
    Guid TenantId,
    AssessmentType Type,
    CmmcLevel Level,
    AssessmentStatus Status,
    DateOnly StartedAt,
    DateOnly? CompletedAt,
    DateOnly? AffirmationDueAt,
    IReadOnlyList<ControlAssessment> Controls,
    EntityAudit Audit);

public sealed record ControlAssessment(
    string ControlId,
    ControlImplementationStatus ImplementationStatus,
    AssessmentResult Result,
    string Notes,
    IReadOnlyList<Guid> EvidenceItemIds,
    Guid? AssessedByUserId,
    DateOnly? AssessedAt);

public sealed record PoamItem(
    Guid Id,
    Guid TenantId,
    Guid AssessmentId,
    string ControlId,
    string Weakness,
    string PlannedRemediation,
    RiskLevel RiskLevel,
    PoamStatus Status,
    Guid? OwnerUserId,
    string OwnerFunction,
    DateOnly TargetCompletionAt,
    DateOnly? CompletedAt,
    Guid? RemediationTaskId,
    IReadOnlyList<Guid> EvidenceItemIds,
    EntityAudit Audit);

public sealed record Asset(
    Guid Id,
    Guid TenantId,
    string Name,
    AssetType Type,
    string Description,
    string OwnerFunction,
    bool StoresFci,
    bool StoresCui,
    Guid? SystemBoundaryId,
    IReadOnlyList<string> Tags,
    EntityAudit Audit);

public sealed record SystemBoundary(
    Guid Id,
    Guid TenantId,
    string Name,
    string Description,
    BoundaryStatus Status,
    IReadOnlyList<Guid> AssetIds,
    IReadOnlyList<Guid> ExternalServiceProviderIds,
    IReadOnlyList<Guid> EvidenceItemIds,
    EntityAudit Audit);

public sealed record AnnualAffirmation(
    Guid Id,
    Guid TenantId,
    CmmcLevel Level,
    DateOnly DueAt,
    DateOnly? SubmittedAt,
    Guid? SubmittedByUserId,
    string? ConfirmationReference,
    IReadOnlyList<Guid> EvidenceItemIds,
    AffirmationStatus Status);

public enum ControlFramework
{
    FarBasicSafeguarding,
    NistSp800171Revision2,
    NistSp800171Revision3,
    NistSp800172,
    Cmmc
}

public enum CmmcLevel
{
    Level1,
    Level2,
    Level3
}

public enum AssessmentType
{
    SelfAssessment,
    Readiness,
    ThirdParty,
    GovernmentLed
}

public enum AssessmentStatus
{
    Planned,
    InProgress,
    Complete,
    Expired,
    Superseded
}

public enum ControlImplementationStatus
{
    NotStarted,
    PartiallyImplemented,
    Implemented,
    NotApplicable,
    NeedsReview
}

public enum AssessmentResult
{
    NotAssessed,
    Met,
    NotMet,
    NotApplicable
}

public enum ControlResponsibilityType
{
    Organization,
    MspEsp,
    CloudProvider,
    Subcontractor,
    Shared
}

public enum PoamStatus
{
    Open,
    InProgress,
    WaitingForValidation,
    Closed,
    AcceptedRisk
}

public enum AssetType
{
    Workstation,
    Server,
    CloudService,
    NetworkDevice,
    Application,
    MobileDevice,
    ExternalServiceProvider,
    Facility,
    Other
}

public enum BoundaryStatus
{
    Draft,
    Active,
    UnderReview,
    Retired
}

public enum AffirmationStatus
{
    NotDue,
    DueSoon,
    Submitted,
    Overdue,
    NotRequired
}
