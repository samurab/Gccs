using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Gccs.Api.Operations;
using Gccs.Application.Operations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Gccs.Infrastructure.Persistence;
using Xunit;
using Xunit.Abstractions;

namespace Gccs.Api.Tests;

public sealed class D01RuntimeDiagnosticObserverTests(ITestOutputHelper output)
{
    private const string Prefix = "Operations:D01RuntimeDiagnostic:";
    private static readonly DateTimeOffset Now = new(2026, 11, 6, 18, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    [InlineData("malformed-sentinel")]
    public async Task Disabled_is_silent_and_never_resolves_a_scope(string? enabled)
    {
        var fixture = new Fixture();
        fixture.Values[Prefix + "Enabled"] = enabled;
        await fixture.RunAsync();
        Assert.Empty(fixture.Logger.Events);
        Assert.Equal(0, fixture.Scopes.Created);
    }

    [Theory]
    [InlineData("ExpectedServerSha256", null)]
    [InlineData("ExpectedServerSha256", "sentinel-secret")]
    [InlineData("ExpectedDatabaseSha256", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("ExpiresUtc", null)]
    [InlineData("ExpiresUtc", "2026-11-07T00:00:00")]
    [InlineData("ExpiresUtc", "2026-11-07T02:00:00+02:00")]
    [InlineData("ExpiresUtc", "invalid-sentinel")]
    [InlineData("ExpiresUtc", "23:59Z")]
    [InlineData("ExpiresUtc", "November 7 2026 23:59:00+00:00")]
    public async Task Invalid_configuration_emits_only_fixed_status_without_resolving_persistence(string key, string? value)
    {
        var fixture = new Fixture();
        fixture.Values[Prefix + key] = value;
        await fixture.RunAsync();
        Assert.Equal("InvalidConfiguration", fixture.SingleStatus());
        Assert.Equal(0, fixture.Scopes.Created);
        Assert.DoesNotContain("sentinel", fixture.Logger.Events.Single().Text);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task Expiry_including_exact_boundary_prevents_scope_creation(int seconds)
    {
        var fixture = new Fixture();
        fixture.Values[Prefix + "ExpiresUtc"] = Now.AddSeconds(seconds).ToString("O");
        await fixture.RunAsync();
        Assert.Equal("Expired", fixture.SingleStatus());
        Assert.Equal(0, fixture.Scopes.Created);
    }

    [Theory]
    [InlineData("2026-11-06T19:00:00Z")]
    [InlineData("2026-11-06T19:00:00.1Z")]
    [InlineData("2026-11-06T19:00:00+00:00")]
    public async Task Explicit_ISO_UTC_expiry_is_supported(string expiry)
    {
        var fixture = new Fixture();
        fixture.Values[Prefix + "ExpiresUtc"] = expiry;
        await fixture.RunAsync();
        Assert.Equal("Matched", fixture.SingleStatus());
    }

    [Fact]
    public void Offline_test_binary_provenance_is_recorded_separately_from_deployment_evidence()
    {
        foreach (var assembly in new[] { typeof(D01RuntimeDiagnosticObserver).Assembly, typeof(GccsDbContext).Assembly })
        {
            Assert.NotEqual(Guid.Empty, assembly.ManifestModule.ModuleVersionId);
            output.WriteLine(JsonSerializer.Serialize(new
            {
                kind = "offline-debug-verification-binary-not-deployment-artifact",
                assembly = assembly.GetName().Name,
                moduleMvid = assembly.ManifestModule.ModuleVersionId,
                informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
                sha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(assembly.Location)))
            }));
        }
    }

    [Fact]
    public async Task Observation_waits_for_application_started_and_logs_exact_allowlist_once()
    {
        var fixture = new Fixture();
        using var observer = fixture.CreateObserver();
        await observer.StartAsync(CancellationToken.None);
        Assert.Empty(fixture.Logger.Events);
        Assert.Equal(0, fixture.Scopes.Created);
        fixture.Lifetime.NotifyStarted();
        await observer.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5));
        fixture.Lifetime.NotifyStarted();
        var entry = Assert.Single(fixture.Logger.Events);
        Assert.Null(entry.Exception);
        Assert.Equal(391601, entry.Id.Id);
        Assert.Equal("Matched", entry.Values["Status"]);
        Assert.Equal(new[]
        {
            "ApiInformationalVersion", "ApiModuleMvid", "DatabaseMatches", "DiagnosticInstanceId",
            "InfrastructureInformationalVersion", "InfrastructureModuleMvid", "ObservedAtUtc", "PortMatches",
            "ProviderMatches", "Purpose", "SchemaVersion", "ServerMatches", "Status", "{OriginalFormat}"
        }, entry.Values.Keys.Order(StringComparer.Ordinal).ToArray());
        Assert.Equal(Now, entry.Values["ObservedAtUtc"]);
        Assert.Equal(1, entry.Values["SchemaVersion"]);
        Assert.Equal("D01RuntimeBinding", entry.Values["Purpose"]);
        Assert.Equal(typeof(D01RuntimeDiagnosticObserver).Assembly.ManifestModule.ModuleVersionId, entry.Values["ApiModuleMvid"]);
        Assert.IsType<Guid>(entry.Values["InfrastructureModuleMvid"]);
        Assert.Equal(1, fixture.Diagnostic.Calls);
        Assert.Equal(1, fixture.Scopes.Created);
        Assert.Equal(1, fixture.Scopes.Disposed);
    }

    [Fact]
    public async Task Separate_process_observers_have_distinct_instance_ids()
    {
        var first = new Fixture();
        var second = new Fixture();
        await first.RunAsync();
        await second.RunAsync();
        Assert.NotEqual(first.Logger.Events.Single().Values["DiagnosticInstanceId"],
            second.Logger.Events.Single().Values["DiagnosticInstanceId"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Stopping_before_start_or_during_observation_discards_output(bool duringObservation)
    {
        var fixture = new Fixture();
        if (duringObservation)
            fixture.Diagnostic.OnObserve = () => fixture.Lifetime.StopApplication();
        else
            fixture.Lifetime.StopApplication();
        await fixture.RunAsync();
        Assert.Empty(fixture.Logger.Events);
        Assert.Equal(duringObservation ? 1 : 0, fixture.Diagnostic.Calls);
    }

    [Fact]
    public async Task Stop_cancels_the_pending_start_notification_without_inspection()
    {
        var fixture = new Fixture();
        using var observer = fixture.CreateObserver();
        await observer.StartAsync(CancellationToken.None);
        await observer.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
        fixture.Lifetime.NotifyStarted();
        Assert.Empty(fixture.Logger.Events);
        Assert.Equal(0, fixture.Scopes.Created);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Expiry_during_resolution_or_observation_discards_result(bool duringObservation)
    {
        var fixture = new Fixture();
        if (duringObservation)
            fixture.Diagnostic.OnObserve = () => fixture.Time.Current = Now.AddHours(1);
        else
            fixture.Scopes.OnResolve = () => fixture.Time.Current = Now.AddHours(1);
        await fixture.RunAsync();
        Assert.Equal("Expired", fixture.SingleStatus());
        Assert.Equal(duringObservation ? 1 : 0, fixture.Diagnostic.Calls);
        Assert.Null(fixture.Logger.Events.Single().Values["ServerMatches"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Resolution_and_observation_failures_do_not_escape_or_log_exception_details(bool resolution)
    {
        var fixture = new Fixture();
        if (resolution)
            fixture.Scopes.OnResolve = () => throw new InvalidOperationException("credential-sentinel");
        else
            fixture.Diagnostic.OnObserve = () => throw new InvalidOperationException("credential-sentinel");
        await fixture.RunAsync();
        Assert.Equal("Failed", fixture.SingleStatus());
        var entry = fixture.Logger.Events.Single();
        Assert.Null(entry.Exception);
        Assert.DoesNotContain("credential-sentinel", entry.Text);
        Assert.DoesNotContain("Exception", entry.Text);
    }

    [Fact]
    public async Task Logger_failure_does_not_fault_the_host_owned_task_or_retry()
    {
        var fixture = new Fixture();
        fixture.Logger.Throw = true;
        await fixture.RunAsync();
        Assert.Equal(1, fixture.Logger.Attempts);
        Assert.Equal(1, fixture.Diagnostic.Calls);
        Assert.Empty(fixture.Logger.Events);
    }

    [Fact]
    public async Task Disposal_failure_produces_one_fixed_failure_event_instead_of_success_then_failure()
    {
        var fixture = new Fixture();
        fixture.Scopes.OnDispose = () => throw new InvalidOperationException("disposal-credential-sentinel");
        await fixture.RunAsync();
        Assert.Equal("Failed", fixture.SingleStatus());
        var entry = Assert.Single(fixture.Logger.Events);
        Assert.Null(entry.Exception);
        Assert.Null(entry.Values["ServerMatches"]);
        Assert.DoesNotContain("disposal-credential-sentinel", entry.Text);
    }

    [Theory]
    [InlineData(RuntimeDatabaseBindingStatus.Mismatch)]
    [InlineData(RuntimeDatabaseBindingStatus.Unavailable)]
    public async Task Nonmatching_metadata_does_not_fault_host_or_trigger_retry(RuntimeDatabaseBindingStatus status)
    {
        var fixture = new Fixture();
        fixture.Diagnostic.Result = new(status, ProviderMatches: false);
        await fixture.RunAsync();
        Assert.Equal(status.ToString(), fixture.SingleStatus());
        Assert.Equal(1, fixture.Diagnostic.Calls);
    }

    [Fact]
    public async Task Application_started_callback_remains_nonblocking_when_metadata_is_slow()
    {
        var fixture = new Fixture();
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Diagnostic.OnObserve = () =>
        {
            entered.TrySetResult();
            if (!release.Wait(TimeSpan.FromSeconds(5)))
                throw new TimeoutException("test controlled metadata wait");
        };
        using var observer = fixture.CreateObserver();
        await observer.StartAsync(CancellationToken.None);
        try
        {
            await Task.Run(fixture.Lifetime.NotifyStarted).WaitAsync(TimeSpan.FromSeconds(2));
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.Empty(fixture.Logger.Events);
            fixture.Lifetime.StopApplication();
        }
        finally
        {
            release.Set();
            await observer.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5));
        }
        Assert.Empty(fixture.Logger.Events);
        Assert.Equal(1, fixture.Scopes.Disposed);
    }

    private sealed class Fixture
    {
        public Dictionary<string, string?> Values { get; } = new()
        {
            [Prefix + "Enabled"] = "true",
            [Prefix + "ExpectedServerSha256"] = new('a', 64),
            [Prefix + "ExpectedDatabaseSha256"] = new('b', 64),
            [Prefix + "ExpiresUtc"] = Now.AddHours(1).ToString("O")
        };
        public FakeLifetime Lifetime { get; } = new();
        public FakeTime Time { get; } = new();
        public FakeDiagnostic Diagnostic { get; } = new();
        public CaptureLogger Logger { get; } = new();
        public CountingScopes Scopes { get; }

        public Fixture() => Scopes = new(Diagnostic);

        public D01RuntimeDiagnosticObserver CreateObserver() => new(
            new ConfigurationBuilder().AddInMemoryCollection(Values).Build(), Scopes, Lifetime, Time, Logger);

        public async Task RunAsync()
        {
            using var observer = CreateObserver();
            await observer.StartAsync(CancellationToken.None);
            Lifetime.NotifyStarted();
            await observer.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5));
        }

        public string SingleStatus() => (string)Assert.Single(Logger.Events).Values["Status"]!;
    }

    private sealed class FakeTime : TimeProvider
    {
        public DateTimeOffset Current { get; set; } = Now;
        public override DateTimeOffset GetUtcNow() => Current;
    }

    private sealed class FakeLifetime : IHostApplicationLifetime
    {
        private readonly CancellationTokenSource _started = new();
        private readonly CancellationTokenSource _stopping = new();
        public CancellationToken ApplicationStarted => _started.Token;
        public CancellationToken ApplicationStopping => _stopping.Token;
        public CancellationToken ApplicationStopped => CancellationToken.None;
        public void NotifyStarted() => _started.Cancel();
        public void StopApplication() => _stopping.Cancel();
    }

    private sealed class FakeDiagnostic : IRuntimeDatabaseBindingDiagnostic
    {
        public int Calls { get; private set; }
        public Action? OnObserve { get; set; }
        public RuntimeDatabaseBindingResult Result { get; set; } = new(RuntimeDatabaseBindingStatus.Matched, true, true, true, true);
        public RuntimeDatabaseBindingResult Observe(RuntimeDatabaseBindingExpectation expectation)
        {
            Calls++;
            OnObserve?.Invoke();
            return Result;
        }
    }

    private sealed class CountingScopes(FakeDiagnostic diagnostic) : IServiceScopeFactory
    {
        public int Created { get; private set; }
        public int Disposed { get; private set; }
        public Action? OnResolve { get; set; }
        public Action? OnDispose { get; set; }
        public IServiceScope CreateScope()
        {
            Created++;
            return new Scope(this, diagnostic);
        }

        private sealed class Scope(CountingScopes owner, FakeDiagnostic diagnostic) : IServiceScope, IServiceProvider
        {
            public IServiceProvider ServiceProvider => this;
            public object? GetService(Type type)
            {
                owner.OnResolve?.Invoke();
                return type == typeof(IRuntimeDatabaseBindingDiagnostic) ? diagnostic : null;
            }
            public void Dispose()
            {
                owner.Disposed++;
                owner.OnDispose?.Invoke();
            }
        }
    }

    private sealed record LogEntry(EventId Id, string Text, IReadOnlyDictionary<string, object?> Values, Exception? Exception);

    private sealed class CaptureLogger : ILogger<D01RuntimeDiagnosticObserver>
    {
        public ConcurrentQueue<LogEntry> Events { get; } = new();
        public bool Throw { get; set; }
        public int Attempts { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Attempts++;
            if (Throw)
                throw new InvalidOperationException("logger-sentinel");
            Events.Enqueue(new(id, formatter(state, exception),
                ((IEnumerable<KeyValuePair<string, object?>>)state!).ToDictionary(pair => pair.Key, pair => pair.Value), exception));
        }
    }
}
