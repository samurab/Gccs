namespace Gccs.Application.Compliance;

public sealed record ComplianceOverviewDto(
    string ProductPromise,
    string MvpDataPosture,
    IReadOnlyList<ModuleStatusDto> Modules,
    IReadOnlyList<ObligationSummaryDto> PriorityObligations);

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
