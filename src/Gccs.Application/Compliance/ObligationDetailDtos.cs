using Gccs.Domain.Compliance;
using Gccs.Domain.Evidence;

namespace Gccs.Application.Compliance;

public sealed record ContractObligationDetailDto(
    string Id,
    Guid ContractId,
    string ContractNumber,
    string ContractTitle,
    Guid ContractClauseId,
    string ClauseNumber,
    string ClauseTitle,
    string ObligationId,
    string Source,
    string SourceUrl,
    string Title,
    string PlainEnglishSummary,
    string TriggerCondition,
    string RequiredAction,
    string OwnerFunction,
    Guid? AssignedUserId,
    string? AssignedUserDisplayName,
    string? AssignedRoleName,
    RiskLevel RiskLevel,
    string Status,
    DateOnly? DueAt,
    string Module,
    bool FlowDownRequired,
    string FlowDownRequirement,
    IReadOnlyList<string> EvidenceExamples,
    string Confidence,
    DateOnly LastReviewedAt,
    bool RequiresExpertReview,
    IReadOnlyList<LinkedObligationTaskDto> LinkedTasks,
    IReadOnlyList<LinkedObligationEvidenceDto> LinkedEvidence);

public sealed record LinkedObligationTaskDto(
    Guid Id,
    string Title,
    string Status,
    DateOnly? DueAt,
    string OwnerFunction,
    RiskLevel RiskLevel);

public sealed record LinkedObligationEvidenceDto(
    Guid Id,
    string Name,
    EvidenceStatus Status,
    EvidenceType Type,
    DateOnly? ExpiresAt,
    string? OriginalFileName);

public sealed record UpdateContractObligationStatusRequest(ComplianceTaskStatus Status);

public sealed record AssignContractObligationOwnerRequest(Guid? UserId, string? RoleName, bool Notify = false);

public sealed record ContractObligationDetailResult(Guid TenantId, ContractObligationDetailDto Detail);

public interface IObligationDetailRepository
{
    Task<ContractObligationDetailResult?> FindCurrentTenantAsync(
        Guid contractClauseId,
        string obligationId,
        CancellationToken cancellationToken = default);

    Task<ContractObligationDetailResult?> UpdateStatusAsync(
        Guid contractClauseId,
        string obligationId,
        ComplianceTaskStatus status,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<ContractObligationDetailResult?> AssignOwnerAsync(
        Guid contractClauseId,
        string obligationId,
        AssignContractObligationOwnerRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}
