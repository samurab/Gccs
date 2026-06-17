using Gccs.Application.Compliance;
using Gccs.Domain.Common;
using Gccs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Compliance;

public sealed class EfComplianceContentReviewRepository(GccsDbContext dbContext) : IComplianceContentReviewRepository
{
    public async Task<ComplianceContentReviewDto?> FindObligationReviewAsync(
        string obligationId,
        CancellationToken cancellationToken = default)
    {
        var obligation = await dbContext.Obligations
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == obligationId, cancellationToken);

        return obligation is null
            ? null
            : new ComplianceContentReviewDto(
                obligation.Id,
                obligation.ReviewState,
                obligation.RequiresExpertReview || obligation.SourceRequiresExpertReview,
                obligation.ReviewedByUserId,
                obligation.LastReviewedAt);
    }

    public async Task<ComplianceContentReviewDto?> UpdateObligationReviewStateAsync(
        string obligationId,
        ReviewState state,
        Guid? reviewerUserId,
        DateOnly? reviewedAt,
        CancellationToken cancellationToken = default)
    {
        var obligation = await dbContext.Obligations
            .FirstOrDefaultAsync(candidate => candidate.Id == obligationId, cancellationToken);

        if (obligation is null)
        {
            return null;
        }

        obligation.ReviewState = state;
        obligation.ReviewedByUserId = reviewerUserId ?? obligation.ReviewedByUserId;
        obligation.LastReviewedAt = reviewedAt ?? obligation.LastReviewedAt;

        var clause = await dbContext.Clauses.FirstOrDefaultAsync(candidate => candidate.Id == obligationId, cancellationToken);
        if (clause is not null)
        {
            clause.ReviewState = state;
            clause.ReviewedByUserId = obligation.ReviewedByUserId;
            clause.LastReviewedAt = obligation.LastReviewedAt;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ComplianceContentReviewDto(
            obligation.Id,
            obligation.ReviewState,
            obligation.RequiresExpertReview || obligation.SourceRequiresExpertReview,
            obligation.ReviewedByUserId,
            obligation.LastReviewedAt);
    }

    public async Task<ComplianceContentReviewDto?> FindClauseReviewAsync(
        string clauseId,
        CancellationToken cancellationToken = default)
    {
        var clause = await dbContext.Clauses
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == clauseId, cancellationToken);

        return clause is null
            ? null
            : new ComplianceContentReviewDto(
                clause.Id,
                clause.ReviewState,
                clause.RequiresExpertReview || clause.SourceRequiresExpertReview,
                clause.ReviewedByUserId,
                clause.LastReviewedAt);
    }

    public async Task<ComplianceContentReviewDto?> UpdateClauseReviewStateAsync(
        string clauseId,
        ReviewState state,
        Guid? reviewerUserId,
        DateOnly? reviewedAt,
        CancellationToken cancellationToken = default)
    {
        var clause = await dbContext.Clauses.FirstOrDefaultAsync(candidate => candidate.Id == clauseId, cancellationToken);
        if (clause is null)
        {
            return null;
        }

        clause.ReviewState = state;
        clause.ReviewedByUserId = reviewerUserId ?? clause.ReviewedByUserId;
        clause.LastReviewedAt = reviewedAt ?? clause.LastReviewedAt;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ComplianceContentReviewDto(
            clause.Id,
            clause.ReviewState,
            clause.RequiresExpertReview || clause.SourceRequiresExpertReview,
            clause.ReviewedByUserId,
            clause.LastReviewedAt);
    }

    public Task<bool> CanUseObligationForNewMappingAsync(
        string obligationId,
        CancellationToken cancellationToken = default) =>
        dbContext.Obligations.AnyAsync(
            obligation => obligation.Id == obligationId && obligation.ReviewState == ReviewState.Published,
            cancellationToken);
}
