using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Ai;
using Gccs.Application.Audit;
using Gccs.Application.Compliance;
using Gccs.Application.Notifications;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Ai;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Compliance;
using Gccs.Infrastructure.Notifications;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class GuardedAssistantEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly WebApplicationFactory<Program> _factory;

    public GuardedAssistantEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Supported_answer_is_tenant_scoped_persisted_and_audited()
    {
        var ids = TestIds.Create();
        await using var factory = CreateFactory(nameof(Supported_answer_is_tenant_scoped_persisted_and_audited), ids);
        using var client = factory.CreateClient();

        var response = await client.SendAsync(Request(HttpMethod.Post, "/api/assistant/questions",
            new AssistantQuestionApiRequest("Explain FCI safeguarding.", "obligation"), ids.TenantId, ids.UserId,
            Permission.ViewObligations));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var answer = await response.Content.ReadFromJsonAsync<GuardedAssistantAnswerDto>(JsonOptions);
        Assert.NotNull(answer);
        Assert.Equal(ids.TenantId, answer.TenantId);
        Assert.Equal("SourceSupported", answer.SupportStatus);
        Assert.NotEmpty(answer.Citations);
        var actionResponse = await client.SendAsync(Request(HttpMethod.Post, $"/api/assistant/answers/{answer.Id}/actions",
            new AssistantDraftActionApiRequest(AssistantDraftActionType.Task, "Review FCI controls", "Review the cited requirements."),
            ids.TenantId, ids.UserId, Permission.ManageTasks));
        Assert.Equal(HttpStatusCode.Created, actionResponse.StatusCode);
        var action = await actionResponse.Content.ReadFromJsonAsync<AssistantDraftActionDto>(JsonOptions);
        Assert.Equal("Draft", action?.Status);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.True(await db.AssistantAnswers.AnyAsync(item => item.Id == answer.Id && item.TenantId == ids.TenantId));
        Assert.True(await db.AssistantDraftActions.AnyAsync(item => item.Id == action!.Id && item.TenantId == ids.TenantId));
        Assert.True(await db.AuditLogEntries.AnyAsync(item => item.TenantId == ids.TenantId && item.EntityType == "AiRetrieval"));
        Assert.True(await db.AuditLogEntries.AnyAsync(item => item.TenantId == ids.TenantId && item.EntityType == "AssistantDraftAction"));
    }

    [Fact]
    public async Task Blocked_prompt_is_sanitized_and_audited_without_prompt_text()
    {
        var ids = TestIds.Create();
        await using var factory = CreateFactory(nameof(Blocked_prompt_is_sanitized_and_audited_without_prompt_text), ids);
        using var client = factory.CreateClient();
        const string prohibitedPrompt = "Analyze this TOP SECRET document payload marker-98765.";

        var response = await client.SendAsync(Request(HttpMethod.Post, "/api/assistant/questions",
            new AssistantQuestionApiRequest(prohibitedPrompt, "contract"), ids.TenantId, ids.UserId,
            Permission.ViewContracts));

        var answer = await response.Content.ReadFromJsonAsync<GuardedAssistantAnswerDto>(JsonOptions);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Blocked", answer?.Status);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var audit = await db.AuditLogEntries.SingleAsync(item => item.EntityType == "GuardedAssistant");
        Assert.Equal(AuditAction.Rejected, audit.Action);
        Assert.DoesNotContain("marker-98765", audit.MetadataJson, StringComparison.Ordinal);
        Assert.DoesNotContain("marker-98765", audit.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Context_and_action_permissions_are_enforced_server_side()
    {
        var ids = TestIds.Create();
        await using var factory = CreateFactory(nameof(Context_and_action_permissions_are_enforced_server_side), ids);
        using var client = factory.CreateClient();

        var wrongContextPermission = await client.SendAsync(Request(HttpMethod.Post, "/api/assistant/questions",
            new AssistantQuestionApiRequest("Explain this contract.", "contract"), ids.TenantId, ids.UserId,
            Permission.ViewObligations));
        Assert.Equal(HttpStatusCode.Forbidden, wrongContextPermission.StatusCode);

        var answerResponse = await client.SendAsync(Request(HttpMethod.Post, "/api/assistant/questions",
            new AssistantQuestionApiRequest("Explain FCI safeguarding.", "obligation"), ids.TenantId, ids.UserId,
            Permission.ViewObligations));
        var answer = await answerResponse.Content.ReadFromJsonAsync<GuardedAssistantAnswerDto>(JsonOptions);
        var deniedAction = await client.SendAsync(Request(HttpMethod.Post, $"/api/assistant/answers/{answer!.Id}/actions",
            new AssistantDraftActionApiRequest(AssistantDraftActionType.EvidenceRequest, "Collect evidence", "Draft request."),
            ids.TenantId, ids.UserId, Permission.ManageTasks));
        Assert.Equal(HttpStatusCode.Forbidden, deniedAction.StatusCode);
    }

    [Fact]
    public async Task Cross_tenant_answer_reference_returns_not_found_and_does_not_mutate()
    {
        var ids = TestIds.Create();
        await using var factory = CreateFactory(nameof(Cross_tenant_answer_reference_returns_not_found_and_does_not_mutate), ids);
        using var client = factory.CreateClient();
        var answerResponse = await client.SendAsync(Request(HttpMethod.Post, "/api/assistant/questions",
            new AssistantQuestionApiRequest("Explain FCI safeguarding.", "obligation"), ids.TenantId, ids.UserId,
            Permission.ViewObligations));
        var answer = await answerResponse.Content.ReadFromJsonAsync<GuardedAssistantAnswerDto>(JsonOptions);

        var response = await client.SendAsync(Request(HttpMethod.Post, $"/api/assistant/answers/{answer!.Id}/actions",
            new AssistantDraftActionApiRequest(AssistantDraftActionType.Task, "Review", "Review answer."),
            ids.OtherTenantId, ids.UserId, Permission.ManageTasks));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Empty(await db.AssistantDraftActions.ToArrayAsync());
    }

    [Fact]
    public async Task Feedback_stores_answer_user_tenant_timestamp_reason_and_audit()
    {
        var ids = TestIds.Create();
        await using var factory = CreateFactory(nameof(Feedback_stores_answer_user_tenant_timestamp_reason_and_audit), ids);
        using var client = factory.CreateClient();
        var answerResponse = await client.SendAsync(Request(HttpMethod.Post, "/api/assistant/questions",
            new AssistantQuestionApiRequest("Explain FCI safeguarding.", "obligation"), ids.TenantId, ids.UserId,
            Permission.ViewObligations));
        var answer = await answerResponse.Content.ReadFromJsonAsync<GuardedAssistantAnswerDto>(JsonOptions);

        var response = await client.SendAsync(Request(HttpMethod.Post, $"/api/assistant/answers/{answer!.Id}/feedback",
            new AssistantFeedbackApiRequest(AssistantFeedbackType.MissingSource, "Add a current agency source."),
            ids.TenantId, ids.UserId, Permission.ViewObligations));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var feedback = await db.AssistantFeedback.SingleAsync();
        Assert.Equal(ids.TenantId, feedback.TenantId);
        Assert.Equal(ids.UserId, feedback.ActorUserId);
        Assert.Equal(answer.Id, feedback.AnswerId);
        Assert.Equal("Add a current agency source.", feedback.Reason);
        Assert.NotEqual(default, feedback.CreatedAt);
        Assert.True(await db.AuditLogEntries.AnyAsync(item => item.EntityType == "AssistantFeedback" && item.EntityId == feedback.Id.ToString()));
    }

    [Fact]
    public async Task Expert_review_route_is_durable_idempotent_tenant_scoped_and_resolvable()
    {
        var ids = TestIds.Create();
        await using var factory = CreateFactory(nameof(Expert_review_route_is_durable_idempotent_tenant_scoped_and_resolvable), ids);
        using var client = factory.CreateClient();
        var answerResponse = await client.SendAsync(Request(HttpMethod.Post, "/api/assistant/questions",
            new AssistantQuestionApiRequest("Explain FCI safeguarding.", "obligation"), ids.TenantId, ids.UserId,
            Permission.ViewObligations));
        var answer = await answerResponse.Content.ReadFromJsonAsync<GuardedAssistantAnswerDto>(JsonOptions);
        var path = $"/api/assistant/answers/{answer!.Id}/expert-review";

        var denied = await client.SendAsync(Request(HttpMethod.Post, path,
            new AssistantExpertReviewApiRequest("Confirm the interpretation."), ids.TenantId, ids.UserId,
            Permission.ViewObligations));
        var crossTenant = await client.SendAsync(Request(HttpMethod.Post, path,
            new AssistantExpertReviewApiRequest("Confirm the interpretation."), ids.OtherTenantId, ids.UserId,
            Permission.ManageObligations));
        var created = await client.SendAsync(Request(HttpMethod.Post, path,
            new AssistantExpertReviewApiRequest("Confirm the interpretation."), ids.TenantId, ids.UserId,
            Permission.ManageObligations));
        var repeated = await client.SendAsync(Request(HttpMethod.Post, path,
            new AssistantExpertReviewApiRequest("Confirm the interpretation."), ids.TenantId, ids.UserId,
            Permission.ManageObligations));
        var escalation = await created.Content.ReadFromJsonAsync<AssistantExpertReviewEscalationDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, crossTenant.StatusCode);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        Assert.True(escalation?.Created);
        Assert.Equal("assistant_answer", escalation?.ReviewItem.SourceType);

        var reviewAnswer = await client.SendAsync(Request<object>(HttpMethod.Get, $"/api/assistant/answers/{answer.Id}",
            null!, ids.TenantId, ids.UserId, Permission.ViewObligations));
        var crossTenantAnswer = await client.SendAsync(Request<object>(HttpMethod.Get, $"/api/assistant/answers/{answer.Id}",
            null!, ids.OtherTenantId, ids.UserId, Permission.ViewObligations));
        Assert.Equal(HttpStatusCode.OK, reviewAnswer.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, crossTenantAnswer.StatusCode);
        var queueResponse = await client.SendAsync(Request<object>(HttpMethod.Get, "/api/assistant/expert-review-items",
            null!, ids.TenantId, ids.UserId, Permission.ViewObligations));
        var queue = await queueResponse.Content.ReadFromJsonAsync<AssistantExpertReviewQueueItemDto[]>(JsonOptions);
        Assert.Equal(HttpStatusCode.OK, queueResponse.StatusCode);
        Assert.Equal(answer.Id, Assert.Single(queue!).Answer?.Id);

        var dueAt = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        var invalidAssignee = await client.SendAsync(Request(HttpMethod.Post, $"/api/expert-review-items/{escalation!.ReviewItem.Id}/assign",
            new AssignExpertReviewRequest(Guid.NewGuid(), dueAt), ids.TenantId, ids.UserId,
            Permission.ManageObligations));
        var assign = await client.SendAsync(Request(HttpMethod.Post, $"/api/expert-review-items/{escalation!.ReviewItem.Id}/assign",
            new AssignExpertReviewRequest(ids.ExpertUserId, dueAt), ids.TenantId, ids.UserId,
            Permission.ManageObligations));
        Assert.Equal(HttpStatusCode.BadRequest, invalidAssignee.StatusCode);
        Assert.Equal(HttpStatusCode.OK, assign.StatusCode);

        var resolutionPath = $"/api/expert-review-items/{escalation.ReviewItem.Id}/resolve";
        var crossTenantResolve = await client.SendAsync(Request(HttpMethod.Post, resolutionPath,
            new ResolveExpertReviewRequest("rejected", "Should not cross tenants."), ids.OtherTenantId, ids.UserId,
            Permission.ManageObligations));
        var resolve = await client.SendAsync(Request(HttpMethod.Post, resolutionPath,
            new ResolveExpertReviewRequest("revision_required", "Clarify the limits before use."), ids.TenantId, ids.UserId,
            Permission.ManageObligations));
        var repeatedResolve = await client.SendAsync(Request(HttpMethod.Post, resolutionPath,
            new ResolveExpertReviewRequest("rejected", "Should not overwrite the first resolution."), ids.TenantId, ids.UserId,
            Permission.ManageObligations));
        Assert.Equal(HttpStatusCode.NotFound, crossTenantResolve.StatusCode);
        Assert.Equal(HttpStatusCode.OK, resolve.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, repeatedResolve.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var item = await db.ExpertReviewItems.SingleAsync();
        Assert.Equal("resolved", item.Status);
        Assert.Equal("revision_required", item.ResolutionDecision);
        Assert.Equal(ids.ExpertUserId, item.AssignedExpertUserId);
        Assert.Equal(dueAt, item.DueAt);
        Assert.Equal(answer.Id, item.SourceId);
        Assert.Single(await db.AssistantFeedback.Where(value => value.FeedbackType == AssistantFeedbackType.NeedsExpertReview).ToArrayAsync());
        var reviewedAnswer = await db.AssistantAnswers.SingleAsync(value => value.Id == answer.Id);
        Assert.Equal("revision_required", reviewedAnswer.HumanReviewStatus);
        Assert.Equal(ids.UserId, reviewedAnswer.ReviewedByUserId);
        Assert.Equal("Clarify the limits before use.", reviewedAnswer.ReviewNotes);
        Assert.Single(await db.NotificationDeliveries.Where(value => value.SourceTaskId == item.Id && value.UserId == ids.ExpertUserId).ToArrayAsync());
        Assert.Single(await db.AssignmentEmailDeliveries.Where(value => value.UserId == ids.ExpertUserId).ToArrayAsync());
        Assert.Equal(3, await db.AuditLogEntries.CountAsync(value => value.EntityType == "ExpertReviewItem"));
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, TestIds ids) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IAiRetrievalSourceRepository>();
                services.RemoveAll<IGuardedAssistantRepository>();
                services.RemoveAll<IExpertReviewQueueRepository>();
                services.RemoveAll<IAssignmentNotificationRepository>();
                services.RemoveAll<IAuditEventWriter>();
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddSingleton<IAiRetrievalSourceRepository>(new FixedRetrievalSourceRepository());
                services.AddScoped<IGuardedAssistantRepository, EfGuardedAssistantRepository>();
                services.AddScoped<IExpertReviewQueueRepository, EfExpertReviewQueueRepository>();
                services.AddScoped<IAssignmentNotificationRepository, EfAssignmentNotificationRepository>();
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();
                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                db.Tenants.AddRange(Tenant(ids.TenantId, "Alpha"), Tenant(ids.OtherTenantId, "Bravo"));
                db.Users.Add(new UserEntity
                {
                    Id = ids.ExpertUserId, TenantId = ids.TenantId, Email = "expert@example.test", DisplayName = "Expert Reviewer",
                    Status = UserStatus.Active, CreatedAt = DateTimeOffset.UtcNow
                });
                db.TenantMemberships.Add(new TenantMembershipEntity
                {
                    Id = Guid.NewGuid(), TenantId = ids.TenantId, UserId = ids.ExpertUserId,
                    RoleName = RoleCatalog.ComplianceManager, Status = MembershipStatus.Active, CreatedAt = DateTimeOffset.UtcNow
                });
                db.SaveChanges();
            });
        });

    private static HttpRequestMessage Request<T>(HttpMethod method, string path, T body, Guid tenantId, Guid userId, params Permission[] permissions)
    {
        var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body, options: JsonOptions) };
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", string.Join(',', permissions));
        return request;
    }

    private static TenantEntity Tenant(Guid id, string name) => new()
    {
        Id = id, Name = name, Status = TenantStatus.Active, DataPosture = TenantDataPosture.NoCui,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private sealed class FixedRetrievalSourceRepository : IAiRetrievalSourceRepository
    {
        public Task<IReadOnlyList<AiRetrievalSourceDto>> ListSourcesAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AiRetrievalSourceDto>>([
                new("library-fci", null, "FAR 52.204-21", "ComplianceLibrary", "https://acquisition.gov/far/52.204-21",
                    null, "section", "2026.1", new DateOnly(2026, 9, 1), ContentClassification.Fci, true, true,
                    "FCI safeguarding requires basic controls.", ["fci", "safeguarding"])
            ]);
    }

    private sealed record TestIds(Guid TenantId, Guid OtherTenantId, Guid UserId, Guid ExpertUserId)
    {
        public static TestIds Create() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
    }
}
