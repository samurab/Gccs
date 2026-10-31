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

public sealed class ExternalPortalAccessPostgresTests
{
    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Invitation_and_required_audit_are_atomic()
    {
        await using var harness = await Harness.CreateAsync();
        var service = new ExternalPortalAccessService(
            new EfExternalPortalAccessRepository(harness.Db), new ThrowingAuditWriter(),
            AllowAllScopeValidator.Instance, harness.TimeProvider);

        await Assert.ThrowsAsync<InvalidOperationException>(() => harness.Transaction.ExecuteAsync(
            token => service.InviteAsync(harness.Request, harness.TenantId, harness.AdminUserId, token)));

        harness.Db.ChangeTracker.Clear();
        Assert.Empty(await harness.Db.ExternalPortalInvitations.Where(item => item.TenantId == harness.TenantId).ToArrayAsync());
        Assert.Empty(await harness.Db.AuditLogEntries.Where(item => item.TenantId == harness.TenantId).ToArrayAsync());
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Access_state_history_and_required_audit_roll_back_together()
    {
        await using var harness = await Harness.CreateAsync();
        var repository = new EfExternalPortalAccessRepository(harness.Db);
        var service = new ExternalPortalAccessService(repository, harness.AuditWriter, AllowAllScopeValidator.Instance, harness.TimeProvider);
        var invitation = await harness.Transaction.ExecuteAsync(
            token => service.InviteAsync(harness.Request, harness.TenantId, harness.AdminUserId, token));
        var failing = new ExternalPortalAccessService(repository, new ThrowingAuditWriter(), AllowAllScopeValidator.Instance, harness.TimeProvider);

        await Assert.ThrowsAsync<InvalidOperationException>(() => harness.Transaction.ExecuteAsync(
            token => failing.ValidateAccessAsync(new ExternalPortalAccessRequest(
                invitation.Id, harness.PackageId, null, harness.ExternalUserId, "reviewer@example.test",
                true, harness.Now), token)));

        harness.Db.ChangeTracker.Clear();
        var stored = await harness.Db.ExternalPortalInvitations.SingleAsync(item => item.Id == invitation.Id);
        Assert.Equal(ExternalPortalInvitationStatus.Pending, stored.Status);
        Assert.Null(stored.ExternalUserId);
        Assert.Null(stored.LastAccessedAt);
        Assert.Empty(await harness.Db.ExternalPortalAccessHistory.Where(item => item.InvitationId == invitation.Id).ToArrayAsync());
        Assert.Single(await harness.Db.AuditLogEntries.Where(item => item.EntityId == invitation.Id.ToString()).ToArrayAsync());
    }

    private sealed class ThrowingAuditWriter : IAuditEventWriter
    {
        public Task WriteAsync(Guid tenantId, Guid actorUserId, AuditAction action, string entityType, string entityId,
            string summary, IReadOnlyDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default) => throw new InvalidOperationException("Injected audit failure.");
    }

    private sealed class AllowAllScopeValidator : IExternalPortalScopeValidator
    {
        public static AllowAllScopeValidator Instance { get; } = new();
        public Task ValidateAsync(Guid tenantId, IReadOnlyList<Guid> packageIds, IReadOnlyList<Guid> contractIds,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class Harness : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;
        public GccsDbContext Db { get; }
        public IApplicationTransaction Transaction { get; }
        public IAuditEventWriter AuditWriter { get; }
        public Guid TenantId { get; } = Guid.NewGuid();
        public Guid AdminUserId { get; } = Guid.NewGuid();
        public Guid ExternalUserId { get; } = Guid.NewGuid();
        public Guid PackageId { get; } = Guid.NewGuid();
        public DateTimeOffset Now { get; } = new(2026, 9, 12, 12, 0, 0, TimeSpan.Zero);
        public FixedTimeProvider TimeProvider { get; }
        public ExternalPortalInvitationRequest Request => new(
            "reviewer@example.test", ExternalPortalRole.AuditorReviewer, [PackageId], [],
            Now.AddDays(30), CanDownload: false, StrongAuthenticationRequired: true);

        private Harness(GccsDbContext db, ServiceProvider provider)
        {
            Db = db;
            _provider = provider;
            Transaction = new EfApplicationTransaction(provider);
            AuditWriter = new EfAuditEventWriter(db,
                new StaticAuditRequestMetadata("127.0.0.1", "external-portal-postgres-test", Guid.NewGuid().ToString()));
            TimeProvider = new FixedTimeProvider(Now);
        }

        public static async Task<Harness> CreateAsync()
        {
            var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")!;
            var options = new DbContextOptionsBuilder<GccsDbContext>().UseGccsPostgres(connectionString).Options;
            var db = new GccsDbContext(options);
            await PostgresTestDatabase.MigrateAsync(db);
            var provider = new ServiceCollection().AddSingleton(db).BuildServiceProvider();
            var harness = new Harness(db, provider);
            db.Tenants.Add(new TenantEntity
            {
                Id = harness.TenantId, Name = "External portal PostgreSQL test tenant",
                Status = TenantStatus.Active, DataPosture = TenantDataPosture.NoCui, CreatedAt = harness.Now
            });
            await db.SaveChangesAsync();
            return harness;
        }

        public async ValueTask DisposeAsync()
        {
            Db.ChangeTracker.Clear();
            await Db.ExternalPortalAccessHistory.Where(item => item.TenantId == TenantId).ExecuteDeleteAsync();
            await Db.ExternalPortalInvitationContractScopes.Where(item => item.TenantId == TenantId).ExecuteDeleteAsync();
            await Db.ExternalPortalInvitationPackageScopes.Where(item => item.TenantId == TenantId).ExecuteDeleteAsync();
            await Db.ExternalPortalInvitations.Where(item => item.TenantId == TenantId).ExecuteDeleteAsync();
            await Db.AuditLogEntries.Where(item => item.TenantId == TenantId).ExecuteDeleteAsync();
            await Db.Tenants.Where(item => item.Id == TenantId).ExecuteDeleteAsync();
            await Db.DisposeAsync();
            await _provider.DisposeAsync();
        }
    }

    private sealed record StaticAuditRequestMetadata(string IpAddress, string UserAgent, string CorrelationId) : IAuditRequestMetadata;
}
