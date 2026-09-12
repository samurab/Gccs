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
}
