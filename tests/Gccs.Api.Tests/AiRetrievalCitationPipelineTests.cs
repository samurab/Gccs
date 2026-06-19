using Gccs.Application.Ai;
using Gccs.Application.Audit;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Infrastructure.Ai;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class AiRetrievalCitationPipelineTests
{
    [Fact]
    public async Task TC_33_1_1_Retrieval_limited_to_current_tenant_and_approved_library_content()
    {
        var ids = StoryIds.Create();
        var service = CreateService(ids, out _);

        var response = await service.AnswerAsync(CreateRequest(ids, "What does FAR 52.204-21 require for FCI?"));

        Assert.Equal("Draft", response.Status);
        Assert.Contains(response.Citations, citation => citation.SourceId == "library-far-52-204-21");
        Assert.DoesNotContain(response.Citations, citation => citation.SourceId == "other-tenant-source");
    }

    [Fact]
    public async Task TC_33_1_2_Substantive_answer_statements_include_citation_metadata()
    {
        var ids = StoryIds.Create();
        var service = CreateService(ids, out _);

        var response = await service.AnswerAsync(CreateRequest(ids, "Explain CMMC Level 1 and FCI safeguards."));

        Assert.NotEmpty(response.Citations);
        Assert.All(response.Citations, citation =>
        {
            Assert.False(string.IsNullOrWhiteSpace(citation.SourceId));
            Assert.False(string.IsNullOrWhiteSpace(citation.Title));
            Assert.False(string.IsNullOrWhiteSpace(citation.SourceType));
            Assert.False(string.IsNullOrWhiteSpace(citation.ExcerptPointer));
            Assert.False(string.IsNullOrWhiteSpace(citation.Version));
        });
        Assert.Contains("[", response.Answer, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TC_33_1_3_No_approved_source_refuses_or_routes_to_review()
    {
        var ids = StoryIds.Create();
        var service = CreateService(ids, out _);

        var response = await service.AnswerAsync(CreateRequest(ids, "What is the approved answer for unsupported export control?"));

        Assert.Equal("NeedsReview", response.Status);
        Assert.Empty(response.Citations);
        Assert.Contains("human review", response.Answer, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_33_1_4_Unsafe_sources_are_excluded()
    {
        var ids = StoryIds.Create();
        var service = CreateService(ids, out _);

        var response = await service.AnswerAsync(CreateRequest(ids, "Tell me about prohibited unknown unapproved CUI source handling."));

        Assert.DoesNotContain(response.Citations, citation => citation.SourceId is "prohibited-source" or "unknown-source" or "unapproved-source" or "cui-source");
        Assert.Contains(response.PolicyLogs, log => log.SourceId == "prohibited-source" && log.Decision == AiRetrievalPolicyDecision.Excluded);
        Assert.Contains(response.PolicyLogs, log => log.SourceId == "unknown-source" && log.Decision == AiRetrievalPolicyDecision.Excluded);
        Assert.Contains(response.PolicyLogs, log => log.SourceId == "unapproved-source" && log.Decision == AiRetrievalPolicyDecision.Excluded);
        Assert.Contains(response.PolicyLogs, log => log.SourceId == "other-tenant-source" && log.Reason == "cross-tenant");
    }

    [Fact]
    public async Task TC_33_1_5_Retrieval_source_ids_policy_tenant_actor_and_context_are_logged()
    {
        var ids = StoryIds.Create();
        var service = CreateService(ids, out var auditWriter);

        var response = await service.AnswerAsync(CreateRequest(ids, "What does FAR 52.204-21 require for FCI?"));

        var audit = Assert.Single(auditWriter.Events);
        Assert.Equal(ids.TenantId, audit.TenantId);
        Assert.Equal(ids.ActorUserId, audit.ActorUserId);
        Assert.Equal("AiRetrieval", audit.EntityType);
        Assert.Equal("contract-intake", audit.Metadata["workflowContext"]);
        Assert.Contains("library-far-52-204-21", audit.Metadata["retrievedSourceIds"], StringComparison.Ordinal);
        Assert.Contains("approved-source", audit.Metadata["policyDecisions"], StringComparison.Ordinal);
        Assert.Equal("Draft", response.Status);
    }

    private static AiRetrievalAssistantService CreateService(StoryIds ids, out CapturingAuditEventWriter auditWriter)
    {
        auditWriter = new CapturingAuditEventWriter();
        var repository = new InMemoryAiRetrievalSourceRepository();
        repository.Seed(
            Source("library-far-52-204-21", null, "FAR 52.204-21", "ComplianceLibrary", ContentClassification.Fci, true, true, "FCI systems require basic safeguarding controls.", ["fci", "far 52.204-21", "safeguards"]),
            Source("tenant-cmmc-l1", ids.TenantId, "Tenant CMMC Level 1 Notes", "TenantDocument", ContentClassification.Fci, true, false, "CMMC Level 1 readiness uses approved tenant notes.", ["cmmc", "level 1"]),
            Source("other-tenant-source", ids.OtherTenantId, "Other Tenant Evidence", "TenantDocument", ContentClassification.Fci, true, false, "Other tenant content must not be retrieved.", ["fci"]),
            Source("unapproved-source", ids.TenantId, "Draft Policy", "TenantDocument", ContentClassification.Fci, false, false, "Draft source.", ["unapproved"]),
            Source("prohibited-source", ids.TenantId, "Prohibited Data", "TenantDocument", ContentClassification.Prohibited, true, false, "Prohibited data.", ["prohibited"]),
            Source("unknown-source", ids.TenantId, "Unknown Classification", "TenantDocument", ContentClassification.Unknown, true, false, "Unknown data.", ["unknown"]),
            Source("cui-source", ids.TenantId, "CUI Source", "TenantDocument", ContentClassification.Cui, true, false, "CUI data.", ["cui"]));
        return new AiRetrievalAssistantService(repository, auditWriter);
    }

    private static AiAssistantQuestionRequest CreateRequest(StoryIds ids, string question) =>
        new(ids.TenantId, ids.ActorUserId, question, "contract-intake");

    private static AiRetrievalSourceDto Source(
        string id,
        Guid? tenantId,
        string title,
        string sourceType,
        ContentClassification classification,
        bool approved,
        bool library,
        string summary,
        IReadOnlyList<string> keywords) =>
        new(
            id,
            tenantId,
            title,
            sourceType,
            "https://example.test/source",
            tenantId.HasValue ? $"tenant-record:{id}" : null,
            "section-1",
            "2026.06",
            new DateOnly(2026, 6, 1),
            classification,
            approved,
            library,
            summary,
            keywords);

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

    private sealed record StoryIds(Guid TenantId, Guid OtherTenantId, Guid ActorUserId)
    {
        public static StoryIds Create() =>
            new(
                Guid.Parse("33133133-3133-1331-3313-3133133133aa"),
                Guid.Parse("33133133-3133-1331-3313-3133133133bb"),
                Guid.Parse("33133133-3133-1331-3313-3133133133cc"));
    }
}
