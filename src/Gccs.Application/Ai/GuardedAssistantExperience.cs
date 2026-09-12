using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Compliance;
using Gccs.Domain.Audit;

namespace Gccs.Application.Ai;

public sealed class GuardedAssistantExperienceService(
    AiRetrievalAssistantService retrievalService,
    IGuardedAssistantRepository repository,
    IAuditEventWriter auditEventWriter,
    IApplicationTransaction transaction,
    ExpertReviewQueueService expertReviewQueue)
{
    private static readonly HashSet<string> AllowedWorkflowContexts = new(StringComparer.OrdinalIgnoreCase)
    {
        "obligation", "contract", "evidence", "cmmc", "ssp", "poam", "labor", "subcontractor"
    };

    public Task<GuardedAssistantAnswerDto?> GetAnswerAsync(
        Guid answerId,
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        repository.FindAnswerAsync(answerId, tenantId, cancellationToken);

    public async Task<IReadOnlyList<AssistantExpertReviewQueueItemDto>> ListExpertReviewQueueAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var items = await expertReviewQueue.ListAsync(
            new ExpertReviewQueueQuery(Status: null, SourceType: "assistant_answer", AssignedExpertUserId: null, Priority: null),
            cancellationToken);
        var answers = await repository.FindAnswersAsync(items.Select(item => item.SourceId).ToArray(), tenantId, cancellationToken);
        var answersById = answers.ToDictionary(answer => answer.Id);
        return items.Select(item => new AssistantExpertReviewQueueItemDto(
            item,
            answersById.GetValueOrDefault(item.SourceId))).ToArray();
    }

    public Task<GuardedAssistantAnswerDto> AskAsync(
        AiAssistantQuestionRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateQuestion(request);
        return transaction.ExecuteAsync(async token =>
        {
            var blockedReason = AssistantPromptGuard.GetBlockedReason(request.Question);
            if (blockedReason is not null)
            {
                var blockedAnswer = CreateAnswer(
                    request,
                    "Blocked",
                    "This request is outside the assistant boundary. Do not include CUI, classified information, another tenant's data, or requests for legal or certification determinations. Route it for qualified human review.",
                    [],
                    "Unsupported",
                    "Blocked",
                    blockedReason);
                await repository.SaveAnswerAsync(blockedAnswer, request.ActorUserId, token);
                await auditEventWriter.WriteAsync(
                    request.TenantId,
                    request.ActorUserId,
                    AuditAction.Rejected,
                    "GuardedAssistant",
                    blockedAnswer.Id.ToString(),
                    "Assistant request was blocked and redirected.",
                    new Dictionary<string, string>
                    {
                        ["reason"] = blockedReason,
                        ["workflowContext"] = request.WorkflowContext
                    },
                    token);
                return blockedAnswer;
            }

            var response = await retrievalService.AnswerAsync(request, token);
            var answer = CreateAnswer(
                request,
                response.Status,
                response.Answer,
                response.Citations,
                response.Citations.Count > 0 ? "SourceSupported" : "NeedsReview",
                response.Status == "Draft" ? "Draft" : response.Status,
                null);
            await repository.SaveAnswerAsync(answer, request.ActorUserId, token);
            return answer;
        }, cancellationToken);
    }

    public Task<AssistantDraftActionDto> CreateDraftActionAsync(
        AssistantDraftActionRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateAction(request);
        return transaction.ExecuteAsync(async token =>
        {
            var answer = await RequireSupportedAnswerAsync(request.AnswerId, tenantId, token);
            var action = await repository.CreateDraftActionAsync(request, tenantId, actorUserId, token);
            await auditEventWriter.WriteAsync(
                tenantId,
                actorUserId,
                AuditAction.Created,
                "AssistantDraftAction",
                action.Id.ToString(),
                action.ActionType == AssistantDraftActionType.ReviewItem
                    ? "Assistant answer was routed for expert review."
                    : "Assistant draft action was created for human review.",
                new Dictionary<string, string>
                {
                    ["answerId"] = answer.Id.ToString(),
                    ["actionType"] = action.ActionType.ToString(),
                    ["workflowContext"] = answer.WorkflowContext
                },
                token);
            return action;
        }, cancellationToken);
    }

    public Task<AssistantFeedbackDto> SubmitFeedbackAsync(
        AssistantFeedbackRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateFeedback(request);
        return transaction.ExecuteAsync(async token =>
        {
            var answer = await repository.FindAnswerAsync(request.AnswerId, tenantId, token) ??
                throw AssistantExperienceException.NotFound();
            var feedback = await repository.SubmitFeedbackAsync(request, tenantId, actorUserId, token);
            await WriteFeedbackAuditAsync(feedback, answer, actorUserId, token);
            return feedback;
        }, cancellationToken);
    }

    public Task<AssistantExpertReviewEscalationDto> EscalateForExpertReviewAsync(
        AssistantExpertReviewEscalationRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateEscalation(request);
        return transaction.ExecuteAsync(async token =>
        {
            var answer = await repository.FindAnswerAsync(request.AnswerId, tenantId, token) ??
                throw AssistantExperienceException.NotFound();
            var escalation = await expertReviewQueue.EscalateWithResultAsync(
                new EscalateExpertReviewRequest(
                    "assistant_answer",
                    answer.Id,
                    request.Reason.Trim(),
                    answer.Status == "Blocked" ? "high" : "medium",
                    $"{DisplayContext(answer.WorkflowContext)} assistant answer review",
                    AssignedExpertUserId: null,
                    DueAt: null),
                tenantId,
                actorUserId,
                token);

            AssistantFeedbackDto? feedback = null;
            if (escalation.Created)
            {
                feedback = await repository.SubmitFeedbackAsync(
                    new AssistantFeedbackRequest(answer.Id, AssistantFeedbackType.NeedsExpertReview, request.Reason),
                    tenantId,
                    actorUserId,
                    token);
                await WriteFeedbackAuditAsync(feedback, answer, actorUserId, token);
            }

            return new AssistantExpertReviewEscalationDto(escalation.Item, feedback, escalation.Created);
        }, cancellationToken);
    }

    private Task WriteFeedbackAuditAsync(
        AssistantFeedbackDto feedback,
        GuardedAssistantAnswerDto answer,
        Guid actorUserId,
        CancellationToken cancellationToken) =>
        auditEventWriter.WriteAsync(
            feedback.TenantId,
            actorUserId,
            AuditAction.Created,
            "AssistantFeedback",
            feedback.Id.ToString(),
            "Assistant answer feedback was recorded.",
            new Dictionary<string, string>
            {
                ["answerId"] = answer.Id.ToString(),
                ["feedbackType"] = feedback.FeedbackType.ToString(),
                ["workflowContext"] = answer.WorkflowContext
            },
            cancellationToken);

    private static string DisplayContext(string workflowContext) => workflowContext switch
    {
        "cmmc" => "CMMC readiness",
        "ssp" => "SSP",
        "poam" => "POA&M",
        _ => char.ToUpperInvariant(workflowContext[0]) + workflowContext[1..]
    };

    private async Task<GuardedAssistantAnswerDto> RequireSupportedAnswerAsync(Guid answerId, Guid tenantId, CancellationToken cancellationToken)
    {
        var answer = await repository.FindAnswerAsync(answerId, tenantId, cancellationToken) ??
            throw AssistantExperienceException.NotFound();
        if (!string.Equals(answer.SupportStatus, "SourceSupported", StringComparison.Ordinal) ||
            !string.Equals(answer.Status, "Draft", StringComparison.Ordinal))
            throw AssistantExperienceException.Validation("answerId", "Draft actions require a source-supported assistant answer.");
        return answer;
    }

    private static GuardedAssistantAnswerDto CreateAnswer(
        AiAssistantQuestionRequest request, string status, string answer, IReadOnlyList<AiCitationDto> citations,
        string supportStatus, string draftLabel, string? blockedReason) =>
        new(
            Guid.NewGuid(), request.TenantId, request.WorkflowContext.Trim().ToLowerInvariant(), status, answer,
            citations, supportStatus, draftLabel, RequiresReview: true,
            EscalationRecommended: blockedReason is not null || citations.Count == 0,
            BlockedReason: blockedReason, CreatedAt: DateTimeOffset.UtcNow,
            HumanReviewStatus: "pending", ReviewedByUserId: null, ReviewedAt: null, ReviewDecision: null, ReviewNotes: null);

    private static void ValidateQuestion(AiAssistantQuestionRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Question)) errors["question"] = ["Question is required."];
        else if (request.Question.Length > 4_000) errors["question"] = ["Question cannot exceed 4,000 characters."];
        if (string.IsNullOrWhiteSpace(request.WorkflowContext) || !AllowedWorkflowContexts.Contains(request.WorkflowContext.Trim()))
            errors["workflowContext"] = ["Workflow context is not supported."];
        if (errors.Count > 0) throw new AssistantExperienceException(errors);
    }

    private static void ValidateAction(AssistantDraftActionRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.AnswerId == Guid.Empty) errors["answerId"] = ["Answer id is required."];
        if (string.IsNullOrWhiteSpace(request.Title)) errors["title"] = ["Title is required."];
        else if (request.Title.Length > 240) errors["title"] = ["Title cannot exceed 240 characters."];
        if (string.IsNullOrWhiteSpace(request.Body)) errors["body"] = ["Draft content is required."];
        else if (request.Body.Length > 4_000) errors["body"] = ["Draft content cannot exceed 4,000 characters."];
        if (SensitiveContentMarkerDetector.ContainsExplicitRestrictedMarking(request.Title) ||
            SensitiveContentMarkerDetector.ContainsExplicitRestrictedMarking(request.Body))
            errors["body"] = ["Assistant actions cannot contain restricted-data markings in the No-CUI service."];
        if (errors.Count > 0) throw new AssistantExperienceException(errors);
    }

    private static void ValidateFeedback(AssistantFeedbackRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.AnswerId == Guid.Empty) errors["answerId"] = ["Answer id is required."];
        if (string.IsNullOrWhiteSpace(request.Reason)) errors["reason"] = ["Feedback reason is required."];
        else if (request.Reason.Length > 1_000) errors["reason"] = ["Feedback reason cannot exceed 1,000 characters."];
        if (SensitiveContentMarkerDetector.ContainsExplicitRestrictedMarking(request.Reason))
            errors["reason"] = ["Feedback cannot contain restricted-data markings in the No-CUI service."];
        if (errors.Count > 0) throw new AssistantExperienceException(errors);
    }

    private static void ValidateEscalation(AssistantExpertReviewEscalationRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.AnswerId == Guid.Empty) errors["answerId"] = ["Answer id is required."];
        if (string.IsNullOrWhiteSpace(request.Reason)) errors["reason"] = ["Escalation reason is required."];
        else if (request.Reason.Trim().Length > 1_000) errors["reason"] = ["Escalation reason cannot exceed 1,000 characters."];
        if (AssistantPromptGuard.GetBlockedReason(request.Reason) is not null)
            errors["reason"] = ["Escalation reason cannot contain prohibited or unsupported content in the No-CUI service."];
        if (errors.Count > 0) throw new AssistantExperienceException(errors);
    }
}

