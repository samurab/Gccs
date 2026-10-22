using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text;
using Gccs.Api.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Trace;
using Xunit;
using Xunit.Abstractions;

namespace Gccs.Api.Tests;

[CollectionDefinition("Telemetry collection", DisableParallelization = true)]
public sealed class TelemetryCollectionDefinition;

[Collection("Telemetry collection")]
public sealed class TelemetryInputDisclosureTests(ITestOutputHelper output)
{
    [Fact]
    public void Privacy_configuration_overrides_opt_outs_and_preserves_prior_enrichment()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ASPNETCORE_DISABLE_URL_QUERY_REDACTION"] = "true",
            ["HTTPCLIENT_DISABLE_URL_QUERY_REDACTION"] = "true"
        }).Build();
        var services = new ServiceCollection();
        services.Configure<AspNetCoreTraceInstrumentationOptions>(options =>
            options.EnrichWithHttpResponse = (activity, _) => activity.SetTag("safe-prior-marker", "retained"));
        services.AddGccsTelemetryPrivacy(configuration);
        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<AspNetCoreTraceInstrumentationOptions>>().Value;
        using var activity = new Activity("synthetic");
        options.EnrichWithHttpResponse!(activity, new DefaultHttpContext().Response);
        Assert.Equal("retained", activity.GetTagItem("safe-prior-marker"));
        Assert.Equal("unmatched", activity.GetTagItem("url.path"));
        Assert.Equal(bool.FalseString, configuration["ASPNETCORE_DISABLE_URL_QUERY_REDACTION"]);
        Assert.Equal(bool.FalseString, configuration["HTTPCLIENT_DISABLE_URL_QUERY_REDACTION"]);
    }

    [Theory]
    [InlineData(200)]
    [InlineData(500)]
    [InlineData(404)]
    public async Task Incoming_collection_keeps_route_status_and_trace_without_raw_input(int status)
    {
        var exported = new MemoryExporter();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        using var host = await new HostBuilder().ConfigureWebHost(web => web.UseTestServer()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IConfiguration>(configuration);
                services.AddRouting();
                services.AddProblemDetails(options => options.CustomizeProblemDetails = ApiProblemDetails.Customize);
                services.AddGccsTelemetryPrivacy(configuration);
                services.AddOpenTelemetry().WithTracing(builder => builder
                    .SetResourceBuilder(ResourceBuilder.CreateEmpty()).SetSampler(new AlwaysOnSampler())
                    .AddAspNetCoreInstrumentation()
                    .AddProcessor(new SimpleActivityExportProcessor(exported)));
            })
            .Configure(app =>
            {
                app.UseGccsCorrelationIds();
                app.UseGccsApiProblemDetails();
                app.UseRouting();
                app.UseEndpoints(endpoints => endpoints.MapGet("/api/telemetry-check/{value}", async context =>
                {
                    if (status == 500) throw new InvalidOperationException("synthetic-exception-canary");
                    context.Response.StatusCode = status;
                    await context.Response.WriteAsync("Synthetic response");
                }));
            })).StartAsync();
        using var client = host.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get,
            status == 404 ? "/synthetic-unmatched-canary?token=synthetic-query-canary"
                : "/api/telemetry-check/synthetic-path-canary?token=synthetic-query-canary");
        request.Headers.Add("Authorization", "Bearer synthetic-header-canary");
        request.Headers.Add("User-Agent", "synthetic-agent-canary");
        request.Headers.Add(ApiCorrelation.HeaderName, "token=synthetic-correlation-canary");
        using var response = await client.SendAsync(request);
        Assert.Equal(status, (int)response.StatusCode);
        var provider = host.Services.GetRequiredService<TracerProvider>();
        await exported.WaitForExportAsync(ActivityKind.Server);
        Assert.True(provider.ForceFlush());
        var activity = Assert.Single(exported.Items, item => item.Kind == ActivityKind.Server);
        output.WriteLine(System.Text.Json.JsonSerializer.Serialize(activity));
        if (status == 200) Assert.Equal("/api/telemetry-check/{value}", activity.Tags["http.route"]);
        Assert.Equal(status == 404 ? "unmatched" : "/api/telemetry-check/{value}", activity.Tags["url.path"]);
        Assert.Equal("GET", activity.Tags["http.request.method"]);
        Assert.Equal(status.ToString(), activity.Tags["http.response.status_code"]);
        Assert.NotEqual(default, activity.TraceId);
        Assert.DoesNotContain("canary", activity.AllText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Outgoing_collection_keeps_dependency_without_synthetic_query_key()
    {
        var exported = new MemoryExporter();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddGccsTelemetryPrivacy(configuration);
        services.AddOpenTelemetry().WithTracing(builder => builder
            .SetResourceBuilder(ResourceBuilder.CreateEmpty()).SetSampler(new AlwaysOnSampler())
            .AddHttpClientInstrumentation().AddProcessor(new SimpleActivityExportProcessor(exported)));
        using var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetRequiredService<TracerProvider>();
        Assert.Equal(bool.FalseString, configuration["ASPNETCORE_DISABLE_URL_QUERY_REDACTION"]);
        Assert.Equal(bool.FalseString, configuration["HTTPCLIENT_DISABLE_URL_QUERY_REDACTION"]);
        using var handler = new SocketsHttpHandler
        {
            UseProxy = false,
            ConnectCallback = (_, _) => ValueTask.FromResult<Stream>(new SyntheticResponseStream())
        };
        using var client = new HttpClient(handler);
        using var response = await client.GetAsync(
            "http://synthetic.invalid/entity-information/v3/entities?ueiSAM=synthetic-uei-canary&api_key=synthetic-key-canary");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await exported.WaitForExportAsync(ActivityKind.Client);
        Assert.True(provider.ForceFlush());
        var activity = Assert.Single(exported.Items, item => item.Kind == ActivityKind.Client);
        output.WriteLine(System.Text.Json.JsonSerializer.Serialize(activity));
        Assert.Equal("GET", activity.Tags["http.request.method"]);
        Assert.Equal("200", activity.Tags["http.response.status_code"]);
        Assert.Equal("synthetic.invalid", activity.Tags["server.address"]);
        Assert.Contains("/entity-information/v3/entities", activity.Tags["url.full"], StringComparison.Ordinal);
        Assert.NotEqual(default, activity.TraceId);
        Assert.DoesNotContain("canary", activity.AllText, StringComparison.Ordinal);
    }

    private sealed record CapturedActivity(ActivityKind Kind, ActivityTraceId TraceId,
        Dictionary<string, string?> Tags, string AllText);

    private sealed class MemoryExporter : BaseExporter<Activity>
    {
        public ConcurrentQueue<CapturedActivity> Items { get; } = new();
        private readonly ConcurrentDictionary<ActivityKind, TaskCompletionSource> completions = new();
        public Task WaitForExportAsync(ActivityKind kind) => completions
            .GetOrAdd(kind, _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously))
            .Task.WaitAsync(TimeSpan.FromSeconds(5));
        public override ExportResult Export(in Batch<Activity> batch)
        {
            foreach (var activity in batch)
            {
                var tags = activity.TagObjects.ToDictionary(pair => pair.Key, pair => pair.Value?.ToString());
                var text = activity.DisplayName + string.Join(";", tags.Select(pair => pair.Key + "=" + pair.Value))
                    + string.Join(";", activity.Events.Select(item => item.Name + string.Join(";", item.Tags)));
                Items.Enqueue(new CapturedActivity(activity.Kind, activity.TraceId, tags, text));
                completions.GetOrAdd(activity.Kind,
                    _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)).TrySetResult();
            }
            return ExportResult.Success;
        }
    }

    // The real HTTP diagnostics run over a synthetic transport; no socket or DNS is used.
    private sealed class SyntheticResponseStream : Stream
    {
        private readonly MemoryStream response = new(Encoding.ASCII.GetBytes(
            "HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\n{}"));
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => response.Read(buffer, offset, count);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => response.ReadAsync(buffer, cancellationToken);
        public override void Write(byte[] buffer, int offset, int count) { }
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { if (disposing) response.Dispose(); base.Dispose(disposing); }
    }
}
