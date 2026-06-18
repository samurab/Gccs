using Gccs.Application.Audit;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class DataHandlingNoticeAcknowledgementTests
{
    private static readonly Guid TenantId = Guid.Parse("1a060200-0000-4000-8000-000000000001");
    private static readonly Guid UserId = Guid.Parse("1a060200-0000-4000-8000-000000000002");

    [Fact]
    public async Task TC_1A_6_2_1_Missing_acknowledgement_blocks_cui_relevant_actions()
    {
        await using var dbContext = CreateDbContext();
        SeedTenant(dbContext);
        var service = CreateService(dbContext);
        var notice = Notice(TenantDataPosture.CuiReady, "2026.06.phase1a");
        var workflows = new[] { "EvidenceUpload", "ClassifiedNote", "ReportGeneration", "ExtractionJob" };

        foreach (var workflow in workflows)
        {
            await Assert.ThrowsAsync<DataHandlingNoticeAcknowledgementRequiredException>(() =>
                service.EnsureAcknowledgedAsync(TenantId, UserId, notice, workflow));
        }
    }

    [Fact]
    public async Task TC_1A_6_2_2_Acknowledgement_metadata_is_persisted()
    {
        await using var dbContext = CreateDbContext();
        SeedTenant(dbContext);
        var service = CreateService(dbContext);
        var notice = Notice(TenantDataPosture.NoCui, "2026.06.phase1a");

        var acknowledgement = await service.AcknowledgeAsync(TenantId, UserId, notice, Request(notice, "EvidenceUpload"));

        Assert.Equal(TenantId, acknowledgement.TenantId);
        Assert.Equal(UserId, acknowledgement.UserId);
        Assert.Equal(TenantDataPosture.NoCui, acknowledgement.Mode);
        Assert.Equal("EvidenceUpload", acknowledgement.WorkflowContext);
        Assert.Equal(notice.Version, acknowledgement.NoticeVersion);
        Assert.NotEqual(default, acknowledgement.AcknowledgedAt);
    }

    [Fact]
    public async Task TC_1A_6_2_3_Updated_notice_requires_renewed_acknowledgement()
    {
        await using var dbContext = CreateDbContext();
        SeedTenant(dbContext);
        var service = CreateService(dbContext);
        var original = Notice(TenantDataPosture.CuiReady, "2026.06.phase1a");
        var updated = Notice(TenantDataPosture.CuiReady, "2026.07.phase1a");

        await service.AcknowledgeAsync(TenantId, UserId, original, Request(original, "ExtractionJob"));
        var history = await service.ListAsync(TenantId, UserId, updated);

        Assert.Equal(DataHandlingNoticeAcknowledgementStatus.Outdated, Assert.Single(history).Status);
        await Assert.ThrowsAsync<DataHandlingNoticeAcknowledgementRequiredException>(() =>
            service.EnsureAcknowledgedAsync(TenantId, UserId, updated, "ExtractionJob"));
    }

    [Fact]
    public void TC_1A_6_2_4_Notice_copy_matches_tenant_mode()
    {
        var demo = Notice(TenantDataPosture.DemoSandbox, "2026.06.phase1a");
        var noCui = Notice(TenantDataPosture.NoCui, "2026.06.phase1a");
        var cuiReady = Notice(TenantDataPosture.CuiReady, "2026.06.phase1a");

        Assert.Contains("synthetic", demo.Body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Real customer CUI upload is prohibited", noCui.Body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("approved tenant workflows", cuiReady.Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_1A_6_2_5_Acknowledgement_and_renewal_are_audit_logged()
    {
        await using var dbContext = CreateDbContext();
        SeedTenant(dbContext);
        var auditWriter = new CapturingAuditEventWriter();
        var service = CreateService(dbContext, auditWriter);
        var original = Notice(TenantDataPosture.CuiReady, "2026.06.phase1a");
        var updated = Notice(TenantDataPosture.CuiReady, "2026.07.phase1a");

        await service.AcknowledgeAsync(TenantId, UserId, original, Request(original, "Support"));
        await service.AcknowledgeAsync(TenantId, UserId, updated, Request(updated, "Support"));

        Assert.Contains(auditWriter.Events, audit => audit.Metadata["noticeVersion"] == original.Version && audit.Metadata["workflowContext"] == "Support");
        Assert.Contains(auditWriter.Events, audit => audit.Metadata["noticeVersion"] == updated.Version && audit.Metadata["workflowContext"] == "Support");
    }

    private static DataHandlingNoticeAcknowledgementService CreateService(
        GccsDbContext dbContext,
        IAuditEventWriter? auditWriter = null) =>
        new(new EfDataHandlingNoticeAcknowledgementRepository(dbContext), auditWriter ?? new CapturingAuditEventWriter());

    private static AcknowledgeDataHandlingNoticeRequest Request(DataHandlingNoticeDto notice, string workflowContext) =>
        new(notice.Mode, workflowContext, notice.NoticeId, notice.Version, true);

    private static DataHandlingNoticeDto Notice(TenantDataPosture mode, string version) =>
        new(
            $"{mode.ToString().ToLowerInvariant()}-general",
            version,
            mode,
            ["Onboarding", "EvidenceUpload", "ClassifiedNote", "ReportGeneration", "ExtractionJob", "Support"],
            $"{mode} Notice",
            mode switch
            {
                TenantDataPosture.DemoSandbox => "Synthetic demo data only.",
                TenantDataPosture.NoCui => "Real customer CUI upload is prohibited.",
                _ => "CUI handling is limited to approved tenant workflows and customer responsibilities."
            },
            "Published",
            "GCCS Product Owner",
            "GCCS Security Owner",
            new DateOnly(2026, 6, 18),
            new DateOnly(2026, 6, 18),
            "Phase 1A notices");

    private static GccsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GccsDbContext>()
            .UseInMemoryDatabase($"data-handling-notice-acknowledgements-{Guid.NewGuid():N}")
            .Options;

        return new GccsDbContext(options);
    }

    private static void SeedTenant(GccsDbContext dbContext)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = TenantId,
            Name = "Notice Acknowledgement Tenant",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = UserId
        });
        dbContext.SaveChanges();
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
            Events.Add(new CapturedAuditEvent(tenantId, actorUserId, action, entityType, entityId, metadata ?? new Dictionary<string, string>()));
            return Task.CompletedTask;
        }
    }

    private sealed record CapturedAuditEvent(
        Guid TenantId,
        Guid ActorUserId,
        AuditAction Action,
        string EntityType,
        string EntityId,
        IReadOnlyDictionary<string, string> Metadata);
}
