using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Cmmc;
using Gccs.Application.Security;
using Gccs.Domain.Audit;
using Gccs.Domain.Cmmc;
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

public sealed class CmmcAssessmentTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public CmmcAssessmentTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_13_1_1_Creates_level_1_and_level_2_readiness_assessments_with_status_dates_and_owner()
    {
        var tenantId = Guid.Parse("13113111-3113-1113-1311-3111311131a1");
        await using var factory = CreateFactory("tc-13-1-1", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();

        var level1 = await CreateAssessmentAsync(client, tenantId, CreateRequestBody("Level 1 workspace", CmmcLevel.Level1));
        var level2 = await CreateAssessmentAsync(client, tenantId, CreateRequestBody("Level 2 workspace", CmmcLevel.Level2));

        Assert.Equal(CmmcLevel.Level1, level1.Level);
        Assert.Equal(CmmcLevel.Level2, level2.Level);
        Assert.Equal(AssessmentStatus.Planned, level1.Status);
        Assert.Equal(new DateOnly(2026, 6, 15), level1.StartedAt);
        Assert.Equal(new DateOnly(2027, 6, 15), level1.AffirmationDueAt);
        Assert.Equal("Security", level1.OwnerFunction);
    }

    [Fact]
    public async Task TC_13_1_2_Links_assessment_to_company_profile_and_contracts_for_detail_display()
    {
        var tenantId = Guid.Parse("13113111-3113-1113-1311-3111311131a2");
        var companyProfileId = Guid.Parse("13113111-3113-1113-1311-3111311131c2");
        var contractId = Guid.Parse("13113111-3113-1113-1311-3111311131d2");
        await using var factory = CreateFactory("tc-13-1-2", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        var created = await CreateAssessmentAsync(client, tenantId, CreateRequestBody("Linked assessment", CmmcLevel.Level2) with
        {
            CompanyProfileId = companyProfileId,
            ContractIds = [contractId]
        });

        using var detailRequest = CreateRequest<object?>(HttpMethod.Get, $"/api/cmmc/assessments/{created.Id}", null, tenantId, Permission.ViewCmmc);
        var detailResponse = await client.SendAsync(detailRequest);
        var detail = await detailResponse.Content.ReadFromJsonAsync<CmmcAssessmentDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        Assert.NotNull(detail);
        Assert.Equal(companyProfileId, detail.CompanyProfileId);
        Assert.Equal([contractId], detail.ContractIds);
    }

    [Fact]
    public async Task TC_13_1_3_Control_status_updates_recalculate_completion_progress()
    {
        var tenantId = Guid.Parse("13113111-3113-1113-1311-3111311131a3");
        await using var factory = CreateFactory("tc-13-1-3", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            dbContext.Controls.AddRange(
                CreateControl("AC.L1-3.1.1", CmmcLevel.Level1),
                CreateControl("IA.L1-3.5.1", CmmcLevel.Level1));
        });
        using var client = factory.CreateClient();
        var created = await CreateAssessmentAsync(client, tenantId, CreateRequestBody("Progress assessment", CmmcLevel.Level1));

        await UpdateControlAsync(client, tenantId, created.Id, "AC.L1-3.1.1", ControlImplementationStatus.Implemented, AssessmentResult.Met);
        await UpdateControlAsync(client, tenantId, created.Id, "IA.L1-3.5.1", ControlImplementationStatus.PartiallyImplemented, AssessmentResult.NotMet);
        using var detailRequest = CreateRequest<object?>(HttpMethod.Get, $"/api/cmmc/assessments/{created.Id}", null, tenantId, Permission.ViewCmmc);
        var detailResponse = await client.SendAsync(detailRequest);
        var detail = await detailResponse.Content.ReadFromJsonAsync<CmmcAssessmentDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        Assert.NotNull(detail);
        Assert.Equal(2, detail.ControlSummary.Total);
        Assert.Equal(1, detail.ControlSummary.Implemented);
        Assert.Equal(1, detail.ControlSummary.PartiallyImplemented);
        Assert.Equal(50, detail.ControlSummary.CompletionPercentage);
    }

    [Fact]
    public async Task TC_13_1_4_Create_update_and_status_changes_are_audit_logged()
    {
        var tenantId = Guid.Parse("13113111-3113-1113-1311-3111311131a4");
        await using var factory = CreateFactory("tc-13-1-4", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        var created = await CreateAssessmentAsync(client, tenantId, CreateRequestBody("Audit assessment", CmmcLevel.Level2));

        using var updateRequest = CreateRequest(
            HttpMethod.Put,
            $"/api/cmmc/assessments/{created.Id}",
            CreateRequestBody("Audit assessment", CmmcLevel.Level2) with { Status = AssessmentStatus.InProgress },
            tenantId,
            Permission.ManageCmmc);
        var updateResponse = await client.SendAsync(updateRequest);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var audits = await dbContext.AuditLogEntries
            .Where(audit => audit.TenantId == tenantId && audit.EntityType == "CmmcAssessment" && audit.EntityId == created.Id.ToString())
            .ToArrayAsync();
        Assert.Contains(audits, audit => audit.Action == AuditAction.Created);
        Assert.Contains(audits, audit => audit.Action == AuditAction.Updated && audit.MetadataJson.Contains("InProgress", StringComparison.Ordinal));
    }

    private static async Task<CmmcAssessmentDto> CreateAssessmentAsync(
        HttpClient client,
        Guid tenantId,
        UpsertCmmcAssessmentRequest body)
    {
        using var request = CreateRequest(HttpMethod.Post, "/api/cmmc/assessments", body, tenantId, Permission.ManageCmmc);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<CmmcAssessmentDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected CMMC assessment response.");
    }

    private static async Task<CmmcControlStatusDto> UpdateControlAsync(
        HttpClient client,
        Guid tenantId,
        Guid assessmentId,
        string controlId,
        ControlImplementationStatus status,
        AssessmentResult result)
    {
        using var request = CreateRequest(
            HttpMethod.Patch,
            $"/api/cmmc/assessments/{assessmentId}/controls/{controlId}",
            new UpsertCmmcControlStatusRequest(status, result, [], null, new DateOnly(2026, 6, 15), "Updated status."),
            tenantId,
            Permission.ManageCmmc);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<CmmcControlStatusDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected CMMC control status response.");
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
                services.AddScoped<ICmmcAssessmentRepository, EfCmmcAssessmentRepository>();
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

    private static UpsertCmmcAssessmentRequest CreateRequestBody(string name, CmmcLevel level) =>
        new(
            name,
            AssessmentType.Readiness,
            level,
            level == CmmcLevel.Level1 ? "FAR-52.204-21" : "NIST-SP-800-171-Rev2",
            AssessmentStatus.Planned,
            new DateOnly(2026, 6, 15),
            null,
            new DateOnly(2027, 6, 15),
            "Security",
            null,
            []);

    private static ControlEntity CreateControl(string id, CmmcLevel level) =>
        new()
        {
            Id = id,
            Framework = ControlFramework.Cmmc,
            CmmcLevel = level,
            Family = id.Split('.')[0],
            Title = $"Control {id}",
            Requirement = "Readiness requirement.",
            AssessmentObjective = "Assessment objective.",
            EvidenceExamplesJson = "[]",
            SourceName = "CMMC",
            SourceUrl = "https://dodcio.defense.gov/CMMC/Resources-Documentation/",
            SourceLastReviewedAt = new DateOnly(2026, 6, 15),
            SourceConfidence = "high",
            SourceRequiresExpertReview = false
        };

    private static void SeedTenant(GccsDbContext dbContext, Guid tenantId)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = "CMMC Assessment Tenant",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}
