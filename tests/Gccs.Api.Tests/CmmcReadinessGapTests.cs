using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Cmmc;
using Gccs.Application.Evidence;
using Gccs.Application.Security;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Compliance;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Cmmc;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class CmmcReadinessGapTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public CmmcReadinessGapTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_27_3_1_and_TC_27_3_2_Gap_dashboard_calculates_priority_and_reason_codes()
    {
        var ids = StoryIds.ForCase("tc-27-3-1");
        await using var factory = CreateFactory("tc-27-3-1", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var gaps = await GetGapsAsync(client, ids.TenantId, ids.AssessmentId, Permission.ViewCmmc);

        Assert.Equal(CmmcGapPriority.Critical, gaps[0].Priority);
        var critical = gaps.Single(gap => gap.ControlId == ids.CriticalControlId);
        Assert.Contains("control-not-implemented", critical.ReasonCodes);
        Assert.Contains("evidence-missing", critical.ReasonCodes);
        Assert.Contains("cui-relevant", critical.ReasonCodes);
        Assert.False(critical.HasAssessmentObjectiveCoverage);
        var high = gaps.Single(gap => gap.ControlId == ids.HighControlId);
        Assert.Equal(CmmcGapPriority.High, high.Priority);
        Assert.Contains("poam-overdue", high.ReasonCodes);
        var low = gaps.Single(gap => gap.ControlId == ids.LowControlId);
        Assert.Equal(CmmcGapPriority.Low, low.Priority);
    }

    [Fact]
    public async Task TC_27_3_3_User_can_create_poam_and_task_from_gap()
    {
        var ids = StoryIds.ForCase("tc-27-3-3");
        await using var factory = CreateFactory("tc-27-3-3", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            HttpMethod.Post,
            $"/api/cmmc/assessments/{ids.AssessmentId}/gaps/{ids.CriticalControlId}/poam-item",
            new CreatePoamFromGapRequest("Security", DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30)),
            ids.TenantId,
            Permission.ManageCmmc);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var poam = JsonSerializer.Deserialize<CmmcPoamItemDto>(body, JsonOptions) ?? throw new InvalidOperationException("Expected POA&M item.");
        Assert.Equal(ids.CriticalControlId, poam.ControlId);
        Assert.NotNull(poam.RemediationTaskId);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.True(await dbContext.ComplianceTasks.AnyAsync(task =>
            task.TenantId == ids.TenantId &&
            task.Id == poam.RemediationTaskId &&
            task.ControlId == ids.CriticalControlId));
    }

    [Fact]
    public async Task TC_27_3_4_Priority_recalculates_when_evidence_status_changes()
    {
        var ids = StoryIds.ForCase("tc-27-3-4");
        await using var factory = CreateFactory("tc-27-3-4", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var before = await GetGapsAsync(client, ids.TenantId, ids.AssessmentId, Permission.ViewCmmc);
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            var request = await dbContext.EvidenceRequests.SingleAsync(candidate => candidate.Id == ids.RecalcEvidenceRequestId);
            request.Status = EvidenceRequestStatus.Accepted.ToString();
            await dbContext.SaveChangesAsync();
        }

        var after = await GetGapsAsync(client, ids.TenantId, ids.AssessmentId, Permission.ViewCmmc);

        Assert.Equal(CmmcGapPriority.Medium, before.Single(gap => gap.ControlId == ids.RecalcControlId).Priority);
        Assert.Equal(CmmcGapPriority.Low, after.Single(gap => gap.ControlId == ids.RecalcControlId).Priority);
    }

    [Fact]
    public async Task TC_27_3_5_Gap_dashboard_is_tenant_scoped_and_requires_permission()
    {
        var ids = StoryIds.ForCase("tc-27-3-5");
        await using var factory = CreateFactory("tc-27-3-5", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        using var otherTenant = CreateRequest<object?>(HttpMethod.Get, $"/api/cmmc/assessments/{ids.AssessmentId}/gaps", null, ids.OtherTenantId, Permission.ViewCmmc);
        using var missingPermission = CreateRequest<object?>(HttpMethod.Get, $"/api/cmmc/assessments/{ids.AssessmentId}/gaps", null, ids.TenantId, Permission.ViewTasks);
        var otherTenantResponse = await client.SendAsync(otherTenant);
        var missingPermissionResponse = await client.SendAsync(missingPermission);

        Assert.Equal(HttpStatusCode.NotFound, otherTenantResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, missingPermissionResponse.StatusCode);
    }

    private static async Task<CmmcReadinessGapDto[]> GetGapsAsync(HttpClient client, Guid tenantId, Guid assessmentId, params Permission[] permissions)
    {
        using var request = CreateRequest<object?>(HttpMethod.Get, $"/api/cmmc/assessments/{assessmentId}/gaps", null, tenantId, permissions);
        var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK, got {response.StatusCode}: {responseBody}");
        return JsonSerializer.Deserialize<CmmcReadinessGapDto[]>(responseBody, JsonOptions) ??
            throw new InvalidOperationException("Expected readiness gaps.");
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<CmmcAssessmentService>();
                services.AddScoped<CmmcPoamService>();
                services.AddScoped<ICmmcAssessmentRepository, EfCmmcAssessmentRepository>();
                services.AddScoped<ICmmcPoamRepository, EfCmmcPoamRepository>();
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
                seed?.Invoke(dbContext);
                dbContext.SaveChanges();
            });
        });

    private static HttpRequestMessage CreateRequest<TContent>(HttpMethod method, string requestUri, TContent content, Guid tenantId, params Permission[] permissions)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", Guid.NewGuid().ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", string.Join(",", permissions.Select(permission => permission.ToString())));
        if (content is not null)
        {
            request.Content = JsonContent.Create(content, options: JsonOptions);
        }

        return request;
    }

    private static void SeedScenario(GccsDbContext dbContext, StoryIds ids)
    {
        dbContext.Tenants.AddRange(CreateTenant(ids.TenantId), CreateTenant(ids.OtherTenantId));
        dbContext.Assessments.Add(new AssessmentEntity
        {
            Id = ids.AssessmentId,
            TenantId = ids.TenantId,
            Name = "Level 2 gap dashboard",
            Type = AssessmentType.SelfAssessment,
            Level = CmmcLevel.Level2,
            Framework = "CMMC 2.0",
            Status = AssessmentStatus.InProgress,
            StartedAt = new DateOnly(2026, 7, 1),
            OwnerFunction = "Security",
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Controls.AddRange(
            CreateControl(ids.CriticalControlId, "AC", "Critical access gap"),
            CreateControl(ids.HighControlId, "IA", "High identity gap"),
            CreateControl(ids.LowControlId, "CM", "Low configuration gap"),
            CreateControl(ids.RecalcControlId, "SI", "Recalculating evidence gap"));
        dbContext.ControlAssessments.AddRange(
            CreateControlAssessment(ids.AssessmentId, ids.CriticalControlId, ControlImplementationStatus.NotStarted, AssessmentResult.NotMet, string.Empty),
            CreateControlAssessment(ids.AssessmentId, ids.HighControlId, ControlImplementationStatus.Implemented, AssessmentResult.Met, "Implemented but POA&M overdue."),
            CreateControlAssessment(ids.AssessmentId, ids.LowControlId, ControlImplementationStatus.Implemented, AssessmentResult.Met, "Covered with accepted evidence.", ids.LowEvidenceItemId),
            CreateControlAssessment(ids.AssessmentId, ids.RecalcControlId, ControlImplementationStatus.Implemented, AssessmentResult.Met, "Covered after evidence acceptance."));
        dbContext.EvidenceRequests.AddRange(
            CreateEvidenceRequest(ids.TenantId, ids.LowEvidenceRequestId, ids.LowControlId, EvidenceRequestStatus.Accepted, ids.ActorUserId),
            CreateEvidenceRequest(ids.TenantId, ids.RecalcEvidenceRequestId, ids.RecalcControlId, EvidenceRequestStatus.Open, ids.ActorUserId));
        dbContext.PoamItems.Add(new PoamItemEntity
        {
            Id = ids.HighPoamId,
            TenantId = ids.TenantId,
            AssessmentId = ids.AssessmentId,
            ControlId = ids.HighControlId,
            Weakness = "Overdue validation",
            PlannedRemediation = "Complete overdue validation.",
            RiskLevel = RiskLevel.High,
            Status = PoamStatus.Open,
            OwnerFunction = "Security",
            TargetCompletionAt = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-3),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = ids.ActorUserId
        });
    }

    private static ControlAssessmentEntity CreateControlAssessment(
        Guid assessmentId,
        string controlId,
        ControlImplementationStatus status,
        AssessmentResult result,
        string implementationDetails,
        Guid? evidenceItemId = null) =>
        new()
        {
            AssessmentId = assessmentId,
            ControlId = controlId,
            ImplementationStatus = status,
            Result = result,
            ImplementationDetails = implementationDetails,
            EvidenceItemIdsJson = evidenceItemId is null ? "[]" : JsonSerializer.Serialize(new[] { evidenceItemId.Value }),
            OwnerFunction = "Security",
            ResponsibilityType = ControlResponsibilityType.Organization
        };

    private static ControlEntity CreateControl(string controlId, string family, string title) =>
        new()
        {
            Id = controlId,
            Framework = ControlFramework.NistSp800171Revision2,
            CmmcLevel = CmmcLevel.Level2,
            Family = family,
            Title = title,
            Requirement = "Protect CUI.",
            AssessmentObjective = "Verify implementation and evidence.",
            SourceName = "NIST",
            SourceUrl = "https://example.test/nist",
            SourceLastReviewedAt = new DateOnly(2026, 6, 17),
            SourceConfidence = "high"
        };

    private static EvidenceRequestEntity CreateEvidenceRequest(
        Guid tenantId,
        Guid evidenceRequestId,
        string controlId,
        EvidenceRequestStatus status,
        Guid actorUserId) =>
        new()
        {
            Id = evidenceRequestId,
            TenantId = tenantId,
            RequesterUserId = actorUserId,
            DueDate = new DateOnly(2026, 9, 1),
            Status = status.ToString(),
            Priority = EvidenceRequestPriority.High.ToString(),
            Instructions = "Provide gap evidence.",
            RelatedRecordType = EvidenceRequestRelatedRecordType.Control.ToString(),
            RelatedRecordId = controlId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = actorUserId
        };

    private static TenantEntity CreateTenant(Guid tenantId) =>
        new()
        {
            Id = tenantId,
            Name = $"CMMC Gap Tenant {tenantId:N}",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private sealed record StoryIds(
        Guid TenantId,
        Guid OtherTenantId,
        Guid AssessmentId,
        Guid ActorUserId,
        string CriticalControlId,
        string HighControlId,
        string LowControlId,
        string RecalcControlId,
        Guid LowEvidenceRequestId,
        Guid RecalcEvidenceRequestId,
        Guid LowEvidenceItemId,
        Guid HighPoamId)
    {
        public static StoryIds ForCase(string caseName)
        {
            var suffix = Math.Abs(caseName.GetHashCode(StringComparison.Ordinal)) % 10000;
            return new StoryIds(
                Guid.Parse($"27327327-3273-2732-7327-32732731{suffix:D4}"),
                Guid.Parse($"27327327-3273-2732-7327-32732732{suffix:D4}"),
                Guid.Parse($"27327327-3273-2732-7327-32732733{suffix:D4}"),
                Guid.Parse("27327327-3273-2732-7327-327327327199"),
                $"AC.L2-3.1.{suffix % 100:D2}",
                $"IA.L2-3.5.{suffix % 100:D2}",
                $"CM.L2-3.4.{suffix % 100:D2}",
                $"SI.L2-3.14.{suffix % 100:D2}",
                Guid.Parse($"27327327-3273-2732-7327-32732734{suffix:D4}"),
                Guid.Parse($"27327327-3273-2732-7327-32732735{suffix:D4}"),
                Guid.Parse($"27327327-3273-2732-7327-32732736{suffix:D4}"),
                Guid.Parse($"27327327-3273-2732-7327-32732737{suffix:D4}"));
        }
    }
}
