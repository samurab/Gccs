using Gccs.Application.Audit;
using Gccs.Application.Compliance;
using Gccs.Application.Security;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Infrastructure.Compliance;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ComplianceContentReviewStateTests
{
    [Fact]
    public async Task TC_6_3_1_Draft_content_is_hidden_from_customer_facing_obligation_views()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Obligations.AddRange(
            CreateObligation("published-obligation", ReviewState.Published),
            CreateObligation("draft-obligation", ReviewState.Draft));
        await dbContext.SaveChangesAsync();
        var repository = new EfObligationRepository(dbContext);

        var obligations = await repository.ListAsync();
        var draft = await repository.FindByIdAsync("draft-obligation");
        var published = await repository.FindByIdAsync("published-obligation");

        Assert.Single(obligations);
        Assert.Equal("published-obligation", obligations[0].Id);
        Assert.Null(draft);
        Assert.NotNull(published);
    }

    [Fact]
    public async Task TC_6_3_2_Expert_review_required_content_cannot_publish_without_reviewer_and_date()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Obligations.Add(CreateObligation("expert-obligation", ReviewState.Approved, requiresExpertReview: true));
        await dbContext.SaveChangesAsync();
        var auditWriter = new CapturingAuditEventWriter();
        var service = CreateService(dbContext, auditWriter);

        var exception = await Assert.ThrowsAsync<ComplianceContentReviewException>(() =>
            service.ChangeObligationStateAsync(
                "expert-obligation",
                new ChangeComplianceContentReviewStateRequest(ReviewState.Published, null, null),
                Guid.NewGuid(),
                Guid.NewGuid()));

        Assert.Contains("reviewerUserId", exception.Message, StringComparison.OrdinalIgnoreCase);
        var obligation = await dbContext.Obligations.SingleAsync(obligation => obligation.Id == "expert-obligation");
        Assert.Equal(ReviewState.Approved, obligation.ReviewState);
        Assert.Empty(auditWriter.Events);
    }

    [Fact]
    public async Task TC_6_3_3_Retired_content_is_excluded_from_new_mappings()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Obligations.Add(CreateObligation("retire-me", ReviewState.Published));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, new CapturingAuditEventWriter());

        var before = await service.CanUseObligationForNewMappingAsync("retire-me");
        await service.ChangeObligationStateAsync(
            "retire-me",
            new ChangeComplianceContentReviewStateRequest(ReviewState.Retired, null, null),
            Guid.NewGuid(),
            Guid.NewGuid());
        var after = await service.CanUseObligationForNewMappingAsync("retire-me");
        var customerFacingRepository = new EfObligationRepository(dbContext);

        Assert.True(before);
        Assert.False(after);
        Assert.Null(await customerFacingRepository.FindByIdAsync("retire-me"));
    }

    [Fact]
    public async Task TC_6_3_4_State_changes_through_workflow_are_audit_logged()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Obligations.Add(CreateObligation("workflow-obligation", ReviewState.Draft, requiresExpertReview: true));
        await dbContext.SaveChangesAsync();
        var auditWriter = new CapturingAuditEventWriter();
        var service = CreateService(dbContext, auditWriter);
        var tenantId = Guid.Parse("63636363-6363-6363-6363-6363636363a4");
        var actorUserId = Guid.Parse("63636363-6363-6363-6363-6363636363b4");
        var reviewerUserId = Guid.Parse("63636363-6363-6363-6363-6363636363c4");
        var reviewedAt = new DateOnly(2026, 6, 15);

        await service.ChangeObligationStateAsync(
            "workflow-obligation",
            new ChangeComplianceContentReviewStateRequest(ReviewState.InReview, null, null),
            tenantId,
            actorUserId);
        await service.ChangeObligationStateAsync(
            "workflow-obligation",
            new ChangeComplianceContentReviewStateRequest(ReviewState.Approved, reviewerUserId, reviewedAt),
            tenantId,
            actorUserId);
        await service.ChangeObligationStateAsync(
            "workflow-obligation",
            new ChangeComplianceContentReviewStateRequest(ReviewState.Published, reviewerUserId, reviewedAt),
            tenantId,
            actorUserId);
        await service.ChangeObligationStateAsync(
            "workflow-obligation",
            new ChangeComplianceContentReviewStateRequest(ReviewState.Retired, null, null),
            tenantId,
            actorUserId);

        Assert.Equal(
            [ReviewState.InReview, ReviewState.Approved, ReviewState.Published, ReviewState.Retired],
            auditWriter.Events.Select(auditEvent => Enum.Parse<ReviewState>(auditEvent.Metadata["afterState"])).ToArray());
        Assert.All(auditWriter.Events, auditEvent =>
        {
            Assert.Equal(tenantId, auditEvent.TenantId);
            Assert.Equal(actorUserId, auditEvent.ActorUserId);
            Assert.Equal(AuditAction.Updated, auditEvent.Action);
            Assert.Equal("Obligation", auditEvent.EntityType);
            Assert.Equal("workflow-obligation", auditEvent.EntityId);
        });
        Assert.Contains(auditWriter.Events, auditEvent => auditEvent.Metadata["reviewerUserId"] == reviewerUserId.ToString());
    }

    [Fact]
    public async Task TC_19_2_1_and_TC_19_2_2_Draft_suggestions_store_ai_metadata_and_are_excluded_from_approved_obligations()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Obligations.Add(CreateObligation("published-obligation", ReviewState.Published));
        await dbContext.SaveChangesAsync();
        var tenantId = Guid.Parse("19219219-2192-1921-9219-2192192192a1");
        var actorUserId = Guid.Parse("19219219-2192-1921-9219-2192192192b1");
        var auditWriter = new CapturingAuditEventWriter();
        var service = CreateSuggestedService(dbContext, tenantId, actorUserId, auditWriter);

        var suggestion = await service.CreateAsync(CreateSuggestionRequest(), tenantId, actorUserId);
        var customerFacingRepository = new EfObligationRepository(dbContext);
        var approvedObligations = await customerFacingRepository.ListAsync();
        var draftSuggestions = await service.ListAsync("draft");

        Assert.Equal("draft", suggestion.ReviewStatus);
        Assert.Equal("medium", suggestion.Confidence);
        Assert.Equal("prompt-19.2-v1", suggestion.PromptVersion);
        Assert.Equal("gpt-test", suggestion.ModelIdentifier);
        Assert.Contains("https://www.acquisition.gov/far/52.204-21", suggestion.SourceCitations);
        Assert.Contains("retrieval:far-52.204-21", suggestion.RetrievedSourceReferences);
        Assert.Equal("published-obligation", Assert.Single(approvedObligations).Id);
        Assert.Equal(suggestion.Id, Assert.Single(draftSuggestions).Id);
        Assert.Contains(auditWriter.Events, auditEvent => auditEvent.Action == AuditAction.Created && auditEvent.Metadata["reviewStatus"] == "draft");
    }

    [Fact]
    public async Task TC_19_2_3_TC_19_2_4_and_TC_19_2_5_Reviewer_can_revise_approve_reject_and_escalate_suggestions()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.Parse("19219219-2192-1921-9219-2192192192a3");
        var actorUserId = Guid.Parse("19219219-2192-1921-9219-2192192192b3");
        var auditWriter = new CapturingAuditEventWriter();
        var service = CreateSuggestedService(dbContext, tenantId, actorUserId, auditWriter);
        var approveSuggestion = await service.CreateAsync(CreateSuggestionRequest(), tenantId, actorUserId);
        var rejectSuggestion = await service.CreateAsync(CreateSuggestionRequest() with { ProposedTitle = "Reject me" }, tenantId, actorUserId);
        var escalateSuggestion = await service.CreateAsync(CreateSuggestionRequest() with { ProposedTitle = "Escalate me" }, tenantId, actorUserId);

        var revised = await service.ReviseAsync(
            approveSuggestion.Id,
            new ReviseSuggestedObligationRequest(
                "Revised draft summary.",
                "Revised safeguarding obligation",
                "IT/security",
                "Apply the basic safeguarding controls and collect evidence.",
                RiskLevel.High,
                ["MFA configuration", "Access review"],
                ["https://www.acquisition.gov/far/52.204-21"],
                "high",
                ["retrieval:far-52.204-21"]),
            actorUserId);
        var approved = await service.ApproveAsync(
            approveSuggestion.Id,
            new SuggestedObligationReviewRequest("SME approved source-backed wording.", ["https://www.acquisition.gov/far/52.204-21"]),
            actorUserId);
        var rejected = await service.RejectAsync(
            rejectSuggestion.Id,
            new SuggestedObligationReviewRequest("Incorrect applicability.", ["https://www.acquisition.gov/far/52.204-21"]),
            actorUserId);
        var escalated = await service.EscalateAsync(
            escalateSuggestion.Id,
            new SuggestedObligationReviewRequest("Needs legal interpretation.", ["https://www.acquisition.gov/far/52.204-21"]),
            actorUserId);
        var rejectedHistory = await service.FindAsync(rejectSuggestion.Id);

        Assert.NotNull(revised);
        Assert.Equal("draft", revised.ReviewStatus);
        Assert.Equal("Revised safeguarding obligation", revised.ProposedTitle);
        Assert.NotNull(approved);
        Assert.Equal("approved", approved.ReviewStatus);
        Assert.Equal(actorUserId, approved.ReviewedByUserId);
        Assert.NotNull(approved.ReviewedAt);
        Assert.Contains("https://www.acquisition.gov/far/52.204-21", approved.SourceCitations);
        Assert.NotNull(rejected);
        Assert.Equal("rejected", rejected.ReviewStatus);
        Assert.Equal("Incorrect applicability.", rejected.ReviewReason);
        Assert.Equal("rejected", rejectedHistory?.ReviewStatus);
        Assert.NotNull(escalated);
        Assert.Equal("escalated", escalated.ReviewStatus);
        Assert.Contains(auditWriter.Events, auditEvent => auditEvent.Action == AuditAction.Approved && auditEvent.Metadata["reviewStatus"] == "approved");
        Assert.Contains(auditWriter.Events, auditEvent => auditEvent.Action == AuditAction.Rejected && auditEvent.Metadata["reviewStatus"] == "rejected");
        Assert.Contains(auditWriter.Events, auditEvent => auditEvent.Summary.Contains("escalated", StringComparison.OrdinalIgnoreCase));
    }

    private static ComplianceContentReviewService CreateService(GccsDbContext dbContext, IAuditEventWriter auditWriter) =>
        new(new EfComplianceContentReviewRepository(dbContext), auditWriter);

    private static SuggestedObligationService CreateSuggestedService(
        GccsDbContext dbContext,
        Guid tenantId,
        Guid userId,
        IAuditEventWriter auditWriter) =>
        new(new EfSuggestedObligationRepository(dbContext, new TestTenantContext(tenantId, userId)), auditWriter);

    private static GccsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GccsDbContext>()
            .UseInMemoryDatabase($"compliance-content-review-{Guid.NewGuid():N}")
            .Options;

        return new GccsDbContext(options);
    }

    private static ObligationEntity CreateObligation(
        string id,
        ReviewState reviewState,
        bool requiresExpertReview = false) =>
        new()
        {
            Id = id,
            Source = "FAR 52.204-21",
            Title = $"Title for {id}",
            PlainEnglishSummary = $"Summary for {id}",
            TriggerCondition = "Contract involves FCI.",
            RequiredAction = "Apply baseline safeguards and retain evidence.",
            OwnerFunction = "IT/security",
            RiskLevel = RiskLevel.High,
            RequiresFlowDown = true,
            FlowDownRequirement = "Flow down when subcontractors may handle FCI.",
            ApplicabilityJson = """
                {
                  "appliesTo": ["prime"],
                  "contractTypes": ["federal contract"],
                  "dataTypes": ["FCI"]
                }
                """,
            EvidenceExamplesJson = """
                [
                  {
                    "name": "Access control policy",
                    "description": "Policy describing authorized access.",
                    "owner": "IT/security"
                  }
                ]
                """,
            SourceName = "FAR 52.204-21",
            SourceUrl = "https://www.acquisition.gov/far/52.204-21",
            SourceLastReviewedAt = new DateOnly(2026, 6, 3),
            SourceConfidence = "high",
            SourceRequiresExpertReview = requiresExpertReview,
            LastReviewedAt = new DateOnly(2026, 6, 3),
            Confidence = "high",
            RequiresExpertReview = requiresExpertReview,
            ReviewState = reviewState
        };

    private static CreateSuggestedObligationRequest CreateSuggestionRequest() =>
        new(
            "FAR 52.204-21",
            "https://www.acquisition.gov/far/52.204-21",
            "AI draft summary for safeguarding FCI.",
            "Safeguard Federal Contract Information",
            "IT/security",
            "Apply baseline safeguarding controls.",
            RiskLevel.High,
            ["Access control policy", "MFA configuration"],
            ["https://www.acquisition.gov/far/52.204-21"],
            "medium",
            "prompt-19.2-v1",
            "gpt-test",
            ["retrieval:far-52.204-21"]);

    private sealed record TestTenantContext(Guid TenantId, Guid UserId) : ICurrentTenantContext
    {
        public string UserEmail => "reviewer@example.com";
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
            Events.Add(new CapturedAuditEvent(
                tenantId,
                actorUserId,
                action,
                entityType,
                entityId,
                summary,
                metadata ?? new Dictionary<string, string>()));

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
}
