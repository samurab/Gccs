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
        var service = CreateServices(ids.TenantId, out var reportDataService, out var auditWriter);
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
        Assert.Equal("Small Disadvantaged Business (SDB)", summary.SocioeconomicCategory);
        Assert.Equal(2, summary.SubcontractorCount);
        Assert.Equal(25000m, summary.TotalSpend);
        Assert.Contains(package.Snapshot.EvidenceReferences, reference =>
            reference.RowId == rowWithEvidence.Id && reference.EvidenceItemId == ids.EvidenceItemId);
        Assert.Single(package.Snapshot.Exceptions);
        var schema = Assert.Single(package.Snapshot.SchemaProfiles);
        Assert.Equal("1.0", schema.Version);
        Assert.Equal(64, schema.DefinitionSha256.Length);
    }

    [Fact]
    public async Task TC_31_3_2_Package_states_fedril_has_not_submitted_report_to_sam_gov()
    {
        var ids = StoryIds.Create();
        var service = CreateServices(ids.TenantId, out var reportDataService, out _);
        await CreateAcceptedRowAsync(reportDataService, ids, ids.SubcontractorId, [ids.EvidenceItemId]);

        var package = await service.GenerateAsync(CreateGenerateRequest(ids), ids.ActorUserId);

        Assert.Contains("has not submitted", package.NotSubmittedDisclaimer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SAM.gov", package.NotSubmittedDisclaimer, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TC_31_3_3_Approve_package_stores_reviewer_approval_date_version_and_notes()
    {
        var ids = StoryIds.Create();
        var service = CreateServices(ids.TenantId, out var reportDataService, out _);
        await CreateAcceptedRowAsync(reportDataService, ids, ids.SubcontractorId, [ids.EvidenceItemId]);
        var package = await service.GenerateAsync(CreateGenerateRequest(ids), ids.ActorUserId);
        await Assert.ThrowsAsync<EsrsReportPackageException>(() => service.ApproveAsync(
            package.Id,
            new EsrsReportPackageReviewRequest("Avery Reviewer", "Approval cannot skip review."),
            ids.ActorUserId));
        await service.BeginReviewAsync(package.Id,
            new EsrsReportPackageReviewRequest("Avery Reviewer", "Review started."), ids.ActorUserId);

        var approved = await service.ApproveAsync(
            package.Id,
            new EsrsReportPackageReviewRequest("Avery Reviewer", "Approved for SAM.gov SPR preparation."),
            ids.ActorUserId);

        Assert.NotNull(approved);
        Assert.Equal(EsrsReportPackageStatus.Approved, approved.Status);
        Assert.Equal("Avery Reviewer", approved.ReviewerName);
        Assert.NotNull(approved.ApprovedAt);
        Assert.Equal(1, approved.Version);
        Assert.Equal("Approved for SAM.gov SPR preparation.", approved.ReviewNotes);
    }

    [Fact]
    public async Task Invalid_manual_receipt_outcome_is_rejected_without_an_audit_event()
    {
        var ids = StoryIds.Create();
        var service = CreateServices(ids.TenantId, out var reportDataService, out var auditWriter);
        await CreateAcceptedRowAsync(reportDataService, ids, ids.SubcontractorId, [ids.EvidenceItemId]);
        var package = await service.GenerateAsync(CreateGenerateRequest(ids), ids.ActorUserId);
        await service.BeginReviewAsync(package.Id, new EsrsReportPackageReviewRequest("Reviewer", "Review started."), ids.ActorUserId);
        await service.ApproveAsync(package.Id, new EsrsReportPackageReviewRequest("Reviewer", "Approved."), ids.ActorUserId);
        var auditCount = auditWriter.Events.Count;

        await Assert.ThrowsAsync<EsrsReportPackageException>(() => service.RecordManualSubmissionReceiptAsync(package.Id,
            new SprManualSubmissionReceiptRequest(DateTimeOffset.UtcNow, "SAM-INVALID", (SprManualSubmissionOutcome)999, null, null),
            ids.ActorUserId));

        Assert.Equal(auditCount, auditWriter.Events.Count);
    }

    [Fact]
    public async Task Review_lifecycle_supports_in_review_and_blocks_empty_package_approval()
    {
        var ids = StoryIds.Create();
        var service = CreateServices(ids.TenantId, out _, out _);
        var empty = await service.GenerateAsync(CreateGenerateRequest(ids), ids.ActorUserId);
        var inReview = await service.BeginReviewAsync(empty.Id,
            new EsrsReportPackageReviewRequest("Reviewer", "Review started."), ids.ActorUserId);
        Assert.Equal(EsrsReportPackageStatus.InReview, inReview!.Status);
        await Assert.ThrowsAsync<EsrsReportPackageException>(() => service.ApproveAsync(empty.Id,
            new EsrsReportPackageReviewRequest("Reviewer", "Approved."), ids.ActorUserId));
    }

    [Fact]
    public async Task Lifecycle_repository_rejects_a_stale_expected_status_without_overwriting_history()
    {
        var ids = StoryIds.Create();
        var repository = new InMemoryEsrsReportPackageRepository(ids.TenantId);
        var package = await repository.CreateAsync(CreateGenerateRequest(ids),
            new EsrsReportPackageSnapshotDto(ids.ContractId, EsrsReportType.Isr, new(2026, 1, 1), new(2026, 3, 31),
                0, 0, [], [], ["No eligible rows."], []), ids.ActorUserId);
        await repository.UpdateStatusAsync(package.Id, EsrsReportPackageStatus.Draft, EsrsReportPackageStatus.InReview,
            "First reviewer", "Review started.", ids.ActorUserId);

        await Assert.ThrowsAsync<EsrsReportPackageConflictException>(() => repository.UpdateStatusAsync(package.Id,
            EsrsReportPackageStatus.Draft, EsrsReportPackageStatus.Archived, "Stale reviewer", "Stale archive.", ids.ActorUserId));

        var stored = await repository.FindAsync(package.Id);
        Assert.Equal(EsrsReportPackageStatus.InReview, stored!.Status);
        Assert.Equal("First reviewer", stored.ReviewerName);
    }

    [Fact]
    public async Task TC_31_3_5_Generation_approval_supersede_and_archive_are_audit_logged()
    {
        var ids = StoryIds.Create();
        var service = CreateServices(ids.TenantId, out var reportDataService, out var auditWriter);
        await CreateAcceptedRowAsync(reportDataService, ids, ids.SubcontractorId, [ids.EvidenceItemId]);
        var package = await service.GenerateAsync(CreateGenerateRequest(ids), ids.ActorUserId);
        await service.BeginReviewAsync(package.Id, new EsrsReportPackageReviewRequest("Reviewer", "Review started."), ids.ActorUserId);
        await service.ApproveAsync(package.Id, new EsrsReportPackageReviewRequest("Reviewer", "Approved."), ids.ActorUserId);
        await service.SupersedeAsync(package.Id, new EsrsReportPackageReviewRequest("Reviewer", "Superseded by v2."), ids.ActorUserId);
        await service.ArchiveAsync(package.Id, new EsrsReportPackageReviewRequest("Reviewer", "Archived."), ids.ActorUserId);

        var packageEvents = auditWriter.Events.Where(auditEvent => auditEvent.EntityType == "EsrsReportPackage").ToArray();
        Assert.Equal(5, packageEvents.Length);
        Assert.Equal(AuditAction.Created, packageEvents[0].Action);
        Assert.Equal("InReview", packageEvents[1].Metadata["status"]);
        Assert.Equal(AuditAction.Approved, packageEvents[2].Action);
        Assert.Equal("Superseded", packageEvents[3].Metadata["status"]);
        Assert.Equal(AuditAction.Archived, packageEvents[4].Action);
        Assert.All(packageEvents, auditEvent =>
        {
            Assert.Equal(ids.TenantId, auditEvent.TenantId);
            Assert.Equal(ids.ActorUserId, auditEvent.ActorUserId);
            Assert.Equal("12500.00", auditEvent.Metadata["totalSpend"]);
        });
    }

    [Fact]
    public async Task Export_is_traceable_and_never_claims_that_fedril_submitted_the_report()
    {
        var ids = StoryIds.Create();
        var service = CreateServices(ids.TenantId, out var reportDataService, out var auditWriter);
        await CreateAcceptedRowAsync(reportDataService, ids, ids.SubcontractorId, [ids.EvidenceItemId]);
        var package = await service.GenerateAsync(CreateGenerateRequest(ids), ids.ActorUserId);

        await service.BeginReviewAsync(package.Id, new EsrsReportPackageReviewRequest("Avery Reviewer", "Review started."), ids.ActorUserId);
        await service.ApproveAsync(package.Id, new EsrsReportPackageReviewRequest("Avery Reviewer", "Approved for manual entry."), ids.ActorUserId);
        var export = await service.ExportAsync(package.Id, SprPackageExportFormat.Html, ids.ActorUserId);

        Assert.NotNull(export);
        Assert.Contains("has not submitted", export.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"Contract: {ids.ContractId}", export.Content, StringComparison.Ordinal);
        Assert.Contains("Report type: ISR", export.Content, StringComparison.Ordinal);
        Assert.Contains("2026-01-01 through 2026-03-31", export.Content, StringComparison.Ordinal);
        Assert.Contains(ids.EvidenceItemId.ToString(), export.Content, StringComparison.Ordinal);
        Assert.Contains("Avery Reviewer", export.Content, StringComparison.Ordinal);
        Assert.Contains("Approved for manual entry.", export.Content, StringComparison.Ordinal);
        var schema = Assert.Single(package.Snapshot.SchemaProfiles);
        var decodedHtml = System.Net.WebUtility.HtmlDecode(export.Content);
        Assert.Contains(schema.SourceUrl, decodedHtml, StringComparison.Ordinal);
        Assert.Contains(schema.DefinitionSha256, decodedHtml, StringComparison.Ordinal);
        Assert.Contains(auditWriter.Events, item => item.Action == AuditAction.Exported && item.EntityId == package.Id.ToString());
    }

    [Fact]
    public async Task Json_export_uses_reviewable_string_metadata_and_preserves_the_disclaimer()
    {
        var ids = StoryIds.Create();
        var service = CreateServices(ids.TenantId, out var reportDataService, out _);
        await CreateAcceptedRowAsync(reportDataService, ids, ids.SubcontractorId, [ids.EvidenceItemId]);
        var package = await service.GenerateAsync(CreateGenerateRequest(ids), ids.ActorUserId);

        var export = await service.ExportAsync(package.Id, SprPackageExportFormat.Json, ids.ActorUserId);

        Assert.NotNull(export);
        Assert.Contains("\"reportType\": \"Isr\"", export.Content, StringComparison.Ordinal);
        Assert.Contains("\"status\": \"Draft\"", export.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("\"reportType\": 0", export.Content, StringComparison.Ordinal);
        Assert.Contains(EsrsReportPackageService.NotSubmittedDisclaimer, export.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Manual_receipts_are_append_only_for_approved_packages_and_are_audited()
    {
        var ids = StoryIds.Create();
        var service = CreateServices(ids.TenantId, out var reportDataService, out var auditWriter);
        await CreateAcceptedRowAsync(reportDataService, ids, ids.SubcontractorId, [ids.EvidenceItemId]);
        var package = await service.GenerateAsync(CreateGenerateRequest(ids), ids.ActorUserId);
        var request = new SprManualSubmissionReceiptRequest(DateTimeOffset.UtcNow, "SAM-REF-1",
            SprManualSubmissionOutcome.Submitted, "Recorded from the customer's SAM.gov confirmation.", ids.EvidenceItemId);
        await Assert.ThrowsAsync<EsrsReportPackageException>(() =>
            service.RecordManualSubmissionReceiptAsync(package.Id, request, ids.ActorUserId));
        await service.BeginReviewAsync(package.Id, new EsrsReportPackageReviewRequest("Reviewer", "Review started."), ids.ActorUserId);
        await service.ApproveAsync(package.Id, new EsrsReportPackageReviewRequest("Reviewer", "Approved."), ids.ActorUserId);

        var first = await service.RecordManualSubmissionReceiptAsync(package.Id, request, ids.ActorUserId);
        await Assert.ThrowsAsync<EsrsReportPackageException>(() => service.RecordManualSubmissionReceiptAsync(package.Id,
            request with { ConfirmationReference = "SAM-REF-UNLINKED", Outcome = SprManualSubmissionOutcome.Corrected,
                Notes = "Correction without a prior receipt." }, ids.ActorUserId));
        var correction = await service.RecordManualSubmissionReceiptAsync(package.Id,
            request with { ConfirmationReference = "SAM-REF-2", Outcome = SprManualSubmissionOutcome.Corrected,
                Notes = "Corrected in SAM.gov.", SupersedesReceiptId = first!.Id }, ids.ActorUserId);

        Assert.NotNull(correction);
        var receipts = await service.ListManualSubmissionReceiptsAsync(package.Id);
        Assert.Equal(2, receipts.Count);
        Assert.Contains(receipts, receipt => receipt.Id == first.Id);
        Assert.Contains(receipts, receipt => receipt.SupersedesReceiptId == first.Id);
        Assert.Equal(2, auditWriter.Events.Count(item => item.EntityType == "SprManualSubmissionReceipt"));
    }

    [Fact]
    public async Task Submission_capability_fails_closed_without_an_authorized_provider()
    {
        var ids = StoryIds.Create();
        var service = CreateServices(ids.TenantId, out var reportDataService, out _);
        await CreateAcceptedRowAsync(reportDataService, ids, ids.SubcontractorId, [ids.EvidenceItemId]);
        var package = await service.GenerateAsync(CreateGenerateRequest(ids), ids.ActorUserId);
        await service.BeginReviewAsync(package.Id, new EsrsReportPackageReviewRequest("Reviewer", "Review started."), ids.ActorUserId);
        await service.ApproveAsync(package.Id, new EsrsReportPackageReviewRequest("Reviewer", "Approved."), ids.ActorUserId);

        Assert.False(service.GetSubmissionCapability().Enabled);
        await Assert.ThrowsAsync<SprSubmissionUnavailableException>(() => service.SubmitAsync(package.Id,
            new SprSubmissionRequest("submission-attempt-1"), ids.ActorUserId));
    }

    private static EsrsReportPackageService CreateServices(Guid tenantId,
        out SubcontractingReportDataService reportDataService,
        out CapturingAuditEventWriter auditWriter)
    {
        auditWriter = new CapturingAuditEventWriter();
        reportDataService = new SubcontractingReportDataService(new InMemorySubcontractingReportDataRepository(tenantId),
            new SprSchemaProfileService(new InMemorySprSchemaProfileRepository()), auditWriter, new TestApplicationTransaction());
        return new EsrsReportPackageService(reportDataService, new InMemoryEsrsReportPackageRepository(tenantId),
            new DisabledSprSubmissionProvider(), auditWriter, new TestApplicationTransaction());
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
                "Small Disadvantaged Business (SDB)",
                "Direct subcontract spend",
                12500m,
                evidenceIds,
                "FAR 52.219-9",
                ReportingRole: SprReportingRole.PrimeContractor,
                ReportingFiscalYear: 2026,
                ReportingPeriod: SprReportingPeriod.March31,
                ReportingEntityUei: "TESTUEI12345",
                PrimeContractPiid: "FA-TEST-312",
                SprEligibilityConfirmed: true,
                SprEligibilityBasis: "Qualifying individual subcontracting plan."),
            ids.ActorUserId);
        return await service.UpdateReviewStatusAsync(row.Id, new(SubcontractingReportDataReviewStatus.Accepted, null, row.Version), ids.ActorUserId) ??
            throw new InvalidOperationException("Expected report data row to be accepted.");
    }

    private static EsrsReportPackageGenerateRequest CreateGenerateRequest(StoryIds ids) =>
        new(
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
