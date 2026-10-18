using Gccs.Application.Marketing;
using Gccs.Application.Security;
using Gccs.Domain.Companies;
using Gccs.Domain.Contracts;
using Gccs.Domain.Identity;
using Gccs.Domain.Reports;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Contracts;
using Gccs.Infrastructure.Identity;
using Gccs.Infrastructure.Marketing;
using Gccs.Infrastructure.Notifications;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Reports;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class Story397QueueClaimPostgresTests
{
    private static readonly Lazy<Task<DbContextOptions<GccsDbContext>>> Database = new(CreateDatabaseAsync);
    private static readonly TimeSpan Lease = TimeSpan.FromMinutes(5);

    [PostgresFact]
    public async Task Extraction_SQL_claims_are_exclusive_and_expired_leases_are_replaced()
    {
        var options = await Database.Value;
        var now = DateTimeOffset.UtcNow;
        var rows = await SeedAsync(options, now, "Extraction");
        await using var a = new GccsDbContext(options);
        await using var b = new GccsDbContext(options);
        var ra = new EfExtractionJobWorkRepository(a);
        var rb = new EfExtractionJobWorkRepository(b);
        var claims = await Task.WhenAll(ra.TryClaimNextAsync(now, Lease, 3), rb.TryClaimNextAsync(now, Lease, 3));
        Assert.All(claims, claim => Assert.NotNull(claim));
        Assert.Equal(2, claims.Select(c => c!.JobId).Distinct().Count());
        foreach (var claim in claims)
        {
            var expected = rows.Single(r => r.Extraction == claim!.JobId);
            Assert.Equal(expected.Tenant, claim!.TenantId);
            Assert.Equal(expected.User, claim.RequestedByUserId);
            Assert.Equal(expected.Email, claim.RequestedByEmail);
            var persisted = await a.Set<ExtractionJobEntity>().AsNoTracking().SingleAsync(x => x.Id == claim.JobId);
            Assert.Equal(ExtractionJobStatus.Processing, persisted.Status);
            Assert.Equal(claim.LeaseId, persisted.ProcessingLeaseId);
            Assert.Equal(1, persisted.ProcessingAttemptCount);
        }
        Assert.Null(await ra.TryClaimNextAsync(now, Lease, 3));
        var reclaimed = await ra.TryClaimNextAsync(now.Add(Lease).AddSeconds(1), Lease, 3);
        Assert.NotNull(reclaimed);
        Assert.Equal(2, reclaimed!.AttemptNumber);
        Assert.NotEqual(claims.Single(c => c!.JobId == reclaimed.JobId)!.LeaseId, reclaimed.LeaseId);
    }

    [PostgresFact]
    public async Task Report_SQL_claims_preserve_tenant_and_reject_wrong_tenant_and_stale_lease_completion()
    {
        var options = await Database.Value;
        var now = DateTimeOffset.UtcNow;
        var rows = await SeedAsync(options, now, "Report");
        await using var a = new GccsDbContext(options);
        await using var b = new GccsDbContext(options);
        var ra = new EfReportExportRepository(a, new TenantContext(rows[0].Tenant, rows[0].User));
        var rb = new EfReportExportRepository(b, new TenantContext(rows[1].Tenant, rows[1].User));
        var claims = await Task.WhenAll(ra.TryClaimNextAsync(now, Lease, 3), rb.TryClaimNextAsync(now, Lease, 3));
        Assert.All(claims, claim => Assert.NotNull(claim));
        Assert.Equal(2, claims.Select(c => c!.ExportId).Distinct().Count());
        Assert.Null(await ra.TryClaimNextAsync(now, Lease, 3));
        var reclaimAt = now.Add(Lease).AddSeconds(1);
        var replacements = await Task.WhenAll(ra.TryClaimNextAsync(reclaimAt, Lease, 3), rb.TryClaimNextAsync(reclaimAt, Lease, 3));
        Assert.All(replacements, claim => Assert.NotNull(claim));
        Assert.Equal(2, replacements.Select(c => c!.ExportId).Distinct().Count());
        foreach (var claim in claims)
        {
            var row = rows.Single(r => r.Export == claim!.ExportId);
            var replacement = replacements.Single(c => c!.ExportId == claim!.ExportId)!;
            Assert.NotEqual(claim!.LeaseId, replacement.LeaseId);
            Assert.Equal(row.Tenant, claim!.TenantId);
            Assert.Equal(row.Email, claim.RequestedByEmail);
            await using var completion = new GccsDbContext(options);
            var wrong = new EfReportExportRepository(completion, new TenantContext(rows.Single(r => r.Tenant != row.Tenant).Tenant, row.User));
            Assert.Null(await wrong.MarkFailedAsync(claim.ExportId, replacement.LeaseId, "synthetic_failure"));
            var correct = new EfReportExportRepository(completion, new TenantContext(row.Tenant, row.User));
            Assert.Null(await correct.MarkFailedAsync(claim.ExportId, claim.LeaseId, "synthetic_failure"));
            var untouched = await completion.ReportExports.AsNoTracking().SingleAsync(x => x.Id == claim.ExportId);
            Assert.Equal(ReportExportStatus.Processing, untouched.Status);
            Assert.Equal(replacement.LeaseId, untouched.ProcessingLeaseId);
            Assert.Equal(2, untouched.ProcessingAttemptCount);
            Assert.Equal(ReportExportStatus.Failed, (await correct.MarkFailedAsync(claim.ExportId, replacement.LeaseId, "synthetic_failure"))!.Status);
            Assert.Null(await correct.MarkFailedAsync(claim.ExportId, replacement.LeaseId, "repeated"));
            Assert.Equal("synthetic_failure", (await completion.ReportExports.AsNoTracking().SingleAsync(x => x.Id == claim.ExportId)).FailureCode);
        }
        Assert.Null(await ra.TryClaimNextAsync(now, Lease, 3));
    }

    [PostgresFact]
    public async Task Invitation_SQL_claims_are_exclusive_and_persist_retry_sent_and_audits()
    {
        var options = await Database.Value;
        var now = DateTimeOffset.UtcNow;
        var rows = await SeedAsync(options, now, "Invitation");
        await using var a = new GccsDbContext(options);
        await using var b = new GccsDbContext(options);
        var ra = new EfInvitationDeliveryRepository(a);
        var rb = new EfInvitationDeliveryRepository(b);
        var claims = await Task.WhenAll(ra.TryClaimNextAsync(now, Lease), rb.TryClaimNextAsync(now, Lease));
        Assert.All(claims, claim => Assert.NotNull(claim));
        Assert.Equal(2, claims.Select(c => c!.InvitationId).Distinct().Count());
        foreach (var claim in claims)
        {
            var row = rows.Single(r => r.Invitation == claim!.InvitationId);
            Assert.Equal(row.Tenant, claim!.TenantId);
            Assert.Equal(row.Email, claim.RecipientEmail);
            Assert.Equal(1, claim.AttemptNumber);
            var persisted = await a.TenantInvitations.AsNoTracking().SingleAsync(x => x.Id == claim.InvitationId);
            Assert.Equal(InvitationDeliveryStatus.Processing, persisted.DeliveryStatus);
            Assert.True(persisted.DeliveryLeaseUntil > now);
        }
        Assert.Null(await ra.TryClaimNextAsync(now, Lease));
        await ra.MarkFailedAsync(claims[0]!.InvitationId, "synthetic_failure", now, now.AddMinutes(1));
        Assert.Null(await ra.TryClaimNextAsync(now, Lease));
        var retried = await ra.TryClaimNextAsync(now.AddMinutes(2), Lease);
        Assert.Equal(claims[0]!.InvitationId, retried!.InvitationId);
        Assert.Equal(2, retried.AttemptNumber);
        a.ChangeTracker.Clear();
        await ra.MarkSentAsync(retried.InvitationId, "synthetic-provider", now.AddMinutes(2));
        await ra.MarkSentAsync(retried.InvitationId, "repeated-provider", now.AddMinutes(2));
        var sent = await a.TenantInvitations.AsNoTracking().SingleAsync(x => x.Id == retried.InvitationId);
        Assert.Equal(InvitationDeliveryStatus.Sent, sent.DeliveryStatus);
        Assert.Equal("synthetic-provider", sent.DeliveryProviderMessageId);
        Assert.Null(sent.DeliveryLeaseUntil);
        Assert.Equal(2, await a.AuditLogEntries.CountAsync(x => x.EntityType == "TenantInvitationDelivery" && x.EntityId == retried.InvitationId.ToString()));
    }

    [PostgresFact]
    public async Task Assignment_SQL_claims_are_exclusive_and_persist_sent_and_single_audit()
    {
        var options = await Database.Value;
        var now = DateTimeOffset.UtcNow;
        var rows = await SeedAsync(options, now, "Assignment");
        await using var a = new GccsDbContext(options);
        await using var b = new GccsDbContext(options);
        var ra = new EfAssignmentEmailDeliveryRepository(a);
        var rb = new EfAssignmentEmailDeliveryRepository(b);
        var claims = await Task.WhenAll(ra.TryClaimNextAsync(now, Lease), rb.TryClaimNextAsync(now, Lease));
        Assert.All(claims, claim => Assert.NotNull(claim));
        Assert.Equal(2, claims.Select(c => c!.DeliveryId).Distinct().Count());
        foreach (var claim in claims)
        {
            var row = rows.Single(r => r.Assignment == claim!.DeliveryId);
            Assert.Equal(row.Tenant, claim!.TenantId);
            Assert.Equal(row.User, claim.UserId);
            Assert.Equal(row.Email, claim.RecipientEmail);
            Assert.Equal(1, claim.AttemptNumber);
            var persisted = await a.AssignmentEmailDeliveries.AsNoTracking().SingleAsync(x => x.Id == claim.DeliveryId);
            Assert.Equal("Processing", persisted.Status);
            Assert.True(persisted.LeaseUntil > now);
            await ra.MarkSentAsync(claim.DeliveryId, "synthetic-provider", now);
            await ra.MarkSentAsync(claim.DeliveryId, "repeated-provider", now);
            var sent = await a.AssignmentEmailDeliveries.AsNoTracking().SingleAsync(x => x.Id == claim.DeliveryId);
            Assert.Equal("Sent", sent.Status);
            Assert.Equal("synthetic-provider", sent.ProviderMessageId);
            Assert.Null(sent.LeaseUntil);
            Assert.Equal(1, await a.AuditLogEntries.CountAsync(x => x.EntityType == "AssignmentEmailDelivery" && x.EntityId == claim.DeliveryId.ToString()));
        }
        Assert.Null(await ra.TryClaimNextAsync(now, Lease));
    }

    [PostgresFact]
    public async Task Demo_SQL_claims_are_exclusive_and_preserve_request_ownership_and_completion()
    {
        var options = await Database.Value;
        var now = DateTimeOffset.UtcNow;
        var rows = await SeedAsync(options, now, "Demo");
        await using var a = new GccsDbContext(options);
        await using var b = new GccsDbContext(options);
        var ra = new EfDemoRequestRepository(a);
        var rb = new EfDemoRequestRepository(b);
        var claims = await Task.WhenAll(ra.TryClaimNextDeliveryAsync(now, Lease), rb.TryClaimNextDeliveryAsync(now, Lease));
        Assert.All(claims, claim => Assert.NotNull(claim));
        Assert.Equal(2, claims.Select(c => c!.DeliveryId).Distinct().Count());
        foreach (var claim in claims)
        {
            var row = rows.Single(r => r.DemoDelivery == claim!.DeliveryId);
            Assert.Equal(row.DemoRequest, claim!.RequestId);
            Assert.Equal(row.Email, claim.Email);
            Assert.Equal(1, claim.AttemptNumber);
            var processing = await a.DemoRequestDeliveries.AsNoTracking().SingleAsync(x => x.Id == claim.DeliveryId);
            Assert.Equal("Processing", processing.Status);
            Assert.True(processing.LeaseUntil > now);
            await ra.MarkDeliveryCompletedAsync(claim.DeliveryId, new(DemoRequestDeliveryDisposition.Captured), now);
            await ra.MarkDeliveryFailedAsync(claim.DeliveryId, "repeated", now, null);
            var completed = await a.DemoRequestDeliveries.AsNoTracking().SingleAsync(x => x.Id == claim.DeliveryId);
            Assert.Equal("Captured", completed.Status);
            Assert.Null(completed.LeaseUntil);
            Assert.Null(completed.FailureCode);
        }
        Assert.Null(await ra.TryClaimNextDeliveryAsync(now, Lease));
    }

    private static async Task<DbContextOptions<GccsDbContext>> CreateDatabaseAsync()
    {
        var builder = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")!);
        var name = "story397_claims_" + Guid.NewGuid().ToString("N");
        await using (var admin = new NpgsqlConnection(builder.ConnectionString))
        {
            await admin.OpenAsync();
            await using var command = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", admin);
            await command.ExecuteNonQueryAsync();
        }
        builder.Database = name;
        var options = new DbContextOptionsBuilder<GccsDbContext>().UseGccsPostgres(builder.ConnectionString).Options;
        await using var db = new GccsDbContext(options);
        // Retained empty database uses the current model; this does not test migration history.
        await db.Database.EnsureCreatedAsync();
        return options;
    }

    private static async Task<Row[]> SeedAsync(DbContextOptions<GccsDbContext> options, DateTimeOffset now, string kind)
    {
        await using var db = new GccsDbContext(options);
        var rows = Enumerable.Range(0, 2).Select(_ => new Row()).ToArray();
        // Each case has its own table population; terminalize no unrelated queue rows.
        foreach (var row in rows)
        {
            db.Tenants.Add(new TenantEntity { Id = row.Tenant, Name = "Synthetic queue tenant", Status = TenantStatus.Active, DataPosture = TenantDataPosture.NoCui, CreatedAt = now });
            db.Users.Add(new UserEntity { Id = row.User, TenantId = row.Tenant, PreferredTenantId = row.Tenant, Email = row.Email, DisplayName = "Synthetic worker", Status = UserStatus.Active, CreatedAt = now });
            if (kind.Equals("Extraction"))
            {
                var contract = new ContractEntity { Id = Guid.NewGuid(), TenantId = row.Tenant, ContractNumber = "SYNTHETIC-" + row.Tenant.ToString("N"), Title = "Synthetic", AgencyOrPrimeName = "Synthetic", Relationship = ContractorRelationship.Prime, Kind = ContractKind.FixedPrice, Status = ContractStatus.Active, PeriodOfPerformanceStart = new(2026, 1, 1), PeriodOfPerformanceEnd = new(2026, 12, 31), PlaceOfPerformance = "Synthetic", Description = "Synthetic No-CUI", DataHandlingPosture = DataHandlingPosture.FciOnly, CreatedAt = now };
                var document = new ContractDocumentEntity { Id = Guid.NewGuid(), ContractId = contract.Id, FileName = "synthetic.txt", ContentType = "text/plain", ValidationStatus = "Accepted", MalwareScanStatus = "Clean", NoticeVersion = "synthetic", UploadedAt = now, UploadedByUserId = row.User };
                db.Contracts.Add(contract); db.Set<ContractDocumentEntity>().Add(document);
                db.Set<ExtractionJobEntity>().Add(new ExtractionJobEntity { Id = row.Extraction, TenantId = row.Tenant, SourceDocumentId = document.Id, RequestedByUserId = row.User, Status = ExtractionJobStatus.Queued, RequestedAt = now });
            }
            if (kind.Equals("Report"))
            {
                var report = new ReportEntity { Id = Guid.NewGuid(), TenantId = row.Tenant, Type = ReportType.ComplianceStatus, Title = "Synthetic", Status = ReportStatus.Complete, GeneratedAt = now, GeneratedByUserId = row.User, CreatedAt = now };
                db.Reports.Add(report);
                db.ReportExports.Add(new ReportExportEntity { Id = row.Export, TenantId = row.Tenant, ReportId = report.Id, Status = ReportExportStatus.Queued, RequestedAt = now, RequestedByUserId = row.User, CreatedAt = now });
            }
            if (kind.Equals("Invitation")) db.TenantInvitations.Add(new TenantInvitationEntity { Id = row.Invitation, TenantId = row.Tenant, Email = row.Email, RoleName = "Viewer", Status = TenantInvitationStatus.Pending, DeliveryStatus = InvitationDeliveryStatus.Queued, ExpiresAt = now.AddDays(1), CreatedAt = now });
            if (kind.Equals("Assignment"))
            {
                var notification = new NotificationDeliveryEntity { Id = Guid.NewGuid(), TenantId = row.Tenant, UserId = row.User, SourceTaskId = Guid.NewGuid(), SourceType = "Synthetic", Category = "Synthetic", Status = "Sent", CreatedAt = now, AttemptedAt = now };
                db.NotificationDeliveries.Add(notification);
                db.AssignmentEmailDeliveries.Add(new AssignmentEmailDeliveryEntity { Id = row.Assignment, TenantId = row.Tenant, UserId = row.User, NotificationDeliveryId = notification.Id, RecipientEmail = row.Email, RecipientDisplayName = "Synthetic", LinkUrl = "/synthetic", Status = "Queued", CreatedAt = now });
            }
            if (kind.Equals("Demo"))
            {
                db.DemoRequests.Add(new DemoRequestEntity { Id = row.DemoRequest, FirstName = "Synthetic", LastName = "Synthetic", Company = "Synthetic", Email = row.Email, DeduplicationKey = row.DemoRequest.ToString("N"), ReceivedAt = now });
                db.DemoRequestDeliveries.Add(new DemoRequestDeliveryEntity { Id = row.DemoDelivery, DemoRequestId = row.DemoRequest, Status = "Queued", CreatedAt = now, UpdatedAt = now });
            }
        }
        await db.SaveChangesAsync();
        return rows;
    }

    private sealed class Row
    {
        public Guid Tenant { get; } = Guid.NewGuid();
        public Guid User { get; } = Guid.NewGuid();
        public Guid Extraction { get; } = Guid.NewGuid();
        public Guid Export { get; } = Guid.NewGuid();
        public Guid Invitation { get; } = Guid.NewGuid();
        public Guid Assignment { get; } = Guid.NewGuid();
        public Guid DemoRequest { get; } = Guid.NewGuid();
        public Guid DemoDelivery { get; } = Guid.NewGuid();
        public string Email => $"synthetic-{User:N}@example.invalid";
    }

    private sealed class TenantContext(Guid tenant, Guid user) : ICurrentTenantContext
    {
        public Guid TenantId => tenant;
        public Guid UserId => user;
        public string UserEmail => "synthetic@example.invalid";
    }
}
