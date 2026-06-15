using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Reports;
using Gccs.Domain.Audit;
using Gccs.Domain.Companies;
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
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class SubcontractorComplianceReportTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public SubcontractorComplianceReportTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_15_5_1_Generate_subcontractor_compliance_report_filtered_by_contract()
    {
        var ids = StoryIds.ForCase("tc-15-5-1");
        await using var factory = CreateFactory("tc-15-5-1", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var report = await GenerateReportAsync(client, ids.TenantId, ids.ContractId);

        Assert.Equal(ids.ContractId, report.Snapshot.ContractId);
        var row = Assert.Single(report.Snapshot.Rows);
        Assert.Equal(ids.SubcontractorId, row.SubcontractorId);
        Assert.Equal("Scoped Supplier LLC", row.Name);
        Assert.Equal("Level 1 in progress", row.CmmcStatus);
        Assert.Equal("Executed", row.NdaStatus);
    }

    [Fact]
    public async Task TC_15_5_2_Subcontractor_report_highlights_missing_and_overdue_evidence_requests()
    {
        var ids = StoryIds.ForCase("tc-15-5-2");
        await using var factory = CreateFactory("tc-15-5-2", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var report = await GenerateReportAsync(client, ids.TenantId, ids.ContractId);

        Assert.Equal(2, report.Snapshot.MissingEvidenceRequests);
        Assert.Equal(1, report.Snapshot.OverdueEvidenceRequests);
        var row = Assert.Single(report.Snapshot.Rows);
        Assert.True(row.HasMissingEvidence);
        Assert.True(row.HasOverdueEvidence);
        Assert.Contains(row.EvidenceRequests, request => request.Id == ids.OverdueRequestId && request.IsMissing && request.IsOverdue);
    }

    [Fact]
    public async Task TC_15_5_3_Subcontractor_report_includes_flow_down_statuses_by_subcontractor_and_contract()
    {
        var ids = StoryIds.ForCase("tc-15-5-3");
        await using var factory = CreateFactory("tc-15-5-3", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var report = await GenerateReportAsync(client, ids.TenantId, ids.ContractId);

        var row = Assert.Single(report.Snapshot.Rows);
        Assert.Contains(row.FlowDowns, flowDown => flowDown.Id == ids.SentFlowDownId && flowDown.Status == FlowDownStatus.Sent);
        Assert.Contains(row.FlowDowns, flowDown => flowDown.Id == ids.SignedFlowDownId && flowDown.Status == FlowDownStatus.Signed);
        Assert.Equal(1, report.Snapshot.OpenFlowDowns);
    }

    [Fact]
    public async Task TC_15_5_4_Subcontractor_report_export_is_tenant_scoped()
    {
        var ids = StoryIds.ForCase("tc-15-5-4");
        await using var factory = CreateFactory("tc-15-5-4", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var report = await GenerateReportAsync(client, ids.TenantId, ids.ContractId);

        Assert.Contains("Scoped Supplier LLC", report.ExportCsv, StringComparison.Ordinal);
        Assert.DoesNotContain("Other Contract Supplier", report.ExportCsv, StringComparison.Ordinal);
        Assert.DoesNotContain("Other Tenant Supplier", report.ExportCsv, StringComparison.Ordinal);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Contains(await dbContext.AuditLogEntries.Where(audit => audit.TenantId == ids.TenantId).ToArrayAsync(), audit =>
            audit.EntityType == "Report" &&
            audit.Action == AuditAction.Created &&
            audit.MetadataJson.Contains("SubcontractorCompliance", StringComparison.Ordinal));
    }

    private static async Task<SubcontractorComplianceReportDto> GenerateReportAsync(HttpClient client, Guid tenantId, Guid contractId)
    {
        using var request = CreateRequest<object?>(HttpMethod.Post, $"/api/reports/subcontractor-compliance?contractId={contractId}", null, tenantId, Permission.ViewReports);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<SubcontractorComplianceReportDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected subcontractor compliance report response.");
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<SubcontractorComplianceReportService>();
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

    private static void SeedScenario(GccsDbContext dbContext, StoryIds ids)
    {
        dbContext.Tenants.AddRange(CreateTenant(ids.TenantId), CreateTenant(ids.OtherTenantId));
        dbContext.Contracts.AddRange(
            CreateContract(ids.ContractId, ids.TenantId, "Scoped contract"),
            CreateContract(ids.OtherContractId, ids.TenantId, "Other contract"),
            CreateContract(ids.OtherTenantContractId, ids.OtherTenantId, "Other tenant contract"));
        dbContext.Subcontractors.AddRange(
            CreateSubcontractor(ids.SubcontractorId, ids.TenantId, "Scoped Supplier LLC"),
            CreateSubcontractor(ids.OtherContractSubcontractorId, ids.TenantId, "Other Contract Supplier"),
            CreateSubcontractor(ids.OtherTenantSubcontractorId, ids.OtherTenantId, "Other Tenant Supplier"));
        dbContext.Set<ContractSubcontractorEntity>().AddRange(
            new ContractSubcontractorEntity { ContractId = ids.ContractId, SubcontractorId = ids.SubcontractorId },
            new ContractSubcontractorEntity { ContractId = ids.OtherContractId, SubcontractorId = ids.OtherContractSubcontractorId },
            new ContractSubcontractorEntity { ContractId = ids.OtherTenantContractId, SubcontractorId = ids.OtherTenantSubcontractorId });
        dbContext.FlowDownClauses.AddRange(
            CreateFlowDown(ids.SentFlowDownId, ids.SubcontractorId, ids.ContractId, "52.204-21", FlowDownStatus.Sent),
            CreateFlowDown(ids.SignedFlowDownId, ids.SubcontractorId, ids.ContractId, "52.204-25", FlowDownStatus.Signed),
            CreateFlowDown(Guid.NewGuid(), ids.OtherContractSubcontractorId, ids.OtherContractId, "52.204-27", FlowDownStatus.Required),
            CreateFlowDown(Guid.NewGuid(), ids.OtherTenantSubcontractorId, ids.OtherTenantContractId, "52.222-41", FlowDownStatus.Required));
        dbContext.EvidenceItems.Add(new EvidenceItemEntity
        {
            Id = ids.ReceivedEvidenceId,
            TenantId = ids.TenantId,
            Name = "Received subcontractor evidence",
            Description = "Received.",
            Type = EvidenceType.SubcontractorCertification,
            OwnerFunction = "Contracts",
            Status = EvidenceStatus.Approved,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.SubcontractorEvidenceRequests.AddRange(
            new SubcontractorEvidenceRequestEntity
            {
                Id = ids.OverdueRequestId,
                TenantId = ids.TenantId,
                SubcontractorId = ids.SubcontractorId,
                RequestedItem = "Updated insurance certificate",
                RequestedEvidenceTypesJson = "[]",
                DueDate = new DateOnly(2026, 1, 1),
                Status = SubcontractorEvidenceRequestStatus.Sent,
                RelatedFlowDownClauseId = ids.SentFlowDownId,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new SubcontractorEvidenceRequestEntity
            {
                Id = ids.MissingRequestId,
                TenantId = ids.TenantId,
                SubcontractorId = ids.SubcontractorId,
                RequestedItem = "Signed flow-down acknowledgement",
                RequestedEvidenceTypesJson = "[]",
                DueDate = new DateOnly(2026, 12, 1),
                Status = SubcontractorEvidenceRequestStatus.Sent,
                RelatedFlowDownClauseId = ids.SentFlowDownId,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new SubcontractorEvidenceRequestEntity
            {
                Id = ids.SatisfiedRequestId,
                TenantId = ids.TenantId,
                SubcontractorId = ids.SubcontractorId,
                RequestedItem = "CMMC status evidence",
                RequestedEvidenceTypesJson = "[]",
                DueDate = new DateOnly(2026, 12, 1),
                Status = SubcontractorEvidenceRequestStatus.Satisfied,
                RelatedFlowDownClauseId = ids.SignedFlowDownId,
                ReceivedEvidenceItemId = ids.ReceivedEvidenceId,
                CreatedAt = DateTimeOffset.UtcNow
            });
    }

    private static TenantEntity CreateTenant(Guid tenantId) =>
        new()
        {
            Id = tenantId,
            Name = "Subcontractor Report Tenant",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static ContractEntity CreateContract(Guid contractId, Guid tenantId, string title) =>
        new()
        {
            Id = contractId,
            TenantId = tenantId,
            ContractNumber = contractId.ToString("N")[..8],
            Title = title,
            AgencyOrPrimeName = "Prime",
            Relationship = ContractorRelationship.Prime,
            Kind = ContractKind.FixedPrice,
            Status = ContractStatus.Active,
            PeriodOfPerformanceStart = new DateOnly(2026, 1, 1),
            PeriodOfPerformanceEnd = new DateOnly(2026, 12, 31),
            PlaceOfPerformance = "Remote",
            Description = title,
            DataHandlingPosture = DataHandlingPosture.FciOnly,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static SubcontractorEntity CreateSubcontractor(Guid subcontractorId, Guid tenantId, string name) =>
        new()
        {
            Id = subcontractorId,
            TenantId = tenantId,
            Name = name,
            Status = SubcontractorStatus.Active,
            RoleDescription = "Supplier readiness support",
            SmallBusinessStatus = "Small",
            CmmcStatus = "Level 1 in progress",
            InsuranceExpiresAt = new DateOnly(2026, 9, 30),
            NdaStatus = "Executed",
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static FlowDownClauseEntity CreateFlowDown(Guid id, Guid subcontractorId, Guid contractId, string clauseNumber, FlowDownStatus status) =>
        new()
        {
            Id = id,
            SubcontractorId = subcontractorId,
            ContractId = contractId,
            ClauseNumber = clauseNumber,
            Title = $"Flow-down {clauseNumber}",
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private sealed record StoryIds(
        Guid TenantId,
        Guid OtherTenantId,
        Guid ContractId,
        Guid OtherContractId,
        Guid OtherTenantContractId,
        Guid SubcontractorId,
        Guid OtherContractSubcontractorId,
        Guid OtherTenantSubcontractorId,
        Guid SentFlowDownId,
        Guid SignedFlowDownId,
        Guid OverdueRequestId,
        Guid MissingRequestId,
        Guid SatisfiedRequestId,
        Guid ReceivedEvidenceId)
    {
        public static StoryIds ForCase(string caseName)
        {
            var suffix = Math.Abs(caseName.GetHashCode(StringComparison.Ordinal)) % 10000;
            return new StoryIds(
                Guid.Parse($"15515515-5155-1551-5515-51551551{suffix:D4}"),
                Guid.Parse($"15515515-5155-1551-5515-51551552{suffix:D4}"),
                Guid.Parse($"15515515-5155-1551-5515-51551553{suffix:D4}"),
                Guid.Parse($"15515515-5155-1551-5515-51551554{suffix:D4}"),
                Guid.Parse($"15515515-5155-1551-5515-51551555{suffix:D4}"),
                Guid.Parse($"15515515-5155-1551-5515-51551556{suffix:D4}"),
                Guid.Parse($"15515515-5155-1551-5515-51551557{suffix:D4}"),
                Guid.Parse($"15515515-5155-1551-5515-51551558{suffix:D4}"),
                Guid.Parse($"15515515-5155-1551-5515-51551559{suffix:D4}"),
                Guid.Parse($"15515515-5155-1551-5515-51551560{suffix:D4}"),
                Guid.Parse($"15515515-5155-1551-5515-51551561{suffix:D4}"),
                Guid.Parse($"15515515-5155-1551-5515-51551562{suffix:D4}"),
                Guid.Parse($"15515515-5155-1551-5515-51551563{suffix:D4}"),
                Guid.Parse($"15515515-5155-1551-5515-51551564{suffix:D4}"));
        }
    }
}
