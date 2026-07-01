namespace Gccs.Application.Storage;

public interface IObjectStorageService
{
    Task<ObjectStorageWriteResult> UploadAsync(
        ObjectStorageWriteRequest request,
        CancellationToken cancellationToken = default);

    Task<ObjectStorageReadResult?> OpenReadAsync(
        ObjectStorageReadRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        ObjectStorageReadRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        ObjectStorageReadRequest request,
        CancellationToken cancellationToken = default);
}

public enum ObjectStorageContainer
{
    Evidence,
    Exports,
    Reports
}

public sealed record ObjectStorageWriteRequest(
    Guid TenantId,
    ObjectStorageContainer Container,
    string ObjectName,
    Stream Content,
    string ContentType,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record ObjectStorageReadRequest(
    Guid TenantId,
    ObjectStorageContainer Container,
    string ObjectName);

public sealed record ObjectStorageWriteResult(
    ObjectStorageContainer Container,
    string BlobName,
    Uri Uri,
    string? ETag,
    DateTimeOffset? LastModified);

public sealed record ObjectStorageReadResult(
    ObjectStorageContainer Container,
    string BlobName,
    Stream Content,
    string ContentType,
    long ContentLength,
    string? ETag,
    DateTimeOffset? LastModified) : IAsyncDisposable
{
    public ValueTask DisposeAsync() => Content.DisposeAsync();
}

public static class ObjectStorageNames
{
    public static string BuildTenantBlobName(Guid tenantId, string objectName)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        var normalized = NormalizeObjectName(objectName);
        return $"tenants/{tenantId:D}/{normalized}";
    }

    public static string NormalizeObjectName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            throw new ArgumentException("Object name is required.", nameof(objectName));
        }

        var trimmed = objectName.Trim().Replace('\\', '/');
        var segments = trimmed
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length == 0 ||
            segments.Any(segment => segment is "." or "..") ||
            segments.Any(segment => segment.Contains('\0', StringComparison.Ordinal)))
        {
            throw new ArgumentException("Object name contains an invalid path segment.", nameof(objectName));
        }

        return string.Join('/', segments);
    }
}
