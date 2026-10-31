using Gccs.Application.Audit;
using Gccs.Application.Cmmc;
using Gccs.Domain.Audit;
using Gccs.Infrastructure.Cmmc;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class SprsScoringRuleBaselineTests
{
    [Fact]
    public async Task TC_30_1_1_Published_scoring_rules_include_required_source_and_review_metadata()
    {
        var service = new SprsScoringRuleService(
            new InMemorySprsScoringRuleRepository(CreateRuleSet("published-rule-set", SprsScoringRuleSetState.Published)),
            new CapturingAuditEventWriter());

        var ruleSets = await service.ListAsync();
        var published = Assert.Single(ruleSets, ruleSet => ruleSet.State == SprsScoringRuleSetState.Published);

        Assert.Equal("published-rule-set", published.Id);
        Assert.False(string.IsNullOrWhiteSpace(published.SourceUrl));
        Assert.False(string.IsNullOrWhiteSpace(published.Version));
        Assert.False(string.IsNullOrWhiteSpace(published.Owner));
        Assert.False(string.IsNullOrWhiteSpace(published.Reviewer));
        Assert.Matches("^[a-f0-9]{64}$", published.SourceSha256!);
        Assert.NotNull(published.ReviewDate);
        Assert.NotNull(published.EffectiveDate);
        Assert.NotEmpty(published.Rules);
        Assert.All(published.Rules, rule =>
        {
            Assert.False(string.IsNullOrWhiteSpace(rule.RequirementId));
            Assert.False(string.IsNullOrWhiteSpace(rule.SourceUrl));
            Assert.True(rule.Deduction > 0);
        });
    }

    [Fact]
    public async Task Source_control_baseline_remains_draft_until_qualified_review()
    {
        var service = new SprsScoringRuleService(new FileSprsScoringRuleRepository(), new CapturingAuditEventWriter());

        var baseline = Assert.Single(await service.ListAsync());

        Assert.Equal(SprsScoringRuleSetState.Draft, baseline.State);
        Assert.Null(baseline.Reviewer);
        Assert.Null(baseline.ReviewDate);
        Assert.Equal(110, baseline.ExpectedRequirementCount);
        Assert.Equal(110, baseline.Rules.Count);
        Assert.EndsWith("NIST-SP-800-171-Assessment-Methodology-Version-1.2.1-6.24.2020.pdf", baseline.SourceUrl);
        Assert.Equal("dd88416ca43f34e817c05b9cb416ffce47f615765a9fd0e1cdc52b9f2de60835", baseline.SourceSha256);

        Assert.Equal(107, baseline.Rules.Count(rule => rule.RuleType is SprsScoringRuleType.FixedDeduction));
        Assert.Equal(2, baseline.Rules.Count(rule => rule.RuleType is SprsScoringRuleType.ConditionalDeduction));
        Assert.Single(baseline.Rules, rule => rule.RuleType is SprsScoringRuleType.AssessmentBlocking);
        Assert.Equal(51, baseline.Rules.Count(rule => rule.RuleType is SprsScoringRuleType.FixedDeduction && rule.Deduction == 1));
        Assert.Equal(14, baseline.Rules.Count(rule => rule.RuleType is SprsScoringRuleType.FixedDeduction && rule.Deduction == 3));
        Assert.Equal(42, baseline.Rules.Count(rule => rule.RuleType is SprsScoringRuleType.FixedDeduction && rule.Deduction == 5));
        Assert.Equal(17, baseline.Rules.Count(rule => rule.IsBasicSafeguardingRequirement));
        Assert.Equal(5, baseline.Rules.Count(rule => rule.NotApplicableWhen is not null));
        Assert.Equal(110, baseline.Rules.Select(rule => rule.RequirementId).Distinct(StringComparer.OrdinalIgnoreCase).Count());

        var mfa = Assert.Single(baseline.Rules, rule => rule.RequirementId == "3.5.3");
        Assert.Equal([3, 5], mfa.ConditionalDeductions!.Select(option => option.Deduction).Order().ToArray());
        var cryptography = Assert.Single(baseline.Rules, rule => rule.RequirementId == "3.13.11");
        Assert.Equal([3, 5], cryptography.ConditionalDeductions!.Select(option => option.Deduction).Order().ToArray());
        var systemSecurityPlan = Assert.Single(baseline.Rules, rule => rule.RequirementId == "3.12.4");
        Assert.Equal(0, systemSecurityPlan.Deduction);

        SprsScoringRuleGovernance.ValidatePublished(baseline with
        {
            State = SprsScoringRuleSetState.Published,
            Reviewer = "Independent reviewer fixture",
            ReviewDate = new DateOnly(2026, 9, 10),
            LastReviewedAt = new DateOnly(2026, 9, 10)
        });
    }

    [Fact]
    public async Task TC_30_1_2_Publish_validation_requires_source_and_review_metadata()
    {
        var repository = new InMemorySprsScoringRuleRepository(
            CreateRuleSet("draft-rule-set", SprsScoringRuleSetState.Approved) with
            {
                SourceUrl = "",
                Reviewer = null,
                ReviewDate = null
            });
        var service = new SprsScoringRuleService(repository, new CapturingAuditEventWriter());

        var exception = await Assert.ThrowsAsync<SprsScoringRuleValidationException>(() =>
            service.ChangeStateAsync(
                "draft-rule-set",
                new ChangeSprsScoringRuleSetStateRequest(SprsScoringRuleSetState.Published, null, null),
                Guid.NewGuid(),
                Guid.NewGuid()));

        Assert.Contains("source URL", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(SprsScoringRuleSetState.Approved, (await repository.FindAsync("draft-rule-set"))?.State);
    }

    [Fact]
    public async Task TC_30_1_3_Retired_rules_are_blocked_for_new_calculations()
    {
        var activeRuleSet = CreateRuleSet("active-rule-set", SprsScoringRuleSetState.Published);
        var retiredRuleSet = CreateRuleSet("retired-rule-set", SprsScoringRuleSetState.Retired);
        var service = new SprsScoringRuleService(
            new InMemorySprsScoringRuleRepository(activeRuleSet, retiredRuleSet),
            new CapturingAuditEventWriter());

        var exception = await Assert.ThrowsAsync<SprsScoringRuleValidationException>(() =>
            service.GetUsableForCalculationAsync("retired-rule-set"));

        Assert.Contains("Retired", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_30_1_4_Calculation_reference_identifies_the_rule_set_and_version()
    {
        var activeRuleSet = CreateRuleSet("active-rule-set", SprsScoringRuleSetState.Published);
        var service = new SprsScoringRuleService(
            new InMemorySprsScoringRuleRepository(activeRuleSet),
            new CapturingAuditEventWriter());

        var reference = await service.CreateCalculationRuleReferenceAsync(activeRuleSet.Id);

        Assert.Equal(activeRuleSet.Id, reference.RuleSetId);
        Assert.Equal(activeRuleSet.Version, reference.RuleSetVersion);
        Assert.Equal(activeRuleSet.SourceUrl, reference.SourceUrl);
        Assert.Equal(activeRuleSet.SourceSha256, reference.SourceSha256);
        Assert.Equal(activeRuleSet.EffectiveDate, reference.EffectiveDate);
    }

    [Fact]
    public async Task TC_30_1_5_Scoring_rule_lifecycle_changes_are_audit_logged()
    {
        var tenantId = Guid.Parse("30130130-1301-3013-0130-1301301301aa");
        var actorUserId = Guid.Parse("30130130-1301-3013-0130-1301301301bb");
        var repository = new InMemorySprsScoringRuleRepository(CreateRuleSet("workflow-rule-set", SprsScoringRuleSetState.Draft));
        var auditWriter = new CapturingAuditEventWriter();
        var service = new SprsScoringRuleService(repository, auditWriter);

        await service.ChangeStateAsync(
            "workflow-rule-set",
            new ChangeSprsScoringRuleSetStateRequest(SprsScoringRuleSetState.Approved, "CMMC SME", new DateOnly(2026, 6, 19)),
            tenantId,
            actorUserId);
        await service.ChangeStateAsync(
            "workflow-rule-set",
            new ChangeSprsScoringRuleSetStateRequest(SprsScoringRuleSetState.Published, "CMMC SME", new DateOnly(2026, 6, 19)),
            tenantId,
            actorUserId);
        await service.ChangeStateAsync(
            "workflow-rule-set",
            new ChangeSprsScoringRuleSetStateRequest(SprsScoringRuleSetState.Superseded, "CMMC SME", new DateOnly(2026, 6, 20)),
            tenantId,
            actorUserId);
        await service.ChangeStateAsync(
            "workflow-rule-set",
            new ChangeSprsScoringRuleSetStateRequest(SprsScoringRuleSetState.Retired, "CMMC SME", new DateOnly(2026, 6, 21)),
            tenantId,
            actorUserId);

        Assert.Equal(
            [SprsScoringRuleSetState.Approved, SprsScoringRuleSetState.Published, SprsScoringRuleSetState.Superseded, SprsScoringRuleSetState.Retired],
            auditWriter.Events.Select(auditEvent => Enum.Parse<SprsScoringRuleSetState>(auditEvent.Metadata["afterState"])).ToArray());
        Assert.All(auditWriter.Events, auditEvent =>
        {
            Assert.Equal(tenantId, auditEvent.TenantId);
            Assert.Equal(actorUserId, auditEvent.ActorUserId);
            Assert.Equal(AuditAction.Updated, auditEvent.Action);
            Assert.Equal("SprsScoringRuleSet", auditEvent.EntityType);
            Assert.Equal("workflow-rule-set", auditEvent.EntityId);
            Assert.Equal("2026.06", auditEvent.Metadata["version"]);
        });
    }

    [Fact]
    public async Task Lifecycle_rejects_skipped_and_terminal_state_transitions_without_mutation_or_audit()
    {
        var draft = CreateRuleSet("draft-rule-set", SprsScoringRuleSetState.Draft);
        var retired = CreateRuleSet("retired-rule-set", SprsScoringRuleSetState.Retired);
        var repository = new InMemorySprsScoringRuleRepository(draft, retired);
        var auditWriter = new CapturingAuditEventWriter();
        var service = new SprsScoringRuleService(repository, auditWriter);

        await Assert.ThrowsAsync<SprsScoringRuleValidationException>(() => service.ChangeStateAsync(
            draft.Id,
            new ChangeSprsScoringRuleSetStateRequest(SprsScoringRuleSetState.Published, "CMMC SME", new DateOnly(2026, 6, 19)),
            Guid.NewGuid(),
            Guid.NewGuid()));
        await Assert.ThrowsAsync<SprsScoringRuleValidationException>(() => service.ChangeStateAsync(
            retired.Id,
            new ChangeSprsScoringRuleSetStateRequest(SprsScoringRuleSetState.Published, "CMMC SME", new DateOnly(2026, 6, 19)),
            Guid.NewGuid(),
            Guid.NewGuid()));

        Assert.Equal(SprsScoringRuleSetState.Draft, (await repository.FindAsync(draft.Id))?.State);
        Assert.Equal(SprsScoringRuleSetState.Retired, (await repository.FindAsync(retired.Id))?.State);
        Assert.Empty(auditWriter.Events);
    }

    [Fact]
    public void Published_rules_reject_duplicate_requirements_invalid_urls_and_invalid_deductions()
    {
        var valid = CreateRuleSet("published-rule-set", SprsScoringRuleSetState.Published);

        Assert.Throws<SprsScoringRuleValidationException>(() =>
            SprsScoringRuleGovernance.ValidatePublished(valid with { SourceUrl = "http://example.test/sprs" }));
        Assert.Throws<SprsScoringRuleValidationException>(() =>
            SprsScoringRuleGovernance.ValidatePublished(valid with { Rules = [valid.Rules[0], valid.Rules[0]] }));
        Assert.Throws<SprsScoringRuleValidationException>(() =>
            SprsScoringRuleGovernance.ValidatePublished(valid with
            {
                Rules = [valid.Rules[0] with { Deduction = 0 }]
            }));
        Assert.Throws<SprsScoringRuleValidationException>(() =>
            SprsScoringRuleGovernance.ValidatePublished(valid with { ExpectedRequirementCount = 2 }));
        Assert.Throws<SprsScoringRuleValidationException>(() =>
            SprsScoringRuleGovernance.ValidatePublished(valid with { SourceSha256 = "not-a-sha256" }));
        Assert.Throws<SprsScoringRuleValidationException>(() =>
            SprsScoringRuleGovernance.ValidatePublished(valid with
            {
                Rules = [valid.Rules[0] with { RuleType = SprsScoringRuleType.AssessmentBlocking }]
            }));
        Assert.Throws<SprsScoringRuleValidationException>(() =>
            SprsScoringRuleGovernance.ValidatePublished(valid with
            {
                Rules =
                [
                    valid.Rules[0] with
                    {
                        RuleType = SprsScoringRuleType.ConditionalDeduction,
                        ConditionalDeductions =
                        [
                            new SprsConditionalDeductionOptionDto("only-option", 5, "Only one option is invalid.")
                        ]
                    }
                ]
            }));
    }

    [Fact]
    public async Task Future_effective_rule_cannot_be_used_for_a_new_calculation()
    {
        var future = CreateRuleSet("future-rule-set", SprsScoringRuleSetState.Published) with
        {
            EffectiveDate = new DateOnly(2099, 1, 1)
        };
        var service = new SprsScoringRuleService(
            new InMemorySprsScoringRuleRepository(future),
            new CapturingAuditEventWriter());

        var exception = await Assert.ThrowsAsync<SprsScoringRuleValidationException>(() =>
            service.GetUsableForCalculationAsync(future.Id));

        Assert.Contains("not effective", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Source_control_repository_rejects_runtime_lifecycle_changes_with_governed_error()
    {
        var service = new SprsScoringRuleService(new FileSprsScoringRuleRepository(), new CapturingAuditEventWriter());
        var baseline = Assert.Single(await service.ListAsync());

        var exception = await Assert.ThrowsAsync<SprsScoringRuleValidationException>(() => service.ChangeStateAsync(
            baseline.Id,
            new ChangeSprsScoringRuleSetStateRequest(SprsScoringRuleSetState.Approved, null, null),
            Guid.NewGuid(),
            Guid.NewGuid()));

        Assert.Contains("source-control governed", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static SprsScoringRuleSetDto CreateRuleSet(string id, SprsScoringRuleSetState state) =>
        new(
            id,
            "2026.06",
            state,
            "DoD NIST SP 800-171 DoD Assessment Methodology",
            "https://www.acq.osd.mil/asda/dpc/cp/cyber/safeguarding.html",
            new DateOnly(2026, 6, 19),
            new DateOnly(2026, 6, 19),
            "Compliance Content Owner",
            "CMMC SME",
            new DateOnly(2026, 6, 19),
            110,
            [
                new SprsScoringRuleDto(
                    "3.1.1",
                    "Limit system access to authorized users.",
                    5,
                    "Deduct if not implemented.",
                    "https://www.acq.osd.mil/asda/dpc/cp/cyber/safeguarding.html")
            ],
            1,
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

    private sealed class InMemorySprsScoringRuleRepository(params SprsScoringRuleSetDto[] seed) :
        ISprsScoringRuleRepository,
        ISprsScoringRuleLifecycleRepository
    {
        private readonly Dictionary<string, SprsScoringRuleSetDto> _ruleSets = seed.ToDictionary(ruleSet => ruleSet.Id);

        public Task<IReadOnlyList<SprsScoringRuleSetDto>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SprsScoringRuleSetDto>>(_ruleSets.Values.OrderBy(ruleSet => ruleSet.Id).ToArray());

        public Task<SprsScoringRuleSetDto?> FindAsync(string ruleSetId, CancellationToken cancellationToken = default)
        {
            _ruleSets.TryGetValue(ruleSetId, out var ruleSet);
            return Task.FromResult(ruleSet);
        }

        public Task<SprsScoringRuleSetDto> UpdateStateAsync(
            string ruleSetId,
            SprsScoringRuleSetState state,
            string? reviewer,
            DateOnly? reviewDate,
            CancellationToken cancellationToken = default)
        {
            var current = _ruleSets[ruleSetId];
            var updated = current with
            {
                State = state,
                Reviewer = reviewer,
                ReviewDate = reviewDate,
                LastReviewedAt = reviewDate ?? current.LastReviewedAt
            };
            _ruleSets[ruleSetId] = updated;
            return Task.FromResult(updated);
        }
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
}
