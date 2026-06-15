using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Companies;
using Gccs.Domain.Audit;
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

public sealed class CompanyProfileTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public CompanyProfileTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_7_1_1_Completion_validates_required_fields()
    {
        var tenantId = Guid.Parse("71717171-7171-7171-7171-7171717171a1");
        await using var factory = CreateFactory("tc-7-1-1", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            HttpMethod.Put,
            "/api/company-profile",
            CreateRequestBody(completeProfile: true) with { Uei = null, CageCode = null, SamRegistrationExpiresAt = null },
            tenantId,
            Guid.NewGuid(),
            Permission.ManageCompanyProfile);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("uei", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cageCode", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samRegistrationExpiresAt", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_7_1_2_Draft_save_persists_without_being_marked_complete()
    {
        var tenantId = Guid.Parse("71717171-7171-7171-7171-7171717171a2");
        await using var factory = CreateFactory("tc-7-1-2", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        using var saveRequest = CreateRequest(
            HttpMethod.Put,
            "/api/company-profile",
            CreateRequestBody(completeProfile: false) with
            {
                Uei = null,
                CageCode = null,
                NaicsCodes = [],
                Locations = [],
                ItEnvironment = new ItEnvironmentSummaryDto("", false, null, [])
            },
            tenantId,
            Guid.NewGuid(),
            Permission.ManageCompanyProfile);

        var saveResponse = await client.SendAsync(saveRequest);
        var saved = await saveResponse.Content.ReadFromJsonAsync<CompanyProfileDto>(JsonOptions);
        using var getRequest = CreateRequest(HttpMethod.Get, "/api/company-profile", tenantId, Guid.NewGuid(), Permission.ViewCompanyProfile);
        var getResponse = await client.SendAsync(getRequest);
        var fetched = await getResponse.Content.ReadFromJsonAsync<CompanyProfileDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, saveResponse.StatusCode);
        Assert.NotNull(saved);
        Assert.False(saved.IsComplete);
        Assert.InRange(saved.CompletionPercentage, 1, 99);
        Assert.NotNull(fetched);
        Assert.Equal(saved.Id, fetched.Id);
        Assert.Equal("Acme Federal Services", fetched.LegalEntityName);
        Assert.False(fetched.IsComplete);
    }

    [Fact]
    public async Task TC_7_1_3_Completion_percentage_recalculates_when_profile_data_changes()
    {
        var tenantId = Guid.Parse("71717171-7171-7171-7171-7171717171a3");
        await using var factory = CreateFactory("tc-7-1-3", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        var actorUserId = Guid.NewGuid();

        using var draftRequest = CreateRequest(
            HttpMethod.Put,
            "/api/company-profile",
            CreateRequestBody(completeProfile: false) with
            {
                Uei = null,
                CageCode = null,
                SamRegistrationExpiresAt = null,
                NaicsCodes = [],
                Certifications = [],
                AgencyCustomers = [],
                ContractorRole = ContractorRole.Unknown,
                ProductsAndServices = "",
                EmployeeRange = CompanyRange.Unknown,
                RevenueRange = CompanyRange.Unknown,
                Locations = [],
                ItEnvironment = new ItEnvironmentSummaryDto("", false, null, []),
                DataHandlingPosture = DataHandlingPosture.Unknown
            },
            tenantId,
            actorUserId,
            Permission.ManageCompanyProfile);
        var draftResponse = await client.SendAsync(draftRequest);
        var draftBody = await draftResponse.Content.ReadAsStringAsync();
        Assert.True(draftResponse.IsSuccessStatusCode, draftBody);
        using var draftJson = JsonDocument.Parse(draftBody);
        var draft = draftJson.Deserialize<CompanyProfileDto>(JsonOptions);

        using var completeRequest = CreateRequest(
            HttpMethod.Put,
            "/api/company-profile",
            CreateRequestBody(completeProfile: true),
            tenantId,
            actorUserId,
            Permission.ManageCompanyProfile);
        var completeResponse = await client.SendAsync(completeRequest);
        var completeBody = await completeResponse.Content.ReadAsStringAsync();
        Assert.True(completeResponse.IsSuccessStatusCode, completeBody);
        using var completeJson = JsonDocument.Parse(completeBody);
        var complete = completeJson.Deserialize<CompanyProfileDto>(JsonOptions);

        using var reducedRequest = CreateRequest(
            HttpMethod.Put,
            "/api/company-profile",
            CreateRequestBody(completeProfile: false) with { Locations = [] },
            tenantId,
            actorUserId,
            Permission.ManageCompanyProfile);
        var reducedResponse = await client.SendAsync(reducedRequest);
        var reducedBody = await reducedResponse.Content.ReadAsStringAsync();
        Assert.True(reducedResponse.IsSuccessStatusCode, reducedBody);
        using var reducedJson = JsonDocument.Parse(reducedBody);
        var reduced = reducedJson.Deserialize<CompanyProfileDto>(JsonOptions);

        Assert.NotNull(draft);
        Assert.NotNull(complete);
        Assert.NotNull(reduced);
        Assert.True(
            complete.CompletionPercentage > draft.CompletionPercentage,
            $"Expected completed profile score to exceed draft score. Draft={draft.CompletionPercentage}, Complete={complete.CompletionPercentage}");
        Assert.True(complete.IsComplete);
        Assert.Equal(100, complete.CompletionPercentage);
        Assert.False(reduced.IsComplete);
        Assert.True(reduced.CompletionPercentage < complete.CompletionPercentage);
    }

    [Fact]
    public async Task TC_7_1_4_Profile_changes_are_audited_and_tenant_scoped()
    {
        var tenantAId = Guid.Parse("71717171-7171-7171-7171-7171717171a4");
        var tenantBId = Guid.Parse("71717171-7171-7171-7171-7171717171b4");
        var actorUserId = Guid.Parse("71717171-7171-7171-7171-7171717171c4");
        await using var factory = CreateFactory("tc-7-1-4", dbContext =>
        {
            SeedTenant(dbContext, tenantAId, "Tenant A");
            SeedTenant(dbContext, tenantBId, "Tenant B");
        });
        using var client = factory.CreateClient();

        using var createRequest = CreateRequest(
            HttpMethod.Put,
            "/api/company-profile",
            CreateRequestBody(completeProfile: false),
            tenantAId,
            actorUserId,
            Permission.ManageCompanyProfile);
        var createResponse = await client.SendAsync(createRequest);
        using var tenantBGetRequest = CreateRequest(HttpMethod.Get, "/api/company-profile", tenantBId, Guid.NewGuid(), Permission.ViewCompanyProfile);
        var tenantBGetResponse = await client.SendAsync(tenantBGetRequest);
        using var updateRequest = CreateRequest(
            HttpMethod.Put,
            "/api/company-profile",
            CreateRequestBody(completeProfile: false) with { LegalEntityName = "Acme Federal Services Updated" },
            tenantAId,
            actorUserId,
            Permission.ManageCompanyProfile);
        var updateResponse = await client.SendAsync(updateRequest);

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, tenantBGetResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var profile = await dbContext.CompanyProfiles.SingleAsync(profile => profile.TenantId == tenantAId);
        var auditEvents = await dbContext.AuditLogEntries
            .Where(audit => audit.TenantId == tenantAId && audit.EntityType == "CompanyProfile")
            .OrderBy(audit => audit.OccurredAt)
            .ToArrayAsync();

        Assert.Equal("Acme Federal Services Updated", profile.LegalEntityName);
        Assert.Equal([AuditAction.Created, AuditAction.Updated], auditEvents.Select(audit => audit.Action).ToArray());
        Assert.All(auditEvents, audit => Assert.Equal(actorUserId, audit.ActorUserId));
    }

    [Fact]
    public async Task TC_7_2_1_Add_multiple_naics_codes_to_profile()
    {
        var tenantId = Guid.Parse("72727272-7272-7272-7272-7272727272a1");
        await using var factory = CreateFactory("tc-7-2-1", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        var requestBody = CreateRequestBody(completeProfile: false) with
        {
            NaicsCodes =
            [
                new CompanyNaicsCodeDto("541330", "Engineering Services", true, "$25.5M", true, new DateOnly(2026, 6, 15)),
                new CompanyNaicsCodeDto("541511", "Custom Computer Programming Services", false, "$34M", true, new DateOnly(2026, 6, 15))
            ]
        };

        using var saveRequest = CreateRequest(HttpMethod.Put, "/api/company-profile", requestBody, tenantId, Guid.NewGuid(), Permission.ManageCompanyProfile);
        var saveResponse = await client.SendAsync(saveRequest);
        var saved = await saveResponse.Content.ReadFromJsonAsync<CompanyProfileDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, saveResponse.StatusCode);
        Assert.NotNull(saved);
        Assert.Equal(["541330", "541511"], saved.NaicsCodes.Select(naics => naics.Code).ToArray());
    }

    [Fact]
    public async Task TC_7_2_2_Only_one_naics_code_is_primary_and_primary_can_switch()
    {
        var tenantId = Guid.Parse("72727272-7272-7272-7272-7272727272a2");
        await using var factory = CreateFactory("tc-7-2-2", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        var actorUserId = Guid.NewGuid();
        var initial = CreateRequestBody(completeProfile: false) with
        {
            NaicsCodes =
            [
                new CompanyNaicsCodeDto("541330", "Engineering Services", true, "$25.5M", true, null),
                new CompanyNaicsCodeDto("541511", "Custom Computer Programming Services", false, "$34M", true, null)
            ]
        };
        var switched = initial with
        {
            NaicsCodes =
            [
                new CompanyNaicsCodeDto("541330", "Engineering Services", false, "$25.5M", true, null),
                new CompanyNaicsCodeDto("541511", "Custom Computer Programming Services", true, "$34M", true, null)
            ]
        };

        using var initialRequest = CreateRequest(HttpMethod.Put, "/api/company-profile", initial, tenantId, actorUserId, Permission.ManageCompanyProfile);
        await client.SendAsync(initialRequest);
        using var switchedRequest = CreateRequest(HttpMethod.Put, "/api/company-profile", switched, tenantId, actorUserId, Permission.ManageCompanyProfile);
        var switchedResponse = await client.SendAsync(switchedRequest);
        var saved = await switchedResponse.Content.ReadFromJsonAsync<CompanyProfileDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, switchedResponse.StatusCode);
        Assert.NotNull(saved);
        Assert.Equal("541511", Assert.Single(saved.NaicsCodes, naics => naics.IsPrimary).Code);
    }

    [Fact]
    public async Task TC_7_2_3_Size_status_and_basis_are_stored_per_naics()
    {
        var tenantId = Guid.Parse("72727272-7272-7272-7272-7272727272a3");
        await using var factory = CreateFactory("tc-7-2-3", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        var requestBody = CreateRequestBody(completeProfile: false) with
        {
            NaicsCodes =
            [
                new CompanyNaicsCodeDto("541330", "Engineering Services", true, "$25.5M receipts basis", true, new DateOnly(2026, 6, 15)),
                new CompanyNaicsCodeDto("236220", "Commercial Building Construction", false, "$45M receipts basis", false, new DateOnly(2026, 6, 15))
            ]
        };

        using var saveRequest = CreateRequest(HttpMethod.Put, "/api/company-profile", requestBody, tenantId, Guid.NewGuid(), Permission.ManageCompanyProfile);
        var saveResponse = await client.SendAsync(saveRequest);
        var saved = await saveResponse.Content.ReadFromJsonAsync<CompanyProfileDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, saveResponse.StatusCode);
        Assert.NotNull(saved);
        Assert.True(saved.NaicsCodes.Single(naics => naics.Code == "541330").QualifiesAsSmall);
        Assert.False(saved.NaicsCodes.Single(naics => naics.Code == "236220").QualifiesAsSmall);
        Assert.Equal("$45M receipts basis", saved.NaicsCodes.Single(naics => naics.Code == "236220").SizeStandard);
    }

    [Fact]
    public async Task TC_7_2_4_Missing_naics_size_status_appears_in_profile_gaps()
    {
        var tenantId = Guid.Parse("72727272-7272-7272-7272-7272727272a4");
        await using var factory = CreateFactory("tc-7-2-4", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        var requestBody = CreateRequestBody(completeProfile: false) with
        {
            NaicsCodes =
            [
                new CompanyNaicsCodeDto("541330", "Engineering Services", true, null, null, null)
            ]
        };

        using var saveRequest = CreateRequest(HttpMethod.Put, "/api/company-profile", requestBody, tenantId, Guid.NewGuid(), Permission.ManageCompanyProfile);
        var saveResponse = await client.SendAsync(saveRequest);
        var saved = await saveResponse.Content.ReadFromJsonAsync<CompanyProfileDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, saveResponse.StatusCode);
        Assert.NotNull(saved);
        Assert.Contains("naicsCodes[0].sizeStatus", saved.ValidationErrors.Keys);
        Assert.Contains("541330", saved.ValidationErrors["naicsCodes[0].sizeStatus"][0], StringComparison.Ordinal);
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
                services.AddScoped<ICompanyProfileRepository, EfCompanyProfileRepository>();
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

    private static UpsertCompanyProfileRequest CreateRequestBody(bool completeProfile) =>
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

    private static HttpRequestMessage CreateRequest<TContent>(
        HttpMethod method,
        string requestUri,
        TContent? content,
        Guid tenantId,
        Guid userId,
        Permission permission)
    {
        var request = CreateRequest(method, requestUri, tenantId, userId, permission);

        if (content is not null)
        {
            request.Content = JsonContent.Create(content, options: JsonOptions);
        }

        return request;
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

    private static void SeedTenant(GccsDbContext dbContext, Guid tenantId, string name = "Company Profile Tenant")
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
