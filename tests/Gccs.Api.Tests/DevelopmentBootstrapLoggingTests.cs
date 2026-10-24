using Gccs.Api.LocalDevelopment;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class DevelopmentBootstrapLoggingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Scope_failure_logs_fixed_warning_and_type_without_attached_exception(bool canceled)
    {
        Exception failure = canceled ? new OperationCanceledException("synthetic-message-canary")
            : new InvalidOperationException("synthetic-message-canary", new Exception("synthetic-inner-canary"));
        failure.Data["synthetic-data-canary"] = "synthetic-value-canary";
        var scopes = new FailingScopeProvider(failure);
        var logs = new CapturingLogger();
        var bootstrapper = new DevelopmentTenantBootstrapper(scopes, Configuration(), Environment("Development"), logs);

        await bootstrapper.StartAsync(CancellationToken.None);

        Assert.Equal(1, scopes.Attempts);
        var entry = Assert.Single(logs.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Null(entry.Exception);
        Assert.Equal(failure.GetType().Name, entry.Properties["ExceptionType"]);
        Assert.Equal(new[] { "ExceptionType", "{OriginalFormat}" }.Order(), entry.Properties.Keys.Order());
        Assert.Equal("Development tenant bootstrap skipped because tenant creation failed. ExceptionType={ExceptionType}",
            entry.Properties["{OriginalFormat}"]);
        Assert.DoesNotContain("canary", entry.Message, StringComparison.Ordinal);
        Assert.All(entry.Properties.Values, value =>
            Assert.DoesNotContain("canary", value?.ToString() ?? string.Empty, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Production", true, true, false, 1)]
    [InlineData("Staging", true, true, false, 1)]
    [InlineData("Development", false, true, false, 0)]
    [InlineData("Development", true, false, false, 0)]
    [InlineData("Development", true, true, true, 1)]
    public async Task Existing_environment_and_configuration_gates_precede_scope_creation(
        string environment, bool seedEnabled, bool authEnabled, bool invalidIds, int expectedLogs)
    {
        var scopes = new FailingScopeProvider(new InvalidOperationException("synthetic-unused-canary"));
        var logs = new CapturingLogger();
        var bootstrapper = new DevelopmentTenantBootstrapper(scopes,
            Configuration(seedEnabled, authEnabled, invalidIds), Environment(environment), logs);

        await bootstrapper.StartAsync(CancellationToken.None);

        Assert.Equal(0, scopes.Attempts);
        Assert.Equal(expectedLogs, logs.Entries.Count);
        Assert.All(logs.Entries, entry =>
        {
            Assert.Null(entry.Exception);
            Assert.DoesNotContain("canary", entry.Message, StringComparison.Ordinal);
            Assert.Equal(invalidIds ? LogLevel.Warning : LogLevel.Information, entry.Level);
        });
    }

    private static IConfiguration Configuration(bool seed = true, bool auth = true, bool invalidIds = false) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["LocalDevelopment:SeedData:Enabled"] = seed.ToString(),
            ["Security:DevelopmentAuth:Enabled"] = auth.ToString(),
            ["Security:DevelopmentAuth:DefaultTenantId"] = invalidIds ? "synthetic-invalid-canary" : "11111111-1111-1111-1111-111111111111",
            ["Security:DevelopmentAuth:DefaultUserId"] = "22222222-2222-2222-2222-222222222222"
        }).Build();

    private static IWebHostEnvironment Environment(string name) => new SyntheticEnvironment { EnvironmentName = name };

    private sealed class SyntheticEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = string.Empty;
        public string ApplicationName { get; set; } = string.Empty;
        public string WebRootPath { get; set; } = string.Empty;
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class FailingScopeProvider(Exception failure) : IServiceProvider, IServiceScopeFactory
    {
        public int Attempts { get; private set; }
        public object? GetService(Type serviceType) => serviceType == typeof(IServiceScopeFactory)
            ? this : throw new InvalidOperationException("Unexpected service resolution.");
        public IServiceScope CreateScope() { Attempts++; throw failure; }
    }

    private sealed record Entry(LogLevel Level, string Message, Exception? Exception, Dictionary<string, object?> Properties);
    private sealed class CapturingLogger : ILogger<DevelopmentTenantBootstrapper>
    {
        public List<Entry> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Entries.Add(new Entry(logLevel, formatter(state, exception), exception,
                ((IEnumerable<KeyValuePair<string, object?>>)state!).ToDictionary(pair => pair.Key, pair => pair.Value)));
    }
}
