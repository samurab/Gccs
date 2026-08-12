using Gccs.Application.Marketing;
using Gccs.Infrastructure.Marketing;
using Microsoft.Extensions.Options;

namespace Gccs.Api;

public sealed class DemoRequestDeliveryWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<DemoRequestOptions> options,
    ILogger<DemoRequestDeliveryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Clamp(options.Value.PollIntervalSeconds, 2, 60));
        var nextRetentionRun = DateTimeOffset.MinValue;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!options.Value.Enabled)
                {
                    await Task.Delay(interval, stoppingToken);
                    continue;
                }
                using var scope = scopeFactory.CreateScope();
                if (DateTimeOffset.UtcNow >= nextRetentionRun)
                {
                    var repository = scope.ServiceProvider.GetRequiredService<IDemoRequestRepository>();
                    var removed = await repository.DeleteExpiredAsync(
                        DateTimeOffset.UtcNow.AddDays(-Math.Clamp(options.Value.RetentionDays, 30, 3650)),
                        stoppingToken);
                    if (removed > 0) logger.LogInformation("Expired demo requests removed. Count={Count}", removed);
                    nextRetentionRun = DateTimeOffset.UtcNow.AddDays(1);
                }
                var result = await scope.ServiceProvider.GetRequiredService<DemoRequestDeliveryService>()
                    .ProcessNextWithResultAsync(stoppingToken);
                if (result.Status == DemoRequestDeliveryProcessingStatus.Completed)
                {
                    logger.LogInformation(
                        "Demo request delivery processed. DeliveryId={DeliveryId} RequestId={RequestId} DeliveryKind={DeliveryKind} Status={Status}",
                        result.DeliveryId,
                        result.RequestId,
                        result.DeliveryKind,
                        result.Status);
                }
                else if (result.Status == DemoRequestDeliveryProcessingStatus.Failed)
                {
                    logger.LogWarning(
                        "Demo request delivery failed. DeliveryId={DeliveryId} RequestId={RequestId} DeliveryKind={DeliveryKind} FailureCode={FailureCode} RetryAt={RetryAt}",
                        result.DeliveryId,
                        result.RequestId,
                        result.DeliveryKind,
                        result.FailureCode,
                        result.RetryAt);
                }

                if (!result.Processed) await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                logger.LogError(exception, "Demo request delivery worker iteration failed. ExceptionType={ExceptionType}", exception.GetType().Name);
                await Task.Delay(interval, stoppingToken);
            }
        }
    }
}
