using Gccs.Application.Ai;

namespace Gccs.Api;

public sealed class AiOutputRetentionWorker(IServiceScopeFactory scopes, ILogger<AiOutputRetentionWorker> logger)
    : BackgroundService
{
    private const int BatchSize = 100;
    private static readonly TimeSpan FullBatchDelay = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var archived = await scope.ServiceProvider.GetRequiredService<AiOutputReviewService>()
                    .ArchiveExpiredAsync(BatchSize, stoppingToken);
                if (archived == BatchSize)
                {
                    await Task.Delay(FullBatchDelay, stoppingToken);
                    continue;
                }
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception exception)
            {
                logger.LogError(exception, "AI output retention processing failed; expired records remain eligible for retry.");
                try { await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            }
        }
    }
}
