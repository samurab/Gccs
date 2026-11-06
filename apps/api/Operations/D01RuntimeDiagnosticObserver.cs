using System.Globalization;
using System.Reflection;
using Gccs.Application.Operations;
using Gccs.Infrastructure.Persistence;

namespace Gccs.Api.Operations;

public sealed class D01RuntimeDiagnosticObserver(
    IConfiguration configuration,
    IServiceScopeFactory scopes,
    IHostApplicationLifetime lifetime,
    TimeProvider time,
    ILogger<D01RuntimeDiagnosticObserver> logger) : BackgroundService
{
    private const string Prefix = "Operations:D01RuntimeDiagnostic:";
    private readonly Guid _instanceId = Guid.NewGuid();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, lifetime.ApplicationStopping);
        var token = cancellation.Token;
        try
        {
            if (!bool.TryParse(configuration[Prefix + "Enabled"], out var enabled) || !enabled)
                return;

            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var registration = lifetime.ApplicationStarted.Register(() => started.TrySetResult());
            await started.Task.WaitAsync(token);
            token.ThrowIfCancellationRequested();

            var expectation = new RuntimeDatabaseBindingExpectation(
                configuration[Prefix + "ExpectedServerSha256"] ?? string.Empty,
                configuration[Prefix + "ExpectedDatabaseSha256"] ?? string.Empty);
            var expiryText = configuration[Prefix + "ExpiresUtc"];
            if (!expectation.IsValid || expiryText is null ||
                !(expiryText.EndsWith('Z') || expiryText.EndsWith("+00:00", StringComparison.Ordinal)) ||
                !DateTimeOffset.TryParseExact(expiryText,
                    ["yyyy-MM-dd'T'HH:mm:ss'Z'", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'",
                        "yyyy-MM-dd'T'HH:mm:sszzz", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz"],
                    CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var expiry) || expiry.Offset != TimeSpan.Zero)
            {
                Log(new(RuntimeDatabaseBindingStatus.InvalidConfiguration));
                return;
            }

            if (time.GetUtcNow() >= expiry)
            {
                Log(new(RuntimeDatabaseBindingStatus.Expired));
                return;
            }

            RuntimeDatabaseBindingResult result;
            using (var scope = scopes.CreateScope())
            {
                var diagnostic = scope.ServiceProvider.GetRequiredService<IRuntimeDatabaseBindingDiagnostic>();
                token.ThrowIfCancellationRequested();
                result = time.GetUtcNow() >= expiry
                    ? new(RuntimeDatabaseBindingStatus.Expired)
                    : diagnostic.Observe(expectation);
            }
            token.ThrowIfCancellationRequested();
            Log(time.GetUtcNow() >= expiry ? new(RuntimeDatabaseBindingStatus.Expired) : result);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            Log(new(RuntimeDatabaseBindingStatus.Failed));
        }
    }

    private void Log(RuntimeDatabaseBindingResult result)
    {
        try
        {
            var api = typeof(D01RuntimeDiagnosticObserver).Assembly;
            var infrastructure = typeof(GccsDbContext).Assembly;
            logger.LogInformation(
                new EventId(391601, "D01RuntimeBinding"),
                "D01RuntimeBinding SchemaVersion={SchemaVersion} Purpose={Purpose} ObservedAtUtc={ObservedAtUtc} " +
                "DiagnosticInstanceId={DiagnosticInstanceId} Status={Status} ProviderMatches={ProviderMatches} " +
                "ServerMatches={ServerMatches} PortMatches={PortMatches} DatabaseMatches={DatabaseMatches} " +
                "ApiModuleMvid={ApiModuleMvid} ApiInformationalVersion={ApiInformationalVersion} " +
                "InfrastructureModuleMvid={InfrastructureModuleMvid} InfrastructureInformationalVersion={InfrastructureInformationalVersion}",
                1, "D01RuntimeBinding", time.GetUtcNow(), _instanceId, result.Status.ToString(),
                result.ProviderMatches, result.ServerMatches, result.PortMatches, result.DatabaseMatches,
                api.ManifestModule.ModuleVersionId, api.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
                infrastructure.ManifestModule.ModuleVersionId,
                infrastructure.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
        }
        catch (Exception)
        {
            // A diagnostic or logging failure must not change API availability or disclose its exception.
        }
    }
}
