using Gccs.Application.Portals;
using Microsoft.Extensions.Options;

namespace Gccs.Api;

public sealed class PortalPackageLifecycleProcessingOptions
{
    public const string SectionName = "PortalPackageLifecycleProcessing";
    public bool Enabled { get; set; } = true;
    public int PollIntervalSeconds { get; set; } = 300;
}

public sealed class PortalPackageLifecycleWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<PortalPackageLifecycleProcessingOptions> options,
    ILogger<PortalPackageLifecycleWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Clamp(options.Value.PollIntervalSeconds, 30, 3600));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<PortalPackageLifecycleService>();
                var result = await service.ProcessDueAsync(timeProvider.GetUtcNow(), stoppingToken);
                if (result.RemindersCreated > 0 || result.PackagesExpired > 0)
                    logger.LogInformation(
                        "Portal package lifecycle processed {ReminderCount} reminders and {ExpirationCount} expirations.",
                        result.RemindersCreated,
                        result.PackagesExpired);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Portal package lifecycle processing failed.");
            }

            await Task.Delay(interval, timeProvider, stoppingToken);
        }
    }
}
