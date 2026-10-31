namespace Gccs.Application.Contracts;

public sealed record ClaimedExtractionJob(
    Guid JobId,
    Guid TenantId,
    Guid RequestedByUserId,
    string RequestedByEmail,
    Guid LeaseId,
    int AttemptNumber);

public interface IExtractionJobWorkRepository
{
    Task<ClaimedExtractionJob?> TryClaimNextAsync(
        DateTimeOffset now,
        TimeSpan leaseDuration,
        int maximumAttempts,
        CancellationToken cancellationToken = default);
}
