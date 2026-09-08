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

public sealed class CuiSupportEscalationWorkflowTests
{
    private static readonly Guid TenantId = Guid.Parse("1a070200-0000-4000-8000-000000000001");
    private static readonly Guid ActorUserId = Guid.Parse("1a070200-0000-4000-8000-000000000002");
    private static readonly Guid EvidenceId = Guid.Parse("1a070200-0000-4000-8000-000000000004");

    [Fact]
    public async Task TC_1A_7_2_1_Status_changes_require_note()
    {
        await using var dbContext = CreateDbContext();
        SeedTenant(dbContext);
        var service = CreateService(dbContext);
        var escalation = await service.CreateAsync(TenantId, CreateRequest(), ActorUserId);

        await Assert.ThrowsAsync<CuiSupportEscalationValidationException>(() =>
            service.ChangeStatusAsync(TenantId, escalation.Id, new ChangeCuiSupportEscalationStatusRequest(CuiSupportEscalationStatus.Triage, ""), ActorUserId));

        var updated = await service.ChangeStatusAsync(
            TenantId,
            escalation.Id,
            new ChangeCuiSupportEscalationStatusRequest(CuiSupportEscalationStatus.Triage, "Triage started."),
            ActorUserId);

        Assert.Equal(CuiSupportEscalationStatus.Triage, updated!.Status);
        Assert.Equal("Triage started.", updated.StatusNote);
        Assert.Equal(ActorUserId, updated.StatusChangedByUserId);
        Assert.NotNull(updated.StatusChangedAt);
    }

    [Fact]
    public async Task TC_1A_7_2_2_Containment_blocks_affected_content_until_resolution()
    {
        await using var dbContext = CreateDbContext();
        SeedTenant(dbContext);
        var service = CreateService(dbContext);
        var escalation = await service.CreateAsync(TenantId, CreateRequest(), ActorUserId);

        Assert.True(escalation.IsAffectedContentBlocked);
        foreach (var status in new[] { CuiSupportEscalationStatus.Submitted, CuiSupportEscalationStatus.Triage, CuiSupportEscalationStatus.Contained })
        {
            escalation = (await service.ChangeStatusAsync(TenantId, escalation.Id, new ChangeCuiSupportEscalationStatusRequest(status, $"Move to {status}."), ActorUserId))!;
            Assert.True(escalation.IsAffectedContentBlocked);
        }

        await Assert.ThrowsAsync<CuiSupportEscalationValidationException>(() => service.ResolveAsync(TenantId, escalation.Id,
            new ResolveCuiSupportEscalationRequest(CuiSupportEscalationResolutionType.ContentRemoved, "File removal alone is insufficient."), ActorUserId));
        ReviewEvidence(dbContext);
        var resolved = await service.ResolveAsync(
            TenantId,
            escalation.Id,
            new ResolveCuiSupportEscalationRequest(CuiSupportEscalationResolutionType.FalsePositive, "Reviewed synthetic test content."),
            ActorUserId);

        Assert.False(resolved!.IsAffectedContentBlocked);
    }

    [Fact]
    public async Task TC_1A_7_2_3_Resolution_records_are_complete()
    {
        await using var dbContext = CreateDbContext();
        SeedTenant(dbContext);
        var service = CreateService(dbContext);
        var escalation = await service.CreateAsync(TenantId, CreateRequest(), ActorUserId);

        await Assert.ThrowsAsync<CuiSupportEscalationValidationException>(() => service.ResolveAsync(TenantId, escalation.Id,
            new ResolveCuiSupportEscalationRequest(CuiSupportEscalationResolutionType.ReferredToCustomer, "Referral is not release."), ActorUserId));
        ReviewEvidence(dbContext);
        var resolved = await service.ResolveAsync(
            TenantId,
            escalation.Id,
            new ResolveCuiSupportEscalationRequest(CuiSupportEscalationResolutionType.FalsePositive, "Reviewed synthetic content."),
            ActorUserId);

        var resolution = Assert.Single(resolved!.Resolutions);
        Assert.Equal(CuiSupportEscalationResolutionType.FalsePositive, resolution.ResolutionType);
        Assert.Equal(ActorUserId, resolution.ResolvedByUserId);
        Assert.NotEqual(default, resolution.ResolvedAt);
        Assert.Equal("Reviewed synthetic content.", resolution.Summary);
    }

