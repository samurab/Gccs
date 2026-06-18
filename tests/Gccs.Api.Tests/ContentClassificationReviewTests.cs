using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Evidence;
using Gccs.Application.Security;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Domain.Evidence;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Common;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ContentClassificationReviewTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public ContentClassificationReviewTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_1A_2_2_2_Unknown_items_appear_in_review_queue()
    {
        var ids = StoryIds.ForCase("tc-1a-2-2-2");
        await using var factory = CreateFactory("tc-1a-2-2-2", dbContext =>
        {
            SeedTenant(dbContext, ids.TenantId);
            SeedEvidence(dbContext, ids.UnknownEvidenceId, ids.TenantId, ContentClassification.Unknown, "Unknown policy");
        });
        using var client = factory.CreateClient();
        using var request = CreateRequest<object?>(HttpMethod.Get, "/api/content-classification-review-items", null, ids.TenantId, ids.ActorUserId, Permission.ViewEvidence);

        var response = await client.SendAsync(request);
        var items = await response.Content.ReadFromJsonAsync<ContentClassificationReviewItemDto[]>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = Assert.Single(items ?? []);
        Assert.Equal("EvidenceItem", item.EntityType);
        Assert.Equal(ContentClassification.Unknown, item.Classification.Classification);
        Assert.Equal("review", item.ReviewRoute);
    }

    [Fact]
    public async Task TC_1A_2_2_3_Prohibited_items_are_routed_to_escalation()
    {
        var ids = StoryIds.ForCase("tc-1a-2-2-3");
        await using var factory = CreateFactory("tc-1a-2-2-3", dbContext =>
        {
            SeedTenant(dbContext, ids.TenantId);
            SeedEvidence(dbContext, ids.UnknownEvidenceId, ids.TenantId, ContentClassification.Prohibited, "Prohibited evidence");
        });
        using var client = factory.CreateClient();
        using var request = CreateRequest<object?>(HttpMethod.Get, "/api/content-classification-review-items", null, ids.TenantId, ids.ActorUserId, Permission.ViewEvidence);

        var response = await client.SendAsync(request);
        var items = await response.Content.ReadFromJsonAsync<ContentClassificationReviewItemDto[]>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = Assert.Single(items ?? []);
        Assert.Equal(ContentClassification.Prohibited, item.Classification.Classification);
        Assert.Equal("escalation", item.ReviewRoute);
    }

    [Fact]
    public async Task TC_1A_2_2_4_Authorized_reviewer_reclassifies_with_reason()
    {
        var ids = StoryIds.ForCase("tc-1a-2-2-4");
        await using var factory = CreateFactory("tc-1a-2-2-4", dbContext =>
        {
            SeedTenant(dbContext, ids.TenantId);
            SeedEvidence(dbContext, ids.UnknownEvidenceId, ids.TenantId, ContentClassification.Unknown, "Unknown policy");
        });
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            HttpMethod.Patch,
            $"/api/evidence-items/{ids.UnknownEvidenceId}/classification",
            new ReclassifyContentRequest(new ContentClassificationRequest(
                ContentClassification.Fci,
                ContentClassificationSource.AdminReviewed,
                ReviewedByUserId: ids.ActorUserId,
                ReviewedAt: DateTimeOffset.Parse("2026-06-18T20:00:00Z"),
                Reason: "Reviewer confirmed FCI.")),
            ids.TenantId,
            ids.ActorUserId,
            Permission.ApproveEvidence);

        var response = await client.SendAsync(request);
        var updated = await response.Content.ReadFromJsonAsync<EvidenceMetadataDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(updated);
        Assert.Equal(ContentClassification.Fci, updated.Classification.Classification);
        Assert.Equal("Reviewer confirmed FCI.", updated.Classification.Reason);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Contains(await dbContext.ContentClassificationHistory.ToArrayAsync(), item =>
            item.EntityId == ids.UnknownEvidenceId.ToString() &&
            item.PreviousClassification == ContentClassification.Unknown &&
            item.NewClassification == ContentClassification.Fci);
        Assert.Contains(await dbContext.AuditLogEntries.ToArrayAsync(), item =>
            item.EntityType == "EvidenceItem" &&
            item.EntityId == ids.UnknownEvidenceId.ToString() &&
            item.Action == AuditAction.Updated);
    }

    [Fact]
    public async Task TC_1A_2_2_4_Reclassification_requires_approval_permission()
    {
        var ids = StoryIds.ForCase("tc-1a-2-2-4-denied");
        await using var factory = CreateFactory("tc-1a-2-2-4-denied", dbContext =>
        {
            SeedTenant(dbContext, ids.TenantId);
            SeedEvidence(dbContext, ids.UnknownEvidenceId, ids.TenantId, ContentClassification.Unknown, "Unknown policy");
        });
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            HttpMethod.Patch,
            $"/api/evidence-items/{ids.UnknownEvidenceId}/classification",
            new ReclassifyContentRequest(new ContentClassificationRequest(ContentClassification.Fci, Reason: "Reviewer confirmed FCI.")),
            ids.TenantId,
            ids.ActorUserId,
            Permission.ManageEvidence);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<TenantDataHandlingModePolicyService>();
                services.AddScoped<ContentClassificationPolicy>();
                services.AddScoped<ContentClassificationReviewService>();
                services.AddScoped<IContentClassificationReviewRepository, EfContentClassificationReviewRepository>();
                services.AddScoped<ITenantRepository, EfTenantRepository>();
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
        TContent? content,
        Guid tenantId,
        Guid userId,
        params Permission[] permissions)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
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
            Name = $"Classification Review Tenant {tenantId:N}",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private static void SeedEvidence(GccsDbContext dbContext, Guid evidenceId, Guid tenantId, ContentClassification classification, string name)
    {
        dbContext.EvidenceItems.Add(new EvidenceItemEntity
        {
            Id = evidenceId,
            TenantId = tenantId,
            Name = name,
            Description = name,
            Type = EvidenceType.Policy,
            OwnerFunction = "Compliance",
            Status = EvidenceStatus.InReview,
            TagsJson = "[]",
            Classification = classification,
            ClassificationSource = ContentClassificationSource.UserSelected,
            ClassificationReason = $"Seeded {classification} classification.",
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private sealed record StoryIds(Guid TenantId, Guid ActorUserId, Guid UnknownEvidenceId)
    {
        public static StoryIds ForCase(string caseName)
        {
            var suffix = Math.Abs(caseName.GetHashCode(StringComparison.Ordinal)) % 10000;
            return new StoryIds(
                Guid.Parse($"1a222222-2222-2222-2222-22222211{suffix:D4}"),
                Guid.Parse($"1a222222-2222-2222-2222-22222212{suffix:D4}"),
                Guid.Parse($"1a222222-2222-2222-2222-22222213{suffix:D4}"));
        }
    }
}
