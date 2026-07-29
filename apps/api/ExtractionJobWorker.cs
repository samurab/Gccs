using Gccs.Api.Security;
using Gccs.Application.Contracts;
using Microsoft.Extensions.Options;

namespace Gccs.Api;

public sealed class ExtractionProcessingOptions
{
    public const string SectionName = "ExtractionProcessing";

    public bool Enabled { get; set; } = true;

    public int PollIntervalSeconds { get; set; } = 2;

    public int LeaseMinutes { get; set; } = 10;

    public int MaximumAttempts { get; set; } = 3;
}

public sealed class ExtractionJobWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ExtractionProcessingOptions> options,
    ILogger<ExtractionJobWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollInterval = TimeSpan.FromSeconds(Math.Clamp(options.Value.PollIntervalSeconds, 1, 60));
        var leaseDuration = TimeSpan.FromMinutes(Math.Clamp(options.Value.LeaseMinutes, 1, 60));
        var maximumAttempts = Math.Clamp(options.Value.MaximumAttempts, 1, 10);

        while (!stoppingToken.IsCancellationRequested)
        {
            ClaimedExtractionJob? claimed = null;
            try
            {
                using (var claimScope = scopeFactory.CreateScope())
                {
                    var repository = claimScope.ServiceProvider.GetRequiredService<IExtractionJobWorkRepository>();
                    claimed = await repository.TryClaimNextAsync(
                        DateTimeOffset.UtcNow,
                        leaseDuration,
                        maximumAttempts,
                        stoppingToken);
                }

                if (claimed is null)
                {
                    await Task.Delay(pollInterval, stoppingToken);
                    continue;
                }

                if (claimed.AttemptNumber > maximumAttempts)
                {
                    await MarkAttemptsExhaustedAsync(claimed, maximumAttempts, stoppingToken);
                    continue;
                }

                using var processingScope = scopeFactory.CreateScope();
                InitializeTenantContext(processingScope.ServiceProvider, claimed);
                var service = processingScope.ServiceProvider.GetRequiredService<ContractService>();
                var result = await service.ProcessExtractionJobAsync(
                    claimed.JobId,
                    claimed.RequestedByUserId,
                    stoppingToken);
                if (result is null)
                {
                    throw new InvalidOperationException("Claimed extraction job was not available in its tenant scope.");
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
                    "Extraction worker iteration failed. JobId={JobId} Attempt={Attempt} ExceptionType={ExceptionType}",
                    claimed?.JobId,
                    claimed?.AttemptNumber,
                    exception.GetType().Name);
                if (claimed is not null)
                {
                    await TryMarkFailedAsync(claimed, exception, stoppingToken);
                }

                await Task.Delay(pollInterval, stoppingToken);
            }
        }
    }

    private async Task TryMarkFailedAsync(
        ClaimedExtractionJob claimed,
        Exception exception,
        CancellationToken cancellationToken)
    {
        try
        {
            using var failureScope = scopeFactory.CreateScope();
            InitializeTenantContext(failureScope.ServiceProvider, claimed);
            var service = failureScope.ServiceProvider.GetRequiredService<ContractService>();
            await service.MarkExtractionJobFailedAsync(
                claimed.JobId,
                claimed.RequestedByUserId,
                $"Background extraction failed ({exception.GetType().Name}).",
                cancellationToken);
        }
        catch (Exception failureException) when (failureException is not OperationCanceledException)
        {
            logger.LogError(
                failureException,
                "Extraction worker could not persist failure state. JobId={JobId} ExceptionType={ExceptionType}",
                claimed.JobId,
                failureException.GetType().Name);
        }
    }

    private async Task MarkAttemptsExhaustedAsync(
        ClaimedExtractionJob claimed,
        int maximumAttempts,
        CancellationToken cancellationToken)
    {
        using var failureScope = scopeFactory.CreateScope();
        InitializeTenantContext(failureScope.ServiceProvider, claimed);
        var service = failureScope.ServiceProvider.GetRequiredService<ContractService>();
        await service.MarkExtractionJobFailedAsync(
            claimed.JobId,
            claimed.RequestedByUserId,
            $"Background extraction did not complete after {maximumAttempts} attempts.",
            cancellationToken);
    }

    private static void InitializeTenantContext(IServiceProvider serviceProvider, ClaimedExtractionJob claimed)
    {
        serviceProvider.GetRequiredService<HttpTenantContext>().InitializeBackground(
            claimed.TenantId,
            claimed.RequestedByUserId,
            claimed.RequestedByEmail);
    }
}
