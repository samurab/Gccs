using Gccs.Application.Audit;
using Gccs.Application.Cmmc;
using Gccs.Application.Reports;
using Gccs.Domain.Audit;
using Gccs.Domain.Cmmc;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class SprsReadinessReportTests
{
    [Fact]
    public async Task TC_30_3_1_Report_includes_score_deductions_gaps_poam_evidence_rule_version_and_generated_date()
    {
        var ids = StoryIds.Create();
        var poamId = Guid.Parse("30330330-3303-3033-0330-3303303303ee");
        var evidenceId = Guid.Parse("30330330-3303-3033-0330-3303303303ff");
        var service = CreateService(ids, [
            CreateStatus(ids.AssessmentId, "3.1.1", ControlImplementationStatus.NotStarted, AssessmentResult.NotMet, [evidenceId], [poamId]),
            CreateStatus(ids.AssessmentId, "3.1.2", ControlImplementationStatus.Implemented, AssessmentResult.Met, [], [])
        ]);

        var report = await service.GenerateAsync(ids.AssessmentId, new SprsReadinessReportRequest("sprs-rules", "Review before submission."), ids.ActorUserId);

        Assert.NotNull(report);
        Assert.Equal(105, report.Score);
        Assert.Equal(110, report.MaximumScore);
        Assert.Equal(5, report.TotalDeduction);
        Assert.Equal("2026.06", report.RuleSetVersion);
        Assert.NotEqual(default, report.GeneratedAt);
        var deduction = Assert.Single(report.Deductions);
        Assert.Equal("3.1.1", deduction.RequirementId);
        var unresolved = Assert.Single(report.UnresolvedControls);
        Assert.Equal("linked", unresolved.EvidenceStatus);
        Assert.Contains(poamId, unresolved.PoamItemIds);
    }

    [Fact]
    public async Task TC_30_3_2_Report_states_gccs_has_not_submitted_score_to_sprs()
    {
        var ids = StoryIds.Create();
        var service = CreateService(ids, [
            CreateStatus(ids.AssessmentId, "3.1.1", ControlImplementationStatus.Implemented, AssessmentResult.Met, [], [])
        ]);

        var report = await service.GenerateAsync(ids.AssessmentId, new SprsReadinessReportRequest("sprs-rules", null), ids.ActorUserId);

        Assert.NotNull(report);
        Assert.Contains("has not submitted", report.SubmissionDisclaimer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SPRS", report.SubmissionDisclaimer, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TC_30_3_3_Report_uses_current_tenant_data_only()
    {
        var ids = StoryIds.Create();
        var service = CreateService(ids, [
            CreateStatus(ids.AssessmentId, "3.1.1", ControlImplementationStatus.NotStarted, AssessmentResult.NotMet, [], [])
        ]);

        var currentTenantReport = await service.GenerateAsync(ids.AssessmentId, new SprsReadinessReportRequest("sprs-rules", null), ids.ActorUserId);
        var otherTenantReport = await service.GenerateAsync(ids.OtherTenantAssessmentId, new SprsReadinessReportRequest("sprs-rules", null), ids.ActorUserId);

        Assert.NotNull(currentTenantReport);
        Assert.Null(otherTenantReport);
        Assert.Equal(ids.TenantId, currentTenantReport.TenantId);
    }

    [Fact]
    public async Task TC_30_3_4_Report_permission_is_enforced()
    {
        var ids = StoryIds.Create();
        var service = CreateService(ids, [
            CreateStatus(ids.AssessmentId, "3.1.1", ControlImplementationStatus.Implemented, AssessmentResult.Met, [], [])
        ]);

        var exception = await Assert.ThrowsAsync<SprsReadinessReportException>(() =>
            service.GenerateAsync(
                ids.AssessmentId,
                new SprsReadinessReportRequest("sprs-rules", null, HasReportPermission: false),
                ids.ActorUserId));

        Assert.Contains("not authorized", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_30_3_5_Report_generation_is_audit_logged()
    {
        var ids = StoryIds.Create();
        var auditWriter = new CapturingAuditEventWriter();
        var service = CreateService(ids, [
            CreateStatus(ids.AssessmentId, "3.1.1", ControlImplementationStatus.NotStarted, AssessmentResult.NotMet, [], [])
        ], auditWriter);

        var report = await service.GenerateAsync(ids.AssessmentId, new SprsReadinessReportRequest("sprs-rules", null), ids.ActorUserId);

        Assert.NotNull(report);
        Assert.Contains(auditWriter.Events, auditEvent =>
            auditEvent.EntityType == "SprsReadinessReport" &&
            auditEvent.TenantId == ids.TenantId &&
            auditEvent.ActorUserId == ids.ActorUserId &&
            auditEvent.Metadata["ruleSetVersion"] == "2026.06" &&
            auditEvent.Metadata["score"] == report.Score.ToString());
    }

    private static SprsReadinessReportService CreateService(
        StoryIds ids,
        IReadOnlyList<CmmcControlStatusDto> statuses,
        CapturingAuditEventWriter? auditWriter = null)
    {
        var assessmentRepository = new FakeCmmcAssessmentRepository(ids.TenantId, ids.AssessmentId, statuses);
        var writer = auditWriter ?? new CapturingAuditEventWriter();
        var calculator = new SprsScoreCalculationService(
            assessmentRepository,
            new FakeSprsScoringRuleRepository(),
            new CapturingCalculationHistoryRepository(),
            writer);
        return new SprsReadinessReportService(calculator, assessmentRepository, writer);
    }

    private static CmmcControlStatusDto CreateStatus(
        Guid assessmentId,
        string controlId,
        ControlImplementationStatus status,
        AssessmentResult result,
        IReadOnlyList<Guid> evidenceIds,
        IReadOnlyList<Guid> poamIds) =>
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
            evidenceIds,
            [],
            [],
            poamIds,
            null,
            null,
            string.Empty,
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
        public Task<CmmcAssessmentDto?> FindCurrentTenantAsync(Guid requestedAssessmentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CmmcAssessmentDto?>(requestedAssessmentId == assessmentId ? Assessment : null);

        public Task<IReadOnlyList<CmmcControlStatusDto>?> ListControlStatusesAsync(Guid requestedAssessmentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CmmcControlStatusDto>?>(requestedAssessmentId == assessmentId ? statuses : null);

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
                new ControlSummaryDto(statuses.Count, 0, 0, 0, 0, 0, 0),
                0,
                0,
                DateTimeOffset.UtcNow,
                null);

        public Task<IReadOnlyList<CmmcAssessmentDto>> ListCurrentTenantAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CmmcAssessmentDto>>([Assessment]);

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

    private sealed class FakeSprsScoringRuleRepository : ISprsScoringRuleRepository
    {
        private static readonly SprsScoringRuleSetDto RuleSet = new(
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
                new SprsScoringRuleDto("3.1.1", "Access control one", 5, "Assess 3.1.1.", "https://example.test/sprs")
            ]);

        public Task<IReadOnlyList<SprsScoringRuleSetDto>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SprsScoringRuleSetDto>>([RuleSet]);

        public Task<SprsScoringRuleSetDto?> FindAsync(string ruleSetId, CancellationToken cancellationToken = default) =>
            Task.FromResult<SprsScoringRuleSetDto?>(ruleSetId == RuleSet.Id ? RuleSet : null);

        public Task<SprsScoringRuleSetDto> UpdateStateAsync(string ruleSetId, SprsScoringRuleSetState state, string? reviewer, DateOnly? reviewDate, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class CapturingCalculationHistoryRepository : ISprsScoreCalculationHistoryRepository
    {
        public Task SaveAsync(SprsScoreCalculationDto calculation, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
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
                Guid.Parse("30330330-3303-3033-0330-3303303303aa"),
                Guid.Parse("30330330-3303-3033-0330-3303303303bb"),
                Guid.Parse("30330330-3303-3033-0330-3303303303cc"),
                Guid.Parse("30330330-3303-3033-0330-3303303303dd"));
    }
}
