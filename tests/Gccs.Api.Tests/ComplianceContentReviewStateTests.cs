using Gccs.Application.Audit;
using Gccs.Application.Compliance;
using Gccs.Application.Notifications;
using Gccs.Application.Security;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Infrastructure.Compliance;
using Gccs.Infrastructure.Notifications;
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

    [Fact]
    public async Task TC_19_3_1_through_TC_19_3_5_Expert_review_queue_tracks_assignment_resolution_and_publication_block()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.Parse("19319319-3193-1931-9319-3193193193a1");
        var actorUserId = Guid.Parse("19319319-3193-1931-9319-3193193193b1");
        var expertUserId = Guid.Parse("19319319-3193-1931-9319-3193193193c1");
        var auditWriter = new CapturingAuditEventWriter();
        var notificationRepository = new EfAssignmentNotificationRepository(dbContext);
        var expertQueue = CreateExpertQueueService(dbContext, tenantId, actorUserId, auditWriter, notificationRepository);
        var suggestedObligationService = CreateSuggestedService(dbContext, tenantId, actorUserId, auditWriter);
        var suggestion = await suggestedObligationService.CreateAsync(CreateSuggestionRequest(), tenantId, actorUserId);
        var clauseCandidateId = Guid.Parse("19319319-3193-1931-9319-3193193193d1");
        dbContext.Set<ClauseCandidateEntity>().Add(new ClauseCandidateEntity
        {
            Id = clauseCandidateId,
            TenantId = tenantId,
            ExtractionJobId = Guid.Parse("19319319-3193-1931-9319-3193193193e1"),
            SourceDocumentId = Guid.Parse("19319319-3193-1931-9319-3193193193f1"),
            NormalizedCitation = "FAR 52.204-21",
            RawExtractedText = "FAR 52.204-21 - Basic Safeguarding.",
            Confidence = 0.7m,
            LocationMetadata = "line 1",
            MatchMethod = "ai_suggested",
            ReviewStatus = "pending_review",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var missingReason = await Assert.ThrowsAsync<ExpertReviewValidationException>(() =>
            expertQueue.EscalateAsync(
                new EscalateExpertReviewRequest("suggested_obligation", suggestion.Id, "", "high", "FAR interpretation", expertUserId, new DateOnly(2026, 7, 1)),
                tenantId,
                actorUserId));
        var suggestedEscalation = await expertQueue.EscalateAsync(
            new EscalateExpertReviewRequest(
                "suggested_obligation",
                suggestion.Id,
                "Low confidence and legal interpretation needed.",
                "high",
                "FAR 52.204-21 suggested obligation",
                expertUserId,
                new DateOnly(2026, 7, 1)),
            tenantId,
            actorUserId);
        var clauseEscalation = await expertQueue.EscalateAsync(
            new EscalateExpertReviewRequest(
                "clause_candidate",
                clauseCandidateId,
                "Candidate may conflict with flow-down attachment.",
                "medium",
                "Clause candidate conflict",
                null,
                null),
            tenantId,
            actorUserId);
        var queue = await expertQueue.ListAsync(new ExpertReviewQueueQuery("open", null, expertUserId, "high"));
        var notifications = await notificationRepository.ListCurrentUserAsync(tenantId, expertUserId);
        var blockedApproval = await suggestedObligationService.ApproveAsync(
            suggestion.Id,
            new SuggestedObligationReviewRequest("Approve before expert resolution.", ["https://www.acquisition.gov/far/52.204-21"]),
            actorUserId);
        var resolved = await expertQueue.ResolveAsync(
            suggestedEscalation.Id,
            new ResolveExpertReviewRequest("approve_after_revision", "Expert confirmed interpretation with minor wording edits."),
            expertUserId);
        var approved = await suggestedObligationService.ApproveAsync(
            suggestion.Id,
            new SuggestedObligationReviewRequest("Approved after expert resolution.", ["https://www.acquisition.gov/far/52.204-21"]),
            actorUserId);

        Assert.Contains("reason", missingReason.Errors.Keys);
        Assert.Equal(suggestedEscalation.Id, Assert.Single(queue).Id);
        var notification = Assert.Single(notifications);
        Assert.Equal("expert_review", notification.Category);
        Assert.Equal("ExpertReviewItem", notification.SourceType);
        Assert.Contains("FAR 52.204-21", notification.Placeholder, StringComparison.Ordinal);
        Assert.NotNull(clauseEscalation);
        var clauseCandidate = await dbContext.Set<ClauseCandidateEntity>().SingleAsync(candidate => candidate.Id == clauseCandidateId);
        Assert.Equal("needs_clarification", clauseCandidate.ReviewStatus);
        Assert.NotNull(blockedApproval);
        Assert.Equal("escalated", blockedApproval.ReviewStatus);
        Assert.NotNull(resolved);
        Assert.Equal("resolved", resolved.Status);
        Assert.Equal(expertUserId, resolved.ResolvedByUserId);
        Assert.NotNull(resolved.ResolvedAt);
        Assert.Equal("approve_after_revision", resolved.ResolutionDecision);
        Assert.Equal("Expert confirmed interpretation with minor wording edits.", resolved.ResolutionNotes);
        Assert.NotNull(approved);
        Assert.Equal("approved", approved.ReviewStatus);
        Assert.Contains(auditWriter.Events, auditEvent => auditEvent.EntityType == "ExpertReviewItem" && auditEvent.Action == AuditAction.Created);
        Assert.Contains(auditWriter.Events, auditEvent => auditEvent.EntityType == "ExpertReviewItem" && auditEvent.Action == AuditAction.Updated);
    }

    private static ComplianceContentReviewService CreateService(GccsDbContext dbContext, IAuditEventWriter auditWriter) =>
        new(new EfComplianceContentReviewRepository(dbContext), auditWriter);

    private static SuggestedObligationService CreateSuggestedService(
        GccsDbContext dbContext,
        Guid tenantId,
        Guid userId,
        IAuditEventWriter auditWriter) =>
        new(new EfSuggestedObligationRepository(dbContext, new TestTenantContext(tenantId, userId)), auditWriter);

    private static ExpertReviewQueueService CreateExpertQueueService(
        GccsDbContext dbContext,
        Guid tenantId,
        Guid userId,
        IAuditEventWriter auditWriter,
        IAssignmentNotificationRepository notificationRepository) =>
        new(
            new EfExpertReviewQueueRepository(dbContext, new TestTenantContext(tenantId, userId)),
            auditWriter,
            [notificationRepository]);

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
