using Gccs.Application.Audit;
using Gccs.Application.Compliance;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Infrastructure.Compliance;
using Gccs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class SbaSizeStandardTests
{
    private static readonly Guid ActorUserId = Guid.Parse("23123123-1231-2312-3123-1231231231a1");

    [Fact]
    public async Task TC_23_1_1_TC_23_1_2_and_TC_23_1_4_Approved_records_have_metadata_and_drafts_excluded_from_helper_results()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, new CapturingAuditEventWriter());
        var imported = await service.ImportAsync(
            [
                Record("541511", ReviewState.Approved),
                Record("541330", ReviewState.Draft),
                Record("236220", ReviewState.Retired)
            ],
            ActorUserId);

        var approved = await service.ListApprovedAsync();
        var reviewerVisible = await service.ListForReviewAsync();

        var approvedRecord = Assert.Single(approved);
        Assert.Equal("541511", approvedRecord.NaicsCode);
        Assert.Equal("Receipts", approvedRecord.Metric);
        Assert.Equal(34_000_000m, approvedRecord.Threshold);
        Assert.Equal("https://www.sba.gov/document/support-table-size-standards", approvedRecord.SourceUrl);
        Assert.Equal(new DateOnly(2026, 1, 1), approvedRecord.EffectiveAt);
        Assert.Equal(new DateOnly(2026, 6, 17), approvedRecord.LastReviewedAt);
        Assert.Equal(ReviewState.Approved, approvedRecord.Status);
        Assert.DoesNotContain(approved, record => record.Status == ReviewState.Draft);
        Assert.Contains(reviewerVisible, record => record.Status == ReviewState.Retired);
        Assert.Equal(3, imported.Count);
    }

    [Fact]
    public async Task TC_23_1_3_Import_rejects_records_missing_source_metadata()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, new CapturingAuditEventWriter());

        var exception = await Assert.ThrowsAsync<SbaSizeStandardImportValidationException>(() =>
            service.ImportAsync(
                [Record("541511", ReviewState.Draft) with { SourceUrl = "", EffectiveAt = null, LastReviewedAt = null }],
                ActorUserId));

        Assert.Contains("records[0].sourceUrl", exception.Errors.Keys);
        Assert.Contains("records[0].effectiveAt", exception.Errors.Keys);
        Assert.Contains("records[0].lastReviewedAt", exception.Errors.Keys);
        Assert.Empty(await dbContext.SbaSizeStandards.ToArrayAsync());
    }

    [Fact]
    public async Task TC_23_1_5_Import_and_approval_actions_are_audit_logged()
    {
        await using var dbContext = CreateDbContext();
        var auditWriter = new CapturingAuditEventWriter();
        var service = CreateService(dbContext, auditWriter);
        var imported = await service.ImportAsync([Record("541511", ReviewState.Draft)], ActorUserId);

        var approved = await service.ChangeStatusAsync(imported.Single().Id, ReviewState.Approved, ActorUserId);

        Assert.NotNull(approved);
        Assert.Equal(ReviewState.Approved, approved.Status);
        Assert.Equal(2, auditWriter.Events.Count);
        Assert.Contains(auditWriter.Events, audit => audit.Action == AuditAction.Created && audit.EntityId == "import");
        Assert.Contains(auditWriter.Events, audit => audit.Action == AuditAction.Updated && audit.Metadata["status"] == ReviewState.Approved.ToString());
    }

    private static SbaSizeStandardService CreateService(GccsDbContext dbContext, IAuditEventWriter auditWriter) =>
        new(new EfSbaSizeStandardRepository(dbContext), auditWriter);

    private static ImportSbaSizeStandardRequest Record(string naicsCode, ReviewState status) =>
        new(
            naicsCode,
            "Receipts",
            34_000_000m,
            "USD",
            "https://www.sba.gov/document/support-table-size-standards",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 6, 17),
            status);

    private static GccsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GccsDbContext>()
            .UseInMemoryDatabase($"sba-size-standards-{Guid.NewGuid():N}")
            .Options;
        return new GccsDbContext(options);
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
}
