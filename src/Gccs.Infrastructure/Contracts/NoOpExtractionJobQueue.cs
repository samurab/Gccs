using Gccs.Application.Contracts;
using Microsoft.Extensions.Logging;

namespace Gccs.Infrastructure.Contracts;

public sealed class NoOpExtractionJobQueue(ILogger<NoOpExtractionJobQueue> logger) : IExtractionJobQueue
{
    public Task EnqueueAsync(Guid extractionJobId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Clause extraction job {ExtractionJobId} queued for future background processing.",
            extractionJobId);

        return Task.CompletedTask;
    }
}
