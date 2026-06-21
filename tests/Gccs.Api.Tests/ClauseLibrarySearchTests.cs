using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Compliance;
using Gccs.Infrastructure.Audit;
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
        var farCategoryResults = await SearchAsync(client, "/api/clauses?category=FAR", tenantId);

        Assert.Equal(["52.204-27"], numberResults.Select(clause => clause.Number).ToArray());
        Assert.Equal(["Service Contract Labor Standards"], titleResults.Select(clause => clause.Title).ToArray());
        Assert.Equal(["DFARS"], categoryResults.Select(clause => clause.Category).Distinct().ToArray());
        Assert.Equal("252.204-7012", Assert.Single(categoryResults).Number);
        Assert.Equal(["52.204-27", "52.222-41"], farCategoryResults.Select(clause => clause.Number).Order(StringComparer.Ordinal).ToArray());
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

    [Fact]
    public async Task TC_20_2_1_through_TC_20_2_4_Search_supports_exact_keyword_source_area_and_flow_down_filters()
    {
        var tenantId = Guid.Parse("20220220-2202-2022-0220-2202202202a1");
        await using var factory = CreateFactory("tc-20-2-filters", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            dbContext.Clauses.AddRange(
                CreateClause("far-flow", "FAR 52.204-21", "52.204-21", "Basic Safeguarding", requiresFlowDown: true),
                CreateClause("dfars-cmmc", "DFARS 252.204-7012", "252.204-7012", "Safeguarding Covered Defense Information", requiresFlowDown: true),
                CreateClause("far-tiktok", "FAR 52.204-27", "52.204-27", "Prohibition on a ByteDance Covered Application", requiresFlowDown: false));
        });
        using var client = factory.CreateClient();

        var exact = await SearchAsync(client, "/api/clauses?query=52.204-21", tenantId);
        var keyword = await SearchAsync(client, "/api/clauses?query=Safeguarding", tenantId);
        var sourceFiltered = await SearchAsync(client, "/api/clauses?sourceFamily=DFARS", tenantId);
        var areaFiltered = await SearchAsync(client, "/api/clauses?obligationArea=ByteDance", tenantId);
        var flowDownFiltered = await SearchAsync(client, "/api/clauses?requiresFlowDown=true", tenantId);

        Assert.Equal(["far-flow"], exact.Select(clause => clause.Id).ToArray());
        Assert.Equal(["dfars-cmmc", "far-flow"], keyword.Select(clause => clause.Id).Order(StringComparer.Ordinal).ToArray());
        Assert.Equal(["DFARS"], sourceFiltered.Select(clause => clause.Category).Distinct().ToArray());
        Assert.Equal("far-tiktok", Assert.Single(areaFiltered).Id);
        Assert.Equal(["dfars-cmmc", "far-flow"], flowDownFiltered.Select(clause => clause.Id).Order(StringComparer.Ordinal).ToArray());
        Assert.Equal("Published", Assert.Single(exact).ReviewState);
        Assert.Equal("high", Assert.Single(exact).Confidence);
        Assert.True(Assert.Single(exact).RequiresFlowDown);
    }

    [Fact]
    public async Task TC_20_2_5_Draft_clauses_require_content_review_permission_to_search()
    {
        var tenantId = Guid.Parse("20220220-2202-2022-0220-2202202202a5");
        await using var factory = CreateFactory("tc-20-2-drafts", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            dbContext.Clauses.AddRange(
                CreateClause("published-review", "FAR 52.204-21", "52.204-21", "Basic Safeguarding", ReviewState.Published),
                CreateClause("draft-review", "FAR 52.204-99", "52.204-99", "Draft Review Clause", ReviewState.Draft));
        });
        using var client = factory.CreateClient();

        var standardUserResults = await SearchAsync(client, "/api/clauses?includeDrafts=true", tenantId);
        var reviewerResults = await SearchAsync(
            client,
            "/api/clauses?includeDrafts=true",
            tenantId,
            Permission.ViewContracts,
            Permission.ManageObligations);

        Assert.Equal(["published-review"], standardUserResults.Select(clause => clause.Id).ToArray());
        Assert.Equal(["draft-review", "published-review"], reviewerResults.Select(clause => clause.Id).Order(StringComparer.Ordinal).ToArray());
        Assert.False(reviewerResults.Single(clause => clause.Id == "draft-review").IsMappable);
    }

    [Fact]
    public async Task TC_20_1_1_through_TC_20_1_4_Version_history_keeps_superseded_clauses_out_of_default_mapping()
    {
        var tenantId = Guid.Parse("20120120-1201-2012-0120-1201201201a1");
        var reviewerUserId = Guid.Parse("20120120-1201-2012-0120-1201201201b1");
        await using var factory = CreateFactory("tc-20-1-history", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            dbContext.Clauses.AddRange(
                CreateClause(
                    "far-52-204-21-v1",
                    "FAR 52.204-21",
                    "52.204-21",
                    "Basic Safeguarding V1",
                    ReviewState.Retired,
                    null,
                    "v1",
                    reviewerUserId,
                    "far-52-204-21-v2",
                    new DateOnly(2026, 6, 1)),
                CreateClause(
                    "far-52-204-21-v2",
                    "FAR 52.204-21",
                    "52.204-21",
                    "Basic Safeguarding V2",
                    ReviewState.Published,
                    null,
                    "v2",
                    reviewerUserId));
        });
        using var client = factory.CreateClient();

        var searchResults = await SearchAsync(client, "/api/clauses?query=52.204-21", tenantId);
        using var detailRequest = CreateRequest(HttpMethod.Get, "/api/clauses/far-52-204-21-v1", tenantId, Guid.NewGuid(), Permission.ViewContracts);
        var detailResponse = await client.SendAsync(detailRequest);
        var detail = await detailResponse.Content.ReadFromJsonAsync<ClauseLibraryDetailDto>(JsonOptions);

        Assert.Equal(["far-52-204-21-v2"], searchResults.Select(clause => clause.Id).ToArray());
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        Assert.NotNull(detail);
        Assert.Equal("52.204-21", detail.Clause.Number);
        Assert.Equal("Retired", detail.Clause.ReviewState);
        Assert.Equal("v1", detail.Clause.ClauseTextVersion);
        Assert.Equal(reviewerUserId, detail.Clause.ReviewedByUserId);
        Assert.Equal("far-52-204-21-v2", detail.Clause.SupersededByClauseId);
        Assert.Equal(["far-52-204-21-v1", "far-52-204-21-v2"], detail.VersionHistory.Select(clause => clause.Id).Order(StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public async Task TC_20_1_5_Clause_review_state_changes_are_audit_logged()
    {
        var tenantId = Guid.Parse("20120120-1201-2012-0120-1201201201a5");
        var actorUserId = Guid.Parse("20120120-1201-2012-0120-1201201201b5");
        var reviewerUserId = Guid.Parse("20120120-1201-2012-0120-1201201201c5");
        await using var factory = CreateFactory("tc-20-1-audit", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            dbContext.Clauses.Add(CreateClause("audit-clause", "FAR 52.204-25", "52.204-25", "Telecom Restrictions", ReviewState.Draft));
        });
        using var client = factory.CreateClient();

        using var request = CreateRequest(
            HttpMethod.Patch,
            "/api/clauses/audit-clause/review-state",
            tenantId,
            actorUserId,
            Permission.ManageObligations);
        request.Content = JsonContent.Create(new ChangeComplianceContentReviewStateRequest(
            ReviewState.Published,
            reviewerUserId,
            new DateOnly(2026, 6, 17)),
            options: JsonOptions);
        var response = await client.SendAsync(request);
        var updated = await response.Content.ReadFromJsonAsync<ComplianceContentReviewDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(updated);
        Assert.Equal(ReviewState.Published, updated.State);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var audit = await dbContext.AuditLogEntries.SingleAsync(entry => entry.EntityType == "Clause" && entry.EntityId == "audit-clause");
        Assert.Equal(actorUserId, audit.ActorUserId);
        Assert.Contains("Published", audit.MetadataJson, StringComparison.Ordinal);
    }

    private async Task<ClauseLibraryItemDto[]> SearchAsync(
        HttpClient client,
        string requestUri,
        Guid tenantId,
        params Permission[] permissions)
    {
        var effectivePermissions = permissions.Length == 0 ? [Permission.ViewContracts] : permissions;
        using var request = CreateRequest(HttpMethod.Get, requestUri, tenantId, Guid.NewGuid(), effectivePermissions);
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
                services.AddScoped<ComplianceContentReviewService>();
                services.AddScoped<IComplianceContentReviewRepository, EfComplianceContentReviewRepository>();
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

    private static ClauseEntity CreateClause(
        string id,
        string source,
        string number,
        string title,
        ReviewState reviewState = ReviewState.Published,
        Guid? tenantId = null,
        string clauseTextVersion = "current",
        Guid? reviewedByUserId = null,
        string? supersededByClauseId = null,
        DateOnly? supersededAt = null,
        bool requiresFlowDown = false) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            Source = source,
            Number = number,
            Title = title,
            PlainEnglishSummary = $"{title} summary.",
            ApplicabilityLogic = "Clause appears in a contract or flow-down attachment.",
            ClauseTextVersion = clauseTextVersion,
            SupersededByClauseId = supersededByClauseId,
            SupersededAt = supersededAt,
            RequiredActionIdsJson = "[]",
            UsuallyRequiresFlowDown = requiresFlowDown,
            SourceName = source,
            SourceUrl = source.StartsWith("FAR", StringComparison.OrdinalIgnoreCase)
                ? $"https://www.acquisition.gov/far/{number}"
                : "https://www.acquisition.gov/dfars",
            SourceLastReviewedAt = new DateOnly(2026, 6, 3),
            SourceConfidence = "high",
            LastReviewedAt = new DateOnly(2026, 6, 3),
            ReviewedByUserId = reviewedByUserId,
            Confidence = "high",
            ReviewState = reviewState
        };

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        string requestUri,
        Guid tenantId,
        Guid userId,
        params Permission[] permissions)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", string.Join(",", permissions.Select(permission => permission.ToString())));
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
