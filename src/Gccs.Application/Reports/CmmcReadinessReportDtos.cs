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
    string TenantName,
    CmmcLevel TargetLevel,
    string ControlVersion,
    DateTimeOffset GeneratedAt,
    Guid ReviewerUserId,
    IReadOnlyList<CmmcFamilyProgressDto> ProgressByFamily,
    IReadOnlyList<CmmcReportControlStatusDto> ControlStatuses,
    IReadOnlyList<CmmcControlGapDto> OpenGaps,
    IReadOnlyList<CmmcReportGapPriorityDto> PrioritizedGaps,
    IReadOnlyList<CmmcReportPoamItemDto> OpenPoamItems,
    IReadOnlyList<CmmcReportEvidenceLinkDto> EvidenceLinks,
    IReadOnlyList<CmmcReportResponsibilityRowDto> ResponsibilityMatrix,
    IReadOnlyList<CmmcReportSourceReferenceDto> SourceReferences,
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

public sealed record CmmcReportControlStatusDto(
    string ControlId,
    string Family,
    string Title,
    ControlImplementationStatus Status,
    AssessmentResult Result,
    string EvidenceStatus,
    string SourceName,
    string SourceUrl,
    DateOnly SourceLastReviewedAt);

public sealed record CmmcReportGapPriorityDto(
    string ControlId,
    string Family,
    string Title,
    CmmcGapPriority Priority,
    IReadOnlyList<string> ReasonCodes,
    string EvidenceStatus);

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

public sealed record CmmcReportResponsibilityRowDto(
    string ControlId,
    string Family,
    ControlResponsibilityType ResponsibilityType,
    string OwnerFunction,
    string? Provider,
    string Notes);

public sealed record CmmcReportSourceReferenceDto(
    string ControlId,
    string SourceName,
    string SourceUrl,
    DateOnly LastReviewedAt,
    string Confidence);

public sealed record CmmcReportAffirmationDto(
    Guid Id,
    CmmcLevel Level,
    DateOnly DueAt,
    DateOnly? SubmittedAt,
    AffirmationStatus Status);
