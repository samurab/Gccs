using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Cmmc;
using Gccs.Application.Reports;
using Gccs.Application.Security;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Common;
using Gccs.Domain.Identity;
using Gccs.Domain.Reports;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Cmmc;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Reports;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class SprsReadinessReportTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> factory;

    public SprsReadinessReportTests(WebApplicationFactory<Program> factory) => this.factory = factory;

    [Fact]
    public async Task TC_30_3_1_2_5_Report_contains_required_context_disclaimer_and_audit()
    {
        var ids = StoryIds.Create();
        await using var storyFactory = CreateFactory("sprs-report-content", db => Seed(db, ids.TenantId, ids.AssessmentId));
        using var client = storyFactory.CreateClient();

        var report = await GenerateAsync(client, ids.TenantId, ids.ActorUserId, ids.AssessmentId);

        Assert.Equal(ReportType.SprsReadiness, report.Type);
        Assert.Equal(105, report.Snapshot.Score);
        Assert.Equal(110, report.Snapshot.MaximumScore);
        Assert.Equal(5, report.Snapshot.TotalDeduction);
        Assert.Equal("2026.09-reviewed", report.Snapshot.RuleSetVersion);
        Assert.Equal("Reviewed", report.Snapshot.LeadershipReviewStatus);
        Assert.NotEqual(default, report.Snapshot.GeneratedAt);
        Assert.Single(report.Snapshot.Deductions);
        var unresolved = Assert.Single(report.Snapshot.UnresolvedControls);
        Assert.Equal("Linked", unresolved.EvidenceStatus);
        Assert.Single(unresolved.PoamItemIds);
        Assert.Contains("has not submitted", report.Disclaimer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("has not submitted", report.Snapshot.SubmissionStatement, StringComparison.OrdinalIgnoreCase);

        await using var scope = storyFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Contains(await db.AuditLogEntries.Where(row => row.TenantId == ids.TenantId).ToArrayAsync(), row =>
            row.EntityType == "Report" &&
            row.EntityId == report.Id.ToString() &&
            row.MetadataJson.Contains("not-submitted", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TC_30_3_3_Report_is_tenant_scoped_for_generation_history_detail_and_export()
    {
        var ids = StoryIds.Create();
        await using var storyFactory = CreateFactory("sprs-report-tenant", db =>
        {
            Seed(db, ids.TenantId, ids.AssessmentId);
            Seed(db, ids.OtherTenantId, ids.OtherTenantAssessmentId);
        });
        using var client = storyFactory.CreateClient();
        var report = await GenerateAsync(client, ids.TenantId, ids.ActorUserId, ids.AssessmentId);

        using var crossTenantGenerate = Request(
            HttpMethod.Post,
            $"/api/reports/sprs-readiness?assessmentId={ids.OtherTenantAssessmentId}",
            ids.TenantId,
            ids.ActorUserId,
            [Permission.ManageReports],
            CreateBody());
        using var crossTenantResponse = await client.SendAsync(crossTenantGenerate);
        Assert.Equal(HttpStatusCode.NotFound, crossTenantResponse.StatusCode);

        using var historyRequest = Request<object?>(HttpMethod.Get, "/api/reports/recent", ids.TenantId, ids.ActorUserId, [Permission.ViewReports]);
        using var historyResponse = await client.SendAsync(historyRequest);
        var history = await historyResponse.Content.ReadFromJsonAsync<ReportHistoryItemDto[]>(JsonOptions);
        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        Assert.Single(history!);
        Assert.Equal(report.Id, history![0].Id);

        using var otherDetail = Request<object?>(HttpMethod.Get, $"/api/reports/{report.Id}", ids.OtherTenantId, ids.ActorUserId, [Permission.ViewReports]);
        using var otherDetailResponse = await client.SendAsync(otherDetail);
        Assert.Equal(HttpStatusCode.NotFound, otherDetailResponse.StatusCode);

        using var exportRequest = Request<object?>(HttpMethod.Post, $"/api/reports/{report.Id}/exports/pdf", ids.TenantId, ids.ActorUserId, [Permission.ExportReports]);
        using var exportResponse = await client.SendAsync(exportRequest);
        Assert.True(
            exportResponse.StatusCode == HttpStatusCode.Accepted,
            $"Expected Accepted, received {exportResponse.StatusCode}: {await exportResponse.Content.ReadAsStringAsync()}");
    }

    [Fact]
    public async Task TC_30_3_4_Report_permissions_are_server_authoritative()
    {
        var ids = StoryIds.Create();
        await using var storyFactory = CreateFactory("sprs-report-rbac", db => Seed(db, ids.TenantId, ids.AssessmentId));
        using var client = storyFactory.CreateClient();
        using var request = Request(
            HttpMethod.Post,
            $"/api/reports/sprs-readiness?assessmentId={ids.AssessmentId}",
            ids.TenantId,
            ids.ActorUserId,
            [Permission.ViewReports],
            CreateBody());

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await using var scope = storyFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.False(await db.Reports.AnyAsync());
        Assert.False(await db.SprsScoreCalculations.AnyAsync());
        Assert.DoesNotContain(await db.AuditLogEntries.ToArrayAsync(), row =>
            row.EntityType is "Report" or "SprsScoreCalculation");
    }

    [Fact]
    public async Task No_cui_tenant_rejects_cui_classified_report_without_business_writes()
    {
        var ids = StoryIds.Create();
        await using var storyFactory = CreateFactory("sprs-report-cui", db => Seed(db, ids.TenantId, ids.AssessmentId));
        using var client = storyFactory.CreateClient();
        var body = CreateBody() with
        {
            Classification = new Gccs.Application.Common.ContentClassificationRequest(ContentClassification.Cui)
        };
        using var request = Request(
            HttpMethod.Post,
            $"/api/reports/sprs-readiness?assessmentId={ids.AssessmentId}",
            ids.TenantId,
            ids.ActorUserId,
            [Permission.ManageReports],
            body);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await using var scope = storyFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.False(await db.Reports.AnyAsync());
        Assert.False(await db.SprsScoreCalculations.AnyAsync());
        Assert.Contains(await db.AuditLogEntries.ToArrayAsync(), row =>
            row.TenantId == ids.TenantId && row.EntityType == "TenantDataHandlingModePolicy");
    }

    [Fact]
    public async Task Same_idempotency_key_and_request_replays_the_original_immutable_report()
    {
        var ids = StoryIds.Create();
        await using var storyFactory = CreateFactory("sprs-report-idempotent-replay", db => Seed(db, ids.TenantId, ids.AssessmentId));
        using var client = storyFactory.CreateClient();
        var idempotencyKey = $"sprs-{Guid.NewGuid():N}";

        var first = await GenerateAsync(client, ids.TenantId, ids.ActorUserId, ids.AssessmentId, idempotencyKey);
        var replay = await GenerateAsync(client, ids.TenantId, ids.ActorUserId, ids.AssessmentId, idempotencyKey);

        Assert.Equal(first.Id, replay.Id);
        Assert.False(first.IsReplay);
        Assert.True(replay.IsReplay);
        await using var scope = storyFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Single(await db.Reports.Where(row => row.TenantId == ids.TenantId).ToArrayAsync());
        Assert.Single(await db.SprsScoreCalculations.Where(row => row.TenantId == ids.TenantId).ToArrayAsync());
        Assert.Equal(2, await db.AuditLogEntries.CountAsync(row =>
            row.TenantId == ids.TenantId &&
            (row.EntityType == "Report" || row.EntityType == "SprsScoreCalculation")));
    }

    [Fact]
    public async Task Reusing_idempotency_key_for_different_input_returns_conflict_without_writes()
    {
        var ids = StoryIds.Create();
        await using var storyFactory = CreateFactory("sprs-report-idempotent-conflict", db => Seed(db, ids.TenantId, ids.AssessmentId));
        using var client = storyFactory.CreateClient();
        var idempotencyKey = $"sprs-{Guid.NewGuid():N}";
        _ = await GenerateAsync(client, ids.TenantId, ids.ActorUserId, ids.AssessmentId, idempotencyKey);
        using var conflictingRequest = Request(
            HttpMethod.Post,
            $"/api/reports/sprs-readiness?assessmentId={ids.AssessmentId}",
            ids.TenantId,
            ids.ActorUserId,
            [Permission.ManageReports],
            CreateBody() with { ReviewerNotes = "Different leadership context." },
            idempotencyKey);

        using var response = await client.SendAsync(conflictingRequest);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("idempotency_conflict", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        await using var scope = storyFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Single(await db.Reports.Where(row => row.TenantId == ids.TenantId).ToArrayAsync());
        Assert.Single(await db.SprsScoreCalculations.Where(row => row.TenantId == ids.TenantId).ToArrayAsync());
        Assert.Contains(await db.AuditLogEntries.Where(row => row.TenantId == ids.TenantId).ToArrayAsync(), row =>
            row.Action == AuditAction.Rejected &&
            row.EntityType == "Report" &&
            row.MetadataJson.Contains("idempotency-conflict", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Idempotency_key_scope_does_not_cross_tenant_boundaries()
    {
        var ids = StoryIds.Create();
        await using var storyFactory = CreateFactory("sprs-report-idempotent-tenant-scope", db =>
        {
            Seed(db, ids.TenantId, ids.AssessmentId);
            Seed(db, ids.OtherTenantId, ids.OtherTenantAssessmentId);
        });
        using var client = storyFactory.CreateClient();
        var idempotencyKey = $"sprs-shared-{Guid.NewGuid():N}";

        var firstTenant = await GenerateAsync(
            client,
            ids.TenantId,
            ids.ActorUserId,
            ids.AssessmentId,
            idempotencyKey);
        var otherTenant = await GenerateAsync(
            client,
            ids.OtherTenantId,
            ids.ActorUserId,
            ids.OtherTenantAssessmentId,
            idempotencyKey);

        Assert.NotEqual(firstTenant.Id, otherTenant.Id);
        Assert.Equal(ids.TenantId, firstTenant.TenantId);
        Assert.Equal(ids.OtherTenantId, otherTenant.TenantId);
    }

    [Fact]
    public async Task Missing_idempotency_key_is_rejected_without_business_writes()
    {
        var ids = StoryIds.Create();
        await using var storyFactory = CreateFactory("sprs-report-idempotency-required", db => Seed(db, ids.TenantId, ids.AssessmentId));
        using var client = storyFactory.CreateClient();
        using var request = Request(
            HttpMethod.Post,
            $"/api/reports/sprs-readiness?assessmentId={ids.AssessmentId}",
            ids.TenantId,
            ids.ActorUserId,
            [Permission.ManageReports],
            CreateBody());
        request.Headers.Remove("Idempotency-Key");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var scope = storyFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.False(await db.Reports.AnyAsync());
        Assert.False(await db.SprsScoreCalculations.AnyAsync());
    }

    [Fact]
    public async Task Explicit_restricted_marking_conflicting_with_unclassified_notes_is_audit_rejected_without_storage()
    {
        var ids = StoryIds.Create();
        await using var storyFactory = CreateFactory("sprs-report-sensitive-marker", db => Seed(db, ids.TenantId, ids.AssessmentId));
        using var client = storyFactory.CreateClient();
        var sensitiveNotes = "Leadership context\nCUI//SP-PRVCY\nDo not distribute.";
        using var request = Request(
            HttpMethod.Post,
            $"/api/reports/sprs-readiness?assessmentId={ids.AssessmentId}",
            ids.TenantId,
            ids.ActorUserId,
            [Permission.ManageReports],
            CreateBody() with { ReviewerNotes = sensitiveNotes });

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await using var scope = storyFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.False(await db.Reports.AnyAsync());
        Assert.False(await db.SprsScoreCalculations.AnyAsync());
        var rejection = Assert.Single(await db.AuditLogEntries.Where(row =>
            row.TenantId == ids.TenantId &&
            row.EntityType == "TenantDataHandlingModePolicy" &&
            row.Action == AuditAction.Rejected).ToArrayAsync());
        Assert.DoesNotContain(sensitiveNotes, rejection.MetadataJson, StringComparison.Ordinal);
        Assert.DoesNotContain("SP-PRVCY", rejection.MetadataJson, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Review CUI handling controls before leadership approval.")]
    [InlineData("Confirm that exports contain no controlled customer documents.")]
    [InlineData("Leadership review context only.")]
    public void Compliance_discussion_without_an_explicit_marking_is_not_flagged(string notes)
    {
        Assert.False(Gccs.Application.Common.SensitiveContentMarkerDetector.ContainsExplicitRestrictedMarking(notes));
    }

    private static SprsReadinessReportRequest CreateBody() =>
        new(
            "reviewed-rules",
            "Leadership review context.",
            "Reviewed",
            null,
            new Gccs.Application.Common.ContentClassificationRequest(ContentClassification.Unclassified));

    private static async Task<SprsReadinessReportDto> GenerateAsync(
        HttpClient client,
        Guid tenantId,
        Guid actorId,
        Guid assessmentId,
        string? idempotencyKey = null)
    {
        using var request = Request(
            HttpMethod.Post,
            $"/api/reports/sprs-readiness?assessmentId={assessmentId}",
            tenantId,
            actorId,
            [Permission.ManageReports],
            CreateBody(),
            idempotencyKey);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<SprsReadinessReportDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected an SPRS readiness report response.");
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext> seed) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ISprsScoringRuleRepository>();
                services.RemoveAll<ISprsScoreCalculationHistoryRepository>();
                services.RemoveAll<ICmmcAssessmentRepository>();
                services.RemoveAll<IReportRepository>();
                services.RemoveAll<IReportExportRepository>();
                services.RemoveAll<IAuditEventWriter>();
                services.RemoveAll<ICurrentDataHandlingNoticeGuard>();
                services.AddSingleton<ISprsScoringRuleRepository, ReviewedRuleRepository>();
                services.AddScoped<ISprsScoreCalculationHistoryRepository, EfSprsScoreCalculationHistoryRepository>();
                services.AddScoped<ICmmcAssessmentRepository, EfCmmcAssessmentRepository>();
                services.AddScoped<IReportRepository, EfReportRepository>();
                services.AddScoped<IReportExportRepository, EfReportExportRepository>();
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();
                services.AddSingleton<ICurrentDataHandlingNoticeGuard, PermissiveNoticeGuard>();
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                seed(db);
                db.SaveChanges();
            });
        });

    private static HttpRequestMessage Request<T>(
        HttpMethod method,
        string uri,
        Guid tenantId,
        Guid actorId,
        IReadOnlyList<Permission> permissions,
        T? content = default,
        string? idempotencyKey = null)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", actorId.ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", string.Join(',', permissions));
        request.Headers.Add("Idempotency-Key", idempotencyKey ?? $"sprs-{Guid.NewGuid():N}");
        if (content is not null)
        {
            request.Content = JsonContent.Create(content, options: JsonOptions);
        }
        return request;
    }

    private static void Seed(GccsDbContext db, Guid tenantId, Guid assessmentId)
    {
        db.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = $"SPRS tenant {tenantId}",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        });
        const string controlId = "AC.L2-3.1.1";
        if (!db.Controls.Local.Any(control => control.Id == controlId))
        {
            db.Controls.Add(new ControlEntity
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
        }
        db.Assessments.Add(new AssessmentEntity
        {
            Id = assessmentId,
            TenantId = tenantId,
            Name = "Level 2 assessment",
            Type = AssessmentType.Readiness,
            Level = CmmcLevel.Level2,
            Framework = "NIST-SP-800-171-Rev2",
            Status = AssessmentStatus.InProgress,
            StartedAt = new DateOnly(2026, 9, 1),
            OwnerFunction = "Security",
            CreatedAt = DateTimeOffset.UtcNow
        });
        var ids = StoryIds.Create();
        db.ControlAssessments.Add(new ControlAssessmentEntity
        {
            AssessmentId = assessmentId,
            ControlId = controlId,
            ImplementationStatus = ControlImplementationStatus.NotStarted,
            Result = AssessmentResult.NotMet,
            EvidenceItemIdsJson = JsonSerializer.Serialize(new[] { ids.EvidenceItemId }),
            PoamItemIdsJson = JsonSerializer.Serialize(new[] { ids.PoamItemId })
        });
    }

    private sealed class ReviewedRuleRepository : ISprsScoringRuleRepository
    {
        private static readonly SprsScoringRuleSetDto RuleSet = new(
            "reviewed-rules", "2026.09-reviewed", SprsScoringRuleSetState.Published,
            "Reviewed methodology", "https://example.test/reviewed-sprs-methodology",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 9, 1), "Content owner", "Qualified reviewer",
            new DateOnly(2026, 9, 1), 110,
            [new SprsScoringRuleDto("3.1.1", "Authorized access", 5, "Subtract five points when not met.", "https://example.test/reviewed-sprs-methodology")],
            1, "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

        public Task<IReadOnlyList<SprsScoringRuleSetDto>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SprsScoringRuleSetDto>>([RuleSet]);

        public Task<SprsScoringRuleSetDto?> FindAsync(string ruleSetId, CancellationToken cancellationToken = default) =>
            Task.FromResult<SprsScoringRuleSetDto?>(ruleSetId == RuleSet.Id ? RuleSet : null);
    }

    private sealed class PermissiveNoticeGuard : ICurrentDataHandlingNoticeGuard
    {
        public Task EnsureAsync(string workflow, Guid actorUserId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed record StoryIds(
        Guid TenantId,
        Guid OtherTenantId,
        Guid AssessmentId,
        Guid OtherTenantAssessmentId,
        Guid ActorUserId,
        Guid EvidenceItemId,
        Guid PoamItemId)
    {
        public static StoryIds Create() => new(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
    }
}
