using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Gccs.Api.Security;
using Gccs.Application.Compliance;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class HandledExceptionDiagnosticsTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData(true, 400)]
    [InlineData(false, 500)]
    public async Task Handled_api_failures_emit_safe_context_without_raw_framework_diagnostics(
        bool validationFailure, int expectedStatus)
    {
        var logs = new CapturingLoggerProvider();
        using var diagnostic = new DiagnosticListener("SyntheticHandledExceptionDiagnostics");
        var events = new DiagnosticEvents();
        using var subscription = diagnostic.Subscribe(events,
            name => name == "Microsoft.AspNetCore.Diagnostics.HandledException");
        var exception = CreateException(validationFailure);
        using var host = await CreateHost(logs, diagnostic, "/api/synthetic-diagnostics/{id}",
            _ => Task.FromException(exception));
        using var client = host.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/synthetic-diagnostics/item");
        request.Headers.Add(ApiCorrelation.HeaderName, "synthetic-diagnostic-correlation");

        using var response = await client.SendAsync(request);

        Assert.Equal(expectedStatus, (int)response.StatusCode);
        Assert.Empty(events.Names);
        AssertSafeLogs(logs.Entries);
        var failure = Assert.Single(logs.Entries, entry => entry.Category == "Gccs.Api.Security.ApiFailureLogging");
        Assert.Equal("/api/synthetic-diagnostics/{id}", failure.Properties["Path"]);
        Assert.Equal(exception.GetType().Name, failure.Properties["ExceptionType"]);
        Assert.Equal(expectedStatus, failure.Properties["StatusCode"]);
        Assert.Equal("synthetic-diagnostic-correlation", failure.Properties["CorrelationId"]);
        Assert.Equal(expectedStatus >= 500 ? LogLevel.Error : LogLevel.Warning, failure.Level);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("canary", body, StringComparison.Ordinal);
        Assert.Contains("synthetic-diagnostic-correlation", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Non_api_exceptions_are_still_rethrown_and_reported()
    {
        var logs = new CapturingLoggerProvider();
        using var diagnostic = new DiagnosticListener("SyntheticNonApiDiagnostics");
        var exception = CreateException(validationFailure: false);
        using var host = await CreateHost(logs, diagnostic, "/synthetic-non-api",
            _ => Task.FromException(exception));
        using var client = host.GetTestClient();

        await Assert.ThrowsAnyAsync<Exception>(() => client.GetAsync("/synthetic-non-api"));

        Assert.Contains(logs.Entries, entry => ReferenceEquals(entry.Exception, exception));
        Assert.DoesNotContain(logs.Entries, entry => entry.Category == "Gccs.Api.Security.ApiFailureLogging");
    }

    [Fact]
    public async Task Started_response_exceptions_are_still_rethrown_and_reported()
    {
        var logs = new CapturingLoggerProvider();
        using var diagnostic = new DiagnosticListener("SyntheticStartedResponseDiagnostics");
        var exception = CreateException(validationFailure: false);
        var started = false;
        using var host = await CreateHost(logs, diagnostic, "/api/synthetic-started", async context =>
        {
            await context.Response.WriteAsync("synthetic response");
            started = context.Response.HasStarted;
            throw exception;
        });
        using var client = host.GetTestClient();

        await Assert.ThrowsAnyAsync<Exception>(() => client.GetAsync("/api/synthetic-started"));

        Assert.True(started);
        Assert.Contains(logs.Entries, entry => ReferenceEquals(entry.Exception, exception));
    }

    [Fact]
    public async Task Overview_failure_keeps_safe_type_and_correlation_without_attached_exception()
    {
        var logs = new CapturingLoggerProvider();
        var exception = CreateException(validationFailure: false);
        await using var host = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.UseSetting("APPLICATIONINSIGHTS_CONNECTION_STRING", string.Empty);
            builder.ConfigureLogging(logging => logging.AddProvider(logs));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IComplianceOverviewRepository>();
                services.AddScoped<IComplianceOverviewRepository>(_ => new ThrowingOverviewRepository(exception));
            });
        });
        using var client = host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/compliance/overview");
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", Guid.NewGuid().ToString());
        request.Headers.Add("X-Gccs-Dev-User", Guid.NewGuid().ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", "ViewObligations");
        request.Headers.Add(ApiCorrelation.HeaderName, "synthetic-overview-correlation");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        AssertSafeLogs(logs.Entries.Where(entry => entry.Category is "Program" or "Gccs.Api.Security.ApiFailureLogging"));
        var failure = Assert.Single(logs.Entries, entry => entry.Category == "Program" &&
            entry.Message.StartsWith("Compliance overview failed", StringComparison.Ordinal));
        Assert.Equal(nameof(InvalidOperationException), failure.Properties["ExceptionType"]);
        Assert.Equal("synthetic-overview-correlation", failure.Properties["CorrelationId"]);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("compliance_overview_unavailable", body.RootElement.GetProperty("errorCode").GetString());
        Assert.Equal("synthetic-overview-correlation", body.RootElement.GetProperty("correlationId").GetString());
        Assert.DoesNotContain("canary", body.RootElement.ToString(), StringComparison.Ordinal);
    }

    private static Task<IHost> CreateHost(CapturingLoggerProvider logs, DiagnosticListener diagnostic,
        string route, RequestDelegate handler) => new HostBuilder().ConfigureWebHost(web => web
        .UseTestServer()
        .ConfigureServices(services =>
        {
            services.AddRouting();
            services.AddSingleton(diagnostic);
        })
        .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Information).AddProvider(logs))
        .Configure(app =>
        {
            app.UseRouting();
            app.UseGccsApiFailureLogging();
            app.UseGccsApiProblemDetails();
            app.UseEndpoints(endpoints => endpoints.MapGet(route, handler));
        })).StartAsync();

    private static Exception CreateException(bool validationFailure)
    {
        var exception = validationFailure
            ? (Exception)new MissingTenantContextException("synthetic-exception-canary")
            : new InvalidOperationException("synthetic-exception-canary", new Exception("synthetic-inner-canary"));
        exception.Data["token"] = "synthetic-data-canary";
        return exception;
    }

    private static void AssertSafeLogs(IEnumerable<LogEntry> entries) => Assert.All(entries, entry =>
    {
        Assert.Null(entry.Exception);
        Assert.DoesNotContain("canary", entry.Message, StringComparison.Ordinal);
        Assert.All(entry.Properties.Values, value =>
            Assert.DoesNotContain("canary", value?.ToString() ?? string.Empty, StringComparison.Ordinal));
    });

    private sealed class ThrowingOverviewRepository(Exception exception) : IComplianceOverviewRepository
    {
        public Task<ComplianceOverviewDto> GetCurrentTenantOverviewAsync(CancellationToken cancellationToken = default) =>
            Task.FromException<ComplianceOverviewDto>(exception);
    }

    private sealed class DiagnosticEvents : IObserver<KeyValuePair<string, object?>>
    {
        public List<string> Names { get; } = [];
        public void OnNext(KeyValuePair<string, object?> value) => Names.Add(value.Key);
        public void OnCompleted() { }
        public void OnError(Exception error) { }
    }

    private sealed record LogEntry(string Category, LogLevel Level, string Message, Exception? Exception,
        Dictionary<string, object?> Properties);

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public ConcurrentQueue<LogEntry> Entries { get; } = new();
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, this);
        public void Dispose() { }

        private sealed class CapturingLogger(string category, CapturingLoggerProvider provider) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter) => provider.Entries.Enqueue(new LogEntry(category, logLevel,
                    formatter(state, exception), exception, state is IEnumerable<KeyValuePair<string, object?>> properties
                        ? properties.ToDictionary(pair => pair.Key, pair => pair.Value)
                        : new Dictionary<string, object?>()));
        }
    }
}
