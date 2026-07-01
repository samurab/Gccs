using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using Gccs.Application.Storage;
using Gccs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Gccs.Api.LocalDevelopment;

public sealed class LocalDependencyHealthService
{
    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(2);
    private static readonly Guid HealthTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly IConfiguration _configuration;
    private readonly GccsDbContext? _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IObjectStorageService? _objectStorageService;
    private readonly LocalDependencyOptions _options;

    public LocalDependencyHealthService(
        IOptions<LocalDependencyOptions> options,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _dbContext = serviceProvider.GetService<GccsDbContext>();
        _objectStorageService = HasAzureObjectStorageConfiguration()
            ? serviceProvider.GetService<IObjectStorageService>()
            : null;
    }

    public async Task<LocalDependencyHealthReport> CheckAsync(CancellationToken cancellationToken)
    {
        var checks = new List<Task<LocalDependencyHealthCheck>>
        {
            CheckDatabaseAsync(cancellationToken),
            CheckObjectStorageAsync(cancellationToken),
            CheckRedisAsync(cancellationToken),
            CheckBackgroundJobsAsync(cancellationToken)
        };

        if (_options.Enabled)
        {
            checks.Add(CheckMalwareScannerAsync(cancellationToken));
        }

        var results = await Task.WhenAll(checks);

        return new LocalDependencyHealthReport(
            results.All(check => check.Status == "ok"),
            results.OrderBy(check => check.Name).ToArray());
    }

