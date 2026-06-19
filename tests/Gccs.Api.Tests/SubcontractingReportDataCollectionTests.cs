using Gccs.Application.Audit;
using Gccs.Application.Reports;
using Gccs.Domain.Audit;
using Gccs.Infrastructure.Reports;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class SubcontractingReportDataCollectionTests
{
    [Fact]
    public async Task TC_31_2_1_Create_report_data_linked_to_contract_and_subcontractor()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out _);

        var row = await service.CreateAsync(CreateRequest(ids), ids.TenantId, ids.ActorUserId);

        Assert.Equal(ids.TenantId, row.TenantId);
        Assert.Equal(ids.ContractId, row.ContractId);
        Assert.Equal(ids.SubcontractorId, row.SubcontractorId);
        Assert.Equal("Small Disadvantaged Business", row.SocioeconomicCategory);
        Assert.Equal("Direct subcontract spend", row.PlanCategory);
        Assert.Equal(12500.25m, row.Amount);
        Assert.Equal(SubcontractingReportDataReviewStatus.Draft, row.ReviewStatus);
        Assert.Equal("FAR 52.219-9", row.SourceReference);
    }

    [Fact]
    public async Task TC_31_2_2_Validation_rejects_negative_missing_duplicate_and_period_mismatch()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out _);
        await service.CreateAsync(CreateRequest(ids), ids.TenantId, ids.ActorUserId);

        await Assert.ThrowsAsync<SubcontractingReportDataValidationException>(() =>
            service.CreateAsync(CreateRequest(ids) with { Amount = -1 }, ids.TenantId, ids.ActorUserId));
        await Assert.ThrowsAsync<SubcontractingReportDataValidationException>(() =>
            service.CreateAsync(CreateRequest(ids) with { SocioeconomicCategory = " " }, ids.TenantId, ids.ActorUserId));
        await Assert.ThrowsAsync<SubcontractingReportDataValidationException>(() =>
            service.CreateAsync(CreateRequest(ids), ids.TenantId, ids.ActorUserId));
        await Assert.ThrowsAsync<SubcontractingReportDataValidationException>(() =>
            service.CreateAsync(
                CreateRequest(ids) with
                {
                    SubcontractorId = ids.SecondSubcontractorId,
                    RowPeriodStart = new DateOnly(2025, 12, 31)
                },
                ids.TenantId,
                ids.ActorUserId));
    }

    [Fact]
    public async Task TC_31_2_3_Evidence_link_is_returned_in_detail_and_package_preparation()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out _);
        var row = await service.CreateAsync(CreateRequest(ids), ids.TenantId, ids.ActorUserId);
        await service.UpdateReviewStatusAsync(row.Id, SubcontractingReportDataReviewStatus.Accepted, ids.ActorUserId);

        var detail = await service.FindAsync(row.Id);
        var packageRows = await service.PreparePackageRowsAsync(CreatePackageRequest(ids, FinalPackage: true));

        Assert.NotNull(detail);
        Assert.Equal([ids.EvidenceItemId], detail.SupportingEvidenceItemIds);
        var packageRow = Assert.Single(packageRows);
        Assert.Equal(row.Id, packageRow.RowId);
        Assert.Equal([ids.EvidenceItemId], packageRow.SupportingEvidenceItemIds);
    }

    [Fact]
    public async Task TC_31_2_4_Final_package_blocks_unreviewed_rows_unless_accepted()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out _);
        var row = await service.CreateAsync(CreateRequest(ids), ids.TenantId, ids.ActorUserId);

        await Assert.ThrowsAsync<SubcontractingReportDataValidationException>(() =>
            service.PreparePackageRowsAsync(CreatePackageRequest(ids, FinalPackage: true)));

        await service.UpdateReviewStatusAsync(row.Id, SubcontractingReportDataReviewStatus.Accepted, ids.ActorUserId);

        var packageRows = await service.PreparePackageRowsAsync(CreatePackageRequest(ids, FinalPackage: true));
        Assert.Single(packageRows);
    }

    [Fact]
    public async Task TC_31_2_5_Create_update_accept_and_reject_report_data_rows_are_audited()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out var auditWriter);
        var row = await service.CreateAsync(CreateRequest(ids), ids.TenantId, ids.ActorUserId);
        await service.UpdateAsync(row.Id, CreateRequest(ids) with { Amount = 15000m }, ids.ActorUserId);
        await service.UpdateReviewStatusAsync(row.Id, SubcontractingReportDataReviewStatus.Accepted, ids.ActorUserId, "Ready for package.");
        await service.UpdateReviewStatusAsync(row.Id, SubcontractingReportDataReviewStatus.Rejected, ids.ActorUserId, "Needs correction.");

        Assert.Equal(4, auditWriter.Events.Count);
        Assert.All(auditWriter.Events, auditEvent =>
        {
            Assert.Equal(ids.TenantId, auditEvent.TenantId);
            Assert.Equal(ids.ActorUserId, auditEvent.ActorUserId);
            Assert.Equal("SubcontractingReportDataRow", auditEvent.EntityType);
        });
        Assert.Equal(AuditAction.Created, auditWriter.Events[0].Action);
        Assert.Equal("15000.00", auditWriter.Events[1].Metadata["amount"]);
        Assert.Equal("Accepted", auditWriter.Events[2].Metadata["reviewStatus"]);
        Assert.Equal("Rejected", auditWriter.Events[3].Metadata["reviewStatus"]);
    }

    [Fact]
    public void Import_template_lists_manual_entry_columns()
    {
        var template = SubcontractingReportDataService.GetImportTemplate();

        Assert.Equal("subcontracting-report-data-template.csv", template.FileName);
        Assert.Contains("subcontractorId", template.Columns);
        Assert.Contains("socioeconomicCategory", template.Columns);
        Assert.Contains("supportingEvidenceItemIds", template.Columns);
    }

    private static SubcontractingReportDataService CreateService(out CapturingAuditEventWriter auditWriter)
    {
        auditWriter = new CapturingAuditEventWriter();
        return new SubcontractingReportDataService(new InMemorySubcontractingReportDataRepository(), auditWriter);
    }

    private static SubcontractingReportDataRowRequest CreateRequest(StoryIds ids) =>
        new(
            ids.ContractId,
            ids.SubcontractorId,
            EsrsReportType.Isr,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 3, 31),
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 3, 31),
            "Small Disadvantaged Business",
            "Direct subcontract spend",
            12500.25m,
            [ids.EvidenceItemId],
            "FAR 52.219-9");

    private static SubcontractingReportPackageRowsRequest CreatePackageRequest(StoryIds ids, bool FinalPackage) =>
        new(
            ids.TenantId,
            ids.ContractId,
            EsrsReportType.Isr,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 3, 31),
            FinalPackage);

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
                Guid.Parse("31231231-1231-2312-3123-1231231231aa"),
                Guid.Parse("31231231-1231-2312-3123-1231231231bb"),
                Guid.Parse("31231231-1231-2312-3123-1231231231cc"),
                Guid.Parse("31231231-1231-2312-3123-1231231231dd"),
                Guid.Parse("31231231-1231-2312-3123-1231231231ee"),
                Guid.Parse("31231231-1231-2312-3123-1231231231ff"));
    }
}
