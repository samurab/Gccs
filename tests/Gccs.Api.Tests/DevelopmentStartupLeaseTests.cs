using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using Gccs.Api.LocalDevelopment;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class DevelopmentStartupLeaseTests
{
    [Fact]
    public async Task Simultaneous_cold_starts_have_one_owner_and_waiters_reuse_its_healthy_url()
    {
        var port = UnusedPort();
        var healthy = false;
        var url = $"http://localhost:{port}";
        Task<string?> Probe() => Task.FromResult(Volatile.Read(ref healthy) ? url : null);
        using var owner = await DevelopmentStartupLease.AcquireAsync([port, port], Probe);
        Assert.Null(owner.ExistingUrl);
        var waiters = Enumerable.Range(0, 7).Select(_ => DevelopmentStartupLease.AcquireAsync([port], Probe, TimeSpan.FromSeconds(3))).ToArray();
        await Task.Delay(100);
        Assert.All(waiters, task => Assert.False(task.IsCompleted));
        Volatile.Write(ref healthy, true);
        foreach (var task in waiters)
        {
            using var reused = await task;
            Assert.Equal(url, reused.ExistingUrl);
        }
    }

    [Fact]
    public async Task Owner_release_allows_a_waiter_to_start_when_the_previous_startup_did_not_finish()
    {
        var port = UnusedPort();
        var owner = await DevelopmentStartupLease.AcquireAsync([port], () => Task.FromResult<string?>(null));
        var waiting = DevelopmentStartupLease.AcquireAsync([port], () => Task.FromResult<string?>(null), TimeSpan.FromSeconds(3));
        try
        {
            await Task.Delay(100);
            Assert.False(waiting.IsCompleted);
        }
        finally { owner.Dispose(); }
        using var replacement = await waiting;
        Assert.Null(replacement.ExistingUrl);
    }

    [Fact]
    public async Task Recheck_after_acquisition_reuses_a_late_healthy_instance_and_releases_the_lease()
    {
        var port = UnusedPort();
        var probes = 0;
        using var reused = await DevelopmentStartupLease.AcquireAsync([port], () =>
            Task.FromResult(++probes == 1 ? null : "http://localhost/"));
        Assert.Equal("http://localhost/", reused.ExistingUrl);
        using var next = await DevelopmentStartupLease.AcquireAsync([port], () => Task.FromResult<string?>(null), TimeSpan.FromMilliseconds(300));
        Assert.Null(next.ExistingUrl);
    }

    [Fact]
    public async Task Busy_coordination_port_fails_closed_and_releases_partially_acquired_leases()
    {
        var ports = new[] { UnusedPort(), UnusedPort() }.Order().ToArray();
        using var owner = await DevelopmentStartupLease.AcquireAsync([ports[1]], () => Task.FromResult<string?>(null));
        await Assert.ThrowsAsync<InvalidOperationException>(() => DevelopmentStartupLease.AcquireAsync(
            ports, () => Task.FromResult<string?>(null), TimeSpan.FromMilliseconds(100)));
        using var releasedFirstPort = await DevelopmentStartupLease.AcquireAsync([ports[0]],
            () => Task.FromResult<string?>(null), TimeSpan.FromMilliseconds(300));
        Assert.Null(releasedFirstPort.ExistingUrl);
    }

    [Theory]
    [InlineData("Production", "http://localhost:5062")]
    [InlineData("Staging", "http://localhost:5062")]
    [InlineData("Development", "https://localhost:5062")]
    [InlineData("Development", "http://0.0.0.0:5062")]
    public async Task Unsupported_startup_modes_do_not_acquire_a_development_lease(string environment, string url)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["urls"] = url }).Build();
        using var lease = await ExistingDevelopmentApi.CoordinateStartupAsync(configuration, new TestHostEnvironment(environment));
        Assert.Null(lease);
    }

    [Fact]
    public async Task Successive_owner_changes_renew_the_wait_until_the_healthy_url_is_available()
    {
        var port = UnusedPort();
        var healthy = false;
        var identities = 0;
        Task<string?> Probe() => Task.FromResult(Volatile.Read(ref healthy) ? "http://localhost/" : null);
        using var owner = await DevelopmentStartupLease.AcquireAsync([port], Probe);
        var waiting = DevelopmentStartupLease.AcquireAsync([port], Probe, TimeSpan.FromMilliseconds(600),
            _ => Task.FromResult<string?>("owner-" + Math.Min(Interlocked.Increment(ref identities), 4)));
        await Task.Delay(1_000);
        Assert.False(waiting.IsCompleted);
        Volatile.Write(ref healthy, true);
        using var reused = await waiting.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal("http://localhost/", reused.ExistingUrl);
        Assert.True(identities >= 3);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("same-owner")]
    public async Task Unknown_or_unchanged_owners_do_not_extend_the_bounded_wait(string? identity)
    {
        var port = UnusedPort();
        using var owner = await DevelopmentStartupLease.AcquireAsync([port], () => Task.FromResult<string?>(null));
        var timer = Stopwatch.StartNew();
        await Assert.ThrowsAsync<InvalidOperationException>(() => DevelopmentStartupLease.AcquireAsync([port],
            () => Task.FromResult<string?>(null), TimeSpan.FromMilliseconds(600), _ => Task.FromResult(identity)));
        Assert.InRange(timer.Elapsed.TotalMilliseconds, 500, 3_000);
    }

    [Fact]
    public async Task Kernel_identity_is_stable_for_a_listener_and_absent_after_it_closes()
    {
        if (OperatingSystem.IsWindows()) return;
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var identity = await DevelopmentStartupOwner.ReadIdentityAsync(port);
        Assert.StartsWith(Environment.ProcessId + "|", identity);
        Assert.Equal(identity, await DevelopmentStartupOwner.ReadIdentityAsync(port));
        listener.Stop();
        Assert.Null(await DevelopmentStartupOwner.ReadIdentityAsync(port));
    }

    private static int UnusedPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
