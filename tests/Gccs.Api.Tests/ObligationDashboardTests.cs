using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Compliance;
using Gccs.Application.Security;
using Gccs.Domain.Companies;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Domain.Contracts;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Compliance;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ObligationDashboardTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public ObligationDashboardTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_10_1_1_Dashboard_returns_current_tenant_obligations_only()
    {
        var tenantAId = Guid.Parse("10101010-1010-1010-1010-1010101010a1");
        var tenantBId = Guid.Parse("10101010-1010-1010-1010-1010101010b1");
        await using var factory = CreateFactory("tc-10-1-1", dbContext =>
        {
            SeedTenant(dbContext, tenantAId, "Tenant A");
            SeedTenant(dbContext, tenantBId, "Tenant B");
            SeedObligationScenario(dbContext, tenantAId, "tenant-a", "FAR 52.204-21", RiskLevel.High);
            SeedObligationScenario(dbContext, tenantBId, "tenant-b", "FAR 52.222-41", RiskLevel.High);
        });
        using var client = factory.CreateClient();

        var obligations = await ListAsync(client, "/api/contract-obligations", tenantAId);

        var obligation = Assert.Single(obligations);
        Assert.Equal("tenant-a-obligation", obligation.ObligationId);
        Assert.DoesNotContain(obligations, item => item.ContractNumber.Contains("tenant-b", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TC_10_1_2_Filters_return_matching_obligation_data()
    {
        var tenantId = Guid.Parse("10101010-1010-1010-1010-1010101010a2");
        var contractId = Guid.Parse("10101010-1010-1010-1010-1010101010c2");
        await using var factory = CreateFactory("tc-10-1-2", dbContext =>
        {
            SeedTenant(dbContext, tenantId, "Filter Tenant");
            SeedObligationScenario(
                dbContext,
                tenantId,
                "match",
                "FAR 52.204-21",
                RiskLevel.High,
                contractId,
                "IT/security",
                ComplianceTaskStatus.Open,
                DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1));
            SeedObligationScenario(
                dbContext,
                tenantId,
                "miss",
                "FAR 52.222-41",
                RiskLevel.Medium,
                ownerFunction: "HR/payroll",
                status: ComplianceTaskStatus.InProgress,
                dueAt: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(20));
        });
        using var client = factory.CreateClient();

        var byContract = await ListAsync(client, $"/api/contract-obligations?contractId={contractId}", tenantId);
        var byRisk = await ListAsync(client, "/api/contract-obligations?riskLevel=High", tenantId);
        var byOwner = await ListAsync(client, "/api/contract-obligations?owner=IT%2Fsecurity", tenantId);
        var byStatus = await ListAsync(client, "/api/contract-obligations?status=Open", tenantId);
        var byModule = await ListAsync(client, "/api/contract-obligations?module=Cybersecurity", tenantId);
        var byDueDate = await ListAsync(client, "/api/contract-obligations?dueDate=overdue", tenantId);
        var bySource = await ListAsync(client, "/api/contract-obligations?source=52.204-21", tenantId);

        foreach (var result in new[] { byContract, byRisk, byOwner, byStatus, byModule, byDueDate, bySource })
        {
            var obligation = Assert.Single(result);
            Assert.Equal("match-obligation", obligation.ObligationId);
        }
    }

    [Fact]
    public async Task TC_10_1_3_High_risk_and_overdue_obligations_are_flagged()
    {
        var tenantId = Guid.Parse("10101010-1010-1010-1010-1010101010a3");
        await using var factory = CreateFactory("tc-10-1-3", dbContext =>
        {
            SeedTenant(dbContext, tenantId, "Priority Tenant");
            SeedObligationScenario(
                dbContext,
                tenantId,
                "priority",
                "FAR 52.204-21",
                RiskLevel.Critical,
                dueAt: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2));
        });
        using var client = factory.CreateClient();

        var obligations = await ListAsync(client, "/api/contract-obligations", tenantId);

        var obligation = Assert.Single(obligations);
        Assert.True(obligation.IsHighRisk);
        Assert.True(obligation.IsOverdue);
        Assert.Equal("Open", obligation.Status);
        Assert.Equal("Cybersecurity", obligation.Module);
    }

    [Fact]
    public async Task TC_10_1_4_Empty_dashboard_returns_no_obligations()
    {
        var tenantId = Guid.Parse("10101010-1010-1010-1010-1010101010a4");
        await using var factory = CreateFactory("tc-10-1-4", dbContext => SeedTenant(dbContext, tenantId, "Empty Tenant"));
        using var client = factory.CreateClient();

        var obligations = await ListAsync(client, "/api/contract-obligations", tenantId);

        Assert.Empty(obligations);
    }

    private async Task<ObligationDashboardItemDto[]> ListAsync(HttpClient client, string requestUri, Guid tenantId)
    {
        using var request = CreateRequest(HttpMethod.Get, requestUri, tenantId, Guid.NewGuid(), Permission.ViewObligations);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<ObligationDashboardItemDto[]>(JsonOptions) ?? [];
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<IObligationDashboardRepository, EfObligationDashboardRepository>();

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
                seed?.Invoke(dbContext);
                dbContext.SaveChanges();
            });
        });

    private static void SeedObligationScenario(
        GccsDbContext dbContext,
        Guid tenantId,
        string key,
        string source,
        RiskLevel riskLevel,
        Guid? contractId = null,
        string ownerFunction = "IT/security",
        ComplianceTaskStatus status = ComplianceTaskStatus.Open,
        DateOnly? dueAt = null)
    {
        var actualContractId = contractId ?? Guid.NewGuid();
        var contractClauseId = Guid.NewGuid();
        var obligationId = $"{key}-obligation";
        dbContext.Contracts.Add(new ContractEntity
        {
            Id = actualContractId,
            TenantId = tenantId,
            ContractNumber = $"CON-{key}",
            Title = $"{key} contract",
            AgencyOrPrimeName = "Sample Agency",
            Relationship = ContractorRelationship.Prime,
            Kind = ContractKind.FixedPrice,
            Status = ContractStatus.Active,
            AwardedAt = new DateOnly(2026, 6, 15),
            PeriodOfPerformanceStart = new DateOnly(2026, 7, 1),
            PeriodOfPerformanceEnd = new DateOnly(2027, 6, 30),
            PlaceOfPerformance = "Remote",
            Description = "Seeded dashboard contract.",
            DataHandlingPosture = DataHandlingPosture.FciOnly,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Set<ContractClauseEntity>().Add(new ContractClauseEntity
        {
            Id = contractClauseId,
            ContractId = actualContractId,
            ClauseLibraryId = $"{key}-clause",
            ClauseNumber = source.Replace("FAR ", string.Empty, StringComparison.OrdinalIgnoreCase),
            Title = $"{source} clause",
            Source = ClauseSource.Far,
            SourceUrl = "https://www.acquisition.gov/far/52.204-21",
            AttachmentReason = "Dashboard test.",
            RequiresFlowDown = true,
            LastReviewedAt = new DateOnly(2026, 6, 3),
            Confidence = "high",
            ReviewState = ReviewState.Published,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Obligations.Add(new ObligationEntity
        {
            Id = obligationId,
            Source = source,
            Title = $"{source} action",
            PlainEnglishSummary = $"Plain-English summary for {source}.",
            TriggerCondition = "Clause appears in contract.",
            RequiredAction = "Perform the required action.",
            OwnerFunction = ownerFunction,
            RiskLevel = riskLevel,
            RequiresFlowDown = true,
            FlowDownRequirement = "Flow down where applicable.",
            ApplicabilityJson = "{}",
            EvidenceExamplesJson = """["Policy record","System configuration"]""",
            SourceName = source,
            SourceUrl = "https://www.acquisition.gov/far/52.204-21",
            SourceLastReviewedAt = new DateOnly(2026, 6, 3),
            SourceConfidence = "high",
            LastReviewedAt = new DateOnly(2026, 6, 3),
            Confidence = "high",
            ReviewState = ReviewState.Published
        });
        dbContext.Set<ContractClauseObligationEntity>().Add(new ContractClauseObligationEntity
        {
            ContractClauseId = contractClauseId,
            ObligationId = obligationId
        });
        dbContext.ComplianceTasks.Add(new ComplianceTaskEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = $"{source} action",
            Description = "Complete the dashboard action.",
            Type = ComplianceTaskType.ObligationAction,
            Status = status,
            RiskLevel = riskLevel,
            OwnerFunction = ownerFunction,
            DueAt = dueAt,
            ContractId = actualContractId,
            ObligationId = obligationId,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        string requestUri,
        Guid tenantId,
        Guid userId,
        Permission permission)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", permission.ToString());
        return request;
    }

    private static void SeedTenant(GccsDbContext dbContext, Guid tenantId, string name)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = name,
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}
