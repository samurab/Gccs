using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Evidence;
using Gccs.Application.Security;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Domain.Evidence;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Evidence;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class EvidenceRequestSubmissionReviewTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public EvidenceRequestSubmissionReviewTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_26_2_1_and_TC_26_2_2_Assignee_can_submit_tenant_scoped_non_cui_evidence()
    {
        var ids = StoryIds.ForCase("tc-26-2-1");
        await using var factory = CreateFactory("tc-26-2-1", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();
        var request = await CreateRequestAsync(client, ids);

        var submitted = await SubmitAsync(client, ids, request.Id, new SubmitEvidenceRequestRequest(ids.EvidenceItemId, false, "Submitted policy."));
        using var cui = CreateHttpRequest(HttpMethod.Put, $"/api/evidence-requests/{request.Id}/submit", new SubmitEvidenceRequestRequest(ids.EvidenceItemId, true, "CUI"), ids.TenantId, ids.AssigneeUserId, Permission.ManageEvidence);
        var cuiResponse = await client.SendAsync(cui);
        using var otherTenantEvidence = CreateHttpRequest(HttpMethod.Put, $"/api/evidence-requests/{request.Id}/submit", new SubmitEvidenceRequestRequest(ids.OtherTenantEvidenceItemId, false, "Wrong tenant"), ids.TenantId, ids.AssigneeUserId, Permission.ManageEvidence);
        var otherTenantResponse = await client.SendAsync(otherTenantEvidence);

        Assert.Equal(EvidenceRequestStatus.Submitted, submitted.Status);
        Assert.Equal(ids.EvidenceItemId, submitted.SubmittedEvidenceItemId);
        Assert.Equal("Submitted policy.", submitted.SubmissionComment);
        Assert.Equal(HttpStatusCode.BadRequest, cuiResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, otherTenantResponse.StatusCode);
    }

    [Fact]
    public async Task TC_26_2_3_and_TC_26_2_4_Reviewer_accepts_or_returns_and_accepted_evidence_is_linked()
    {
        var ids = StoryIds.ForCase("tc-26-2-3");
        await using var factory = CreateFactory("tc-26-2-3", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();
        var acceptedRequest = await CreateRequestAsync(client, ids);
        await SubmitAsync(client, ids, acceptedRequest.Id, new SubmitEvidenceRequestRequest(ids.EvidenceItemId, false, "Ready."));

        var accepted = await ReviewAsync(client, ids, acceptedRequest.Id, new ReviewEvidenceRequestRequest(EvidenceRequestReviewDecision.Accept, "Accepted."));
        var returnedRequest = await CreateRequestAsync(client, ids);
        await SubmitAsync(client, ids, returnedRequest.Id, new SubmitEvidenceRequestRequest(ids.SecondEvidenceItemId, false, "Needs review."));
        var returned = await ReviewAsync(client, ids, returnedRequest.Id, new ReviewEvidenceRequestRequest(EvidenceRequestReviewDecision.Return, "Please update."));

        Assert.Equal(EvidenceRequestStatus.Accepted, accepted.Status);
        Assert.Equal("Accepted.", accepted.ReviewComment);
        Assert.Equal(EvidenceRequestStatus.Returned, returned.Status);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.True(await dbContext.Set<EvidenceObligationEntity>().AnyAsync(link =>
            link.EvidenceItemId == ids.EvidenceItemId && link.ObligationId == ids.ObligationId));
        Assert.True(await dbContext.NotificationDeliveries.AnyAsync(notification =>
            notification.SourceTaskId == returnedRequest.Id &&
            notification.UserId == ids.AssigneeUserId &&
            notification.Category == "evidence_request_returned"));
    }

    [Fact]
    public async Task TC_26_2_5_Status_changes_and_review_decisions_are_audit_logged()
    {
        var ids = StoryIds.ForCase("tc-26-2-5");
        await using var factory = CreateFactory("tc-26-2-5", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();
        var request = await CreateRequestAsync(client, ids);
        await SubmitAsync(client, ids, request.Id, new SubmitEvidenceRequestRequest(ids.EvidenceItemId, false, "Submitted."));
        await ReviewAsync(client, ids, request.Id, new ReviewEvidenceRequestRequest(EvidenceRequestReviewDecision.Accept, "Accepted."));

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var audits = await dbContext.AuditLogEntries
            .Where(audit => audit.TenantId == ids.TenantId && audit.EntityType == "EvidenceRequest" && audit.EntityId == request.Id.ToString())
            .ToArrayAsync();
        Assert.Contains(audits, audit => audit.Action == AuditAction.Updated && audit.MetadataJson.Contains("Submitted", StringComparison.Ordinal));
        Assert.Contains(audits, audit => audit.Action == AuditAction.Approved && audit.MetadataJson.Contains("Accepted", StringComparison.Ordinal));
    }

    private static async Task<EvidenceRequestDto> CreateRequestAsync(HttpClient client, StoryIds ids)
    {
        using var request = CreateHttpRequest(
            HttpMethod.Post,
            "/api/evidence-requests",
            new CreateEvidenceRequestRequest(EvidenceRequestRelatedRecordType.Obligation, ids.ObligationId, ids.AssigneeUserId, null, new DateOnly(2026, 9, 1), "Submit policy evidence."),
            ids.TenantId,
            ids.RequesterUserId,
            Permission.ManageEvidence);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<EvidenceRequestDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected evidence request.");
    }

    private static async Task<EvidenceRequestDto> SubmitAsync(HttpClient client, StoryIds ids, Guid requestId, SubmitEvidenceRequestRequest body)
    {
        using var request = CreateHttpRequest(HttpMethod.Put, $"/api/evidence-requests/{requestId}/submit", body, ids.TenantId, ids.AssigneeUserId, Permission.ManageEvidence);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<EvidenceRequestDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected submitted request.");
    }

    private static async Task<EvidenceRequestDto> ReviewAsync(HttpClient client, StoryIds ids, Guid requestId, ReviewEvidenceRequestRequest body)
    {
        using var request = CreateHttpRequest(HttpMethod.Put, $"/api/evidence-requests/{requestId}/review", body, ids.TenantId, ids.RequesterUserId, Permission.ApproveEvidence);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<EvidenceRequestDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected reviewed request.");
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<EvidenceRequestService>();
                services.AddScoped<IEvidenceRequestRepository, EfEvidenceRequestRepository>();
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

    private static HttpRequestMessage CreateHttpRequest<TContent>(HttpMethod method, string requestUri, TContent content, Guid tenantId, Guid userId, params Permission[] permissions)
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

    private static void SeedScenario(GccsDbContext dbContext, StoryIds ids)
    {
        dbContext.Tenants.AddRange(CreateTenant(ids.TenantId), CreateTenant(ids.OtherTenantId));
        dbContext.Users.AddRange(CreateUser(ids.TenantId, ids.RequesterUserId), CreateUser(ids.TenantId, ids.AssigneeUserId));
        dbContext.Obligations.Add(new ObligationEntity
        {
            Id = ids.ObligationId,
            Source = "FAR",
            Title = "Submit evidence",
            PlainEnglishSummary = "Submit evidence.",
            TriggerCondition = "Request issued.",
            RequiredAction = "Submit evidence.",
            OwnerFunction = "Compliance",
            RiskLevel = RiskLevel.Medium,
            SourceName = "FAR",
            SourceUrl = "https://example.test",
            SourceLastReviewedAt = new DateOnly(2026, 6, 17),
            LastReviewedAt = new DateOnly(2026, 6, 17),
            Confidence = "high",
            SourceConfidence = "high",
            ReviewState = ReviewState.Approved
        });
        dbContext.EvidenceItems.AddRange(
            Evidence(ids.TenantId, ids.EvidenceItemId, "Submitted policy"),
            Evidence(ids.TenantId, ids.SecondEvidenceItemId, "Returned policy"),
            Evidence(ids.OtherTenantId, ids.OtherTenantEvidenceItemId, "Other tenant policy"));
    }

    private static EvidenceItemEntity Evidence(Guid tenantId, Guid evidenceItemId, string name) =>
        new()
        {
            Id = evidenceItemId,
            TenantId = tenantId,
            Name = name,
            Description = name,
            Type = EvidenceType.Policy,
            OwnerFunction = "Compliance",
            Status = EvidenceStatus.Uploaded,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static TenantEntity CreateTenant(Guid tenantId) =>
        new()
        {
            Id = tenantId,
            Name = $"Evidence Submission Tenant {tenantId:N}",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static UserEntity CreateUser(Guid tenantId, Guid userId) =>
        new()
        {
            Id = userId,
            TenantId = tenantId,
            Email = $"{userId:N}@example.test",
            DisplayName = "Evidence User",
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private sealed record StoryIds(
        Guid TenantId,
        Guid OtherTenantId,
        Guid RequesterUserId,
        Guid AssigneeUserId,
        Guid EvidenceItemId,
        Guid SecondEvidenceItemId,
        Guid OtherTenantEvidenceItemId,
        string ObligationId)
    {
        public static StoryIds ForCase(string caseName)
        {
            var suffix = Math.Abs(caseName.GetHashCode(StringComparison.Ordinal)) % 10000;
            return new StoryIds(
                Guid.Parse($"26226226-2262-2622-6226-22622631{suffix:D4}"),
                Guid.Parse($"26226226-2262-2622-6226-22622632{suffix:D4}"),
                Guid.Parse($"26226226-2262-2622-6226-22622633{suffix:D4}"),
                Guid.Parse($"26226226-2262-2622-6226-22622634{suffix:D4}"),
                Guid.Parse($"26226226-2262-2622-6226-22622635{suffix:D4}"),
                Guid.Parse($"26226226-2262-2622-6226-22622636{suffix:D4}"),
                Guid.Parse($"26226226-2262-2622-6226-22622637{suffix:D4}"),
                $"obligation-26-2-{suffix:D4}");
        }
    }
}
