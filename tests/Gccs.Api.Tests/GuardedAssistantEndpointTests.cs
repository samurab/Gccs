using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Ai;
using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Compliance;
using Gccs.Application.Notifications;
using Gccs.Application.Reports;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Domain.Evidence;
using Gccs.Domain.Identity;
using Gccs.Domain.Reports;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Ai;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Compliance;
using Gccs.Infrastructure.Notifications;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Reports;
using Gccs.Infrastructure.Tenancy;
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
    public async Task Source_family_permissions_are_derived_from_server_claims()
    {
        var ids = TestIds.Create();
        await using var factory = CreateFactory(nameof(Source_family_permissions_are_derived_from_server_claims), ids);
        using var client = factory.CreateClient();

        var allowedResponse = await client.SendAsync(Request(HttpMethod.Post, "/api/assistant/questions",
            new AssistantQuestionApiRequest("Explain tenant-only evidence marker.", "evidence"), ids.TenantId, ids.UserId,
            Permission.ViewEvidence));
        var allowed = await allowedResponse.Content.ReadFromJsonAsync<GuardedAssistantAnswerDto>(JsonOptions);
        Assert.Equal(HttpStatusCode.OK, allowedResponse.StatusCode);
        Assert.Contains(allowed!.Citations, citation => citation.SourceType == "EvidenceMetadata");

        var restrictedResponse = await client.SendAsync(Request(HttpMethod.Post, "/api/assistant/questions",
            new AssistantQuestionApiRequest("Explain tenant-only evidence marker.", "obligation"), ids.TenantId, ids.UserId,
            Permission.ViewObligations));
        var restricted = await restrictedResponse.Content.ReadFromJsonAsync<GuardedAssistantAnswerDto>(JsonOptions);
        Assert.Equal(HttpStatusCode.OK, restrictedResponse.StatusCode);
        Assert.Equal("NeedsReview", restricted!.Status);
        Assert.DoesNotContain(restricted.Citations, citation => citation.SourceType == "EvidenceMetadata");
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

    [Fact]
    public async Task Ai_output_review_endpoints_enforce_rbac_tenant_scope_history_export_and_deliverable_gate()
    {
        var ids = TestIds.Create();
        await using var factory = CreateFactory(nameof(Ai_output_review_endpoints_enforce_rbac_tenant_scope_history_export_and_deliverable_gate), ids);
        using var client = factory.CreateClient();
        var ask = await client.SendAsync(Request(HttpMethod.Post, "/api/assistant/questions",
            new AssistantQuestionApiRequest("Explain FCI safeguarding.", "obligation"), ids.TenantId, ids.UserId,
            Permission.ViewObligations));
        var answer = await ask.Content.ReadFromJsonAsync<GuardedAssistantAnswerDto>(JsonOptions);
        var reportId = Guid.NewGuid();
        var otherTenantReportId = Guid.NewGuid();
        using (var seedScope = factory.Services.CreateScope())
        {
            var seedDb = seedScope.ServiceProvider.GetRequiredService<GccsDbContext>();
            seedDb.Reports.AddRange(
                new ReportEntity
                {
                    Id = reportId, TenantId = ids.TenantId, Type = ReportType.ComplianceStatus,
                    Title = "AI provenance target", Status = ReportStatus.Complete, GeneratedAt = DateTimeOffset.UtcNow,
                    GeneratedByUserId = ids.UserId, SnapshotJson = "{}", ExportHtml = ""
                },
                new ReportEntity
                {
                    Id = otherTenantReportId, TenantId = ids.OtherTenantId, Type = ReportType.ComplianceStatus,
                    Title = "Other tenant target", Status = ReportStatus.Complete, GeneratedAt = DateTimeOffset.UtcNow,
                    GeneratedByUserId = ids.UserId, SnapshotJson = "{}", ExportHtml = ""
                });
            await seedDb.SaveChangesAsync();
        }

        var useDraft = await client.SendAsync(Request(HttpMethod.Post, $"/api/assistant/outputs/{answer!.Id}/deliverable-uses",
            new AiOutputUsageRequest(AiDeliverableType.Report, reportId.ToString()), ids.TenantId, ids.UserId,
            Permission.ManageReports, Permission.ViewObligations));
        var unrelatedWorkflowList = await client.SendAsync(Request<object>(HttpMethod.Get, "/api/assistant/outputs",
            null!, ids.TenantId, ids.UserId, Permission.ViewEvidence));
        var unrelatedWorkflowOutputs = await unrelatedWorkflowList.Content.ReadFromJsonAsync<GuardedAssistantAnswerDto[]>(JsonOptions);
        var unrelatedWorkflowHistory = await client.SendAsync(Request<object>(HttpMethod.Get,
            $"/api/assistant/outputs/{answer.Id}/reviews", null!, ids.TenantId, ids.UserId, Permission.ViewEvidence));
        var deniedReview = await client.SendAsync(Request(HttpMethod.Post, $"/api/assistant/outputs/{answer.Id}/reviews",
            new AiOutputReviewDecisionRequest(AiOutputReviewState.Approved, "Reviewed.", null, 0), ids.TenantId, ids.UserId,
            Permission.ViewObligations));
        var unrelatedWorkflowReview = await client.SendAsync(Request(HttpMethod.Post, $"/api/assistant/outputs/{answer.Id}/reviews",
            new AiOutputReviewDecisionRequest(AiOutputReviewState.Approved, "Reviewed.", null, 0), ids.TenantId, ids.UserId,
            Permission.ManageObligations, Permission.ViewEvidence));
        var crossTenant = await client.SendAsync(Request(HttpMethod.Post, $"/api/assistant/outputs/{answer.Id}/reviews",
            new AiOutputReviewDecisionRequest(AiOutputReviewState.Approved, "Reviewed.", null, 0), ids.OtherTenantId, ids.UserId,
            Permission.ManageObligations));
        var approve = await client.SendAsync(Request(HttpMethod.Post, $"/api/assistant/outputs/{answer.Id}/reviews",
            new AiOutputReviewDecisionRequest(AiOutputReviewState.Approved, "Sources verified by reviewer.", null, 0),
            ids.TenantId, ids.UserId, Permission.ManageObligations, Permission.ViewObligations));
        var staleReview = await client.SendAsync(Request(HttpMethod.Post, $"/api/assistant/outputs/{answer.Id}/reviews",
            new AiOutputReviewDecisionRequest(AiOutputReviewState.Archived, "Stale decision must not win.", null, 0),
            ids.TenantId, ids.UserId, Permission.ManageObligations, Permission.ViewObligations));
        var wrongPermission = await client.SendAsync(Request(HttpMethod.Post, $"/api/assistant/outputs/{answer.Id}/deliverable-uses",
            new AiOutputUsageRequest(AiDeliverableType.Report, reportId.ToString()), ids.TenantId, ids.UserId, Permission.ManageObligations));
        var crossTenantTarget = await client.SendAsync(Request(HttpMethod.Post, $"/api/assistant/outputs/{answer.Id}/deliverable-uses",
            new AiOutputUsageRequest(AiDeliverableType.Report, otherTenantReportId.ToString()), ids.TenantId, ids.UserId,
            Permission.ManageReports, Permission.ViewObligations));
        var unrelatedWorkflowLink = await client.SendAsync(Request(HttpMethod.Post, $"/api/assistant/outputs/{answer.Id}/deliverable-uses",
            new AiOutputUsageRequest(AiDeliverableType.Report, reportId.ToString()), ids.TenantId, ids.UserId,
            Permission.ManageReports, Permission.ViewEvidence));
        var linked = await client.SendAsync(Request(HttpMethod.Post, $"/api/assistant/outputs/{answer.Id}/deliverable-uses",
            new AiOutputUsageRequest(AiDeliverableType.Report, reportId.ToString()), ids.TenantId, ids.UserId,
            Permission.ManageReports, Permission.ViewObligations));
        var generatedWithProvenance = await client.SendAsync(Request(HttpMethod.Post, "/api/reports/compliance-status",
            new ClassifiedWorkflowRequest(new ContentClassificationRequest(ContentClassification.Unclassified), answer.Id),
            ids.TenantId, ids.UserId, Permission.ManageReports));
        var deniedExport = await client.SendAsync(Request<object>(HttpMethod.Get, "/api/assistant/outputs/export",
            null!, ids.TenantId, ids.UserId, Permission.ViewObligations));
        var unrelatedWorkflowExport = await client.SendAsync(Request<object>(HttpMethod.Get, "/api/assistant/outputs/export",
            null!, ids.TenantId, ids.UserId, Permission.ExportReports, Permission.ViewEvidence));
        var unrelatedWorkflowExportBody = await unrelatedWorkflowExport.Content.ReadFromJsonAsync<AiOutputExportDto>(JsonOptions);
        var export = await client.SendAsync(Request<object>(HttpMethod.Get, "/api/assistant/outputs/export",
            null!, ids.TenantId, ids.UserId, Permission.ExportReports, Permission.ViewObligations));
        var exportBody = await export.Content.ReadFromJsonAsync<AiOutputExportDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, useDraft.StatusCode);
        Assert.Empty(unrelatedWorkflowOutputs!);
        Assert.Equal(HttpStatusCode.NotFound, unrelatedWorkflowHistory.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deniedReview.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unrelatedWorkflowReview.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, crossTenant.StatusCode);
        Assert.Equal(HttpStatusCode.OK, approve.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, staleReview.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, wrongPermission.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, crossTenantTarget.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unrelatedWorkflowLink.StatusCode);
        Assert.Equal(HttpStatusCode.Created, linked.StatusCode);
        Assert.Equal(HttpStatusCode.Created, generatedWithProvenance.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deniedExport.StatusCode);
        Assert.Equal(HttpStatusCode.OK, unrelatedWorkflowExport.StatusCode);
        Assert.Equal(0, unrelatedWorkflowExportBody?.LogCount);
        Assert.Equal(HttpStatusCode.OK, export.StatusCode);
        Assert.Equal(1, exportBody?.LogCount);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Single(await db.AssistantOutputReviews.Where(x => x.TenantId == ids.TenantId && x.AnswerId == answer.Id).ToArrayAsync());
        Assert.Equal(2, await db.AssistantOutputUsages.CountAsync(x => x.TenantId == ids.TenantId && x.AnswerId == answer.Id));
        Assert.True(await db.AuditLogEntries.AnyAsync(x => x.EntityType == "AiInteractionLog" && x.EntityId == answer.Id.ToString()));
        Assert.True(await db.AuditLogEntries.AnyAsync(x => x.EntityType == "AiOutputUsage"));
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, TestIds ids) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddAcknowledgedNoticeFixture();
                services.RemoveAll<IAiRetrievalSourceRepository>();
                services.RemoveAll<IGuardedAssistantRepository>();
                services.RemoveAll<IExpertReviewQueueRepository>();
                services.RemoveAll<IAssignmentNotificationRepository>();
                services.RemoveAll<IAuditEventWriter>();
                services.RemoveAll<IReportRepository>();
                services.RemoveAll<ITenantRepository>();
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<IAiRetrievalSourceRepository, EfAiRetrievalSourceRepository>();
                services.AddScoped<IGuardedAssistantRepository, EfGuardedAssistantRepository>();
                services.AddScoped<IExpertReviewQueueRepository, EfExpertReviewQueueRepository>();
                services.AddScoped<IAssignmentNotificationRepository, EfAssignmentNotificationRepository>();
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();
                services.AddScoped<IReportRepository, EfReportRepository>();
                services.AddScoped<ITenantRepository, EfTenantRepository>();
                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                db.Tenants.AddRange(Tenant(ids.TenantId, "Alpha"), Tenant(ids.OtherTenantId, "Bravo"));
                db.Obligations.Add(new ObligationEntity
                {
                    Id = $"library-fci-{databaseName}",
                    Source = "FAR 52.204-21",
                    Title = "Basic Safeguarding of Covered Contractor Information Systems",
                    PlainEnglishSummary = "FCI safeguarding requires basic controls.",
                    RequiredAction = "Apply the source-backed safeguards to covered information systems.",
                    SourceName = "Acquisition.gov",
                    SourceUrl = "https://www.acquisition.gov/far/52.204-21",
                    SourceLastReviewedAt = new DateOnly(2026, 9, 1),
                    LastReviewedAt = new DateOnly(2026, 9, 1),
                    ReviewState = ReviewState.Published
                });
                db.EvidenceItems.Add(new EvidenceItemEntity
                {
                    Id = Guid.NewGuid(),
                    TenantId = ids.TenantId,
                    Name = "Tenant-only evidence marker",
                    Description = "Approved evidence metadata for source-family authorization tests.",
                    Type = EvidenceType.Policy,
                    OwnerFunction = "Security",
                    Status = EvidenceStatus.Approved,
                    ApprovedByUserId = ids.UserId,
                    ApprovedAt = DateTimeOffset.UtcNow,
                    Classification = ContentClassification.Unclassified,
                    CreatedAt = DateTimeOffset.UtcNow
                });
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

    private sealed record TestIds(Guid TenantId, Guid OtherTenantId, Guid UserId, Guid ExpertUserId)
    {
        public static TestIds Create() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
    }
}
