using Gccs.Api.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ApiFailureLoggingTests
{
    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(405)]
    [InlineData(409)]
    [InlineData(423)]
    [InlineData(428)]
    [InlineData(429)]
    [InlineData(500)]
    [InlineData(503)]
    public async Task Failures_log_route_templates_without_request_values(int statusCode)
    {
        var endpoint = new RouteEndpoint(_ => Task.CompletedTask,
            RoutePatternFactory.Parse("/api/invitations/{token}"), 0,
            EndpointMetadataCollection.Empty, "synthetic-display-canary");

        await AssertFailureLog(statusCode, endpoint, "/api/invitations/{token}");
    }

    [Theory]
    [InlineData(404)]
    [InlineData(500)]
    public async Task Missing_route_metadata_uses_fixed_fallback(int statusCode)
    {
        await AssertFailureLog(statusCode, null, "unmatched");
        await AssertFailureLog(statusCode,
            new Endpoint(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, "synthetic-display-canary"),
            "unmatched");
    }

    [Theory]
    [InlineData(true, 400)]
    [InlineData(false, 500)]
    public async Task Handled_exceptions_retain_the_original_route_template(bool validationFailure, int statusCode)
    {
        var logs = new CapturingLoggerProvider();
        using var host = await new HostBuilder().ConfigureWebHost(web => web
            .UseTestServer()
            .ConfigureServices(services => services.AddRouting())
            .ConfigureLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(logs))
            .Configure(app =>
            {
                app.UseRouting();
                app.UseGccsApiFailureLogging();
                app.UseGccsApiProblemDetails();
                app.UseEndpoints(endpoints =>
                {
                    RequestDelegate handler = _ => Task.FromException(validationFailure
                        ? new MissingTenantContextException("synthetic-exception-canary")
                        : new InvalidOperationException("synthetic-exception-canary"));
                    endpoints.MapGet("/api/invitations/{token}", handler);
                });
            })).StartAsync();
        using var client = host.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get,
            "/api/invitations/synthetic-path-canary?token=example-synthetic-query-canary");
        request.Headers.Add(ApiCorrelation.HeaderName, "synthetic-composed-correlation");

        using var response = await client.SendAsync(request);

        Assert.Equal(statusCode, (int)response.StatusCode);
        var log = Assert.Single(logs.Entries);
        Assert.Equal("/api/invitations/{token}", log.Properties["Path"]);
        Assert.Equal("synthetic-composed-correlation", log.Properties["CorrelationId"]);
        Assert.Equal(validationFailure ? nameof(MissingTenantContextException) : nameof(InvalidOperationException),
            log.Properties["ExceptionType"]);
        Assert.Null(log.Exception);
        Assert.DoesNotContain("canary", log.Message, StringComparison.Ordinal);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("canary", body, StringComparison.Ordinal);
        Assert.Contains("synthetic-composed-correlation", body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/api/synthetic-path-canary", 200)]
    [InlineData("/api/synthetic-path-canary", 302)]
    [InlineData("/health/synthetic-path-canary", 500)]
    [InlineData("/api-other/synthetic-path-canary", 404)]
    public async Task Successful_and_non_api_requests_do_not_emit_failure_events(string path, int statusCode)
    {
        var logs = new CapturingLoggerProvider();
        using var services = CreateServices(logs);
        var context = CreateContext(services);
        context.Request.Path = path;

        await CreatePipeline(services, statusCode)(context);

        Assert.Empty(logs.Entries);
        Assert.Equal(statusCode, context.Response.StatusCode);
        Assert.True((bool)context.Items["completed"]!);
    }

    private static async Task AssertFailureLog(int statusCode, Endpoint? endpoint, string expectedPath)
    {
        var logs = new CapturingLoggerProvider();
        using var services = CreateServices(logs);
        var context = CreateContext(services);
        context.SetEndpoint(endpoint);

        await CreatePipeline(services, statusCode)(context);

        var log = Assert.Single(logs.Entries);
        Assert.Equal("Gccs.Api.Security.ApiFailureLogging", log.Category);
        Assert.Equal(statusCode >= 500 ? LogLevel.Error : LogLevel.Warning, log.Level);
        Assert.Null(log.Exception);
        Assert.Equal(statusCode, log.Properties["StatusCode"]);
        Assert.Equal("POST", log.Properties["Method"]);
        Assert.Equal(expectedPath, log.Properties["Path"]);
        Assert.Equal("synthetic-correlation", log.Properties["CorrelationId"]);
        Assert.Equal("synthetic-correlation", log.Properties["TraceId"]);
        Assert.Equal("anonymous", log.Properties["UserId"]);
        Assert.Equal("none", log.Properties["TenantId"]);
        Assert.Equal("none", log.Properties["ExceptionType"]);
        Assert.Equal(new[] { "StatusCode", "Method", "Path", "CorrelationId", "TraceId", "UserId", "TenantId", "ExceptionType", "{OriginalFormat}" }.Order(),
            log.Properties.Keys.Order());
        Assert.DoesNotContain("canary", log.Message, StringComparison.Ordinal);
        Assert.All(log.Properties.Values, value =>
            Assert.DoesNotContain("canary", value?.ToString() ?? string.Empty, StringComparison.Ordinal));
        Assert.Equal(statusCode, context.Response.StatusCode);
        Assert.True((bool)context.Items["completed"]!);
        Assert.Same(endpoint, context.GetEndpoint());
        Assert.Equal("/api/invitations/synthetic-path-canary", context.Request.Path.Value);
        Assert.Equal("Bearer synthetic-header-canary", context.Request.Headers.Authorization.ToString());
    }

    private static ServiceProvider CreateServices(CapturingLoggerProvider logs) => new ServiceCollection()
        .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(logs))
        .BuildServiceProvider();

    private static DefaultHttpContext CreateContext(IServiceProvider services)
    {
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Method = "POST";
        context.Request.Path = "/api/invitations/synthetic-path-canary";
        context.Request.QueryString = new QueryString("?token=example-synthetic-query-canary");
        context.Request.Headers.Authorization = "Bearer synthetic-header-canary";
        context.Request.Headers[ApiCorrelation.HeaderName] = "synthetic-correlation";
        return context;
    }

    private static RequestDelegate CreatePipeline(IServiceProvider services, int statusCode)
    {
        var app = new ApplicationBuilder(services);
        app.UseGccsApiFailureLogging();
        app.Run(context =>
        {
            context.Response.StatusCode = statusCode;
            context.Items["completed"] = true;
            return Task.CompletedTask;
        });
        return app.Build();
    }

    private sealed record LogEntry(string Category, LogLevel Level, string Message, Exception? Exception,
        Dictionary<string, object?> Properties);

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public List<LogEntry> Entries { get; } = [];
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, this);
        public void Dispose() { }

        private sealed class CapturingLogger(string category, CapturingLoggerProvider provider) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (category == "Gccs.Api.Security.ApiFailureLogging")
                {
                    provider.Entries.Add(new LogEntry(category, logLevel, formatter(state, exception), exception,
                        ((IEnumerable<KeyValuePair<string, object?>>)state!).ToDictionary(pair => pair.Key, pair => pair.Value)));
                }
            }
        }
    }
}
