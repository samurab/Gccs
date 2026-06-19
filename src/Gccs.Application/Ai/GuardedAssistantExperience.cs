using Gccs.Application.Audit;
using Gccs.Domain.Audit;

namespace Gccs.Application.Ai;

public sealed class GuardedAssistantExperienceService(
    AiRetrievalAssistantService retrievalService,
    IGuardedAssistantRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public async Task<GuardedAssistantAnswerDto> AskAsync(
        AiAssistantQuestionRequest request,
        CancellationToken cancellationToken = default)
    {
        var blocked = GetBlockedReason(request.Question);
        if (blocked is not null)
        {
            await auditEventWriter.WriteAsync(
                request.TenantId,
                request.ActorUserId,
                AuditAction.Rejected,
                "GuardedAssistant",
                request.WorkflowContext,
                "Assistant request was blocked or redirected.",
                new Dictionary<string, string>
                {
                    ["reason"] = blocked,
                    ["workflowContext"] = request.WorkflowContext
                },
                cancellationToken);
            return new GuardedAssistantAnswerDto(
                Guid.NewGuid(),
                request.TenantId,
                "Blocked",
                "This request requires expert review or is outside the assistant boundary.",
                [],
                "Unsupported",
                DraftLabel: "Blocked",
                RequiresReview: true,
                BlockedReason: blocked);
        }

        var response = await retrievalService.AnswerAsync(request, cancellationToken);
        var answer = new GuardedAssistantAnswerDto(
            Guid.NewGuid(),
            request.TenantId,
            response.Status,
            response.Answer,
            response.Citations,
            response.Citations.Count > 0 ? "SourceSupported" : "NeedsReview",
            DraftLabel: response.Status == "Draft" ? "Draft" : response.Status,
            RequiresReview: true,
            BlockedReason: null);
        await repository.SaveAnswerAsync(answer, request.ActorUserId, cancellationToken);
        return answer;
    }

    public async Task<AssistantDraftActionDto> CreateDraftActionAsync(
        AssistantDraftActionRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var action = await repository.CreateDraftActionAsync(request, tenantId, actorUserId, cancellationToken);
        await auditEventWriter.WriteAsync(
            tenantId,
            actorUserId,
            AuditAction.Created,
            "AssistantDraftAction",
            action.Id.ToString(),
            "Assistant draft action was created.",
            new Dictionary<string, string>
            {
                ["answerId"] = action.AnswerId.ToString(),
                ["actionType"] = action.ActionType.ToString()
            },
            cancellationToken);
        return action;
    }

    public async Task<AssistantFeedbackDto> SubmitFeedbackAsync(
        AssistantFeedbackRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        await repository.SubmitFeedbackAsync(request, tenantId, actorUserId, cancellationToken);

    private static string? GetBlockedReason(string question)
    {
        var normalized = question.ToLowerInvariant();
        if (normalized.Contains("legal determination", StringComparison.Ordinal) ||
            normalized.Contains("certify", StringComparison.Ordinal) ||
            normalized.Contains("certification claim", StringComparison.Ordinal))
        {
            return "legal-or-certification";
        }

        if (normalized.Contains("classified", StringComparison.Ordinal))
        {
            return "classified-content";
        }

        if (normalized.Contains("prohibited", StringComparison.Ordinal) || normalized.Contains("unsupported cui", StringComparison.Ordinal))
        {
            return "prohibited-or-unsupported-cui";
        }

        if (normalized.Contains("other tenant", StringComparison.Ordinal) || normalized.Contains("cross-tenant", StringComparison.Ordinal))
        {
            return "cross-tenant";
        }

        return null;
    }
}

public interface IGuardedAssistantRepository
{
    Task SaveAnswerAsync(GuardedAssistantAnswerDto answer, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<AssistantDraftActionDto> CreateDraftActionAsync(AssistantDraftActionRequest request, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<AssistantFeedbackDto> SubmitFeedbackAsync(AssistantFeedbackRequest request, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default);
}

public sealed record GuardedAssistantAnswerDto(
    Guid Id,
    Guid TenantId,
    string Status,
    string Answer,
    IReadOnlyList<AiCitationDto> Citations,
    string SupportStatus,
    string DraftLabel,
    bool RequiresReview,
    string? BlockedReason);

public sealed record AssistantDraftActionRequest(
    Guid AnswerId,
    AssistantDraftActionType ActionType,
    string Title,
    string Body);

public sealed record AssistantDraftActionDto(
    Guid Id,
    Guid TenantId,
    Guid AnswerId,
    AssistantDraftActionType ActionType,
    string Title,
    string Body,
    string Status,
    DateTimeOffset CreatedAt);

public sealed record AssistantFeedbackRequest(
    Guid AnswerId,
    AssistantFeedbackType FeedbackType,
    string Reason);

public sealed record AssistantFeedbackDto(
    Guid Id,
    Guid TenantId,
    Guid AnswerId,
    Guid ActorUserId,
    AssistantFeedbackType FeedbackType,
    string Reason,
    DateTimeOffset CreatedAt);

public enum AssistantDraftActionType
{
    Task,
    EvidenceRequest,
    Note,
    ReviewItem
}

public enum AssistantFeedbackType
{
    Helpful,
    Incorrect,
    MissingSource,
    NeedsExpertReview
}
