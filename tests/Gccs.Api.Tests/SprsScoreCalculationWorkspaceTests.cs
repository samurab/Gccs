using Gccs.Application.Audit;
using Gccs.Application.Cmmc;
using Gccs.Application.Common;
using Gccs.Domain.Audit;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Compliance;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class SprsScoreCalculationWorkspaceTests
{
    [Fact]
    public async Task TC_30_2_1_Calculation_returns_score_deductions_reasons_rule_version_and_gaps()
    {
        var ids = StoryIds.Create();
        var service = CreateService(ids, [
            CreateStatus(ids.AssessmentId, "3.1.1", ControlImplementationStatus.Implemented, AssessmentResult.Met),
            CreateStatus(ids.AssessmentId, "3.1.2", ControlImplementationStatus.NotStarted, AssessmentResult.NotMet),
            CreateStatus(ids.AssessmentId, "3.5.3", ControlImplementationStatus.PartiallyImplemented, AssessmentResult.NotMet)
        ]);

        var calculation = await service.CalculateAsync(
            ids.AssessmentId,
            new SprsScoreCalculationRequest("sprs-rules", "Leadership review note.", ManualNotesClassification: ContentClassificationPolicy.DefaultUnclassified()),
            ids.ActorUserId);

        Assert.NotNull(calculation);
        Assert.Equal("2026.06", calculation.RuleSetVersion);
        Assert.Equal(110, calculation.MaximumScore);
        Assert.Equal(100, calculation.Score);
        Assert.Equal(10, calculation.TotalDeduction);
        Assert.Equal(3, calculation.LineItems.Count);
        Assert.Equal(2, calculation.UnresolvedGaps.Count);
        Assert.Contains(calculation.LineItems, item => item.RequirementId == "3.1.2" && item.Reason == "control-not-implemented");
        Assert.Contains(calculation.LineItems, item => item.RequirementId == "3.5.3" && item.Reason == "control-partially-implemented");
    }

    [Fact]
    public async Task TC_30_2_2_Calculation_uses_current_tenant_assessment_data_only()
    {
        var ids = StoryIds.Create();
        var repository = CreateAssessmentRepository(ids, [
            CreateStatus(ids.AssessmentId, "3.1.1", ControlImplementationStatus.NotStarted, AssessmentResult.NotMet)
        ]);
        var service = CreateService(repository);

        var currentTenantCalculation = await service.CalculateAsync(ids.AssessmentId, new SprsScoreCalculationRequest("sprs-rules", null), ids.ActorUserId);
        var otherTenantCalculation = await service.CalculateAsync(ids.OtherTenantAssessmentId, new SprsScoreCalculationRequest("sprs-rules", null), ids.ActorUserId);

        Assert.NotNull(currentTenantCalculation);
        Assert.Null(otherTenantCalculation);
        Assert.Equal(ids.TenantId, currentTenantCalculation.TenantId);
    }

    [Fact]
    public async Task TC_30_2_3_Recalculation_updates_score_when_control_status_changes()
    {
        var ids = StoryIds.Create();
        var repository = CreateAssessmentRepository(ids, [
            CreateStatus(ids.AssessmentId, "3.1.1", ControlImplementationStatus.NotStarted, AssessmentResult.NotMet),
            CreateStatus(ids.AssessmentId, "3.1.2", ControlImplementationStatus.Implemented, AssessmentResult.Met),
            CreateStatus(ids.AssessmentId, "3.5.3", ControlImplementationStatus.Implemented, AssessmentResult.Met)
        ]);
        var service = CreateService(repository);

        var before = await service.CalculateAsync(ids.AssessmentId, new SprsScoreCalculationRequest("sprs-rules", null), ids.ActorUserId);
        repository.ReplaceStatus(CreateStatus(ids.AssessmentId, "3.1.1", ControlImplementationStatus.Implemented, AssessmentResult.Met));
        var after = await service.CalculateAsync(ids.AssessmentId, new SprsScoreCalculationRequest("sprs-rules", null), ids.ActorUserId);

        Assert.NotNull(before);
        Assert.NotNull(after);
        Assert.Equal(105, before.Score);
        Assert.Equal(110, after.Score);
        Assert.Equal(5, before.TotalDeduction);
        Assert.Equal(0, after.TotalDeduction);
    }

    [Fact]
    public async Task Calculation_preserves_governed_negative_scores_instead_of_clamping_to_zero()
    {
        var ids = StoryIds.Create();
        var rules = new[]
        {
            new SprsScoringRuleDto("3.1.1", "Requirement one", 100, "Subtract when not met.", "https://example.test/sprs"),
            new SprsScoringRuleDto("3.1.2", "Requirement two", 100, "Subtract when not met.", "https://example.test/sprs")
        };
        var service = CreateService(CreateAssessmentRepository(ids, []), ruleSet: CreatePublishedRuleSet(rules));

        var calculation = await service.CalculateAsync(
            ids.AssessmentId,
            new SprsScoreCalculationRequest("sprs-rules", null),
            ids.ActorUserId);

        Assert.NotNull(calculation);
        Assert.Equal(-90, calculation.Score);
        Assert.Equal(200, calculation.TotalDeduction);
    }

    [Fact]
    public async Task TC_30_2_4_Manual_notes_are_stored_separately_from_calculated_values()
    {
        var ids = StoryIds.Create();
        var history = new CapturingCalculationHistoryRepository();
        var service = CreateService(ids, [
            CreateStatus(ids.AssessmentId, "3.1.1", ControlImplementationStatus.NotStarted, AssessmentResult.NotMet),
            CreateStatus(ids.AssessmentId, "3.1.2", ControlImplementationStatus.Implemented, AssessmentResult.Met),
            CreateStatus(ids.AssessmentId, "3.5.3", ControlImplementationStatus.Implemented, AssessmentResult.Met)
        ], history: history);

        var calculation = await service.CalculateAsync(
            ids.AssessmentId,
            new SprsScoreCalculationRequest("sprs-rules", "  Reviewer says validate MFA scope.  ", ManualNotesClassification: ContentClassificationPolicy.DefaultUnclassified()),
            ids.ActorUserId);

        Assert.NotNull(calculation);
        Assert.Equal(105, calculation.Score);
        Assert.Equal(5, calculation.TotalDeduction);
        Assert.Equal("Reviewer says validate MFA scope.", calculation.ManualNotes);
        Assert.Equal(calculation.Score, Assert.Single(history.Calculations).Score);
        Assert.Equal(calculation.ManualNotes, Assert.Single(history.Calculations).ManualNotes);
    }

    [Fact]
    public async Task Manual_notes_require_explicit_classification_without_writing_history_or_audit()
    {
        var ids = StoryIds.Create();
        var history = new CapturingCalculationHistoryRepository();
        var audit = new CapturingAuditEventWriter();
        var service = CreateService(ids, [
            CreateStatus(ids.AssessmentId, "3.1.1", ControlImplementationStatus.Implemented, AssessmentResult.Met)
        ], history, audit);

        var exception = await Assert.ThrowsAsync<SprsScoreCalculationException>(() => service.CalculateAsync(
            ids.AssessmentId,
            new SprsScoreCalculationRequest("sprs-rules", "Reviewer note without classification."),
            ids.ActorUserId));

        Assert.Contains("classification", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(history.Calculations);
        Assert.Empty(audit.Events);
    }

    [Fact]
    public async Task TC_30_2_5_Score_calculation_is_audit_logged()
    {
        var ids = StoryIds.Create();
        var auditWriter = new CapturingAuditEventWriter();
        var service = CreateService(ids, [
            CreateStatus(ids.AssessmentId, "3.1.1", ControlImplementationStatus.NotStarted, AssessmentResult.NotMet),
            CreateStatus(ids.AssessmentId, "3.1.2", ControlImplementationStatus.Implemented, AssessmentResult.Met),
            CreateStatus(ids.AssessmentId, "3.5.3", ControlImplementationStatus.Implemented, AssessmentResult.Met)
        ], auditWriter: auditWriter);

        var calculation = await service.CalculateAsync(ids.AssessmentId, new SprsScoreCalculationRequest("sprs-rules", null), ids.ActorUserId);

        Assert.NotNull(calculation);
        var auditEvent = Assert.Single(auditWriter.Events);
        Assert.Equal(ids.TenantId, auditEvent.TenantId);
        Assert.Equal(ids.ActorUserId, auditEvent.ActorUserId);
        Assert.Equal(AuditAction.Created, auditEvent.Action);
        Assert.Equal("SprsScoreCalculation", auditEvent.EntityType);
        Assert.Equal("2026.06", auditEvent.Metadata["ruleSetVersion"]);
        Assert.Equal("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", auditEvent.Metadata["ruleSetSourceSha256"]);
        Assert.Equal("105", auditEvent.Metadata["score"]);
        Assert.Equal("5", auditEvent.Metadata["totalDeduction"]);
    }

    [Fact]
    public async Task TC_30_1_4_Calculation_history_stores_the_rule_set_version_used()
    {
        var ids = StoryIds.Create();
        var history = new CapturingCalculationHistoryRepository();
        var service = CreateService(ids, [
            CreateStatus(ids.AssessmentId, "3.1.1", ControlImplementationStatus.Implemented, AssessmentResult.Met),
            CreateStatus(ids.AssessmentId, "3.1.2", ControlImplementationStatus.Implemented, AssessmentResult.Met),
            CreateStatus(ids.AssessmentId, "3.5.3", ControlImplementationStatus.Implemented, AssessmentResult.Met)
        ], history: history);

        var calculation = await service.CalculateAsync(
            ids.AssessmentId,
            new SprsScoreCalculationRequest("sprs-rules", null),
            ids.ActorUserId);

        var stored = Assert.Single(history.Calculations);
        Assert.NotNull(calculation);
        Assert.Equal(calculation.RuleSetId, stored.RuleSetId);
        Assert.Equal(calculation.RuleSetVersion, stored.RuleSetVersion);
        Assert.Equal("2026.06", stored.RuleSetVersion);
        Assert.Equal("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", stored.RuleSetSourceSha256);
    }

    [Theory]
    [InlineData("privileged-and-remote-only", 3, 107)]
    [InlineData("not-implemented", 5, 105)]
    public async Task Conditional_rules_apply_only_the_selected_governed_deduction(
        string optionCode,
        int expectedDeduction,
        int expectedScore)
    {
        var ids = StoryIds.Create();
        var rule = new SprsScoringRuleDto(
            "3.5.3",
            "Use multifactor authentication.",
            5,
            "Apply the selected conditional deduction.",
            "https://example.test/sprs",
            SprsScoringRuleType.ConditionalDeduction,
            [
                new SprsConditionalDeductionOptionDto("not-implemented", 5, "MFA is not implemented."),
                new SprsConditionalDeductionOptionDto("privileged-and-remote-only", 3, "MFA excludes general users.")
            ]);
        var service = CreateService(
            CreateAssessmentRepository(ids, [
                CreateStatus(ids.AssessmentId, rule.RequirementId, ControlImplementationStatus.PartiallyImplemented, AssessmentResult.NotMet)
            ]),
            ruleSet: CreatePublishedRuleSet(rule));

        var calculation = await service.CalculateAsync(
            ids.AssessmentId,
            new SprsScoreCalculationRequest(
                "sprs-rules",
                null,
                [new SprsConditionalDeductionSelection(rule.RequirementId, optionCode)]),
            ids.ActorUserId);

        Assert.NotNull(calculation);
        Assert.Equal(expectedDeduction, calculation.TotalDeduction);
        Assert.Equal(expectedScore, calculation.Score);
        Assert.Equal($"conditional-{optionCode}", Assert.Single(calculation.LineItems).Reason);
    }

    [Fact]
    public async Task Missing_conditional_selection_rejects_calculation_without_history_or_audit()
    {
        var ids = StoryIds.Create();
        var history = new CapturingCalculationHistoryRepository();
        var audit = new CapturingAuditEventWriter();
        var rule = new SprsScoringRuleDto(
            "3.13.11",
            "Employ FIPS-validated cryptography.",
            5,
            "Apply the selected conditional deduction.",
            "https://example.test/sprs",
            SprsScoringRuleType.ConditionalDeduction,
            [
                new SprsConditionalDeductionOptionDto("no-cryptography", 5, "No cryptography is employed."),
                new SprsConditionalDeductionOptionDto("mostly-not-fips-validated", 3, "Cryptography is mostly not FIPS validated.")
            ]);
        var service = CreateService(
            CreateAssessmentRepository(ids, [
                CreateStatus(ids.AssessmentId, rule.RequirementId, ControlImplementationStatus.NotStarted, AssessmentResult.NotMet)
            ]),
            history,
            audit,
            CreatePublishedRuleSet(rule));

        var exception = await Assert.ThrowsAsync<SprsScoreCalculationException>(() => service.CalculateAsync(
            ids.AssessmentId,
            new SprsScoreCalculationRequest("sprs-rules", null),
            ids.ActorUserId));

        Assert.Contains("explicit conditional deduction", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(history.Calculations);
        Assert.Empty(audit.Events);
    }

    [Fact]
    public async Task Missing_system_security_plan_blocks_calculation_without_history_or_audit()
    {
        var ids = StoryIds.Create();
        var history = new CapturingCalculationHistoryRepository();
        var audit = new CapturingAuditEventWriter();
        var rule = new SprsScoringRuleDto(
            "3.12.4",
            "Develop and maintain a system security plan.",
            0,
            "Block calculation when the system security plan is absent.",
            "https://example.test/sprs",
            SprsScoringRuleType.AssessmentBlocking);
        var service = CreateService(
            CreateAssessmentRepository(ids, [
                CreateStatus(ids.AssessmentId, rule.RequirementId, ControlImplementationStatus.NotStarted, AssessmentResult.NotMet)
            ]),
            history,
            audit,
            CreatePublishedRuleSet(rule));

        var exception = await Assert.ThrowsAsync<SprsScoreCalculationException>(() => service.CalculateAsync(
            ids.AssessmentId,
            new SprsScoreCalculationRequest("sprs-rules", null),
            ids.ActorUserId));

        Assert.Contains("cannot be completed", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(history.Calculations);
        Assert.Empty(audit.Events);
    }

    [Fact]
    public async Task Not_applicable_requires_an_explicit_rule_applicability_condition()
    {
        var ids = StoryIds.Create();
        var fixedRule = new SprsScoringRuleDto(
            "3.1.3",
            "Control the flow of CUI.",
            1,
            "Subtract one point when not met.",
            "https://example.test/sprs");
        var service = CreateService(
            CreateAssessmentRepository(ids, [
                CreateStatus(ids.AssessmentId, fixedRule.RequirementId, ControlImplementationStatus.NotApplicable, AssessmentResult.NotApplicable)
            ]),
            ruleSet: CreatePublishedRuleSet(fixedRule));

        var exception = await Assert.ThrowsAsync<SprsScoreCalculationException>(() => service.CalculateAsync(
            ids.AssessmentId,
            new SprsScoreCalculationRequest("sprs-rules", null),
            ids.ActorUserId));

        Assert.Contains("cannot be marked not applicable", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Governed_not_applicable_condition_requires_and_preserves_documented_rationale()
    {
        var ids = StoryIds.Create();
        var rule = new SprsScoringRuleDto(
            "3.1.12",
            "Monitor and control remote access sessions.",
            5,
            "Subtract five points when not met.",
            "https://example.test/sprs",
            NotApplicableWhen: "Remote access is not permitted.");
        var notApplicableStatus = CreateStatus(
                ids.AssessmentId,
                rule.RequirementId,
                ControlImplementationStatus.NotApplicable,
                AssessmentResult.NotApplicable,
                "Remote access is prohibited by the assessed boundary policy.") with
            {
                ReviewedBy = ids.ActorUserId,
                ReviewedAtUtc = DateTimeOffset.UtcNow,
                ReviewStatus = "approved"
            };
        var repository = CreateAssessmentRepository(ids, [notApplicableStatus]);
        var service = CreateService(repository, ruleSet: CreatePublishedRuleSet(rule));

        var calculation = await service.CalculateAsync(
            ids.AssessmentId,
            new SprsScoreCalculationRequest("sprs-rules", null),
            ids.ActorUserId);

        Assert.NotNull(calculation);
        Assert.Equal(110, calculation.Score);
        var lineItem = Assert.Single(calculation.LineItems);
        Assert.Equal("not-applicable", lineItem.Reason);
        Assert.Equal("Remote access is prohibited by the assessed boundary policy.", lineItem.ApplicabilityRationale);
    }

    [Fact]
    public async Task Governed_not_applicable_condition_rejects_unreviewed_rationale()
    {
        var ids = StoryIds.Create();
        var rule = new SprsScoringRuleDto(
            "3.1.12",
            "Monitor and control remote access sessions.",
            5,
            "Subtract five points when not met.",
            "https://example.test/sprs",
            NotApplicableWhen: "Remote access is not permitted.");
        var repository = CreateAssessmentRepository(ids, [
            CreateStatus(
                ids.AssessmentId,
                rule.RequirementId,
                ControlImplementationStatus.NotApplicable,
                AssessmentResult.NotApplicable,
                "Remote access is prohibited by policy.")
        ]);
        var service = CreateService(repository, ruleSet: CreatePublishedRuleSet(rule));

        var exception = await Assert.ThrowsAsync<SprsScoreCalculationException>(() => service.CalculateAsync(
            ids.AssessmentId,
            new SprsScoreCalculationRequest("sprs-rules", null),
            ids.ActorUserId));

        Assert.Contains("approved review metadata", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static SprsScoreCalculationService CreateService(
        StoryIds ids,
        IReadOnlyList<CmmcControlStatusDto> statuses,
        CapturingCalculationHistoryRepository? history = null,
        CapturingAuditEventWriter? auditWriter = null) =>
        CreateService(CreateAssessmentRepository(ids, statuses), history, auditWriter);

    private static SprsScoreCalculationService CreateService(
        FakeCmmcAssessmentRepository assessmentRepository,
        CapturingCalculationHistoryRepository? history = null,
        CapturingAuditEventWriter? auditWriter = null,
        SprsScoringRuleSetDto? ruleSet = null) =>
        new(
            assessmentRepository,
            new FakeSprsScoringRuleRepository(ruleSet),
            history ?? new CapturingCalculationHistoryRepository(),
            auditWriter ?? new CapturingAuditEventWriter());

    private static SprsScoringRuleSetDto CreatePublishedRuleSet(params SprsScoringRuleDto[] rules) =>
        FakeSprsScoringRuleRepository.DefaultRuleSet with
        {
            Rules = rules,
            ExpectedRequirementCount = rules.Length
        };

    private static FakeCmmcAssessmentRepository CreateAssessmentRepository(
        StoryIds ids,
        IReadOnlyList<CmmcControlStatusDto> statuses) =>
        new(ids.TenantId, ids.AssessmentId, statuses);

    private static CmmcControlStatusDto CreateStatus(
        Guid assessmentId,
        string controlId,
        ControlImplementationStatus status,
        AssessmentResult result,
        string notes = "") =>
        new(
            assessmentId,
            controlId,
            $"Control {controlId}",
            "AC",
            "Protect CUI.",
            "Assess implementation.",
            "NIST SP 800-171 Rev. 2",
            "https://example.test/nist",
            new DateOnly(2026, 6, 19),
            "high",
            status,
            result,
            [],
            [],
            [],
            [],
            null,
            null,
            notes,
            string.Empty,
            false,
            null,
            false,
            null,
            ControlResponsibilityType.Organization,
            "Security",
            null,
            string.Empty,
            []);

    private sealed class FakeCmmcAssessmentRepository(
        Guid tenantId,
        Guid assessmentId,
        IReadOnlyList<CmmcControlStatusDto> statuses) : ICmmcAssessmentRepository
    {
        private readonly List<CmmcControlStatusDto> _statuses = [.. statuses];

        public void ReplaceStatus(CmmcControlStatusDto status)
        {
            _statuses.RemoveAll(existing => existing.ControlId == status.ControlId);
            _statuses.Add(status);
        }

        public Task<CmmcAssessmentDto?> FindCurrentTenantAsync(Guid requestedAssessmentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CmmcAssessmentDto?>(requestedAssessmentId == assessmentId ? Assessment : null);

        public Task<IReadOnlyList<CmmcControlStatusDto>?> ListControlStatusesAsync(Guid requestedAssessmentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CmmcControlStatusDto>?>(requestedAssessmentId == assessmentId ? _statuses.ToArray() : null);

        private CmmcAssessmentDto Assessment =>
            new(
                assessmentId,
                tenantId,
                "Level 2 readiness",
                AssessmentType.SelfAssessment,
                CmmcLevel.Level2,
                "CMMC 2.0",
                AssessmentStatus.InProgress,
                new DateOnly(2026, 6, 19),
                null,
                null,
                "Security",
                null,
                [],
                new ControlSummaryDto(_statuses.Count, 0, 0, 0, 0, 0, 0),
                0,
                0,
                DateTimeOffset.UtcNow,
                null);

        public Task<IReadOnlyList<CmmcAssessmentDto>> ListCurrentTenantAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CmmcAssessmentDto>>([Assessment]);

        public Task<IReadOnlyList<CmmcControlLibraryDto>> ListControlLibraryAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CmmcControlLibraryDto>>(
                _statuses.Select(status => new CmmcControlLibraryDto(
                    status.ControlId,
                    status.Title,
                    status.Family,
                    CmmcLevel.Level2,
                    status.Requirement,
                    status.AssessmentObjective,
                    status.SourceName,
                    status.SourceUrl,
                    status.SourceLastReviewedAt,
                    status.SourceConfidence)).ToArray());

        public Task<CmmcAssessmentDto> CreateCurrentTenantAsync(UpsertCmmcAssessmentRequest request, Guid actorUserId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<CmmcAssessmentDto?> UpdateCurrentTenantAsync(Guid assessmentId, UpsertCmmcAssessmentRequest request, Guid actorUserId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<CmmcControlStatusDto?> UpsertControlStatusAsync(Guid assessmentId, string controlId, UpsertCmmcControlStatusRequest request, Guid actorUserId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<CmmcResponsibilityMatrixRowDto>?> GetResponsibilityMatrixAsync(Guid assessmentId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<string?> ExportResponsibilityMatrixCsvAsync(Guid assessmentId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<CmmcReadinessGapDto>?> GetReadinessGapsAsync(Guid assessmentId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeSprsScoringRuleRepository(SprsScoringRuleSetDto? ruleSet = null) : ISprsScoringRuleRepository
    {
        public static readonly SprsScoringRuleSetDto DefaultRuleSet = new(
            "sprs-rules",
            "2026.06",
            SprsScoringRuleSetState.Published,
            "DoD NIST SP 800-171 DoD Assessment Methodology",
            "https://example.test/sprs",
            new DateOnly(2026, 6, 19),
            new DateOnly(2026, 6, 19),
            "Compliance Content Owner",
            "CMMC SME",
            new DateOnly(2026, 6, 19),
            110,
            [
                new SprsScoringRuleDto("3.1.1", "Access control one", 5, "Assess 3.1.1.", "https://example.test/sprs"),
                new SprsScoringRuleDto("3.1.2", "Access control two", 5, "Assess 3.1.2.", "https://example.test/sprs"),
                new SprsScoringRuleDto("3.5.3", "MFA", 5, "Assess MFA.", "https://example.test/sprs")
            ],
            3,
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

        private readonly SprsScoringRuleSetDto _ruleSet = ruleSet ?? DefaultRuleSet;

        public Task<IReadOnlyList<SprsScoringRuleSetDto>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SprsScoringRuleSetDto>>([_ruleSet]);

        public Task<SprsScoringRuleSetDto?> FindAsync(string ruleSetId, CancellationToken cancellationToken = default) =>
            Task.FromResult<SprsScoringRuleSetDto?>(ruleSetId == _ruleSet.Id ? _ruleSet : null);

        public Task<SprsScoringRuleSetDto> UpdateStateAsync(string ruleSetId, SprsScoringRuleSetState state, string? reviewer, DateOnly? reviewDate, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class CapturingCalculationHistoryRepository : ISprsScoreCalculationHistoryRepository
    {
        public List<SprsScoreCalculationDto> Calculations { get; } = [];

        public Task SaveAsync(SprsScoreCalculationDto calculation, CancellationToken cancellationToken = default)
        {
            Calculations.Add(calculation);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<SprsScoreCalculationDto>?> ListCurrentTenantAsync(
            Guid assessmentId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SprsScoreCalculationDto>?>(Calculations
                .Where(calculation => calculation.AssessmentId == assessmentId)
                .ToArray());
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

    private sealed record StoryIds(Guid TenantId, Guid AssessmentId, Guid OtherTenantAssessmentId, Guid ActorUserId)
    {
        public static StoryIds Create() =>
            new(
                Guid.Parse("30230230-2302-3023-0230-2302302302aa"),
                Guid.Parse("30230230-2302-3023-0230-2302302302bb"),
                Guid.Parse("30230230-2302-3023-0230-2302302302cc"),
                Guid.Parse("30230230-2302-3023-0230-2302302302dd"));
    }
}
