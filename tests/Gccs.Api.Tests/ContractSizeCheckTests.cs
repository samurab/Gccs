using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Contracts;
using Gccs.Application.Security;
using Gccs.Domain.Companies;
using Gccs.Domain.Common;
using Gccs.Domain.Contracts;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Contracts;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ContractSizeCheckTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public ContractSizeCheckTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_23_3_1_TC_23_3_2_and_TC_23_3_4_Run_size_check_returns_source_context_and_history()
    {
        var tenantId = Guid.Parse("23323323-3233-2332-3323-3232332333a1");
        var contractId = Guid.Parse("23323323-3233-2332-3323-3232332333b1");
        await using var factory = CreateFactory("tc-23-3-1", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            SeedContract(dbContext, tenantId, contractId);
            SeedSizeStandard(dbContext, "541511", ReviewState.Approved, 34_000_000m);
        });
        using var client = factory.CreateClient();

        using var request = CreateRequest(HttpMethod.Post, $"/api/contracts/{contractId}/size-checks", new ContractSizeCheckRequest("541511", 20_000_000m, null), tenantId, Permission.ManageContracts);
        var response = await client.SendAsync(request);
        using var historyRequest = CreateRequest<object?>(HttpMethod.Get, $"/api/contracts/{contractId}/size-checks", null, tenantId, Permission.ViewContracts);
        var historyResponse = await client.SendAsync(historyRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ContractSizeCheckDto>(JsonOptions);
        var history = await historyResponse.Content.ReadFromJsonAsync<ContractSizeCheckDto[]>(JsonOptions) ?? [];
        Assert.NotNull(result);
        Assert.Equal("LikelySmall", result.Result);
        Assert.Equal("541511", result.NaicsCode);
        Assert.Equal(34_000_000m, result.Threshold);
        Assert.Equal("https://www.sba.gov/document/support-table-size-standards", result.SourceUrl);
        Assert.Empty(result.MissingInformation);
        Assert.Single(history);
    }

    [Fact]
    public async Task TC_23_3_3_Expert_review_recommended_can_create_owner_task()
    {
        var tenantId = Guid.Parse("23323323-3233-2332-3323-3232332333a2");
        var contractId = Guid.Parse("23323323-3233-2332-3323-3232332333b2");
        await using var factory = CreateFactory("tc-23-3-3", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            SeedContract(dbContext, tenantId, contractId);
        });
        using var client = factory.CreateClient();

        using var request = CreateRequest(
            HttpMethod.Post,
            $"/api/contracts/{contractId}/size-checks",
            new ContractSizeCheckRequest("999999", null, null, true, "Proposal manager"),
            tenantId,
            Permission.ManageContracts);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ContractSizeCheckDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal("ExpertReviewRecommended", result.Result);
        Assert.Contains("approvedSizeStandard", result.MissingInformation);
        Assert.NotNull(result.ExpertReviewTaskId);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var task = await dbContext.ComplianceTasks.SingleAsync(task => task.Id == result.ExpertReviewTaskId);
        Assert.Equal("Proposal manager", task.OwnerFunction);
    }

    [Fact]
    public async Task TC_23_3_5_Size_check_is_tenant_scoped_permissioned_and_audit_logged()
    {
        var tenantAId = Guid.Parse("23323323-3233-2332-3323-3232332333a3");
        var tenantBId = Guid.Parse("23323323-3233-2332-3323-3232332333b3");
        var contractAId = Guid.Parse("23323323-3233-2332-3323-3232332333c3");
        await using var factory = CreateFactory("tc-23-3-5", dbContext =>
        {
            SeedTenant(dbContext, tenantAId);
            SeedTenant(dbContext, tenantBId);
            SeedContract(dbContext, tenantAId, contractAId);
            SeedSizeStandard(dbContext, "541511", ReviewState.Approved, 34_000_000m);
        });
        using var client = factory.CreateClient();

        using var forbidden = CreateRequest(HttpMethod.Post, $"/api/contracts/{contractAId}/size-checks", new ContractSizeCheckRequest("541511", 20_000_000m, null), tenantAId, Permission.ViewContracts);
        var forbiddenResponse = await client.SendAsync(forbidden);
        using var otherTenant = CreateRequest(HttpMethod.Post, $"/api/contracts/{contractAId}/size-checks", new ContractSizeCheckRequest("541511", 20_000_000m, null), tenantBId, Permission.ManageContracts);
        var otherTenantResponse = await client.SendAsync(otherTenant);
        using var allowed = CreateRequest(HttpMethod.Post, $"/api/contracts/{contractAId}/size-checks", new ContractSizeCheckRequest("541511", 20_000_000m, null), tenantAId, Permission.ManageContracts);
        var allowedResponse = await client.SendAsync(allowed);

        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, otherTenantResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, allowedResponse.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var audit = await dbContext.AuditLogEntries.SingleAsync(entry => entry.TenantId == tenantAId && entry.EntityType == "ContractSizeCheck");
        Assert.Contains("LikelySmall", audit.MetadataJson, StringComparison.Ordinal);
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<ContractSizeCheckService>();
                services.AddScoped<IContractSizeCheckRepository, EfContractSizeCheckRepository>();
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

    private static HttpRequestMessage CreateRequest<TContent>(HttpMethod method, string requestUri, TContent content, Guid tenantId, Permission permission)
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

    private static void SeedTenant(GccsDbContext dbContext, Guid tenantId)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = $"Tenant {tenantId:N}",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private static void SeedContract(GccsDbContext dbContext, Guid tenantId, Guid contractId)
    {
        dbContext.Contracts.Add(new ContractEntity
        {
            Id = contractId,
            TenantId = tenantId,
            ContractNumber = $"SIZE-{contractId:N}"[..14],
            Title = "Size check contract",
            AgencyOrPrimeName = "SBA",
            Relationship = ContractorRelationship.Prime,
            Kind = ContractKind.FixedPrice,
            Status = ContractStatus.Active,
            PeriodOfPerformanceStart = new DateOnly(2026, 7, 1),
            PeriodOfPerformanceEnd = new DateOnly(2027, 6, 30),
            PlaceOfPerformance = "Remote",
            DataHandlingPosture = DataHandlingPosture.FciOnly,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private static void SeedSizeStandard(GccsDbContext dbContext, string naicsCode, ReviewState status, decimal threshold)
    {
        dbContext.SbaSizeStandards.Add(new SbaSizeStandardEntity
        {
            Id = Guid.NewGuid(),
            NaicsCode = naicsCode,
            Metric = "Receipts",
            Threshold = threshold,
            Unit = "USD",
            SourceUrl = "https://www.sba.gov/document/support-table-size-standards",
            EffectiveAt = new DateOnly(2026, 1, 1),
            LastReviewedAt = new DateOnly(2026, 6, 17),
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = Guid.NewGuid()
        });
    }
}
