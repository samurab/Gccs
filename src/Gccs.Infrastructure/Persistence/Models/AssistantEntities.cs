using Gccs.Application.Ai;
using Gccs.Domain.Common;

namespace Gccs.Infrastructure.Persistence.Models;

public sealed class AssistantAnswerEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ActorUserId { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public bool PromptWasRedacted { get; set; }
    public string PromptMetadataJson { get; set; } = "{}";
    public string ModelConfigurationJson { get; set; } = "{}";
    public string RetrievalPolicyJson { get; set; } = "[]";
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
    public string? RejectionReason { get; set; }
    public AiOutputReviewState ReviewState { get; set; } = AiOutputReviewState.Draft;
    public ContentClassification Classification { get; set; } = ContentClassification.Unclassified;
    public string Result { get; set; } = string.Empty;
    public DateTimeOffset RetainUntil { get; set; }
    public long Version { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<AssistantDraftActionEntity> Actions { get; set; } = [];
    public ICollection<AssistantFeedbackEntity> Feedback { get; set; } = [];
    public ICollection<AssistantOutputReviewEntity> Reviews { get; set; } = [];
    public ICollection<AssistantOutputUsageEntity> DeliverableUses { get; set; } = [];
}

public sealed class AssistantOutputReviewEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid AnswerId { get; set; }
    public AiOutputReviewState PreviousState { get; set; }
    public AiOutputReviewState NewState { get; set; }
    public Guid? ReviewerUserId { get; set; }
    public string? Note { get; set; }
    public string? RejectionReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public AssistantAnswerEntity? Answer { get; set; }
}

public sealed class AssistantOutputUsageEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid AnswerId { get; set; }
    public AiDeliverableType DeliverableType { get; set; }
    public string DeliverableId { get; set; } = string.Empty;
    public Guid LinkedByUserId { get; set; }
    public DateTimeOffset LinkedAt { get; set; }
    public AssistantAnswerEntity? Answer { get; set; }
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
