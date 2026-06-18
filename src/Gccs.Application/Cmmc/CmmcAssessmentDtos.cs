using Gccs.Domain.Cmmc;

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
    IReadOnlyList<CmmcControlStatusHistoryDto> StatusHistory);

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
    string? EspName = null);

public sealed record CmmcControlStatusHistoryDto(
    Guid Id,
    ControlImplementationStatus Status,
    AssessmentResult Result,
    Guid ChangedByUserId,
    DateTimeOffset ChangedAt,
    string? Notes);

public interface ICmmcAssessmentRepository
{
    Task<IReadOnlyList<CmmcAssessmentDto>> ListCurrentTenantAsync(CancellationToken cancellationToken = default);

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
}
