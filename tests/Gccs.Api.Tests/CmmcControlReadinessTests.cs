using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Cmmc;
using Gccs.Application.Security;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Compliance;
using Gccs.Domain.Evidence;
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

public sealed class CmmcControlReadinessTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public CmmcControlReadinessTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_13_2_1_Level_1_controls_and_level_2_mappings_load_for_selected_scope()
    {
        var tenantId = Guid.Parse("13213212-3213-1213-2132-1321321321a1");
        await using var factory = CreateFactory("tc-13-2-1", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            SeedControls(dbContext);
        });
        using var client = factory.CreateClient();

        var level1 = await CreateAssessmentAsync(client, tenantId, CmmcLevel.Level1);
        var level2 = await CreateAssessmentAsync(client, tenantId, CmmcLevel.Level2);
        var level1Controls = await GetControlsAsync(client, tenantId, level1.Id);
        var level2Controls = await GetControlsAsync(client, tenantId, level2.Id);

        Assert.Equal(["AC.L1-3.1.1", "IA.L1-3.5.1"], level1Controls.Select(control => control.ControlId).Order().ToArray());
        Assert.Equal(["AC.L1-3.1.1", "AC.L2-3.1.3", "IA.L1-3.5.1"], level2Controls.Select(control => control.ControlId).Order().ToArray());
        Assert.All(level1Controls, control => Assert.Equal(ControlImplementationStatus.NotStarted, control.Status));
    }

    [Fact]
    public async Task TC_13_2_2_Control_status_can_be_set_to_each_readiness_state()
    {
        var tenantId = Guid.Parse("13213212-3213-1213-2132-1321321321a2");
        await using var factory = CreateFactory("tc-13-2-2", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            SeedControls(dbContext);
            SeedApprovedEvidence(dbContext, tenantId, Guid.Parse("13213212-3213-1213-2132-1321321321e2"));
        });
        using var client = factory.CreateClient();
        var assessment = await CreateAssessmentAsync(client, tenantId, CmmcLevel.Level1);
        var statuses = new[]
        {
            ControlImplementationStatus.NotStarted,
            ControlImplementationStatus.Implemented,
            ControlImplementationStatus.PartiallyImplemented,
            ControlImplementationStatus.NotApplicable,
            ControlImplementationStatus.NeedsReview
        };

        foreach (var status in statuses)
        {
            var updated = await UpdateControlAsync(
                client,
                tenantId,
                assessment.Id,
                "AC.L1-3.1.1",
                status,
                evidenceIds: status == ControlImplementationStatus.Implemented
                    ? [Guid.Parse("13213212-3213-1213-2132-1321321321e2")]
                    : []);
            Assert.Equal(status, updated.Status);
        }
    }

    [Fact]
    public async Task TC_13_2_3_Control_links_evidence_tasks_assets_and_poam_items()
    {
        var tenantId = Guid.Parse("13213212-3213-1213-2132-1321321321a3");
        var evidenceId = Guid.Parse("13213212-3213-1213-2132-1321321321e3");
        var taskId = Guid.Parse("13213212-3213-1213-2132-1321321321f3");
        var assetId = Guid.Parse("13213212-3213-1213-2132-1321321321b3");
        var poamId = Guid.Parse("13213212-3213-1213-2132-1321321321c3");
        await using var factory = CreateFactory("tc-13-2-3", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            SeedControls(dbContext);
            SeedApprovedEvidence(dbContext, tenantId, evidenceId);
            dbContext.PoamItems.Add(new PoamItemEntity
            {
                Id = poamId,
                TenantId = tenantId,
                AssessmentId = Guid.Empty,
                ControlId = "AC.L1-3.1.1",
                Weakness = "Traceability POA&M",
                PlannedRemediation = "Close gap.",
                RiskLevel = RiskLevel.Medium,
                Status = PoamStatus.Open,
                OwnerFunction = "Security",
                TargetCompletionAt = new DateOnly(2026, 8, 1),
                CreatedAt = DateTimeOffset.UtcNow
            });
        });
        using var client = factory.CreateClient();
        var assessment = await CreateAssessmentAsync(client, tenantId, CmmcLevel.Level1);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            var poam = await dbContext.PoamItems.SingleAsync(item => item.Id == poamId);
            poam.AssessmentId = assessment.Id;
            await dbContext.SaveChangesAsync();
        }

        var updated = await UpdateControlAsync(
            client,
            tenantId,
            assessment.Id,
            "AC.L1-3.1.1",
            ControlImplementationStatus.PartiallyImplemented,
            evidenceIds: [evidenceId],
            taskIds: [taskId],
            assetIds: [assetId],
            poamItemIds: [poamId]);
        var controls = await GetControlsAsync(client, tenantId, assessment.Id);
        var listed = controls.Single(control => control.ControlId == "AC.L1-3.1.1");
        var linkedEvidence = updated.LinkedEvidence ?? [];
        var openPoams = updated.OpenPoamItems ?? [];

        Assert.Equal([evidenceId], updated.EvidenceItemIds);
        Assert.Equal([evidenceId], linkedEvidence.Select(evidence => evidence.Id).ToArray());
        Assert.Equal("Approved", Assert.Single(linkedEvidence).ReviewStatus);
        Assert.Equal([poamId], openPoams.Select(poam => poam.Id).ToArray());
        Assert.Equal([taskId], listed.TaskIds);
        Assert.Equal([assetId], listed.AssetIds);
        Assert.Equal([poamId], listed.PoamItemIds);
    }

    [Fact]
    public async Task TC_13_2_4_Source_baseline_is_visible_and_status_contributes_to_progress()
    {
        var tenantId = Guid.Parse("13213212-3213-1213-2132-1321321321a4");
        await using var factory = CreateFactory("tc-13-2-4", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            SeedControls(dbContext);
            SeedApprovedEvidence(dbContext, tenantId, Guid.Parse("13213212-3213-1213-2132-1321321321e4"));
        });
        using var client = factory.CreateClient();
        var assessment = await CreateAssessmentAsync(client, tenantId, CmmcLevel.Level1);

        await UpdateControlAsync(
            client,
            tenantId,
            assessment.Id,
            "AC.L1-3.1.1",
            ControlImplementationStatus.Implemented,
            evidenceIds: [Guid.Parse("13213212-3213-1213-2132-1321321321e4")]);
        var controls = await GetControlsAsync(client, tenantId, assessment.Id);
        using var detailRequest = CreateRequest<object?>(HttpMethod.Get, $"/api/cmmc/assessments/{assessment.Id}", null, tenantId, Permission.ViewCmmc);
        var detailResponse = await client.SendAsync(detailRequest);
        var detail = await detailResponse.Content.ReadFromJsonAsync<CmmcAssessmentDto>(JsonOptions);

        var control = controls.Single(candidate => candidate.ControlId == "AC.L1-3.1.1");
        Assert.Equal("CMMC", control.SourceName);
        Assert.Equal("https://dodcio.defense.gov/CMMC/Resources-Documentation/", control.SourceUrl);
        Assert.Contains("baseline", control.Requirement, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        Assert.NotNull(detail);
        Assert.Equal(2, detail.ControlSummary.Total);
        Assert.Equal(1, detail.ControlSummary.Implemented);
        Assert.Equal(50, detail.ControlSummary.CompletionPercentage);
    }

    [Fact]
    public async Task Control_cannot_be_marked_complete_without_reviewed_evidence_and_metadata()
    {
        var tenantId = Guid.Parse("13213212-3213-1213-2132-1321321321a5");
        await using var factory = CreateFactory("control-traceability-missing", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            SeedControls(dbContext);
        });
        using var client = factory.CreateClient();
        var assessment = await CreateAssessmentAsync(client, tenantId, CmmcLevel.Level1);
        using var request = CreateRequest(
            HttpMethod.Patch,
            $"/api/cmmc/assessments/{assessment.Id}/controls/AC.L1-3.1.1",
            new UpsertCmmcControlStatusRequest(
                ControlImplementationStatus.Implemented,
                AssessmentResult.Met,
                [],
                [],
                [],
                [],
                null,
                null,
                ""),
            tenantId,
            Permission.ManageCmmc);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("linked evidence", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Control_cannot_link_cross_tenant_evidence()
    {
        var tenantAId = Guid.Parse("13213212-3213-1213-2132-1321321321a6");
        var tenantBId = Guid.Parse("13213212-3213-1213-2132-1321321321b6");
        var evidenceBId = Guid.Parse("13213212-3213-1213-2132-1321321321e6");
        await using var factory = CreateFactory("control-traceability-cross-tenant", dbContext =>
        {
            SeedTenant(dbContext, tenantAId);
            SeedTenant(dbContext, tenantBId);
            SeedControls(dbContext);
            SeedApprovedEvidence(dbContext, tenantBId, evidenceBId);
        });
        using var client = factory.CreateClient();
        var assessment = await CreateAssessmentAsync(client, tenantAId, CmmcLevel.Level1);
        using var request = CreateRequest(
            HttpMethod.Patch,
            $"/api/cmmc/assessments/{assessment.Id}/controls/AC.L1-3.1.1",
            new UpsertCmmcControlStatusRequest(
                ControlImplementationStatus.PartiallyImplemented,
                AssessmentResult.NotAssessed,
                [evidenceBId],
                [],
                [],
                [],
                null,
                new DateOnly(2026, 6, 15),
                "Attempt cross-tenant evidence link."),
            tenantAId,
            Permission.ManageCmmc);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("current tenant", body, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<CmmcAssessmentDto> CreateAssessmentAsync(HttpClient client, Guid tenantId, CmmcLevel level)
    {
        using var request = CreateRequest(
            HttpMethod.Post,
            "/api/cmmc/assessments",
            new UpsertCmmcAssessmentRequest(
                $"{level} readiness",
                AssessmentType.Readiness,
                level,
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

    private static async Task<CmmcControlStatusDto[]> GetControlsAsync(HttpClient client, Guid tenantId, Guid assessmentId)
    {
        using var request = CreateRequest<object?>(HttpMethod.Get, $"/api/cmmc/assessments/{assessmentId}/controls", null, tenantId, Permission.ViewCmmc);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<CmmcControlStatusDto[]>(JsonOptions) ?? [];
    }

    private static async Task<CmmcControlStatusDto> UpdateControlAsync(
        HttpClient client,
        Guid tenantId,
        Guid assessmentId,
        string controlId,
        ControlImplementationStatus status,
        IReadOnlyList<Guid>? evidenceIds = null,
        IReadOnlyList<Guid>? taskIds = null,
        IReadOnlyList<Guid>? assetIds = null,
        IReadOnlyList<Guid>? poamItemIds = null)
    {
        using var request = CreateRequest(
            HttpMethod.Patch,
            $"/api/cmmc/assessments/{assessmentId}/controls/{controlId}",
            new UpsertCmmcControlStatusRequest(
                status,
                status == ControlImplementationStatus.Implemented ? AssessmentResult.Met : AssessmentResult.NotAssessed,
                evidenceIds ?? [],
                taskIds ?? [],
                assetIds ?? [],
                poamItemIds ?? [],
                status == ControlImplementationStatus.Implemented
                    ? Guid.Parse("13213212-3213-1213-2132-132132132199")
                    : null,
                new DateOnly(2026, 6, 15),
                "Control readiness updated."),
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

    private static void SeedTenant(GccsDbContext dbContext, Guid tenantId)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = "CMMC Control Readiness Tenant",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private static void SeedControls(GccsDbContext dbContext)
    {
        dbContext.Controls.AddRange(
            CreateControl("AC.L1-3.1.1", CmmcLevel.Level1),
            CreateControl("IA.L1-3.5.1", CmmcLevel.Level1),
            CreateControl("AC.L2-3.1.3", CmmcLevel.Level2));
    }

    private static void SeedApprovedEvidence(GccsDbContext dbContext, Guid tenantId, Guid evidenceId)
    {
        var reviewerId = Guid.Parse("13213212-3213-1213-2132-132132132199");
        dbContext.EvidenceItems.Add(new EvidenceItemEntity
        {
            Id = evidenceId,
            TenantId = tenantId,
            Name = $"Reviewed evidence {evidenceId:N}",
            Description = "Reviewed control evidence.",
            Type = EvidenceType.Policy,
            OwnerFunction = "Security",
            Status = EvidenceStatus.Approved,
            TagsJson = "[]",
            ApprovedByUserId = reviewerId,
            ApprovedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private static ControlEntity CreateControl(string id, CmmcLevel level) =>
        new()
        {
            Id = id,
            Framework = ControlFramework.Cmmc,
            CmmcLevel = level,
            Family = id.Split('.')[0],
            Title = $"Control {id}",
            Requirement = $"Readiness baseline requirement for {id}.",
            AssessmentObjective = $"Assessment objective for {id}.",
            EvidenceExamplesJson = "[]",
            SourceName = "CMMC",
            SourceUrl = "https://dodcio.defense.gov/CMMC/Resources-Documentation/",
            SourceLastReviewedAt = new DateOnly(2026, 6, 15),
            SourceConfidence = "high",
            SourceRequiresExpertReview = false
        };
}
