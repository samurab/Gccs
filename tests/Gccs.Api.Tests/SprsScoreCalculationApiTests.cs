using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Cmmc;
using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Security;
using Gccs.Application.Tenancy;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Common;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Cmmc;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class SprsScoreCalculationApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public SprsScoreCalculationApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Authorized_calculation_is_durable_audited_and_recalculates_from_current_status()
    {
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();
        await using var factory = CreateFactory("sprs-api-calculate", db => SeedAssessment(db, tenantId, assessmentId));
        using var client = factory.CreateClient();

        var before = await CalculateAsync(client, tenantId, actorId, assessmentId, "Reviewer context only.");

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            var status = await db.ControlAssessments.SingleAsync(row => row.AssessmentId == assessmentId && row.ControlId == "AC.L2-3.1.1");
            status.ImplementationStatus = ControlImplementationStatus.Implemented;
            status.Result = AssessmentResult.Met;
            await db.SaveChangesAsync();
        }

        var after = await CalculateAsync(client, tenantId, actorId, assessmentId, null);

        Assert.Equal(105, before.Score);
        Assert.Equal(110, after.Score);
        Assert.Equal(5, before.TotalDeduction);
        Assert.Equal(0, after.TotalDeduction);
        Assert.Equal("2026.09-reviewed", before.RuleSetVersion);
        Assert.NotEmpty(before.RuleSetSourceSha256);
        Assert.Single(before.UnresolvedGaps);

        using var historyRequest = Request<object?>(HttpMethod.Get, $"/api/cmmc/assessments/{assessmentId}/sprs-calculations", tenantId, actorId, Permission.ViewCmmc);
        var historyResponse = await client.SendAsync(historyRequest);
        var history = await historyResponse.Content.ReadFromJsonAsync<SprsScoreCalculationDto[]>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        Assert.NotNull(history);
        Assert.Equal(2, history.Length);
        Assert.Equal(after.Id, history[0].Id);
        Assert.Equal("Reviewer context only.", history[1].ManualNotes);

        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(2, await verificationDb.SprsScoreCalculations.CountAsync(row => row.TenantId == tenantId));
        var note = await verificationDb.SprsScoreCalculationNotes.SingleAsync(row => row.TenantId == tenantId);
        Assert.Equal(before.Id, note.CalculationId);
        Assert.Equal("Reviewer context only.", note.Note);
        Assert.Equal(ContentClassification.Unclassified, note.Classification);
        Assert.Equal(ContentClassificationSource.UserSelected, note.ClassificationSource);
        Assert.Equal(2, await verificationDb.AuditLogEntries.CountAsync(row =>
            row.TenantId == tenantId && row.EntityType == "SprsScoreCalculation"));
    }

    [Fact]
    public async Task Cross_tenant_history_and_calculation_return_not_found_without_writes()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();
        await using var factory = CreateFactory("sprs-api-tenant", db => SeedAssessment(db, otherTenantId, assessmentId));
        using var client = factory.CreateClient();

        using var calculateRequest = Request(
            HttpMethod.Post,
            $"/api/cmmc/assessments/{assessmentId}/sprs-calculations",
            tenantId,
            Guid.NewGuid(),
            Permission.ManageCmmc,
            new SprsScoreCalculationRequest("reviewed-rules", null));
        var calculateResponse = await client.SendAsync(calculateRequest);
        using var historyRequest = Request<object?>(HttpMethod.Get, $"/api/cmmc/assessments/{assessmentId}/sprs-calculations", tenantId, Guid.NewGuid(), Permission.ViewCmmc);
        var historyResponse = await client.SendAsync(historyRequest);

        Assert.Equal(HttpStatusCode.NotFound, calculateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, historyResponse.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Empty(await db.SprsScoreCalculations.ToArrayAsync());
        Assert.Empty(await db.SprsScoreCalculationNotes.ToArrayAsync());
        Assert.DoesNotContain(await db.AuditLogEntries.ToArrayAsync(), row => row.EntityType == "SprsScoreCalculation");
    }

    [Fact]
    public async Task Missing_manage_permission_is_forbidden_without_history_or_audit()
    {
        var tenantId = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();
        await using var factory = CreateFactory("sprs-api-rbac", db => SeedAssessment(db, tenantId, assessmentId));
        using var client = factory.CreateClient();
        using var request = Request(
            HttpMethod.Post,
            $"/api/cmmc/assessments/{assessmentId}/sprs-calculations",
            tenantId,
            Guid.NewGuid(),
            Permission.ViewCmmc,
            new SprsScoreCalculationRequest("reviewed-rules", null));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Empty(await db.SprsScoreCalculations.ToArrayAsync());
        Assert.DoesNotContain(await db.AuditLogEntries.ToArrayAsync(), row => row.EntityType == "SprsScoreCalculation");
    }

    [Fact]
    public async Task No_cui_tenant_rejects_cui_classified_reviewer_notes_without_calculation_history()
    {
        var tenantId = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();
        await using var factory = CreateFactory("sprs-api-cui", db => SeedAssessment(db, tenantId, assessmentId));
        using var client = factory.CreateClient();
        using var request = Request(
            HttpMethod.Post,
            $"/api/cmmc/assessments/{assessmentId}/sprs-calculations",
            tenantId,
            Guid.NewGuid(),
            Permission.ManageCmmc,
            new SprsScoreCalculationRequest(
                "reviewed-rules",
                "Potentially restricted context.",
                ManualNotesClassification: new ContentClassificationRequest(ContentClassification.Cui)));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Empty(await db.SprsScoreCalculations.ToArrayAsync());
        Assert.Empty(await db.SprsScoreCalculationNotes.ToArrayAsync());
        Assert.Contains(await db.AuditLogEntries.ToArrayAsync(), row =>
            row.TenantId == tenantId && row.EntityType == "TenantDataHandlingModePolicy" && row.Action == Gccs.Domain.Audit.AuditAction.Rejected);
    }

    private async Task<SprsScoreCalculationDto> CalculateAsync(
        HttpClient client,
        Guid tenantId,
        Guid actorId,
        Guid assessmentId,
        string? note)
    {
        using var request = Request(
            HttpMethod.Post,
            $"/api/cmmc/assessments/{assessmentId}/sprs-calculations",
            tenantId,
            actorId,
            Permission.ManageCmmc,
            new SprsScoreCalculationRequest(
                "reviewed-rules",
                note,
                ManualNotesClassification: note is null ? null : ContentClassificationPolicy.DefaultUnclassified()));
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<SprsScoreCalculationDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected a draft SPRS calculation response.");
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext> seed) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ISprsScoringRuleRepository>();
                services.RemoveAll<ISprsScoreCalculationHistoryRepository>();
                services.RemoveAll<ICmmcAssessmentRepository>();
                services.RemoveAll<IAuditEventWriter>();
                services.RemoveAll<ICurrentDataHandlingNoticeGuard>();
                services.AddSingleton<ISprsScoringRuleRepository, ReviewedRuleRepository>();
                services.AddScoped<ISprsScoreCalculationHistoryRepository, EfSprsScoreCalculationHistoryRepository>();
                services.AddScoped<ICmmcAssessmentRepository, EfCmmcAssessmentRepository>();
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
        Permission permission,
        T? content = default)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", actorId.ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", permission.ToString());
        if (content is not null)
        {
            request.Content = JsonContent.Create(content, options: JsonOptions);
        }
        return request;
    }

    private static void SeedAssessment(GccsDbContext db, Guid tenantId, Guid assessmentId)
    {
        db.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = "SPRS tenant",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.Controls.Add(new ControlEntity
        {
            Id = "AC.L2-3.1.1",
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
        db.ControlAssessments.Add(new ControlAssessmentEntity
        {
            AssessmentId = assessmentId,
            ControlId = "AC.L2-3.1.1",
            ImplementationStatus = ControlImplementationStatus.NotStarted,
            Result = AssessmentResult.NotMet
        });
    }

    private sealed class ReviewedRuleRepository : ISprsScoringRuleRepository
    {
        private static readonly SprsScoringRuleSetDto RuleSet = new(
            Id: "reviewed-rules",
            Version: "2026.09-reviewed",
            State: SprsScoringRuleSetState.Published,
            SourceName: "Reviewed test methodology",
            SourceUrl: "https://example.test/reviewed-sprs-methodology",
            EffectiveDate: new DateOnly(2026, 1, 1),
            LastReviewedAt: new DateOnly(2026, 9, 1),
            Owner: "Content owner",
            Reviewer: "Qualified reviewer",
            ReviewDate: new DateOnly(2026, 9, 1),
            MaximumScore: 110,
            Rules: [new SprsScoringRuleDto("3.1.1", "Authorized access", 5, "Subtract five points when not met.", "https://example.test/reviewed-sprs-methodology")],
            ExpectedRequirementCount: 1,
            SourceSha256: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

        public Task<IReadOnlyList<SprsScoringRuleSetDto>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SprsScoringRuleSetDto>>([RuleSet]);

        public Task<SprsScoringRuleSetDto?> FindAsync(string ruleSetId, CancellationToken cancellationToken = default) =>
            Task.FromResult<SprsScoringRuleSetDto?>(ruleSetId == RuleSet.Id ? RuleSet : null);
    }

    private sealed class PermissiveNoticeGuard : ICurrentDataHandlingNoticeGuard
    {
        public Task EnsureAsync(string workflow, Guid actorUserId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
