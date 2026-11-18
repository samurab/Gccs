using System.ComponentModel;
using System.Diagnostics;

namespace Gccs.Api.LocalDevelopment;

internal static class DevelopmentStartupOwner
{
    public static async Task<string?> ReadIdentityAsync(int port)
    {
        if (OperatingSystem.IsWindows()) return null;
        var start = new ProcessStartInfo("/bin/bash") { UseShellExecute = false, RedirectStandardOutput = true };
        start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "LocalDevelopment", "startup-owner-identity.sh"));
        start.ArgumentList.Add(port.ToString());
        try
        {
            using var process = Process.Start(start);
            if (process is null) return null;
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            try
            {
                var identity = (await process.StandardOutput.ReadToEndAsync(timeout.Token)).Trim();
                await process.WaitForExitAsync(timeout.Token);
                var parts = identity.Split('|', 2);
                return process.ExitCode == 0 && parts.Length == 2 && int.TryParse(parts[0], out var pid) && pid > 0 &&
                    !string.IsNullOrWhiteSpace(parts[1]) && !parts[1].Contains('\n') ? identity : null;
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); }
                catch (InvalidOperationException) { /* Already exited. */ }
                return null;
            }
        }
        catch (Exception error) when (error is Win32Exception or IOException) { return null; }
    }
}