public static class AssistantPromptGuard
{
    public static string? GetBlockedReason(string question)
    {
        var normalized = question.ToLowerInvariant();
        if (ContainsAny(normalized, "legal advice", "legal determination", "legal opinion", "act as my attorney", "binding interpretation"))
            return "legal-determination";
        if (ContainsAny(normalized, "certify us", "certify me", "certify we", "certification claim", "declare us compliant", "guarantee compliance"))
            return "certification-claim";
        if (ContainsAny(normalized, "classified", "top secret", "secret document", "confidential clearance"))
            return "classified-content";
        if (SensitiveContentMarkerDetector.ContainsExplicitRestrictedMarking(question) ||
            (ContainsAny(normalized, "cui", "itar", "export-controlled", "export controlled", "government-furnished information", "government furnished information") &&
             ContainsAny(normalized, "upload", "store", "process", "analyze", "paste", "send", "ingest", "retain")))
            return "prohibited-or-unsupported-data";
        if (ContainsAny(normalized, "other tenant", "another tenant", "different tenant", "other customer", "another customer", "other client's", "another client's", "cross-tenant"))
            return "cross-tenant";
        return null;
    }

    private static bool ContainsAny(string value, params string[] candidates) =>
        candidates.Any(candidate => value.Contains(candidate, StringComparison.Ordinal));
}

