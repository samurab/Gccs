using System.Diagnostics;

namespace Gccs.Api.LocalDevelopment;

internal sealed class DevelopmentStartupWatchdog : IDisposable
{
    private readonly Process? _process;
    private readonly CancellationTokenSource _stopped = new();
    private int _disposed;

    public DevelopmentStartupWatchdog()
    {
        // Other platforms retain lease-only coordination; no production startup uses this helper.
        if (OperatingSystem.IsWindows()) return;
        var start = new ProcessStartInfo("/bin/bash") { UseShellExecute = false, RedirectStandardInput = true };
        start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "LocalDevelopment", "startup-owner-watchdog.sh"));
        start.ArgumentList.Add(Environment.ProcessId.ToString());
        start.ArgumentList.Add("45");
        _process = Process.Start(start) ?? throw new InvalidOperationException("Could not start the local API startup watchdog.");
        _process.StandardInput.AutoFlush = true;
    }

    public void Waiting() => _process?.StandardInput.WriteLine("waiting");

    public async Task WatchHealthAsync(Func<Task<string?>> findHealthyUrl)
    {
        try
        {
            while (!_stopped.IsCancellationRequested)
            {
                if (await findHealthyUrl() is not null)
                {
                    Dispose();
                    return;
                }
                await Task.Delay(200, _stopped.Token);
            }
        }
        catch (OperationCanceledException) when (_stopped.IsCancellationRequested) { }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _stopped.Cancel();
        _process?.StandardInput.WriteLine("ready");
        _process?.StandardInput.Close();
        _process?.Dispose();
    }
}
