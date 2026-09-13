using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Portals;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Domain.Evidence;
using Gccs.Domain.Reports;
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

public sealed class ApprovedPackagePortalReviewPostgresTests
{
    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Failed_message_audit_rolls_back_message_activity_and_access_state()
    {
        await using var harness = await Harness.CreateAsync();
        var repository = new EfPortalPackageRepository(harness.Db, harness.TimeProvider);
        var auditWriter = new FailPortalMessageAuditWriter(harness.AuditWriter);
        var accessService = new ExternalPortalAccessService(
            new EfExternalPortalAccessRepository(harness.Db), auditWriter,
            AllowAllScopeValidator.Instance, harness.TimeProvider);
        var lifecycleService = new PortalPackageLifecycleService(
            new EfPortalPackageLifecycleRepository(harness.Db), AllowAllEligibilityValidator.Instance,
            auditWriter, harness.Transaction, harness.TimeProvider);
        var service = new ApprovedPackagePortalReviewService(
            accessService, repository, lifecycleService, auditWriter, harness.Transaction,
            harness.TimeProvider, new PortalReviewDownloadPolicy(true));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddMessageAsync(
            harness.InvitationId, harness.SharedPackageId,
            new PortalPackageReviewMessageRequest(PortalCommentKind.Question, "Rollback this message."),
            new PortalReviewerIdentity(harness.ExternalUserId, "reviewer@example.test", true)));

        harness.Db.ChangeTracker.Clear();
        Assert.Empty(await harness.Db.PortalPackageReviewMessages
            .Where(item => item.TenantId == harness.TenantId).ToArrayAsync());
        Assert.Empty(await harness.Db.PortalPackageActivities
            .Where(item => item.TenantId == harness.TenantId).ToArrayAsync());
        Assert.Empty(await harness.Db.ExternalPortalAccessHistory
            .Where(item => item.TenantId == harness.TenantId).ToArrayAsync());
        Assert.Empty(await harness.Db.AuditLogEntries
            .Where(item => item.TenantId == harness.TenantId).ToArrayAsync());
        var invitation = await harness.Db.ExternalPortalInvitations.AsNoTracking()
            .SingleAsync(item => item.Id == harness.InvitationId);
        Assert.Equal(ExternalPortalInvitationStatus.Pending, invitation.Status);
        Assert.Null(invitation.ExternalUserId);
        Assert.Null(invitation.LastAccessedAt);
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Failed_download_audit_rolls_back_download_activity_and_access_state()
    {
        await using var harness = await Harness.CreateAsync();
        var repository = new EfPortalPackageRepository(harness.Db, harness.TimeProvider);
        var auditWriter = new FailDownloadAuditWriter(harness.AuditWriter);
        var service = new ApprovedPackagePortalReviewService(
            new ExternalPortalAccessService(new EfExternalPortalAccessRepository(harness.Db), auditWriter,
                AllowAllScopeValidator.Instance, harness.TimeProvider),
            repository,
            new PortalPackageLifecycleService(new EfPortalPackageLifecycleRepository(harness.Db),
                AllowAllEligibilityValidator.Instance, auditWriter, harness.Transaction, harness.TimeProvider),
            auditWriter, harness.Transaction, harness.TimeProvider, new PortalReviewDownloadPolicy(true));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DownloadAsync(
            harness.InvitationId, harness.SharedPackageId,
            new PortalReviewerIdentity(harness.ExternalUserId, "reviewer@example.test", true)));

