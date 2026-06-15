using System.Net.Sockets;
using System.Text;
using Gccs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Gccs.Api.LocalDevelopment;

public sealed class LocalDependencyHealthService
{
    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(2);
    private readonly GccsDbContext? _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly LocalDependencyOptions _options;

    public LocalDependencyHealthService(
        IOptions<LocalDependencyOptions> options,
        IHttpClientFactory httpClientFactory,
        IServiceProvider serviceProvider)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _dbContext = serviceProvider.GetService<GccsDbContext>();
    }

    public async Task<LocalDependencyHealthReport> CheckAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return new LocalDependencyHealthReport(true, []);
        }

        var checks = await Task.WhenAll(
            CheckDatabaseAsync(cancellationToken),
            CheckRedisAsync(cancellationToken),
            CheckObjectStorageAsync(cancellationToken),
            CheckMalwareScannerAsync(cancellationToken),
            CheckBackgroundJobsAsync(cancellationToken));

        return new LocalDependencyHealthReport(
            checks.All(check => check.Status == "ok"),
            checks.OrderBy(check => check.Name).ToArray());
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
        try
        {
            var (host, port) = ParseHostPort(_options.Redis.ConnectionString, 6379);
            using var client = await OpenTcpClientAsync(host, port, cancellationToken);
            await using var stream = client.GetStream();

            var ping = Encoding.ASCII.GetBytes("*1\r\n$4\r\nPING\r\n");
            await stream.WriteAsync(ping, cancellationToken);

            var buffer = new byte[64];
            var bytesRead = await stream.ReadAsync(buffer, cancellationToken).AsTask().WaitAsync(CheckTimeout, cancellationToken);
            var response = Encoding.ASCII.GetString(buffer, 0, bytesRead);

            return response.StartsWith("+PONG", StringComparison.OrdinalIgnoreCase)
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
        try
        {
            var (host, port) = ParseHostPort(_options.Redis.ConnectionString, 6379);
            using var client = await OpenTcpClientAsync(host, port, cancellationToken);
            await using var stream = client.GetStream();

            var ping = Encoding.ASCII.GetBytes("*1\r\n$4\r\nPING\r\n");
            await stream.WriteAsync(ping, cancellationToken);

            var buffer = new byte[64];
            var bytesRead = await stream.ReadAsync(buffer, cancellationToken).AsTask().WaitAsync(CheckTimeout, cancellationToken);
            var response = Encoding.ASCII.GetString(buffer, 0, bytesRead);

            return response.StartsWith("+PONG", StringComparison.OrdinalIgnoreCase)
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
}

public sealed record LocalDependencyHealthReport(bool IsHealthy, IReadOnlyCollection<LocalDependencyHealthCheck> Dependencies);

public sealed record LocalDependencyHealthCheck(string Name, string Status, string Detail)
{
    public static LocalDependencyHealthCheck Healthy(string name, string detail) => new(name, "ok", detail);

    public static LocalDependencyHealthCheck Unhealthy(string name, string detail) => new(name, "unhealthy", detail);
}
