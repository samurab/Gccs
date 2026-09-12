using Gccs.Application.Ai;

namespace Gccs.Infrastructure.Persistence.Models;

public sealed class AssistantAnswerEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ActorUserId { get; set; }
    public string WorkflowContext { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string CitationsJson { get; set; } = "[]";
    public string SupportStatus { get; set; } = string.Empty;
    public string DraftLabel { get; set; } = string.Empty;
    public bool RequiresReview { get; set; }
    public bool EscalationRecommended { get; set; }
    public string? BlockedReason { get; set; }
    public string HumanReviewStatus { get; set; } = "pending";
    public Guid? ReviewedByUserId { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewDecision { get; set; }
    public string? ReviewNotes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<AssistantDraftActionEntity> Actions { get; set; } = [];
    public ICollection<AssistantFeedbackEntity> Feedback { get; set; } = [];
}

public sealed class AssistantDraftActionEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid AnswerId { get; set; }
    public AssistantDraftActionType ActionType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public AssistantAnswerEntity? Answer { get; set; }
}

public sealed class AssistantFeedbackEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid AnswerId { get; set; }
    public Guid ActorUserId { get; set; }
    public AssistantFeedbackType FeedbackType { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public AssistantAnswerEntity? Answer { get; set; }
}