public interface IGuardedAssistantRepository
{
    Task SaveAnswerAsync(GuardedAssistantAnswerDto answer, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<GuardedAssistantAnswerDto?> FindAnswerAsync(Guid answerId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GuardedAssistantAnswerDto>> FindAnswersAsync(IReadOnlyCollection<Guid> answerIds, Guid tenantId, CancellationToken cancellationToken = default);
    Task<AssistantDraftActionDto> CreateDraftActionAsync(AssistantDraftActionRequest request, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<AssistantFeedbackDto> SubmitFeedbackAsync(AssistantFeedbackRequest request, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default);
}

public sealed record GuardedAssistantAnswerDto(
    Guid Id, Guid TenantId, string WorkflowContext, string Status, string Answer, IReadOnlyList<AiCitationDto> Citations,
    string SupportStatus, string DraftLabel, bool RequiresReview, bool EscalationRecommended, string? BlockedReason,
    DateTimeOffset CreatedAt, string HumanReviewStatus, Guid? ReviewedByUserId, DateTimeOffset? ReviewedAt,
    string? ReviewDecision, string? ReviewNotes);

public sealed record AssistantQuestionApiRequest(string Question, string WorkflowContext);
public sealed record AssistantDraftActionApiRequest(AssistantDraftActionType ActionType, string Title, string Body);
public sealed record AssistantFeedbackApiRequest(AssistantFeedbackType FeedbackType, string Reason);
public sealed record AssistantExpertReviewApiRequest(string Reason);
public sealed record AssistantDraftActionRequest(Guid AnswerId, AssistantDraftActionType ActionType, string Title, string Body);
public sealed record AssistantDraftActionDto(
    Guid Id, Guid TenantId, Guid AnswerId, AssistantDraftActionType ActionType, string Title, string Body,
    string Status, Guid CreatedByUserId, DateTimeOffset CreatedAt);
public sealed record AssistantFeedbackRequest(Guid AnswerId, AssistantFeedbackType FeedbackType, string Reason);
public sealed record AssistantFeedbackDto(
    Guid Id, Guid TenantId, Guid AnswerId, Guid ActorUserId, AssistantFeedbackType FeedbackType, string Reason,
    DateTimeOffset CreatedAt);
public sealed record AssistantExpertReviewEscalationRequest(Guid AnswerId, string Reason);
public sealed record AssistantExpertReviewEscalationDto(
    ExpertReviewItemDto ReviewItem,
    AssistantFeedbackDto? Feedback,
    bool Created);
public sealed record AssistantExpertReviewQueueItemDto(
    ExpertReviewItemDto ReviewItem,
    GuardedAssistantAnswerDto? Answer);

public enum AssistantDraftActionType { Task, EvidenceRequest, Note, ReviewItem }
public enum AssistantFeedbackType { Helpful, Incorrect, MissingSource, NeedsExpertReview }

public sealed class AssistantExperienceException(IReadOnlyDictionary<string, string[]> errors)
    : InvalidOperationException("Assistant request is invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
    public bool IsNotFound { get; private init; }

    public static AssistantExperienceException Validation(string key, string message) =>
        new(new Dictionary<string, string[]> { [key] = [message] });
    public static AssistantExperienceException NotFound() =>
        new(new Dictionary<string, string[]> { ["answerId"] = ["Assistant answer was not found."] }) { IsNotFound = true };
}
