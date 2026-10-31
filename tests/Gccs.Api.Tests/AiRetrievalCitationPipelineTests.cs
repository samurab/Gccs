using Gccs.Application.Ai;
using Gccs.Application.Audit;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Ai;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class AiRetrievalCitationPipelineTests
{
    [Fact]
    public async Task Retrieval_fails_closed_without_server_authorized_permission_context()
    {
        var ids = StoryIds.Create();
        var service = CreateService(ids, out var auditWriter);

        await Assert.ThrowsAsync<AiRetrievalPolicyException>(() => service.AnswerAsync(
            new AiAssistantQuestionRequest(ids.TenantId, ids.ActorUserId, "Explain FCI.", "obligation")));

        Assert.Empty(auditWriter.Events);
    }

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
        Assert.All(response.Answer.Split('\n', StringSplitOptions.RemoveEmptyEntries), statement =>
        {
            Assert.StartsWith("- [", statement, StringComparison.Ordinal);
            Assert.Contains(response.Citations, citation => statement.StartsWith($"- [{citation.SourceId}]", StringComparison.Ordinal));
        });
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

        var response = await service.AnswerAsync(CreateRequest(ids,
            "handling prohibited unknown unapproved CUI synthetic FCI"));

        Assert.DoesNotContain(response.Citations, citation => citation.SourceId is "prohibited-source" or "unknown-source" or "unapproved-source" or "cui-source");
        Assert.Contains(response.PolicyLogs, log => log.SourceId == "prohibited-source" && log.Decision == AiRetrievalPolicyDecision.Excluded);
        Assert.Contains(response.PolicyLogs, log => log.SourceId == "unknown-source" && log.Decision == AiRetrievalPolicyDecision.Excluded);
        Assert.Contains(response.PolicyLogs, log => log.SourceId == "unapproved-source" && log.Decision == AiRetrievalPolicyDecision.Excluded);
        Assert.Contains(response.PolicyLogs, log => log.SourceId == "[cross-tenant-redacted]" && log.Reason == "cross-tenant");
        Assert.Contains(response.PolicyLogs, log => log.SourceId == "synthetic-cui-source" && log.Reason == "unsafe-classification");
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

    [Fact]
    public async Task Tenant_source_family_requires_server_derived_source_access()
    {
        var ids = StoryIds.Create();
        var service = CreateService(ids, out _);

        var response = await service.AnswerAsync(new AiAssistantQuestionRequest(
            ids.TenantId,
            ids.ActorUserId,
            "Explain CMMC Level 1.",
            "obligation",
            HasAssistantPermission: true,
            SourceAccess: AiRetrievalSourceAccess.ComplianceLibrary));

        Assert.Equal("NeedsReview", response.Status);
        Assert.DoesNotContain(response.Citations, citation => citation.SourceId == "tenant-cmmc-l1");
        Assert.Contains(response.PolicyLogs, log => log.SourceId == "tenant-cmmc-l1" && log.Reason == "rbac-source-family");
    }

    [Fact]
    public async Task Audit_metadata_does_not_disclose_cross_tenant_identifiers_or_source_content()
    {
        var ids = StoryIds.Create();
        var service = CreateService(ids, out var auditWriter);

        await service.AnswerAsync(CreateRequest(ids, "Explain FCI safeguards."));

        var audit = Assert.Single(auditWriter.Events);
        var metadata = string.Join("|", audit.Metadata.Values);
        Assert.DoesNotContain("other-tenant-source", metadata, StringComparison.Ordinal);
        Assert.DoesNotContain("Other tenant content", metadata, StringComparison.Ordinal);
        Assert.Contains("[cross-tenant-redacted]", audit.Metadata["policyDecisions"], StringComparison.Ordinal);
        Assert.Equal(AiRetrievalAssistantService.PolicyVersion, audit.Metadata["policyVersion"]);
        Assert.Equal("deterministic-token-minimum-match-v1", audit.Metadata["retrievalStrategy"]);
        Assert.Equal("NoCui", audit.Metadata["tenantDataPosture"]);
    }

    [Fact]
    public async Task Demo_sandbox_excludes_tenant_FCI_that_lacks_demo_safe_provenance()
    {
        var ids = StoryIds.Create();
        var service = CreateService(ids, out _, TenantDataPosture.DemoSandbox);

        var response = await service.AnswerAsync(CreateRequest(ids, "Explain CMMC Level 1."));

        Assert.Equal("NeedsReview", response.Status);
        Assert.Contains(response.PolicyLogs, log => log.SourceId == "tenant-cmmc-l1" && log.Reason == "tenant-posture");
    }

    private static AiRetrievalAssistantService CreateService(
        StoryIds ids,
        out CapturingAuditEventWriter auditWriter,
        TenantDataPosture dataPosture = TenantDataPosture.NoCui)
    {
        auditWriter = new CapturingAuditEventWriter();
        var repository = new InMemoryAiRetrievalSourceRepository();
        repository.DataPosture = dataPosture;
        repository.Seed(
            Source("library-far-52-204-21", null, "FAR 52.204-21", "ComplianceLibrary", ContentClassification.Fci, true, true, "FCI systems require basic safeguarding controls.", ["fci", "far 52.204-21", "safeguards"]),
            Source("tenant-cmmc-l1", ids.TenantId, "Tenant CMMC Level 1 Notes", "TenantDocument", ContentClassification.Fci, true, false, "CMMC Level 1 readiness uses approved tenant notes.", ["cmmc", "level 1"]),
            Source("other-tenant-source", ids.OtherTenantId, "Other Tenant Evidence", "TenantDocument", ContentClassification.Fci, true, false, "Other tenant content must not be retrieved.", ["fci", "safeguards", "handling"]),
            Source("unapproved-source", ids.TenantId, "Draft Policy", "TenantDocument", ContentClassification.Fci, false, false, "Draft source.", ["unapproved", "handling"]),
            Source("prohibited-source", ids.TenantId, "Prohibited Data", "TenantDocument", ContentClassification.Prohibited, true, false, "Prohibited data.", ["prohibited", "handling"]),
            Source("unknown-source", ids.TenantId, "Unknown Classification", "TenantDocument", ContentClassification.Unknown, true, false, "Unknown data.", ["unknown", "handling"]),
            Source("cui-source", ids.TenantId, "CUI Source", "TenantDocument", ContentClassification.Cui, true, false, "CUI data.", ["cui", "handling"]),
            Source("synthetic-cui-source", ids.TenantId, "Synthetic CUI Source", "TenantDocument", ContentClassification.SyntheticCui, true, false, "Synthetic CUI data.", ["synthetic", "handling"]));
        return new AiRetrievalAssistantService(repository, auditWriter);
    }

    private static AiAssistantQuestionRequest CreateRequest(StoryIds ids, string question) =>
        new(ids.TenantId, ids.ActorUserId, question, "contract-intake", HasAssistantPermission: true,
            SourceAccess: AiRetrievalSourceAccess.All);

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
            keywords,
            library ? AiRetrievalSourceKind.ComplianceLibrary : AiRetrievalSourceKind.TenantDocument);

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
