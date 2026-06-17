using Gccs.Application.Compliance;
using Gccs.Application.Security;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Compliance;

public sealed class EfExpertReviewQueueRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : IExpertReviewQueueRepository
{
    public async Task<ExpertReviewItemDto> CreateEscalationAsync(
        EscalateExpertReviewRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new ExpertReviewItemEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SourceType = request.SourceType,
            SourceId = request.SourceId,
            Reason = request.Reason,
            Priority = request.Priority,
            Topic = request.Topic,
            AssignedExpertUserId = request.AssignedExpertUserId,
            DueAt = request.DueAt,
            Status = "open",
            CreatedByUserId = actorUserId,
            CreatedAt = now
        };

        await MarkSourceEscalatedAsync(request.SourceType, request.SourceId, tenantId, request.Reason, actorUserId, now, cancellationToken);
        dbContext.ExpertReviewItems.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<IReadOnlyList<ExpertReviewItemDto>> ListAsync(
        ExpertReviewQueueQuery query,
        CancellationToken cancellationToken = default)
    {
        var items = dbContext.ExpertReviewItems
            .AsNoTracking()
            .Where(item => item.TenantId == tenantContext.TenantId);

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim();
            items = items.Where(item => item.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.SourceType))
        {
            var sourceType = query.SourceType.Trim();
            items = items.Where(item => item.SourceType == sourceType);
        }

        if (query.AssignedExpertUserId is { } assignedExpertUserId)
        {
            items = items.Where(item => item.AssignedExpertUserId == assignedExpertUserId);
        }

        if (!string.IsNullOrWhiteSpace(query.Priority))
        {
            var priority = query.Priority.Trim();
            items = items.Where(item => item.Priority == priority);
        }

        return await items
            .OrderBy(item => item.DueAt ?? DateOnly.MaxValue)
            .ThenByDescending(item => item.CreatedAt)
            .Select(item => ToDto(item))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<ExpertReviewItemDto?> ResolveAsync(
        Guid itemId,
        ResolveExpertReviewRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var item = await dbContext.ExpertReviewItems.FirstOrDefaultAsync(
            candidate => candidate.Id == itemId && candidate.TenantId == tenantContext.TenantId,
            cancellationToken);
        if (item is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        item.Status = "resolved";
        item.ResolvedByUserId = actorUserId;
        item.ResolvedAt = now;
        item.ResolutionDecision = request.Decision;
        item.ResolutionNotes = request.Notes;
        await MarkSourceResolvedAsync(item.SourceType, item.SourceId, item.TenantId, request.Decision, actorUserId, now, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(item);
    }

    private async Task MarkSourceEscalatedAsync(
        string sourceType,
        Guid sourceId,
        Guid tenantId,
        string reason,
        Guid actorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (sourceType == "suggested_obligation")
        {
            var suggestion = await dbContext.SuggestedObligations.FirstOrDefaultAsync(
                item => item.Id == sourceId && item.TenantId == tenantId,
                cancellationToken);
            if (suggestion is not null)
            {
                suggestion.ReviewStatus = "escalated";
                suggestion.ReviewReason = reason;
                suggestion.ReviewedByUserId = actorUserId;
                suggestion.ReviewedAt = now;
            }
        }
        else if (sourceType == "clause_candidate")
        {
            var candidate = await dbContext.Set<ClauseCandidateEntity>().FirstOrDefaultAsync(
                item => item.Id == sourceId && item.TenantId == tenantId,
                cancellationToken);
            if (candidate is not null)
            {
                candidate.ReviewStatus = "needs_clarification";
                candidate.DecisionReason = reason;
                candidate.ReviewedByUserId = actorUserId;
                candidate.ReviewedAt = now;
            }
        }
    }

    private async Task MarkSourceResolvedAsync(
        string sourceType,
        Guid sourceId,
        Guid tenantId,
        string decision,
        Guid actorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (sourceType == "suggested_obligation")
        {
            var suggestion = await dbContext.SuggestedObligations.FirstOrDefaultAsync(
                item => item.Id == sourceId && item.TenantId == tenantId,
                cancellationToken);
            if (suggestion is not null)
            {
                suggestion.ReviewStatus = "draft";
                suggestion.ReviewReason = decision;
                suggestion.ReviewedByUserId = actorUserId;
                suggestion.ReviewedAt = now;
            }
        }
    }

    private static ExpertReviewItemDto ToDto(ExpertReviewItemEntity entity) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.SourceType,
            entity.SourceId,
            entity.Reason,
            entity.Priority,
            entity.Topic,
            entity.AssignedExpertUserId,
            entity.DueAt,
            entity.Status,
            entity.CreatedByUserId,
            entity.CreatedAt,
            entity.ResolvedByUserId,
            entity.ResolvedAt,
            entity.ResolutionDecision,
            entity.ResolutionNotes);
}
