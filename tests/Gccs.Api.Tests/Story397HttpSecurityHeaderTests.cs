using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class Story397HttpSecurityHeaderTests
{
    private static WebApplicationFactory<Program> Host() => new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            foreach (var setting in new Dictionary<string, string?>
            {
                ["LocalDependencies:Enabled"] = "false",
                ["ConnectionStrings:GccsDatabase"] = string.Empty,
                ["Security:DevelopmentAuth:Enabled"] = "true",
                ["Authentication:Workforce:Authority"] = "https://identity.example.invalid/synthetic",
                ["Authentication:Workforce:Audience"] = "api://synthetic-boundary",
                ["Cors:AllowedOrigins:0"] = "https://web.example.invalid",
                ["Cors:AllowedOrigins:1"] = "https://web.example.invalid",
                ["AllowedHosts"] = "api.example.invalid",
                ["TaskSearch:CursorSigningKey"] = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA="
            }) builder.UseSetting(setting.Key, setting.Value);
        });

    [Theory]
    // This isolated Production fixture has no ready deployment dependencies.
    [InlineData("/health", HttpStatusCode.ServiceUnavailable)]
    [InlineData("/api/platform/me/access", HttpStatusCode.Unauthorized)]
    public async Task Production_https_responses_have_security_headers_and_no_authentication_cookie(string path, HttpStatusCode status)
    {
        await using var host = Host();
        using var client = host.CreateClient(new() { BaseAddress = new Uri("https://api.example.invalid") });
        using var response = await client.GetAsync(path);
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("DENY", Assert.Single(response.Headers.GetValues("X-Frame-Options")));
        Assert.Equal("no-referrer", Assert.Single(response.Headers.GetValues("Referrer-Policy")));
        Assert.Equal("camera=(), microphone=(), geolocation=()", Assert.Single(response.Headers.GetValues("Permissions-Policy")));
        Assert.Contains("max-age=", Assert.Single(response.Headers.GetValues("Strict-Transport-Security")));
        Assert.False(response.Headers.Contains("Set-Cookie"));
        if (path.StartsWith("/api/", StringComparison.Ordinal)) Assert.True(response.Headers.CacheControl!.NoStore);
    }

    [Theory]
    [InlineData("https://web.example.invalid", true)]
    [InlineData("https://foreign.example.invalid", false)]
    public async Task Cors_allows_only_configured_origin_and_does_not_enable_cookie_credentials(string origin, bool allowed)
    {
        await using var host = Host();
        using var client = host.CreateClient(new() { BaseAddress = new Uri("https://api.example.invalid") });
        using var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/platform/me/access");
        preflight.Headers.Add("Origin", origin);
        preflight.Headers.Add("Access-Control-Request-Method", "GET");
        preflight.Headers.Add("Access-Control-Request-Headers", "authorization");
        using var response = await client.SendAsync(preflight);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(allowed, response.Headers.Contains("Access-Control-Allow-Origin"));
        if (allowed) Assert.Equal(origin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.False(response.Headers.Contains("Access-Control-Allow-Credentials"));
        Assert.False(response.Headers.Contains("Set-Cookie"));
        using var actual = new HttpRequestMessage(HttpMethod.Get, "/api/platform/me/access");
        actual.Headers.Add("Origin", origin);
        using var actualResponse = await client.SendAsync(actual);
        Assert.Equal(HttpStatusCode.Unauthorized, actualResponse.StatusCode);
        Assert.Equal(allowed, actualResponse.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.False(actualResponse.Headers.Contains("Set-Cookie"));
    }
}
