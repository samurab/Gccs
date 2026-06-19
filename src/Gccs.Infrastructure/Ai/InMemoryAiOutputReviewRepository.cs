using Gccs.Application.Ai;

namespace Gccs.Infrastructure.Ai;

public sealed class InMemoryAiOutputReviewRepository : IAiOutputReviewRepository
{
    private readonly List<AiInteractionLogDto> _logs = [];

    public Task<AiInteractionLogDto> CreateAsync(
        AiInteractionLogRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var log = new AiInteractionLogDto(
            Guid.NewGuid(),
            tenantId,
            actorUserId,
            request.Prompt,
            request.PromptMetadata,
            request.ModelConfiguration,
            request.RetrievedSourceIds.ToArray(),
            request.GeneratedOutput,
            request.WorkflowContext,
            request.Classification,
            AiOutputReviewState.Draft,
            null,
            null,
            null,
            DateTimeOffset.UtcNow,
            null,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(365).Date));
        _logs.Add(log);
        return Task.FromResult(log);
    }

    public Task<AiInteractionLogDto?> FindAsync(Guid logId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_logs.SingleOrDefault(log => log.Id == logId));

    public Task<IReadOnlyList<AiInteractionLogDto>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AiInteractionLogDto>>(_logs.Where(log => log.TenantId == tenantId).ToArray());

    public Task<AiInteractionLogDto?> UpdateStateAsync(
        Guid logId,
        AiOutputReviewState state,
        string? note,
        string? reason,
        Guid reviewerUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = _logs.SingleOrDefault(log => log.Id == logId);
        if (existing is null)
        {
            return Task.FromResult<AiInteractionLogDto?>(null);
        }

        var updated = existing with
        {
            State = state,
            ReviewerUserId = reviewerUserId,
            ReviewNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            RejectionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            ReviewedAt = DateTimeOffset.UtcNow
        };
        _logs.Remove(existing);
        _logs.Add(updated);
        return Task.FromResult<AiInteractionLogDto?>(updated);
    }
}
