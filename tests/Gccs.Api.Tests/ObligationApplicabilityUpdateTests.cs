using Gccs.Application.Audit;
using Gccs.Application.Compliance;
using Gccs.Application.Security;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Domain.Companies;
using Gccs.Domain.Compliance;
using Gccs.Domain.Contracts;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Compliance;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ObligationApplicabilityUpdateTests
{
    private static readonly Guid TenantId = Guid.Parse("21321321-3213-2132-1321-3213213213a1");
    private static readonly Guid OtherTenantId = Guid.Parse("21321321-3213-2132-1321-3213213213b1");
    private static readonly Guid ActorUserId = Guid.Parse("21321321-3213-2132-1321-3213213213c1");

    [Fact]
    public async Task TC_21_3_1_TC_21_3_4_and_TC_21_3_5_Reevaluates_retains_history_and_audits_material_change()
    {
        await using var dbContext = CreateDbContext();
        var ids = SeedScenario(dbContext, TenantId, "FciOnly");
        await dbContext.SaveChangesAsync();
        var auditWriter = new CapturingAuditEventWriter();
        var service = CreateService(dbContext, TenantId, auditWriter);

        var first = await service.ReevaluateAsync(ids.ContractClauseId, ids.ObligationId, FciRule(), ActorUserId);
        var contract = await dbContext.Contracts.SingleAsync(contract => contract.Id == ids.ContractId);
        contract.DataHandlingPosture = DataHandlingPosture.Unknown;
        contract.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();
        var second = await service.ReevaluateAsync(ids.ContractClauseId, ids.ObligationId, FciRule(), ActorUserId);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(ApplicabilityRuleResultState.Applicable.ToString(), first.State);
        Assert.Equal(ApplicabilityRuleResultState.InsufficientInformation.ToString(), second.State);
        Assert.Equal(first.Id, second.PreviousEvaluationId);
        Assert.Equal(2, await dbContext.ObligationApplicabilityEvaluations.CountAsync());
        Assert.Empty(auditWriter.Events);

        contract.DataHandlingPosture = DataHandlingPosture.FciOnly;
        await dbContext.SaveChangesAsync();
        var restored = await service.ReevaluateAsync(ids.ContractClauseId, ids.ObligationId, FciRule(), ActorUserId);

        Assert.NotNull(restored);
        Assert.Equal(ApplicabilityRuleResultState.Applicable.ToString(), restored.State);
        Assert.Empty(auditWriter.Events);

        var reviewRule = FciRule(requiresExpertReview: true);
        var third = await service.ReevaluateAsync(ids.ContractClauseId, ids.ObligationId, reviewRule, ActorUserId);

        Assert.NotNull(third);
        Assert.Equal(ApplicabilityRuleResultState.NeedsReview.ToString(), third.State);
        var audit = Assert.Single(auditWriter.Events);
        Assert.Equal("ObligationApplicability", audit.EntityType);
        Assert.Equal(ApplicabilityRuleResultState.Applicable.ToString(), audit.Metadata["previousState"]);
        Assert.Equal(ApplicabilityRuleResultState.NeedsReview.ToString(), audit.Metadata["state"]);
    }

    [Fact]
    public async Task TC_21_3_2_and_TC_21_3_3_Dashboard_and_detail_show_current_state_explanation_and_facts()
    {
        await using var dbContext = CreateDbContext();
        var ids = SeedScenario(dbContext, TenantId, "FciOnly");
        await dbContext.SaveChangesAsync();
        var tenantContext = new FixedTenantContext(TenantId);
        var service = CreateService(dbContext, TenantId, new CapturingAuditEventWriter());
        await service.ReevaluateAsync(ids.ContractClauseId, ids.ObligationId, FciRule(), ActorUserId);

        var dashboard = await new EfObligationDashboardRepository(dbContext, tenantContext)
            .ListCurrentTenantAsync(new ObligationDashboardQuery(null, null, null, null, null, null, null));
        var detail = await new EfObligationDetailRepository(dbContext, tenantContext)
            .FindCurrentTenantAsync(ids.ContractClauseId, ids.ObligationId);

        var dashboardItem = Assert.Single(dashboard);
        Assert.NotNull(dashboardItem.Applicability);
        Assert.Equal(ApplicabilityRuleResultState.Applicable.ToString(), dashboardItem.Applicability.State);
        Assert.Equal("far-52-204-21-fci", dashboardItem.Applicability.SourceRuleId);
        Assert.Contains("contract.data_type=FciOnly", dashboardItem.Applicability.FactsUsed);
        Assert.NotNull(detail?.Detail.Applicability);
        Assert.Equal("far-52-204-21-fci", detail.Detail.Applicability.SourceRuleId);
        Assert.Contains("matched all required conditions", detail.Detail.Applicability.Explanation, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TC_21_3_3_Missing_facts_are_exposed_and_TC_21_3_Tenant_scope_is_enforced()
    {
        await using var dbContext = CreateDbContext();
        var ids = SeedScenario(dbContext, TenantId, "Unknown");
        SeedScenario(dbContext, OtherTenantId, "FciOnly");
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, TenantId, new CapturingAuditEventWriter());

        var result = await service.ReevaluateAsync(ids.ContractClauseId, ids.ObligationId, FciRule(), ActorUserId);
        var otherTenantEvaluationCount = await dbContext.ObligationApplicabilityEvaluations.CountAsync(evaluation => evaluation.TenantId == OtherTenantId);

        Assert.NotNull(result);
        Assert.Equal(ApplicabilityRuleResultState.InsufficientInformation.ToString(), result.State);
        Assert.Contains("contract.data_type", result.MissingFacts);
        Assert.Equal(TenantId, result.TenantId);
        Assert.Equal(0, otherTenantEvaluationCount);
    }

    private static ObligationApplicabilityService CreateService(
        GccsDbContext dbContext,
        Guid tenantId,
        IAuditEventWriter auditWriter) =>
        new(
            new EfObligationApplicabilityRepository(
                dbContext,
                new FixedTenantContext(tenantId),
                new EfApplicabilityFactRepository(dbContext)),
            auditWriter);

    private static ApplicabilityRuleDefinition FciRule(bool requiresExpertReview = false) =>
        new(
            "far-52-204-21-fci",
            null,
            "far-52-204-21-obligation",
            "FAR 52.204-21 FCI applicability",
            new ApplicabilityRuleMetadata(
                "FAR 52.204-21",
                "https://www.acquisition.gov/far/52.204-21",
                "high",
                new DateOnly(2025, 11, 10),
                new DateOnly(2026, 6, 17),
                ActorUserId,
                ReviewState.Published,
                requiresExpertReview),
            [
                new ApplicabilityRuleCondition("contract.data_type", ApplicabilityRuleOperator.AnyOf, ["FciOnly", "FciAndCui"]),
                new ApplicabilityRuleCondition("clause.citation", ApplicabilityRuleOperator.Equals, ["52.204-21"])
            ]);

    private static ScenarioIds SeedScenario(GccsDbContext dbContext, Guid tenantId, string dataHandlingPosture)
    {
        var contractId = Guid.NewGuid();
        var contractClauseId = Guid.NewGuid();
        var obligationId = $"far-52-204-21-obligation-{tenantId:N}";
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = $"Tenant {tenantId:N}",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.CompanyProfiles.Add(new CompanyProfileEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LegalEntityName = "Applicability Co",
            ContractorRole = ContractorRole.Prime,
            EmployeeRange = CompanyRange.Small,
            RevenueRange = CompanyRange.Small,
            DataHandlingPosture = DataHandlingPosture.FciOnly,
            AgencyCustomersJson = "[]",
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Contracts.Add(new ContractEntity
        {
            Id = contractId,
            TenantId = tenantId,
            ContractNumber = $"APP-{tenantId:N}"[..12],
            Title = "Applicability contract",
            AgencyOrPrimeName = "Defense Logistics Agency",
            Relationship = ContractorRelationship.Prime,
            Kind = ContractKind.FixedPrice,
            Status = ContractStatus.Active,
            PeriodOfPerformanceStart = new DateOnly(2026, 7, 1),
            PeriodOfPerformanceEnd = new DateOnly(2027, 6, 30),
            PlaceOfPerformance = "Virginia",
            DataHandlingPosture = Enum.Parse<DataHandlingPosture>(dataHandlingPosture),
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Set<ContractClauseEntity>().Add(new ContractClauseEntity
        {
            Id = contractClauseId,
            ContractId = contractId,
            ClauseLibraryId = "far-52-204-21",
            ClauseNumber = "52.204-21",
            Title = "Basic Safeguarding",
            Source = ClauseSource.Far,
            SourceUrl = "https://www.acquisition.gov/far/52.204-21",
            AttachmentReason = "Applicability test.",
            LastReviewedAt = new DateOnly(2026, 6, 17),
            Confidence = "high",
            ReviewState = ReviewState.Published,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Obligations.Add(new ObligationEntity
        {
            Id = obligationId,
            Source = "FAR 52.204-21",
            Title = "Safeguard FCI",
            PlainEnglishSummary = "Apply basic safeguarding controls.",
            TriggerCondition = "Contract involves FCI.",
            RequiredAction = "Maintain safeguarding controls.",
            OwnerFunction = "IT/security",
            RiskLevel = RiskLevel.High,
            RequiresFlowDown = true,
            FlowDownRequirement = "Flow down where applicable.",
            ApplicabilityJson = "{}",
            EvidenceExamplesJson = "[]",
            SourceName = "FAR",
            SourceUrl = "https://www.acquisition.gov/far/52.204-21",
            SourceLastReviewedAt = new DateOnly(2026, 6, 17),
            SourceConfidence = "high",
            LastReviewedAt = new DateOnly(2026, 6, 17),
            Confidence = "high",
            ReviewState = ReviewState.Published
        });
        dbContext.Set<ContractClauseObligationEntity>().Add(new ContractClauseObligationEntity
        {
            ContractClauseId = contractClauseId,
            ObligationId = obligationId
        });

        return new ScenarioIds(contractId, contractClauseId, obligationId);
    }

    private static GccsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GccsDbContext>()
            .UseInMemoryDatabase($"obligation-applicability-{Guid.NewGuid():N}")
            .Options;
        return new GccsDbContext(options);
    }

    private sealed class FixedTenantContext(Guid tenantId) : ICurrentTenantContext
    {
        public Guid TenantId { get; } = tenantId;
        public Guid UserId => ActorUserId;
        public string UserEmail => "actor@example.test";
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
            Events.Add(new CapturedAuditEvent(tenantId, actorUserId, action, entityType, entityId, summary, metadata ?? new Dictionary<string, string>()));
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

    private sealed record ScenarioIds(Guid ContractId, Guid ContractClauseId, string ObligationId);
}
