using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Portals;
using Gccs.Domain.Audit;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Common;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Portals;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class PortalPackageLifecyclePostgresTests
{
    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Reissue_persists_bidirectional_lineage_and_atomic_audits()
    {
        var harness = await CreateHarnessAsync();
        try
        {
            var original = await harness.Service.ShareAsync(
                new SharedPortalPackageRequest(harness.PackageId, harness.InvitationId, harness.Now.AddDays(30)),
                harness.TenantId,
                harness.ActorUserId);

            var replacement = await harness.Service.ReissueAsync(
                original.Id,
                harness.TenantId,
                new ReissueSharedPortalPackageRequest(Guid.NewGuid(), harness.Now.AddDays(60)),
                harness.ActorUserId);
            Assert.NotNull(replacement);

            harness.Db.ChangeTracker.Clear();
            var rows = await harness.Db.SharedPortalPackages.AsNoTracking()
                .Where(row => row.TenantId == harness.TenantId).OrderBy(row => row.Version).ToArrayAsync();
            Assert.Equal(2, rows.Length);
            Assert.Equal(replacement.Id, rows[0].ReplacementSharedPackageId);
            Assert.Equal(original.Id, rows[1].SupersedesSharedPackageId);
            Assert.Equal(SharedPortalPackageState.Superseded, rows[0].State);
            Assert.Equal(SharedPortalPackageState.Active, rows[1].State);
            Assert.Equal(3, await harness.Db.AuditLogEntries.CountAsync(entry =>
                entry.TenantId == harness.TenantId && entry.EntityType == "SharedPortalPackage"));
        }
        finally
        {
            await harness.DisposeAsync();
        }
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Failed_revocation_audit_rolls_back_state_and_activity()
    {
        var harness = await CreateHarnessAsync();
        try
        {
            var original = await harness.Service.ShareAsync(
                new SharedPortalPackageRequest(harness.PackageId, harness.InvitationId, harness.Now.AddDays(30)),
                harness.TenantId,
                harness.ActorUserId);
            var failing = new PortalPackageLifecycleService(
                new EfPortalPackageLifecycleRepository(harness.Db),
                new AllowAllEligibilityValidator(),
                new ThrowingAuditWriter(),
                new EfApplicationTransaction(harness.Provider),
                new FixedTimeProvider(harness.Now));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                failing.RevokeAsync(original.Id, harness.TenantId, "Rollback test.", harness.ActorUserId));

            harness.Db.ChangeTracker.Clear();
            var stored = await harness.Db.SharedPortalPackages.AsNoTracking().SingleAsync(row => row.Id == original.Id);
            Assert.Equal(SharedPortalPackageState.Active, stored.State);
            Assert.Empty(await harness.Db.PortalPackageActivities.Where(activity =>
                activity.SharedPackageId == original.Id && activity.ActivityType == PortalPackageActivityType.Revocation).ToArrayAsync());
        }
        finally
        {
            await harness.DisposeAsync();
        }
    }

    private static async Task<Harness> CreateHarnessAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")!;
        var options = new DbContextOptionsBuilder<GccsDbContext>().UseGccsPostgres(connectionString).Options;
        var db = new GccsDbContext(options);
        await PostgresTestDatabase.MigrateAsync(db);
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);
        db.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = "Portal lifecycle test tenant",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = now
        });
        await db.SaveChangesAsync();
        var provider = new ServiceCollection().AddSingleton(db).BuildServiceProvider();
        var service = new PortalPackageLifecycleService(
            new EfPortalPackageLifecycleRepository(db),
            new AllowAllEligibilityValidator(),
            new EfAuditEventWriter(db, new StaticAuditRequestMetadata("127.0.0.1", "portal-postgres-test", Guid.NewGuid().ToString())),
            new EfApplicationTransaction(provider),
            new FixedTimeProvider(now));
        return new(service, db, provider, tenantId, actorUserId, Guid.NewGuid(), Guid.NewGuid(), now);
    }

    private sealed class AllowAllEligibilityValidator : IPortalPackageShareEligibilityValidator
    {
        public Task<PortalPackageApprovalMetadataDto> ValidateAsync(Guid packageId, Guid invitationId, Guid tenantId, DateTimeOffset asOf, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PortalPackageApprovalMetadataDto(1, new string('a', 64)));
    }

    private sealed class ThrowingAuditWriter : IAuditEventWriter
    {
        public Task WriteAsync(Guid tenantId, Guid actorUserId, AuditAction action, string entityType, string entityId, string summary, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Injected audit failure.");
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed record StaticAuditRequestMetadata(string IpAddress, string UserAgent, string CorrelationId) : IAuditRequestMetadata;

    private sealed record Harness(
        PortalPackageLifecycleService Service,
        GccsDbContext Db,
        ServiceProvider Provider,
        Guid TenantId,
        Guid ActorUserId,
        Guid PackageId,
        Guid InvitationId,
        DateTimeOffset Now) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            Db.ChangeTracker.Clear();
            await Db.PortalPackageActivities.Where(row => row.TenantId == TenantId).ExecuteDeleteAsync();
            await Db.SharedPortalPackages.Where(row => row.TenantId == TenantId).ExecuteDeleteAsync();
            await Db.AuditLogEntries.Where(row => row.TenantId == TenantId).ExecuteDeleteAsync();
            await Db.Tenants.Where(row => row.Id == TenantId).ExecuteDeleteAsync();
            await Db.DisposeAsync();
            await Provider.DisposeAsync();
        }
    }
}
