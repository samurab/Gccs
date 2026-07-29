using Gccs.Application.Contracts;
using Gccs.Domain.Contracts;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Contracts;

public sealed class EfExtractionJobWorkRepository(GccsDbContext dbContext) : IExtractionJobWorkRepository
{
    public async Task<ClaimedExtractionJob?> TryClaimNextAsync(
        DateTimeOffset now,
        TimeSpan leaseDuration,
        int maximumAttempts,
        CancellationToken cancellationToken = default)
    {
        var candidateIds = await dbContext.Set<ExtractionJobEntity>()
            .AsNoTracking()
            .Where(job =>
                job.ProcessingAttemptCount <= maximumAttempts &&
                (job.Status == ExtractionJobStatus.Queued ||
                 (job.Status == ExtractionJobStatus.Processing &&
                  (job.ProcessingLeaseUntil == null ||
                   job.ProcessingLeaseUntil < now))))
            .OrderBy(job => job.RequestedAt)
            .Select(job => job.Id)
            .Take(10)
            .ToArrayAsync(cancellationToken);

        foreach (var candidateId in candidateIds)
        {
            var leaseId = Guid.NewGuid();
            var claimed = await dbContext.Set<ExtractionJobEntity>()
                .Where(job =>
                    job.Id == candidateId &&
                    job.ProcessingAttemptCount <= maximumAttempts &&
                    (job.Status == ExtractionJobStatus.Queued ||
                     (job.Status == ExtractionJobStatus.Processing &&
                      (job.ProcessingLeaseUntil == null ||
                       job.ProcessingLeaseUntil < now))))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(job => job.Status, ExtractionJobStatus.Processing)
                    .SetProperty(job => job.StartedAt, job => job.StartedAt ?? now)
                    .SetProperty(job => job.ProcessingLeaseId, leaseId)
                    .SetProperty(job => job.ProcessingLeaseUntil, now.Add(leaseDuration))
                    .SetProperty(job => job.LastProcessingAttemptAt, now)
                    .SetProperty(job => job.ProcessingAttemptCount, job => job.ProcessingAttemptCount + 1),
                    cancellationToken);
            if (claimed != 1)
            {
                continue;
            }

            var job = await dbContext.Set<ExtractionJobEntity>()
                .AsNoTracking()
                .Where(item => item.Id == candidateId && item.ProcessingLeaseId == leaseId)
                .Select(item => new
                {
                    item.Id,
                    item.TenantId,
                    item.RequestedByUserId,
                    item.ProcessingAttemptCount
                })
                .SingleAsync(cancellationToken);
            var email = await dbContext.Users
                .AsNoTracking()
                .Where(user => user.Id == job.RequestedByUserId && user.TenantId == job.TenantId)
                .Select(user => user.Email)
                .SingleOrDefaultAsync(cancellationToken);

            return new ClaimedExtractionJob(
                job.Id,
                job.TenantId,
                job.RequestedByUserId,
                string.IsNullOrWhiteSpace(email) ? "extraction-worker@example.com" : email,
                leaseId,
                job.ProcessingAttemptCount);
        }

        return null;
    }
}