        harness.Db.ChangeTracker.Clear();
        Assert.Empty(await harness.Db.PortalPackageActivities.Where(item => item.TenantId == harness.TenantId).ToArrayAsync());
        Assert.Empty(await harness.Db.ExternalPortalAccessHistory.Where(item => item.TenantId == harness.TenantId).ToArrayAsync());
        Assert.Empty(await harness.Db.AuditLogEntries.Where(item => item.TenantId == harness.TenantId).ToArrayAsync());
    }

    private sealed class FailPortalMessageAuditWriter(IAuditEventWriter inner) : IAuditEventWriter
    {
        public Task WriteAsync(Guid tenantId, Guid actorUserId, AuditAction action, string entityType,
            string entityId, string summary, IReadOnlyDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default) =>
            entityType == "PortalPackageReviewMessage"
                ? throw new InvalidOperationException("Injected portal message audit failure.")
                : inner.WriteAsync(tenantId, actorUserId, action, entityType, entityId, summary, metadata, cancellationToken);
    }

    private sealed class FailDownloadAuditWriter(IAuditEventWriter inner) : IAuditEventWriter
    {
        public Task WriteAsync(Guid tenantId, Guid actorUserId, AuditAction action, string entityType,
            string entityId, string summary, IReadOnlyDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default) =>
            entityType == "PortalPackage" && action == AuditAction.Downloaded
                ? throw new InvalidOperationException("Injected portal download audit failure.")
                : inner.WriteAsync(tenantId, actorUserId, action, entityType, entityId, summary, metadata, cancellationToken);
    }

    private sealed class AllowAllScopeValidator : IExternalPortalScopeValidator
    {
        public static AllowAllScopeValidator Instance { get; } = new();
        public Task ValidateAsync(Guid tenantId, IReadOnlyList<Guid> packageIds, IReadOnlyList<Guid> contractIds,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class AllowAllEligibilityValidator : IPortalPackageShareEligibilityValidator
    {
        public static AllowAllEligibilityValidator Instance { get; } = new();
        public Task<PortalPackageApprovalMetadataDto> ValidateAsync(Guid packageId, Guid invitationId, Guid tenantId, DateTimeOffset asOf,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new PortalPackageApprovalMetadataDto(1, new string('a', 64)));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed record StaticAuditRequestMetadata(string IpAddress, string UserAgent, string CorrelationId)
        : IAuditRequestMetadata;

    private sealed class Harness : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;
        public GccsDbContext Db { get; }
        public IApplicationTransaction Transaction { get; }
        public IAuditEventWriter AuditWriter { get; }
        public Guid TenantId { get; } = Guid.NewGuid();
        public Guid AdminUserId { get; } = Guid.NewGuid();
        public Guid ExternalUserId { get; } = Guid.NewGuid();
        public Guid EvidenceId { get; } = Guid.NewGuid();
        public Guid PackageId { get; } = Guid.NewGuid();
        public Guid InvitationId { get; } = Guid.NewGuid();
        public Guid SharedPackageId { get; } = Guid.NewGuid();
        public DateTimeOffset Now { get; } = new(2026, 9, 13, 12, 0, 0, TimeSpan.Zero);
        public FixedTimeProvider TimeProvider { get; }

        private Harness(GccsDbContext db, ServiceProvider provider)
        {
            Db = db;
            _provider = provider;
            Transaction = new EfApplicationTransaction(provider);
            AuditWriter = new EfAuditEventWriter(db,
                new StaticAuditRequestMetadata("127.0.0.1", "portal-review-postgres-test", Guid.NewGuid().ToString()));
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
                Id = harness.TenantId, Name = "Portal review PostgreSQL test tenant",
                Status = TenantStatus.Active, DataPosture = TenantDataPosture.NoCui, CreatedAt = harness.Now
            });
            db.EvidenceItems.Add(new EvidenceItemEntity
            {
                Id = harness.EvidenceId, TenantId = harness.TenantId, Name = "Approved evidence",
                Description = "External-safe reference", Type = EvidenceType.Policy,
                Status = EvidenceStatus.Approved, Classification = ContentClassification.Fci,
                ApprovedAt = harness.Now, ApprovedByUserId = harness.AdminUserId, CreatedAt = harness.Now
            });
            var report = new ReportEntity
            {
                Id = harness.PackageId, TenantId = harness.TenantId, Type = ReportType.CmmcReadiness,
                Title = "Approved package", Status = ReportStatus.Complete, GeneratedAt = harness.Now,
                GeneratedByUserId = harness.AdminUserId, Classification = ContentClassification.Fci,
                SnapshotJson = "{}", ExportHtml = "<main>Approved package</main>", CreatedAt = harness.Now
            };
            report.EvidenceItems.Add(new ReportEvidenceEntity
            {
                ReportId = harness.PackageId, EvidenceItemId = harness.EvidenceId
            });
            db.Reports.Add(report);
            db.ExternalPortalInvitations.Add(new ExternalPortalInvitationEntity
            {
                Id = harness.InvitationId, TenantId = harness.TenantId, Email = "reviewer@example.test",
                Role = ExternalPortalRole.AuditorReviewer, ExpiresAt = harness.Now.AddDays(30),
                CanDownload = true, StrongAuthenticationRequired = true,
                Status = ExternalPortalInvitationStatus.Pending, Version = 1, CreatedAt = harness.Now,
                PackageScopes =
                [
                    new ExternalPortalInvitationPackageScopeEntity
                    {
                        TenantId = harness.TenantId, InvitationId = harness.InvitationId,
                        PackageId = harness.PackageId
                    }
                ]
            });
            db.SharedPortalPackages.Add(new SharedPortalPackageEntity
            {
                Id = harness.SharedPackageId, TenantId = harness.TenantId,
                InvitationId = harness.InvitationId, PackageId = harness.PackageId,
                Version = 1, State = SharedPortalPackageState.Active,
                ExpiresAt = harness.Now.AddDays(30), ReviewDueAt = harness.Now.AddDays(21),
                ExternalReviewApprovedAt = harness.Now,
                ExternalReviewApprovedByUserId = harness.AdminUserId,
                ExternalReviewApprovalReason = "PostgreSQL atomicity test approval",
                ApprovedSourceVersion = 1,
                ApprovedSourceFingerprint = PortalPackageFingerprint.Create(new PortalPackageDto(
                    harness.PackageId, harness.TenantId, null, "Approved package", 1,
                    PortalPackageStatus.Approved, ContentClassification.Fci, false,
                    [harness.EvidenceId], harness.Now)
                {
                    SourceKind = $"Report:{ReportType.CmmcReadiness}",
                    EvidenceReferences = [new PortalEvidenceReferenceDto(
                        harness.EvidenceId, "Approved evidence", EvidenceType.Policy.ToString(),
                        ContentClassification.Fci, harness.Now, null)],
                    SourceIntegrityFingerprint = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                        System.Text.Encoding.UTF8.GetBytes(string.Join('\u001f',
                            "Approved package", ReportStatus.Complete.ToString(), ContentClassification.Fci.ToString(),
                            "{}", "<main>Approved package</main>")))).ToLowerInvariant()
                }),
                ReminderAt = harness.Now.AddDays(23), CreatedAt = harness.Now
            });
            await db.SaveChangesAsync();
            return harness;
        }

        public async ValueTask DisposeAsync()
        {
            Db.ChangeTracker.Clear();
            await Db.PortalPackageReviewMessages.Where(item => item.TenantId == TenantId).ExecuteDeleteAsync();
            await Db.PortalPackageActivities.Where(item => item.TenantId == TenantId).ExecuteDeleteAsync();
            await Db.SharedPortalPackages.Where(item => item.TenantId == TenantId).ExecuteDeleteAsync();
            await Db.ExternalPortalAccessHistory.Where(item => item.TenantId == TenantId).ExecuteDeleteAsync();
            await Db.ExternalPortalInvitationPackageScopes.Where(item => item.TenantId == TenantId).ExecuteDeleteAsync();
            await Db.ExternalPortalInvitations.Where(item => item.TenantId == TenantId).ExecuteDeleteAsync();
            await Db.Set<ReportEvidenceEntity>().Where(item => item.ReportId == PackageId).ExecuteDeleteAsync();
            await Db.Reports.Where(item => item.TenantId == TenantId).ExecuteDeleteAsync();
            await Db.EvidenceItems.Where(item => item.TenantId == TenantId).ExecuteDeleteAsync();
            await Db.AuditLogEntries.Where(item => item.TenantId == TenantId).ExecuteDeleteAsync();
            await Db.Tenants.Where(item => item.Id == TenantId).ExecuteDeleteAsync();
            await Db.DisposeAsync();
            await _provider.DisposeAsync();
        }
    }
}
