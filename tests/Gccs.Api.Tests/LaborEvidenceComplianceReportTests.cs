using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Labor;
using Gccs.Application.Security;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Domain.Reports;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class LaborEvidenceComplianceReportTests
{
    [Fact]
    public async Task TC_32_3_1_Dashboard_forwards_tenant_safe_filters_and_redacts_sensitive_fields()
    {
        var ids = StoryIds.Create();
        var harness = CreateHarness(ids);
        var query = new LaborDashboardQuery(ids.ContractId, AsOfDate: new DateOnly(2027, 1, 1));

        var dashboard = await harness.Service.GetDashboardAsync(query, includeSensitiveEmployeeData: false);

        Assert.NotNull(dashboard);
        Assert.Equal(ids.TenantId, dashboard.TenantId);
        Assert.Equal(ids.ContractId, dashboard.Filters.ContractId);
        Assert.Null(Assert.Single(dashboard.Assignments).EmployeeName);
        Assert.False(harness.Repository.LastIncludeSensitiveEmployeeData);
    }

    [Fact]
    public async Task TC_32_3_2_Report_includes_source_backed_snapshot_and_generated_date()
    {
        var ids = StoryIds.Create();
        var report = await CreateHarness(ids).Service.GenerateAsync(Request(ids), ids.ActorUserId, false);

        Assert.NotNull(report);
        Assert.NotEqual(default, report.GeneratedAt);
        Assert.Contains(report.Snapshot.Obligations, item => item.SourceClause == "FAR 52.222-41");
        Assert.Contains(report.Snapshot.Categories, item => item.Title == "Help Desk Technician II");
        Assert.Single(report.Snapshot.Assignments);
        Assert.Single(report.Snapshot.Gaps);
        Assert.Contains(report.Snapshot.EvidenceReferences, item => item.EvidenceItemId == ids.EvidenceItemId);
        Assert.Equal(ContentClassification.Unclassified, report.Classification.Classification);
    }

    [Fact]
    public async Task TC_32_3_3_Employee_sensitive_sections_require_explicit_server_authorization()
    {
        var ids = StoryIds.Create();
        var harness = CreateHarness(ids);

        var restricted = await harness.Service.GenerateAsync(Request(ids), ids.ActorUserId, false);
        var permitted = await harness.Service.GenerateAsync(Request(ids), ids.ActorUserId, true);

        Assert.Null(Assert.Single(restricted!.Snapshot.Assignments).EmployeeName);
        Assert.Equal("Taylor Employee", Assert.Single(permitted!.Snapshot.Assignments).EmployeeName);
    }

    [Fact]
    public async Task TC_32_3_4_Report_uses_workflow_language_without_legal_or_certification_claims()
    {
        var ids = StoryIds.Create();
        var report = await CreateHarness(ids).Service.GenerateAsync(Request(ids), ids.ActorUserId, false);

        Assert.Contains("Workflow guidance only", report!.Snapshot.WorkflowDisclaimer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not legal advice", report.Snapshot.WorkflowDisclaimer, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("certified compliant", report.Snapshot.WorkflowDisclaimer, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_32_3_5_Report_generation_is_audited_with_snapshot_counts()
    {
        var ids = StoryIds.Create();
        var harness = CreateHarness(ids);

        var report = await harness.Service.GenerateAsync(Request(ids), ids.ActorUserId, false);

        var auditEvent = Assert.Single(harness.AuditWriter.Events);
        Assert.Equal(report!.Id.ToString(), auditEvent.EntityId);
        Assert.Equal("Report", auditEvent.EntityType);
        Assert.Equal(AuditAction.Created, auditEvent.Action);
        Assert.Equal("1", auditEvent.Metadata["obligations"]);
        Assert.Equal(ids.ContractId.ToString(), auditEvent.Metadata["contractId"]);
    }

    private static LaborComplianceReportRequest Request(StoryIds ids) =>
        new(ids.ContractId, "Reviewed against the linked source records.", new LaborDashboardQuery(ids.ContractId),
            new ContentClassificationRequest(ContentClassification.Unclassified));

    private static StoryHarness CreateHarness(StoryIds ids)
    {
        var auditWriter = new CapturingAuditEventWriter();
        var repository = new StubLaborComplianceReportRepository(ids);
        var modePolicy = new TenantDataHandlingModePolicyService(
            new ServiceCollection().BuildServiceProvider(),
            new FixedTenantContext(ids.TenantId, ids.ActorUserId), auditWriter, new AcknowledgedNoticeGuard());
        return new StoryHarness(
            new LaborComplianceReportService(repository, auditWriter, modePolicy,
                new ContentClassificationPolicy(modePolicy), new TestApplicationTransaction()),
            repository, auditWriter);
    }

    private sealed class StubLaborComplianceReportRepository(StoryIds ids) : ILaborComplianceReportRepository
    {
        public bool LastIncludeSensitiveEmployeeData { get; private set; }

        public Task<LaborDashboardDto?> GetDashboardAsync(LaborDashboardQuery query, bool includeSensitiveEmployeeData,
            CancellationToken cancellationToken = default)
        {
            LastIncludeSensitiveEmployeeData = includeSensitiveEmployeeData;
            return Task.FromResult<LaborDashboardDto?>(Dashboard(query, includeSensitiveEmployeeData));
        }

        public Task<LaborComplianceReportDto?> GenerateAsync(LaborComplianceReportRequest request, Guid actorUserId,
            bool includeSensitiveEmployeeData, ContentClassificationRequest classification,
            CancellationToken cancellationToken = default)
        {
            LastIncludeSensitiveEmployeeData = includeSensitiveEmployeeData;
            var generatedAt = DateTimeOffset.UtcNow;
            var dashboard = Dashboard(request.Filters ?? new LaborDashboardQuery(request.ContractId), includeSensitiveEmployeeData);
            var snapshot = new LaborComplianceSnapshotDto(
                generatedAt, request.ContractId, "ActionRequired", LaborComplianceReportService.WorkflowDisclaimer,
                request.ReviewerNotes, dashboard.Filters, dashboard.Obligations, dashboard.Categories,
                dashboard.Assignments, dashboard.Gaps, dashboard.OverdueItems,
                dashboard.Assignments.SelectMany(item => item.EvidenceReferences).ToArray(),
                dashboard.EvidenceStatusCounts, includeSensitiveEmployeeData);
            return Task.FromResult<LaborComplianceReportDto?>(new LaborComplianceReportDto(
                ids.ReportId, ids.TenantId, ReportType.LaborCompliance, ReportStatus.Complete,
                "Labor compliance report", generatedAt, actorUserId, snapshot,
                new ContentClassificationDto(classification.Classification, classification.Source,
                    classification.Confidence, classification.ReviewedByUserId, classification.ReviewedAt,
                    classification.Reason, classification.IsApprovedDemoContent)));
        }

        private LaborDashboardDto Dashboard(LaborDashboardQuery query, bool includeSensitiveEmployeeData)
        {
            var evidence = new LaborEvidenceReferenceDto(ids.AssignmentId, ids.EvidenceItemId,
                LaborEvidenceType.ClassificationReview, "Classification review", "Approved");
            return new LaborDashboardDto(ids.TenantId, query,
                [new LaborObligationReportDto(ids.ObligationId, ids.ContractId, "SCA", "FAR 52.222-41",
                    "WD-2015-4341 Rev 24", "Norfolk, VA", LaborApplicabilityStatus.Active,
                    LaborApplicabilityReviewStatus.PendingReview, null, new DateOnly(2026, 12, 1), ids.EvidenceItemId)],
                [new LaborCategoryReportDto(ids.CategoryId, ids.ContractId, "Help Desk Technician II",
                    "Computer Operator IV", 34.12m, 4.98m, "WD-2015-4341 Rev 24", true)],
                [new LaborAssignmentReportDto(ids.AssignmentId, ids.ContractId, ids.EmployeeId,
                    includeSensitiveEmployeeData ? "Taylor Employee" : null,
                    includeSensitiveEmployeeData ? "taylor@example.test" : null,
                    ids.CategoryId, "Help Desk Technician II", "Norfolk, VA", LaborAssignmentStatus.Active,
                    LaborClassificationReviewStatus.PendingReview,
                    includeSensitiveEmployeeData ? "HR classification review 2026-01" : null,
                    "WD-2015-4341 Rev 24", [evidence])],
                [new LaborGapDto(ids.ContractId, ids.AssignmentId,
                    "Assignment classification review is incomplete.", "Review")],
                [new LaborOverdueItemDto(ids.ContractId, ids.ObligationId, "LaborObligation",
                    "Labor applicability review is overdue.", new DateOnly(2026, 12, 1))],
                new Dictionary<string, int> { ["Approved"] = 1 });
        }
    }

    private sealed class CapturingAuditEventWriter : IAuditEventWriter
    {
        public List<CapturedAuditEvent> Events { get; } = [];

        public Task WriteAsync(Guid tenantId, Guid actorUserId, AuditAction action, string entityType,
            string entityId, string summary, IReadOnlyDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            Events.Add(new CapturedAuditEvent(tenantId, action, entityType, entityId, metadata?.ToDictionary() ?? []));
            return Task.CompletedTask;
        }
    }

    private sealed record CapturedAuditEvent(Guid TenantId, AuditAction Action, string EntityType,
        string EntityId, IReadOnlyDictionary<string, string> Metadata);

    private sealed class FixedTenantContext(Guid tenantId, Guid userId) : ICurrentTenantContext
    {
        public Guid TenantId => tenantId;
        public Guid UserId => userId;
        public string UserEmail => "release-test@example.test";
    }

    private sealed class AcknowledgedNoticeGuard : ICurrentDataHandlingNoticeGuard
    {
        public Task EnsureAsync(string workflow, Guid actorUserId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed record StoryHarness(LaborComplianceReportService Service,
        StubLaborComplianceReportRepository Repository, CapturingAuditEventWriter AuditWriter);

    private sealed record StoryIds(Guid TenantId, Guid ContractId, Guid EmployeeId, Guid EvidenceItemId,
        Guid ReportId, Guid ObligationId, Guid CategoryId, Guid AssignmentId, Guid ActorUserId)
    {
        public static StoryIds Create() => new(
            Guid.Parse("32332332-2332-3323-3233-2332332332aa"),
            Guid.Parse("32332332-2332-3323-3233-2332332332bb"),
            Guid.Parse("32332332-2332-3323-3233-2332332332cc"),
            Guid.Parse("32332332-2332-3323-3233-2332332332dd"),
            Guid.Parse("32332332-2332-3323-3233-2332332332ee"),
            Guid.Parse("32332332-2332-3323-3233-2332332332f1"),
            Guid.Parse("32332332-2332-3323-3233-2332332332f2"),
            Guid.Parse("32332332-2332-3323-3233-2332332332f3"),
            Guid.Parse("32332332-2332-3323-3233-2332332332f4"));
    }
}
