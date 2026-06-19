using Gccs.Application.Ai;

namespace Gccs.Infrastructure.Ai;

public sealed class InMemoryGuardedAssistantRepository : IGuardedAssistantRepository
{
    public List<GuardedAssistantAnswerDto> Answers { get; } = [];
    public List<AssistantDraftActionDto> Actions { get; } = [];
    public List<AssistantFeedbackDto> Feedback { get; } = [];

    public Task SaveAnswerAsync(GuardedAssistantAnswerDto answer, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        Answers.Add(answer);
        return Task.CompletedTask;
    }

    public Task<AssistantDraftActionDto> CreateDraftActionAsync(
        AssistantDraftActionRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var action = new AssistantDraftActionDto(
            Guid.NewGuid(),
            tenantId,
            request.AnswerId,
            request.ActionType,
            request.Title.Trim(),
            request.Body.Trim(),
            "Draft",
            DateTimeOffset.UtcNow);
        Actions.Add(action);
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
        Feedback.Add(feedback);
        return Task.FromResult(feedback);
    }
}
