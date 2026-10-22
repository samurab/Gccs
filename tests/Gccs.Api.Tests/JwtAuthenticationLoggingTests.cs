using Gccs.Api.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class JwtAuthenticationLoggingTests
{
    public static TheoryData<string, string, string> AuthenticationPlanes => new()
    {
        { "Production", ApiSecurityExtensions.WorkforceJwtAuthenticationScheme, "workforce" },
        { "Production", ApiSecurityExtensions.CustomerJwtAuthenticationScheme, "customer" },
        { "Staging", ApiSecurityExtensions.WorkforceJwtAuthenticationScheme, "workforce" },
        { "Staging", ApiSecurityExtensions.CustomerJwtAuthenticationScheme, "customer" }
    };

    [Theory]
    [MemberData(nameof(AuthenticationPlanes))]
    public async Task Failure_logs_only_allowlisted_fields_without_exception_details(
        string environment, string scheme, string plane)
    {
        var logs = new CapturingLoggerProvider();
        using var services = CreateServices(environment, logs);
        var options = services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(scheme);
        var http = CreateContext(services, authorizationPresent: true);
        var exception = new InvalidOperationException("synthetic-exception-canary",
            new ArgumentException("synthetic-inner-canary"));
        exception.Data["token"] = "synthetic-data-canary";
        var context = new AuthenticationFailedContext(http, CreateScheme(scheme), options)
        {
            Exception = exception
        };

        await options.Events.AuthenticationFailed(context);

        var log = Assert.Single(logs.Entries);
        Assert.Equal(LogLevel.Warning, log.Level);
        Assert.Null(log.Exception);
        Assert.Equal(plane, log.Properties["AuthenticationPlane"]);
        Assert.Equal(nameof(InvalidOperationException), log.Properties["ExceptionType"]);
        Assert.Equal(new[] { "AuthenticationPlane", "ExceptionType", "{OriginalFormat}" }.Order(),
            log.Properties.Keys.Order());
        AssertNoCanary(log);
        Assert.Same(exception, context.Exception);
        Assert.Null(context.Result);
        Assert.Equal("Bearer synthetic-header-canary", http.Request.Headers.Authorization.ToString());
    }

    [Theory]
    [MemberData(nameof(AuthenticationPlanes))]
    public async Task Request_logs_only_plane_and_header_presence(string environment, string scheme, string plane)
    {
        foreach (var authorizationPresent in new[] { false, true })
        {
            var logs = new CapturingLoggerProvider();
            using var services = CreateServices(environment, logs);
            var options = services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(scheme);
            var context = new MessageReceivedContext(CreateContext(services, authorizationPresent),
                CreateScheme(scheme), options);

            await options.Events.MessageReceived(context);

            var log = Assert.Single(logs.Entries);
            Assert.Equal(LogLevel.Debug, log.Level);
            Assert.Null(log.Exception);
            Assert.Equal(plane, log.Properties["AuthenticationPlane"]);
            Assert.Equal(authorizationPresent, log.Properties["AuthorizationHeaderPresent"]);
            Assert.Equal(new[] { "AuthenticationPlane", "AuthorizationHeaderPresent", "{OriginalFormat}" }.Order(),
                log.Properties.Keys.Order());
            AssertNoCanary(log);
            Assert.Null(context.Result);
            Assert.Null(context.Token);
            Assert.Equal(authorizationPresent, context.Request.Headers.ContainsKey("Authorization"));
        }
    }

    [Theory]
    [MemberData(nameof(AuthenticationPlanes))]
    public void Logging_configuration_preserves_token_validation(string environment, string scheme, string plane)
    {
        using var services = CreateServices(environment, new CapturingLoggerProvider());
        var options = services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(scheme);

        Assert.Equal($"https://{plane}.example.invalid", options.Authority);
        Assert.Equal($"api://{plane}", options.Audience);
        Assert.True(options.RequireHttpsMetadata);
        Assert.False(options.MapInboundClaims);
        Assert.True(options.TokenValidationParameters.ValidateIssuer);
        Assert.True(options.TokenValidationParameters.ValidateAudience);
        Assert.True(options.TokenValidationParameters.ValidateLifetime);
        Assert.True(options.TokenValidationParameters.ValidateIssuerSigningKey);
    }

    private static ServiceProvider CreateServices(string environment, CapturingLoggerProvider logs)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:Workforce:Authority"] = "https://workforce.example.invalid",
            ["Authentication:Workforce:Audience"] = "api://workforce",
            ["Authentication:Customer:Authority"] = "https://customer.example.invalid",
            ["Authentication:Customer:Audience"] = "api://customer"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(logs));
        services.AddGccsApiSecurity(configuration, new TestEnvironment(environment));
        return services.BuildServiceProvider();
    }

    private static DefaultHttpContext CreateContext(IServiceProvider services, bool authorizationPresent)
    {
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Path = "/api/synthetic-path-canary";
        context.Request.QueryString = new QueryString("?token=synthetic-query-canary");
        if (authorizationPresent)
        {
            context.Request.Headers.Authorization = "Bearer synthetic-header-canary";
        }
        return context;
    }

    private static AuthenticationScheme CreateScheme(string scheme) => new(scheme, scheme, typeof(JwtBearerHandler));

    private static void AssertNoCanary(LogEntry log)
    {
        Assert.DoesNotContain("canary", log.Message, StringComparison.Ordinal);
        Assert.All(log.Properties.Values, value =>
            Assert.DoesNotContain("canary", value?.ToString() ?? string.Empty, StringComparison.Ordinal));
    }

    private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception,
        Dictionary<string, object?> Properties);

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public List<LogEntry> Entries { get; } = [];
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);
        public void Dispose() { }

        private sealed class CapturingLogger(CapturingLoggerProvider provider) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter) => provider.Entries.Add(new LogEntry(logLevel,
                    formatter(state, exception), exception,
                    ((IEnumerable<KeyValuePair<string, object?>>)state!).ToDictionary(pair => pair.Key, pair => pair.Value)));
        }
    }

    private sealed class TestEnvironment(string environment) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = environment;
        public string ApplicationName { get; set; } = "Gccs.Api.Tests";
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
