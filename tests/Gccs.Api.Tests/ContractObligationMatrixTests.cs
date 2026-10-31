using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Reports;
using Gccs.Application.Security;
using Gccs.Domain.Common;
using Gccs.Domain.Companies;
using Gccs.Domain.Compliance;
using Gccs.Domain.Contracts;
using Gccs.Domain.Evidence;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Domain.Vendors;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Reports;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ContractObligationMatrixTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public ContractObligationMatrixTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_15_2_1_Generate_contract_obligation_matrix_includes_required_columns()
    {
        var ids = StoryIds.ForCase("tc-15-2-1");
        await using var factory = CreateFactory("tc-15-2-1", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var rows = await ListMatrixAsync(client, ids.TenantId, ids.ContractId);

        var row = Assert.Single(rows);
        Assert.Equal(ids.ContractId, row.ContractId);
        Assert.Equal(ids.ContractClauseId, row.ContractClauseId);
        Assert.Equal("52.204-21", row.ClauseNumber);
        Assert.Equal("Fci safeguarding obligation", row.ObligationTitle);
        Assert.Equal("Security", row.OwnerFunction);
        Assert.Equal("Open", row.Status);
        Assert.Equal(RiskLevel.High, row.RiskLevel);
        Assert.Equal(ids.DueDate, row.DueAt);
        Assert.Equal([ids.EvidenceItemId], row.EvidenceItemIds);
        Assert.Equal(["Access control policy"], row.EvidenceNames);
    }

    [Fact]
    public async Task TC_15_2_2_Matrix_rows_include_source_links_and_last_reviewed_dates()
    {
        var ids = StoryIds.ForCase("tc-15-2-2");
        await using var factory = CreateFactory("tc-15-2-2", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var row = Assert.Single(await ListMatrixAsync(client, ids.TenantId, ids.ContractId));

        Assert.Equal("https://example.test/clause", row.ClauseSourceUrl);
        Assert.Equal(new DateOnly(2026, 6, 15), row.ClauseLastReviewedAt);
        Assert.Equal("https://example.test/obligation", row.ObligationSourceUrl);
        Assert.Equal(new DateOnly(2026, 6, 15), row.ObligationLastReviewedAt);
    }

    [Fact]
    public async Task TC_15_2_3_Matrix_identifies_obligations_requiring_flow_down()
    {
        var ids = StoryIds.ForCase("tc-15-2-3");
        await using var factory = CreateFactory("tc-15-2-3", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var row = Assert.Single(await ListMatrixAsync(client, ids.TenantId, ids.ContractId));

        Assert.True(row.RequiresFlowDown);
        Assert.Equal(["Sent"], row.FlowDownStatuses);
    }

    [Fact]
    public async Task TC_15_2_4_Export_rows_and_fields_match_matrix_data()
    {
        var ids = StoryIds.ForCase("tc-15-2-4");
        await using var factory = CreateFactory("tc-15-2-4", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var rows = await ListMatrixAsync(client, ids.TenantId, ids.ContractId);
        var export = await ExportMatrixAsync(client, ids.TenantId, ids.ContractId);

        Assert.Equal(ids.ContractId, export.ContractId);
        Assert.Equal("text/csv", export.ContentType);
        Assert.Equal(
            JsonSerializer.Serialize(rows, JsonOptions),
            JsonSerializer.Serialize(export.Rows, JsonOptions));
        Assert.Contains("clauseNumber", export.Csv, StringComparison.Ordinal);
        Assert.Contains("52.204-21", export.Csv, StringComparison.Ordinal);
    }

    private static async Task<ContractObligationMatrixRowDto[]> ListMatrixAsync(HttpClient client, Guid tenantId, Guid contractId)
    {
        using var request = CreateRequest<object?>(HttpMethod.Get, $"/api/contracts/{contractId}/obligations", null, tenantId);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<ContractObligationMatrixRowDto[]>(JsonOptions) ?? [];
    }

    private static async Task<ContractObligationMatrixExportDto> ExportMatrixAsync(HttpClient client, Guid tenantId, Guid contractId)
    {
        using var request = CreateRequest<object?>(HttpMethod.Get, $"/api/contracts/{contractId}/obligations/export", null, tenantId);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<ContractObligationMatrixExportDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected contract obligation matrix export.");
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<IContractObligationMatrixRepository, EfContractObligationMatrixRepository>();

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
                seed?.Invoke(dbContext);
                dbContext.SaveChanges();
            });
        });

    private static HttpRequestMessage CreateRequest<TContent>(HttpMethod method, string requestUri, TContent content, Guid tenantId)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", Guid.NewGuid().ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", Permission.ViewObligations.ToString());
        if (content is not null)
        {
            request.Content = JsonContent.Create(content, options: JsonOptions);
        }

        return request;
    }

    private static void SeedScenario(GccsDbContext dbContext, StoryIds ids)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = ids.TenantId,
            Name = "Matrix Tenant",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Contracts.Add(new ContractEntity
        {
            Id = ids.ContractId,
            TenantId = ids.TenantId,
            ContractNumber = "MATRIX-2026-001",
            Title = "Matrix contract",
            AgencyOrPrimeName = "Agency",
            Relationship = ContractorRelationship.Prime,
            Kind = ContractKind.FixedPrice,
            Status = ContractStatus.Active,
            AwardedAt = new DateOnly(2026, 6, 15),
            PeriodOfPerformanceStart = new DateOnly(2026, 7, 1),
            PeriodOfPerformanceEnd = new DateOnly(2027, 6, 30),
            PlaceOfPerformance = "Arlington, VA",
            Description = "No-CUI matrix seed.",
            DataHandlingPosture = DataHandlingPosture.FciOnly,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Set<ContractClauseEntity>().Add(new ContractClauseEntity
        {
            Id = ids.ContractClauseId,
            ContractId = ids.ContractId,
            ClauseLibraryId = "matrix-clause",
            ClauseNumber = "52.204-21",
            Title = "Basic Safeguarding of Covered Contractor Information Systems",
            Source = ClauseSource.Far,
            SourceUrl = "https://example.test/clause",
            AttachmentReason = "Matrix test clause.",
            RequiresFlowDown = true,
            LastReviewedAt = new DateOnly(2026, 6, 15),
            Confidence = "high",
            ReviewState = ReviewState.Approved,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Obligations.Add(new ObligationEntity
        {
            Id = ids.ObligationId,
            Source = "FAR",
            Title = "Fci safeguarding obligation",
            PlainEnglishSummary = "Protect FCI.",
            TriggerCondition = "Contract includes FCI handling.",
            RequiredAction = "Maintain basic safeguards.",
            OwnerFunction = "IT",
            RiskLevel = RiskLevel.High,
            RequiresFlowDown = true,
            FlowDownRequirement = "Flow down to subcontractors handling FCI.",
            EvidenceExamplesJson = "[\"Access control policy\"]",
            SourceName = "Acquisition source",
            SourceUrl = "https://example.test/obligation",
            SourceLastReviewedAt = new DateOnly(2026, 6, 15),
            LastReviewedAt = new DateOnly(2026, 6, 15),
            Confidence = "high",
            SourceConfidence = "high",
            ReviewState = ReviewState.Published
        });
        dbContext.Set<ContractClauseObligationEntity>().Add(new ContractClauseObligationEntity
        {
            ContractClauseId = ids.ContractClauseId,
            ObligationId = ids.ObligationId
        });
        dbContext.ComplianceTasks.Add(new ComplianceTaskEntity
        {
            Id = Guid.NewGuid(),
            TenantId = ids.TenantId,
            Title = "Matrix obligation task",
            Description = "Track matrix obligation.",
            Type = ComplianceTaskType.ObligationAction,
            Status = ComplianceTaskStatus.Open,
            RiskLevel = RiskLevel.High,
            OwnerFunction = "Security",
            DueAt = ids.DueDate,
            ContractId = ids.ContractId,
            ObligationId = ids.ObligationId,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.EvidenceItems.Add(new EvidenceItemEntity
        {
            Id = ids.EvidenceItemId,
            TenantId = ids.TenantId,
            Name = "Access control policy",
            Description = "Evidence for matrix obligation.",
            Type = EvidenceType.Policy,
            OwnerFunction = "Security",
            Status = EvidenceStatus.Approved,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Set<EvidenceContractEntity>().Add(new EvidenceContractEntity
        {
            EvidenceItemId = ids.EvidenceItemId,
            ContractId = ids.ContractId
        });
        dbContext.Set<EvidenceObligationEntity>().Add(new EvidenceObligationEntity
        {
            EvidenceItemId = ids.EvidenceItemId,
            ObligationId = ids.ObligationId
        });
        dbContext.Subcontractors.Add(new SubcontractorEntity
        {
            Id = ids.SubcontractorId,
            TenantId = ids.TenantId,
            Name = "Matrix Supplier LLC",
            Status = SubcontractorStatus.Active,
            RoleDescription = "FCI support",
            SmallBusinessStatus = "Small",
            CmmcStatus = "Level 1",
            NdaStatus = "Executed",
            WorkshareDescription = "Matrix support",
            HasFciAccess = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.FlowDownClauses.Add(new FlowDownClauseEntity
        {
            Id = Guid.NewGuid(),
            SubcontractorId = ids.SubcontractorId,
            ContractId = ids.ContractId,
            ContractClauseId = ids.ContractClauseId,
            ObligationId = ids.ObligationId,
            ClauseNumber = "52.204-21",
            Title = "Basic safeguarding flow-down",
            Status = FlowDownStatus.Sent,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private sealed record StoryIds(
        Guid TenantId,
        Guid ContractId,
        Guid ContractClauseId,
        string ObligationId,
        Guid EvidenceItemId,
        Guid SubcontractorId,
        DateOnly DueDate)
    {
        public static StoryIds ForCase(string caseName)
        {
            var suffix = Math.Abs(caseName.GetHashCode(StringComparison.Ordinal)) % 10000;
            return new StoryIds(
                Guid.Parse($"15215215-2152-1521-5215-21521521{suffix:D4}"),
                Guid.Parse($"15215215-2152-1521-5215-21521522{suffix:D4}"),
                Guid.Parse($"15215215-2152-1521-5215-21521523{suffix:D4}"),
                $"obligation-15-2-{suffix:D4}",
                Guid.Parse($"15215215-2152-1521-5215-21521524{suffix:D4}"),
                Guid.Parse($"15215215-2152-1521-5215-21521525{suffix:D4}"),
                new DateOnly(2026, 8, 15));
        }
    }
}
