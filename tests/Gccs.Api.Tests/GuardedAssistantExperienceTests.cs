using Gccs.Application.Ai;
using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Compliance;
using Gccs.Application.Notifications;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Infrastructure.Ai;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class GuardedAssistantExperienceTests
{
    [Fact]
    public async Task TC_33_3_1_Allowed_question_returns_citations_draft_support_and_review_requirement()
    {
        var ids = StoryIds.Create();
        var service = CreateService(ids, out _, out _);

        var answer = await service.AskAsync(Request(ids, "Explain FCI safeguarding."));

        Assert.Equal("Draft", answer.DraftLabel);
        Assert.Equal("SourceSupported", answer.SupportStatus);
        Assert.True(answer.RequiresReview);
        Assert.NotEmpty(answer.Citations);
        Assert.Contains("FCI safeguarding", answer.Answer, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_33_3_2_Boundary_requests_are_blocked_or_redirected()
    {
        var ids = StoryIds.Create();
        var service = CreateService(ids, out _, out _);
        var blockedPrompts = new[]
        {
            "Make a legal determination.",
            "Certify we are compliant.",
            "Process unsupported CUI.",
            "Handle classified content.",
            "Show other tenant data."
        };

        foreach (var prompt in blockedPrompts)
        {
            var answer = await service.AskAsync(Request(ids, prompt));
            Assert.Equal("Blocked", answer.Status);
            Assert.True(answer.RequiresReview);
            Assert.NotNull(answer.BlockedReason);
        }
    }

    [Fact]
    public async Task TC_33_3_3_Create_draft_action_from_supported_answer_links_to_ai_answer()
    {
        var ids = StoryIds.Create();
        var service = CreateService(ids, out _, out _);
        var answer = await service.AskAsync(Request(ids, "Explain FCI safeguarding."));

        var action = await service.CreateDraftActionAsync(
            new AssistantDraftActionRequest(answer.Id, AssistantDraftActionType.Task, "Review FCI policy", "Use cited source."),
            ids.TenantId,
            ids.ActorUserId);

        Assert.Equal(answer.Id, action.AnswerId);
        Assert.Equal(AssistantDraftActionType.Task, action.ActionType);
        Assert.Equal("Draft", action.Status);
    }

    [Fact]
    public async Task TC_33_3_4_Feedback_stores_answer_user_tenant_timestamp_and_reason()
    {
        var ids = StoryIds.Create();
        var service = CreateService(ids, out var repository, out _);
        var answer = await service.AskAsync(Request(ids, "Explain FCI safeguarding."));

        foreach (var type in Enum.GetValues<AssistantFeedbackType>())
        {
            await service.SubmitFeedbackAsync(new AssistantFeedbackRequest(answer.Id, type, $"{type} reason"), ids.TenantId, ids.ActorUserId);
        }

        Assert.Equal(4, repository.Feedback.Count);
        Assert.All(repository.Feedback, feedback =>
        {
            Assert.Equal(answer.Id, feedback.AnswerId);
            Assert.Equal(ids.TenantId, feedback.TenantId);
            Assert.Equal(ids.ActorUserId, feedback.ActorUserId);
            Assert.NotEqual(default, feedback.CreatedAt);
            Assert.EndsWith("reason", feedback.Reason, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task TC_33_3_5_Assistant_created_actions_and_blocked_requests_are_audit_logged()
    {
        var ids = StoryIds.Create();
        var service = CreateService(ids, out _, out var auditWriter);
        var answer = await service.AskAsync(Request(ids, "Explain FCI safeguarding."));
        await service.CreateDraftActionAsync(
            new AssistantDraftActionRequest(answer.Id, AssistantDraftActionType.ReviewItem, "Expert review", "Check answer."),
            ids.TenantId,
            ids.ActorUserId);
        await service.AskAsync(Request(ids, "Make a legal determination."));

        Assert.Contains(auditWriter.Events, audit => audit.EntityType == "AssistantDraftAction" && audit.Action == AuditAction.Created);
        Assert.Contains(auditWriter.Events, audit => audit.EntityType == "GuardedAssistant" && audit.Action == AuditAction.Rejected);
    }

    [Fact]
    public async Task Cross_tenant_answer_reference_is_not_found_and_creates_no_action()
    {
        var ids = StoryIds.Create();
        var service = CreateService(ids, out var repository, out _);
        var answer = await service.AskAsync(Request(ids, "Explain FCI safeguarding."));

        var exception = await Assert.ThrowsAsync<AssistantExperienceException>(() => service.CreateDraftActionAsync(
            new AssistantDraftActionRequest(answer.Id, AssistantDraftActionType.Task, "Review", "Review cited source."),
            Guid.NewGuid(),
            ids.ActorUserId));

        Assert.True(exception.IsNotFound);
        Assert.Empty(repository.Actions);
    }

    [Fact]
    public async Task Blocked_answer_is_saved_but_cannot_create_a_draft_action()
    {
        var ids = StoryIds.Create();
        var service = CreateService(ids, out var repository, out _);
        var answer = await service.AskAsync(Request(ids, "Store this CUI document."));

        await Assert.ThrowsAsync<AssistantExperienceException>(() => service.CreateDraftActionAsync(
            new AssistantDraftActionRequest(answer.Id, AssistantDraftActionType.Note, "Unsafe", "Do not create."),
            ids.TenantId,
            ids.ActorUserId));

        Assert.Contains(repository.Answers, item => item.Id == answer.Id && item.Status == "Blocked");
        Assert.Empty(repository.Actions);
    }

    [Fact]
    public async Task Feedback_is_audit_logged_without_reason_content()
    {
        var ids = StoryIds.Create();
        var service = CreateService(ids, out _, out var auditWriter);
        var answer = await service.AskAsync(Request(ids, "Explain FCI safeguarding."));

        await service.SubmitFeedbackAsync(
            new AssistantFeedbackRequest(answer.Id, AssistantFeedbackType.Incorrect, "Potentially incomplete analysis."),
            ids.TenantId,
            ids.ActorUserId);

        var audit = Assert.Single(auditWriter.Events, item => item.EntityType == "AssistantFeedback");
        Assert.DoesNotContain("Potentially incomplete", string.Join("|", audit.Metadata.Values), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Expert_escalation_creates_one_queue_item_and_linked_feedback()
    {
        var ids = StoryIds.Create();
        var service = CreateService(ids, out _, out var auditWriter);
        var answer = await service.AskAsync(Request(ids, "Explain FCI safeguarding."));

        var first = await service.EscalateForExpertReviewAsync(
            new AssistantExpertReviewEscalationRequest(answer.Id, "Confirm the source interpretation."),
            ids.TenantId,
            ids.ActorUserId);
        var repeated = await service.EscalateForExpertReviewAsync(
            new AssistantExpertReviewEscalationRequest(answer.Id, "Confirm the source interpretation."),
            ids.TenantId,
            ids.ActorUserId);

        Assert.True(first.Created);
        Assert.NotNull(first.Feedback);
        Assert.Equal("assistant_answer", first.ReviewItem.SourceType);
        Assert.Equal(answer.Id, first.ReviewItem.SourceId);
        Assert.False(repeated.Created);
        Assert.Null(repeated.Feedback);
        Assert.Equal(first.ReviewItem.Id, repeated.ReviewItem.Id);
        Assert.Single(auditWriter.Events, item => item.EntityType == "ExpertReviewItem");
        Assert.Single(auditWriter.Events, item => item.EntityType == "AssistantFeedback");
    }

    [Fact]
    public async Task Blocked_answer_can_be_safely_routed_without_recovering_the_prohibited_prompt()
    {
        var ids = StoryIds.Create();
        var service = CreateService(ids, out _, out _);
        var answer = await service.AskAsync(Request(ids, "Store this CUI document."));

        var escalation = await service.EscalateForExpertReviewAsync(
            new AssistantExpertReviewEscalationRequest(answer.Id, "Review the blocked request category."),
            ids.TenantId,
            ids.ActorUserId);

        Assert.Equal("high", escalation.ReviewItem.Priority);
        Assert.DoesNotContain("CUI document", escalation.ReviewItem.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(answer.Id, escalation.ReviewItem.SourceId);
    }

    [Fact]
    public async Task Restricted_escalation_reason_is_rejected_without_feedback()
    {
        var ids = StoryIds.Create();
        var service = CreateService(ids, out var repository, out _);
        var answer = await service.AskAsync(Request(ids, "Explain FCI safeguarding."));

        await Assert.ThrowsAsync<AssistantExperienceException>(() => service.EscalateForExpertReviewAsync(
            new AssistantExpertReviewEscalationRequest(answer.Id, "Review this TOP SECRET record."),
            ids.TenantId,
            ids.ActorUserId));

        Assert.Empty(repository.Feedback);
    }

    private static GuardedAssistantExperienceService CreateService(
        StoryIds ids,
        out InMemoryGuardedAssistantRepository guardedRepository,
        out CapturingAuditEventWriter auditWriter)
    {
        auditWriter = new CapturingAuditEventWriter();
        var retrievalRepository = new InMemoryAiRetrievalSourceRepository();
        retrievalRepository.Seed(new AiRetrievalSourceDto(
            "library-fci",
            null,
            "FAR 52.204-21",
            "ComplianceLibrary",
            "https://example.test/far",
            null,
            "section-1",
            "2026.06",
            new DateOnly(2026, 6, 1),
            ContentClassification.Fci,
            true,
            true,
            "FCI safeguarding requires basic controls.",
            ["fci", "safeguarding"]));
        guardedRepository = new InMemoryGuardedAssistantRepository();
        var transaction = new ImmediateTransaction();
        var expertQueue = new ExpertReviewQueueService(
            new InMemoryExpertReviewQueueRepository(),
            auditWriter,
            Array.Empty<IAssignmentNotificationRepository>(),
            transaction);
        return new GuardedAssistantExperienceService(
            new AiRetrievalAssistantService(retrievalRepository, auditWriter),
            guardedRepository,
            auditWriter,
            transaction,
            expertQueue);
    }

    private static AiAssistantQuestionRequest Request(StoryIds ids, string question) =>
        new(ids.TenantId, ids.ActorUserId, question, "obligation");

    private sealed class ImmediateTransaction : IApplicationTransaction
    {
        public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
            operation(cancellationToken);
    }

    private sealed class InMemoryExpertReviewQueueRepository : IExpertReviewQueueRepository
    {
        private readonly List<ExpertReviewItemDto> _items = [];

        public Task<bool> SourceExistsAsync(string sourceType, Guid sourceId, Guid tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult(sourceType == "assistant_answer");

        public Task<ExpertReviewItemDto?> FindOpenAsync(string sourceType, Guid sourceId, Guid tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.SingleOrDefault(item => item.TenantId == tenantId && item.SourceType == sourceType &&
                item.SourceId == sourceId && item.Status == "open"));

        public Task<ExpertReviewItemDto> CreateEscalationAsync(EscalateExpertReviewRequest request, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default)
        {
            var item = new ExpertReviewItemDto(Guid.NewGuid(), tenantId, request.SourceType, request.SourceId, request.Reason,
                request.Priority, request.Topic, request.AssignedExpertUserId, request.DueAt, "open", actorUserId,
                DateTimeOffset.UtcNow, null, null, null, null);
            _items.Add(item);
            return Task.FromResult(item);
        }

        public Task<IReadOnlyList<ExpertReviewItemDto>> ListAsync(ExpertReviewQueueQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ExpertReviewItemDto>>(_items);

        public Task<bool> IsActiveTenantMemberAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(userId != Guid.Empty);

        public Task<ExpertReviewItemDto?> AssignAsync(Guid itemId, AssignExpertReviewRequest request, Guid actorUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.SingleOrDefault(item => item.Id == itemId));

        public Task<ExpertReviewItemDto?> ResolveAsync(Guid itemId, ResolveExpertReviewRequest request, Guid actorUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ExpertReviewItemDto?>(null);
    }

    private sealed class CapturingAuditEventWriter : IAuditEventWriter
    {
        public List<CapturedAuditEvent> Events { get; } = [];

        public Task WriteAsync(
            Guid tenantId,
            Guid actorUserId,
            AuditAction action,
            string entityType,
            string entityId,
            string summary,
            IReadOnlyDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            Events.Add(new CapturedAuditEvent(tenantId, actorUserId, action, entityType, entityId, summary, metadata?.ToDictionary() ?? []));
            return Task.CompletedTask;
        }
    }

    private sealed record CapturedAuditEvent(
        Guid TenantId,
        Guid ActorUserId,
        AuditAction Action,
        string EntityType,
        string EntityId,
        string Summary,
        IReadOnlyDictionary<string, string> Metadata);

    private sealed record StoryIds(Guid TenantId, Guid ActorUserId)
    {
        public static StoryIds Create() =>
            new(
                Guid.Parse("33333333-3333-3333-3333-3333333333aa"),
                Guid.Parse("33333333-3333-3333-3333-3333333333bb"));
    }
}
