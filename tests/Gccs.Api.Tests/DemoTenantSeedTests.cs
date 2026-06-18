using Gccs.Application.Audit;
using Gccs.Application.Demo;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Demo;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class DemoTenantSeedTests
{
    private static readonly Guid ActorUserId = Guid.Parse("1a030200-0000-4000-8000-000000000001");

    [Fact]
    public async Task TC_1A_3_2_1_Seed_runs_only_for_demo_sandbox_tenants()
    {
        var dataset = await LoadDatasetAsync();
        await using var dbContext = CreateDbContext();
        var demoTenantId = SeedTenant(dbContext, TenantDataPosture.DemoSandbox);
        var noCuiTenantId = SeedTenant(dbContext, TenantDataPosture.NoCui);
        var cuiReadyTenantId = SeedTenant(dbContext, TenantDataPosture.CuiReady);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var demoResult = await service.SeedAsync(dataset, demoTenantId, ActorUserId);
        var noCuiError = await Assert.ThrowsAsync<DemoTenantSeedValidationException>(() => service.SeedAsync(dataset, noCuiTenantId, ActorUserId));
        var cuiReadyError = await Assert.ThrowsAsync<DemoTenantSeedValidationException>(() => service.SeedAsync(dataset, cuiReadyTenantId, ActorUserId));

        Assert.True(demoResult.CreatedCount > 0);
        Assert.Contains("DemoSandbox", noCuiError.Message);
        Assert.Contains("DemoSandbox", cuiReadyError.Message);
        Assert.Equal(1, await dbContext.Contracts.CountAsync(contract => contract.TenantId == demoTenantId));
        Assert.Empty(await dbContext.Contracts.Where(contract => contract.TenantId == noCuiTenantId || contract.TenantId == cuiReadyTenantId).ToArrayAsync());
    }

    [Fact]
    public async Task TC_1A_3_2_2_Seed_process_is_idempotent()
    {
        var dataset = await LoadDatasetAsync();
        await using var dbContext = CreateDbContext();
        var tenantId = SeedTenant(dbContext, TenantDataPosture.DemoSandbox);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var first = await service.SeedAsync(dataset, tenantId, ActorUserId);
        var second = await service.SeedAsync(dataset, tenantId, ActorUserId);

        Assert.True(first.CreatedCount > 0);
        Assert.Equal(0, second.CreatedCount);
        Assert.Equal(1, await dbContext.Contracts.CountAsync(contract => contract.TenantId == tenantId));
        Assert.Equal(1, await dbContext.EvidenceItems.CountAsync(evidence => evidence.TenantId == tenantId));
        Assert.Equal(1, await dbContext.Reports.CountAsync(report => report.TenantId == tenantId));
    }

    [Fact]
    public async Task TC_1A_3_2_3_End_to_end_demo_data_present()
    {
        var dataset = await LoadDatasetAsync();
        await using var dbContext = CreateDbContext();
        var tenantId = SeedTenant(dbContext, TenantDataPosture.DemoSandbox);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        await service.SeedAsync(dataset, tenantId, ActorUserId);

        Assert.Contains(await dbContext.Contracts.Where(contract => contract.TenantId == tenantId).ToArrayAsync(), contract => contract.Title.Contains("Synthetic demo data", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(await dbContext.Set<ContractDocumentEntity>().ToArrayAsync(), document => document.Classification == ContentClassification.SyntheticCui && document.ClassificationIsApprovedDemoContent);
        Assert.Contains(await dbContext.Set<ContractClauseEntity>().ToArrayAsync(), clause => clause.Title.Contains("Synthetic demo data", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(await dbContext.Set<ContractClauseObligationEntity>().ToArrayAsync(), link => link.ObligationId == "demo-synthetic-cui-safeguarding");
        Assert.Contains(await dbContext.EvidenceItems.Where(evidence => evidence.TenantId == tenantId).ToArrayAsync(), evidence => evidence.Classification == ContentClassification.SyntheticCui);
        Assert.Contains(await dbContext.Assessments.Where(assessment => assessment.TenantId == tenantId).ToArrayAsync(), assessment => assessment.Name.Contains("Synthetic demo data", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(await dbContext.Subcontractors.Where(subcontractor => subcontractor.TenantId == tenantId).ToArrayAsync(), subcontractor => subcontractor.Name.Contains("Synthetic demo data", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(await dbContext.Reports.Where(report => report.TenantId == tenantId).ToArrayAsync(), report => report.Classification == ContentClassification.SyntheticCui);
        Assert.Contains(await dbContext.ExpertReviewItems.Where(item => item.TenantId == tenantId).ToArrayAsync(), item => item.SourceType == "SyntheticDemoSeed");
    }

    [Fact]
    public async Task TC_1A_3_2_4_Customer_tenants_cannot_receive_demo_seed_data()
    {
        var dataset = await LoadDatasetAsync();
        await using var dbContext = CreateDbContext();
        var tenantId = SeedTenant(dbContext, TenantDataPosture.NoCui);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<DemoTenantSeedValidationException>(() => service.SeedAsync(dataset, tenantId, ActorUserId));

        Assert.Empty(await dbContext.Contracts.Where(contract => contract.TenantId == tenantId).ToArrayAsync());
        Assert.Empty(await dbContext.EvidenceItems.Where(evidence => evidence.TenantId == tenantId).ToArrayAsync());
        Assert.Empty(await dbContext.Reports.Where(report => report.TenantId == tenantId).ToArrayAsync());
    }

    [Fact]
    public async Task TC_1A_3_2_5_Seed_and_reset_are_audited()
    {
        var dataset = await LoadDatasetAsync();
        await using var dbContext = CreateDbContext();
        var tenantId = SeedTenant(dbContext, TenantDataPosture.DemoSandbox);
        await dbContext.SaveChangesAsync();
        var auditWriter = new CapturingAuditEventWriter();
        var service = CreateService(dbContext, auditWriter);

        var seed = await service.SeedAsync(dataset, tenantId, ActorUserId);
        var reset = await service.ResetAsync(dataset, tenantId, ActorUserId);

        Assert.True(seed.CreatedCount > 0);
        Assert.True(reset.DeletedCount > 0);
        Assert.Empty(await dbContext.Contracts.Where(contract => contract.TenantId == tenantId).ToArrayAsync());
        Assert.Collection(
            auditWriter.Events,
            audit => AssertAudit(audit, tenantId, "seed", dataset.Metadata.Version, "succeeded"),
            audit => AssertAudit(audit, tenantId, "reset", dataset.Metadata.Version, "succeeded"));
    }

    private static async Task<SyntheticDemoDatasetDefinition> LoadDatasetAsync()
    {
        var service = new SyntheticDemoDatasetService(new FileSyntheticDemoDatasetRepository());
        return await service.GetAsync(GetDemoContentPackageRoot());
    }

    private static DemoTenantSeedService CreateService(GccsDbContext dbContext, IAuditEventWriter? auditWriter = null) =>
        new(new EfDemoTenantSeedRepository(dbContext), auditWriter ?? new CapturingAuditEventWriter());

    private static GccsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GccsDbContext>()
            .UseInMemoryDatabase($"demo-tenant-seed-{Guid.NewGuid():N}")
            .Options;

        return new GccsDbContext(options);
    }

    private static Guid SeedTenant(GccsDbContext dbContext, TenantDataPosture posture)
    {
        var tenantId = Guid.NewGuid();
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = $"{posture} Tenant",
            Status = TenantStatus.Active,
            DataPosture = posture,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = ActorUserId
        });
        return tenantId;
    }

    private static string GetDemoContentPackageRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Gccs.slnx")))
        {
            current = current.Parent;
        }

        if (current is null)
        {
            throw new DirectoryNotFoundException("Could not locate repository root from test output directory.");
        }

        return Path.Combine(current.FullName, "packages", "demo-content");
    }

    private static void AssertAudit(CapturedAuditEvent audit, Guid tenantId, string action, string datasetVersion, string result)
    {
        Assert.Equal(tenantId, audit.TenantId);
        Assert.Equal(ActorUserId, audit.ActorUserId);
        Assert.Equal("SyntheticDemoSeed", audit.EntityType);
        Assert.Equal(action, audit.Metadata["seedAction"]);
        Assert.Equal(datasetVersion, audit.Metadata["datasetVersion"]);
        Assert.Equal(result, audit.Metadata["result"]);
        Assert.Equal(tenantId.ToString(), audit.Metadata["tenantId"]);
        Assert.Equal(ActorUserId.ToString(), audit.Metadata["actorUserId"]);
        Assert.True(DateTimeOffset.TryParse(audit.Metadata["timestamp"], out _));
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
