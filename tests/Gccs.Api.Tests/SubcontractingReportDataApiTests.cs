using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Reports;
using Gccs.Domain.Compliance;
using Gccs.Domain.Contracts;
using Gccs.Domain.Companies;
using Gccs.Domain.Evidence;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Domain.Vendors;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class SubcontractingReportDataApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private readonly WebApplicationFactory<Program> factory;
    public SubcontractingReportDataApiTests(WebApplicationFactory<Program> factory) => this.factory = factory;

    [Fact]
    public async Task TC_31_2_1_3_5_Create_persists_tenant_links_evidence_and_audit()
    {
        var ids = Ids.Create(); await using var app = CreateFactory(nameof(TC_31_2_1_3_5_Create_persists_tenant_links_evidence_and_audit), ids);
        using var client = app.CreateClient(); var response = await client.SendAsync(Request(HttpMethod.Post,
            $"/api/contracts/{ids.ContractId}/esrs-report-data", ValidRequest(ids), ids.TenantId, Permission.ManageReports));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var row = Assert.IsType<SubcontractingReportDataRowDto>(await response.Content.ReadFromJsonAsync<SubcontractingReportDataRowDto>(JsonOptions));
        Assert.Equal(ids.ContractId, row.ContractId); Assert.Equal(ids.SubcontractorId, row.SubcontractorId);
        Assert.Equal([ids.EvidenceId], row.SupportingEvidenceItemIds); Assert.Equal(SubcontractingReportDataReviewStatus.Draft, row.ReviewStatus);
        Assert.False(row.IsPackageEligible);
        await using var scope = app.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.True(await db.SubcontractingReportDataRows.AnyAsync(item => item.Id == row.Id && item.TenantId == ids.TenantId));
        Assert.True(await db.SubcontractingReportDataEvidence.AnyAsync(item => item.ReportDataRowId == row.Id && item.EvidenceItemId == ids.EvidenceId));
        Assert.True(await db.AuditLogEntries.AnyAsync(item => item.EntityType == "SubcontractingReportDataRow" && item.EntityId == row.Id.ToString()));
    }

    [Fact]
    public async Task TC_31_2_2_Rejects_bad_duplicate_cross_tenant_and_mismatched_data_without_writes()
    {
        var ids = Ids.Create(); await using var app = CreateFactory(nameof(TC_31_2_2_Rejects_bad_duplicate_cross_tenant_and_mismatched_data_without_writes), ids);
        using var client = app.CreateClient();
        foreach (var request in new[] {
            ValidRequest(ids) with { Amount = -1 }, ValidRequest(ids) with { SocioeconomicCategory = " " },
            ValidRequest(ids) with { ReportPeriodStart = new(2025, 10, 1) }
        })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(Request(HttpMethod.Post,
                $"/api/contracts/{ids.ContractId}/esrs-report-data", request, ids.TenantId, Permission.ManageReports))).StatusCode);
        foreach (var request in new[] { ValidRequest(ids) with { SubcontractorId = ids.OtherSubcontractorId },
            ValidRequest(ids) with { SupportingEvidenceItemIds = [ids.OtherEvidenceId] } })
            Assert.Equal(HttpStatusCode.NotFound, (await client.SendAsync(Request(HttpMethod.Post,
                $"/api/contracts/{ids.ContractId}/esrs-report-data", request, ids.TenantId, Permission.ManageReports))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.SendAsync(Request(HttpMethod.Post,
            $"/api/contracts/{ids.ContractId}/esrs-report-data", ValidRequest(ids), ids.TenantId, Permission.ManageReports))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(Request(HttpMethod.Post,
            $"/api/contracts/{ids.ContractId}/esrs-report-data", ValidRequest(ids), ids.TenantId, Permission.ManageReports))).StatusCode);
        await using var scope = app.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(1, await db.SubcontractingReportDataRows.CountAsync());
        Assert.Equal(1, await db.AuditLogEntries.CountAsync(item => item.EntityType == "SubcontractingReportDataRow"));
    }

    [Fact]
    public async Task TC_31_2_4_Final_package_is_blocked_until_explicit_acceptance_and_edit_resets_review()
    {
        var ids = Ids.Create(); await using var app = CreateFactory(nameof(TC_31_2_4_Final_package_is_blocked_until_explicit_acceptance_and_edit_resets_review), ids);
        using var client = app.CreateClient(); var row = await CreateAsync(client, ids);
        var eligibility = await client.SendAsync(Request<object>(HttpMethod.Get,
            $"/api/contracts/{ids.ContractId}/esrs-report-data/package-eligibility?reportType=Isr&periodStart=2026-01-01&periodEnd=2026-03-31",
            null, ids.TenantId, Permission.ViewReports));
        Assert.Contains("\"eligible\":false", await eligibility.Content.ReadAsStringAsync());
        var acceptedResponse = await client.SendAsync(Request(HttpMethod.Patch,
            $"/api/contracts/{ids.ContractId}/esrs-report-data/{row.Id}/review",
            new SubcontractingReportDataReviewRequest(SubcontractingReportDataReviewStatus.Accepted, "Accepted for package preparation.", row.Version),
            ids.TenantId, Permission.ManageReports));
        var accepted = Assert.IsType<SubcontractingReportDataRowDto>(await acceptedResponse.Content.ReadFromJsonAsync<SubcontractingReportDataRowDto>(JsonOptions));
        Assert.True(accepted.IsPackageEligible); Assert.Equal(ids.UserId, accepted.ReviewedByUserId); Assert.NotNull(accepted.ReviewedAt);
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            var reviewerForeignKey = db.Model.FindEntityType(typeof(SubcontractingReportDataRowEntity))!.GetForeignKeys()
                .Single(foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(UserEntity));
            Assert.Equal([nameof(SubcontractingReportDataRowEntity.ReviewedByUserId)], reviewerForeignKey.Properties.Select(property => property.Name));
            Assert.True(await db.AuditLogEntries.AnyAsync(item => item.EntityId == row.Id.ToString() && item.Action == Gccs.Domain.Audit.AuditAction.Approved));
        }
        var updatedResponse = await client.SendAsync(Request(HttpMethod.Put,
            $"/api/contracts/{ids.ContractId}/esrs-report-data/{row.Id}", ValidRequest(ids) with { Amount = 13000, ExpectedVersion = accepted.Version },
            ids.TenantId, Permission.ManageReports));
        var updated = Assert.IsType<SubcontractingReportDataRowDto>(await updatedResponse.Content.ReadFromJsonAsync<SubcontractingReportDataRowDto>(JsonOptions));
        Assert.Equal(SubcontractingReportDataReviewStatus.PendingReview, updated.ReviewStatus); Assert.False(updated.IsPackageEligible);
        Assert.Null(updated.ReviewedByUserId); Assert.Null(updated.ReviewedAt);
    }

    [Fact]
    public async Task Tenant_scope_and_server_RBAC_fail_closed()
    {
        var ids = Ids.Create(); await using var app = CreateFactory(nameof(Tenant_scope_and_server_RBAC_fail_closed), ids);
        using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(Request(HttpMethod.Post,
            $"/api/contracts/{ids.ContractId}/esrs-report-data", ValidRequest(ids), ids.TenantId, Permission.ViewReports))).StatusCode);
        var row = await CreateAsync(client, ids);
        Assert.Equal(HttpStatusCode.NotFound, (await client.SendAsync(Request<object>(HttpMethod.Get,
            $"/api/contracts/{ids.ContractId}/esrs-report-data/{row.Id}", null, ids.OtherTenantId, Permission.ViewReports))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.SendAsync(Request(HttpMethod.Patch,
            $"/api/contracts/{ids.ContractId}/esrs-report-data/{row.Id}/review",
            new SubcontractingReportDataReviewRequest(SubcontractingReportDataReviewStatus.Accepted, null, row.Version),
            ids.OtherTenantId, Permission.ManageReports))).StatusCode);
        await using var scope = app.Services.CreateAsyncScope(); var stored = await scope.ServiceProvider.GetRequiredService<GccsDbContext>()
            .SubcontractingReportDataRows.SingleAsync(item => item.Id == row.Id);
        Assert.Equal(SubcontractingReportDataReviewStatus.Draft, stored.ReviewStatus);
    }

    [Fact]
    public async Task Import_template_and_csv_import_use_the_validated_contract()
    {
        var ids = Ids.Create(); await using var app = CreateFactory(nameof(Import_template_and_csv_import_use_the_validated_contract), ids);
        using var client = app.CreateClient();
        var template = await client.SendAsync(Request<object>(HttpMethod.Get, "/api/esrs/report-data/import-template", null, ids.TenantId, Permission.ViewReports));
        Assert.Equal("text/csv", template.Content.Headers.ContentType?.MediaType); Assert.Contains("sourceReference", await template.Content.ReadAsStringAsync());
        var csv = SubcontractingReportDataService.GetImportTemplate().CsvContent +
            $"{ids.ContractId},{ids.SubcontractorId},Isr,2026-01-01,2026-03-31,2026-01-01,2026-03-31,Small Business,Direct spend,250.50,{ids.EvidenceId},FAR 52.219-9\n";
        var imported = await client.SendAsync(Request(HttpMethod.Post, "/api/esrs/report-data/import",
            new SubcontractingReportDataImportRequest(csv), ids.TenantId, Permission.ManageReports));
        Assert.Equal(HttpStatusCode.OK, imported.StatusCode);
        Assert.Single((await imported.Content.ReadFromJsonAsync<SubcontractingReportDataRowDto[]>(JsonOptions))!);
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task PostgreSQL_audit_failure_rolls_back_report_row_and_evidence_links()
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")
            ?? throw new InvalidOperationException("GCCS_TEST_POSTGRES_CONNECTION is required.");
        var ids = new Ids(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await using var app = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<GccsDbContext>(); services.RemoveAll<DbContextOptions<GccsDbContext>>(); services.RemoveAll<IAuditEventWriter>();
                services.AddDbContext<GccsDbContext>(options => options.UseGccsPostgres(connectionString));
                services.AddScoped<IAuditEventWriter, FailingAuditWriter>();
                using var provider = services.BuildServiceProvider(); using var scope = provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>(); PostgresTestDatabase.Migrate(db);
                db.Tenants.Add(Tenant(ids.TenantId));
                db.Users.Add(new UserEntity { Id = ids.UserId, TenantId = ids.TenantId, PreferredTenantId = ids.TenantId,
                    Email = $"report-{ids.UserId:N}@example.test", DisplayName = "Report reviewer", Status = UserStatus.Active, CreatedAt = DateTimeOffset.UtcNow });
                db.TenantMemberships.Add(new TenantMembershipEntity { Id = Guid.NewGuid(), TenantId = ids.TenantId, UserId = ids.UserId,
                    Status = MembershipStatus.Active, RoleName = "ContractsManager", CreatedAt = DateTimeOffset.UtcNow });
                db.Contracts.Add(Contract(ids.ContractId, ids.TenantId)); db.Subcontractors.Add(Subcontractor(ids.SubcontractorId, ids.TenantId));
                db.Set<ContractSubcontractorEntity>().Add(new() { ContractId = ids.ContractId, SubcontractorId = ids.SubcontractorId });
                db.EvidenceItems.Add(Evidence(ids.EvidenceId, ids.TenantId)); var taskId = Guid.NewGuid();
                db.ComplianceTasks.Add(new() { Id = taskId, TenantId = ids.TenantId, Title = "ISR report", Description = "No-CUI reporting metadata.",
                    Type = ComplianceTaskType.Report, Status = ComplianceTaskStatus.Open, RiskLevel = RiskLevel.Medium,
                    OwnerFunction = "Contracts", ContractId = ids.ContractId, CreatedAt = DateTimeOffset.UtcNow });
                db.EsrsApplicabilities.Add(new() { Id = Guid.NewGuid(), TenantId = ids.TenantId, ContractId = ids.ContractId, TaskId = taskId,
                    ContractType = "Prime", Agency = "DoD", SubcontractingPlanType = "Individual", PrimeOrLowerTierRole = "Prime",
                    ReportType = EsrsReportType.Isr, PeriodStart = new(2026, 1, 1), PeriodEnd = new(2026, 3, 31), DueDate = new(2026, 4, 30),
                    SourceClause = "FAR 52.219-9", OwnerFunction = "Contracts", ReviewedByUserId = ids.UserId,
                    ReviewedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow }); db.SaveChanges();
            });
        });
        try
        {
            using var client = app.CreateClient();
            var response = await client.SendAsync(Request(HttpMethod.Post, $"/api/contracts/{ids.ContractId}/esrs-report-data",
                ValidRequest(ids), ids.TenantId, Permission.ManageReports, ids.UserId));
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            await using var scope = app.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            Assert.False(await db.SubcontractingReportDataRows.AnyAsync(item => item.TenantId == ids.TenantId));
            Assert.False(await db.SubcontractingReportDataEvidence.AnyAsync(item => item.TenantId == ids.TenantId));
            Assert.False(await db.AuditLogEntries.AnyAsync(item => item.TenantId == ids.TenantId && item.EntityType == "SubcontractingReportDataRow"));
        }
        finally
        {
            await using var scope = app.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            await db.EsrsApplicabilities.Where(item => item.TenantId == ids.TenantId).ExecuteDeleteAsync();
            await db.ComplianceTasks.Where(item => item.TenantId == ids.TenantId).ExecuteDeleteAsync();
            await db.Set<ContractSubcontractorEntity>().Where(item => item.ContractId == ids.ContractId).ExecuteDeleteAsync();
            await db.EvidenceItems.Where(item => item.TenantId == ids.TenantId).ExecuteDeleteAsync();
            await db.Subcontractors.Where(item => item.TenantId == ids.TenantId).ExecuteDeleteAsync();
            await db.Contracts.Where(item => item.TenantId == ids.TenantId).ExecuteDeleteAsync();
            await db.TenantMemberships.Where(item => item.TenantId == ids.TenantId).ExecuteDeleteAsync();
            await db.Users.Where(item => item.TenantId == ids.TenantId).ExecuteDeleteAsync();
            await db.Tenants.Where(item => item.Id == ids.TenantId).ExecuteDeleteAsync();
        }
    }

    private static async Task<SubcontractingReportDataRowDto> CreateAsync(HttpClient client, Ids ids)
    {
        var response = await client.SendAsync(Request(HttpMethod.Post, $"/api/contracts/{ids.ContractId}/esrs-report-data", ValidRequest(ids), ids.TenantId, Permission.ManageReports));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SubcontractingReportDataRowDto>(JsonOptions))!;
    }

    private WebApplicationFactory<Program> CreateFactory(string name, Ids ids) => factory.WithWebHostBuilder(builder =>
    {
        builder.UseSetting("LocalDependencies:Enabled", "false"); builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
        builder.ConfigureServices(services =>
        {
            services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase($"esrs-report-data-{name}"));
            services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();
            using var provider = services.BuildServiceProvider(); using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>(); db.Database.EnsureDeleted(); db.Database.EnsureCreated();
            db.Tenants.AddRange(Tenant(ids.TenantId), Tenant(ids.OtherTenantId));
            db.Users.Add(new UserEntity { Id = ids.UserId, TenantId = ids.OtherTenantId, PreferredTenantId = ids.OtherTenantId,
                Email = "report-reviewer@example.test", DisplayName = "Report reviewer", Status = UserStatus.Active, CreatedAt = DateTimeOffset.UtcNow });
            db.TenantMemberships.Add(new TenantMembershipEntity { Id = Guid.NewGuid(), TenantId = ids.TenantId, UserId = ids.UserId,
                Status = MembershipStatus.Active, RoleName = "ContractsManager", CreatedAt = DateTimeOffset.UtcNow });
            db.Contracts.AddRange(Contract(ids.ContractId, ids.TenantId), Contract(ids.OtherContractId, ids.OtherTenantId));
            db.Subcontractors.AddRange(Subcontractor(ids.SubcontractorId, ids.TenantId), Subcontractor(ids.OtherSubcontractorId, ids.OtherTenantId));
            db.Set<ContractSubcontractorEntity>().Add(new() { ContractId = ids.ContractId, SubcontractorId = ids.SubcontractorId });
            db.EvidenceItems.AddRange(Evidence(ids.EvidenceId, ids.TenantId), Evidence(ids.OtherEvidenceId, ids.OtherTenantId));
            var taskId = Guid.NewGuid(); db.ComplianceTasks.Add(new() { Id = taskId, TenantId = ids.TenantId, Title = "ISR report",
                Description = "No-CUI reporting metadata.", Type = ComplianceTaskType.Report, Status = ComplianceTaskStatus.Open,
                RiskLevel = RiskLevel.Medium, OwnerFunction = "Contracts", ContractId = ids.ContractId, CreatedAt = DateTimeOffset.UtcNow });
            db.EsrsApplicabilities.Add(new() { Id = Guid.NewGuid(), TenantId = ids.TenantId, ContractId = ids.ContractId, TaskId = taskId,
                ContractType = "Prime", Agency = "DoD", SubcontractingPlanType = "Individual", PrimeOrLowerTierRole = "Prime",
                ReportType = EsrsReportType.Isr, PeriodStart = new(2026, 1, 1), PeriodEnd = new(2026, 3, 31), DueDate = new(2026, 4, 30),
                SourceClause = "FAR 52.219-9", OwnerFunction = "Contracts", ReviewedByUserId = ids.UserId,
                ReviewedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow });
            db.SaveChanges();
        });
    });

    private static TenantEntity Tenant(Guid id) => new() { Id = id, Name = $"Tenant {id:N}", Status = TenantStatus.Active, DataPosture = TenantDataPosture.NoCui, CreatedAt = DateTimeOffset.UtcNow };
    private static ContractEntity Contract(Guid id, Guid tenantId) => new() { Id = id, TenantId = tenantId, ContractNumber = $"FA-{id:N}", Title = "Report contract",
        AgencyOrPrimeName = "DoD", Relationship = ContractorRelationship.Prime, Kind = ContractKind.FixedPrice, Status = ContractStatus.Active,
        PeriodOfPerformanceStart = new(2025, 1, 1), PeriodOfPerformanceEnd = new(2027, 1, 1), PlaceOfPerformance = "Virginia",
        Description = "No-CUI metadata.", DataHandlingPosture = DataHandlingPosture.FciOnly, CreatedAt = DateTimeOffset.UtcNow };
    private static SubcontractorEntity Subcontractor(Guid id, Guid tenantId) => new() { Id = id, TenantId = tenantId, Name = "Atlas",
        Status = SubcontractorStatus.Active, RoleDescription = "Supplier", SmallBusinessStatus = "Small Business", CmmcStatus = "Unknown",
        NdaStatus = "OnFile", CreatedAt = DateTimeOffset.UtcNow };
    private static EvidenceItemEntity Evidence(Guid id, Guid tenantId) => new() { Id = id, TenantId = tenantId, Name = "Paid invoice", Description = "Metadata-only invoice reference.",
        Type = EvidenceType.Other, OwnerFunction = "Contracts", Status = EvidenceStatus.Approved, Classification = Gccs.Domain.Common.ContentClassification.Unclassified,
        CreatedAt = DateTimeOffset.UtcNow };
    private static SubcontractingReportDataRowRequest ValidRequest(Ids ids) => new(ids.ContractId, ids.SubcontractorId, EsrsReportType.Isr,
        new(2026, 1, 1), new(2026, 3, 31), new(2026, 1, 1), new(2026, 3, 31), "Small Disadvantaged Business",
        "Direct subcontract spend", 12500.25m, [ids.EvidenceId], "FAR 52.219-9");

    private static HttpRequestMessage Request<T>(HttpMethod method, string uri, T? body, Guid tenantId, Permission permission, Guid? userId = null)
    {
        var request = new HttpRequestMessage(method, uri); request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString()); request.Headers.Add("X-Gccs-Dev-User", (userId ?? Ids.User).ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", permission.ToString()); if (body is not null) request.Content = JsonContent.Create(body, options: JsonOptions); return request;
    }

    private sealed record Ids(Guid TenantId, Guid OtherTenantId, Guid ContractId, Guid OtherContractId, Guid SubcontractorId,
        Guid OtherSubcontractorId, Guid EvidenceId, Guid OtherEvidenceId, Guid UserId)
    {
        public static readonly Guid User = Guid.Parse("31231231-1231-2312-3123-1231231231ff");
        public static Ids Create() => new(Guid.Parse("31231231-1231-2312-3123-1231231231aa"), Guid.Parse("31231231-1231-2312-3123-1231231231ab"),
            Guid.Parse("31231231-1231-2312-3123-1231231231bb"), Guid.Parse("31231231-1231-2312-3123-1231231231bc"),
            Guid.Parse("31231231-1231-2312-3123-1231231231cc"), Guid.Parse("31231231-1231-2312-3123-1231231231cd"),
            Guid.Parse("31231231-1231-2312-3123-1231231231ee"), Guid.Parse("31231231-1231-2312-3123-1231231231ef"), User);
    }

    private sealed class FailingAuditWriter : IAuditEventWriter
    {
        public Task WriteAsync(Guid tenantId, Guid actorUserId, Gccs.Domain.Audit.AuditAction action, string entityType,
            string entityId, string summary, IReadOnlyDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default) => throw new AuditWriteException("Synthetic report-data audit failure.");
    }
}
