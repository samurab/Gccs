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

public sealed class SharedResponsibilityMatrixAcknowledgementTests
{
    private static readonly Guid TenantId = Guid.Parse("1a050200-0000-4000-8000-000000000001");
    private static readonly Guid ActorUserId = Guid.Parse("1a050200-0000-4000-8000-000000000002");

    [Fact]
    public async Task TC_1A_5_2_1_Tenant_admin_acknowledges_current_matrix()
    {
        await using var dbContext = CreateDbContext();
        SeedTenant(dbContext);
        var service = CreateService(dbContext);
        var matrix = Matrix("2026.06.phase1a");

        var acknowledgement = await service.AcknowledgeAsync(TenantId, matrix, Acknowledge(matrix), ActorUserId);
        var history = await service.ListAsync(TenantId, matrix);

        Assert.Equal(TenantId, acknowledgement.TenantId);
        Assert.Equal(matrix.Version, acknowledgement.MatrixVersion);
        Assert.Equal(SharedResponsibilityMatrixAcknowledgementStatus.Current, acknowledgement.Status);
        Assert.Single(history);
        Assert.Equal(acknowledgement.Id, history[0].Id);
    }

    [Fact]
    public async Task TC_1A_5_2_2_Missing_acknowledgement_blocks_cui_ready_approval_gate()
    {
        await using var dbContext = CreateDbContext();
        SeedTenant(dbContext);
        var auditWriter = new CapturingAuditEventWriter();
        var service = CreateService(dbContext, auditWriter);
        var matrix = Matrix("2026.06.phase1a");

        var exception = await Assert.ThrowsAsync<SharedResponsibilityMatrixAcknowledgementException>(() =>
            service.EnsureCurrentAcknowledgedAsync(TenantId, matrix, ActorUserId));

        Assert.Contains("required", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(auditWriter.Events, audit =>
            audit.Action == AuditAction.Rejected &&
            audit.EntityType == "SharedResponsibilityMatrixAcknowledgement" &&
            audit.Metadata["matrixVersion"] == matrix.Version &&
            audit.Metadata["result"] == "failed");
    }

    [Fact]
    public async Task TC_1A_5_2_3_Acknowledgement_history_records_version_user_tenant_timestamp_and_status()
    {
        await using var dbContext = CreateDbContext();
        SeedTenant(dbContext);
        var service = CreateService(dbContext);
        var matrix = Matrix("2026.06.phase1a");

        await service.AcknowledgeAsync(TenantId, matrix, Acknowledge(matrix), ActorUserId);
        var acknowledgement = Assert.Single(await service.ListAsync(TenantId, matrix));

        Assert.Equal(TenantId, acknowledgement.TenantId);
        Assert.Equal(ActorUserId, acknowledgement.AcknowledgedByUserId);
        Assert.Equal(matrix.Version, acknowledgement.MatrixVersion);
        Assert.NotEqual(default, acknowledgement.AcknowledgedAt);
        Assert.Equal(SharedResponsibilityMatrixAcknowledgementStatus.Current, acknowledgement.Status);
    }

    [Fact]
    public async Task TC_1A_5_2_4_New_matrix_version_marks_prior_acknowledgement_outdated()
    {
        await using var dbContext = CreateDbContext();
        SeedTenant(dbContext);
        var service = CreateService(dbContext);
        var originalMatrix = Matrix("2026.06.phase1a");
        var newMatrix = Matrix("2026.07.phase1a");

        await service.AcknowledgeAsync(TenantId, originalMatrix, Acknowledge(originalMatrix), ActorUserId);
        var history = await service.ListAsync(TenantId, newMatrix);

        var prior = Assert.Single(history);
        Assert.Equal(SharedResponsibilityMatrixAcknowledgementStatus.Outdated, prior.Status);
        await Assert.ThrowsAsync<SharedResponsibilityMatrixAcknowledgementException>(() =>
            service.EnsureCurrentAcknowledgedAsync(TenantId, newMatrix, ActorUserId));
    }

    [Fact]
    public async Task TC_1A_5_2_5_Matrix_acknowledgement_is_audited()
    {
        await using var dbContext = CreateDbContext();
        SeedTenant(dbContext);
        var auditWriter = new CapturingAuditEventWriter();
        var service = CreateService(dbContext, auditWriter);
        var matrix = Matrix("2026.06.phase1a");

        var acknowledgement = await service.AcknowledgeAsync(TenantId, matrix, Acknowledge(matrix), ActorUserId);

        Assert.Contains(auditWriter.Events, audit =>
            audit.Action == AuditAction.Created &&
            audit.TenantId == TenantId &&
            audit.ActorUserId == ActorUserId &&
            audit.EntityId == acknowledgement.Id.ToString() &&
            audit.Metadata["matrixVersion"] == matrix.Version &&
            audit.Metadata["result"] == "acknowledged" &&
            !string.IsNullOrWhiteSpace(audit.Metadata["acknowledgedAt"]));
    }

    private static SharedResponsibilityMatrixAcknowledgementService CreateService(
        GccsDbContext dbContext,
        IAuditEventWriter? auditWriter = null) =>
        new(new EfSharedResponsibilityMatrixAcknowledgementRepository(dbContext), auditWriter ?? new CapturingAuditEventWriter());

    private static SharedResponsibilityMatrixDto Matrix(string version) =>
        new(
            "gccs-cui-ready-baseline",
            version,
            "GCCS CUI-Ready Shared Responsibility Matrix",
            "Published",
            new DateOnly(2026, 6, 18),
            "GCCS Security Owner",
            new DateOnly(2026, 6, 18),
            "Phase 1A CUI readiness baseline",
            []);

    private static AcknowledgeSharedResponsibilityMatrixRequest Acknowledge(SharedResponsibilityMatrixDto matrix) =>
        new(matrix.MatrixId, matrix.Version, true);

    private static GccsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GccsDbContext>()
            .UseInMemoryDatabase($"shared-responsibility-matrix-acknowledgements-{Guid.NewGuid():N}")
            .Options;

        return new GccsDbContext(options);
    }

    private static void SeedTenant(GccsDbContext dbContext)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = TenantId,
            Name = "Matrix Acknowledgement Tenant",
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
