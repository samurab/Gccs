using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Companies;
using Gccs.Application.Compliance;
using Gccs.Application.Contracts;
using Gccs.Application.NoCui;
using Gccs.Application.SamGov;
using Gccs.Domain.Companies;
using Gccs.Domain.Common;
using Gccs.Domain.Contracts;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Companies;
using Gccs.Infrastructure.Compliance;
using Gccs.Infrastructure.Contracts;
using Gccs.Infrastructure.NoCui;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ApiErrorContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public ApiErrorContractTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Validation_error_returns_standard_400_shape()
    {
        var tenantId = Guid.Parse("90909090-9090-9090-9090-9090909090a1");
        await using var factory = CreateFactory("api-error-validation", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            HttpMethod.Put,
            "/api/company-profile",
            CreateCompanyProfileRequest(completeProfile: true) with { Uei = null, CageCode = null, SamRegistrationExpiresAt = null },
            tenantId,
            Permission.ManageCompanyProfile);

        var response = await client.SendAsync(request);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        AssertStandardProblem(payload, HttpStatusCode.BadRequest, "validation_failed");
    }

    [Fact]
    public async Task Unauthenticated_request_returns_standard_401_shape()
    {
        await using var factory = CreateFactory("api-error-unauthenticated");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/me/access");
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        AssertStandardProblem(payload, HttpStatusCode.Unauthorized, "authentication_required");
    }

    [Fact]
    public async Task Unauthorized_request_returns_standard_403_shape()
    {
        var tenantId = Guid.Parse("90909090-9090-9090-9090-9090909090a3");
        await using var factory = CreateFactory("api-error-forbidden", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        using var request = CreateRequest(HttpMethod.Get, "/api/contracts", tenantId, Permission.ViewEvidence);

        var response = await client.SendAsync(request);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        AssertStandardProblem(payload, HttpStatusCode.Forbidden, "permission_denied");
    }

    [Fact]
    public async Task Missing_resource_returns_standard_404_shape()
    {
        var tenantId = Guid.Parse("90909090-9090-9090-9090-9090909090a4");
        await using var factory = CreateFactory("api-error-not-found", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        using var request = CreateRequest(HttpMethod.Get, $"/api/contracts/{Guid.NewGuid()}", tenantId, Permission.ViewContracts);

        var response = await client.SendAsync(request);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        AssertStandardProblem(payload, HttpStatusCode.NotFound, "resource_not_found");
    }

    [Fact]
    public async Task Conflict_returns_standard_409_shape()
    {
        var tenantId = Guid.Parse("90909090-9090-9090-9090-9090909090a5");
        await using var factory = CreateFactory("api-error-conflict", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            dbContext.CompanyProfiles.Add(new CompanyProfileEntity
            {
                Id = Guid.Parse("90909090-9090-9090-9090-9090909090b5"),
                TenantId = tenantId,
                LegalEntityName = "Existing Legal Name",
                Uei = "OLDUEI123456",
                CageCode = "OLD1",
                AgencyCustomersJson = "[]",
                KeySystemsJson = "[]",
                ContractorRole = ContractorRole.Unknown,
                ProductsAndServices = string.Empty,
                EmployeeRange = CompanyRange.Unknown,
                RevenueRange = CompanyRange.Unknown,
                DataHandlingPosture = DataHandlingPosture.Unknown,
                CreatedAt = DateTimeOffset.UtcNow
            });
        });
        using var client = factory.CreateClient();
        var lookup = await SearchCompanyAsync(client, tenantId);
        using var request = CreateRequest(
            HttpMethod.Post,
            "/api/company-profile/sam-lookup/apply",
            new ApplyCompanyEntityLookupRequest(lookup, ["legalEntityName", "uei"], false),
            tenantId,
            Permission.ManageCompanyProfile);

        var response = await client.SendAsync(request);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        AssertStandardProblem(payload, HttpStatusCode.Conflict, "company_profile_sam_lookup_conflict");
    }

    [Fact]
    public async Task Unexpected_exception_returns_standard_500_shape_without_stack_trace()
    {
        const string correlationId = "api-error-500-correlation";
        await using var factory = CreateFactory(
            "api-error-exception",
            configureServices: services =>
            {
                services.RemoveAll<IComplianceOverviewRepository>();
                services.AddScoped<IComplianceOverviewRepository, ThrowingComplianceOverviewRepository>();
            });
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            HttpMethod.Get,
            "/api/compliance/overview",
            Guid.Parse("90909090-9090-9090-9090-9090909090a6"),
            Permission.ViewObligations);
        request.Headers.Add("X-Correlation-ID", correlationId);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        using var payload = JsonDocument.Parse(body);

        AssertStandardProblem(payload, HttpStatusCode.InternalServerError, "compliance_overview_unavailable", correlationId);
        Assert.DoesNotContain("InvalidOperationException", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Simulated API error contract failure", body, StringComparison.Ordinal);
        Assert.DoesNotContain(" at ", body, StringComparison.Ordinal);
    }

    private WebApplicationFactory<Program> CreateFactory(
        string databaseName,
        Action<GccsDbContext>? seed = null,
        Action<IServiceCollection>? configureServices = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<CompanyProfileService>();
                services.AddScoped<CompanyEntityLookupService>();
                services.AddScoped<ICompanyProfileRepository, EfCompanyProfileRepository>();
                services.AddScoped<ContractService>();
                services.AddScoped<IContractRepository, EfContractRepository>();
                services.AddScoped<INoCuiAcknowledgementRepository, EfNoCuiAcknowledgementRepository>();
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();
                services.AddScoped<IComplianceOverviewRepository, EfComplianceOverviewRepository>();
                services.AddSingleton<ISamGovEntityLookupClient, FakeSamGovLookupClient>();
                configureServices?.Invoke(services);

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
                seed?.Invoke(dbContext);
                dbContext.SaveChanges();
            });
        });

    private static async Task<CompanyEntityLookupResultDto> SearchCompanyAsync(HttpClient client, Guid tenantId)
    {
        using var request = CreateRequest(
            HttpMethod.Post,
            "/api/company-profile/sam-lookup/search",
            new CompanyEntityLookupRequest("ABC123DEF456", null),
            tenantId,
            Permission.ViewCompanyProfile);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = await response.Content.ReadFromJsonAsync<CompanyEntityLookupResultDto[]>(JsonOptions) ?? [];
        return Assert.Single(results);
    }

    private static void AssertStandardProblem(
        JsonDocument payload,
        HttpStatusCode statusCode,
        string errorCode,
        string? correlationId = null)
    {
        var root = payload.RootElement;
        Assert.Equal((int)statusCode, root.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("title").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("detail").GetString()));
        Assert.Equal(errorCode, root.GetProperty("errorCode").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("correlationId").GetString()));
        if (correlationId is not null)
        {
            Assert.Equal(correlationId, root.GetProperty("correlationId").GetString());
        }
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string requestUri, Guid tenantId, Permission permission)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", Guid.NewGuid().ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", permission.ToString());
        return request;
    }

    private static HttpRequestMessage CreateRequest<TContent>(
        HttpMethod method,
        string requestUri,
        TContent content,
        Guid tenantId,
        Permission permission)
    {
        var request = CreateRequest(method, requestUri, tenantId, permission);
        request.Content = JsonContent.Create(content, options: JsonOptions);
        return request;
    }

    private static UpsertCompanyProfileRequest CreateCompanyProfileRequest(bool completeProfile) =>
        new(
            "Acme Federal Services",
            "Acme Gov",
            "ABCDEF123456",
            "1A2B3",
            new DateOnly(2027, 6, 15),
            [new CompanyNaicsCodeDto("541330", "Engineering Services", true, "$25.5M", true, new DateOnly(2026, 6, 15))],
            [new CompanyCertificationDto(null, CertificationType.Wosb, CertificationStatus.Active, "SBA", new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), "WOSB-1")],
            ["Department of Defense"],
            ContractorRole.Subcontractor,
            "Engineering and cybersecurity support services",
            CompanyRange.Small,
            CompanyRange.Small,
            [new CompanyLocationDto("HQ", "100 Main St", null, "Arlington", "VA", "22201", "USA", true)],
            new ItEnvironmentSummaryDto("Microsoft 365 GCC High with managed endpoints.", true, "Trusted MSP", ["Microsoft 365", "Intune"]),
            DataHandlingPosture.FciOnly,
            completeProfile);

    private static void SeedTenant(GccsDbContext dbContext, Guid tenantId)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = $"API Error Tenant {tenantId:N}",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private sealed class ThrowingComplianceOverviewRepository : IComplianceOverviewRepository
    {
        public Task<ComplianceOverviewDto> GetCurrentTenantOverviewAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated API error contract failure");
    }

    private sealed class FakeSamGovLookupClient : ISamGovEntityLookupClient
    {
        public Task<SamGovEntityLookupResult> LookupByUeiAsync(string uei, CancellationToken cancellationToken = default) =>
            SearchAsync(new SamGovEntitySearchRequest(uei, null), cancellationToken);

        public Task<SamGovEntityLookupResult> SearchAsync(SamGovEntitySearchRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(SamGovEntityLookupResult.Success(
                """
                {
                  "entityData": [
                    {
                      "legalBusinessName": "Acme Federal Services",
                      "ueiSAM": "ABC123DEF456",
                      "cageCode": "1ABC2",
                      "registrationStatus": "Active",
                      "expirationDate": "2027-06-30",
                      "physicalAddress": {
                        "addressLine1": "100 Main St",
                        "addressLine2": "Suite 200",
                        "city": "Arlington",
                        "stateOrProvince": "VA",
                        "zipCode": "22201",
                        "countryCode": "US"
                      },
                      "naicsCode": [
                        { "code": "541511", "title": "Custom Computer Programming Services" }
                      ]
                    }
                  ]
                }
                """));
    }
}
