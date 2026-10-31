using System.Net;
using Gccs.Application.SamGov;
using Gccs.Infrastructure.SamGov;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class SamGovApiConfigurationTests
{
    [Fact]
    public void TC_22_1_1_Sam_gov_api_key_is_not_stored_in_source_configuration()
    {
        var root = FindRepositoryRoot();
        var appsettings = File.ReadAllText(Path.Combine(root, "apps", "api", "appsettings.json"));
        var developmentSettings = File.ReadAllText(Path.Combine(root, "apps", "api", "appsettings.Development.json"));

        Assert.Contains("\"SamGov\"", appsettings, StringComparison.Ordinal);
        Assert.DoesNotContain("\"ApiKey\"", appsettings, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"SamGov\"", developmentSettings, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TC_22_1_2_Lookup_uses_configured_retry_behavior()
    {
        var handler = new SequenceHandler(
            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError),
            _ => new HttpResponseMessage(HttpStatusCode.TooManyRequests),
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"entityRegistration":{"ueiSAM":"ABC123"}}""")
            });
        var client = CreateClient(handler, new SamGovOptions { ApiKey = "test-key", MaxRetries = 2, TimeoutSeconds = 5 });

        var result = await client.LookupByUeiAsync("ABC123");

        Assert.True(result.IsSuccess);
        Assert.Equal(3, handler.RequestCount);
    }

    [Fact]
    public async Task TC_22_1_2_Lookup_uses_configured_timeout()
    {
        var handler = new DelayingHandler();
        var client = CreateClient(handler, new SamGovOptions { ApiKey = "test-key", MaxRetries = 0, TimeoutSeconds = 1 });

        var result = await client.LookupByUeiAsync("ABC123");

        Assert.False(result.IsSuccess);
        Assert.True(handler.SawCanceledToken);
        Assert.Equal(SamGovEntityLookupClient.UserSafeFailureMessage, result.Error?.Message);
    }

    [Fact]
    public async Task TC_22_1_3_and_TC_22_1_4_Failures_are_user_safe_and_logs_redact_secrets_and_payloads()
    {
        var logger = new CapturingLogger<SamGovEntityLookupClient>();
        var handler = new SequenceHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("sensitive-response-payload")
        });
        var client = CreateClient(handler, new SamGovOptions { ApiKey = "secret-sam-key", MaxRetries = 0 }, logger);

        var result = await client.LookupByUeiAsync("ABC123");
        var logs = string.Join("\n", logger.Messages);

        Assert.False(result.IsSuccess);
        Assert.Equal("samgov_unavailable", result.Error?.Code);
        Assert.Equal(SamGovEntityLookupClient.UserSafeFailureMessage, result.Error?.Message);
        Assert.DoesNotContain("secret-sam-key", logs, StringComparison.Ordinal);
        Assert.DoesNotContain("sensitive-response-payload", logs, StringComparison.Ordinal);
        Assert.DoesNotContain("api_key", logs, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("https://api.sam.gov/entity-information/v3/entities?ueiSAM=ABC123&api_key=<redacted>",
            SamGovEntityLookupClient.RedactSensitiveValue(handler.RequestUris.Single().ToString(), new SamGovOptions { ApiKey = "secret-sam-key" }));
    }

    [Fact]
    public async Task TC_22_1_5_Adapter_can_be_replaced_or_mocked_in_tests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISamGovEntityLookupClient>(new FakeSamGovClient());
        var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<ISamGovEntityLookupClient>();
        var result = await client.LookupByUeiAsync("MOCKUEI");

        Assert.True(result.IsSuccess);
        Assert.Contains("MOCKUEI", result.PayloadJson, StringComparison.Ordinal);
    }

    private static SamGovEntityLookupClient CreateClient(
        HttpMessageHandler handler,
        SamGovOptions options,
        ILogger<SamGovEntityLookupClient>? logger = null) =>
        new(
            new HttpClient(handler),
            Options.Create(options),
            logger ?? new CapturingLogger<SamGovEntityLookupClient>());

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Gccs.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private sealed class SequenceHandler(params Func<HttpRequestMessage, HttpResponseMessage>[] responses) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        public List<Uri> RequestUris { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            RequestUris.Add(request.RequestUri!);
            return Task.FromResult(responses[Math.Min(RequestCount - 1, responses.Length - 1)](request));
        }
    }

    private sealed class DelayingHandler : HttpMessageHandler
    {
        public bool SawCanceledToken { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            catch (OperationCanceledException)
            {
                SawCanceledToken = true;
                throw;
            }
        }
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));
    }

    private sealed class FakeSamGovClient : ISamGovEntityLookupClient
    {
        public Task<SamGovEntityLookupResult> LookupByUeiAsync(string uei, CancellationToken cancellationToken = default) =>
            Task.FromResult(SamGovEntityLookupResult.Success($$"""{"ueiSAM":"{{uei}}"}"""));

        public Task<SamGovEntityLookupResult> SearchAsync(SamGovEntitySearchRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(SamGovEntityLookupResult.Success($$"""{"ueiSAM":"{{request.Uei ?? request.LegalBusinessName}}"}"""));
    }
}
