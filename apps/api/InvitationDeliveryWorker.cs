using Gccs.Application.Identity;
using Gccs.Infrastructure.Identity;
using Microsoft.Extensions.Options;

namespace Gccs.Api;

public sealed class InvitationDeliveryWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<InvitationEmailOptions> options,
    ILogger<InvitationDeliveryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollInterval = TimeSpan.FromSeconds(Math.Clamp(options.Value.PollIntervalSeconds, 2, 60));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!options.Value.Enabled)
                {
                    await Task.Delay(pollInterval, stoppingToken);
                    continue;
                }

                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<InvitationDeliveryService>();
                var processed = await service.ProcessNextAsync(stoppingToken);
                if (!processed)
                {
                    await Task.Delay(pollInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Invitation delivery worker iteration failed. ExceptionType={ExceptionType}", exception.GetType().Name);
                await Task.Delay(pollInterval, stoppingToken);
            }
        }
    }
}
