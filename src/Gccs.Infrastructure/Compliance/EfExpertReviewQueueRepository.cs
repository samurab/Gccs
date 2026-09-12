using Gccs.Application.Compliance;
using Gccs.Application.Security;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gccs.Infrastructure.Compliance;

public sealed class EfExpertReviewQueueRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : IExpertReviewQueueRepository
{
    public Task<bool> SourceExistsAsync(
        string sourceType,
        Guid sourceId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        EnsureCurrentTenant(tenantId);
        return sourceType switch
        {
            "suggested_obligation" => dbContext.SuggestedObligations.AsNoTracking()
                .AnyAsync(item => item.Id == sourceId && item.TenantId == tenantId, cancellationToken),
            "clause_candidate" => dbContext.Set<ClauseCandidateEntity>().AsNoTracking()
                .AnyAsync(item => item.Id == sourceId && item.TenantId == tenantId, cancellationToken),
            "assistant_answer" => dbContext.AssistantAnswers.AsNoTracking()
                .AnyAsync(item => item.Id == sourceId && item.TenantId == tenantId, cancellationToken),
            _ => Task.FromResult(false)
        };
    }

    public async Task<ExpertReviewItemDto?> FindOpenAsync(
        string sourceType,
        Guid sourceId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        EnsureCurrentTenant(tenantId);
        var item = await dbContext.ExpertReviewItems.AsNoTracking().SingleOrDefaultAsync(
            candidate => candidate.TenantId == tenantId && candidate.SourceType == sourceType &&
                candidate.SourceId == sourceId && candidate.Status == "open",
            cancellationToken);
        return item is null ? null : ToDto(item);
    }

    public async Task<ExpertReviewItemDto> CreateEscalationAsync(
        EscalateExpertReviewRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        EnsureCurrentTenant(tenantId);
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
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_expert_review_items_tenant_id_source_type_source_id"
            })
        {
            throw new ExpertReviewDuplicateException();
        }
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

    public Task<bool> IsActiveTenantMemberAsync(Guid userId, CancellationToken cancellationToken = default) =>
        dbContext.TenantMemberships.AsNoTracking().AnyAsync(
            membership => membership.TenantId == tenantContext.TenantId && membership.UserId == userId &&
                membership.Status == Gccs.Domain.Identity.MembershipStatus.Active && membership.User != null &&
                membership.User.Status == Gccs.Domain.Identity.UserStatus.Active,
            cancellationToken);

    public async Task<ExpertReviewItemDto?> AssignAsync(
        Guid itemId,
        AssignExpertReviewRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var item = await dbContext.ExpertReviewItems.SingleOrDefaultAsync(
            candidate => candidate.Id == itemId && candidate.TenantId == tenantContext.TenantId && candidate.Status == "open",
            cancellationToken);
        if (item is null) return null;
        item.AssignedExpertUserId = request.AssignedExpertUserId;
        item.DueAt = request.DueAt;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(item);
    }

    public async Task<ExpertReviewItemDto?> ResolveAsync(
        Guid itemId,
        ResolveExpertReviewRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var item = await dbContext.ExpertReviewItems.FirstOrDefaultAsync(
            candidate => candidate.Id == itemId && candidate.TenantId == tenantContext.TenantId && candidate.Status == "open",
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
        await MarkSourceResolvedAsync(item.SourceType, item.SourceId, item.TenantId, request.Decision, request.Notes, actorUserId, now, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(item);
    }

    private void EnsureCurrentTenant(Guid tenantId)
    {
        if (tenantId != tenantContext.TenantId)
            throw new InvalidOperationException("Expert review repository tenant scope does not match the current tenant.");
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
        else if (sourceType == "assistant_answer")
        {
            var answer = await dbContext.AssistantAnswers.FirstOrDefaultAsync(
                item => item.Id == sourceId && item.TenantId == tenantId,
                cancellationToken);
            if (answer is not null)
            {
                answer.HumanReviewStatus = "queued";
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
        string notes,
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
        else if (sourceType == "assistant_answer")
        {
            var answer = await dbContext.AssistantAnswers.FirstOrDefaultAsync(
                item => item.Id == sourceId && item.TenantId == tenantId,
                cancellationToken);
            if (answer is not null)
            {
                answer.HumanReviewStatus = decision;
                answer.ReviewedByUserId = actorUserId;
                answer.ReviewedAt = now;
                answer.ReviewDecision = decision;
                answer.ReviewNotes = notes;
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
