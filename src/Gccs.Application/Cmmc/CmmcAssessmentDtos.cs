using Gccs.Domain.Cmmc;
using Gccs.Domain.Compliance;

namespace Gccs.Application.Cmmc;

public sealed record CmmcAssessmentDto(
    Guid Id,
    Guid TenantId,
    string Name,
    AssessmentType Type,
    CmmcLevel Level,
    string Framework,
    AssessmentStatus Status,
    DateOnly StartedAt,
    DateOnly? CompletedAt,
    DateOnly? AffirmationDueAt,
    string OwnerFunction,
    Guid? CompanyProfileId,
    IReadOnlyList<Guid> ContractIds,
    ControlSummaryDto ControlSummary,
    int OpenPoamItemCount,
    int OverduePoamItemCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record UpsertCmmcAssessmentRequest(
    string Name,
    AssessmentType Type,
    CmmcLevel Level,
    string Framework,
    AssessmentStatus Status,
    DateOnly StartedAt,
    DateOnly? CompletedAt,
    DateOnly? AffirmationDueAt,
    string OwnerFunction,
    Guid? CompanyProfileId,
    IReadOnlyList<Guid> ContractIds);

public sealed record ControlSummaryDto(
    int Total,
    int Implemented,
    int PartiallyImplemented,
    int NotStarted,
    int NotApplicable,
    int NeedsReview,
    int CompletionPercentage);

public sealed record CmmcControlStatusDto(
    Guid AssessmentId,
    string ControlId,
    string Title,
    string Family,
    string Requirement,
    string AssessmentObjective,
    string SourceName,
    string SourceUrl,
    DateOnly SourceLastReviewedAt,
    string SourceConfidence,
    ControlImplementationStatus Status,
    AssessmentResult Result,
    IReadOnlyList<Guid> EvidenceItemIds,
    IReadOnlyList<Guid> TaskIds,
    IReadOnlyList<Guid> AssetIds,
    IReadOnlyList<Guid> PoamItemIds,
    Guid? AssessedByUserId,
    DateOnly? AssessedAt,
    string Notes,
    string ImplementationDetails,
    bool IsInherited,
    string? InheritedFrom,
    bool EspResponsible,
    string? EspName,
    ControlResponsibilityType ResponsibilityType,
    string OwnerFunction,
    string? ResponsibilityProvider,
    string ResponsibilityNotes,
    IReadOnlyList<CmmcControlStatusHistoryDto> StatusHistory);

public sealed record CmmcControlLibraryDto(
    string ControlId,
    string Title,
    string Family,
    CmmcLevel CmmcLevel,
    string Requirement,
    string AssessmentObjective,
    string SourceName,
    string SourceUrl,
    DateOnly SourceLastReviewedAt,
    string SourceConfidence);

public sealed record UpsertCmmcControlStatusRequest(
    ControlImplementationStatus Status,
    AssessmentResult Result,
    IReadOnlyList<Guid> EvidenceItemIds,
    IReadOnlyList<Guid> TaskIds,
    IReadOnlyList<Guid> AssetIds,
    IReadOnlyList<Guid> PoamItemIds,
    Guid? AssessedByUserId,
    DateOnly? AssessedAt,
    string? Notes,
    string? ImplementationDetails = null,
    bool IsInherited = false,
    string? InheritedFrom = null,
    bool EspResponsible = false,
    string? EspName = null,
    ControlResponsibilityType ResponsibilityType = ControlResponsibilityType.Organization,
    string? OwnerFunction = null,
    string? ResponsibilityProvider = null,
    string? ResponsibilityNotes = null);

public sealed record CmmcControlStatusHistoryDto(
    Guid Id,
    ControlImplementationStatus Status,
    AssessmentResult Result,
    Guid ChangedByUserId,
    DateTimeOffset ChangedAt,
    string? Notes);

public sealed record CmmcResponsibilityMatrixRowDto(
    Guid AssessmentId,
    string ControlId,
    string Title,
    string Family,
    ControlResponsibilityType ResponsibilityType,
    string OwnerFunction,
    string? Provider,
    string EvidenceStatus,
    string Notes);

public sealed record CmmcReadinessGapDto(
    Guid AssessmentId,
    string ControlId,
    string Title,
    string Family,
    CmmcGapPriority Priority,
    IReadOnlyList<string> ReasonCodes,
    ControlImplementationStatus ControlStatus,
    AssessmentResult AssessmentResult,
    string EvidenceStatus,
    RiskLevel? PoamRiskLevel,
    DateOnly? PoamTargetCompletionAt,
    bool IsPoamOverdue,
    bool IsCuiRelevant,
    bool IsInherited,
    bool HasAssessmentObjectiveCoverage);

public sealed record CreatePoamFromGapRequest(
    string OwnerFunction,
    DateOnly TargetCompletionAt,
    Guid? OwnerUserId = null);

public interface ICmmcAssessmentRepository
{
    Task<IReadOnlyList<CmmcAssessmentDto>> ListCurrentTenantAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CmmcControlLibraryDto>> ListControlLibraryAsync(CancellationToken cancellationToken = default);

    Task<CmmcAssessmentDto?> FindCurrentTenantAsync(Guid assessmentId, CancellationToken cancellationToken = default);

    Task<CmmcAssessmentDto> CreateCurrentTenantAsync(
        UpsertCmmcAssessmentRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<CmmcAssessmentDto?> UpdateCurrentTenantAsync(
        Guid assessmentId,
        UpsertCmmcAssessmentRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CmmcControlStatusDto>?> ListControlStatusesAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default);

    Task<CmmcControlStatusDto?> UpsertControlStatusAsync(
        Guid assessmentId,
        string controlId,
        UpsertCmmcControlStatusRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CmmcResponsibilityMatrixRowDto>?> GetResponsibilityMatrixAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default);

    Task<string?> ExportResponsibilityMatrixCsvAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CmmcReadinessGapDto>?> GetReadinessGapsAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default);
}
