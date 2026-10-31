using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Reports;
using Gccs.Application.Security;
using Gccs.Domain.Companies;
using Gccs.Domain.Contracts;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Common;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class SprReportPackagePostgresConcurrencyTests
{
    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Concurrent_review_decisions_commit_one_transition_and_one_audit_event()
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")!;
        var options = new DbContextOptionsBuilder<GccsDbContext>().UseGccsPostgres(connectionString).Options;
        var tenantId = Guid.NewGuid(); var actorUserId = Guid.NewGuid(); var contractId = Guid.NewGuid(); var packageId = Guid.NewGuid();
        await using (var setup = new GccsDbContext(options))
        {
            await PostgresTestDatabase.MigrateAsync(setup);
            var now = DateTimeOffset.UtcNow;
            setup.Tenants.Add(new TenantEntity { Id = tenantId, Name = "SPR concurrency tenant", Status = TenantStatus.Active,
                DataPosture = TenantDataPosture.NoCui, CreatedAt = now });
            setup.Users.Add(new UserEntity { Id = actorUserId, TenantId = tenantId, PreferredTenantId = tenantId,
                Email = $"spr-{actorUserId:N}@example.invalid", DisplayName = "SPR reviewer", Status = UserStatus.Active, CreatedAt = now });
            setup.Contracts.Add(new ContractEntity { Id = contractId, TenantId = tenantId, ContractNumber = $"SPR-{contractId:N}",
                Title = "SPR concurrency contract", AgencyOrPrimeName = "Synthetic agency", Relationship = ContractorRelationship.Prime,
                Kind = ContractKind.FixedPrice, Status = ContractStatus.Active, PeriodOfPerformanceStart = new(2026, 1, 1),
                PeriodOfPerformanceEnd = new(2026, 12, 31), PlaceOfPerformance = "Synthetic", Description = "No-CUI test data.",
                DataHandlingPosture = DataHandlingPosture.FciOnly, CreatedAt = now });
            setup.SprReportPackages.Add(new SprReportPackageEntity { Id = packageId, TenantId = tenantId, ContractId = contractId,
                ReportType = EsrsReportType.Isr, PeriodStart = new(2026, 1, 1), PeriodEnd = new(2026, 3, 31),
                Status = EsrsReportPackageStatus.Draft, Version = 1, NotSubmittedDisclaimer = EsrsReportPackageService.NotSubmittedDisclaimer,
                SnapshotJson = JsonSerializer.Serialize(EmptySnapshot(contractId), JsonOptions), GeneratedAt = now, CreatedAt = now,
                CreatedByUserId = actorUserId });
            await setup.SaveChangesAsync();
        }

        await using var first = CreateHarness(options, tenantId, actorUserId, new TwoReaderBarrier());
        await using var second = CreateHarness(options, tenantId, actorUserId, first.Barrier);
        try
        {
            var attempts = new[]
            {
                AttemptReviewAsync(first.Service, packageId, "First reviewer", actorUserId),
                AttemptReviewAsync(second.Service, packageId, "Second reviewer", actorUserId)
            };
            var results = await Task.WhenAll(attempts);

            Assert.Single(results, result => result);
            Assert.Single(results, result => !result);
            await using var verification = new GccsDbContext(options);
            var package = await verification.SprReportPackages.AsNoTracking().SingleAsync(item => item.Id == packageId);
            Assert.Equal(EsrsReportPackageStatus.InReview, package.Status);
            Assert.Equal(1, await verification.AuditLogEntries.CountAsync(item => item.TenantId == tenantId &&
                item.EntityType == "EsrsReportPackage" && item.EntityId == packageId.ToString()));
        }
        finally
        {
            await CleanupAsync(options, tenantId);
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static EsrsReportPackageSnapshotDto EmptySnapshot(Guid contractId) => new(contractId, EsrsReportType.Isr,
        new(2026, 1, 1), new(2026, 3, 31), 0, 0, [], [], ["No eligible rows."], []);

    private static async Task<bool> AttemptReviewAsync(EsrsReportPackageService service, Guid packageId, string reviewer, Guid actorUserId)
    {
        try
        {
            await service.BeginReviewAsync(packageId, new EsrsReportPackageReviewRequest(reviewer, "Concurrent review attempt."), actorUserId);
            return true;
        }
        catch (EsrsReportPackageConflictException)
        {
            return false;
        }
    }

    private static Harness CreateHarness(DbContextOptions<GccsDbContext> options, Guid tenantId, Guid actorUserId, TwoReaderBarrier barrier)
    {
        var db = new GccsDbContext(options);
        var services = new ServiceCollection().AddSingleton(db).BuildServiceProvider();
        var repository = new BarrierPackageRepository(new EfEsrsReportPackageRepository(db, new FixedTenantContext(tenantId, actorUserId)), barrier);
        var auditWriter = new EfAuditEventWriter(db, new StaticAuditRequestMetadata("127.0.0.1", "postgres-concurrency-test", Guid.NewGuid().ToString()));
        var reportData = new SubcontractingReportDataService(new InMemorySubcontractingReportDataRepository(tenantId),
            new SprSchemaProfileService(new InMemorySprSchemaProfileRepository()), auditWriter, new EfApplicationTransaction(services));
        var service = new EsrsReportPackageService(reportData, repository, new DisabledSprSubmissionProvider(), auditWriter,
            new EfApplicationTransaction(services));
        return new Harness(service, db, services, barrier);
    }

    private static async Task CleanupAsync(DbContextOptions<GccsDbContext> options, Guid tenantId)
    {
        await using var cleanup = new GccsDbContext(options);
        await cleanup.AuditLogEntries.Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
        await cleanup.SprManualSubmissionReceipts.Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
        await cleanup.SprReportPackages.Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
        await cleanup.Contracts.Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
        await cleanup.Users.Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
        await cleanup.Tenants.Where(item => item.Id == tenantId).ExecuteDeleteAsync();
    }

    private sealed record FixedTenantContext(Guid TenantId, Guid UserId) : ICurrentTenantContext
    {
        public string UserEmail => "spr-reviewer@example.invalid";
    }

    private sealed record StaticAuditRequestMetadata(string IpAddress, string UserAgent, string CorrelationId) : IAuditRequestMetadata;

    private sealed class TwoReaderBarrier
    {
        private readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int readers;
        public async Task SignalAndWaitAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref readers) == 2) completion.TrySetResult();
            await completion.Task.WaitAsync(cancellationToken);
        }
    }

    private sealed class BarrierPackageRepository(IEsrsReportPackageRepository inner, TwoReaderBarrier barrier) : IEsrsReportPackageRepository
    {
        public Task<EsrsReportPackageDto> CreateAsync(EsrsReportPackageGenerateRequest request, EsrsReportPackageSnapshotDto snapshot,
            Guid actorUserId, CancellationToken cancellationToken = default) => inner.CreateAsync(request, snapshot, actorUserId, cancellationToken);
        public async Task<EsrsReportPackageDto?> FindAsync(Guid packageId, CancellationToken cancellationToken = default)
        {
            var package = await inner.FindAsync(packageId, cancellationToken);
            await barrier.SignalAndWaitAsync(cancellationToken);
            return package;
        }
        public Task<IReadOnlyList<EsrsReportPackageDto>> ListAsync(CancellationToken cancellationToken = default) => inner.ListAsync(cancellationToken);
        public Task<EsrsReportPackageDto?> UpdateStatusAsync(Guid packageId, EsrsReportPackageStatus expectedStatus,
            EsrsReportPackageStatus status, string reviewerName, string? reviewNotes, Guid actorUserId,
            CancellationToken cancellationToken = default) =>
            inner.UpdateStatusAsync(packageId, expectedStatus, status, reviewerName, reviewNotes, actorUserId, cancellationToken);
        public Task<SprManualSubmissionReceiptDto> CreateManualSubmissionReceiptAsync(Guid packageId,
            SprManualSubmissionReceiptRequest request, Guid actorUserId, CancellationToken cancellationToken = default) =>
            inner.CreateManualSubmissionReceiptAsync(packageId, request, actorUserId, cancellationToken);
        public Task<IReadOnlyList<SprManualSubmissionReceiptDto>> ListManualSubmissionReceiptsAsync(Guid packageId,
            CancellationToken cancellationToken = default) => inner.ListManualSubmissionReceiptsAsync(packageId, cancellationToken);
    }

    private sealed record Harness(EsrsReportPackageService Service, GccsDbContext Db, ServiceProvider Provider, TwoReaderBarrier Barrier) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await Provider.DisposeAsync();
        }
    }
}