    [Fact]
    public async Task TC_1A_7_2_4_Reopen_preserves_resolution_history()
    {
        await using var dbContext = CreateDbContext();
        SeedTenant(dbContext);
        var service = CreateService(dbContext);
        var escalation = await service.CreateAsync(TenantId, CreateRequest(), ActorUserId);
        ReviewEvidence(dbContext);
        var resolved = await service.ResolveAsync(
            TenantId,
            escalation.Id,
            new ResolveCuiSupportEscalationRequest(CuiSupportEscalationResolutionType.FalsePositive, "Initial false positive."),
            ActorUserId);

        var reopened = await service.ChangeStatusAsync(
            TenantId,
            resolved!.Id,
            new ChangeCuiSupportEscalationStatusRequest(CuiSupportEscalationStatus.Triage, "Reopened after customer correction."),
            ActorUserId);

        Assert.Equal(CuiSupportEscalationStatus.Triage, reopened!.Status);
        Assert.Single(reopened.Resolutions);
    }

    [Fact]
    public async Task TC_1A_7_2_5_Escalation_workflow_events_are_audited()
    {
        await using var dbContext = CreateDbContext();
        SeedTenant(dbContext);
        var auditWriter = new CapturingAuditEventWriter();
        var service = CreateService(dbContext, auditWriter);
        var escalation = await service.CreateAsync(TenantId, CreateRequest(), ActorUserId);

        await service.ChangeStatusAsync(TenantId, escalation.Id, new ChangeCuiSupportEscalationStatusRequest(CuiSupportEscalationStatus.Triage, "Triage."), ActorUserId);
        await service.ChangeStatusAsync(TenantId, escalation.Id, new ChangeCuiSupportEscalationStatusRequest(CuiSupportEscalationStatus.Contained, "Contained."), ActorUserId);
        ReviewEvidence(dbContext);
        await service.ResolveAsync(TenantId, escalation.Id, new ResolveCuiSupportEscalationRequest(CuiSupportEscalationResolutionType.FalsePositive, "Resolved."), ActorUserId);

        Assert.Contains(auditWriter.Events, audit => audit.Metadata["lifecycleAction"] == "created");
        Assert.Contains(auditWriter.Events, audit => audit.Metadata["lifecycleAction"] == "status_changed" && audit.Metadata["status"] == "Triage");
        Assert.Contains(auditWriter.Events, audit => audit.Metadata["lifecycleAction"] == "status_changed" && audit.Metadata["status"] == "Contained");
        Assert.Contains(auditWriter.Events, audit => audit.Metadata["lifecycleAction"] == "resolved" && audit.Metadata["status"] == "Resolved");
    }

    private static CreateCuiSupportEscalationRequest CreateRequest() =>
        new("UploadRejection", "EvidenceItem", EvidenceId.ToString(), CuiSupportEscalationCategory.ProhibitedData, CuiSupportEscalationSeverity.High, "Prohibited data suspected.");

    private static void ReviewEvidence(GccsDbContext db)
    {
        var item = db.EvidenceItems.Single(e => e.Id == EvidenceId);
        item.Classification = Gccs.Domain.Common.ContentClassification.Unclassified;
        item.ClassificationSource = Gccs.Domain.Common.ContentClassificationSource.AdminReviewed;
        item.ClassificationReviewedAt = DateTimeOffset.UtcNow;
        item.ClassificationReviewedByUserId = ActorUserId;
        db.SaveChanges();
    }

    private static CuiSupportEscalationService CreateService(GccsDbContext dbContext, IAuditEventWriter? auditWriter = null) =>
        new(new EfCuiSupportEscalationRepository(dbContext), auditWriter ?? new CapturingAuditEventWriter(), new TestApplicationTransaction());

    private static GccsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GccsDbContext>()
            .UseInMemoryDatabase($"cui-support-escalation-workflow-{Guid.NewGuid():N}")
            .Options;

        return new GccsDbContext(options);
    }

    private static void SeedTenant(GccsDbContext dbContext)
    {
        dbContext.EvidenceItems.Add(new EvidenceItemEntity { Id = EvidenceId, TenantId = TenantId, Name = "Synthetic containment fixture" });
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = TenantId,
            Name = "Escalation Workflow Tenant",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = ActorUserId
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
