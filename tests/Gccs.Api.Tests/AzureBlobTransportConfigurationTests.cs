using Gccs.Application.Storage;
using Gccs.Infrastructure;
using Gccs.Infrastructure.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class AzureBlobTransportConfigurationTests
{
    private static readonly string SyntheticKey = Convert.ToBase64String(new byte[32]);

    public static IEnumerable<object?[]> RejectedConnectionStrings()
    {
        foreach (var environment in new string?[] { null, "Production", "Staging", "Custom" })
        foreach (var connection in new[]
        {
            Connection("http://127.0.0.1:10000/synthetic"),
            Connection("http://remote.invalid"),
            "UseDevelopmentStorage=true",
            "BlobEndpoint=http://remote.invalid;SharedAccessSignature=sv=2025-05-05&sig=synthetic-signature"
        })
            yield return [environment, connection];

        yield return ["Development", Connection("http://remote.invalid")];
        yield return ["Development", $"DefaultEndpointsProtocol=http;AccountName=synthetic;AccountKey={SyntheticKey}"];
        yield return ["Development", "UseDevelopmentStorage=true;DevelopmentStorageProxyUri=http://remote.invalid"];
    }

    [Theory]
    [MemberData(nameof(RejectedConnectionStrings))]
    public void Non_development_or_remote_http_is_rejected_before_storage_operations(string? environment, string connection)
    {
        var error = Assert.Throws<InvalidOperationException>(() => Create(environment, false, connection));
        Assert.Contains("HTTPS", error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(SyntheticKey, error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("synthetic-signature", error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(connection, error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Development")]
    public void Https_connection_strings_and_managed_identity_remain_constructible(string? environment)
    {
        Assert.IsType<AzureBlobObjectStorageService>(Create(environment, false, Connection("https://storage.invalid")));
        Assert.IsType<AzureBlobObjectStorageService>(Create(environment, true, null, "https://storage.invalid"));
        Assert.IsType<AzureBlobObjectStorageService>(Create(environment, false,
            "BlobEndpoint=https://storage.invalid;SharedAccessSignature=sv=2025-05-05&sig=synthetic-signature"));
    }

    [Theory]
    [InlineData("http://127.0.0.1:10000/synthetic")]
    [InlineData("http://localhost:10000/synthetic")]
    [InlineData("http://[::1]:10000/synthetic")]
    public void Explicit_development_preserves_loopback_connection_string_http(string endpoint) =>
        Assert.IsType<AzureBlobObjectStorageService>(Create("Development", false, Connection(endpoint)));

    [Fact]
    public void Explicit_development_preserves_development_storage_shortcut() =>
        Assert.IsType<AzureBlobObjectStorageService>(Create("Development", false, "UseDevelopmentStorage=true"));

    [Theory]
    [InlineData(null)]
    [InlineData("Production")]
    [InlineData("Development")]
    public void Http_managed_identity_and_implicit_credential_fallback_are_rejected(string? environment)
    {
        Assert.Throws<InvalidOperationException>(() => Create(environment, true, null, "http://127.0.0.1:10000/synthetic"));
        Assert.Throws<InvalidOperationException>(() => Create(environment, false, null, "http://127.0.0.1:10000/synthetic"));
    }

    [Fact]
    public void Resolved_blob_endpoint_takes_precedence_over_default_protocol()
    {
        Assert.IsType<AzureBlobObjectStorageService>(Create("Production", false,
            $"DefaultEndpointsProtocol=http;AccountName=synthetic;AccountKey={SyntheticKey};BlobEndpoint=https://storage.invalid"));
        Assert.Throws<InvalidOperationException>(() => Create("Production", false,
            $"DefaultEndpointsProtocol=https;AccountName=synthetic;AccountKey={SyntheticKey};BlobEndpoint=http://127.0.0.1:10000/synthetic"));
    }

    [Fact]
    public void Legacy_constructor_without_host_environment_fails_closed_for_http()
    {
        var configuration = Configuration(Connection("http://127.0.0.1:10000/synthetic"));
        Assert.Throws<InvalidOperationException>(() => new AzureBlobObjectStorageService(
            Options.Create(new AzureBlobStorageOptions { UseManagedIdentity = false }), configuration));
        Assert.IsType<AzureBlobObjectStorageService>(new AzureBlobObjectStorageService(
            Options.Create(new AzureBlobStorageOptions { UseManagedIdentity = false }), Configuration(Connection("https://storage.invalid"))));
    }

    [Theory]
    [InlineData("Development", true)]
    [InlineData("Staging", false)]
    [InlineData("Production", false)]
    [InlineData("Custom", false)]
    public void Api_composition_uses_its_actual_host_environment(string environment, bool allowsLoopbackHttp)
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            foreach (var setting in new Dictionary<string, string?>
            {
                ["LocalDependencies:Enabled"] = "false",
                ["ConnectionStrings:GccsDatabase"] = "",
                ["InvitationDelivery:Enabled"] = "false",
                ["DemoRequests:Enabled"] = "false",
                ["Authentication:Workforce:Authority"] = "https://identity.example.invalid/synthetic",
                ["Authentication:Workforce:Audience"] = "api://synthetic-boundary",
                ["Cors:AllowedOrigins:0"] = "https://web.invalid",
                ["Cors:AllowedOrigins:1"] = "https://web.invalid",
                ["AllowedHosts"] = "api.invalid",
                ["TaskSearch:CursorSigningKey"] = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=",
                ["ConnectionStrings:AzureStorage"] = Connection("http://127.0.0.1:10000/synthetic"),
                ["Storage:UseManagedIdentity"] = "false"
            }) builder.UseSetting(setting.Key, setting.Value);
            builder.ConfigureServices(services => services.RemoveAll<IHostedService>());
        });
        using var scope = factory.Services.CreateScope();
        if (allowsLoopbackHttp)
        {
            Assert.IsType<AzureBlobObjectStorageService>(scope.ServiceProvider.GetRequiredService<IObjectStorageService>());
        }
        else
        {
            Assert.Throws<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<IObjectStorageService>());
        }
    }

    private static IObjectStorageService Create(string? environment, bool managedIdentity, string? connection, string? uri = null)
    {
        var configuration = Configuration(connection, managedIdentity, uri);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        var host = environment is null ? null : new TestHostEnvironment(environment);
        if (host is null) services.AddGccsInfrastructure(configuration);
        else services.AddGccsInfrastructure(configuration, allowDevelopmentLoopbackHttp: host.IsDevelopment());
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IObjectStorageService>();
    }

    private static IConfiguration Configuration(string? connection, bool managedIdentity = false, string? uri = null) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:AzureStorage"] = connection,
            ["Storage:UseManagedIdentity"] = managedIdentity.ToString(),
            ["Storage:BlobServiceUri"] = uri,
            // Configuration text alone must not grant the trusted host's Development exception.
            ["ASPNETCORE_ENVIRONMENT"] = "Development"
        }).Build();

    private static string Connection(string endpoint) =>
        $"DefaultEndpointsProtocol=https;AccountName=synthetic;AccountKey={SyntheticKey};BlobEndpoint={endpoint}";
}
