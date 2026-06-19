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
        var service = new SprsScoringRuleService(new FileSprsScoringRuleRepository(), new CapturingAuditEventWriter());

        var ruleSets = await service.ListAsync();
        var published = Assert.Single(ruleSets, ruleSet => ruleSet.State == SprsScoringRuleSetState.Published);

        Assert.Equal("sprs-basic-assessment-nist-800-171-r2-v1", published.Id);
        Assert.False(string.IsNullOrWhiteSpace(published.SourceUrl));
        Assert.False(string.IsNullOrWhiteSpace(published.Version));
        Assert.False(string.IsNullOrWhiteSpace(published.Owner));
        Assert.False(string.IsNullOrWhiteSpace(published.Reviewer));
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
    public async Task TC_30_1_3_and_TC_30_1_4_Retired_rules_are_blocked_and_calculation_references_rule_version()
    {
        var activeRuleSet = CreateRuleSet("active-rule-set", SprsScoringRuleSetState.Published);
        var retiredRuleSet = CreateRuleSet("retired-rule-set", SprsScoringRuleSetState.Retired);
        var service = new SprsScoringRuleService(
            new InMemorySprsScoringRuleRepository(activeRuleSet, retiredRuleSet),
            new CapturingAuditEventWriter());

        var reference = await service.CreateCalculationRuleReferenceAsync("active-rule-set");
        var exception = await Assert.ThrowsAsync<SprsScoringRuleValidationException>(() =>
            service.GetUsableForCalculationAsync("retired-rule-set"));

        Assert.Equal(activeRuleSet.Id, reference.RuleSetId);
        Assert.Equal(activeRuleSet.Version, reference.RuleSetVersion);
        Assert.Equal(activeRuleSet.SourceUrl, reference.SourceUrl);
        Assert.Equal(activeRuleSet.EffectiveDate, reference.EffectiveDate);
        Assert.Contains("Retired", exception.Message, StringComparison.OrdinalIgnoreCase);
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
            ]);

    private sealed class InMemorySprsScoringRuleRepository(params SprsScoringRuleSetDto[] seed) : ISprsScoringRuleRepository
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
