using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Gccs.Api.LocalDevelopment;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ExistingDevelopmentApiTests
{
    private const string Healthy = "{\"service\":\"gccs-api\",\"status\":\"ok\"}";

    [Fact]
    public async Task Healthy_local_api_is_reused_at_every_configured_url()
    {
        await WithServerAsync(context => context.Response.WriteAsync(Healthy), async url =>
        {
            Assert.Equal(url, await FindAsync(url));
            Assert.Equal($"{url}; {url}", await FindAsync($"{url}; {url}"));
        });
    }

    [Theory]
    [InlineData(200, "{\"service\":\"another-app\",\"status\":\"ok\"}")]
    [InlineData(200, "{\"service\":\"gccs-api\",\"status\":\"degraded\"}")]
    [InlineData(200, "{\"service\":\"gccs-api\"}")]
    [InlineData(200, "{\"service\":123,\"status\":\"ok\"}")]
    [InlineData(200, "null")]
    [InlineData(200, "not JSON")]
    [InlineData(503, Healthy)]
    public async Task Unhealthy_or_unrelated_listener_does_not_suppress_startup(int statusCode, string body)
    {
        await WithServerAsync(context =>
        {
            context.Response.StatusCode = statusCode;
            return context.Response.WriteAsync(body);
        }, async url => Assert.Null(await FindAsync(url)));
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task Non_development_environment_never_reuses_an_existing_api(string environment)
    {
        await WithServerAsync(context => context.Response.WriteAsync(Healthy), async url =>
            Assert.Null(await FindAsync(url, environment)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("http://0.0.0.0:5062")]
    [InlineData("http://example.com:5062")]
    [InlineData("https://localhost:5062")]
    [InlineData("http://localhost:0")]
    [InlineData("http://localhost:5062/path")]
    [InlineData("http://user:password@localhost:5062")]
    public async Task Unsupported_bindings_do_not_suppress_startup(string? url)
    {
        Assert.Null(await FindAsync(url));
    }

    [Fact]
    public async Task Kestrel_endpoint_override_does_not_reuse_a_different_url()
    {
        await WithServerAsync(context => context.Response.WriteAsync(Healthy), async url =>
            Assert.Null(await FindAsync(url, endpointOverride: true)));
    }

    [Fact]
    public async Task Missing_listener_on_any_binding_preserves_normal_startup()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var missingUrl = $"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}";
        listener.Stop();
        Assert.Null(await FindAsync(missingUrl));
        await WithServerAsync(context => context.Response.WriteAsync(Healthy), async url =>
            Assert.Null(await FindAsync($"{url};{missingUrl}")));
    }

    [Fact]
    public async Task Redirect_is_not_accepted_as_a_healthy_local_api()
    {
        var requests = 0;
        await WithServerAsync(context =>
        {
            requests++;
            if (context.Request.Path == "/health") context.Response.Redirect("/other");
            return context.Response.WriteAsync(Healthy);
        }, async url => Assert.Null(await FindAsync(url)));
        Assert.Equal(1, requests);
    }

    [Fact]
    public async Task Hung_listener_has_a_bounded_probe_and_preserves_normal_startup()
    {
        await WithServerAsync(async context =>
        {
            try { await Task.Delay(Timeout.InfiniteTimeSpan, context.RequestAborted); }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { }
        }, async url =>
        {
            var timer = Stopwatch.StartNew();
            Assert.Null(await FindAsync(url));
            Assert.True(timer.Elapsed < TimeSpan.FromSeconds(8));
        });
    }

    private static Task<string?> FindAsync(string? url, string environment = "Development", bool endpointOverride = false)
    {
        var values = new Dictionary<string, string?> { ["urls"] = url };
        if (endpointOverride) values["Kestrel:Endpoints:Http:Url"] = "http://127.0.0.1:0";
        return ExistingDevelopmentApi.FindHealthyUrlAsync(
            new ConfigurationBuilder().AddInMemoryCollection(values).Build(), new TestHostEnvironment(environment));
    }

    private static async Task WithServerAsync(RequestDelegate handler, Func<string, Task> test)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        await using var app = builder.Build();
        app.Run(handler);
        await app.StartAsync();
        try { await test(app.Urls.Single()); }
        finally { await app.StopAsync(); }
    }
}
