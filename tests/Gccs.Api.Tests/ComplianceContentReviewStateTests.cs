using Gccs.Application.Audit;
using Gccs.Application.Compliance;
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

    private static ComplianceContentReviewService CreateService(GccsDbContext dbContext, IAuditEventWriter auditWriter) =>
        new(new EfComplianceContentReviewRepository(dbContext), auditWriter);

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
