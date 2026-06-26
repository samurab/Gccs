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
    IReadOnlyList<RecentAuditEventDto> RecentAuditEvents)
{
    public string ProductPromise { get; init; } =
        "Help small government contractors know what applies, prove what they did, and stay ready for audits, renewals, bids, and certifications.";

    public string MvpDataPosture { get; init; } = "No-CUI / compliance management only";

    public IReadOnlyList<ModuleStatusDto> Modules { get; init; } =
    [
        new("company-profile", "Company compliance profile", "Capture UEI, CAGE, SAM, NAICS, certifications, roles, and data posture.", "planned"),
        new("contract-intake", "Contract and clause intake", "Collect solicitations, contracts, flow-downs, wage determinations, and CUI guides.", "planned"),
        new("obligations", "Obligation dashboard", "Map clauses to required actions, owners, evidence, deadlines, and source links.", "seeded"),
        new("calendar", "Compliance calendar", "Track renewals, reports, training, affirmations, deliverables, and policy reviews.", "planned"),
        new("evidence-vault", "Evidence vault", "Tag evidence by obligation, contract, control, vendor, employee, and expiration date.", "planned"),
        new("cmmc", "CMMC readiness tracker", "Track Level 1 and Level 2 controls, evidence, SSP, POA&M, assets, and affirmations.", "planned"),
        new("subcontractors", "Subcontractor flow-down tracker", "Track flow-down clauses, CMMC status, insurance, NDAs, CUI access, and workshare.", "planned"),
        new("reports", "Basic reports", "Generate obligation matrices, readiness reports, evidence packages, and risk dashboards.", "planned")
    ];

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
