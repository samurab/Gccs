namespace Gccs.Application.Compliance;

public sealed record ComplianceOverviewDto(
    Guid TenantId,
    int ControlsTotal,
    int ControlsImplemented,
    int ControlsInProgress,
    int ControlsNotStarted,
    int OpenPoams,
    int OverduePoams,
    int EvidenceItems,
    ReadinessScoreDto ReadinessScore,
    ContractRiskIndicatorDto ContractRiskIndicator,
    IReadOnlyList<RecentAuditEventDto> RecentAuditEvents,
    IReadOnlyList<ModuleStatusDto> Modules)
{
    public string ProductPromise { get; init; } =
        "Help small government contractors know what applies, prove what they did, and stay ready for audits, renewals, bids, and certifications.";

    public string MvpDataPosture { get; init; } = "No-CUI / compliance management only";

    public IReadOnlyList<ObligationSummaryDto> PriorityObligations { get; init; } = [];

    public IReadOnlyList<ComplianceDashboardAlertDto> Alerts { get; init; } = [];
}

public sealed record ComplianceDashboardAlertDto(
    string AlertType,
    string Severity,
    string Title,
    string Message,
    string EntityType,
    string EntityId,
    DateTimeOffset DetectedUtc);

public sealed record ReadinessScoreDto(
    int? Score,
    int ControlsTotal,
    int ControlsApplicable,
    int ControlsImplemented,
    int ControlsNotApplicable,
    string Status);

public sealed record ContractRiskIndicatorDto(
    string Level,
    int ActiveContracts,
    int HighRiskObligations,
    int OverduePoams,
    int OpenPoams,
    int MissingEvidenceControls,
    int OpenHighRiskTasks,
    int OverdueHighRiskTasks);

public sealed record RecentAuditEventDto(
    Guid Id,
    Guid? ActorUserId,
    string Action,
    string EntityType,
    string EntityId,
    DateTimeOffset OccurredAt,
    string CorrelationId,
    string Summary);

public sealed record ModuleStatusDto(
    string Key,
    string Name,
    string Purpose,
    string Status);

public sealed record ObligationSummaryDto(
    string Id,
    string Source,
    string Title,
    string OwnerFunction,
    string RiskLevel,
    string SourceUrl,
    DateOnly LastReviewedAt);
