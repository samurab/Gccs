using Gccs.Application.Audit;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class CuiReadyApprovalChecklistTests
{
    private static readonly Guid TenantId = Guid.Parse("1a040100-0000-4000-8000-000000000001");
    private static readonly Guid ActorUserId = Guid.Parse("1a040100-0000-4000-8000-000000000002");

    [Fact]
    public async Task TC_1A_4_1_1_Required_checklist_items_block_approval()
    {
        await using var dbContext = CreateDbContext();
        SeedTenant(dbContext);
        var service = CreateService(dbContext);
        var checklist = await service.CreateAsync(TenantId, ActorUserId);

        var exception = await Assert.ThrowsAsync<CuiReadyApprovalChecklistValidationException>(
            () => service.ApproveAsync(TenantId, checklist.Id, new ReviewCuiReadyChecklistRequest("Ready."), ActorUserId));

        Assert.Contains("required items are incomplete", exception.Message);
    }

    [Fact]
    public async Task TC_1A_4_1_6_Antitrust_procurement_integrity_item_is_required()
    {
        await using var dbContext = CreateDbContext();
        SeedTenant(dbContext);
        var service = CreateService(dbContext);

        var checklist = await service.CreateAsync(TenantId, ActorUserId);

        var item = Assert.Single(checklist.Items, candidate => candidate.ItemKey == "antitrust-procurement-integrity");
        Assert.True(item.IsRequired);
        Assert.Equal("Antitrust and procurement integrity", item.Section);
        Assert.Contains("pricing", item.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_1A_4_1_2_Completed_item_metadata_required_and_stored()
    {
        await using var dbContext = CreateDbContext();
        SeedTenant(dbContext);
        var service = CreateService(dbContext);
        var checklist = await service.CreateAsync(TenantId, ActorUserId);
        var item = checklist.Items[0];

        await Assert.ThrowsAsync<CuiReadyApprovalChecklistValidationException>(() =>
            service.UpdateItemAsync(TenantId, checklist.Id, item.ItemKey, new UpdateCuiReadyChecklistItemRequest(
                CuiReadyChecklistItemStatus.Complete,
                Owner: "",
                EvidenceLink: null,
                ReviewerUserId: ActorUserId,
                ReviewedAt: new DateOnly(2026, 6, 18),
                Notes: "Reviewed."), ActorUserId));

        var updated = await service.UpdateItemAsync(TenantId, checklist.Id, item.ItemKey, new UpdateCuiReadyChecklistItemRequest(
            CuiReadyChecklistItemStatus.Complete,
            Owner: "Security",
            EvidenceLink: null,
            ReviewerUserId: ActorUserId,
            ReviewedAt: new DateOnly(2026, 6, 18),
            Notes: "Security review evidence retained."), ActorUserId);

        var completed = Assert.Single(updated!.Items, candidate => candidate.ItemKey == item.ItemKey);
        Assert.Equal("Security", completed.Owner);
        Assert.Equal(ActorUserId, completed.ReviewerUserId);
        Assert.Equal(new DateOnly(2026, 6, 18), completed.ReviewedAt);
        Assert.Equal("Security review evidence retained.", completed.Notes);
    }

    [Fact]
    public async Task TC_1A_4_1_3_Rejected_checklist_records_reason()
    {
        await using var dbContext = CreateDbContext();
        SeedTenant(dbContext);
        var service = CreateService(dbContext);
        var checklist = await service.CreateAsync(TenantId, ActorUserId);

        var rejected = await service.RejectAsync(TenantId, checklist.Id, new ReviewCuiReadyChecklistRequest("Backup restore evidence missing."), ActorUserId);

        Assert.NotNull(rejected);
        Assert.Equal(TenantId, rejected.TenantId);
        Assert.Equal(CuiReadyChecklistState.Rejected, rejected.State);
        Assert.Equal("Backup restore evidence missing.", rejected.RejectionReason);
        Assert.Equal(ActorUserId, rejected.ReviewedByUserId);
        Assert.NotNull(rejected.ReviewedAt);
    }

    [Fact]
    public async Task TC_1A_4_1_4_Approved_checklist_required_for_cui_ready_mode()
    {
        await using var dbContext = CreateDbContext();
        SeedTenant(dbContext);
        var auditWriter = new CapturingAuditEventWriter();
        var checklistService = CreateService(dbContext, auditWriter);
        var checklist = await checklistService.CreateAsync(TenantId, ActorUserId);
        checklist = await CompleteAllItemsAsync(checklistService, checklist);
        var approved = await checklistService.ApproveAsync(TenantId, checklist.Id, new ReviewCuiReadyChecklistRequest("Approved for CUI-ready mode."), ActorUserId);
        var tenantRepository = new CapturingTenantRepository(TenantDataPosture.NoCui);
        var tenantService = new TenantService(tenantRepository, auditWriter, checklistService);

        await Assert.ThrowsAsync<CuiReadyApprovalChecklistValidationException>(() =>
            tenantService.UpdateDataHandlingModeAsync(TenantId, new UpdateTenantDataHandlingModeRequest(TenantDataPosture.CuiReady, "Enable CUI-ready mode.", Guid.NewGuid().ToString()), ActorUserId));

        var tenant = await tenantService.UpdateDataHandlingModeAsync(
            TenantId,
            new UpdateTenantDataHandlingModeRequest(TenantDataPosture.CuiReady, "Enable CUI-ready mode.", approved!.Id.ToString()),
            ActorUserId);

        Assert.NotNull(tenant);
        Assert.Equal(TenantDataPosture.CuiReady, tenant.DataHandlingMode);
        Assert.Equal(approved.Id.ToString(), tenantRepository.History.Single().ApprovalRecordReference);
    }

    [Fact]
    public async Task TC_1A_4_1_5_Checklist_changes_are_audited()
    {
        await using var dbContext = CreateDbContext();
        SeedTenant(dbContext);
        var auditWriter = new CapturingAuditEventWriter();
        var service = CreateService(dbContext, auditWriter);

        var rejected = await service.CreateAsync(TenantId, ActorUserId);
        await service.UpdateItemAsync(TenantId, rejected.Id, rejected.Items[0].ItemKey, CompletedRequest(), ActorUserId);
        await service.RejectAsync(TenantId, rejected.Id, new ReviewCuiReadyChecklistRequest("Need support escalation."), ActorUserId);
        var approved = await service.CreateAsync(TenantId, ActorUserId);
        approved = await CompleteAllItemsAsync(service, approved);
        await service.ApproveAsync(TenantId, approved.Id, new ReviewCuiReadyChecklistRequest("Approved."), ActorUserId);
        await service.SupersedeAsync(TenantId, approved.Id, new ReviewCuiReadyChecklistRequest("Replaced by later checklist."), ActorUserId);

        Assert.Contains(auditWriter.Events, audit => audit.Metadata["lifecycleAction"] == "created");
        Assert.Contains(auditWriter.Events, audit => audit.Metadata["lifecycleAction"] == "updated");
        Assert.Contains(auditWriter.Events, audit => audit.Metadata["lifecycleAction"] == "approved");
        Assert.Contains(auditWriter.Events, audit => audit.Metadata["lifecycleAction"] == "rejected");
        Assert.Contains(auditWriter.Events, audit => audit.Metadata["lifecycleAction"] == "superseded");
    }

    [Fact]
    public async Task TC_1A_4_2_2_Invalid_checklist_states_block_cui_ready()
    {
        await using var dbContext = CreateDbContext();
        SeedTenant(dbContext);
        var auditWriter = new CapturingAuditEventWriter();
        var service = CreateService(dbContext, auditWriter);
        var tenantService = new TenantService(new CapturingTenantRepository(TenantDataPosture.NoCui), auditWriter, service);
        var draft = await service.CreateAsync(TenantId, ActorUserId);
        var rejected = await service.CreateAsync(TenantId, ActorUserId);
        await service.RejectAsync(TenantId, rejected.Id, new ReviewCuiReadyChecklistRequest("Rejected."), ActorUserId);
        var superseded = await service.CreateAsync(TenantId, ActorUserId);
        superseded = await CompleteAllItemsAsync(service, superseded);
        await service.ApproveAsync(TenantId, superseded.Id, new ReviewCuiReadyChecklistRequest("Approved."), ActorUserId);
        await service.SupersedeAsync(TenantId, superseded.Id, new ReviewCuiReadyChecklistRequest("Superseded."), ActorUserId);
        var expired = await service.CreateAsync(TenantId, ActorUserId);
        expired = await CompleteAllItemsAsync(service, expired);
        await service.ApproveAsync(TenantId, expired.Id, new ReviewCuiReadyChecklistRequest("Approved but old."), ActorUserId);
        var expiredEntity = await dbContext.CuiReadyApprovalChecklists.SingleAsync(checklist => checklist.Id == expired.Id);
        expiredEntity.ReviewedAt = DateTimeOffset.UtcNow.AddYears(-2);
        await dbContext.SaveChangesAsync();

        foreach (var checklistId in new[] { draft.Id, rejected.Id, superseded.Id, expired.Id })
        {
            await Assert.ThrowsAsync<CuiReadyApprovalChecklistValidationException>(() =>
                tenantService.UpdateDataHandlingModeAsync(
                    TenantId,
                    new UpdateTenantDataHandlingModeRequest(TenantDataPosture.CuiReady, "Enable CUI-ready mode.", checklistId.ToString()),
                    ActorUserId));
        }
    }

    [Fact]
    public async Task TC_1A_4_2_3_Final_approval_metadata_persisted()
    {
        await using var dbContext = CreateDbContext();
        SeedTenant(dbContext);
        var service = CreateService(dbContext);
        var checklist = await service.CreateAsync(TenantId, ActorUserId);
        checklist = await CompleteAllItemsAsync(service, checklist);

        var approved = await service.ApproveAsync(TenantId, checklist.Id, new ReviewCuiReadyChecklistRequest("Final approval notes."), ActorUserId);

        Assert.NotNull(approved);
        Assert.Equal(CuiReadyChecklistState.Approved, approved.State);
        Assert.Equal(ActorUserId, approved.ReviewedByUserId);
        Assert.NotNull(approved.ReviewedAt);
        Assert.Equal(1, approved.Version);
        Assert.Equal("Final approval notes.", approved.ReviewNotes);
    }

    [Fact]
    public async Task TC_1A_4_2_5_Failed_attempts_are_audited()
    {
        await using var dbContext = CreateDbContext();
        SeedTenant(dbContext);
        var auditWriter = new CapturingAuditEventWriter();
        var service = CreateService(dbContext, auditWriter);
        var checklist = await service.CreateAsync(TenantId, ActorUserId);
        var tenantService = new TenantService(new CapturingTenantRepository(TenantDataPosture.NoCui), auditWriter, service);

        await Assert.ThrowsAsync<CuiReadyApprovalChecklistValidationException>(() =>
            service.ApproveAsync(TenantId, checklist.Id, new ReviewCuiReadyChecklistRequest("Premature approval."), ActorUserId));
        await Assert.ThrowsAsync<CuiReadyApprovalChecklistValidationException>(() =>
            tenantService.UpdateDataHandlingModeAsync(
                TenantId,
                new UpdateTenantDataHandlingModeRequest(TenantDataPosture.CuiReady, "Enable CUI-ready mode.", checklist.Id.ToString()),
                ActorUserId));

        Assert.Contains(auditWriter.Events, audit =>
            audit.EntityType == "CuiReadyApprovalChecklist" &&
            audit.Metadata.TryGetValue("result", out var result) &&
            result == "failed");
        Assert.Contains(auditWriter.Events, audit =>
            audit.EntityType == "TenantDataHandlingMode" &&
            audit.Metadata.TryGetValue("result", out var result) &&
            result == "failed");
    }

    private static async Task<CuiReadyApprovalChecklistDto> CompleteAllItemsAsync(
        CuiReadyApprovalChecklistService service,
        CuiReadyApprovalChecklistDto checklist)
    {
        foreach (var item in checklist.Items)
        {
            checklist = (await service.UpdateItemAsync(TenantId, checklist.Id, item.ItemKey, CompletedRequest(), ActorUserId))!;
        }

        return checklist;
    }

    private static UpdateCuiReadyChecklistItemRequest CompletedRequest() =>
        new(
            CuiReadyChecklistItemStatus.Complete,
            Owner: "Security",
            EvidenceLink: "https://example.invalid/evidence/cui-ready",
            ReviewerUserId: ActorUserId,
            ReviewedAt: new DateOnly(2026, 6, 18),
            Notes: null);

    private static CuiReadyApprovalChecklistService CreateService(GccsDbContext dbContext, IAuditEventWriter? auditWriter = null) =>
        new(new EfCuiReadyApprovalChecklistRepository(dbContext), auditWriter ?? new CapturingAuditEventWriter());

    private static GccsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GccsDbContext>()
            .UseInMemoryDatabase($"cui-ready-checklist-{Guid.NewGuid():N}")
            .Options;

        return new GccsDbContext(options);
    }

    private static void SeedTenant(GccsDbContext dbContext)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = TenantId,
            Name = "CUI Ready Tenant",
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

    private sealed class CapturingTenantRepository(TenantDataPosture initialMode) : ITenantRepository
    {
        private TenantDataPosture _mode = initialMode;
        public List<TenantDataHandlingModeHistoryDto> History { get; } = [];

        public Task<Tenant?> FindInCurrentTenantScopeAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Tenant?>(tenantId == TenantId ? ToTenant() : null);

        public Task<TenantDataPosture?> FindCurrentTenantDataHandlingModeAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<TenantDataPosture?>(_mode);

        public Task<IReadOnlyList<TenantDataHandlingModeHistoryDto>> ListDataHandlingModeHistoryInCurrentTenantScopeAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TenantDataHandlingModeHistoryDto>>(History);

        public Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task AddDataHandlingModeHistoryAsync(Guid tenantId, TenantDataPosture? previousMode, TenantDataPosture newMode, Guid actorUserId, string reason, string? approvalRecordReference, CancellationToken cancellationToken = default)
        {
            History.Add(new TenantDataHandlingModeHistoryDto(Guid.NewGuid(), tenantId, previousMode, newMode, actorUserId, DateTimeOffset.UtcNow, reason, approvalRecordReference));
            return Task.CompletedTask;
        }

        public Task<Tenant?> UpdateStatusInCurrentTenantScopeAsync(Guid tenantId, TenantStatus status, CancellationToken cancellationToken = default) =>
            Task.FromResult<Tenant?>(ToTenant());

        public Task<Tenant?> UpdateDataHandlingModeInCurrentTenantScopeAsync(Guid tenantId, TenantDataPosture dataHandlingMode, Guid actorUserId, CancellationToken cancellationToken = default)
        {
            _mode = dataHandlingMode;
            return Task.FromResult<Tenant?>(ToTenant());
        }

        private Tenant ToTenant() =>
            new(TenantId, "CUI Ready Tenant", TenantStatus.Active, _mode, null, new EntityAudit(DateTimeOffset.UtcNow, ActorUserId, null, null));
    }
}
