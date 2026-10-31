using Gccs.Application.Ai;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Ai;

public sealed class EfAiOutputRetentionRepository(GccsDbContext dbContext) : IAiOutputRetentionRepository
{
    public async Task<IReadOnlyList<AiOutputRetentionArchiveDto>> ArchiveExpiredAsync(
        DateTimeOffset now, int batchSize, CancellationToken cancellationToken = default)
    {
        // Production retention processing can run on more than one API instance. Lock only
        // the selected batch and skip rows claimed by another worker so the same expiration
        // cannot create duplicate review/audit history or turn routine contention into retries.
        var answers = await dbContext.AssistantAnswers
            .FromSqlInterpolated($"""
                SELECT * FROM gccs.assistant_answers
                WHERE retain_until <= {now} AND review_state <> {(int)AiOutputReviewState.Archived}
                ORDER BY retain_until, id
                LIMIT {batchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToArrayAsync(cancellationToken);
        var archived = new List<AiOutputRetentionArchiveDto>(answers.Length);
        foreach (var answer in answers)
        {
            var previous = answer.ReviewState;
            archived.Add(new(answer.TenantId, answer.Id, previous));
            answer.ReviewState = AiOutputReviewState.Archived;
            answer.HumanReviewStatus = "Archived";
            answer.ReviewDecision = "Archived";
            answer.ReviewNotes = "Archived automatically after the configured retention date.";
            answer.ReviewedByUserId = null;
            answer.ReviewedAt = now;
            answer.Version++;
            dbContext.AssistantOutputReviews.Add(new AssistantOutputReviewEntity
            {
                Id = Guid.NewGuid(), TenantId = answer.TenantId, AnswerId = answer.Id,
                PreviousState = previous, NewState = AiOutputReviewState.Archived,
                ReviewerUserId = null, Note = answer.ReviewNotes, CreatedAt = now
            });
        }
        if (answers.Length > 0) await dbContext.SaveChangesAsync(cancellationToken);
        return archived;
    }
}
