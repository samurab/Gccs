using Gccs.Application.Ai;
using Gccs.Application.Audit;
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
        return new GuardedAssistantExperienceService(
            new AiRetrievalAssistantService(retrievalRepository, auditWriter),
            guardedRepository,
            auditWriter);
    }

    private static AiAssistantQuestionRequest Request(StoryIds ids, string question) =>
        new(ids.TenantId, ids.ActorUserId, question, "assistant-panel");

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
