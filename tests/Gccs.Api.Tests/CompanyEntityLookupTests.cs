using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Companies;
using Gccs.Application.SamGov;
using Gccs.Domain.Common;
using Gccs.Domain.Companies;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Companies;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class CompanyEntityLookupTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public CompanyEntityLookupTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_22_2_1_and_TC_22_2_2_Authorized_user_can_search_by_uei_or_name_with_source_metadata()
    {
        var tenantId = Guid.Parse("22222222-2222-2222-2222-2222222222a1");
        await using var factory = CreateFactory("tc-22-2-1", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();

        using var byUeiRequest = CreateRequest(HttpMethod.Post, "/api/company-profile/sam-lookup/search", new CompanyEntityLookupRequest("ABC123DEF456", null), tenantId, Permission.ViewCompanyProfile);
        using var byNameRequest = CreateRequest(HttpMethod.Post, "/api/company-profile/sam-lookup/search", new CompanyEntityLookupRequest(null, "Acme Federal Services"), tenantId, Permission.ViewCompanyProfile);
        using var forbiddenRequest = CreateRequest(HttpMethod.Post, "/api/company-profile/sam-lookup/search", new CompanyEntityLookupRequest("ABC123DEF456", null), tenantId, Permission.ViewContracts);

        var byUeiResponse = await client.SendAsync(byUeiRequest);
        var byNameResponse = await client.SendAsync(byNameRequest);
        var forbiddenResponse = await client.SendAsync(forbiddenRequest);

        Assert.Equal(HttpStatusCode.OK, byUeiResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, byNameResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
        var results = await byUeiResponse.Content.ReadFromJsonAsync<CompanyEntityLookupResultDto[]>(JsonOptions) ?? [];
        var result = Assert.Single(results);
        Assert.Equal("SAM.gov", result.Source);
        Assert.True(result.RetrievedAt > DateTimeOffset.UtcNow.AddMinutes(-1));
        Assert.Equal("Acme Federal Services", result.LegalBusinessName);
        Assert.Equal("1ABC2", result.CageCode);
    }

    [Fact]
    public async Task TC_22_2_3_and_TC_22_2_5_User_can_apply_selected_fields_and_changes_are_audit_logged()
    {
        var tenantId = Guid.Parse("22222222-2222-2222-2222-2222222222a2");
        await using var factory = CreateFactory("tc-22-2-3", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        var lookup = await SearchAsync(client, tenantId);
        using var applyRequest = CreateRequest(
            HttpMethod.Post,
            "/api/company-profile/sam-lookup/apply",
            new ApplyCompanyEntityLookupRequest(lookup, ["legalEntityName", "uei", "cageCode", "samRegistrationExpiresAt", "address", "naics"], false),
            tenantId,
            Permission.ManageCompanyProfile);

        var response = await client.SendAsync(applyRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<CompanyProfileDto>(JsonOptions);
        Assert.NotNull(profile);
        Assert.Equal("Acme Federal Services", profile.LegalEntityName);
        Assert.Equal("ABC123DEF456", profile.Uei);
        Assert.Equal("1ABC2", profile.CageCode);
        Assert.Equal(new DateOnly(2027, 6, 30), profile.SamRegistrationExpiresAt);
        Assert.Contains(profile.NaicsCodes, naics => naics.Code == "541511");

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var audit = await dbContext.AuditLogEntries.SingleAsync(entry => entry.TenantId == tenantId && entry.EntityType == "CompanyProfileSamLookup");
        Assert.Contains("SAM.gov", audit.MetadataJson, StringComparison.Ordinal);
        Assert.Contains("legalEntityName", audit.MetadataJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TC_22_2_4_Existing_values_are_not_overwritten_without_confirmation_and_scope_is_tenant_specific()
    {
        var tenantAId = Guid.Parse("22222222-2222-2222-2222-2222222222a3");
        var tenantBId = Guid.Parse("22222222-2222-2222-2222-2222222222b3");
        await using var factory = CreateFactory("tc-22-2-4", dbContext =>
        {
            SeedTenant(dbContext, tenantAId);
            SeedTenant(dbContext, tenantBId);
            SeedProfile(dbContext, tenantAId, "Existing Legal Name", "OLDUEI123456");
            SeedProfile(dbContext, tenantBId, "Tenant B Name", "TENANTB12345");
        });
        using var client = factory.CreateClient();
        var lookup = await SearchAsync(client, tenantAId);
        var request = new ApplyCompanyEntityLookupRequest(lookup, ["legalEntityName", "uei"], false);

        using var conflictRequest = CreateRequest(HttpMethod.Post, "/api/company-profile/sam-lookup/apply", request, tenantAId, Permission.ManageCompanyProfile);
        var conflictResponse = await client.SendAsync(conflictRequest);

        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);

        using var confirmedRequest = CreateRequest(HttpMethod.Post, "/api/company-profile/sam-lookup/apply", request with { ConfirmOverwrite = true }, tenantAId, Permission.ManageCompanyProfile);
        var confirmedResponse = await client.SendAsync(confirmedRequest);

        Assert.Equal(HttpStatusCode.OK, confirmedResponse.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var tenantAProfile = await dbContext.CompanyProfiles.SingleAsync(profile => profile.TenantId == tenantAId);
        var tenantBProfile = await dbContext.CompanyProfiles.SingleAsync(profile => profile.TenantId == tenantBId);
        Assert.Equal("Acme Federal Services", tenantAProfile.LegalEntityName);
        Assert.Equal("Tenant B Name", tenantBProfile.LegalEntityName);
    }

    private async Task<CompanyEntityLookupResultDto> SearchAsync(HttpClient client, Guid tenantId)
    {
        using var request = CreateRequest(HttpMethod.Post, "/api/company-profile/sam-lookup/search", new CompanyEntityLookupRequest("ABC123DEF456", null), tenantId, Permission.ViewCompanyProfile);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = await response.Content.ReadFromJsonAsync<CompanyEntityLookupResultDto[]>(JsonOptions) ?? [];
        return Assert.Single(results);
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
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
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();
                services.AddSingleton<ISamGovEntityLookupClient, FakeSamGovLookupClient>();

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
        request.Content = JsonContent.Create(content, options: JsonOptions);
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

    private static void SeedProfile(GccsDbContext dbContext, Guid tenantId, string legalName, string uei)
    {
        dbContext.CompanyProfiles.Add(new CompanyProfileEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LegalEntityName = legalName,
            Uei = uei,
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
