using Gccs.Domain.Cmmc;
using Gccs.Domain.Compliance;

namespace Gccs.Application.Cmmc;

public sealed record CmmcPoamItemDto(
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
    bool IsOverdue,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    Guid? CreatedByUserId,
    Guid? UpdatedByUserId)
{
    public string Title => Weakness;

    public string Description => Weakness;

    public RiskLevel Severity => RiskLevel;

    public DateOnly DueDate => TargetCompletionAt;

    public string RemediationPlan => PlannedRemediation;

    public Guid? CreatedBy => CreatedByUserId;

    public DateTimeOffset CreatedUtc => CreatedAt;

    public Guid? UpdatedBy => UpdatedByUserId;

    public DateTimeOffset? UpdatedUtc => UpdatedAt;
}

public sealed record UpsertCmmcPoamItemRequest(
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
    IReadOnlyList<Guid> EvidenceItemIds);

public interface ICmmcPoamRepository
{
    Task<IReadOnlyList<CmmcPoamItemDto>> ListCurrentTenantAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CmmcPoamItemDto>?> ListCurrentTenantAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default);

    Task<CmmcPoamItemDto?> FindCurrentTenantAsync(
        Guid poamItemId,
        CancellationToken cancellationToken = default);

    Task<CmmcPoamItemDto?> CreateAsync(
        Guid assessmentId,
        UpsertCmmcPoamItemRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<CmmcPoamItemDto?> UpdateAsync(
        Guid assessmentId,
        Guid poamItemId,
        UpsertCmmcPoamItemRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<CmmcPoamItemDto?> UpdateCurrentTenantAsync(
        Guid poamItemId,
        UpsertCmmcPoamItemRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<CmmcPoamItemDto?> CloseCurrentTenantAsync(
        Guid poamItemId,
        Guid actorUserId,
        DateOnly closedAt,
        CancellationToken cancellationToken = default);
}
