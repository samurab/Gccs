using System.Text.Json;
using Gccs.Application.Ai;
using Gccs.Application.Security;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gccs.Infrastructure.Ai;

public sealed class EfGuardedAssistantRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : IGuardedAssistantRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task SaveAnswerAsync(
        GuardedAssistantAnswerDto answer,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        EnsureCurrentTenant(answer.TenantId);
        dbContext.AssistantAnswers.Add(new AssistantAnswerEntity
        {
            Id = answer.Id,
            TenantId = tenantContext.TenantId,
            ActorUserId = actorUserId,
            Prompt = answer.Prompt,
            PromptWasRedacted = answer.PromptWasRedacted,
            PromptMetadataJson = answer.PromptMetadata,
            ModelConfigurationJson = answer.ModelConfiguration,
            RetrievalPolicyJson = answer.RetrievalPolicy,
            WorkflowContext = answer.WorkflowContext,
            Status = answer.Status,
            Answer = answer.Answer,
            CitationsJson = JsonSerializer.Serialize(answer.Citations, JsonOptions),
            SupportStatus = answer.SupportStatus,
            DraftLabel = answer.DraftLabel,
            RequiresReview = answer.RequiresReview,
            EscalationRecommended = answer.EscalationRecommended,
            BlockedReason = answer.BlockedReason,
            HumanReviewStatus = answer.HumanReviewStatus,
            ReviewedByUserId = answer.ReviewedByUserId,
            ReviewedAt = answer.ReviewedAt,
            ReviewDecision = answer.ReviewDecision,
            ReviewNotes = answer.ReviewNotes,
            RejectionReason = answer.RejectionReason,
            ReviewState = answer.ReviewState,
            Classification = answer.Classification,
            Result = answer.Result,
            RetainUntil = answer.RetainUntil,
            Version = answer.Version,
            CreatedAt = answer.CreatedAt
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<GuardedAssistantAnswerDto?> FindAnswerAsync(
        Guid answerId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        EnsureCurrentTenant(tenantId);
        var answer = await dbContext.AssistantAnswers.AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == answerId && candidate.TenantId == tenantContext.TenantId,
                cancellationToken);
        return answer is null ? null : ToDto(answer);
    }

    public async Task<IReadOnlyList<GuardedAssistantAnswerDto>> FindAnswersAsync(
        IReadOnlyCollection<Guid> answerIds,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        EnsureCurrentTenant(tenantId);
        if (answerIds.Count == 0) return [];
        var answers = await dbContext.AssistantAnswers.AsNoTracking()
            .Where(candidate => candidate.TenantId == tenantContext.TenantId && answerIds.Contains(candidate.Id))
            .ToArrayAsync(cancellationToken);
        return answers.Select(ToDto).ToArray();
    }

    public async Task<AssistantDraftActionDto> CreateDraftActionAsync(
        AssistantDraftActionRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        EnsureCurrentTenant(tenantId);
        var entity = new AssistantDraftActionEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            AnswerId = request.AnswerId,
            ActionType = request.ActionType,
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            Status = "Draft",
            CreatedByUserId = actorUserId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.AssistantDraftActions.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(entity.Id, entity.TenantId, entity.AnswerId, entity.ActionType, entity.Title, entity.Body,
            entity.Status, entity.CreatedByUserId, entity.CreatedAt);
    }

    public async Task<AssistantFeedbackDto> SubmitFeedbackAsync(
        AssistantFeedbackRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        EnsureCurrentTenant(tenantId);
        var entity = new AssistantFeedbackEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            AnswerId = request.AnswerId,
            ActorUserId = actorUserId,
            FeedbackType = request.FeedbackType,
            Reason = request.Reason.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.AssistantFeedback.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(entity.Id, entity.TenantId, entity.AnswerId, entity.ActorUserId, entity.FeedbackType,
            entity.Reason, entity.CreatedAt);
    }

    public async Task<IReadOnlyList<GuardedAssistantAnswerDto>> ListAnswersAsync(
        Guid tenantId,
        bool includeArchived,
        IReadOnlyCollection<string>? allowedWorkflowContexts = null,
        CancellationToken cancellationToken = default)
    {
        EnsureCurrentTenant(tenantId);
        var query = dbContext.AssistantAnswers.AsNoTracking()
            .Where(x => x.TenantId == tenantContext.TenantId);
        if (!includeArchived) query = query.Where(x => x.ReviewState != AiOutputReviewState.Archived);
        if (allowedWorkflowContexts is not null)
        {
            var contexts = allowedWorkflowContexts
                .Select(value => value.Trim().ToLowerInvariant())
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            query = contexts.Length == 0
                ? query.Where(_ => false)
                : query.Where(x => contexts.Contains(x.WorkflowContext));
        }
        return (await query.OrderByDescending(x => x.CreatedAt).Take(500).ToArrayAsync(cancellationToken))
            .Select(ToDto).ToArray();
    }

    public async Task<AiOutputReviewResultDto?> ReviewAnswerAsync(
        Guid answerId, Guid tenantId, AiOutputReviewDecisionRequest request, Guid reviewerUserId,
        CancellationToken cancellationToken = default)
    {
        EnsureCurrentTenant(tenantId);
        var answer = await dbContext.AssistantAnswers.SingleOrDefaultAsync(
            x => x.Id == answerId && x.TenantId == tenantContext.TenantId, cancellationToken);
        if (answer is null) return null;
        if (answer.Version != request.ExpectedVersion) throw new AiOutputReviewConflictException();
        var previous = answer.ReviewState;
        answer.ReviewState = request.State;
        answer.HumanReviewStatus = request.State.ToString();
        answer.ReviewDecision = request.State.ToString();
        answer.ReviewNotes = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        answer.RejectionReason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        answer.ReviewedByUserId = reviewerUserId;
        answer.ReviewedAt = DateTimeOffset.UtcNow;
        answer.Version++;
        var history = new AssistantOutputReviewEntity
        {
            Id = Guid.NewGuid(), TenantId = tenantContext.TenantId, AnswerId = answer.Id,
            PreviousState = previous, NewState = request.State, ReviewerUserId = reviewerUserId,
            Note = answer.ReviewNotes, RejectionReason = answer.RejectionReason, CreatedAt = answer.ReviewedAt.Value
        };
        dbContext.AssistantOutputReviews.Add(history);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AiOutputReviewConflictException();
        }
        return new(ToDto(answer), ToHistoryDto(history));
    }

    public async Task<IReadOnlyList<AiOutputReviewHistoryDto>> ListReviewHistoryAsync(
        Guid answerId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        EnsureCurrentTenant(tenantId);
        var exists = await dbContext.AssistantAnswers.AsNoTracking().AnyAsync(
            x => x.Id == answerId && x.TenantId == tenantContext.TenantId, cancellationToken);
        if (!exists) return [];
        return (await dbContext.AssistantOutputReviews.AsNoTracking()
            .Where(x => x.TenantId == tenantContext.TenantId && x.AnswerId == answerId)
            .OrderBy(x => x.CreatedAt).ToArrayAsync(cancellationToken)).Select(ToHistoryDto).ToArray();
    }

    public async Task<AiOutputUsageDto?> LinkDeliverableAsync(
        Guid answerId, Guid tenantId, AiOutputUsageRequest request, Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        EnsureCurrentTenant(tenantId);
        var answer = await dbContext.AssistantAnswers.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == answerId && x.TenantId == tenantContext.TenantId, cancellationToken);
        if (answer is null) return null;
        var existing = await dbContext.AssistantOutputUsages.AsNoTracking().SingleOrDefaultAsync(
            x => x.TenantId == tenantContext.TenantId && x.DeliverableType == request.DeliverableType &&
                x.DeliverableId == request.DeliverableId.Trim(), cancellationToken);
        if (existing is not null)
        {
            if (existing.AnswerId != answerId)
                throw new AiOutputReviewValidationException("deliverableId", "The deliverable is already linked to a different AI output.");
            return new(existing.Id, existing.TenantId, existing.AnswerId, existing.DeliverableType,
                existing.DeliverableId, existing.LinkedByUserId, existing.LinkedAt);
        }
        var entity = new AssistantOutputUsageEntity
        {
            Id = Guid.NewGuid(), TenantId = tenantContext.TenantId, AnswerId = answerId,
            DeliverableType = request.DeliverableType, DeliverableId = request.DeliverableId.Trim(),
            LinkedByUserId = actorUserId, LinkedAt = DateTimeOffset.UtcNow
        };
        dbContext.AssistantOutputUsages.Add(entity);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            dbContext.Entry(entity).State = EntityState.Detached;
            // PostgreSQL marks the current transaction as aborted after a uniqueness
            // violation, so querying the winner here would itself fail. Let the outer
            // transaction roll back and return a retryable conflict instead. A retry
            // is idempotent through the pre-insert lookup above.
            throw new AiOutputReviewConflictException();
        }
        return new(entity.Id, entity.TenantId, entity.AnswerId, entity.DeliverableType,
            entity.DeliverableId, entity.LinkedByUserId, entity.LinkedAt);
    }

    public async Task<bool> DeliverableExistsAsync(Guid tenantId, AiOutputUsageRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCurrentTenant(tenantId);
        if (!Guid.TryParse(request.DeliverableId, out var id)) return false;
        return request.DeliverableType switch
        {
            AiDeliverableType.Report => await dbContext.Reports.AsNoTracking().AnyAsync(x => x.TenantId == tenantId && x.Id == id, cancellationToken),
            AiDeliverableType.Policy => await dbContext.GeneratedPolicies.AsNoTracking().AnyAsync(x => x.TenantId == tenantId && x.Id == id, cancellationToken),
            AiDeliverableType.Ssp => await dbContext.SspNarratives.AsNoTracking().AnyAsync(x => x.TenantId == tenantId && x.Id == id, cancellationToken),
            AiDeliverableType.Poam => await dbContext.PoamItems.AsNoTracking().AnyAsync(x => x.TenantId == tenantId && x.Id == id, cancellationToken),
            AiDeliverableType.CustomerDeliverable =>
                await dbContext.ReportExports.AsNoTracking().AnyAsync(x => x.TenantId == tenantId && x.Id == id, cancellationToken) ||
                await dbContext.SspExportPackages.AsNoTracking().AnyAsync(x => x.TenantId == tenantId && x.Id == id, cancellationToken) ||
                await dbContext.SprReportPackages.AsNoTracking().AnyAsync(x => x.TenantId == tenantId && x.Id == id, cancellationToken) ||
                await dbContext.SharedPortalPackages.AsNoTracking().AnyAsync(x => x.TenantId == tenantId && x.Id == id, cancellationToken),
            _ => false
        };
    }

    private void EnsureCurrentTenant(Guid tenantId)
    {
        if (tenantId != tenantContext.TenantId)
            throw new InvalidOperationException("Assistant repository tenant scope does not match the current tenant.");
    }

    private static GuardedAssistantAnswerDto ToDto(AssistantAnswerEntity answer) =>
        new(
            answer.Id,
            answer.TenantId,
            answer.WorkflowContext,
            answer.Status,
            answer.Answer,
            JsonSerializer.Deserialize<AiCitationDto[]>(answer.CitationsJson, JsonOptions) ?? [],
            answer.SupportStatus,
            answer.DraftLabel,
            answer.RequiresReview,
            answer.EscalationRecommended,
            answer.BlockedReason,
            answer.CreatedAt,
            answer.HumanReviewStatus,
            answer.ReviewedByUserId,
            answer.ReviewedAt,
            answer.ReviewDecision,
            answer.ReviewNotes,
            answer.Prompt,
            answer.PromptWasRedacted,
            answer.PromptMetadataJson,
            answer.ModelConfigurationJson,
            answer.RetrievalPolicyJson,
            answer.Classification,
            answer.Result,
            answer.ReviewState,
            answer.RejectionReason,
            answer.RetainUntil,
            answer.Version,
            answer.ActorUserId);

    private static AiOutputReviewHistoryDto ToHistoryDto(AssistantOutputReviewEntity value) =>
        new(value.Id, value.TenantId, value.AnswerId, value.PreviousState, value.NewState,
            value.ReviewerUserId, value.Note, value.RejectionReason, value.CreatedAt);
}
