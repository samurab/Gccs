using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Compliance;
using Gccs.Domain.Common;
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

public sealed class ClauseLibrarySearchTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public ClauseLibrarySearchTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_9_1_1_Search_clause_library_by_number_title_and_category()
    {
        var tenantId = Guid.Parse("91919191-9191-9191-9191-9191919191a1");
        await using var factory = CreateFactory("tc-9-1-1", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            dbContext.Clauses.AddRange(
                CreateClause("far-52-204-27", "FAR 52.204-27", "52.204-27", "Prohibition on a ByteDance Covered Application"),
                CreateClause("far-52-222-41", "FAR 52.222-41", "52.222-41", "Service Contract Labor Standards"),
                CreateClause("dfars-252-204-7012", "DFARS 252.204-7012", "252.204-7012", "Safeguarding Covered Defense Information"));
        });
        using var client = factory.CreateClient();

        var numberResults = await SearchAsync(client, "/api/clauses?query=52.204-27", tenantId);
        var titleResults = await SearchAsync(client, "/api/clauses?query=Service%20Contract", tenantId);
        var categoryResults = await SearchAsync(client, "/api/clauses?category=DFARS", tenantId);

        Assert.Equal(["52.204-27"], numberResults.Select(clause => clause.Number).ToArray());
        Assert.Equal(["Service Contract Labor Standards"], titleResults.Select(clause => clause.Title).ToArray());
        Assert.Equal(["DFARS"], categoryResults.Select(clause => clause.Category).Distinct().ToArray());
        Assert.Equal("252.204-7012", Assert.Single(categoryResults).Number);
    }

    [Fact]
    public async Task TC_9_1_2_Only_published_clauses_are_available_for_customer_mapping()
    {
        var tenantId = Guid.Parse("91919191-9191-9191-9191-9191919191a2");
        await using var factory = CreateFactory("tc-9-1-2", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            dbContext.Clauses.AddRange(
                CreateClause("published-clause", "FAR 52.204-21", "52.204-21", "Basic Safeguarding", ReviewState.Published),
                CreateClause("draft-clause", "FAR 52.204-99", "52.204-99", "Draft Hidden Clause", ReviewState.Draft));
        });
        using var client = factory.CreateClient();

        var results = await SearchAsync(client, "/api/clauses", tenantId);

        var clause = Assert.Single(results);
        Assert.Equal("published-clause", clause.Id);
        Assert.True(clause.IsMappable);
        Assert.DoesNotContain(results, result => result.Id == "draft-clause");
    }

    [Fact]
    public async Task TC_9_1_3_Search_results_show_source_url_and_last_reviewed_date()
    {
        var tenantId = Guid.Parse("91919191-9191-9191-9191-9191919191a3");
        await using var factory = CreateFactory("tc-9-1-3", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            dbContext.Clauses.Add(CreateClause("far-52-204-25", "FAR 52.204-25", "52.204-25", "Telecom Restrictions"));
        });
        using var client = factory.CreateClient();

        var results = await SearchAsync(client, "/api/clauses?query=52.204-25", tenantId);

        var clause = Assert.Single(results);
        Assert.Equal("https://www.acquisition.gov/far/52.204-25", clause.SourceUrl);
        Assert.Equal(new DateOnly(2026, 6, 3), clause.LastReviewedAt);
    }

    [Fact]
    public async Task TC_9_1_4_Search_does_not_expose_draft_retired_or_other_tenant_custom_content()
    {
        var tenantAId = Guid.Parse("91919191-9191-9191-9191-9191919191a4");
        var tenantBId = Guid.Parse("91919191-9191-9191-9191-9191919191b4");
        await using var factory = CreateFactory("tc-9-1-4", dbContext =>
        {
            SeedTenant(dbContext, tenantAId, "Tenant A");
            SeedTenant(dbContext, tenantBId, "Tenant B");
            dbContext.Clauses.AddRange(
                CreateClause("tenant-a-custom", "Custom", "CUSTOM-A", "Tenant A Custom Clause", ReviewState.Published, tenantAId),
                CreateClause("tenant-b-custom", "Custom", "CUSTOM-B", "Tenant B Custom Clause", ReviewState.Published, tenantBId),
                CreateClause("draft-custom", "Custom", "CUSTOM-DRAFT", "Draft Custom Clause", ReviewState.Draft, tenantAId),
                CreateClause("retired-custom", "Custom", "CUSTOM-RETIRED", "Retired Custom Clause", ReviewState.Retired, tenantAId));
        });
        using var client = factory.CreateClient();

        var results = await SearchAsync(client, "/api/clauses?category=Custom", tenantAId);
        var tenantBSearch = await SearchAsync(client, "/api/clauses?query=Tenant%20B%20Custom", tenantAId);

        Assert.Equal(["tenant-a-custom"], results.Select(clause => clause.Id).ToArray());
        Assert.Empty(tenantBSearch);
        Assert.DoesNotContain(results, clause => clause.Id is "draft-custom" or "retired-custom" or "tenant-b-custom");
    }

    private async Task<ClauseLibraryItemDto[]> SearchAsync(HttpClient client, string requestUri, Guid tenantId)
    {
        using var request = CreateRequest(HttpMethod.Get, requestUri, tenantId, Guid.NewGuid(), Permission.ViewContracts);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<ClauseLibraryItemDto[]>(JsonOptions) ?? [];
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<ClauseLibraryService>();
                services.AddScoped<IClauseLibraryRepository, EfClauseLibraryRepository>();

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
                seed?.Invoke(dbContext);
                dbContext.SaveChanges();
            });
        });

    private static ClauseEntity CreateClause(
        string id,
        string source,
        string number,
        string title,
        ReviewState reviewState = ReviewState.Published,
        Guid? tenantId = null) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            Source = source,
            Number = number,
            Title = title,
            PlainEnglishSummary = $"{title} summary.",
            ApplicabilityLogic = "Clause appears in a contract or flow-down attachment.",
            ClauseTextVersion = "current",
            RequiredActionIdsJson = "[]",
            SourceName = source,
            SourceUrl = source.StartsWith("FAR", StringComparison.OrdinalIgnoreCase)
                ? $"https://www.acquisition.gov/far/{number}"
                : "https://www.acquisition.gov/dfars",
            SourceLastReviewedAt = new DateOnly(2026, 6, 3),
            SourceConfidence = "high",
            LastReviewedAt = new DateOnly(2026, 6, 3),
            Confidence = "high",
            ReviewState = reviewState
        };

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

    private static void SeedTenant(GccsDbContext dbContext, Guid tenantId, string name = "Clause Tenant")
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
