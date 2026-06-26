using System.Net;
using Gccs.Application.Audit;
using Gccs.Application.Reports;
using Gccs.Domain.Audit;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Domain.Evidence;
using Gccs.Domain.Identity;
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

public sealed class SimpleReportExportTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SimpleReportExportTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Authorized_user_can_export_own_tenant_report_with_metadata()
    {
        var tenantId = Guid.Parse("74747474-7474-7474-7474-7474747474a1");
        await using var factory = CreateFactory("simple-export-authorized", dbContext => SeedScenario(dbContext, tenantId));
        using var client = factory.CreateClient();
        using var request = CreateRequest("/api/reports/exports/compliance-overview", tenantId, Permission.ViewReports);

        var response = await client.SendAsync(request);
        var csv = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains($"TenantId,{tenantId}", csv, StringComparison.Ordinal);
        Assert.Contains("TenantName,Export Tenant A", csv, StringComparison.Ordinal);
        Assert.Contains("GeneratedDate,", csv, StringComparison.Ordinal);
        Assert.Contains("GeneratedBy,", csv, StringComparison.Ordinal);
        Assert.Contains("ReportType,ComplianceOverview", csv, StringComparison.Ordinal);
        Assert.Contains("AppliedFilters,none", csv, StringComparison.Ordinal);
        Assert.Contains("ControlsTotal,1", csv, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unauthorized_user_cannot_export_domain_report_without_required_permission()
    {
        var tenantId = Guid.Parse("74747474-7474-7474-7474-7474747474a2");
        await using var factory = CreateFactory("simple-export-unauthorized", dbContext => SeedScenario(dbContext, tenantId));
        using var client = factory.CreateClient();
        using var request = CreateRequest("/api/reports/exports/evidence-inventory", tenantId, Permission.ViewReports);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Export_is_tenant_scoped_and_does_not_include_other_tenant_rows()
    {
        var tenantAId = Guid.Parse("74747474-7474-7474-7474-7474747474a3");
        var tenantBId = Guid.Parse("74747474-7474-7474-7474-7474747474b3");
        await using var factory = CreateFactory("simple-export-tenant-scope", dbContext =>
        {
            SeedScenario(dbContext, tenantAId, "A");
            SeedScenario(dbContext, tenantBId, "B");
        });
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            "/api/reports/exports/evidence-inventory",
            tenantAId,
            Permission.ViewReports,
            Permission.ViewEvidence);

        var response = await client.SendAsync(request);
        var csv = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Evidence A", csv, StringComparison.Ordinal);
        Assert.DoesNotContain("Evidence B", csv, StringComparison.Ordinal);
        Assert.DoesNotContain("s3://tenant-b", csv, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Export_creates_audit_log_entry()
    {
        var tenantId = Guid.Parse("74747474-7474-7474-7474-7474747474a4");
        await using var factory = CreateFactory("simple-export-audit", dbContext => SeedScenario(dbContext, tenantId));
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            "/api/reports/exports/poam-list",
            tenantId,
            Permission.ViewReports,
            Permission.ViewCmmc);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Contains(await dbContext.AuditLogEntries.Where(audit => audit.TenantId == tenantId).ToArrayAsync(), audit =>
            audit.Action == AuditAction.Exported &&
            audit.EntityType == "ReportExport" &&
            audit.MetadataJson.Contains("PoamList", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Empty_tenant_export_succeeds_with_metadata_and_headers()
    {
        var tenantId = Guid.Parse("74747474-7474-7474-7474-7474747474a5");
        await using var factory = CreateFactory("simple-export-empty", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "Empty Export Tenant"));
        });
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            "/api/reports/exports/poam-list",
            tenantId,
            Permission.ViewReports,
            Permission.ViewCmmc);

        var response = await client.SendAsync(request);
        var csv = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("TenantName,Empty Export Tenant", csv, StringComparison.Ordinal);
        Assert.Contains("Id,AssessmentId,ControlId,Title,RemediationPlan,Severity,Status,OwnerUserId,OwnerFunction,DueDate,CompletedAt,CreatedAt,CreatedBy", csv, StringComparison.Ordinal);
        Assert.DoesNotContain("Missing MFA evidence", csv, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Audit_log_export_is_tenant_scoped_and_requires_audit_permission()
    {
        var tenantAId = Guid.Parse("74747474-7474-7474-7474-7474747474a6");
        var tenantBId = Guid.Parse("74747474-7474-7474-7474-7474747474b6");
        await using var factory = CreateFactory("simple-export-audit-log", dbContext =>
        {
            SeedScenario(dbContext, tenantAId, "A");
            SeedScenario(dbContext, tenantBId, "B");
        });
        using var client = factory.CreateClient();
        using var deniedRequest = CreateRequest("/api/reports/exports/audit-log", tenantAId, Permission.ViewReports);
        var deniedResponse = await client.SendAsync(deniedRequest);
        using var allowedRequest = CreateRequest(
            "/api/reports/exports/audit-log",
            tenantAId,
            Permission.ViewReports,
            Permission.ViewAuditLog);

        var response = await client.SendAsync(allowedRequest);
        var csv = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, deniedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Tenant A audit event", csv, StringComparison.Ordinal);
        Assert.DoesNotContain("Tenant B audit event", csv, StringComparison.Ordinal);
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<SimpleReportExportService>();
                services.AddScoped<ISimpleReportExportRepository, EfSimpleReportExportRepository>();
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

    private static HttpRequestMessage CreateRequest(string requestUri, Guid tenantId, params Permission[] permissions)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", Guid.Parse("85858585-8585-8585-8585-858585858585").ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", string.Join(",", permissions.Select(permission => permission.ToString())));
        return request;
    }

    private static void SeedScenario(GccsDbContext dbContext, Guid tenantId, string suffix = "A")
    {
        var assessmentId = Guid.NewGuid();
        dbContext.Tenants.Add(CreateTenant(tenantId, $"Export Tenant {suffix}"));
        dbContext.Assessments.Add(new AssessmentEntity
        {
            Id = assessmentId,
            TenantId = tenantId,
            Name = $"Assessment {suffix}",
            Type = AssessmentType.Readiness,
            Level = CmmcLevel.Level1,
            Framework = "CMMC",
            Status = AssessmentStatus.InProgress,
            StartedAt = DateOnly.Parse("2026-06-01"),
            OwnerFunction = "Security",
            CreatedAt = DateTimeOffset.Parse("2026-06-20T12:00:00Z")
        });
        dbContext.ControlAssessments.Add(new ControlAssessmentEntity
        {
            AssessmentId = assessmentId,
            ControlId = $"AC.{suffix}.1",
            ImplementationStatus = ControlImplementationStatus.Implemented,
            Result = AssessmentResult.Met
        });
        dbContext.PoamItems.Add(new PoamItemEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AssessmentId = assessmentId,
            ControlId = $"AC.{suffix}.1",
            Weakness = $"Missing MFA evidence {suffix}",
            PlannedRemediation = "Collect reviewer-approved evidence.",
            RiskLevel = RiskLevel.High,
            Status = PoamStatus.Open,
            OwnerFunction = "Security",
            TargetCompletionAt = DateOnly.Parse("2026-06-30"),
            CreatedAt = DateTimeOffset.Parse("2026-06-20T12:00:00Z")
        });
        dbContext.EvidenceItems.Add(new EvidenceItemEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = $"Evidence {suffix}",
            Description = "Metadata only.",
            Type = EvidenceType.Policy,
            Status = EvidenceStatus.Approved,
            OwnerFunction = "Security",
            OriginalFileName = $"evidence-{suffix}.pdf",
            ContentType = "application/pdf",
            SizeBytes = 1024,
            StorageUri = suffix == "B" ? "s3://tenant-b/secret-file.pdf" : "s3://tenant-a/evidence.pdf",
            CreatedAt = DateTimeOffset.Parse("2026-06-20T12:00:00Z")
        });
        dbContext.AuditLogEntries.Add(new AuditLogEntryEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ActorUserId = Guid.Parse("85858585-8585-8585-8585-858585858585"),
            Action = AuditAction.Created,
            EntityType = "Tenant",
            EntityId = tenantId.ToString(),
            OccurredAt = DateTimeOffset.Parse("2026-06-20T12:00:00Z"),
            IpAddress = "127.0.0.1",
            UserAgent = "test",
            CorrelationId = $"export-{suffix}",
            Summary = $"Tenant {suffix} audit event",
            MetadataJson = "{}"
        });
    }

    private static TenantEntity CreateTenant(Guid tenantId, string name) =>
        new()
        {
            Id = tenantId,
            Name = name,
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.Parse("2026-06-20T12:00:00Z")
        };
}
