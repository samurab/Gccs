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

public sealed class CuiSupportEscalationTests
{
    private static readonly Guid TenantId = Guid.Parse("1a070100-0000-4000-8000-000000000001");
    private static readonly Guid OtherTenantId = Guid.Parse("1a070100-0000-4000-8000-000000000003");
    private static readonly Guid ActorUserId = Guid.Parse("1a070100-0000-4000-8000-000000000002");

    [Fact]
    public async Task TC_1A_7_1_1_Authorized_users_create_escalations_from_cui_relevant_workflows()
    {
        await using var dbContext = CreateDbContext();
        SeedTenants(dbContext);
        var service = CreateService(dbContext);
        var workflows = new[] { "UploadRejection", "EvidenceDetail", "NoteDetail", "ReportDetail", "ExtractionJobDetail", "SupportPage" };

        foreach (var workflow in workflows)
        {
            await service.CreateAsync(TenantId, Request(workflow, CuiSupportEscalationCategory.SuspectedCui), ActorUserId);
        }

        var escalations = await service.ListAsync(TenantId);
        Assert.Equal(workflows.Length, escalations.Count);
        Assert.All(workflows, workflow => Assert.Contains(escalations, escalation => escalation.SourceWorkflow == workflow));
    }

    [Fact]
    public async Task TC_1A_7_1_2_Escalations_are_tenant_scoped()
    {
        await using var dbContext = CreateDbContext();
        SeedTenants(dbContext);
        var service = CreateService(dbContext);

        await service.CreateAsync(TenantId, Request("SupportPage", CuiSupportEscalationCategory.SuspectedCui), ActorUserId);
        await service.CreateAsync(OtherTenantId, Request("SupportPage", CuiSupportEscalationCategory.ProhibitedData), ActorUserId);

        var tenantEscalations = await service.ListAsync(TenantId);

        var escalation = Assert.Single(tenantEscalations);
        Assert.Equal(TenantId, escalation.TenantId);
        Assert.Equal(CuiSupportEscalationCategory.SuspectedCui, escalation.Category);
    }

    [Fact]
    public async Task TC_1A_7_1_3_Prohibited_data_escalation_blocks_affected_content()
    {
        await using var dbContext = CreateDbContext();
        SeedTenants(dbContext);
        var service = CreateService(dbContext);

        var escalation = await service.CreateAsync(TenantId, Request("UploadRejection", CuiSupportEscalationCategory.ProhibitedData), ActorUserId);

        Assert.True(escalation.IsAffectedContentBlocked);
        Assert.Equal("EvidenceItem", escalation.AffectedEntityType);
        Assert.Equal("affected-123", escalation.AffectedEntityId);
    }

    [Fact]
    public async Task TC_1A_7_1_4_Support_fields_can_be_assigned()
    {
        await using var dbContext = CreateDbContext();
        SeedTenants(dbContext);
        var service = CreateService(dbContext);
        var escalation = await service.CreateAsync(TenantId, Request("SupportPage", CuiSupportEscalationCategory.SuspectedCui), ActorUserId);

        var updated = await service.UpdateSupportFieldsAsync(
            TenantId,
            escalation.Id,
            new UpdateCuiSupportEscalationRequest("Security Support", CuiSupportEscalationSeverity.Critical, CuiSupportEscalationStatus.Triage),
            ActorUserId);

        Assert.NotNull(updated);
        Assert.Equal("Security Support", updated.Owner);
        Assert.Equal(CuiSupportEscalationSeverity.Critical, updated.Severity);
        Assert.Equal(CuiSupportEscalationStatus.Triage, updated.Status);
    }

    [Fact]
    public async Task TC_1A_7_1_5_Escalation_create_and_update_are_audited()
    {
        await using var dbContext = CreateDbContext();
        SeedTenants(dbContext);
        var auditWriter = new CapturingAuditEventWriter();
        var service = CreateService(dbContext, auditWriter);

        var escalation = await service.CreateAsync(TenantId, Request("EvidenceDetail", CuiSupportEscalationCategory.ProhibitedData), ActorUserId);
        await service.UpdateSupportFieldsAsync(
            TenantId,
            escalation.Id,
            new UpdateCuiSupportEscalationRequest("Support Lead", CuiSupportEscalationSeverity.High, CuiSupportEscalationStatus.Contained),
            ActorUserId);

        Assert.Contains(auditWriter.Events, audit => audit.Action == AuditAction.Created && audit.Metadata["lifecycleAction"] == "created");
        Assert.Contains(auditWriter.Events, audit => audit.Action == AuditAction.Updated && audit.Metadata["lifecycleAction"] == "updated");
        Assert.All(auditWriter.Events, audit => Assert.Equal(TenantId, audit.TenantId));
    }

    private static CreateCuiSupportEscalationRequest Request(string workflow, CuiSupportEscalationCategory category) =>
        new(workflow, "EvidenceItem", "affected-123", category, CuiSupportEscalationSeverity.High, "Potential CUI or prohibited data needs review.");

    private static CuiSupportEscalationService CreateService(GccsDbContext dbContext, IAuditEventWriter? auditWriter = null) =>
        new(new EfCuiSupportEscalationRepository(dbContext), auditWriter ?? new CapturingAuditEventWriter());

    private static GccsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GccsDbContext>()
            .UseInMemoryDatabase($"cui-support-escalations-{Guid.NewGuid():N}")
            .Options;

        return new GccsDbContext(options);
    }

    private static void SeedTenants(GccsDbContext dbContext)
    {
        dbContext.Tenants.AddRange(
            CreateTenant(TenantId, "Support Escalation Tenant"),
            CreateTenant(OtherTenantId, "Other Support Escalation Tenant"));
        dbContext.SaveChanges();
    }

    private static TenantEntity CreateTenant(Guid tenantId, string name) =>
        new()
        {
            Id = tenantId,
            Name = name,
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = ActorUserId
        };

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
