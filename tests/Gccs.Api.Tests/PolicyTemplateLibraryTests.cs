using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Compliance;
using Gccs.Application.Security;
using Gccs.Domain.Audit;
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

public sealed class PolicyTemplateLibraryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public PolicyTemplateLibraryTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_25_1_1_and_TC_25_1_2_Approved_templates_include_metadata_and_drafts_are_hidden()
    {
        var tenantId = Guid.Parse("25125125-1251-2512-5125-1251251251a1");
        await using var factory = CreateFactory("tc-25-1-1", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        await CreateTemplateAsync(client, tenantId, Template("Access Control Policy", PolicyTemplateStatus.Approved));
        await CreateTemplateAsync(client, tenantId, Template("Draft Incident Policy", PolicyTemplateStatus.Draft) with { SourceReferences = [], LastReviewedAt = null, ReviewerUserId = null });

        var visible = await ListTemplatesAsync(client, tenantId, includeReviewStates: false, Permission.ViewObligations);

        var approved = Assert.Single(visible);
        Assert.Equal("Access Control Policy", approved.Title);
        Assert.Equal("Cybersecurity", approved.Category);
        Assert.Equal("v1.0", approved.Version);
        Assert.Equal("Compliance Content", approved.OwnerFunction);
        Assert.Equal(new DateOnly(2026, 6, 17), approved.LastReviewedAt);
        Assert.NotEmpty(approved.SourceReferences);
        Assert.Equal(PolicyTemplateStatus.Approved, approved.Status);
    }

    [Fact]
    public async Task TC_25_1_3_Deprecated_templates_remain_visible_to_content_reviewers()
    {
        var tenantId = Guid.Parse("25125125-1251-2512-5125-1251251251a3");
        await using var factory = CreateFactory("tc-25-1-3", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        await CreateTemplateAsync(client, tenantId, Template("Deprecated Policy", PolicyTemplateStatus.Deprecated));

        var standard = await ListTemplatesAsync(client, tenantId, includeReviewStates: false, Permission.ViewObligations);
        var reviewer = await ListTemplatesAsync(client, tenantId, includeReviewStates: true, Permission.ViewObligations, Permission.ManageObligations);

        Assert.Empty(standard);
        Assert.Contains(reviewer, template => template.Title == "Deprecated Policy" && template.Status == PolicyTemplateStatus.Deprecated);
    }

    [Fact]
    public async Task TC_25_1_4_Template_approval_requires_source_and_review_metadata()
    {
        var tenantId = Guid.Parse("25125125-1251-2512-5125-1251251251a4");
        await using var factory = CreateFactory("tc-25-1-4", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        var draft = await CreateTemplateAsync(client, tenantId, Template("Metadata Gap Policy", PolicyTemplateStatus.Draft) with
        {
            SourceReferences = [],
            LastReviewedAt = null,
            ReviewerUserId = null
        });

        using var approve = CreateRequest(
            HttpMethod.Put,
            $"/api/policy-templates/{draft.Id}/lifecycle",
            new ChangePolicyTemplateLifecycleRequest(PolicyTemplateStatus.Approved, null, null),
            tenantId,
            Permission.ManageObligations);
        var response = await client.SendAsync(approve);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("sourceReferences", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("lastReviewedAt", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_25_1_5_Template_lifecycle_changes_create_version_history_and_audit_events()
    {
        var tenantId = Guid.Parse("25125125-1251-2512-5125-1251251251a5");
        var reviewerUserId = Guid.Parse("25125125-1251-2512-5125-125125125199");
        await using var factory = CreateFactory("tc-25-1-5", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        var created = await CreateTemplateAsync(client, tenantId, Template("Lifecycle Policy", PolicyTemplateStatus.UnderReview));

        using var approve = CreateRequest(
            HttpMethod.Put,
            $"/api/policy-templates/{created.Id}/lifecycle",
            new ChangePolicyTemplateLifecycleRequest(PolicyTemplateStatus.Approved, reviewerUserId, new DateOnly(2026, 6, 18)),
            tenantId,
            Permission.ManageObligations);
        var approveResponse = await client.SendAsync(approve);
        var approved = await approveResponse.Content.ReadFromJsonAsync<PolicyTemplateDto>(JsonOptions);
        var versions = await ListVersionsAsync(client, tenantId, created.Id);

        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
        Assert.NotNull(approved);
        Assert.Equal(PolicyTemplateStatus.Approved, approved.Status);
        Assert.True(versions.Length >= 2);
        Assert.Contains(versions, version => version.Status == PolicyTemplateStatus.UnderReview);
        Assert.Contains(versions, version => version.Status == PolicyTemplateStatus.Approved);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var audits = await dbContext.AuditLogEntries
            .Where(audit => audit.TenantId == tenantId && audit.EntityType == "PolicyTemplate" && audit.EntityId == created.Id.ToString())
            .ToArrayAsync();
        Assert.Contains(audits, audit => audit.Action == AuditAction.Created);
        Assert.Contains(audits, audit => audit.Action == AuditAction.Approved && audit.MetadataJson.Contains("previousStatus", StringComparison.Ordinal));
    }

    private static async Task<PolicyTemplateDto> CreateTemplateAsync(HttpClient client, Guid tenantId, UpsertPolicyTemplateRequest body)
    {
        using var request = CreateRequest(HttpMethod.Post, "/api/policy-templates", body, tenantId, Permission.ManageObligations);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<PolicyTemplateDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected policy template response.");
    }

    private static async Task<PolicyTemplateDto[]> ListTemplatesAsync(HttpClient client, Guid tenantId, bool includeReviewStates, params Permission[] permissions)
    {
        using var request = CreateRequest<object?>(
            HttpMethod.Get,
            $"/api/policy-templates?includeReviewStates={includeReviewStates.ToString().ToLowerInvariant()}",
            null,
            tenantId,
            permissions);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<PolicyTemplateDto[]>(JsonOptions) ?? [];
    }

    private static async Task<PolicyTemplateVersionDto[]> ListVersionsAsync(HttpClient client, Guid tenantId, Guid templateId)
    {
        using var request = CreateRequest<object?>(HttpMethod.Get, $"/api/policy-templates/{templateId}/versions", null, tenantId, Permission.ManageObligations);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<PolicyTemplateVersionDto[]>(JsonOptions) ?? [];
    }

    private static UpsertPolicyTemplateRequest Template(string title, PolicyTemplateStatus status) =>
        new(
            title,
            "Cybersecurity",
            "This policy applies to {{company_name}} and its covered systems.",
            ["company_name", "system_boundary"],
            [new PolicyTemplateSourceReferenceDto("FAR 52.204-21", "https://www.acquisition.gov/far/52.204-21", new DateOnly(2026, 6, 17))],
            "v1.0",
            status,
            "Compliance Content",
            new DateOnly(2026, 6, 17),
            Guid.Parse("25125125-1251-2512-5125-125125125188"),
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

    private static void SeedTenant(GccsDbContext dbContext, Guid tenantId)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = $"Policy Template Tenant {tenantId:N}",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}
