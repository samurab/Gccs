using Gccs.Application.Storage;

namespace Gccs.Api.Tests;

internal sealed class TestObjectStorageService : IObjectStorageService
{
    private readonly Dictionary<string, StoredObject> _objects = new(StringComparer.Ordinal);

    public async Task<ObjectStorageWriteResult> UploadAsync(
        ObjectStorageWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        var blobName = ObjectStorageNames.BuildTenantBlobName(request.TenantId, request.ObjectName);
        using var buffer = new MemoryStream();
        await request.Content.CopyToAsync(buffer, cancellationToken);
        _objects[blobName] = new StoredObject(buffer.ToArray(), request.ContentType, DateTimeOffset.UtcNow);

        return new ObjectStorageWriteResult(
            request.Container,
            blobName,
            new Uri($"https://storage.test/{blobName}"),
            "\"test\"",
            _objects[blobName].LastModified);
    }

    public Task<ObjectStorageReadResult?> OpenReadAsync(
        ObjectStorageReadRequest request,
        CancellationToken cancellationToken = default)
    {
        var blobName = ObjectStorageNames.BuildTenantBlobName(request.TenantId, request.ObjectName);
        if (!_objects.TryGetValue(blobName, out var storedObject))
        {
            return Task.FromResult<ObjectStorageReadResult?>(null);
        }

        return Task.FromResult<ObjectStorageReadResult?>(new ObjectStorageReadResult(
            request.Container,
            blobName,
            new MemoryStream(storedObject.Content, writable: false),
            storedObject.ContentType,
            storedObject.Content.Length,
            "\"test\"",
            storedObject.LastModified));
    }

    public Task<bool> ExistsAsync(
        ObjectStorageReadRequest request,
        CancellationToken cancellationToken = default)
    {
        var blobName = ObjectStorageNames.BuildTenantBlobName(request.TenantId, request.ObjectName);
        return Task.FromResult(_objects.ContainsKey(blobName));
    }

    public Task<bool> DeleteAsync(
        ObjectStorageReadRequest request,
        CancellationToken cancellationToken = default)
    {
        var blobName = ObjectStorageNames.BuildTenantBlobName(request.TenantId, request.ObjectName);
        return Task.FromResult(_objects.Remove(blobName));
    }

    private sealed record StoredObject(byte[] Content, string ContentType, DateTimeOffset LastModified);
}
