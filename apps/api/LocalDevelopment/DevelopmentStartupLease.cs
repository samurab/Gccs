using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Gccs.Api.LocalDevelopment;

internal sealed class DevelopmentStartupLease : IDisposable
{
    private readonly List<TcpListener> _listeners = [];
    private DevelopmentStartupWatchdog? _watchdog;
    public string? ExistingUrl { get; private set; }

    public static async Task<DevelopmentStartupLease> AcquireAsync(
        IEnumerable<int> ports, Func<Task<string?>> findHealthyUrl, TimeSpan? timeout = null,
        Func<int, Task<string?>>? readOwnerIdentity = null)
    {
        var lease = new DevelopmentStartupLease();
        var coordinationPorts = ports.Distinct().Order().Select(port => (port - 1 + 32_768) % 65_535 + 1).ToArray();
        var timer = Stopwatch.StartNew();
        var limit = timeout ?? TimeSpan.FromSeconds(90);
        var previousOwners = new Dictionary<int, string>();
        var nextOwnerProbe = TimeSpan.Zero;
        readOwnerIdentity ??= DevelopmentStartupOwner.ReadIdentityAsync;
        try
        {
            while (true)
            {
                lease._watchdog?.Waiting();
                if (await findHealthyUrl() is { } existingUrl)
                {
                    lease.ExistingUrl = existingUrl;
                    lease.Dispose();
                    return lease;
                }
                // Arm before publishing the lease, including the first instruction after bind.
                lease._watchdog ??= new DevelopmentStartupWatchdog();
                var busyPort = 0;
                try
                {
                    foreach (var port in coordinationPorts)
                    {
                        busyPort = port;
                        var listener = new TcpListener(IPAddress.Loopback, port) { ExclusiveAddressUse = true };
                        lease._listeners.Add(listener);
                        listener.Start();
                    }
                }
                catch (SocketException error) when (error.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    lease.ReleaseListeners();
                    if (timer.Elapsed >= nextOwnerProbe)
                    {
                        var owner = await readOwnerIdentity(busyPort);
                        if (owner is not null)
                        {
                            if (previousOwners.TryGetValue(busyPort, out var previous) && previous != owner)
                                timer.Restart();
                            previousOwners[busyPort] = owner;
                        }
                        nextOwnerProbe = timer.Elapsed + TimeSpan.FromMilliseconds(Math.Min(1_000, limit.TotalMilliseconds / 4));
                    }
                    if (timer.Elapsed >= limit)
                        throw new InvalidOperationException("Another local API startup or coordination-port owner did not become healthy within the startup wait; no new API was started.");
                    await Task.Delay(200);
                    continue;
                }

                // The previous owner may have finished between our probe and acquisition.
                lease.ExistingUrl = await findHealthyUrl();
                if (lease.ExistingUrl is not null) lease.Dispose();
                else _ = lease._watchdog.WatchHealthAsync(findHealthyUrl);
                return lease;
            }
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        _watchdog?.Dispose();
        ReleaseListeners();
    }

    private void ReleaseListeners()
    {
        foreach (var listener in _listeners) listener.Stop();
        _listeners.Clear();
    }
}
