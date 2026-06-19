using Gccs.Application.Audit;
using Gccs.Application.Labor;
using Gccs.Domain.Audit;
using Gccs.Infrastructure.Labor;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class LaborEvidenceComplianceReportTests
{
    [Fact]
    public async Task TC_32_3_1_Dashboard_filters_current_tenant_labor_obligations_assignments_gaps_and_overdue()
    {
        var ids = StoryIds.Create();
        var harness = CreateHarness();
        await SeedLaborDataAsync(harness, ids, includeSensitiveOtherTenant: true);

        var dashboard = await harness.ReportService.GetDashboardAsync(
            new LaborDashboardQuery(ids.TenantId, ids.ContractId, new DateOnly(2027, 1, 1)));

        Assert.Equal(ids.TenantId, dashboard.TenantId);
        Assert.Equal(ids.ContractId, dashboard.ContractId);
        Assert.Equal(2, dashboard.Obligations.Count);
        Assert.Single(dashboard.Assignments);
        Assert.Single(dashboard.Gaps);
        Assert.Equal(2, dashboard.OverdueItems);
        Assert.DoesNotContain(dashboard.Assignments, assignment => assignment.ContractId == ids.OtherTenantContractId);
    }

    [Fact]
    public async Task TC_32_3_2_Report_includes_sources_wage_determinations_categories_assignments_gaps_evidence_and_generated_date()
    {
        var ids = StoryIds.Create();
        var harness = CreateHarness();
        await SeedLaborDataAsync(harness, ids);

        var report = await harness.ReportService.GenerateAsync(new LaborComplianceReportRequest(ids.TenantId, ids.ContractId), ids.ActorUserId);

        Assert.NotEqual(default, report.GeneratedAt);
        Assert.Contains(report.Obligations, obligation => obligation.SourceClause == "FAR 52.222-41");
        Assert.Contains(report.Obligations, obligation => obligation.WageDeterminationReference == "WD-2015-4341 Rev 24");
        Assert.Contains(report.Categories, category => category.Title == "Help Desk Technician II");
        Assert.Single(report.Assignments);
        Assert.Single(report.Gaps);
        Assert.Contains(report.EvidenceReferences, reference => reference.EvidenceItemId == ids.EvidenceItemId);
    }

    [Fact]
    public async Task TC_32_3_3_Employee_sensitive_sections_require_hr_permission()
    {
        var ids = StoryIds.Create();
        var harness = CreateHarness();
        await SeedLaborDataAsync(harness, ids);

        var restricted = await harness.ReportService.GenerateAsync(
            new LaborComplianceReportRequest(ids.TenantId, ids.ContractId, IncludeSensitiveEmployeeData: false),
            ids.ActorUserId);
        var hr = await harness.ReportService.GenerateAsync(
            new LaborComplianceReportRequest(ids.TenantId, ids.ContractId, IncludeSensitiveEmployeeData: true),
            ids.ActorUserId);

        Assert.Null(Assert.Single(restricted.Assignments).EmployeeName);
        Assert.Null(Assert.Single(restricted.Assignments).EmployeeEmail);
        Assert.Equal("Taylor Employee", Assert.Single(hr.Assignments).EmployeeName);
        Assert.Equal("taylor@example.test", Assert.Single(hr.Assignments).EmployeeEmail);
    }

    [Fact]
    public async Task TC_32_3_4_Report_presents_workflow_status_without_final_legal_determination_language()
    {
        var ids = StoryIds.Create();
        var harness = CreateHarness();
        await SeedLaborDataAsync(harness, ids);

        var report = await harness.ReportService.GenerateAsync(new LaborComplianceReportRequest(ids.TenantId, ids.ContractId), ids.ActorUserId);

        Assert.Contains("workflow status", report.WorkflowDisclaimer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not a final legal determination", report.WorkflowDisclaimer, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("certified compliant", report.WorkflowDisclaimer, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_32_3_5_Report_generation_and_export_are_audit_logged()
    {
        var ids = StoryIds.Create();
        var harness = CreateHarness();
        await SeedLaborDataAsync(harness, ids);
        var report = await harness.ReportService.GenerateAsync(new LaborComplianceReportRequest(ids.TenantId, ids.ContractId), ids.ActorUserId);

        var export = await harness.ReportService.ExportAsync(report, ids.ActorUserId);

        Assert.Equal("labor-compliance-report.csv", export.FileName);
        var reportEvents = harness.AuditWriter.Events.Where(auditEvent => auditEvent.EntityType == "LaborComplianceReport").ToArray();
        Assert.Equal(2, reportEvents.Length);
        Assert.Equal(AuditAction.Created, reportEvents[0].Action);
        Assert.Equal(AuditAction.Exported, reportEvents[1].Action);
        Assert.Equal("2", reportEvents[0].Metadata["obligations"]);
        Assert.Equal(ids.ContractId.ToString(), reportEvents[1].Metadata["contractId"]);
    }

    private static async Task SeedLaborDataAsync(StoryHarness harness, StoryIds ids, bool includeSensitiveOtherTenant = false)
    {
        var activeWithEvidence = await harness.ApplicabilityService.RecordAsync(
            CreateApplicability(ids, ids.ContractId, ids.EvidenceItemId, "FAR 52.222-41"),
            ids.TenantId,
            ids.ActorUserId);
        await harness.ApplicabilityService.ActivateAsync(activeWithEvidence.Id, ids.ActorUserId);
        var activeMissingEvidence = await harness.ApplicabilityService.RecordAsync(
            CreateApplicability(ids, ids.ContractId, null, "FAR 52.222-55"),
            ids.TenantId,
            ids.ActorUserId);
        await harness.ApplicabilityService.ActivateAsync(activeMissingEvidence.Id, ids.ActorUserId);

        var category = await harness.ClassificationService.CreateCategoryAsync(CreateCategory(ids, ids.ContractId), ids.TenantId, ids.ActorUserId);
        await harness.ClassificationService.CreateAssignmentAsync(CreateAssignment(ids, ids.ContractId, category.Id), ids.TenantId, ids.ActorUserId);

        if (includeSensitiveOtherTenant)
        {
            var otherCategory = await harness.ClassificationService.CreateCategoryAsync(
                CreateCategory(ids, ids.OtherTenantContractId),
                ids.OtherTenantId,
                ids.ActorUserId);
            await harness.ClassificationService.CreateAssignmentAsync(
                CreateAssignment(ids, ids.OtherTenantContractId, otherCategory.Id),
                ids.OtherTenantId,
                ids.ActorUserId);
        }
    }

    private static LaborApplicabilityRequest CreateApplicability(StoryIds ids, Guid contractId, Guid? evidenceId, string sourceClause) =>
        new(
            contractId,
            "SCA",
            "Norfolk, VA",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            "WD-2015-4341 Rev 24",
            evidenceId,
            sourceClause,
            "Labor compliance source-backed applicability.",
            "Contracts/HR");

    private static LaborCategoryRequest CreateCategory(StoryIds ids, Guid contractId) =>
        new(
            contractId,
            "Help Desk Technician II",
            "Computer Operator IV",
            34.12m,
            4.98m,
            "Health and welfare fringe",
            new DateOnly(2026, 1, 1),
            null,
            "WD-2015-4341 Rev 24");

    private static LaborEmployeeAssignmentRequest CreateAssignment(StoryIds ids, Guid contractId, Guid categoryId) =>
        new(
            ids.EmployeeId,
            "Taylor Employee",
            "taylor@example.test",
            contractId,
            categoryId,
            "Norfolk, VA",
            new DateOnly(2026, 1, 1),
            null,
            "HR classification review 2026-01");

    private static StoryHarness CreateHarness()
    {
        var auditWriter = new CapturingAuditEventWriter();
        var applicabilityRepository = new InMemoryLaborApplicabilityRepository();
        var classificationRepository = new InMemoryLaborClassificationRepository();
        return new StoryHarness(
            new LaborApplicabilityService(applicabilityRepository, new AllowingLaborUploadGuard(), auditWriter),
            new LaborClassificationService(classificationRepository, auditWriter),
            new LaborComplianceReportService(applicabilityRepository, classificationRepository, auditWriter),
            auditWriter);
    }

    private sealed class AllowingLaborUploadGuard : ILaborWageDeterminationUploadGuard
    {
        public Task EnsureAllowedAsync(WageDeterminationUploadRequest request, Guid actorUserId, CancellationToken cancellationToken = default) =>
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

    private sealed record StoryHarness(
        LaborApplicabilityService ApplicabilityService,
        LaborClassificationService ClassificationService,
        LaborComplianceReportService ReportService,
        CapturingAuditEventWriter AuditWriter);

    private sealed record StoryIds(
        Guid TenantId,
        Guid OtherTenantId,
        Guid ContractId,
        Guid OtherTenantContractId,
        Guid EmployeeId,
        Guid EvidenceItemId,
        Guid ActorUserId)
    {
        public static StoryIds Create() =>
            new(
                Guid.Parse("32332332-2332-3323-3233-2332332332aa"),
                Guid.Parse("32332332-2332-3323-3233-2332332332ab"),
                Guid.Parse("32332332-2332-3323-3233-2332332332bb"),
                Guid.Parse("32332332-2332-3323-3233-2332332332bc"),
                Guid.Parse("32332332-2332-3323-3233-2332332332cc"),
                Guid.Parse("32332332-2332-3323-3233-2332332332dd"),
                Guid.Parse("32332332-2332-3323-3233-2332332332ee"));
    }
}
