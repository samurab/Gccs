using Gccs.Domain.Cmmc;
using Gccs.Domain.Compliance;
using Gccs.Domain.Reports;

namespace Gccs.Application.Reports;

public sealed record CmmcReadinessReportDto(
    Guid Id,
    Guid TenantId,
    ReportType Type,
    ReportStatus Status,
    string Title,
    DateTimeOffset GeneratedAt,
    Guid GeneratedByUserId,
    CmmcReadinessSnapshotDto Snapshot,
    string ExportHtml);

public sealed record CmmcReadinessSnapshotDto(
    Guid AssessmentId,
    string AssessmentName,
    CmmcLevel TargetLevel,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<CmmcFamilyProgressDto> ProgressByFamily,
    IReadOnlyList<CmmcControlGapDto> OpenGaps,
    IReadOnlyList<CmmcReportPoamItemDto> OpenPoamItems,
    IReadOnlyList<CmmcReportEvidenceLinkDto> EvidenceLinks,
    IReadOnlyList<CmmcReportAffirmationDto> Affirmations,
    int SnapshotHistoryCount);

public sealed record CmmcFamilyProgressDto(
    string Family,
    int Total,
    int Implemented,
    int Partial,
    int NotStarted,
    int NeedsReview,
    int NotApplicable);

public sealed record CmmcControlGapDto(
    string ControlId,
    string Family,
    string Title,
    ControlImplementationStatus Status,
    AssessmentResult Result);

public sealed record CmmcReportPoamItemDto(
    Guid Id,
    string ControlId,
    string Weakness,
    RiskLevel RiskLevel,
    PoamStatus Status,
    DateOnly TargetCompletionAt);

public sealed record CmmcReportEvidenceLinkDto(
    Guid EvidenceItemId,
    string Name,
    string ControlId);

public sealed record CmmcReportAffirmationDto(
    Guid Id,
    CmmcLevel Level,
    DateOnly DueAt,
    DateOnly? SubmittedAt,
    AffirmationStatus Status);
