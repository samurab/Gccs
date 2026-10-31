using Gccs.Domain.Cmmc;

namespace Gccs.Application.Cmmc;

public sealed record CmmcAffirmationDto(
    Guid Id,
    Guid TenantId,
    CmmcLevel Level,
    DateOnly DueAt,
    DateOnly? SubmittedAt,
    Guid? SubmittedByUserId,
    string? ConfirmationReference,
    IReadOnlyList<Guid> EvidenceItemIds,
    AffirmationStatus Status,
    bool IsDueSoon,
    bool IsOverdue,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record UpsertCmmcAffirmationRequest(
    CmmcLevel Level,
    DateOnly DueAt,
    DateOnly? SubmittedAt,
    Guid? SubmittedByUserId,
    string? ConfirmationReference,
    IReadOnlyList<Guid> EvidenceItemIds,
    AffirmationStatus Status);

public interface ICmmcAffirmationRepository
{
    Task<IReadOnlyList<CmmcAffirmationDto>> ListCurrentTenantAsync(CancellationToken cancellationToken = default);

    Task<CmmcAffirmationDto> CreateAsync(
        UpsertCmmcAffirmationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<CmmcAffirmationDto?> UpdateAsync(
        Guid affirmationId,
        UpsertCmmcAffirmationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}