    private async Task<LocalDependencyHealthCheck> CheckDatabaseAsync(CancellationToken cancellationToken)
    {
        if (_dbContext is null)
        {
            return LocalDependencyHealthCheck.Unhealthy(
                "postgresql",
                "GccsDbContext is not registered. Check ConnectionStrings:GccsDatabase.");
        }

        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? LocalDependencyHealthCheck.Healthy("postgresql", "Connected to local PostgreSQL.")
                : LocalDependencyHealthCheck.Unhealthy("postgresql", "Could not connect to local PostgreSQL.");
        }
        catch (Exception exception)
        {
            return LocalDependencyHealthCheck.Unhealthy(
                "postgresql",
                $"Could not connect to local PostgreSQL using ConnectionStrings:GccsDatabase ({exception.GetType().Name}).");
        }
    }

    private async Task<LocalDependencyHealthCheck> CheckRedisAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Redis.ConnectionString))
        {
            return LocalDependencyHealthCheck.Unhealthy(
                "redis",
                "Redis cache is not configured. Set LocalDependencies:Redis:ConnectionString or the staging cache equivalent before launch smoke approval.");
        }

        try
        {
            var response = await PingRedisAsync(_options.Redis.ConnectionString, cancellationToken);
            return response.Contains("PONG", StringComparison.OrdinalIgnoreCase)
                ? LocalDependencyHealthCheck.Healthy("redis", "Redis responded to PING.")
                : LocalDependencyHealthCheck.Unhealthy("redis", "Redis did not return PONG.");
        }
        catch (Exception exception)
        {
            return LocalDependencyHealthCheck.Unhealthy(
                "redis",
                $"Could not connect to local Redis ({exception.GetType().Name}).");
        }
    }

    private async Task<LocalDependencyHealthCheck> CheckObjectStorageAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled && HasAzureObjectStorageConfiguration())
        {
            if (_objectStorageService is null)
            {
                return LocalDependencyHealthCheck.Unhealthy(
                    "object-storage",
                    "Object storage service is not registered.");
            }

            try
            {
                await _objectStorageService.ExistsAsync(
                    new ObjectStorageReadRequest(
                        HealthTenantId,
                        ObjectStorageContainer.Evidence,
                        "health/probe.txt"),
                    cancellationToken);
                return LocalDependencyHealthCheck.Healthy(
                    "object-storage",
                    "Azure object storage endpoint is reachable.");
            }
            catch (Exception exception)
            {
                return LocalDependencyHealthCheck.Unhealthy(
                    "object-storage",
                    $"Could not connect to Azure object storage ({exception.GetType().Name}).");
            }
        }

        if (!_options.Enabled)
        {
            return LocalDependencyHealthCheck.Unhealthy(
                "object-storage",
                "Object storage is not configured.");
        }

        try
        {
            var endpoint = _options.ObjectStorage.Endpoint.TrimEnd('/');
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{endpoint}/minio/health/live");
            using var client = _httpClientFactory.CreateClient();
            using var response = await client.SendAsync(request, cancellationToken).WaitAsync(CheckTimeout, cancellationToken);

            return response.IsSuccessStatusCode
                ? LocalDependencyHealthCheck.Healthy("object-storage", $"MinIO health endpoint is reachable for bucket '{_options.ObjectStorage.Bucket}'.")
                : LocalDependencyHealthCheck.Unhealthy("object-storage", $"MinIO health endpoint returned {(int)response.StatusCode}.");
        }
        catch (Exception exception)
        {
            return LocalDependencyHealthCheck.Unhealthy(
                "object-storage",
                $"Could not connect to local object storage ({exception.GetType().Name}).");
        }
    }

    private async Task<LocalDependencyHealthCheck> CheckMalwareScannerAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var client = await OpenTcpClientAsync(_options.MalwareScanner.Host, _options.MalwareScanner.Port, cancellationToken);
            await using var stream = client.GetStream();

            var ping = Encoding.ASCII.GetBytes("PING\n");
            await stream.WriteAsync(ping, cancellationToken);

            var buffer = new byte[64];
            var bytesRead = await stream.ReadAsync(buffer, cancellationToken).AsTask().WaitAsync(CheckTimeout, cancellationToken);
            var response = Encoding.ASCII.GetString(buffer, 0, bytesRead);

            return response.Contains("PONG", StringComparison.OrdinalIgnoreCase)
                ? LocalDependencyHealthCheck.Healthy("malware-scanner", "ClamAV placeholder responded to PING.")
                : LocalDependencyHealthCheck.Unhealthy("malware-scanner", "ClamAV placeholder did not return PONG.");
        }
        catch (Exception exception)
        {
            return LocalDependencyHealthCheck.Unhealthy(
                "malware-scanner",
                $"Could not connect to local ClamAV placeholder ({exception.GetType().Name}).");
        }
    }

    private async Task<LocalDependencyHealthCheck> CheckBackgroundJobsAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Redis.ConnectionString))
        {
            return LocalDependencyHealthCheck.Unhealthy(
                "background-jobs",
                "Background job queue coordination is not configured. Configure Redis or the approved staging queue dependency before launch smoke approval.");
        }

        try
        {
            var response = await PingRedisAsync(_options.Redis.ConnectionString, cancellationToken);
            return response.Contains("PONG", StringComparison.OrdinalIgnoreCase)
                ? LocalDependencyHealthCheck.Healthy("background-jobs", "Background job queue coordination is reachable through Redis.")
                : LocalDependencyHealthCheck.Unhealthy("background-jobs", "Background job queue coordination did not return PONG.");
        }
        catch (Exception exception)
        {
            return LocalDependencyHealthCheck.Unhealthy(
                "background-jobs",
                $"Could not connect to background job queue coordination ({exception.GetType().Name}).");
        }
    }

    private static async Task<TcpClient> OpenTcpClientAsync(string host, int port, CancellationToken cancellationToken)
    {
        var client = new TcpClient();
        try
        {
            await client.ConnectAsync(host, port, cancellationToken).AsTask().WaitAsync(CheckTimeout, cancellationToken);
            return client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    private static (string Host, int Port) ParseHostPort(string connectionString, int defaultPort)
    {
        var endpoint = connectionString.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? string.Empty;
        var parts = endpoint.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 2 && int.TryParse(parts[1], out var port))
        {
            return (parts[0], port);
        }

        return (endpoint, defaultPort);
    }

    private static async Task<string> PingRedisAsync(string connectionString, CancellationToken cancellationToken)
    {
        var settings = ParseRedisConnectionString(connectionString);
        using var client = await OpenTcpClientAsync(settings.Host, settings.Port, cancellationToken);
        await using var networkStream = client.GetStream();
        Stream stream = networkStream;

        if (settings.UseSsl)
        {
            var sslStream = new SslStream(networkStream, leaveInnerStreamOpen: false);
            await sslStream.AuthenticateAsClientAsync(settings.Host).WaitAsync(CheckTimeout, cancellationToken);
            stream = sslStream;
        }

        await using (stream)
        {
            if (!string.IsNullOrWhiteSpace(settings.Password))
            {
                await WriteRedisCommandAsync(stream, ["AUTH", settings.Password], cancellationToken);
                var authResponse = await ReadRedisResponseAsync(stream, cancellationToken);
                if (authResponse.StartsWith("-ERR", StringComparison.OrdinalIgnoreCase))
                {
                    return authResponse;
                }
            }

            await WriteRedisCommandAsync(stream, ["PING"], cancellationToken);
            return await ReadRedisResponseAsync(stream, cancellationToken);
        }
    }

    private static async Task WriteRedisCommandAsync(Stream stream, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        builder.Append('*').Append(arguments.Count).Append("\r\n");
        foreach (var argument in arguments)
        {
            builder.Append('$').Append(Encoding.UTF8.GetByteCount(argument)).Append("\r\n");
            builder.Append(argument).Append("\r\n");
        }

        var command = Encoding.UTF8.GetBytes(builder.ToString());
        await stream.WriteAsync(command, cancellationToken).AsTask().WaitAsync(CheckTimeout, cancellationToken);
    }

    private static async Task<string> ReadRedisResponseAsync(Stream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[256];
        var bytesRead = await stream.ReadAsync(buffer, cancellationToken).AsTask().WaitAsync(CheckTimeout, cancellationToken);
        return Encoding.UTF8.GetString(buffer, 0, bytesRead);
    }

    private static RedisConnectionSettings ParseRedisConnectionString(string connectionString)
    {
        var parts = connectionString.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var endpoint = parts.FirstOrDefault() ?? string.Empty;
        var (host, parsedPort) = ParseHostPort(endpoint, 6379);
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var part in parts.Skip(1))
        {
            var pair = part.Split('=', 2, StringSplitOptions.TrimEntries);
            if (pair.Length == 2)
            {
                values[pair[0]] = pair[1];
            }
        }

        var password = values.TryGetValue("password", out var configuredPassword)
            ? configuredPassword
            : string.Empty;
        var useSsl = parsedPort == 6380 ||
            (values.TryGetValue("ssl", out var ssl) && bool.TryParse(ssl, out var parsedSsl) && parsedSsl);

        return new RedisConnectionSettings(host, parsedPort, useSsl, password);
    }

    private bool HasAzureObjectStorageConfiguration() =>
        !string.IsNullOrWhiteSpace(_configuration["Storage:BlobServiceUri"]) ||
        !string.IsNullOrWhiteSpace(_configuration["Storage:AccountName"]);
}

public sealed record RedisConnectionSettings(string Host, int Port, bool UseSsl, string Password);

public sealed record LocalDependencyHealthReport(bool IsHealthy, IReadOnlyCollection<LocalDependencyHealthCheck> Dependencies);

public sealed record LocalDependencyHealthCheck(string Name, string Status, string Detail)
{
    public static LocalDependencyHealthCheck Healthy(string name, string detail) => new(name, "ok", detail);

    public static LocalDependencyHealthCheck Unhealthy(string name, string detail) => new(name, "unhealthy", detail);
}
