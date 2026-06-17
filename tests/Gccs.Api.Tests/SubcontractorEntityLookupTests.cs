using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.SamGov;
using Gccs.Application.Security;
using Gccs.Application.Subcontractors;
using Gccs.Domain.Tenancy;
using Gccs.Domain.Identity;
using Gccs.Domain.Vendors;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Subcontractors;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class SubcontractorEntityLookupTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public SubcontractorEntityLookupTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_22_3_1_Authorized_user_can_search_by_uei_or_name()
    {
        var tenantId = Guid.Parse("22322322-3223-2232-2322-3223223223a1");
        var subcontractorId = Guid.Parse("22322322-3223-2232-2322-3223223223b1");
        await using var factory = CreateFactory("tc-22-3-1", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            SeedSubcontractor(dbContext, tenantId, subcontractorId, "Original Sub");
        });
        using var client = factory.CreateClient();

        using var byUei = CreateRequest(HttpMethod.Post, $"/api/subcontractors/{subcontractorId}/sam-lookup/search", new SubcontractorEntityLookupRequest("SUBUEI123456", null), tenantId, Permission.ViewSubcontractors);
        using var byName = CreateRequest(HttpMethod.Post, $"/api/subcontractors/{subcontractorId}/sam-lookup/search", new SubcontractorEntityLookupRequest(null, "Sub Federal LLC"), tenantId, Permission.ViewSubcontractors);
        using var forbidden = CreateRequest(HttpMethod.Post, $"/api/subcontractors/{subcontractorId}/sam-lookup/search", new SubcontractorEntityLookupRequest("SUBUEI123456", null), tenantId, Permission.ViewContracts);

        var byUeiResponse = await client.SendAsync(byUei);
        var byNameResponse = await client.SendAsync(byName);
        var forbiddenResponse = await client.SendAsync(forbidden);

        Assert.Equal(HttpStatusCode.OK, byUeiResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, byNameResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
        var results = await byUeiResponse.Content.ReadFromJsonAsync<SubcontractorEntityLookupResultDto[]>(JsonOptions) ?? [];
        var result = Assert.Single(results);
        Assert.Equal("SAM.gov", result.Source);
        Assert.Equal("SUBUEI123456", result.Uei);
        Assert.Equal("No Active Exclusions", result.ExclusionStatus);
    }

    [Fact]
    public async Task TC_22_3_2_TC_22_3_4_and_TC_22_3_5_Apply_updates_current_tenant_metadata_and_audits()
    {
        var tenantAId = Guid.Parse("22322322-3223-2232-2322-3223223223a2");
        var tenantBId = Guid.Parse("22322322-3223-2232-2322-3223223223b2");
        var subcontractorAId = Guid.Parse("22322322-3223-2232-2322-3223223223c2");
        var subcontractorBId = Guid.Parse("22322322-3223-2232-2322-3223223223d2");
        await using var factory = CreateFactory("tc-22-3-2", dbContext =>
        {
            SeedTenant(dbContext, tenantAId);
            SeedTenant(dbContext, tenantBId);
            SeedSubcontractor(dbContext, tenantAId, subcontractorAId, "Original Sub A");
            SeedSubcontractor(dbContext, tenantBId, subcontractorBId, "Original Sub B");
        });
        using var client = factory.CreateClient();
        var lookup = await SearchAsync(client, tenantAId, subcontractorAId, "SUBUEI123456");
        using var apply = CreateRequest(
            HttpMethod.Post,
            $"/api/subcontractors/{subcontractorAId}/sam-lookup/apply",
            new ApplySubcontractorEntityLookupRequest(lookup, ["name", "uei", "cageCode"]),
            tenantAId,
            Permission.ManageSubcontractors);

        var response = await client.SendAsync(apply);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<SubcontractorDto>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal("Sub Federal LLC", updated.Name);
        Assert.Equal("SUBUEI123456", updated.Uei);
        Assert.Equal("8XYZ1", updated.CageCode);
        Assert.Equal("Active", updated.SamRegistrationStatus);
        Assert.Equal(new DateOnly(2027, 8, 31), updated.SamRegistrationExpiresAt);
        Assert.Equal("SAM.gov", updated.SamSource);
        Assert.NotNull(updated.SamRetrievedAt);
        Assert.Contains(updated.SamNaicsCodes, naics => naics.Code == "541330");

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var tenantBSub = await dbContext.Subcontractors.SingleAsync(subcontractor => subcontractor.Id == subcontractorBId);
        var audit = await dbContext.AuditLogEntries.SingleAsync(entry => entry.TenantId == tenantAId && entry.EntityType == "SubcontractorSamLookup");
        Assert.Equal("Original Sub B", tenantBSub.Name);
        Assert.Contains("SAM.gov", audit.MetadataJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TC_22_3_3_No_match_and_multiple_match_results_do_not_change_existing_data()
    {
        var tenantId = Guid.Parse("22322322-3223-2232-2322-3223223223a3");
        var subcontractorId = Guid.Parse("22322322-3223-2232-2322-3223223223b3");
        await using var factory = CreateFactory("tc-22-3-3", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            SeedSubcontractor(dbContext, tenantId, subcontractorId, "Original Sub");
        });
        using var client = factory.CreateClient();

        var noMatch = await SearchAsync(client, tenantId, subcontractorId, "NO_MATCH");
        using var multipleRequest = CreateRequest(HttpMethod.Post, $"/api/subcontractors/{subcontractorId}/sam-lookup/search", new SubcontractorEntityLookupRequest("MULTI_MATCH", null), tenantId, Permission.ViewSubcontractors);
        var multipleResponse = await client.SendAsync(multipleRequest);
        var multiple = await multipleResponse.Content.ReadFromJsonAsync<SubcontractorEntityLookupResultDto[]>(JsonOptions) ?? [];

        Assert.Equal(string.Empty, noMatch.Uei);
        Assert.Equal(2, multiple.Length);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var subcontractor = await dbContext.Subcontractors.SingleAsync(item => item.Id == subcontractorId);
        Assert.Equal("Original Sub", subcontractor.Name);
        Assert.Null(subcontractor.SamRetrievedAt);
    }

    private async Task<SubcontractorEntityLookupResultDto> SearchAsync(HttpClient client, Guid tenantId, Guid subcontractorId, string uei)
    {
        using var request = CreateRequest(HttpMethod.Post, $"/api/subcontractors/{subcontractorId}/sam-lookup/search", new SubcontractorEntityLookupRequest(uei, null), tenantId, Permission.ViewSubcontractors);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = await response.Content.ReadFromJsonAsync<SubcontractorEntityLookupResultDto[]>(JsonOptions) ?? [];
        return results.FirstOrDefault() ?? new SubcontractorEntityLookupResultDto(string.Empty, string.Empty, null, null, null, [], null, "SAM.gov", DateTimeOffset.UtcNow);
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<SubcontractorService>();
                services.AddScoped<SubcontractorEntityLookupService>();
                services.AddScoped<ISubcontractorRepository, EfSubcontractorRepository>();
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

    private static HttpRequestMessage CreateRequest<TContent>(HttpMethod method, string requestUri, TContent content, Guid tenantId, Permission permission)
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

    private static void SeedSubcontractor(GccsDbContext dbContext, Guid tenantId, Guid subcontractorId, string name)
    {
        dbContext.Subcontractors.Add(new SubcontractorEntity
        {
            Id = subcontractorId,
            TenantId = tenantId,
            Name = name,
            Status = SubcontractorStatus.Active,
            RoleDescription = "IT services",
            SmallBusinessStatus = "Unknown",
            CmmcStatus = "Unknown",
            NdaStatus = "NotOnFile",
            WorkshareDescription = string.Empty,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private sealed class FakeSamGovLookupClient : ISamGovEntityLookupClient
    {
        public Task<SamGovEntityLookupResult> LookupByUeiAsync(string uei, CancellationToken cancellationToken = default) =>
            SearchAsync(new SamGovEntitySearchRequest(uei, null), cancellationToken);

        public Task<SamGovEntityLookupResult> SearchAsync(SamGovEntitySearchRequest request, CancellationToken cancellationToken = default)
        {
            var query = request.Uei ?? request.LegalBusinessName ?? string.Empty;
            if (query == "NO_MATCH")
            {
                return Task.FromResult(SamGovEntityLookupResult.Success("""{"entityData":[]}"""));
            }

            if (query == "MULTI_MATCH")
            {
                return Task.FromResult(SamGovEntityLookupResult.Success(
                    """
                    {"entityData":[{"legalBusinessName":"Sub One","ueiSAM":"ONE123"},{"legalBusinessName":"Sub Two","ueiSAM":"TWO123"}]}
                    """));
            }

            return Task.FromResult(SamGovEntityLookupResult.Success(
                """
                {
                  "entityData": [
                    {
                      "legalBusinessName": "Sub Federal LLC",
                      "ueiSAM": "SUBUEI123456",
                      "cageCode": "8XYZ1",
                      "registrationStatus": "Active",
                      "expirationDate": "2027-08-31",
                      "exclusionStatus": "No Active Exclusions",
                      "naicsCode": [
                        { "code": "541330", "title": "Engineering Services" }
                      ]
                    }
                  ]
                }
                """));
        }
    }
}
