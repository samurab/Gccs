using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Evidence;
using Gccs.Application.Reports;
using Gccs.Domain.Audit;
using Gccs.Domain.Evidence;
using Gccs.Domain.Identity;
using Gccs.Domain.Reports;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Evidence;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Reports;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class EvidenceApprovalTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public EvidenceApprovalTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_12_3_1_Only_configured_roles_can_approve_evidence()
    {
        var tenantId = Guid.Parse("12312312-3123-1231-2312-3123123123a1");
        var evidenceItemId = Guid.Parse("12312312-3123-1231-2312-3123123123e1");
        var reviewerUserId = Guid.Parse("12312312-3123-1231-2312-3123123123b1");
        await using var factory = CreateFactory("tc-12-3-1", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            dbContext.EvidenceItems.Add(CreateEvidence(evidenceItemId, tenantId, EvidenceStatus.Submitted));
        });
        using var client = factory.CreateClient();

        using var contributorRequest = CreateRoleRequest(
            HttpMethod.Post,
            $"/api/evidence-items/{evidenceItemId}/reviews",
            new EvidenceReviewRequest(EvidenceReviewDecision.Approve, "Ready for reporting."),
            tenantId,
            reviewerUserId,
            RoleCatalog.Contributor);
        var contributorResponse = await client.SendAsync(contributorRequest);

        using var managerRequest = CreateRoleRequest(
            HttpMethod.Post,
            $"/api/evidence-items/{evidenceItemId}/reviews",
            new EvidenceReviewRequest(EvidenceReviewDecision.Approve, "Ready for reporting."),
            tenantId,
            reviewerUserId,
            RoleCatalog.ComplianceManager);
        var managerResponse = await client.SendAsync(managerRequest);
        var review = await managerResponse.Content.ReadFromJsonAsync<EvidenceReviewDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, contributorResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, managerResponse.StatusCode);
        Assert.NotNull(review);
        Assert.Equal(EvidenceStatus.Approved, review.Status);
        Assert.True(review.EligibleForReports);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var evidence = await dbContext.EvidenceItems.SingleAsync(item => item.Id == evidenceItemId);
        Assert.Equal(EvidenceStatus.Approved, evidence.Status);
        Assert.Equal(reviewerUserId, evidence.ApprovedByUserId);
        Assert.NotNull(evidence.ApprovedAt);
    }

    [Fact]
    public async Task TC_12_3_2_Rejection_without_comment_or_reason_fails_validation()
    {
        var tenantId = Guid.Parse("12312312-3123-1231-2312-3123123123a2");
        var evidenceItemId = Guid.Parse("12312312-3123-1231-2312-3123123123e2");
        await using var factory = CreateFactory("tc-12-3-2", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            dbContext.EvidenceItems.Add(CreateEvidence(evidenceItemId, tenantId, EvidenceStatus.Submitted));
        });
        using var client = factory.CreateClient();

        using var request = CreateRoleRequest(
            HttpMethod.Post,
            $"/api/evidence-items/{evidenceItemId}/reviews",
            new EvidenceReviewRequest(EvidenceReviewDecision.Reject, " "),
            tenantId,
            Guid.NewGuid(),
            RoleCatalog.ComplianceManager);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(EvidenceStatus.Submitted, (await dbContext.EvidenceItems.SingleAsync(item => item.Id == evidenceItemId)).Status);
    }

    [Fact]
    public async Task TC_12_3_3_Approved_evidence_is_included_in_report_packages()
    {
        var tenantId = Guid.Parse("12312312-3123-1231-2312-3123123123a3");
        var reportId = Guid.Parse("12312312-3123-1231-2312-3123123123c3");
        var evidenceItemId = Guid.Parse("12312312-3123-1231-2312-3123123123e3");
        await using var factory = CreateFactory("tc-12-3-3", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            dbContext.Reports.Add(CreateEvidencePackage(reportId, tenantId));
            dbContext.EvidenceItems.Add(CreateEvidence(evidenceItemId, tenantId, EvidenceStatus.Submitted));
            dbContext.Set<ReportEvidenceEntity>().Add(new ReportEvidenceEntity
            {
                ReportId = reportId,
                EvidenceItemId = evidenceItemId
            });
        });
        using var client = factory.CreateClient();

        await ReviewAsync(client, tenantId, evidenceItemId, EvidenceReviewDecision.Approve, "Approved for package.");
        using var packagesRequest = CreateRoleRequest<object?>(
            HttpMethod.Get,
            "/api/reports/approved-evidence-packages",
            null,
            tenantId,
            Guid.NewGuid(),
            RoleCatalog.Auditor);
        var packagesResponse = await client.SendAsync(packagesRequest);
        var packages = await packagesResponse.Content.ReadFromJsonAsync<ApprovedEvidencePackageDto[]>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, packagesResponse.StatusCode);
        var package = Assert.Single(packages ?? []);
        var evidence = Assert.Single(package.EvidenceItems);
        Assert.Equal(evidenceItemId, evidence.EvidenceItemId);
        Assert.Equal(EvidenceStatus.Approved, evidence.Status);
        Assert.NotNull(evidence.ApprovedAt);
    }

    [Fact]
    public async Task TC_12_3_4_Approve_reject_archive_and_expire_decisions_are_audit_logged()
    {
        var tenantId = Guid.Parse("12312312-3123-1231-2312-3123123123a4");
        var approveId = Guid.Parse("12312312-3123-1231-2312-3123123123e4");
        var rejectId = Guid.Parse("12312312-3123-1231-2312-3123123123e5");
        var archiveId = Guid.Parse("12312312-3123-1231-2312-3123123123e6");
        var expireId = Guid.Parse("12312312-3123-1231-2312-3123123123e7");
        await using var factory = CreateFactory("tc-12-3-4", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            dbContext.EvidenceItems.AddRange(
                CreateEvidence(approveId, tenantId, EvidenceStatus.Submitted),
                CreateEvidence(rejectId, tenantId, EvidenceStatus.Submitted),
                CreateEvidence(archiveId, tenantId, EvidenceStatus.Approved),
                CreateEvidence(expireId, tenantId, EvidenceStatus.Approved));
        });
        using var client = factory.CreateClient();

        await ReviewAsync(client, tenantId, approveId, EvidenceReviewDecision.Approve, "Approved.");
        await ReviewAsync(client, tenantId, rejectId, EvidenceReviewDecision.Reject, "Screenshot is stale.");
        await ReviewAsync(client, tenantId, archiveId, EvidenceReviewDecision.Archive, null);
        await ReviewAsync(client, tenantId, expireId, EvidenceReviewDecision.Expire, null);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var audits = await dbContext.AuditLogEntries
            .Where(audit => audit.TenantId == tenantId && audit.EntityType == "EvidenceItem")
            .ToArrayAsync();
        Assert.Contains(audits, audit => audit.Action == AuditAction.Approved && audit.EntityId == approveId.ToString());
        Assert.Contains(audits, audit => audit.Action == AuditAction.Rejected && audit.EntityId == rejectId.ToString());
        Assert.Contains(audits, audit => audit.Action == AuditAction.Archived && audit.EntityId == archiveId.ToString());
        Assert.Contains(audits, audit => audit.Action == AuditAction.Expired && audit.EntityId == expireId.ToString());
    }

    private static async Task<EvidenceReviewDto> ReviewAsync(
        HttpClient client,
        Guid tenantId,
        Guid evidenceItemId,
        EvidenceReviewDecision decision,
        string? comment)
    {
        using var request = CreateRoleRequest(
            HttpMethod.Post,
            $"/api/evidence-items/{evidenceItemId}/reviews",
            new EvidenceReviewRequest(decision, comment),
            tenantId,
            Guid.NewGuid(),
            RoleCatalog.ComplianceManager);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<EvidenceReviewDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected evidence review response.");
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<EvidenceApprovalService>();
                services.AddScoped<EvidenceMetadataService>();
                services.AddScoped<IEvidenceMetadataRepository, EfEvidenceMetadataRepository>();
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

    private static HttpRequestMessage CreateRoleRequest<TContent>(
        HttpMethod method,
        string requestUri,
        TContent content,
        Guid tenantId,
        Guid userId,
        string roleName)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        request.Headers.Add("X-Gccs-Dev-Role", roleName);
        if (content is not null)
        {
            request.Content = JsonContent.Create(content, options: JsonOptions);
        }

        return request;
    }

    private static EvidenceItemEntity CreateEvidence(Guid evidenceItemId, Guid tenantId, EvidenceStatus status) =>
        new()
        {
            Id = evidenceItemId,
            TenantId = tenantId,
            Name = $"Evidence {evidenceItemId:N}",
            Description = "Evidence pending review.",
            Type = EvidenceType.Policy,
            OwnerFunction = "Compliance",
            Status = status,
            TagsJson = "[]",
            ApprovedAt = status == EvidenceStatus.Approved ? DateTimeOffset.Parse("2026-06-15T12:00:00Z") : null,
            ApprovedByUserId = status == EvidenceStatus.Approved ? Guid.Parse("12312312-3123-1231-2312-312312312398") : null,
            CreatedAt = DateTimeOffset.Parse("2026-06-15T10:00:00Z"),
            CreatedByUserId = Guid.Parse("12312312-3123-1231-2312-312312312397")
        };

    private static ReportEntity CreateEvidencePackage(Guid reportId, Guid tenantId) =>
        new()
        {
            Id = reportId,
            TenantId = tenantId,
            Type = ReportType.PrimeEvidencePackage,
            Title = "Prime evidence package",
            Status = ReportStatus.Complete,
            GeneratedAt = DateTimeOffset.Parse("2026-06-15T11:00:00Z"),
            GeneratedByUserId = Guid.Parse("12312312-3123-1231-2312-312312312399"),
            CreatedAt = DateTimeOffset.Parse("2026-06-15T11:00:00Z"),
            CreatedByUserId = Guid.Parse("12312312-3123-1231-2312-312312312399")
        };

    private static void SeedTenant(GccsDbContext dbContext, Guid tenantId)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = "Evidence Approval Tenant",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}
