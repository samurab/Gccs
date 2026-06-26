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

    [Fact]
    public async Task Tenant_user_can_list_and_get_own_tenant_poam_items()
    {
        var tenantId = Guid.Parse("13313313-3313-1313-3133-1331331331b1");
        await using var factory = CreateFactory("poam-crud-own-tenant", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            SeedControls(dbContext);
        });
        using var client = factory.CreateClient();
        var assessment = await CreateAssessmentAsync(client, tenantId);
        var created = await CreatePoamAsync(client, tenantId, assessment.Id, CreatePoamRequest());

        using var listRequest = CreateRequest<object?>(HttpMethod.Get, "/api/cmmc/poam-items", null, tenantId, Permission.ViewCmmc);
        var listResponse = await client.SendAsync(listRequest);
        var items = await listResponse.Content.ReadFromJsonAsync<CmmcPoamItemDto[]>(JsonOptions);
        using var getRequest = CreateRequest<object?>(HttpMethod.Get, $"/api/cmmc/poam-items/{created.Id}", null, tenantId, Permission.ViewCmmc);
        var getResponse = await client.SendAsync(getRequest);
        var item = await getResponse.Content.ReadFromJsonAsync<CmmcPoamItemDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var listed = Assert.Single(items ?? []);
        Assert.Equal(created.Id, listed.Id);
        Assert.Equal("MFA evidence gap", listed.Title);
        Assert.Equal(RiskLevel.High, listed.Severity);
        Assert.Equal(new DateOnly(2026, 7, 15), listed.DueDate);
        Assert.Equal("Collect configuration export and validate access review.", listed.RemediationPlan);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(item);
        Assert.Equal(created.Id, item.Id);
    }

    [Fact]
    public async Task Empty_tenant_poam_list_returns_empty_array()
    {
        var tenantId = Guid.Parse("13313313-3313-1313-3133-1331331331b2");
        await using var factory = CreateFactory("poam-empty-list", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            SeedControls(dbContext);
        });
        using var client = factory.CreateClient();
        using var request = CreateRequest<object?>(HttpMethod.Get, "/api/cmmc/poam-items", null, tenantId, Permission.ViewCmmc);

        var response = await client.SendAsync(request);
        var items = await response.Content.ReadFromJsonAsync<CmmcPoamItemDto[]>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(items ?? []);
    }

    [Fact]
    public async Task Tenant_user_cannot_access_another_tenant_poam_item()
    {
        var tenantAId = Guid.Parse("13313313-3313-1313-3133-1331331331b3");
        var tenantBId = Guid.Parse("13313313-3313-1313-3133-1331331331c3");
        await using var factory = CreateFactory("poam-cross-tenant", dbContext =>
        {
            SeedTenant(dbContext, tenantAId);
            SeedTenant(dbContext, tenantBId);
            SeedControls(dbContext);
        });
        using var client = factory.CreateClient();
        var assessmentB = await CreateAssessmentAsync(client, tenantBId);
        var poamB = await CreatePoamAsync(client, tenantBId, assessmentB.Id, CreatePoamRequest());

        using var getRequest = CreateRequest<object?>(HttpMethod.Get, $"/api/cmmc/poam-items/{poamB.Id}", null, tenantAId, Permission.ViewCmmc);
        var getResponse = await client.SendAsync(getRequest);
        using var updateRequest = CreateRequest(HttpMethod.Patch, $"/api/cmmc/poam-items/{poamB.Id}", CreatePoamRequest() with { Status = PoamStatus.InProgress }, tenantAId, Permission.ManageCmmc);
        var updateResponse = await client.SendAsync(updateRequest);

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);
    }

    [Fact]
    public async Task Unauthorized_user_cannot_create_update_or_close_poam_item()
    {
        var tenantId = Guid.Parse("13313313-3313-1313-3133-1331331331b4");
        await using var factory = CreateFactory("poam-unauthorized", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            SeedControls(dbContext);
        });
        using var client = factory.CreateClient();
        var assessment = await CreateAssessmentAsync(client, tenantId);
        var created = await CreatePoamAsync(client, tenantId, assessment.Id, CreatePoamRequest());

        using var createRequest = CreateRequest(HttpMethod.Post, $"/api/cmmc/assessments/{assessment.Id}/poam-items", CreatePoamRequest(), tenantId, Permission.ViewCmmc);
        using var updateRequest = CreateRequest(HttpMethod.Patch, $"/api/cmmc/poam-items/{created.Id}", CreatePoamRequest() with { Status = PoamStatus.InProgress }, tenantId, Permission.ViewCmmc);
        using var closeRequest = CreateRequest<object?>(HttpMethod.Post, $"/api/cmmc/poam-items/{created.Id}/close", null, tenantId, Permission.ViewCmmc);

        var createResponse = await client.SendAsync(createRequest);
        var updateResponse = await client.SendAsync(updateRequest);
        var closeResponse = await client.SendAsync(closeRequest);

        Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, closeResponse.StatusCode);
    }

    [Fact]
    public async Task Update_and_close_actions_create_audit_log_entries()
    {
        var tenantId = Guid.Parse("13313313-3313-1313-3133-1331331331b5");
        await using var factory = CreateFactory("poam-close-audit", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            SeedControls(dbContext);
        });
        using var client = factory.CreateClient();
        var assessment = await CreateAssessmentAsync(client, tenantId);
        var created = await CreatePoamAsync(client, tenantId, assessment.Id, CreatePoamRequest());

        using var updateRequest = CreateRequest(HttpMethod.Patch, $"/api/cmmc/poam-items/{created.Id}", CreatePoamRequest() with { Status = PoamStatus.Blocked }, tenantId, Permission.ManageCmmc);
        var updateResponse = await client.SendAsync(updateRequest);
        using var closeRequest = CreateRequest<object?>(HttpMethod.Post, $"/api/cmmc/poam-items/{created.Id}/close", null, tenantId, Permission.ManageCmmc);
        var closeResponse = await client.SendAsync(closeRequest);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, closeResponse.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var audits = await dbContext.AuditLogEntries
            .Where(audit => audit.TenantId == tenantId && audit.EntityType == "CmmcPoamItem" && audit.EntityId == created.Id.ToString())
            .OrderBy(audit => audit.OccurredAt)
            .ToArrayAsync();

        Assert.Contains(audits, audit => audit.Action == AuditAction.Created);
        Assert.Contains(audits, audit => audit.Action == AuditAction.Updated && audit.MetadataJson.Contains("Blocked", StringComparison.Ordinal));
        Assert.Contains(audits, audit => audit.Action == AuditAction.Updated && audit.Summary.Contains("closed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Invalid_status_or_severity_fails_validation()
    {
        var tenantId = Guid.Parse("13313313-3313-1313-3133-1331331331b6");
        await using var factory = CreateFactory("poam-invalid-enum", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            SeedControls(dbContext);
        });
        using var client = factory.CreateClient();
        var assessment = await CreateAssessmentAsync(client, tenantId);

        using var invalidStatusRequest = CreateRawJsonRequest(
            HttpMethod.Post,
            $"/api/cmmc/assessments/{assessment.Id}/poam-items",
            """
            {
              "controlId": "AC.L1-3.1.1",
              "weakness": "Invalid status",
              "plannedRemediation": "Fix invalid status.",
              "riskLevel": "High",
              "status": "Impossible",
              "ownerUserId": null,
              "ownerFunction": "Security",
              "targetCompletionAt": "2026-07-15",
              "completedAt": null,
              "remediationTaskId": null,
              "evidenceItemIds": []
            }
            """,
            tenantId,
            Permission.ManageCmmc);
        using var invalidSeverityRequest = CreateRawJsonRequest(
            HttpMethod.Post,
            $"/api/cmmc/assessments/{assessment.Id}/poam-items",
            """
            {
              "controlId": "AC.L1-3.1.1",
              "weakness": "Invalid severity",
              "plannedRemediation": "Fix invalid severity.",
              "riskLevel": "Severe",
              "status": "Open",
              "ownerUserId": null,
              "ownerFunction": "Security",
              "targetCompletionAt": "2026-07-15",
              "completedAt": null,
              "remediationTaskId": null,
              "evidenceItemIds": []
            }
            """,
            tenantId,
            Permission.ManageCmmc);

        var invalidStatusResponse = await client.SendAsync(invalidStatusRequest);
        var invalidSeverityResponse = await client.SendAsync(invalidSeverityRequest);

        Assert.Equal(HttpStatusCode.BadRequest, invalidStatusResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidSeverityResponse.StatusCode);
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

    private static HttpRequestMessage CreateRawJsonRequest(
        HttpMethod method,
        string requestUri,
        string json,
        Guid tenantId,
        Permission permission)
    {
        var request = CreateRequest<object?>(method, requestUri, null, tenantId, permission);
        request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
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
