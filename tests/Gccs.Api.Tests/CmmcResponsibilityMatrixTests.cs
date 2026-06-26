using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Cmmc;
using Gccs.Application.Evidence;
using Gccs.Application.Security;
using Gccs.Domain.Audit;
using Gccs.Domain.Cmmc;
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

public sealed class CmmcResponsibilityMatrixTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public CmmcResponsibilityMatrixTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_27_2_1_and_TC_27_2_2_User_assigns_responsibility_and_matrix_shows_current_detail()
    {
        var ids = StoryIds.ForCase("tc-27-2-1");
        await using var factory = CreateFactory("tc-27-2-1", dbContext => SeedScenario(dbContext, ids, EvidenceRequestStatus.Accepted));
        using var client = factory.CreateClient();

        var updated = await UpdateControlAsync(client, ids, ResponsibilityRequest(ids));
        var matrix = await GetMatrixAsync(client, ids);

        Assert.Equal(ControlResponsibilityType.Shared, updated.ResponsibilityType);
        Assert.Equal("Security", updated.OwnerFunction);
        Assert.Equal("Secure MSP", updated.ResponsibilityProvider);
        Assert.Equal("Customer owns policy; MSP owns monitoring.", updated.ResponsibilityNotes);
        var row = Assert.Single(matrix);
        Assert.Equal(ids.ControlId, row.ControlId);
        Assert.Equal("AC", row.Family);
        Assert.Equal(ControlResponsibilityType.Shared, row.ResponsibilityType);
        Assert.Equal("Security", row.OwnerFunction);
        Assert.Equal("Secure MSP", row.Provider);
        Assert.Equal("Accepted", row.EvidenceStatus);
        Assert.Equal("Customer owns policy; MSP owns monitoring.", row.Notes);
    }

    [Fact]
    public async Task TC_27_2_3_External_or_shared_responsibility_requires_provider_or_notes()
    {
        var ids = StoryIds.ForCase("tc-27-2-3");
        await using var factory = CreateFactory("tc-27-2-3", dbContext => SeedScenario(dbContext, ids, EvidenceRequestStatus.Open));
        using var client = factory.CreateClient();

        using var invalid = CreateRequest(
            HttpMethod.Patch,
            $"/api/cmmc/assessments/{ids.AssessmentId}/controls/{ids.ControlId}",
            ResponsibilityRequest(ids) with { ResponsibilityProvider = null, ResponsibilityNotes = null },
            ids.TenantId,
            Permission.ManageCmmc);
        var invalidResponse = await client.SendAsync(invalid);

        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
    }

    [Fact]
    public async Task TC_27_2_4_Responsibility_changes_are_audit_logged()
    {
        var ids = StoryIds.ForCase("tc-27-2-4");
        await using var factory = CreateFactory("tc-27-2-4", dbContext => SeedScenario(dbContext, ids, EvidenceRequestStatus.Submitted));
        using var client = factory.CreateClient();

        await UpdateControlAsync(client, ids, ResponsibilityRequest(ids));

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var audits = await dbContext.AuditLogEntries
            .Where(audit => audit.TenantId == ids.TenantId && audit.EntityType == "ControlAssessment" && audit.EntityId == $"{ids.AssessmentId}:{ids.ControlId}")
            .ToArrayAsync();
        Assert.Contains(audits, audit => audit.Action == AuditAction.Updated && audit.MetadataJson.Contains("Shared", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TC_27_2_5_Matrix_export_reflects_current_tenant_data_and_is_tenant_scoped()
    {
        var ids = StoryIds.ForCase("tc-27-2-5");
        await using var factory = CreateFactory("tc-27-2-5", dbContext => SeedScenario(dbContext, ids, EvidenceRequestStatus.Returned));
        using var client = factory.CreateClient();
        await UpdateControlAsync(client, ids, ResponsibilityRequest(ids) with { ResponsibilityType = ControlResponsibilityType.MspEsp });

        using var otherTenant = CreateRequest(HttpMethod.Get, $"/api/cmmc/assessments/{ids.AssessmentId}/responsibility-matrix", (object?)null, ids.OtherTenantId, Permission.ViewCmmc);
        var otherTenantResponse = await client.SendAsync(otherTenant);
        using var export = CreateRequest(HttpMethod.Get, $"/api/cmmc/assessments/{ids.AssessmentId}/responsibility-matrix/export", (object?)null, ids.TenantId, Permission.ViewCmmc);
        var exportResponse = await client.SendAsync(export);
        var csv = await exportResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, otherTenantResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, exportResponse.StatusCode);
        Assert.Contains("Control,Family,Title,Responsibility Type,Owner,Provider,Evidence Status,Notes", csv, StringComparison.Ordinal);
        Assert.Contains(ids.ControlId, csv, StringComparison.Ordinal);
        Assert.Contains("MspEsp", csv, StringComparison.Ordinal);
        Assert.Contains("Returned", csv, StringComparison.Ordinal);
        Assert.Contains("Secure MSP", csv, StringComparison.Ordinal);
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

    private static async Task<CmmcResponsibilityMatrixRowDto[]> GetMatrixAsync(HttpClient client, StoryIds ids)
    {
        using var request = CreateRequest(HttpMethod.Get, $"/api/cmmc/assessments/{ids.AssessmentId}/responsibility-matrix", (object?)null, ids.TenantId, Permission.ViewCmmc);
        var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK, got {response.StatusCode}: {responseBody}");
        return JsonSerializer.Deserialize<CmmcResponsibilityMatrixRowDto[]>(responseBody, JsonOptions) ??
            throw new InvalidOperationException("Expected CMMC responsibility matrix.");
    }

    private static UpsertCmmcControlStatusRequest ResponsibilityRequest(StoryIds ids) =>
        new(
            ControlImplementationStatus.Implemented,
            AssessmentResult.Met,
            [ids.EvidenceItemId],
            [],
            [],
            [],
            Guid.Parse("27227227-2272-2722-7227-227227227199"),
            new DateOnly(2026, 8, 2),
            "Responsibility reviewed.",
            "Control implemented through shared operations.",
            false,
            null,
            true,
            "Secure MSP",
            ControlResponsibilityType.Shared,
            "Security",
            "Secure MSP",
            "Customer owns policy; MSP owns monitoring.");

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

    private static void SeedScenario(GccsDbContext dbContext, StoryIds ids, EvidenceRequestStatus evidenceStatus)
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
        dbContext.EvidenceRequests.Add(new EvidenceRequestEntity
        {
            Id = ids.EvidenceRequestId,
            TenantId = ids.TenantId,
            RequesterUserId = ids.AssessorUserId,
            DueDate = new DateOnly(2026, 9, 1),
            Status = evidenceStatus.ToString(),
            Priority = EvidenceRequestPriority.High.ToString(),
            Instructions = "Provide control ownership evidence.",
            RelatedRecordType = EvidenceRequestRelatedRecordType.Control.ToString(),
            RelatedRecordId = ids.ControlId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = ids.AssessorUserId
        });
        dbContext.EvidenceItems.Add(new EvidenceItemEntity
        {
            Id = ids.EvidenceItemId,
            TenantId = ids.TenantId,
            Name = "Control ownership evidence",
            Description = "Approved evidence supporting implemented control responsibility.",
            Type = EvidenceType.Policy,
            OwnerFunction = "Security",
            Status = EvidenceStatus.Approved,
            EffectiveAt = new DateOnly(2026, 8, 1),
            TagsJson = "[]",
            ApprovedAt = DateTimeOffset.UtcNow,
            ApprovedByUserId = ids.AssessorUserId,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Set<EvidenceControlEntity>().Add(new EvidenceControlEntity
        {
            EvidenceItemId = ids.EvidenceItemId,
            ControlId = ids.ControlId
        });
    }

    private static TenantEntity CreateTenant(Guid tenantId) =>
        new()
        {
            Id = tenantId,
            Name = $"CMMC Matrix Tenant {tenantId:N}",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private sealed record StoryIds(Guid TenantId, Guid OtherTenantId, Guid AssessmentId, string ControlId, Guid AssessorUserId, Guid EvidenceRequestId, Guid EvidenceItemId)
    {
        public static StoryIds ForCase(string caseName)
        {
            var suffix = Math.Abs(caseName.GetHashCode(StringComparison.Ordinal)) % 10000;
            return new StoryIds(
                Guid.Parse($"27227227-2272-2722-7227-22722731{suffix:D4}"),
                Guid.Parse($"27227227-2272-2722-7227-22722732{suffix:D4}"),
                Guid.Parse($"27227227-2272-2722-7227-22722733{suffix:D4}"),
                $"AC.L2-3.1.{suffix % 100:D2}",
                Guid.Parse("27227227-2272-2722-7227-227227227199"),
                Guid.Parse($"27227227-2272-2722-7227-22722734{suffix:D4}"),
                Guid.Parse($"27227227-2272-2722-7227-22722735{suffix:D4}"));
        }
    }
}
