using Gccs.Application.Notifications;
using Microsoft.Extensions.Options;

namespace Gccs.Api;

public sealed class DueDateReminderProcessingOptions
{
    public const string SectionName = "DueDateReminderProcessing";
    public bool Enabled { get; set; } = true;
    public int PollIntervalMinutes { get; set; } = 60;
    public int LeadTimeDays { get; set; } = 14;
    public int BatchSize { get; set; } = 200;
}

public sealed class DueDateReminderWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<DueDateReminderProcessingOptions> options,
    ILogger<DueDateReminderWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Clamp(options.Value.PollIntervalMinutes, 1, 1440));
        var leadTimeDays = Math.Clamp(options.Value.LeadTimeDays, 0, 365);
        var batchSize = Math.Clamp(options.Value.BatchSize, 1, 1000);
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await Task.Delay(interval, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            try
            {
                if (options.Value.Enabled)
                {
                    using var scope = scopeFactory.CreateScope();
                    var created = await scope.ServiceProvider.GetRequiredService<IDueDateReminderRepository>()
                        .RunAutomatedAsync(leadTimeDays, batchSize, stoppingToken);
                    if (created > 0) logger.LogInformation("Created {ReminderCount} due-date reminders.", created);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception exception)
            {
                logger.LogError(exception, "Due-date reminder worker iteration failed. ExceptionType={ExceptionType}", exception.GetType().Name);
            }
        }
    }
}
