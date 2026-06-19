using Gccs.Application.Ai;
using Gccs.Application.Audit;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Infrastructure.Ai;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class AiOutputLoggingReviewTests
{
    [Fact]
    public async Task TC_33_2_1_AI_interaction_log_stores_metadata_sources_output_actor_tenant_timestamp_and_context()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out _);

        var log = await service.LogInteractionAsync(CreateRequest(), ids.TenantId, ids.ActorUserId);

        Assert.Equal(ids.TenantId, log.TenantId);
        Assert.Equal(ids.ActorUserId, log.ActorUserId);
        Assert.Equal("prompt-metadata", log.PromptMetadata);
        Assert.Equal("gpt-test temperature=0", log.ModelConfiguration);
        Assert.Equal(["library-far-52-204-21"], log.RetrievedSourceIds);
        Assert.Equal("Draft answer with citation.", log.GeneratedOutput);
        Assert.Equal("report-draft", log.WorkflowContext);
        Assert.NotEqual(default, log.CreatedAt);
    }

    [Fact]
    public async Task TC_33_2_2_AI_output_remains_draft_until_human_approved_for_deliverables()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out _);
        var log = await service.LogInteractionAsync(CreateRequest(), ids.TenantId, ids.ActorUserId);

        await Assert.ThrowsAsync<AiOutputReviewException>(() =>
            service.EnsureApprovedForDeliverableAsync(log.Id, AiDeliverableType.Report));

        await service.ReviewAsync(log.Id, new AiOutputReviewDecisionRequest(AiOutputReviewState.Approved, "Approved.", null), ids.ReviewerUserId);

        await service.EnsureApprovedForDeliverableAsync(log.Id, AiDeliverableType.Report);
    }

    [Fact]
    public async Task TC_33_2_3_Approve_reject_supersede_and_archive_retain_reviewer_notes_reason_and_timestamp()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out _);
        var log = await service.LogInteractionAsync(CreateRequest(), ids.TenantId, ids.ActorUserId);

        var approved = await service.ReviewAsync(log.Id, new AiOutputReviewDecisionRequest(AiOutputReviewState.Approved, "Looks good.", null), ids.ReviewerUserId);
        var rejected = await service.ReviewAsync(log.Id, new AiOutputReviewDecisionRequest(AiOutputReviewState.Rejected, "Needs rewrite.", "Missing citation."), ids.ReviewerUserId);
        var superseded = await service.ReviewAsync(log.Id, new AiOutputReviewDecisionRequest(AiOutputReviewState.Superseded, "Newer answer exists.", null), ids.ReviewerUserId);
        var archived = await service.ReviewAsync(log.Id, new AiOutputReviewDecisionRequest(AiOutputReviewState.Archived, "Retention archive.", null), ids.ReviewerUserId);

        Assert.Equal(AiOutputReviewState.Approved, approved?.State);
        Assert.Equal(ids.ReviewerUserId, approved?.ReviewerUserId);
        Assert.NotNull(approved?.ReviewedAt);
        Assert.Equal("Missing citation.", rejected?.RejectionReason);
        Assert.Equal(AiOutputReviewState.Superseded, superseded?.State);
        Assert.Equal(AiOutputReviewState.Archived, archived?.State);
    }

    [Fact]
    public async Task TC_33_2_4_AI_logs_are_tenant_scoped_rbac_protected_and_follow_retention_and_data_rules()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out _);
        await service.LogInteractionAsync(CreateRequest(), ids.TenantId, ids.ActorUserId);
        await service.LogInteractionAsync(CreateRequest(), ids.OtherTenantId, ids.ActorUserId);

        var currentTenantLogs = await service.ListAsync(ids.TenantId, hasReviewPermission: true);
        var export = await service.ExportAsync(ids.TenantId, hasReviewPermission: true);

        Assert.Single(currentTenantLogs);
        Assert.True(export.RetainUntil > DateOnly.FromDateTime(DateTime.UtcNow.Date));
        await Assert.ThrowsAsync<AiOutputReviewException>(() => service.ListAsync(ids.TenantId, hasReviewPermission: false));
        await Assert.ThrowsAsync<AiOutputReviewException>(() =>
            service.LogInteractionAsync(CreateRequest() with { Classification = ContentClassification.Prohibited }, ids.TenantId, ids.ActorUserId));
    }

    [Fact]
    public async Task TC_33_2_5_AI_review_decisions_and_state_changes_are_audit_logged()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out var auditWriter);
        var log = await service.LogInteractionAsync(CreateRequest(), ids.TenantId, ids.ActorUserId);
        await service.ReviewAsync(log.Id, new AiOutputReviewDecisionRequest(AiOutputReviewState.Approved, "Approved.", null), ids.ReviewerUserId);

        var events = auditWriter.Events.Where(auditEvent => auditEvent.EntityType == "AiInteractionLog").ToArray();
        Assert.Equal(2, events.Length);
        Assert.Equal(AuditAction.Created, events[0].Action);
        Assert.Equal(AuditAction.Updated, events[1].Action);
        Assert.Equal("Approved", events[1].Metadata["state"]);
        Assert.Equal("library-far-52-204-21", events[1].Metadata["retrievedSources"]);
    }

    private static AiOutputReviewService CreateService(out CapturingAuditEventWriter auditWriter)
    {
        auditWriter = new CapturingAuditEventWriter();
        return new AiOutputReviewService(new InMemoryAiOutputReviewRepository(), auditWriter);
    }

    private static AiInteractionLogRequest CreateRequest() =>
        new(
            "Explain FAR 52.204-21.",
            "prompt-metadata",
            "gpt-test temperature=0",
            ["library-far-52-204-21"],
            "Draft answer with citation.",
            "report-draft",
            ContentClassification.Fci);

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
            Events.Add(new CapturedAuditEvent(
                tenantId,
                actorUserId,
                action,
                entityType,
                entityId,
                summary,
                metadata?.ToDictionary() ?? []));
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

    private sealed record StoryIds(Guid TenantId, Guid OtherTenantId, Guid ActorUserId, Guid ReviewerUserId)
    {
        public static StoryIds Create() =>
            new(
                Guid.Parse("33233233-3233-2332-3323-3233233233aa"),
                Guid.Parse("33233233-3233-2332-3323-3233233233bb"),
                Guid.Parse("33233233-3233-2332-3323-3233233233cc"),
                Guid.Parse("33233233-3233-2332-3323-3233233233dd"));
    }
}
