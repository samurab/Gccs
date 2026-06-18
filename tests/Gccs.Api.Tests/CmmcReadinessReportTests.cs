using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Reports;
using Gccs.Domain.Audit;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Domain.Evidence;
using Gccs.Domain.Identity;
using Gccs.Domain.Reports;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Reports;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class CmmcReadinessReportTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public CmmcReadinessReportTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_15_3_1_Cmmc_readiness_report_rolls_up_control_status_by_family()
    {
        var ids = StoryIds.ForCase("tc-15-3-1");
        await using var factory = CreateFactory("tc-15-3-1", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var report = await GenerateReportAsync(client, ids.TenantId, ids.AssessmentId, includeEvidencePermission: true);

        var accessFamily = Assert.Single(report.Snapshot.ProgressByFamily, family => family.Family == "Access Control");
        Assert.Equal(2, accessFamily.Total);
        Assert.Equal(1, accessFamily.Implemented);
        Assert.Equal(1, accessFamily.Partial);
        var incidentFamily = Assert.Single(report.Snapshot.ProgressByFamily, family => family.Family == "Incident Response");
        Assert.Equal(1, incidentFamily.NotStarted);
    }

    [Fact]
    public async Task TC_15_3_2_Cmmc_readiness_report_includes_poam_gaps_evidence_and_affirmations()
    {
        var ids = StoryIds.ForCase("tc-15-3-2");
        await using var factory = CreateFactory("tc-15-3-2", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var report = await GenerateReportAsync(client, ids.TenantId, ids.AssessmentId, includeEvidencePermission: true);

        Assert.Contains(report.Snapshot.OpenGaps, gap => gap.ControlId == "AC.L1-3.1.2");
        Assert.Contains(report.Snapshot.OpenPoamItems, poam => poam.ControlId == "AC.L1-3.1.2" && poam.Status == PoamStatus.Open);
        Assert.Contains(report.Snapshot.EvidenceLinks, evidence => evidence.EvidenceItemId == ids.EvidenceItemId && evidence.ControlId == "AC.L1-3.1.1");
        Assert.Contains(report.Snapshot.Affirmations, affirmation => affirmation.Level == CmmcLevel.Level1 && affirmation.Status == AffirmationStatus.DueSoon);
        Assert.Contains("CMMC readiness report", report.ExportHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TC_15_3_3_Cmmc_readiness_report_omits_evidence_links_without_evidence_permission()
    {
        var ids = StoryIds.ForCase("tc-15-3-3");
        await using var factory = CreateFactory("tc-15-3-3", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var restricted = await GenerateReportAsync(client, ids.TenantId, ids.AssessmentId, includeEvidencePermission: false);
        var allowed = await GenerateReportAsync(client, ids.TenantId, ids.AssessmentId, includeEvidencePermission: true);

        Assert.Empty(restricted.Snapshot.EvidenceLinks);
        Assert.NotEmpty(allowed.Snapshot.EvidenceLinks);
    }

    [Fact]
    public async Task TC_15_3_4_Cmmc_readiness_report_is_rbac_protected_and_retains_snapshots()
    {
        var ids = StoryIds.ForCase("tc-15-3-4");
        await using var factory = CreateFactory("tc-15-3-4", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        using var denied = CreateRequest<object?>(HttpMethod.Post, $"/api/reports/cmmc-readiness?assessmentId={ids.AssessmentId}", null, ids.TenantId, Permission.ViewCmmc);
        var deniedResponse = await client.SendAsync(denied);
        var first = await GenerateReportAsync(client, ids.TenantId, ids.AssessmentId, includeEvidencePermission: true);
        var second = await GenerateReportAsync(client, ids.TenantId, ids.AssessmentId, includeEvidencePermission: true);

        Assert.Equal(HttpStatusCode.Forbidden, deniedResponse.StatusCode);
        Assert.Equal(1, first.Snapshot.SnapshotHistoryCount);
        Assert.Equal(2, second.Snapshot.SnapshotHistoryCount);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(2, await dbContext.Reports.CountAsync(report => report.TenantId == ids.TenantId && report.Type == ReportType.CmmcReadiness));
        Assert.Contains(await dbContext.AuditLogEntries.Where(audit => audit.TenantId == ids.TenantId).ToArrayAsync(), audit =>
            audit.EntityType == "Report" && audit.Action == AuditAction.Created && audit.MetadataJson.Contains("CmmcReadiness", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TC_27_4_1_through_TC_27_4_5_Level_2_readiness_report_has_draft_scoped_sections_and_audit()
    {
        var ids = StoryIds.ForCase("tc-27-4");
        await using var factory = CreateFactory("tc-27-4", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var report = await GenerateReportAsync(client, ids.TenantId, ids.AssessmentId, includeEvidencePermission: true);

        Assert.Equal(CmmcLevel.Level2, report.Snapshot.TargetLevel);
        Assert.Equal("CMMC", report.Snapshot.ControlVersion);
        Assert.Equal("CMMC Report Tenant", report.Snapshot.TenantName);
        Assert.NotEqual(Guid.Empty, report.Snapshot.ReviewerUserId);
        Assert.Contains(report.Snapshot.ControlStatuses, row =>
            row.ControlId == "AC.L1-3.1.1" &&
            row.EvidenceStatus == "Approved" &&
            row.SourceUrl == "https://example.test/cmmc");
        Assert.Contains(report.Snapshot.PrioritizedGaps, gap =>
            gap.ControlId == "AC.L1-3.1.2" &&
            gap.ReasonCodes.Contains("control-status-gap"));
        Assert.Contains(report.Snapshot.OpenPoamItems, poam => poam.ControlId == "AC.L1-3.1.2");
        Assert.Contains(report.Snapshot.ResponsibilityMatrix, row =>
            row.ControlId == "AC.L1-3.1.2" &&
            row.ResponsibilityType == ControlResponsibilityType.Shared &&
            row.Provider == "Secure MSP");
        Assert.Contains(report.Snapshot.SourceReferences, source =>
            source.ControlId == "AC.L1-3.1.1" &&
            source.LastReviewedAt == new DateOnly(2026, 6, 15));
        Assert.Contains("Draft readiness tracking only", report.ExportHtml, StringComparison.Ordinal);
        Assert.Contains("Responsibility matrix", report.ExportHtml, StringComparison.Ordinal);
        Assert.Contains("Source references", report.ExportHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("pass", report.ExportHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fail", report.ExportHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("certification", report.ExportHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Other tenant evidence", report.ExportHtml, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Contains(await dbContext.AuditLogEntries.Where(audit => audit.TenantId == ids.TenantId).ToArrayAsync(), audit =>
            audit.EntityType == "Report" && audit.Action == AuditAction.Created && audit.MetadataJson.Contains(ids.AssessmentId.ToString(), StringComparison.Ordinal));
    }

    private static async Task<CmmcReadinessReportDto> GenerateReportAsync(
        HttpClient client,
        Guid tenantId,
        Guid assessmentId,
        bool includeEvidencePermission)
    {
        var permissions = includeEvidencePermission
            ? [Permission.ViewReports, Permission.ViewEvidence]
            : new[] { Permission.ViewReports };
        using var request = CreateRequest<object?>(HttpMethod.Post, $"/api/reports/cmmc-readiness?assessmentId={assessmentId}", null, tenantId, permissions);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<CmmcReadinessReportDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected CMMC readiness report response.");
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<CmmcReadinessReportService>();
                services.AddScoped<IReportRepository, EfReportRepository>();
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
        dbContext.Tenants.AddRange(new TenantEntity
        {
            Id = ids.TenantId,
            Name = "CMMC Report Tenant",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        },
        new TenantEntity
        {
            Id = ids.OtherTenantId,
            Name = "Other CMMC Report Tenant",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Controls.AddRange(
            CreateControl("AC.L1-3.1.1", "Access Control", "Limit access", CmmcLevel.Level1),
            CreateControl("AC.L1-3.1.2", "Access Control", "Limit transactions", CmmcLevel.Level1),
            CreateControl("IR.L1-3.6.1", "Incident Response", "Report incidents", CmmcLevel.Level1));
        dbContext.EvidenceItems.Add(new EvidenceItemEntity
        {
            Id = ids.EvidenceItemId,
            TenantId = ids.TenantId,
            Name = "MFA configuration",
            Description = "Accessible evidence link.",
            Type = EvidenceType.SystemConfiguration,
            OwnerFunction = "Security",
            Status = EvidenceStatus.Approved,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.EvidenceItems.Add(new EvidenceItemEntity
        {
            Id = ids.OtherTenantEvidenceItemId,
            TenantId = ids.OtherTenantId,
            Name = "Other tenant evidence",
            Description = "Must not appear in tenant report.",
            Type = EvidenceType.SystemConfiguration,
            OwnerFunction = "Security",
            Status = EvidenceStatus.Approved,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Assessments.Add(new AssessmentEntity
        {
            Id = ids.AssessmentId,
            TenantId = ids.TenantId,
            Name = "Level 2 readiness",
            Type = AssessmentType.Readiness,
            Level = CmmcLevel.Level2,
            Framework = "CMMC",
            Status = AssessmentStatus.InProgress,
            StartedAt = new DateOnly(2026, 6, 15),
            OwnerFunction = "Security",
            CreatedAt = DateTimeOffset.UtcNow,
            Controls =
            [
                new ControlAssessmentEntity
                {
                    AssessmentId = ids.AssessmentId,
                    ControlId = "AC.L1-3.1.1",
                    ImplementationStatus = ControlImplementationStatus.Implemented,
                    Result = AssessmentResult.Met,
                    ImplementationDetails = "MFA evidence reviewed.",
                    OwnerFunction = "Security",
                    EvidenceItemIdsJson = JsonSerializer.Serialize(new[] { ids.EvidenceItemId }, JsonOptions)
                },
                new ControlAssessmentEntity
                {
                    AssessmentId = ids.AssessmentId,
                    ControlId = "AC.L1-3.1.2",
                    ImplementationStatus = ControlImplementationStatus.PartiallyImplemented,
                    Result = AssessmentResult.NotMet,
                    ResponsibilityType = ControlResponsibilityType.Shared,
                    OwnerFunction = "Security",
                    ResponsibilityProvider = "Secure MSP",
                    ResponsibilityNotes = "Shared operations."
                }
            ]
        });
        dbContext.PoamItems.Add(new PoamItemEntity
        {
            Id = ids.PoamItemId,
            TenantId = ids.TenantId,
            AssessmentId = ids.AssessmentId,
            ControlId = "AC.L1-3.1.2",
            Weakness = "Document access review cadence.",
            PlannedRemediation = "Define and review access quarterly.",
            RiskLevel = RiskLevel.High,
            Status = PoamStatus.Open,
            OwnerFunction = "Security",
            TargetCompletionAt = new DateOnly(2026, 8, 31),
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.AnnualAffirmations.Add(new AnnualAffirmationEntity
        {
            Id = ids.AffirmationId,
            TenantId = ids.TenantId,
            Level = CmmcLevel.Level1,
            DueAt = new DateOnly(2026, 9, 30),
            Status = AffirmationStatus.DueSoon,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private static ControlEntity CreateControl(string id, string family, string title, CmmcLevel level) =>
        new()
        {
            Id = id,
            Framework = ControlFramework.Cmmc,
            CmmcLevel = level,
            Family = family,
            Title = title,
            Requirement = title,
            AssessmentObjective = title,
            SourceName = "CMMC",
            SourceUrl = "https://example.test/cmmc",
            SourceLastReviewedAt = new DateOnly(2026, 6, 15),
            SourceConfidence = "high"
        };

    private sealed record StoryIds(Guid TenantId, Guid OtherTenantId, Guid AssessmentId, Guid EvidenceItemId, Guid OtherTenantEvidenceItemId, Guid PoamItemId, Guid AffirmationId)
    {
        public static StoryIds ForCase(string caseName)
        {
            var suffix = Math.Abs(caseName.GetHashCode(StringComparison.Ordinal)) % 10000;
            return new StoryIds(
                Guid.Parse($"15315315-3153-1531-5315-31531531{suffix:D4}"),
                Guid.Parse($"15315315-3153-1531-5315-31531536{suffix:D4}"),
                Guid.Parse($"15315315-3153-1531-5315-31531532{suffix:D4}"),
                Guid.Parse($"15315315-3153-1531-5315-31531533{suffix:D4}"),
                Guid.Parse($"15315315-3153-1531-5315-31531537{suffix:D4}"),
                Guid.Parse($"15315315-3153-1531-5315-31531534{suffix:D4}"),
                Guid.Parse($"15315315-3153-1531-5315-31531535{suffix:D4}"));
        }
    }
}
