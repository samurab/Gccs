using System.Collections.Concurrent;
using System.Text.Json;
using Gccs.Api.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ApiCorrelationInputTests
{
    public static IEnumerable<object?[]> ReplacedInputs => new object?[][]
    {
        [null], [""], ["   "], ["Bearer synthetic-header-canary"],
        ["token=example-synthetic-header-canary"], ["synthetic@example.invalid"],
        ["{\"token\":\"synthetic-header-canary\"}"], ["synthetic/canary"],
        ["synthetic,canary"], ["synthetic\r\ncanary"], ["synthetic\tcanary"],
        ["non-ascii-\u00e9-canary"], [new string('a', 121)]
    };

    [Theory]
    [MemberData(nameof(ReplacedInputs))]
    public void Invalid_or_absent_value_gets_a_stable_server_id(string? input)
    {
        var context = new DefaultHttpContext();
        if (input is not null) context.Request.Headers[ApiCorrelation.HeaderName] = input;
        var id = ApiCorrelation.Ensure(context);
        Assert.True(Guid.TryParseExact(id, "N", out _));
        Assert.Equal(id, context.TraceIdentifier);
        Assert.Equal(id, ApiCorrelation.Get(context));
        Assert.Equal(id, new HttpAuditRequestMetadata(new HttpContextAccessor { HttpContext = context }).CorrelationId);
        Assert.All(context.Items.Values, value => Assert.Equal(id, value));
    }

    [Theory]
    [InlineData("tc-3-1-correlation", "tc-3-1-correlation")]
    [InlineData("  Legacy_ID.test:abc-123  ", "Legacy_ID.test:abc-123")]
    [InlineData("0123456789abcdef0123456789abcdef", "0123456789abcdef0123456789abcdef")]
    [InlineData("01234567-89ab-cdef-0123-456789abcdef", "01234567-89ab-cdef-0123-456789abcdef")]
    public void Existing_opaque_ids_and_trimming_are_preserved(string input, string expected)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[ApiCorrelation.HeaderName] = input;
        Assert.Equal(expected, ApiCorrelation.Ensure(context));
        Assert.Equal(expected, context.TraceIdentifier);
    }

    [Fact]
    public void Maximum_length_id_is_preserved_after_trimming()
    {
        var context = new DefaultHttpContext();
        var id = new string('a', 120);
        context.Request.Headers[ApiCorrelation.HeaderName] = "  " + id + "  ";
        Assert.Equal(id, ApiCorrelation.Ensure(context));
    }

    [Fact]
    public void First_header_value_policy_is_preserved()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[ApiCorrelation.HeaderName] = new StringValues(["first-valid-id", "token=example-synthetic-header-canary"]);
        Assert.Equal("first-valid-id", ApiCorrelation.Ensure(context));
    }

    [Fact]
    public void Syntax_validation_does_not_claim_to_classify_valid_looking_sensitive_ids()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[ApiCorrelation.HeaderName] = "synthetic-valid-looking-secret";
        Assert.Equal("synthetic-valid-looking-secret", ApiCorrelation.Ensure(context));
    }

    [Fact]
    public void Cached_canonical_id_restores_trace_identifier()
    {
        var context = new DefaultHttpContext();
        var id = ApiCorrelation.Ensure(context);
        context.TraceIdentifier = "token=example-synthetic-trace-canary";
        Assert.Equal(id, ApiCorrelation.Get(context));
        Assert.Equal(id, context.TraceIdentifier);
    }

    [Theory]
    [InlineData("token=example-synthetic-cache-canary")]
    [InlineData("non-ascii-\u00e9-canary")]
    public void Malformed_cached_value_is_not_trusted(string input)
    {
        var context = new DefaultHttpContext();
        ApiCorrelation.Ensure(context);
        context.Items[Assert.Single(context.Items).Key] = input;
        var id = ApiCorrelation.Get(context);
        Assert.True(Guid.TryParseExact(id, "N", out _));
        Assert.Equal(id, context.TraceIdentifier);
        Assert.Equal(id, Assert.Single(context.Items).Value);
    }

    [Fact]
    public void Problem_customizer_establishes_correlation_before_capturing_trace()
    {
        var context = new DefaultHttpContext { TraceIdentifier = "token=example-synthetic-trace-canary" };
        context.Request.Headers[ApiCorrelation.HeaderName] = "token=example-synthetic-header-canary";
        var problem = new ProblemDetails { Status = 400 };
        ApiProblemDetails.Customize(new ProblemDetailsContext { HttpContext = context, ProblemDetails = problem });
        var id = ApiCorrelation.Get(context);
        Assert.True(Guid.TryParseExact(id, "N", out _));
        Assert.Equal(id, problem.Extensions["correlationId"]);
        Assert.Equal(id, problem.Extensions["traceId"]);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(409)]
    [InlineData(500)]
    public async Task Rejected_header_is_not_copied_to_response_failure_log_or_audit_adapter(int status)
    {
        var logs = new CapturingLoggerProvider();
        string? auditId = null;
        string? expectedProblemTraceId = null;
        using var host = await new HostBuilder().ConfigureWebHost(web => web.UseTestServer()
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddProblemDetails(options => options.CustomizeProblemDetails = ApiProblemDetails.Customize);
            })
            .ConfigureLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(logs))
            .Configure(app =>
            {
                app.UseGccsCorrelationIds();
                app.UseRouting();
                app.UseGccsApiFailureLogging();
                app.UseEndpoints(endpoints => endpoints.MapGet("/api/correlation-check", async context =>
                {
                    auditId = new HttpAuditRequestMetadata(new HttpContextAccessor { HttpContext = context }).CorrelationId;
                    // The framework preserves its distributed trace separately from request correlation.
                    expectedProblemTraceId = System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier;
                    await ApiProblemDetails.Create(context, "Synthetic failure", "Synthetic detail", status, "synthetic_failure")
                        .ExecuteAsync(context);
                }));
            })).StartAsync();
        using var client = host.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/correlation-check");
        request.Headers.Add(ApiCorrelation.HeaderName, "token=example-synthetic-header-canary");
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(status, (int)response.StatusCode);
        var id = Assert.Single(response.Headers.GetValues(ApiCorrelation.HeaderName));
        Assert.True(Guid.TryParseExact(id, "N", out _));
        Assert.Equal(id, auditId);
        using var payload = JsonDocument.Parse(body);
        Assert.Equal(id, payload.RootElement.GetProperty("correlationId").GetString());
        Assert.Equal(expectedProblemTraceId, payload.RootElement.GetProperty("traceId").GetString());
        Assert.DoesNotContain("canary", body, StringComparison.Ordinal);
        var failure = Assert.Single(logs.Entries, entry => entry.Category == "Gccs.Api.Security.ApiFailureLogging");
        Assert.Equal(status >= 500 ? LogLevel.Error : LogLevel.Warning, failure.Level);
        Assert.Null(failure.Exception);
        Assert.Equal(id, failure.Properties["CorrelationId"]);
        Assert.Equal(id, failure.Properties["TraceId"]);
        Assert.All(logs.Entries, entry =>
        {
            Assert.DoesNotContain("canary", entry.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("canary", entry.Exception?.ToString() ?? string.Empty, StringComparison.Ordinal);
            Assert.All(entry.Properties.Values, value =>
                Assert.DoesNotContain("canary", value?.ToString() ?? string.Empty, StringComparison.Ordinal));
        });
    }

    private sealed record LogEntry(string Category, LogLevel Level, string Message, Exception? Exception,
        Dictionary<string, object?> Properties);
    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public ConcurrentQueue<LogEntry> Entries { get; } = new();
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(this, categoryName);
        public void Dispose() { }
        private sealed class CapturingLogger(CapturingLoggerProvider provider, string category) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel level) => true;
            public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                var properties = state is IEnumerable<KeyValuePair<string, object?>> values
                    ? values.ToDictionary(pair => pair.Key, pair => pair.Value)
                    : new Dictionary<string, object?> { ["State"] = state };
                provider.Entries.Enqueue(new LogEntry(category, level, formatter(state, exception), exception, properties));
            }
        }
    }
}
