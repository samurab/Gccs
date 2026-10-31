using Gccs.Application.Ai;
using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Infrastructure.Ai;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class AiOutputLoggingReviewTests
{
    [Fact]
    public async Task TC_33_2_1_interaction_metadata_is_retained_in_authoritative_answer()
    {
        var fixture = await Fixture.CreateAsync();
        var log = Assert.Single(await fixture.Service.ListAsync(fixture.TenantId, false));
        Assert.Equal(fixture.ActorId, log.ActorUserId);
        Assert.Equal("Explain FAR 52.204-21.", log.Prompt);
        Assert.Equal("{\"provider\":\"deterministic\"}", log.ModelConfiguration);
        Assert.Equal("source-1", Assert.Single(log.Citations).SourceId);
        Assert.Equal("report", log.WorkflowContext);
        Assert.Equal(AiOutputReviewState.Draft, log.ReviewState);
        Assert.True(log.RetainUntil > log.CreatedAt);
    }

    [Fact]
    public async Task TC_33_2_2_deliverable_gate_rejects_draft_and_accepts_current_approved_output()
    {
        var fixture = await Fixture.CreateAsync();
        var deliverableId = Guid.NewGuid().ToString();
        await Assert.ThrowsAsync<AiOutputReviewValidationException>(() => fixture.Service.LinkDeliverableAsync(
            fixture.Answer.Id, fixture.TenantId, new(AiDeliverableType.Report, deliverableId), fixture.ActorId));
        await fixture.Service.ReviewAsync(fixture.Answer.Id, fixture.TenantId,
            new(AiOutputReviewState.Approved, "Qualified reviewer verified the cited source.", null, 0), fixture.ReviewerId);
        var usage = await fixture.Service.LinkDeliverableAsync(fixture.Answer.Id, fixture.TenantId,
            new(AiDeliverableType.Report, deliverableId), fixture.ActorId);
        Assert.Equal(deliverableId, usage?.DeliverableId);
    }

    [Theory]
    [InlineData(AiOutputReviewState.Approved, null)]
    [InlineData(AiOutputReviewState.Rejected, "Citation does not support the conclusion.")]
    [InlineData(AiOutputReviewState.Superseded, null)]
    [InlineData(AiOutputReviewState.Archived, null)]
    public async Task TC_33_2_3_review_decisions_append_history(AiOutputReviewState state, string? reason)
    {
        var fixture = await Fixture.CreateAsync();
        var result = await fixture.Service.ReviewAsync(fixture.Answer.Id, fixture.TenantId,
            new(state, "Reviewer note retained.", reason, 0), fixture.ReviewerId);
        Assert.Equal(state, result?.Answer.ReviewState);
        Assert.Equal(fixture.ReviewerId, result?.Review.ReviewerUserId);
        Assert.Equal(reason, result?.Review.RejectionReason);
        Assert.Single(await fixture.Service.HistoryAsync(fixture.Answer.Id, fixture.TenantId));
    }

    [Fact]
    public async Task TC_33_2_4_logs_are_tenant_scoped_and_prohibited_review_text_is_rejected()
    {
        var fixture = await Fixture.CreateAsync();
        Assert.Empty(await fixture.Service.ListAsync(Guid.NewGuid(), false));
        await Assert.ThrowsAsync<AiOutputReviewValidationException>(() => fixture.Service.ReviewAsync(
            fixture.Answer.Id, fixture.TenantId,
            new(AiOutputReviewState.Approved, "Paste CUI here for analysis.", null, 0), fixture.ReviewerId));
        Assert.Empty(await fixture.Service.HistoryAsync(fixture.Answer.Id, fixture.TenantId));
    }

    [Fact]
    public async Task TC_33_2_5_review_and_deliverable_use_are_audited()
    {
        var fixture = await Fixture.CreateAsync();
        await fixture.Service.ReviewAsync(fixture.Answer.Id, fixture.TenantId,
            new(AiOutputReviewState.Approved, "Approved after source review.", null, 0), fixture.ReviewerId);
        await fixture.Service.LinkDeliverableAsync(fixture.Answer.Id, fixture.TenantId,
            new(AiDeliverableType.Policy, Guid.NewGuid().ToString()), fixture.ActorId);
        Assert.Contains(fixture.Audit.Events, x => x.EntityType == "AiInteractionLog" && x.Metadata["state"] == "Approved");
        Assert.Contains(fixture.Audit.Events, x => x.EntityType == "AiOutputUsage");
    }

    [Fact]
    public async Task Expired_outputs_are_archived_in_bounded_batches_and_audited()
    {
        var fixture = await Fixture.CreateAsync();
        var retention = new CapturingRetentionRepository(fixture.TenantId, fixture.Answer.Id);
        var service = new AiOutputReviewService(fixture.Repository, fixture.Audit,
            new PassthroughTransaction(), TimeProvider.System, retention);
        Assert.Equal(1, await service.ArchiveExpiredAsync(100));
        Assert.Equal(100, retention.BatchSize);
        Assert.Contains(fixture.Audit.Events, x => x.EntityType == "AiInteractionLog" && x.Metadata["result"] == "retention-archived");
    }

    private sealed class Fixture
    {
        public Guid TenantId { get; } = Guid.NewGuid();
        public Guid ActorId { get; } = Guid.NewGuid();
        public Guid ReviewerId { get; } = Guid.NewGuid();
        public InMemoryGuardedAssistantRepository Repository { get; } = new();
        public CapturingAuditWriter Audit { get; } = new();
        public AiOutputReviewService Service { get; private set; } = null!;
        public GuardedAssistantAnswerDto Answer { get; private set; } = null!;

        public static async Task<Fixture> CreateAsync()
        {
            var value = new Fixture();
            value.Service = new(value.Repository, value.Audit, new PassthroughTransaction(), TimeProvider.System);
            value.Answer = new(Guid.NewGuid(), value.TenantId, "report", "Draft", "Draft output.",
                [new("source-1", "FAR source", "ComplianceLibrary", "https://example.test", null, "section", "1", null)],
                "SourceSupported", "Draft", true, false, null, DateTimeOffset.UtcNow, "pending", null, null, null, null,
                "Explain FAR 52.204-21.", false, "{\"length\":24}", "{\"provider\":\"deterministic\"}", "[]",
                ContentClassification.Unclassified, "Draft", AiOutputReviewState.Draft, null, DateTimeOffset.UtcNow.AddDays(365), 0, value.ActorId);
            await value.Repository.SaveAnswerAsync(value.Answer, value.ActorId);
            return value;
        }
    }

    private sealed class PassthroughTransaction : IApplicationTransaction
    {
        public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) => operation(cancellationToken);
    }

    private sealed class CapturingAuditWriter : IAuditEventWriter
    {
        public List<CapturedAudit> Events { get; } = [];
        public Task WriteAsync(Guid tenantId, Guid actorUserId, AuditAction action, string entityType, string entityId,
            string summary, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        {
            Events.Add(new(entityType, metadata ?? new Dictionary<string, string>()));
            return Task.CompletedTask;
        }
    }
    private sealed class CapturingRetentionRepository(Guid tenantId, Guid answerId) : IAiOutputRetentionRepository
    {
        public int BatchSize { get; private set; }
        public Task<IReadOnlyList<AiOutputRetentionArchiveDto>> ArchiveExpiredAsync(DateTimeOffset now, int batchSize,
            CancellationToken cancellationToken = default)
        {
            BatchSize = batchSize;
            return Task.FromResult<IReadOnlyList<AiOutputRetentionArchiveDto>>(
                [new(tenantId, answerId, AiOutputReviewState.Draft)]);
        }
    }
    private sealed record CapturedAudit(string EntityType, IReadOnlyDictionary<string, string> Metadata);
}
