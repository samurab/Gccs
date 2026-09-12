using System.Text.Json;
using Gccs.Application.Ai;
using Gccs.Application.Security;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

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
            answer.ReviewNotes);
}
