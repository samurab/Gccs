using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Reports;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Common;
using Gccs.Domain.Companies;
using Gccs.Domain.Compliance;
using Gccs.Domain.Contracts;
using Gccs.Domain.Evidence;
using Gccs.Domain.Identity;
using Gccs.Domain.Reports;
using Gccs.Domain.Tenancy;
using Gccs.Domain.Vendors;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Reports;
using Gccs.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class EvidencePackageReportTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public EvidencePackageReportTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_15_4_1_Generate_evidence_package_scoped_by_obligations_contract_controls_or_subcontractor()
    {
        var ids = StoryIds.ForCase("tc-15-4-1");
        await using var factory = CreateFactory("tc-15-4-1", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();
        var request = CreateGenerateRequest(ids, includeDraftOrRejected: false);

        var report = await GeneratePackageAsync(client, ids.TenantId, request, Permission.ManageReports);

        Assert.Equal(ReportType.PrimeEvidencePackage, report.Type);
        Assert.Equal(ReportStatus.Complete, report.Status);
        Assert.Equal(4, report.Manifest.Items.Count);
        Assert.Contains(report.Manifest.Items, item => item.EvidenceItemId == ids.ObligationEvidenceId);
        Assert.Contains(report.Manifest.Items, item => item.EvidenceItemId == ids.ContractEvidenceId);
        Assert.Contains(report.Manifest.Items, item => item.EvidenceItemId == ids.ControlEvidenceId);
        Assert.Contains(report.Manifest.Items, item => item.EvidenceItemId == ids.SubcontractorEvidenceId);
        Assert.DoesNotContain(report.Manifest.Items, item => item.EvidenceItemId == ids.OutOfScopeEvidenceId);
    }

    [Fact]
    public async Task TC_15_4_2_Approved_evidence_is_default_and_draft_rejected_require_authorized_override()
    {
        var ids = StoryIds.ForCase("tc-15-4-2");
        await using var factory = CreateFactory("tc-15-4-2", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();
        var defaultRequest = new EvidencePackageGenerateRequest
        {
            Title = "Approved-only package",
            ObligationIds = [ids.ObligationId]
        };
        var overrideRequest = defaultRequest with
        {
            Title = "Override package",
            IncludeDraftOrRejectedEvidence = true
        };

        var defaultReport = await GeneratePackageAsync(client, ids.TenantId, defaultRequest, Permission.ManageReports);
        using var deniedRequest = CreateRequest(HttpMethod.Post, "/api/reports/evidence-packages", overrideRequest, ids.TenantId, Permission.ManageReports);
        var deniedResponse = await client.SendAsync(deniedRequest);
        var overrideReport = await GeneratePackageAsync(
            client,
            ids.TenantId,
            overrideRequest,
            Permission.ManageReports,
            Permission.ApproveEvidence);

        Assert.Equal(HttpStatusCode.Forbidden, deniedResponse.StatusCode);
        Assert.All(defaultReport.Manifest.Items, item => Assert.Equal(EvidenceStatus.Approved, item.Status));
        Assert.Contains(overrideReport.Manifest.Items, item => item.Status == EvidenceStatus.Approved);
        Assert.Contains(overrideReport.Manifest.Items, item => item.Status == EvidenceStatus.Draft);
        Assert.Contains(overrideReport.Manifest.Items, item => item.Status == EvidenceStatus.Rejected);
    }

    [Fact]
    public async Task TC_15_4_3_Evidence_package_manifest_includes_required_metadata()
    {
        var ids = StoryIds.ForCase("tc-15-4-3");
        await using var factory = CreateFactory("tc-15-4-3", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();
        var request = CreateGenerateRequest(ids, includeDraftOrRejected: false);

        var report = await GeneratePackageAsync(client, ids.TenantId, request, Permission.ManageReports);

        Assert.Equal(request.Title, report.Manifest.Title);
        Assert.Equal(report.GeneratedAt, report.Manifest.GeneratedAt);
        var item = Assert.Single(report.Manifest.Items, candidate => candidate.EvidenceItemId == ids.ControlEvidenceId);
        Assert.Equal("Control evidence", item.Title);
        Assert.Equal(EvidenceType.SystemConfiguration, item.Type);
        Assert.Equal(EvidenceStatus.Approved, item.Status);
        Assert.NotNull(item.ApprovedAt);
        Assert.Contains(ids.ControlId, item.ControlIds);
        Assert.Contains(ids.ObligationId, report.Manifest.Scope.ObligationIds);
        Assert.True(item.ManifestedAt <= DateTimeOffset.UtcNow.AddMinutes(1));
        Assert.Contains("Evidence package", report.ExportHtml, StringComparison.Ordinal);
        Assert.Contains("not legal advice", report.ExportHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("a certification decision", report.ExportHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("government endorsement", report.ExportHtml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_15_4_4_Evidence_package_view_is_read_only_and_generation_is_audit_logged()
    {
        var ids = StoryIds.ForCase("tc-15-4-4");
        await using var factory = CreateFactory("tc-15-4-4", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();
        var request = CreateGenerateRequest(ids, includeDraftOrRejected: false);

        using var readOnlyCreateRequest = CreateRequest(HttpMethod.Post, "/api/reports/evidence-packages", request, ids.TenantId, Permission.ViewReports);
        var readOnlyCreateResponse = await client.SendAsync(readOnlyCreateRequest);
        var report = await GeneratePackageAsync(client, ids.TenantId, request, Permission.ManageReports);
        using var viewRequest = CreateRequest<object?>(HttpMethod.Get, $"/api/reports/evidence-packages/{report.Id}", null, ids.TenantId, Permission.ViewReports);
        var viewResponse = await client.SendAsync(viewRequest);
        var viewed = await viewResponse.Content.ReadFromJsonAsync<EvidencePackageReportDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, readOnlyCreateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, viewResponse.StatusCode);
        Assert.Equal(report.Id, viewed?.Id);
        Assert.Equal(report.Manifest.Items.Count, viewed?.Manifest.Items.Count);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Contains(await dbContext.AuditLogEntries.Where(audit => audit.TenantId == ids.TenantId).ToArrayAsync(), audit =>
            audit.EntityType == "Report" &&
            audit.Action == AuditAction.Created &&
            audit.MetadataJson.Contains("PrimeEvidencePackage", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TC_PR_0_3_NoCui_evidence_package_generation_blocks_legacy_or_direct_cui_evidence()
    {
        var ids = StoryIds.ForCase("tc-pr-0-3-report");
        await using var factory = CreateFactory("tc-pr-0-3-report", dbContext =>
        {
            SeedScenario(dbContext, ids);
            var cuiEvidence = dbContext.EvidenceItems.Local.Single(evidence => evidence.Id == ids.ObligationEvidenceId);
            cuiEvidence.Classification = ContentClassification.Cui;
            cuiEvidence.ClassificationSource = ContentClassificationSource.AdminReviewed;
            cuiEvidence.ClassificationConfidence = 1m;
            cuiEvidence.ClassificationReviewedAt = DateTimeOffset.Parse("2026-06-26T12:00:00Z");
            cuiEvidence.ClassificationReviewedByUserId = Guid.Parse("15415415-4154-1541-5415-415415419997");
            cuiEvidence.ClassificationReason = "Regression fixture simulates legacy or direct database CUI evidence.";
        });
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            HttpMethod.Post,
            "/api/reports/evidence-packages",
            new EvidencePackageGenerateRequest
            {
                Title = "Blocked CUI package",
                ObligationIds = [ids.ObligationId]
            },
            ids.TenantId,
            Permission.ManageReports);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("tenant_data_handling_mode_restricted", body, StringComparison.Ordinal);
        Assert.Contains("Report", body, StringComparison.Ordinal);
    }

    private static EvidencePackageGenerateRequest CreateGenerateRequest(StoryIds ids, bool includeDraftOrRejected) =>
        new()
        {
            Title = "Evidence package",
            ObligationIds = [ids.ObligationId],
            ContractIds = [ids.ContractId],
            ControlIds = [ids.ControlId],
            SubcontractorIds = [ids.SubcontractorId],
            IncludeDraftOrRejectedEvidence = includeDraftOrRejected
        };

    private static async Task<EvidencePackageReportDto> GeneratePackageAsync(
        HttpClient client,
        Guid tenantId,
        EvidencePackageGenerateRequest body,
        params Permission[] permissions)
    {
        using var request = CreateRequest(HttpMethod.Post, "/api/reports/evidence-packages", body, tenantId, permissions);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<EvidencePackageReportDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected evidence package response.");
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<EvidencePackageReportService>();
                services.AddScoped<IReportRepository, EfReportRepository>();
                services.AddScoped<ITenantRepository, EfTenantRepository>();
                services.AddScoped<TenantDataHandlingModePolicyService>();
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
        params Permission[] permissions)
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
        dbContext.Tenants.Add(CreateTenant(ids.TenantId));
        dbContext.Contracts.Add(CreateContract(ids.ContractId, ids.TenantId));
        dbContext.Subcontractors.Add(new SubcontractorEntity
        {
            Id = ids.SubcontractorId,
            TenantId = ids.TenantId,
            Name = "Readiness subcontractor",
            Status = SubcontractorStatus.Active,
            RoleDescription = "Performs scoped work.",
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Obligations.Add(CreateObligation(ids.ObligationId));
        dbContext.Controls.Add(new ControlEntity
        {
            Id = ids.ControlId,
            Framework = ControlFramework.Cmmc,
            CmmcLevel = CmmcLevel.Level1,
            Family = "Access Control",
            Title = "Limit access",
            Requirement = "Limit system access.",
            AssessmentObjective = "Verify access limitation.",
            SourceName = "CMMC",
            SourceUrl = "https://example.test/cmmc",
            SourceLastReviewedAt = new DateOnly(2026, 6, 15),
            SourceConfidence = "high"
        });
        dbContext.EvidenceItems.AddRange(
            CreateEvidence(ids.ObligationEvidenceId, ids.TenantId, "Obligation evidence", EvidenceType.Policy, EvidenceStatus.Approved),
            CreateEvidence(ids.ContractEvidenceId, ids.TenantId, "Contract evidence", EvidenceType.AccessReview, EvidenceStatus.Approved),
            CreateEvidence(ids.ControlEvidenceId, ids.TenantId, "Control evidence", EvidenceType.SystemConfiguration, EvidenceStatus.Approved),
            CreateEvidence(ids.SubcontractorEvidenceId, ids.TenantId, "Subcontractor evidence", EvidenceType.SubcontractorCertification, EvidenceStatus.Approved),
            CreateEvidence(ids.DraftEvidenceId, ids.TenantId, "Draft evidence", EvidenceType.Policy, EvidenceStatus.Draft),
            CreateEvidence(ids.RejectedEvidenceId, ids.TenantId, "Rejected evidence", EvidenceType.Policy, EvidenceStatus.Rejected),
            CreateEvidence(ids.OutOfScopeEvidenceId, ids.TenantId, "Out of scope evidence", EvidenceType.Policy, EvidenceStatus.Approved));
        dbContext.Set<EvidenceObligationEntity>().AddRange(
            new EvidenceObligationEntity { EvidenceItemId = ids.ObligationEvidenceId, ObligationId = ids.ObligationId },
            new EvidenceObligationEntity { EvidenceItemId = ids.DraftEvidenceId, ObligationId = ids.ObligationId },
            new EvidenceObligationEntity { EvidenceItemId = ids.RejectedEvidenceId, ObligationId = ids.ObligationId });
        dbContext.Set<EvidenceContractEntity>().Add(new EvidenceContractEntity { EvidenceItemId = ids.ContractEvidenceId, ContractId = ids.ContractId });
        dbContext.Set<EvidenceControlEntity>().Add(new EvidenceControlEntity { EvidenceItemId = ids.ControlEvidenceId, ControlId = ids.ControlId });
        dbContext.Set<SubcontractorEvidenceEntity>().Add(new SubcontractorEvidenceEntity { EvidenceItemId = ids.SubcontractorEvidenceId, SubcontractorId = ids.SubcontractorId });
    }

    private static TenantEntity CreateTenant(Guid tenantId) =>
        new()
        {
            Id = tenantId,
            Name = "Evidence Package Tenant",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static ContractEntity CreateContract(Guid contractId, Guid tenantId) =>
        new()
        {
            Id = contractId,
            TenantId = tenantId,
            ContractNumber = "FAKE-15-4",
            Title = "Evidence package contract",
            AgencyOrPrimeName = "Prime",
            Relationship = ContractorRelationship.Prime,
            Kind = ContractKind.FixedPrice,
            Status = ContractStatus.Active,
            PeriodOfPerformanceStart = new DateOnly(2026, 1, 1),
            PeriodOfPerformanceEnd = new DateOnly(2026, 12, 31),
            PlaceOfPerformance = "Remote",
            Description = "Contract scoped for evidence packaging.",
            DataHandlingPosture = DataHandlingPosture.FciOnly,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static ObligationEntity CreateObligation(string obligationId) =>
        new()
        {
            Id = obligationId,
            Source = "FAR",
            Title = "Protect FCI",
            PlainEnglishSummary = "Protect federal contract information.",
            TriggerCondition = "Contract includes FCI.",
            RequiredAction = "Maintain evidence.",
            OwnerFunction = "Compliance",
            RiskLevel = RiskLevel.High,
            SourceName = "FAR",
            SourceUrl = "https://example.test/far",
            SourceLastReviewedAt = new DateOnly(2026, 6, 15),
            LastReviewedAt = new DateOnly(2026, 6, 15),
            SourceConfidence = "high",
            Confidence = "high",
            ReviewState = ReviewState.Published
        };

    private static EvidenceItemEntity CreateEvidence(
        Guid evidenceItemId,
        Guid tenantId,
        string name,
        EvidenceType type,
        EvidenceStatus status,
        ContentClassification classification = ContentClassification.Unclassified,
        ContentClassificationSource classificationSource = ContentClassificationSource.UserSelected,
        bool isApprovedDemoContent = false) =>
        new()
        {
            Id = evidenceItemId,
            TenantId = tenantId,
            Name = name,
            Description = $"{name} description.",
            Type = type,
            OwnerFunction = "Compliance",
            Status = status,
            TagsJson = "[]",
            ApprovedAt = status == EvidenceStatus.Approved ? DateTimeOffset.Parse("2026-06-15T12:00:00Z") : null,
            ApprovedByUserId = status == EvidenceStatus.Approved ? Guid.Parse("15415415-4154-1541-5415-415415419998") : null,
            Classification = classification,
            ClassificationSource = classificationSource,
            ClassificationIsApprovedDemoContent = isApprovedDemoContent,
            CreatedAt = DateTimeOffset.Parse("2026-06-15T10:00:00Z")
        };

    private sealed record StoryIds(
        Guid TenantId,
        Guid ContractId,
        Guid SubcontractorId,
        string ObligationId,
        string ControlId,
        Guid ObligationEvidenceId,
        Guid ContractEvidenceId,
        Guid ControlEvidenceId,
        Guid SubcontractorEvidenceId,
        Guid DraftEvidenceId,
        Guid RejectedEvidenceId,
        Guid OutOfScopeEvidenceId)
    {
        public static StoryIds ForCase(string caseName)
        {
            var suffix = Math.Abs(caseName.GetHashCode(StringComparison.Ordinal)) % 10000;
            return new StoryIds(
                Guid.Parse($"15415415-4154-1541-5415-41541541{suffix:D4}"),
                Guid.Parse($"15415415-4154-1541-5415-41541542{suffix:D4}"),
                Guid.Parse($"15415415-4154-1541-5415-41541543{suffix:D4}"),
                $"OBL-15.4-{suffix:D4}",
                $"AC.L1-15.4.{suffix:D4}",
                Guid.Parse($"15415415-4154-1541-5415-41541544{suffix:D4}"),
                Guid.Parse($"15415415-4154-1541-5415-41541545{suffix:D4}"),
                Guid.Parse($"15415415-4154-1541-5415-41541546{suffix:D4}"),
                Guid.Parse($"15415415-4154-1541-5415-41541547{suffix:D4}"),
                Guid.Parse($"15415415-4154-1541-5415-41541548{suffix:D4}"),
                Guid.Parse($"15415415-4154-1541-5415-41541549{suffix:D4}"),
                Guid.Parse($"15415415-4154-1541-5415-41541550{suffix:D4}"));
        }
    }
}
