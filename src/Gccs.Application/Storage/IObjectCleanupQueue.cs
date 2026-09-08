namespace Gccs.Application.Storage;

/// <summary>Enqueue inside the same transaction as the resource tombstone and audit event.</summary>
public interface IObjectCleanupQueue
{
    Task EnqueueAsync(ObjectStorageReadRequest target, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<bool> ProcessNextAsync(CancellationToken cancellationToken = default);
}
