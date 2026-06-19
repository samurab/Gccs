using Gccs.Application.Audit;
using Gccs.Application.Reports;
using Gccs.Domain.Audit;
using Gccs.Infrastructure.Reports;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class EsrsReportPackageTests
{
    [Fact]
    public async Task TC_31_3_1_Generate_package_with_spend_summaries_exceptions_evidence_and_generated_date()
    {
        var ids = StoryIds.Create();
        var service = CreateServices(out var reportDataService, out _);
        var rowWithEvidence = await CreateAcceptedRowAsync(reportDataService, ids, ids.SubcontractorId, [ids.EvidenceItemId]);
        await CreateAcceptedRowAsync(reportDataService, ids, ids.SecondSubcontractorId, []);

        var package = await service.GenerateAsync(CreateGenerateRequest(ids), ids.ActorUserId);

        Assert.Equal(ids.TenantId, package.TenantId);
        Assert.Equal(ids.ContractId, package.ContractId);
        Assert.Equal(EsrsReportType.Isr, package.ReportType);
        Assert.NotEqual(default, package.GeneratedAt);
        Assert.Equal(2, package.Snapshot.RowCount);
        Assert.Equal(25000m, package.Snapshot.TotalSpend);
        var summary = Assert.Single(package.Snapshot.SpendSummaries);
        Assert.Equal("Small Disadvantaged Business", summary.SocioeconomicCategory);
        Assert.Equal(2, summary.SubcontractorCount);
        Assert.Equal(25000m, summary.TotalSpend);
        Assert.Contains(package.Snapshot.EvidenceReferences, reference =>
            reference.RowId == rowWithEvidence.Id && reference.EvidenceItemId == ids.EvidenceItemId);
        Assert.Single(package.Snapshot.Exceptions);
    }

    [Fact]
    public async Task TC_31_3_2_Package_states_gccs_has_not_submitted_report_to_esrs()
    {
        var ids = StoryIds.Create();
        var service = CreateServices(out var reportDataService, out _);
        await CreateAcceptedRowAsync(reportDataService, ids, ids.SubcontractorId, [ids.EvidenceItemId]);

        var package = await service.GenerateAsync(CreateGenerateRequest(ids), ids.ActorUserId);

        Assert.Contains("has not submitted", package.NotSubmittedDisclaimer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("eSRS", package.NotSubmittedDisclaimer, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TC_31_3_3_Approve_package_stores_reviewer_approval_date_version_and_notes()
    {
        var ids = StoryIds.Create();
        var service = CreateServices(out var reportDataService, out _);
        await CreateAcceptedRowAsync(reportDataService, ids, ids.SubcontractorId, [ids.EvidenceItemId]);
        var package = await service.GenerateAsync(CreateGenerateRequest(ids), ids.ActorUserId);

        var approved = await service.ApproveAsync(
            package.Id,
            new EsrsReportPackageReviewRequest("Avery Reviewer", "Approved for eSRS preparation."),
            ids.ActorUserId);

        Assert.NotNull(approved);
        Assert.Equal(EsrsReportPackageStatus.Approved, approved.Status);
        Assert.Equal("Avery Reviewer", approved.ReviewerName);
        Assert.NotNull(approved.ApprovedAt);
        Assert.Equal(1, approved.Version);
        Assert.Equal("Approved for eSRS preparation.", approved.ReviewNotes);
    }

    [Fact]
    public async Task TC_31_3_4_Package_generation_approval_and_viewing_permissions_are_enforced()
    {
        var ids = StoryIds.Create();
        var service = CreateServices(out var reportDataService, out _);
        await CreateAcceptedRowAsync(reportDataService, ids, ids.SubcontractorId, [ids.EvidenceItemId]);
        var package = await service.GenerateAsync(CreateGenerateRequest(ids), ids.ActorUserId);

        await Assert.ThrowsAsync<EsrsReportPackageException>(() =>
            service.GenerateAsync(CreateGenerateRequest(ids) with { HasReportPermission = false }, ids.ActorUserId));
        await Assert.ThrowsAsync<EsrsReportPackageException>(() =>
            service.ApproveAsync(package.Id, new EsrsReportPackageReviewRequest("Reviewer", null, HasReportPermission: false), ids.ActorUserId));
        await Assert.ThrowsAsync<EsrsReportPackageException>(() =>
            service.FindAsync(package.Id, hasReportPermission: false));
    }

    [Fact]
    public async Task TC_31_3_5_Generation_approval_supersede_and_archive_are_audit_logged()
    {
        var ids = StoryIds.Create();
        var service = CreateServices(out var reportDataService, out var auditWriter);
        await CreateAcceptedRowAsync(reportDataService, ids, ids.SubcontractorId, [ids.EvidenceItemId]);
        var package = await service.GenerateAsync(CreateGenerateRequest(ids), ids.ActorUserId);
        await service.ApproveAsync(package.Id, new EsrsReportPackageReviewRequest("Reviewer", "Approved."), ids.ActorUserId);
        await service.SupersedeAsync(package.Id, new EsrsReportPackageReviewRequest("Reviewer", "Superseded by v2."), ids.ActorUserId);
        await service.ArchiveAsync(package.Id, new EsrsReportPackageReviewRequest("Reviewer", "Archived."), ids.ActorUserId);

        var packageEvents = auditWriter.Events.Where(auditEvent => auditEvent.EntityType == "EsrsReportPackage").ToArray();
        Assert.Equal(4, packageEvents.Length);
        Assert.Equal(AuditAction.Created, packageEvents[0].Action);
        Assert.Equal(AuditAction.Approved, packageEvents[1].Action);
        Assert.Equal("Superseded", packageEvents[2].Metadata["status"]);
        Assert.Equal(AuditAction.Archived, packageEvents[3].Action);
        Assert.All(packageEvents, auditEvent =>
        {
            Assert.Equal(ids.TenantId, auditEvent.TenantId);
            Assert.Equal(ids.ActorUserId, auditEvent.ActorUserId);
            Assert.Equal("12500.00", auditEvent.Metadata["totalSpend"]);
        });
    }

    private static EsrsReportPackageService CreateServices(
        out SubcontractingReportDataService reportDataService,
        out CapturingAuditEventWriter auditWriter)
    {
        auditWriter = new CapturingAuditEventWriter();
        reportDataService = new SubcontractingReportDataService(new InMemorySubcontractingReportDataRepository(), auditWriter);
        return new EsrsReportPackageService(reportDataService, new InMemoryEsrsReportPackageRepository(), auditWriter);
    }

    private static async Task<SubcontractingReportDataRowDto> CreateAcceptedRowAsync(
        SubcontractingReportDataService service,
        StoryIds ids,
        Guid subcontractorId,
        IReadOnlyList<Guid> evidenceIds)
    {
        var row = await service.CreateAsync(
            new SubcontractingReportDataRowRequest(
                ids.ContractId,
                subcontractorId,
                EsrsReportType.Isr,
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 3, 31),
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 3, 31),
                "Small Disadvantaged Business",
                "Direct subcontract spend",
                12500m,
                evidenceIds,
                "FAR 52.219-9"),
            ids.TenantId,
            ids.ActorUserId);
        return await service.UpdateReviewStatusAsync(row.Id, SubcontractingReportDataReviewStatus.Accepted, ids.ActorUserId) ??
            throw new InvalidOperationException("Expected report data row to be accepted.");
    }

    private static EsrsReportPackageGenerateRequest CreateGenerateRequest(StoryIds ids) =>
        new(
            ids.TenantId,
            ids.ContractId,
            EsrsReportType.Isr,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 3, 31));

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

    private sealed record StoryIds(
        Guid TenantId,
        Guid ContractId,
        Guid SubcontractorId,
        Guid SecondSubcontractorId,
        Guid EvidenceItemId,
        Guid ActorUserId)
    {
        public static StoryIds Create() =>
            new(
                Guid.Parse("31331331-1331-3313-3133-1331331331aa"),
                Guid.Parse("31331331-1331-3313-3133-1331331331bb"),
                Guid.Parse("31331331-1331-3313-3133-1331331331cc"),
                Guid.Parse("31331331-1331-3313-3133-1331331331dd"),
                Guid.Parse("31331331-1331-3313-3133-1331331331ee"),
                Guid.Parse("31331331-1331-3313-3133-1331331331ff"));
    }
}
