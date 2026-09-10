using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Gccs.Application.Reports;
using Gccs.Application.Audit;
using Gccs.Application.Cmmc;
using Gccs.Application.Storage;
using Gccs.Api.Security;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Audit;
using Gccs.Domain.Identity;
using Gccs.Domain.Reports;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ReportPostgresTransactionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ReportPostgresTransactionTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Sprs_report_audit_failure_rolls_back_calculation_report_and_prior_audit_event()
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")
            ?? throw new InvalidOperationException("GCCS_TEST_POSTGRES_CONNECTION is required.");

        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();
        var controlId = $"AC.L2-{Guid.NewGuid():N}-3.1.1";
        await using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<GccsDbContext>();
                services.RemoveAll<DbContextOptions<GccsDbContext>>();
                services.RemoveAll<IAuditEventWriter>();
                services.RemoveAll<ISprsScoringRuleRepository>();
                services.AddDbContext<GccsDbContext>(options => options.UseGccsPostgres(connectionString));
                services.AddSingleton<ISprsScoringRuleRepository, ReviewedSprsRuleRepository>();
                services.AddScoped<IAuditEventWriter, FailOnSecondAuditEventWriter>();

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                PostgresTestDatabase.Migrate(dbContext);
                dbContext.Tenants.Add(new TenantEntity
                {
                    Id = tenantId,
                    Name = "Atomic SPRS report tenant",
                    Status = TenantStatus.Active,
                    DataPosture = TenantDataPosture.NoCui,
                    CreatedAt = DateTimeOffset.UtcNow
                });
                dbContext.Controls.Add(new ControlEntity
                {
                    Id = controlId,
                    Framework = ControlFramework.Cmmc,
                    CmmcLevel = CmmcLevel.Level2,
                    Family = "AC",
                    Title = "Authorized access",
                    Requirement = "Limit access.",
                    AssessmentObjective = "Assess access.",
                    SourceName = "NIST SP 800-171 Rev. 2",
                    SourceUrl = "https://csrc.nist.gov/pubs/sp/800/171/r2/upd1/final",
                    SourceLastReviewedAt = new DateOnly(2026, 9, 1),
                    SourceConfidence = "high"
                });
                dbContext.Assessments.Add(new AssessmentEntity
                {
                    Id = assessmentId,
                    TenantId = tenantId,
                    Name = "Atomic Level 2 assessment",
                    Type = AssessmentType.Readiness,
                    Level = CmmcLevel.Level2,
                    Framework = "NIST-SP-800-171-Rev2",
                    Status = AssessmentStatus.InProgress,
                    StartedAt = new DateOnly(2026, 9, 1),
                    OwnerFunction = "Security",
                    CreatedAt = DateTimeOffset.UtcNow
                });
                dbContext.ControlAssessments.Add(new ControlAssessmentEntity
                {
                    AssessmentId = assessmentId,
                    ControlId = controlId,
                    ImplementationStatus = ControlImplementationStatus.NotStarted,
                    Result = AssessmentResult.NotMet,
                    EvidenceItemIdsJson = "[]",
                    PoamItemIdsJson = "[]"
                });
                NoticeTestData.Seed(dbContext, actorUserId);
                dbContext.SaveChanges();
            });
        });

        try
        {
            using var client = factory.CreateClient();
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/reports/sprs-readiness?assessmentId={assessmentId}")
            {
                Content = JsonContent.Create(new SprsReadinessReportRequest(
                    "reviewed-rules",
                    null,
                    "Pending",
                    null,
                    new Gccs.Application.Common.ContentClassificationRequest(
                        Gccs.Domain.Common.ContentClassification.Unclassified)))
            };
            request.Headers.Add("X-Gccs-Dev-Auth", "true");
            request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
            request.Headers.Add("X-Gccs-Dev-User", actorUserId.ToString());
            request.Headers.Add("X-Gccs-Dev-Permissions", Permission.ManageReports.ToString());
            request.Headers.Add("Idempotency-Key", $"sprs-rollback-{Guid.NewGuid():N}");

            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Contains("audit_write_failed", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
            await using var verificationScope = factory.Services.CreateAsyncScope();
            var verificationDbContext = verificationScope.ServiceProvider.GetRequiredService<GccsDbContext>();
            Assert.False(await verificationDbContext.SprsScoreCalculations.AnyAsync(row => row.TenantId == tenantId));
            Assert.False(await verificationDbContext.SprsScoreCalculationNotes.AnyAsync(row => row.TenantId == tenantId));
            Assert.False(await verificationDbContext.Reports.AnyAsync(row => row.TenantId == tenantId));
            Assert.False(await verificationDbContext.ReportExports.AnyAsync(row => row.TenantId == tenantId));
            Assert.False(await verificationDbContext.ContentClassificationHistory.AnyAsync(row => row.TenantId == tenantId));
            Assert.False(await verificationDbContext.AuditLogEntries.AnyAsync(row => row.TenantId == tenantId));
        }
        finally
        {
            await using var cleanupScope = factory.Services.CreateAsyncScope();
            var cleanupDbContext = cleanupScope.ServiceProvider.GetRequiredService<GccsDbContext>();
            await cleanupDbContext.ReportExports
                .Where(row => row.TenantId == tenantId)
                .ExecuteDeleteAsync();
            await cleanupDbContext.ContentClassificationHistory
                .Where(row => row.TenantId == tenantId)
                .ExecuteDeleteAsync();
            await cleanupDbContext.Reports
                .Where(row => row.TenantId == tenantId)
                .ExecuteDeleteAsync();
            await cleanupDbContext.SprsScoreCalculationNotes
                .Where(row => row.TenantId == tenantId)
                .ExecuteDeleteAsync();
            await cleanupDbContext.SprsScoreCalculations
                .Where(row => row.TenantId == tenantId)
                .ExecuteDeleteAsync();
            await cleanupDbContext.AuditLogEntries
                .Where(row => row.TenantId == tenantId)
                .ExecuteDeleteAsync();
            await cleanupDbContext.DataHandlingNoticeAcknowledgements
                .Where(row => row.TenantId == tenantId)
                .ExecuteDeleteAsync();
            await cleanupDbContext.ControlAssessments
                .Where(row => row.AssessmentId == assessmentId)
                .ExecuteDeleteAsync();
            await cleanupDbContext.Assessments
                .Where(row => row.Id == assessmentId)
                .ExecuteDeleteAsync();
            await cleanupDbContext.Controls
                .Where(row => row.Id == controlId)
                .ExecuteDeleteAsync();
            await cleanupDbContext.Tenants
                .Where(row => row.Id == tenantId)
                .ExecuteDeleteAsync();
        }
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Concurrent_sprs_requests_with_one_idempotency_key_create_one_immutable_report()
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")
            ?? throw new InvalidOperationException("GCCS_TEST_POSTGRES_CONNECTION is required.");
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();
        var controlId = $"AC.L2-{Guid.NewGuid():N}-3.1.1";
        await using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<GccsDbContext>();
                services.RemoveAll<DbContextOptions<GccsDbContext>>();
                services.RemoveAll<ISprsScoringRuleRepository>();
                services.AddDbContext<GccsDbContext>(options => options.UseGccsPostgres(connectionString));
                services.AddSingleton<ISprsScoringRuleRepository, ReviewedSprsRuleRepository>();

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                PostgresTestDatabase.Migrate(dbContext);
                dbContext.Tenants.Add(new TenantEntity
                {
                    Id = tenantId,
                    Name = "Concurrent SPRS report tenant",
                    Status = TenantStatus.Active,
                    DataPosture = TenantDataPosture.NoCui,
                    CreatedAt = DateTimeOffset.UtcNow
                });
                dbContext.Controls.Add(new ControlEntity
                {
                    Id = controlId,
                    Framework = ControlFramework.Cmmc,
                    CmmcLevel = CmmcLevel.Level2,
                    Family = "AC",
                    Title = "Authorized access",
                    Requirement = "Limit access.",
                    AssessmentObjective = "Assess access.",
                    SourceName = "NIST SP 800-171 Rev. 2",
                    SourceUrl = "https://csrc.nist.gov/pubs/sp/800/171/r2/upd1/final",
                    SourceLastReviewedAt = new DateOnly(2026, 9, 1),
                    SourceConfidence = "high"
                });
                dbContext.Assessments.Add(new AssessmentEntity
                {
                    Id = assessmentId,
                    TenantId = tenantId,
                    Name = "Concurrent Level 2 assessment",
                    Type = AssessmentType.Readiness,
                    Level = CmmcLevel.Level2,
                    Framework = "NIST-SP-800-171-Rev2",
                    Status = AssessmentStatus.InProgress,
                    StartedAt = new DateOnly(2026, 9, 1),
                    OwnerFunction = "Security",
                    CreatedAt = DateTimeOffset.UtcNow
                });
                dbContext.ControlAssessments.Add(new ControlAssessmentEntity
                {
                    AssessmentId = assessmentId,
                    ControlId = controlId,
                    ImplementationStatus = ControlImplementationStatus.NotStarted,
                    Result = AssessmentResult.NotMet,
                    EvidenceItemIdsJson = "[]",
                    PoamItemIdsJson = "[]"
                });
                NoticeTestData.Seed(dbContext, actorUserId);
                dbContext.SaveChanges();
            });
        });

        try
        {
            using var firstClient = factory.CreateClient();
            using var secondClient = factory.CreateClient();
            var idempotencyKey = $"sprs-concurrent-{Guid.NewGuid():N}";
            using var firstRequest = CreateSprsRequest(assessmentId, tenantId, actorUserId, idempotencyKey);
            using var secondRequest = CreateSprsRequest(assessmentId, tenantId, actorUserId, idempotencyKey);

            var responses = await Task.WhenAll(
                firstClient.SendAsync(firstRequest),
                secondClient.SendAsync(secondRequest));
            using var firstResponse = responses[0];
            using var secondResponse = responses[1];

            Assert.All(responses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));
            using var firstDocument = JsonDocument.Parse(await firstResponse.Content.ReadAsStringAsync());
            using var secondDocument = JsonDocument.Parse(await secondResponse.Content.ReadAsStringAsync());
            var firstJson = firstDocument.RootElement;
            var secondJson = secondDocument.RootElement;
            Assert.Equal(firstJson.GetProperty("id").GetGuid(), secondJson.GetProperty("id").GetGuid());
            Assert.Equal(
                [false, true],
                new[] { firstJson.GetProperty("isReplay").GetBoolean(), secondJson.GetProperty("isReplay").GetBoolean() }
                    .Order()
                    .ToArray());

            await using var verificationScope = factory.Services.CreateAsyncScope();
            var db = verificationScope.ServiceProvider.GetRequiredService<GccsDbContext>();
            Assert.Single(await db.Reports.Where(row => row.TenantId == tenantId).ToArrayAsync());
            Assert.Single(await db.SprsScoreCalculations.Where(row => row.TenantId == tenantId).ToArrayAsync());
            Assert.Equal(2, await db.AuditLogEntries.CountAsync(row =>
                row.TenantId == tenantId &&
                (row.EntityType == "Report" || row.EntityType == "SprsScoreCalculation")));
        }
        finally
        {
            await using var cleanupScope = factory.Services.CreateAsyncScope();
            var db = cleanupScope.ServiceProvider.GetRequiredService<GccsDbContext>();
            await db.ContentClassificationHistory.Where(row => row.TenantId == tenantId).ExecuteDeleteAsync();
            await db.Reports.Where(row => row.TenantId == tenantId).ExecuteDeleteAsync();
            await db.SprsScoreCalculationNotes.Where(row => row.TenantId == tenantId).ExecuteDeleteAsync();
            await db.SprsScoreCalculations.Where(row => row.TenantId == tenantId).ExecuteDeleteAsync();
            await db.AuditLogEntries.Where(row => row.TenantId == tenantId).ExecuteDeleteAsync();
            await db.DataHandlingNoticeAcknowledgements.Where(row => row.TenantId == tenantId).ExecuteDeleteAsync();
            await db.ControlAssessments.Where(row => row.AssessmentId == assessmentId).ExecuteDeleteAsync();
            await db.Assessments.Where(row => row.Id == assessmentId).ExecuteDeleteAsync();
            await db.Controls.Where(row => row.Id == controlId).ExecuteDeleteAsync();
            await db.Tenants.Where(row => row.Id == tenantId).ExecuteDeleteAsync();
        }
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Audit_failure_rolls_back_report_generation_and_archive_lifecycle_changes()
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")
            ?? throw new InvalidOperationException("GCCS_TEST_POSTGRES_CONNECTION is required.");

        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var existingReportId = Guid.NewGuid();
        await using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<GccsDbContext>();
                services.RemoveAll<DbContextOptions<GccsDbContext>>();
                services.RemoveAll<IAuditEventWriter>();
                services.RemoveAll<IObjectStorageService>();
                services.AddDbContext<GccsDbContext>(options => options.UseGccsPostgres(connectionString));
                services.AddScoped<IAuditEventWriter, FailingAuditEventWriter>();
                services.AddSingleton<TestObjectStorageService>();
                services.AddSingleton<IObjectStorageService>(provider =>
                    provider.GetRequiredService<TestObjectStorageService>());

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                PostgresTestDatabase.Migrate(dbContext);
                dbContext.Tenants.Add(new TenantEntity
                {
                    Id = tenantId,
                    Name = "Atomic report tenant",
                    Status = TenantStatus.Active,
                    DataPosture = TenantDataPosture.NoCui,
                    CreatedAt = DateTimeOffset.UtcNow
                });
                dbContext.Reports.Add(new ReportEntity
                {
                    Id = existingReportId,
                    TenantId = tenantId,
                    Type = ReportType.SubcontractorCompliance,
                    Title = "Existing immutable report",
                    Status = ReportStatus.Complete,
                    GeneratedAt = DateTimeOffset.UtcNow,
                    GeneratedByUserId = Guid.NewGuid(),
                    SnapshotJson = "{}",
                    CreatedAt = DateTimeOffset.UtcNow
                });
                NoticeTestData.Seed(dbContext, actorUserId);
                dbContext.SaveChanges();
            });
        });

        try
        {
            using var client = factory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/reports/compliance-status");
            request.Headers.Add("X-Gccs-Dev-Auth", "true");
            request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
            request.Headers.Add("X-Gccs-Dev-User", actorUserId.ToString());
            request.Headers.Add("X-Gccs-Dev-Permissions", Permission.ManageReports.ToString());

            ClassifiedWorkflowTestData.Confirm(request);
            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Contains("audit_write_failed", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
            using var verificationScope = factory.Services.CreateScope();
            var verificationDbContext = verificationScope.ServiceProvider.GetRequiredService<GccsDbContext>();
            Assert.False(await verificationDbContext.Reports.AnyAsync(report =>
                report.TenantId == tenantId &&
                report.Type == ReportType.ComplianceStatus));
            Assert.False(await verificationDbContext.AuditLogEntries.AnyAsync(audit => audit.TenantId == tenantId));

            using var archiveRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/reports/{existingReportId}/archive")
            {
                Content = JsonContent.Create(new ReportLifecycleRequest("Synthetic rollback verification."))
            };
            archiveRequest.Headers.Add("X-Gccs-Dev-Auth", "true");
            archiveRequest.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
            archiveRequest.Headers.Add("X-Gccs-Dev-User", actorUserId.ToString());
            archiveRequest.Headers.Add("X-Gccs-Dev-Permissions", Permission.ArchiveReports.ToString());

            using var archiveResponse = await client.SendAsync(archiveRequest);

            Assert.Equal(HttpStatusCode.InternalServerError, archiveResponse.StatusCode);
            Assert.Contains("audit_write_failed", await archiveResponse.Content.ReadAsStringAsync(), StringComparison.Ordinal);
            verificationDbContext.ChangeTracker.Clear();
            var unchangedReport = await verificationDbContext.Reports.SingleAsync(report => report.Id == existingReportId);
            Assert.Equal(ReportStatus.Complete, unchangedReport.Status);
            Assert.Null(unchangedReport.ArchivedAt);
            Assert.Null(unchangedReport.ArchivedByUserId);
            Assert.Null(unchangedReport.ArchiveReason);
            Assert.False(await verificationDbContext.AuditLogEntries.AnyAsync(audit => audit.TenantId == tenantId));

            using var exportRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/reports/{existingReportId}/exports/pdf");
            exportRequest.Headers.Add("X-Gccs-Dev-Auth", "true");
            exportRequest.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
            exportRequest.Headers.Add("X-Gccs-Dev-User", actorUserId.ToString());
            exportRequest.Headers.Add("X-Gccs-Dev-Permissions", Permission.ExportReports.ToString());

            using var exportResponse = await client.SendAsync(exportRequest);

            Assert.Equal(HttpStatusCode.InternalServerError, exportResponse.StatusCode);
            Assert.Contains("audit_write_failed", await exportResponse.Content.ReadAsStringAsync(), StringComparison.Ordinal);
            verificationDbContext.ChangeTracker.Clear();
            Assert.False(await verificationDbContext.ReportExports.AnyAsync(export => export.TenantId == tenantId));
            Assert.False(await verificationDbContext.AuditLogEntries.AnyAsync(audit => audit.TenantId == tenantId));

            var processingActorId = actorUserId;
            var processingExportId = Guid.NewGuid();
            var processingLeaseId = Guid.NewGuid();
            verificationDbContext.ReportExports.Add(new ReportExportEntity
            {
                Id = processingExportId,
                TenantId = tenantId,
                ReportId = existingReportId,
                Format = "pdf",
                RenderVersion = ReportExportConstants.RenderVersion,
                Status = ReportExportStatus.Processing,
                ObjectName = ReportExportService.BuildPendingObjectName(existingReportId, processingExportId),
                FileName = "atomic-report.pdf",
                ContentType = "application/pdf",
                RequestedByUserId = processingActorId,
                RequestedAt = DateTimeOffset.UtcNow,
                ProcessingAttemptCount = 1,
                ProcessingLeaseId = processingLeaseId,
                ProcessingLeaseUntil = DateTimeOffset.UtcNow.AddMinutes(10),
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedByUserId = processingActorId
            });
            await verificationDbContext.SaveChangesAsync();

            using (var processingScope = factory.Services.CreateScope())
            {
                processingScope.ServiceProvider.GetRequiredService<HttpTenantContext>().InitializeBackground(
                    tenantId,
                    processingActorId,
                    "owner@example.test");
                var claimed = new ClaimedReportExport(
                    processingExportId,
                    tenantId,
                    existingReportId,
                    processingActorId,
                    "owner@example.test",
                    processingLeaseId,
                    1);
                await Assert.ThrowsAsync<AuditWriteException>(() =>
                    processingScope.ServiceProvider.GetRequiredService<ReportExportService>().ProcessAsync(claimed));
            }

            Assert.Equal(0, factory.Services.GetRequiredService<TestObjectStorageService>().Count);
            verificationDbContext.ChangeTracker.Clear();
            var unchangedExport = await verificationDbContext.ReportExports.SingleAsync(export => export.Id == processingExportId);
            Assert.Equal(ReportExportStatus.Processing, unchangedExport.Status);
            Assert.Equal(
                ReportExportService.BuildPendingObjectName(existingReportId, processingExportId),
                unchangedExport.ObjectName);
            Assert.False(await verificationDbContext.AuditLogEntries.AnyAsync(audit => audit.TenantId == tenantId));
        }
        finally
        {
            using var cleanupScope = factory.Services.CreateScope();
            var cleanupDbContext = cleanupScope.ServiceProvider.GetRequiredService<GccsDbContext>();
            await cleanupDbContext.DataHandlingNoticeAcknowledgements.Where(a => a.TenantId == tenantId).ExecuteDeleteAsync();
            var reports = await cleanupDbContext.Reports
                .Where(candidate => candidate.TenantId == tenantId)
                .ToArrayAsync();
            cleanupDbContext.Reports.RemoveRange(reports);
            var tenant = await cleanupDbContext.Tenants.SingleOrDefaultAsync(candidate => candidate.Id == tenantId);
            if (tenant is not null)
            {
                cleanupDbContext.Tenants.Remove(tenant);
            }

            await cleanupDbContext.SaveChangesAsync();
        }
    }

    private sealed class FailingAuditEventWriter : IAuditEventWriter
    {
        public Task WriteAsync(
            Guid tenantId,
            Guid actorUserId,
            AuditAction action,
            string entityType,
            string entityId,
            string summary,
            IReadOnlyDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default) =>
            throw new AuditWriteException("Synthetic audit persistence failure.");
    }

    private static HttpRequestMessage CreateSprsRequest(
        Guid assessmentId,
        Guid tenantId,
        Guid actorUserId,
        string idempotencyKey)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/reports/sprs-readiness?assessmentId={assessmentId}")
        {
            Content = JsonContent.Create(new SprsReadinessReportRequest(
                "reviewed-rules",
                null,
                "Pending",
                null,
                new Gccs.Application.Common.ContentClassificationRequest(
                    Gccs.Domain.Common.ContentClassification.Unclassified)))
        };
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", actorUserId.ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", Permission.ManageReports.ToString());
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return request;
    }

    private sealed class FailOnSecondAuditEventWriter(
        GccsDbContext dbContext,
        IAuditRequestMetadata requestMetadata) : IAuditEventWriter
    {
        private readonly EfAuditEventWriter _inner = new(dbContext, requestMetadata);
        private int _writeCount;

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
            if (Interlocked.Increment(ref _writeCount) == 2)
            {
                throw new AuditWriteException("Synthetic SPRS report audit persistence failure.");
            }

            return _inner.WriteAsync(
                tenantId,
                actorUserId,
                action,
                entityType,
                entityId,
                summary,
                metadata,
                cancellationToken);
        }
    }

    private sealed class ReviewedSprsRuleRepository : ISprsScoringRuleRepository
    {
        private static readonly SprsScoringRuleSetDto RuleSet = new(
            "reviewed-rules",
            "2026.09-reviewed",
            SprsScoringRuleSetState.Published,
            "Reviewed methodology fixture",
            "https://example.test/reviewed-sprs-methodology",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 9, 1),
            "Content owner",
            "Qualified reviewer",
            new DateOnly(2026, 9, 1),
            110,
            [new SprsScoringRuleDto(
                "3.1.1",
                "Authorized access",
                5,
                "Subtract five points when not met.",
                "https://example.test/reviewed-sprs-methodology")],
            1,
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

        public Task<IReadOnlyList<SprsScoringRuleSetDto>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SprsScoringRuleSetDto>>([RuleSet]);

        public Task<SprsScoringRuleSetDto?> FindAsync(
            string ruleSetId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<SprsScoringRuleSetDto?>(ruleSetId == RuleSet.Id ? RuleSet : null);
    }
}
