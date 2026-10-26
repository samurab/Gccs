using Gccs.Domain.Storage;

namespace Gccs.Application.Storage;

public sealed class DurableObjectWriteCoordinator(IObjectWriteIntentRepository intents, IObjectStorageService storage)
{
    public async Task<T?> ExecuteAsync<T>(ObjectStorageWriteRequest request, Guid actorUserId,
        string resourceType, Guid resourceId, Func<ObjectStorageWriteResult, CancellationToken, Task<T?>> publish,
        CancellationToken cancellationToken = default, Guid? resourceLeaseId = null) where T : class
    {
        // Preparation commits independently before the one provider attempt can be issued.
        var handle = await intents.PrepareAsync(request, actorUserId, resourceType, resourceId, resourceLeaseId, cancellationToken);
        await intents.StartAsync(handle, cancellationToken);
        ObjectStorageWriteResult stored;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            stored = await storage.UploadAsync(request with { CreateOnly = true }, timeout.Token).WaitAsync(timeout.Token);
        }
        catch
        {
            // The provider may have persisted bytes before throwing or observing cancellation.
            await RetainAmbiguousAsync(handle);
            throw;
        }
        if (string.IsNullOrWhiteSpace(stored.ETag) || stored.Container != request.Container ||
            stored.BlobName != ObjectStorageNames.BuildTenantBlobName(request.TenantId, request.ObjectName))
        {
            await RetainAmbiguousAsync(handle);
            throw new InvalidOperationException("The object write outcome could not be verified.");
        }
        if (!await intents.RecordWrittenAsync(handle, stored.ETag, cancellationToken))
            throw new InvalidOperationException("The object write publication authority expired.");
        T? result = null;
        try
        {
            result = await intents.PublishAsync(handle, async token => result = await publish(stored, token), cancellationToken);
            if (result is not null) return result;
        }
        catch (ObjectWritePublicationUnavailableException)
        {
            await intents.ReconcileFailureAsync(handle, stored.ETag, CancellationToken.None);
            return null;
        }
        catch
        {
            // A commit acknowledgement can fail after the transaction actually committed.
            // Re-query durable state before making any cleanup eligible. DB failure leaves the pointer held.
            try
            {
                var state = await intents.ReconcileFailureAsync(handle, stored.ETag, CancellationToken.None);
                if (state == ObjectWriteIntentState.Committed && result is not null) return result;
            }
            catch { /* The precommitted ledger remains the recovery obligation. */ }
            throw;
        }
        await intents.ReconcileFailureAsync(handle, stored.ETag, CancellationToken.None);
        return null;
    }

    private async Task RetainAmbiguousAsync(ObjectWriteIntentHandle handle)
    {
        try { await intents.MarkAmbiguousAsync(handle, CancellationToken.None); }
        catch { /* A durable Writing pointer already exists; never enqueue unknown outcomes. */ }
    }
}
