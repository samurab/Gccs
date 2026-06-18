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

public sealed class CmmcControlAssessmentDetailTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public CmmcControlAssessmentDetailTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_27_1_1_and_TC_27_1_2_Authorized_user_updates_level_2_control_detail()
    {
        var ids = StoryIds.ForCase("tc-27-1-1");
        await using var factory = CreateFactory("tc-27-1-1", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var updated = await UpdateControlAsync(client, ids, Request());

        Assert.Equal(ControlImplementationStatus.Implemented, updated.Status);
        Assert.Equal(AssessmentResult.Met, updated.Result);
        Assert.Equal("MFA enforced through IdP.", updated.ImplementationDetails);
        Assert.True(updated.IsInherited);
        Assert.Equal("Corporate IdP", updated.InheritedFrom);
        Assert.True(updated.EspResponsible);
        Assert.Equal("Secure MSP", updated.EspName);
        Assert.Equal(ids.AssessorUserId, updated.AssessedByUserId);
        Assert.Equal(new DateOnly(2026, 8, 1), updated.AssessedAt);
    }

    [Fact]
    public async Task TC_27_1_3_Status_history_is_retained()
    {
        var ids = StoryIds.ForCase("tc-27-1-3");
        await using var factory = CreateFactory("tc-27-1-3", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();
        await UpdateControlAsync(client, ids, Request() with { Status = ControlImplementationStatus.PartiallyImplemented, Result = AssessmentResult.NotMet, Notes = "First pass" });
        var updated = await UpdateControlAsync(client, ids, Request() with { Notes = "Second pass" });

        Assert.True(updated.StatusHistory.Count >= 2);
        Assert.Contains(updated.StatusHistory, history => history.Status == ControlImplementationStatus.PartiallyImplemented);
        Assert.Contains(updated.StatusHistory, history => history.Status == ControlImplementationStatus.Implemented);
    }

    [Fact]
    public async Task TC_27_1_4_Control_updates_are_tenant_scoped_and_validated()
    {
        var ids = StoryIds.ForCase("tc-27-1-4");
        await using var factory = CreateFactory("tc-27-1-4", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        using var otherTenant = CreateRequest(HttpMethod.Patch, $"/api/cmmc/assessments/{ids.AssessmentId}/controls/{ids.ControlId}", Request(), ids.OtherTenantId, Permission.ManageCmmc);
        var otherTenantResponse = await client.SendAsync(otherTenant);
        using var invalid = CreateRequest(HttpMethod.Patch, $"/api/cmmc/assessments/{ids.AssessmentId}/controls/{ids.ControlId}", Request() with { IsInherited = true, InheritedFrom = null }, ids.TenantId, Permission.ManageCmmc);
        var invalidResponse = await client.SendAsync(invalid);

        Assert.Equal(HttpStatusCode.NotFound, otherTenantResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
    }

    [Fact]
    public async Task TC_27_1_5_Control_assessment_updates_are_audit_logged()
    {
        var ids = StoryIds.ForCase("tc-27-1-5");
        await using var factory = CreateFactory("tc-27-1-5", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();
        await UpdateControlAsync(client, ids, Request());

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var audits = await dbContext.AuditLogEntries
            .Where(audit => audit.TenantId == ids.TenantId && audit.EntityType == "ControlAssessment" && audit.EntityId == $"{ids.AssessmentId}:{ids.ControlId}")
            .ToArrayAsync();
        Assert.Contains(audits, audit => audit.Action == AuditAction.Updated && audit.MetadataJson.Contains("Implemented", StringComparison.Ordinal));
    }

    private static async Task<CmmcControlStatusDto> UpdateControlAsync(HttpClient client, StoryIds ids, UpsertCmmcControlStatusRequest body)
    {
        using var request = CreateRequest(HttpMethod.Patch, $"/api/cmmc/assessments/{ids.AssessmentId}/controls/{ids.ControlId}", body, ids.TenantId, Permission.ManageCmmc);
        var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK, got {response.StatusCode}: {responseBody}");
        return await response.Content.ReadFromJsonAsync<CmmcControlStatusDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected CMMC control status.");
    }

    private static UpsertCmmcControlStatusRequest Request() =>
        new(
            ControlImplementationStatus.Implemented,
            AssessmentResult.Met,
            [],
            [],
            [],
            [],
            Guid.Parse("27127127-1271-2712-7127-127127127199"),
            new DateOnly(2026, 8, 1),
            "Validated with screenshots.",
            "MFA enforced through IdP.",
            true,
            "Corporate IdP",
            true,
            "Secure MSP");

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
            Name = "Level 2 readiness",
            Type = AssessmentType.SelfAssessment,
            Level = CmmcLevel.Level2,
            Framework = "CMMC 2.0",
            Status = AssessmentStatus.InProgress,
            StartedAt = new DateOnly(2026, 7, 1),
            OwnerFunction = "Security",
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Controls.Add(new ControlEntity
        {
            Id = ids.ControlId,
            Framework = ControlFramework.NistSp800171Revision2,
            CmmcLevel = CmmcLevel.Level2,
            Family = "AC",
            Title = "Access Control",
            Requirement = "Limit access.",
            AssessmentObjective = "Verify control implementation.",
            SourceName = "NIST",
            SourceUrl = "https://example.test/nist",
            SourceLastReviewedAt = new DateOnly(2026, 6, 17),
            SourceConfidence = "high"
        });
    }

    private static TenantEntity CreateTenant(Guid tenantId) =>
        new()
        {
            Id = tenantId,
            Name = $"CMMC Detail Tenant {tenantId:N}",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private sealed record StoryIds(Guid TenantId, Guid OtherTenantId, Guid AssessmentId, string ControlId, Guid AssessorUserId)
    {
        public static StoryIds ForCase(string caseName)
        {
            var suffix = Math.Abs(caseName.GetHashCode(StringComparison.Ordinal)) % 10000;
            return new StoryIds(
                Guid.Parse($"27127127-1271-2712-7127-12712731{suffix:D4}"),
                Guid.Parse($"27127127-1271-2712-7127-12712732{suffix:D4}"),
                Guid.Parse($"27127127-1271-2712-7127-12712733{suffix:D4}"),
                $"AC.L2-3.1.{suffix % 100:D2}",
                Guid.Parse("27127127-1271-2712-7127-127127127199"));
        }
    }
}
