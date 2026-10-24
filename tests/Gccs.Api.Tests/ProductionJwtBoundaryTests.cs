using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using Gccs.Api.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ProductionJwtBoundaryTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData("valid", HttpStatusCode.OK)]
    [InlineData("wrong-issuer", HttpStatusCode.Unauthorized)]
    [InlineData("wrong-audience", HttpStatusCode.Unauthorized)]
    [InlineData("wrong-signature", HttpStatusCode.Unauthorized)]
    [InlineData("expired", HttpStatusCode.Unauthorized)]
    [InlineData("future", HttpStatusCode.Unauthorized)]
    [InlineData("missing", HttpStatusCode.Unauthorized)]
    public async Task Production_host_validates_signed_tokens_and_ignores_development_auth_headers(
        string scenario, HttpStatusCode expected)
    {
        const string issuer = "https://identity.example.invalid/synthetic";
        const string audience = "api://synthetic-security-boundary";
        var key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32)) { KeyId = "synthetic-key" };
        var metadata = new OpenIdConnectConfiguration { Issuer = issuer };
        metadata.SigningKeys.Add(key);
        await using var host = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            foreach (var setting in new Dictionary<string, string?>
                {
                    ["LocalDependencies:Enabled"] = "false",
                    ["ConnectionStrings:GccsDatabase"] = string.Empty,
                    ["Security:DevelopmentAuth:Enabled"] = "true",
                    ["Authentication:Workforce:Authority"] = issuer,
                    ["Authentication:Workforce:Audience"] = audience,
                    ["Cors:AllowedOrigins:0"] = "https://web.example.invalid",
                    ["Cors:AllowedOrigins:1"] = "https://web.example.invalid",
                    ["AllowedHosts"] = "localhost",
                    ["TaskSearch:CursorSigningKey"] = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA="
                })
            {
                builder.UseSetting(setting.Key, setting.Value);
            }
            builder.ConfigureServices(services => services.PostConfigure<JwtBearerOptions>(
                ApiSecurityExtensions.WorkforceJwtAuthenticationScheme,
                options => options.ConfigurationManager =
                    new StaticConfigurationManager<OpenIdConnectConfiguration>(metadata)));
        });
        using var client = host.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/platform/me/access");
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        var userId = Guid.NewGuid();
        if (scenario != "missing")
        {
            var now = DateTime.UtcNow;
            var signingKey = scenario == "wrong-signature"
                ? new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32)) { KeyId = key.KeyId }
                : key;
            var jwt = new JwtSecurityToken(
                issuer: scenario == "wrong-issuer" ? issuer + "/other" : issuer,
                audience: scenario == "wrong-audience" ? audience + "/other" : audience,
                claims: [new Claim("oid", userId.ToString()), new Claim("roles", PlatformAuthorization.PlatformOperatorRole)],
                notBefore: scenario == "future" ? now.AddMinutes(10) : now.AddMinutes(-20),
                expires: scenario == "expired" ? now.AddMinutes(-10) : now.AddMinutes(20),
                signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(jwt));
        }

        using var response = await client.SendAsync(request);
        Assert.Equal(expected, response.StatusCode);
        if (scenario == "valid")
        {
            var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            Assert.Equal(userId.ToString(), body.GetProperty("userId").GetString());
            Assert.True(body.GetProperty("canProvisionTenants").GetBoolean());
        }
    }
}
