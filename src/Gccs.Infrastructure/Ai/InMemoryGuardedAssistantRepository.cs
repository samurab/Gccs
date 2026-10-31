using Gccs.Application.Ai;

namespace Gccs.Infrastructure.Ai;

public sealed class InMemoryGuardedAssistantRepository : IGuardedAssistantRepository
{
    private readonly object _sync = new();
    public List<GuardedAssistantAnswerDto> Answers { get; } = [];
    public List<AssistantDraftActionDto> Actions { get; } = [];
    public List<AssistantFeedbackDto> Feedback { get; } = [];

    public Task SaveAnswerAsync(GuardedAssistantAnswerDto answer, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        lock (_sync) Answers.Add(answer);
        return Task.CompletedTask;
    }

    public Task<GuardedAssistantAnswerDto?> FindAnswerAsync(
        Guid answerId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(Answers.SingleOrDefault(answer => answer.Id == answerId && answer.TenantId == tenantId));
        }
    }

    public Task<IReadOnlyList<GuardedAssistantAnswerDto>> FindAnswersAsync(
        IReadOnlyCollection<Guid> answerIds,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult<IReadOnlyList<GuardedAssistantAnswerDto>>(
                Answers.Where(answer => answer.TenantId == tenantId && answerIds.Contains(answer.Id)).ToArray());
        }
    }

    public Task<AssistantDraftActionDto> CreateDraftActionAsync(
        AssistantDraftActionRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var action = new AssistantDraftActionDto(
            Guid.NewGuid(), tenantId, request.AnswerId, request.ActionType, request.Title.Trim(), request.Body.Trim(),
            "Draft", actorUserId, DateTimeOffset.UtcNow);
        lock (_sync) Actions.Add(action);
        return Task.FromResult(action);
    }

    public Task<AssistantFeedbackDto> SubmitFeedbackAsync(
        AssistantFeedbackRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var feedback = new AssistantFeedbackDto(
            Guid.NewGuid(),
            tenantId,
            request.AnswerId,
            actorUserId,
            request.FeedbackType,
            request.Reason.Trim(),
            DateTimeOffset.UtcNow);
        lock (_sync) Feedback.Add(feedback);
        return Task.FromResult(feedback);
    }

    public List<AiOutputReviewHistoryDto> Reviews { get; } = [];
    public List<AiOutputUsageDto> DeliverableUses { get; } = [];

    public Task<IReadOnlyList<GuardedAssistantAnswerDto>> ListAnswersAsync(
        Guid tenantId,
        bool includeArchived,
        IReadOnlyCollection<string>? allowedWorkflowContexts = null,
        CancellationToken cancellationToken = default)
    {
        var contexts = allowedWorkflowContexts?.Select(value => value.Trim().ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);
        lock (_sync) return Task.FromResult<IReadOnlyList<GuardedAssistantAnswerDto>>(Answers
            .Where(x => x.TenantId == tenantId &&
                (includeArchived || x.ReviewState != AiOutputReviewState.Archived) &&
                (contexts is null || contexts.Contains(x.WorkflowContext)))
            .OrderByDescending(x => x.CreatedAt).ToArray());
    }

    public Task<AiOutputReviewResultDto?> ReviewAnswerAsync(Guid answerId, Guid tenantId,
        AiOutputReviewDecisionRequest request, Guid reviewerUserId, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var current = Answers.SingleOrDefault(x => x.Id == answerId && x.TenantId == tenantId);
            if (current is null) return Task.FromResult<AiOutputReviewResultDto?>(null);
            if (current.Version != request.ExpectedVersion) throw new AiOutputReviewConflictException();
            var at = DateTimeOffset.UtcNow;
            var updated = current with { ReviewState = request.State, HumanReviewStatus = request.State.ToString(),
                ReviewDecision = request.State.ToString(), ReviewNotes = request.Note?.Trim(), RejectionReason = request.Reason?.Trim(),
                ReviewedByUserId = reviewerUserId, ReviewedAt = at, Version = current.Version + 1 };
            Answers[Answers.IndexOf(current)] = updated;
            var history = new AiOutputReviewHistoryDto(Guid.NewGuid(), tenantId, answerId, current.ReviewState,
                request.State, reviewerUserId, updated.ReviewNotes, updated.RejectionReason, at);
            Reviews.Add(history);
            return Task.FromResult<AiOutputReviewResultDto?>(new(updated, history));
        }
    }

    public Task<IReadOnlyList<AiOutputReviewHistoryDto>> ListReviewHistoryAsync(Guid answerId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        lock (_sync) return Task.FromResult<IReadOnlyList<AiOutputReviewHistoryDto>>(Reviews
            .Where(x => x.TenantId == tenantId && x.AnswerId == answerId).OrderBy(x => x.CreatedAt).ToArray());
    }

    public Task<AiOutputUsageDto?> LinkDeliverableAsync(Guid answerId, Guid tenantId, AiOutputUsageRequest request,
        Guid actorUserId, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (!Answers.Any(x => x.Id == answerId && x.TenantId == tenantId)) return Task.FromResult<AiOutputUsageDto?>(null);
            var existing = DeliverableUses.SingleOrDefault(x => x.TenantId == tenantId &&
                x.DeliverableType == request.DeliverableType && x.DeliverableId == request.DeliverableId.Trim());
            if (existing is not null)
            {
                if (existing.AnswerId != answerId)
                    throw new AiOutputReviewValidationException("deliverableId", "The deliverable is already linked to a different AI output.");
                return Task.FromResult<AiOutputUsageDto?>(existing);
            }
            var usage = new AiOutputUsageDto(Guid.NewGuid(), tenantId, answerId, request.DeliverableType,
                request.DeliverableId.Trim(), actorUserId, DateTimeOffset.UtcNow);
            DeliverableUses.Add(usage);
            return Task.FromResult<AiOutputUsageDto?>(usage);
        }
    }

    public Task<bool> DeliverableExistsAsync(Guid tenantId, AiOutputUsageRequest request,
        CancellationToken cancellationToken = default) => Task.FromResult(Guid.TryParse(request.DeliverableId, out _));
}
