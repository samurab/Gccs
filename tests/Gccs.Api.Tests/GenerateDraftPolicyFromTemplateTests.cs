using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Compliance;
using Gccs.Application.Security;
using Gccs.Domain.Companies;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Compliance;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class GenerateDraftPolicyFromTemplateTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public GenerateDraftPolicyFromTemplateTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_25_2_1_Generates_draft_policy_from_approved_template_only()
    {
        var tenantId = Guid.Parse("25225225-2252-2522-5225-2252252252a1");
        await using var factory = CreateFactory("tc-25-2-1", dbContext => SeedTenantAndCompany(dbContext, tenantId));
        using var client = factory.CreateClient();
        var approved = await CreateTemplateAsync(client, tenantId, Template("Approved Policy", PolicyTemplateStatus.Approved));
        var draft = await CreateTemplateAsync(client, tenantId, Template("Draft Policy", PolicyTemplateStatus.Draft) with { SourceReferences = [], LastReviewedAt = null, ReviewerUserId = null });

        var generated = await GenerateAsync(client, tenantId, approved.Id, HttpStatusCode.Created);
        using var draftGenerate = CreateRequest<object?>(HttpMethod.Post, $"/api/policy-templates/{draft.Id}/generate", new { }, tenantId, Permission.ManageEvidence);
        var draftResponse = await client.SendAsync(draftGenerate);

        Assert.NotNull(generated);
        Assert.Equal(GeneratedPolicyStatus.Draft, generated.Status);
        Assert.Equal(approved.Id, generated.SourceTemplateId);
        Assert.Equal(HttpStatusCode.NotFound, draftResponse.StatusCode);
    }

    [Fact]
    public async Task TC_25_2_2_and_TC_25_2_3_Populates_available_placeholders_and_flags_missing_values()
    {
        var tenantId = Guid.Parse("25225225-2252-2522-5225-2252252252a2");
        await using var factory = CreateFactory("tc-25-2-2", dbContext => SeedTenantAndCompany(dbContext, tenantId));
        using var client = factory.CreateClient();
        var template = await CreateTemplateAsync(client, tenantId, Template("Placeholder Policy", PolicyTemplateStatus.Approved));

        var generated = await GenerateAsync(client, tenantId, template.Id, HttpStatusCode.Created);

        Assert.NotNull(generated);
        Assert.Contains("Acme Federal Services LLC", generated.Body, StringComparison.Ordinal);
        Assert.Equal("Acme Federal Services LLC", generated.PlaceholderValues["company_name"]);
        Assert.Equal("ABCDEF123456", generated.PlaceholderValues["uei"]);
        Assert.Contains("unmapped_placeholder", generated.MissingPlaceholders);
        Assert.Contains("{{unmapped_placeholder}}", generated.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TC_25_2_4_and_TC_25_2_5_Generated_policy_stores_source_version_generation_date_and_stays_draft()
    {
        var tenantId = Guid.Parse("25225225-2252-2522-5225-2252252252a4");
        await using var factory = CreateFactory("tc-25-2-4", dbContext => SeedTenantAndCompany(dbContext, tenantId));
        using var client = factory.CreateClient();
        var template = await CreateTemplateAsync(client, tenantId, Template("Versioned Policy", PolicyTemplateStatus.Approved) with { Version = "v2.1" });

        var generated = await GenerateAsync(client, tenantId, template.Id, HttpStatusCode.Created);
        Assert.NotNull(generated);

        var fetched = await GetGeneratedPolicyAsync(client, tenantId, generated.Id);
        Assert.Equal("v2.1", fetched.SourceTemplateVersion);
        Assert.True(fetched.GeneratedAt <= DateTimeOffset.UtcNow);
        Assert.Equal(GeneratedPolicyStatus.Draft, fetched.Status);

        using var update = CreateRequest(
            HttpMethod.Put,
            $"/api/generated-policies/{fetched.Id}",
            new UpdateGeneratedPolicyRequest("Edited Policy", fetched.Body + "\nReviewed by compliance."),
            tenantId,
            Permission.ManageEvidence);
        var updateResponse = await client.SendAsync(update);
        var updated = await updateResponse.Content.ReadFromJsonAsync<GeneratedPolicyDto>(JsonOptions);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.NotNull(updated);
        Assert.Equal(GeneratedPolicyStatus.Draft, updated.Status);
        Assert.Equal("Edited Policy", updated.Title);
    }

    [Fact]
    public async Task TC_25_2_6_Generation_is_tenant_scoped_and_requires_manage_evidence()
    {
        var tenantId = Guid.Parse("25225225-2252-2522-5225-2252252252a6");
        var otherTenantId = Guid.Parse("25225225-2252-2522-5225-2252252252b6");
        await using var factory = CreateFactory("tc-25-2-6", dbContext =>
        {
            SeedTenantAndCompany(dbContext, tenantId);
            SeedTenantAndCompany(dbContext, otherTenantId);
        });
        using var client = factory.CreateClient();
        var template = await CreateTemplateAsync(client, tenantId, Template("Scoped Policy", PolicyTemplateStatus.Approved));

        using var noPermission = CreateRequest<object?>(HttpMethod.Post, $"/api/policy-templates/{template.Id}/generate", new { }, tenantId, Permission.ViewEvidence);
        var noPermissionResponse = await client.SendAsync(noPermission);
        using var otherTenant = CreateRequest<object?>(HttpMethod.Post, $"/api/policy-templates/{template.Id}/generate", new { }, otherTenantId, Permission.ManageEvidence);
        var otherTenantResponse = await client.SendAsync(otherTenant);

        Assert.Equal(HttpStatusCode.Forbidden, noPermissionResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, otherTenantResponse.StatusCode);
    }

    private static async Task<PolicyTemplateDto> CreateTemplateAsync(HttpClient client, Guid tenantId, UpsertPolicyTemplateRequest body)
    {
        using var request = CreateRequest(HttpMethod.Post, "/api/policy-templates", body, tenantId, Permission.ManageObligations);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<PolicyTemplateDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected policy template response.");
    }

    private static async Task<GeneratedPolicyDto?> GenerateAsync(HttpClient client, Guid tenantId, Guid templateId, HttpStatusCode expectedStatus)
    {
        using var request = CreateRequest<object?>(HttpMethod.Post, $"/api/policy-templates/{templateId}/generate", new { }, tenantId, Permission.ManageEvidence);
        var response = await client.SendAsync(request);
        Assert.Equal(expectedStatus, response.StatusCode);
        return expectedStatus == HttpStatusCode.Created
            ? await response.Content.ReadFromJsonAsync<GeneratedPolicyDto>(JsonOptions)
            : null;
    }

    private static async Task<GeneratedPolicyDto> GetGeneratedPolicyAsync(HttpClient client, Guid tenantId, Guid policyId)
    {
        using var request = CreateRequest<object?>(HttpMethod.Get, $"/api/generated-policies/{policyId}", null, tenantId, Permission.ViewEvidence);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<GeneratedPolicyDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected generated policy response.");
    }

    private static UpsertPolicyTemplateRequest Template(string title, PolicyTemplateStatus status) =>
        new(
            title,
            "Cybersecurity",
            "Policy for {{company_name}} with UEI {{uei}} and {{unmapped_placeholder}}.",
            ["company_name", "uei", "unmapped_placeholder"],
            [new PolicyTemplateSourceReferenceDto("FAR 52.204-21", "https://www.acquisition.gov/far/52.204-21", new DateOnly(2026, 6, 17))],
            "v1.0",
            status,
            "Compliance Content",
            new DateOnly(2026, 6, 17),
            Guid.Parse("25225225-2252-2522-5225-225225225288"),
            false);

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<PolicyTemplateService>();
                services.AddScoped<IPolicyTemplateRepository, EfPolicyTemplateRepository>();
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
        params Permission[] permissions)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", Guid.NewGuid().ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", string.Join(",", permissions.Select(permission => permission.ToString())));
        if (content is not null)
        {
            request.Content = JsonContent.Create(content, options: JsonOptions);
        }

        return request;
    }

    private static void SeedTenantAndCompany(GccsDbContext dbContext, Guid tenantId)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = $"Generated Policy Tenant {tenantId:N}",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.CompanyProfiles.Add(new CompanyProfileEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LegalEntityName = "Acme Federal Services LLC",
            Uei = "ABCDEF123456",
            CageCode = "1AB23",
            ContractorRole = ContractorRole.Prime,
            ProductsAndServices = "IT services",
            EmployeeRange = CompanyRange.Small,
            RevenueRange = CompanyRange.Small,
            ItEnvironmentDescription = "Commercial cloud",
            DataHandlingPosture = DataHandlingPosture.FciOnly,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}
