using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Calendar;
using Gccs.Application.Cmmc;
using Gccs.Application.Security;
using Gccs.Domain.Audit;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Compliance;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Calendar;
using Gccs.Infrastructure.Cmmc;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class CmmcPoamItemTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public CmmcPoamItemTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_13_3_1_Creates_poam_item_with_control_gap_plan_owner_due_date_risk_and_status()
    {
        var tenantId = Guid.Parse("13313313-3313-1313-3133-1331331331a1");
        await using var factory = CreateFactory("tc-13-3-1", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            SeedControls(dbContext);
        });
        using var client = factory.CreateClient();
        var assessment = await CreateAssessmentAsync(client, tenantId);

        var created = await CreatePoamAsync(client, tenantId, assessment.Id, CreatePoamRequest());

        Assert.Equal(assessment.Id, created.AssessmentId);
        Assert.Equal("AC.L1-3.1.1", created.ControlId);
        Assert.Equal("MFA evidence gap", created.Weakness);
        Assert.Equal("Collect configuration export and validate access review.", created.PlannedRemediation);
        Assert.Equal(RiskLevel.High, created.RiskLevel);
        Assert.Equal(PoamStatus.Open, created.Status);
        Assert.Equal("Security", created.OwnerFunction);
        Assert.Equal(new DateOnly(2026, 7, 15), created.TargetCompletionAt);
        Assert.NotNull(created.RemediationTaskId);
    }

    [Fact]
    public async Task TC_13_3_2_Poam_task_is_created_and_appears_on_calendar()
    {
        var tenantId = Guid.Parse("13313313-3313-1313-3133-1331331331a2");
        await using var factory = CreateFactory("tc-13-3-2", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            SeedControls(dbContext);
        });
        using var client = factory.CreateClient();
        var assessment = await CreateAssessmentAsync(client, tenantId);

        var created = await CreatePoamAsync(client, tenantId, assessment.Id, CreatePoamRequest());
        var calendar = await ListCalendarAsync(client, tenantId);

        Assert.NotNull(created.RemediationTaskId);
        Assert.Contains(calendar, item =>
            item.Id == $"task:{created.RemediationTaskId}" &&
            item.Title.Contains("POA&M", StringComparison.OrdinalIgnoreCase) &&
            item.Module == "CMMC" &&
            item.RelatedEntityType == "control" &&
            item.RelatedEntityId == "AC.L1-3.1.1");
    }

    [Fact]
    public async Task TC_13_3_3_Open_and_overdue_poam_items_roll_into_cmmc_summary()
    {
        var tenantId = Guid.Parse("13313313-3313-1313-3133-1331331331a3");
        await using var factory = CreateFactory("tc-13-3-3", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            SeedControls(dbContext);
        });
        using var client = factory.CreateClient();
        var assessment = await CreateAssessmentAsync(client, tenantId);

        await CreatePoamAsync(client, tenantId, assessment.Id, CreatePoamRequest() with { TargetCompletionAt = new DateOnly(2026, 6, 1) });
        await CreatePoamAsync(client, tenantId, assessment.Id, CreatePoamRequest() with
        {
            Weakness = "Accepted compensating control",
            Status = PoamStatus.AcceptedRisk,
            TargetCompletionAt = new DateOnly(2026, 6, 1)
        });
        using var detailRequest = CreateRequest<object?>(HttpMethod.Get, $"/api/cmmc/assessments/{assessment.Id}", null, tenantId, Permission.ViewCmmc);
        var detailResponse = await client.SendAsync(detailRequest);
        var detail = await detailResponse.Content.ReadFromJsonAsync<CmmcAssessmentDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        Assert.NotNull(detail);
        Assert.Equal(1, detail.OpenPoamItemCount);
        Assert.Equal(1, detail.OverduePoamItemCount);
    }

    [Fact]
    public async Task TC_13_3_4_Create_update_and_status_changes_are_audit_logged()
    {
        var tenantId = Guid.Parse("13313313-3313-1313-3133-1331331331a4");
        await using var factory = CreateFactory("tc-13-3-4", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            SeedControls(dbContext);
        });
        using var client = factory.CreateClient();
        var assessment = await CreateAssessmentAsync(client, tenantId);
        var created = await CreatePoamAsync(client, tenantId, assessment.Id, CreatePoamRequest());

        using var updateRequest = CreateRequest(
            HttpMethod.Patch,
            $"/api/cmmc/assessments/{assessment.Id}/poam-items/{created.Id}",
            CreatePoamRequest() with { Status = PoamStatus.InProgress },
            tenantId,
            Permission.ManageCmmc);
        var updateResponse = await client.SendAsync(updateRequest);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var audits = await dbContext.AuditLogEntries
            .Where(audit => audit.TenantId == tenantId && audit.EntityType == "CmmcPoamItem" && audit.EntityId == created.Id.ToString())
            .ToArrayAsync();
        Assert.Contains(audits, audit => audit.Action == AuditAction.Created);
        Assert.Contains(audits, audit => audit.Action == AuditAction.Updated && audit.MetadataJson.Contains("InProgress", StringComparison.Ordinal));
    }

    private static async Task<CmmcAssessmentDto> CreateAssessmentAsync(HttpClient client, Guid tenantId)
    {
        using var request = CreateRequest(
            HttpMethod.Post,
            "/api/cmmc/assessments",
            new UpsertCmmcAssessmentRequest(
                "POA&M assessment",
                AssessmentType.Readiness,
                CmmcLevel.Level1,
                "CMMC",
                AssessmentStatus.Planned,
                new DateOnly(2026, 6, 15),
                null,
                new DateOnly(2027, 6, 15),
                "Security",
                null,
                []),
            tenantId,
            Permission.ManageCmmc);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<CmmcAssessmentDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected CMMC assessment response.");
    }

    private static async Task<CmmcPoamItemDto> CreatePoamAsync(
        HttpClient client,
        Guid tenantId,
        Guid assessmentId,
        UpsertCmmcPoamItemRequest body)
    {
        using var request = CreateRequest(HttpMethod.Post, $"/api/cmmc/assessments/{assessmentId}/poam-items", body, tenantId, Permission.ManageCmmc);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<CmmcPoamItemDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected CMMC POA&M response.");
    }

    private static async Task<CalendarEventDto[]> ListCalendarAsync(HttpClient client, Guid tenantId)
    {
        using var request = CreateRequest<object?>(
            HttpMethod.Get,
            "/api/calendar/events?from=2026-06-01&to=2026-08-31",
            null,
            tenantId,
            Permission.ViewTasks);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<CalendarEventDto[]>(JsonOptions) ?? [];
    }

    private static UpsertCmmcPoamItemRequest CreatePoamRequest() =>
        new(
            "AC.L1-3.1.1",
            "MFA evidence gap",
            "Collect configuration export and validate access review.",
            RiskLevel.High,
            PoamStatus.Open,
            null,
            "Security",
            new DateOnly(2026, 7, 15),
            null,
            null,
            []);

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
                services.AddScoped<ICalendarRepository, EfCalendarRepository>();
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

    private static HttpRequestMessage CreateRequest<TContent>(
        HttpMethod method,
        string requestUri,
        TContent content,
        Guid tenantId,
        Permission permission)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", Guid.NewGuid().ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", permission.ToString());
        if (content is not null)
        {
            request.Content = JsonContent.Create(content, options: JsonOptions);
        }

        return request;
    }

    private static void SeedTenant(GccsDbContext dbContext, Guid tenantId)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = "CMMC POA&M Tenant",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private static void SeedControls(GccsDbContext dbContext)
    {
        dbContext.Controls.Add(new ControlEntity
        {
            Id = "AC.L1-3.1.1",
            Framework = ControlFramework.Cmmc,
            CmmcLevel = CmmcLevel.Level1,
            Family = "Access Control",
            Title = "Authorized access control",
            Requirement = "Level 1 baseline requirement.",
            AssessmentObjective = "Verify authorized access.",
            EvidenceExamplesJson = "[]",
            SourceName = "CMMC",
            SourceUrl = "https://dodcio.defense.gov/CMMC/Resources-Documentation/",
            SourceLastReviewedAt = new DateOnly(2026, 6, 15),
            SourceConfidence = "high",
            SourceRequiresExpertReview = false
        });
    }
}
