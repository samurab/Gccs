using Gccs.Domain.Storage;

namespace Gccs.Application.Storage;

// Server-owned attempt capability. A fence authorizes publication, never cancellation of provider I/O.
public sealed record ObjectWriteIntentHandle(Guid Id, Guid TenantId, Guid OwnerId, long Generation,
    ObjectStorageContainer Container, string ObjectName);

public interface IObjectWriteIntentRepository
{
    Task<ObjectWriteIntentHandle> PrepareAsync(ObjectStorageWriteRequest request, Guid actorUserId,
        string resourceType, Guid resourceId, Guid? resourceLeaseId, CancellationToken cancellationToken);
    Task StartAsync(ObjectWriteIntentHandle handle, CancellationToken cancellationToken);
    Task<bool> RecordWrittenAsync(ObjectWriteIntentHandle handle, string etag, CancellationToken cancellationToken);
    Task<T?> PublishAsync<T>(ObjectWriteIntentHandle handle, Func<CancellationToken, Task<T?>> publish,
        CancellationToken cancellationToken) where T : class;
    Task MarkAmbiguousAsync(ObjectWriteIntentHandle handle, CancellationToken cancellationToken);
    Task<ObjectWriteIntentState?> ReconcileFailureAsync(ObjectWriteIntentHandle handle, string? etag,
        CancellationToken cancellationToken);
}

public sealed class ObjectWritePublicationUnavailableException() : InvalidOperationException("The object write resource could not be published.");
