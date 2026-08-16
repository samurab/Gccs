using Gccs.Api.Security;
using Gccs.Application.Reports;
using Microsoft.Extensions.Options;

namespace Gccs.Api;

public sealed class ReportExportProcessingOptions
{
    public const string SectionName = "ReportExportProcessing";
    public bool Enabled { get; set; } = true;
    public int PollIntervalSeconds { get; set; } = 2;
    public int LeaseMinutes { get; set; } = 10;
    public int MaximumAttempts { get; set; } = 3;
}

public sealed class ReportExportWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ReportExportProcessingOptions> options,
    ILogger<ReportExportWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollInterval = TimeSpan.FromSeconds(Math.Clamp(options.Value.PollIntervalSeconds, 1, 60));
        var leaseDuration = TimeSpan.FromMinutes(Math.Clamp(options.Value.LeaseMinutes, 1, 60));
        var maximumAttempts = Math.Clamp(options.Value.MaximumAttempts, 1, 10);

        while (!stoppingToken.IsCancellationRequested)
        {
            ClaimedReportExport? claimed = null;
            try
            {
                using (var claimScope = scopeFactory.CreateScope())
                {
                    claimed = await claimScope.ServiceProvider
                        .GetRequiredService<IReportExportRepository>()
                        .TryClaimNextAsync(DateTimeOffset.UtcNow, leaseDuration, maximumAttempts, stoppingToken);
                }

                if (claimed is null)
                {
                    await Task.Delay(pollInterval, stoppingToken);
                    continue;
                }

                using var processingScope = scopeFactory.CreateScope();
                InitializeTenantContext(processingScope.ServiceProvider, claimed);
                var service = processingScope.ServiceProvider.GetRequiredService<ReportExportService>();
                var result = await service.ProcessAsync(claimed, stoppingToken);
                if (result is null)
                {
                    throw new InvalidOperationException("The claimed PDF report export lease was no longer valid.");
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "PDF report export worker iteration failed. ExportId={ExportId} Attempt={Attempt} ExceptionType={ExceptionType}",
                    claimed?.ExportId,
                    claimed?.AttemptNumber,
                    exception.GetType().Name);
                if (claimed is not null)
                {
                    await TryRecordFailureAsync(claimed, maximumAttempts, exception, stoppingToken);
                }

                await Task.Delay(pollInterval, stoppingToken);
            }
        }
    }

    private async Task TryRecordFailureAsync(
        ClaimedReportExport claimed,
        int maximumAttempts,
        Exception exception,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            InitializeTenantContext(scope.ServiceProvider, claimed);
            var service = scope.ServiceProvider.GetRequiredService<ReportExportService>();
            await service.RecordProcessingFailureAsync(
                claimed,
                maximumAttempts,
                $"render_failed_{exception.GetType().Name.ToLowerInvariant()}",
                cancellationToken);
        }
        catch (Exception failureException) when (failureException is not OperationCanceledException)
        {
            logger.LogError(
                failureException,
                "PDF report export worker could not persist failure state. ExportId={ExportId} ExceptionType={ExceptionType}",
                claimed.ExportId,
                failureException.GetType().Name);
        }
    }

    private static void InitializeTenantContext(IServiceProvider serviceProvider, ClaimedReportExport claimed) =>
        serviceProvider.GetRequiredService<HttpTenantContext>().InitializeBackground(
            claimed.TenantId,
            claimed.RequestedByUserId,
            claimed.RequestedByEmail);
}
