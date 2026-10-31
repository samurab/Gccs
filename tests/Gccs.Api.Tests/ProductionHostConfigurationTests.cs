using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ProductionHostConfigurationTests
{
    public static IEnumerable<object[]> InvalidOrigins()
    {
        foreach (var environment in new[] { "Production", "Staging" })
        foreach (var origin in new[]
        {
            "", "*", "https://*.example.invalid", "http://web.example.invalid",
            "https://web.example.invalid/path", "https://web.example.invalid/",
            "https://web.example.invalid?synthetic-config-canary",
            "https://web.example.invalid#synthetic-config-canary",
            "https://synthetic-config-canary@web.example.invalid",
            "https://@web.example.invalid", "https://web.example.invalid/a/..",
            "https://web.example.invalid\\a\\..",
            " https://web.example.invalid", "https://web.example.invalid ",
            "null", "https://localhost:3000"
        }) yield return [environment, origin];
    }

    public static IEnumerable<object[]> InvalidHosts()
    {
        foreach (var environment in new[] { "Production", "Staging" })
        foreach (var hosts in new[]
        {
            "", "*", " * ", "api.example.invalid;*", "*;api.example.invalid",
            "api.example.invalid;0.0.0.0", "api.example.invalid;[::]",
            ";api.example.invalid", "api.example.invalid;",
            "api.example.invalid; ;other.example.invalid"
        }) yield return [environment, hosts];
    }

    public static IEnumerable<object[]> ValidConfigurations()
    {
        foreach (var environment in new[] { "Production", "Staging" })
        foreach (var configuration in new[]
        {
            new[] { "https://web.example.invalid", "api.example.invalid", "https://api.example.invalid" },
            new[] { "https://web.example.invalid:8443", "api.example.invalid;other.example.invalid", "https://other.example.invalid" },
            new[] { "https://web.example.invalid", "*.example.invalid", "https://child.example.invalid" },
            new[] { "https://127.0.0.1:8443", "127.0.0.1", "https://127.0.0.1" },
            new[] { "https://[::1]:8443", "[::1]", "https://[::1]" },
            new[] { "https://web.example.invalid", "bücher.example.invalid", "https://xn--bcher-kva.example.invalid" },
            new[] { "https://bücher.example.invalid", "api.example.invalid", "https://api.example.invalid", "https://xn--bcher-kva.example.invalid" },
            new[] { "HTTPS://WEB.EXAMPLE.INVALID:8443", "api.example.invalid", "https://api.example.invalid", "https://web.example.invalid:8443" }
        }) yield return [environment, configuration[0], configuration[1], configuration[2],
            configuration.Length > 3 ? configuration[3] : configuration[0]];
    }

    [Theory]
    [MemberData(nameof(InvalidOrigins))]
    public void Non_development_startup_rejects_invalid_origins_without_echoing_values(string environment, string origin)
    {
        using var factory = Host(environment, origin, "api.example.invalid");
        var error = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
        Assert.Contains("CORS", error.Message);
        Assert.DoesNotContain("synthetic-config-canary", error.ToString());
    }

    [Theory]
    [MemberData(nameof(InvalidHosts))]
    public void Non_development_startup_rejects_unrestricted_or_empty_host_entries(string environment, string hosts)
    {
        using var factory = Host(environment, "https://web.example.invalid", hosts);
        var error = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
        Assert.Contains("AllowedHosts", error.Message);
    }

    [Theory]
    [InlineData("Production", "0。0。0。0")]
    [InlineData("Staging", "0。0。0。0")]
    [InlineData("Production", "0．0．0．0")]
    [InlineData("Staging", "0．0．0．0")]
    [InlineData("Production", "synthetic-config-canary\u200D.invalid")]
    [InlineData("Staging", "synthetic-config-canary\u200D.invalid")]
    public void Non_development_startup_rejects_normalized_universal_or_invalid_idn_hosts(string environment, string host)
    {
        using var factory = Host(environment, "https://web.example.invalid", "api.example.invalid;" + host);
        var error = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
        Assert.Contains("AllowedHosts", error.Message);
        Assert.DoesNotContain("synthetic-config-canary", error.ToString());
    }

    [Theory]
    [MemberData(nameof(ValidConfigurations))]
    public async Task Valid_configuration_preserves_origin_host_and_authentication_boundaries(
        string environment, string origin, string hosts, string requestBase, string requestOrigin)
    {
        await using var factory = Host(environment, origin, hosts);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri(requestBase) });
        using var allowed = new HttpRequestMessage(HttpMethod.Get, "/api/platform/me/access");
        allowed.Headers.Add("Origin", requestOrigin);
        using var response = await client.SendAsync(allowed);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(requestOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.False(response.Headers.Contains("Access-Control-Allow-Credentials"));
        Assert.False(response.Headers.Contains("Set-Cookie"));

        using var denied = new HttpRequestMessage(HttpMethod.Get, "/api/platform/me/access");
        denied.Headers.Add("Origin", "https://unconfigured.invalid");
        using var deniedResponse = await client.SendAsync(denied);
        Assert.Equal(HttpStatusCode.Unauthorized, deniedResponse.StatusCode);
        Assert.False(deniedResponse.Headers.Contains("Access-Control-Allow-Origin"));

        using var deniedHost = await client.GetAsync("https://unconfigured.invalid/api/platform/me/access");
        Assert.Equal(HttpStatusCode.BadRequest, deniedHost.StatusCode);
    }

    [Fact]
    public async Task Development_retains_local_origin_and_wildcard_host_configuration()
    {
        await using var factory = Host("Development", "http://localhost:3000", "*");
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://api.example.invalid") });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/platform/me/access");
        request.Headers.Add("Origin", "http://localhost:3000");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("http://localhost:3000", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    private static WebApplicationFactory<Program> Host(string environment, string origin, string hosts) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            foreach (var setting in new Dictionary<string, string?>
            {
                ["LocalDependencies:Enabled"] = "false",
                ["ConnectionStrings:GccsDatabase"] = string.Empty,
                ["InvitationDelivery:Enabled"] = "false",
                ["DemoRequests:Enabled"] = "false",
                ["Security:DevelopmentAuth:Enabled"] = "true",
                ["Authentication:Workforce:Authority"] = "https://identity.example.invalid/synthetic",
                ["Authentication:Workforce:Audience"] = "api://synthetic-boundary",
                ["Cors:AllowedOrigins:0"] = origin,
                ["Cors:AllowedOrigins:1"] = origin,
                ["AllowedHosts"] = hosts,
                ["TaskSearch:CursorSigningKey"] = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA="
            }) builder.UseSetting(setting.Key, setting.Value);
        });
}
