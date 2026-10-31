using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Storage;
using Gccs.Domain.Audit;
using Gccs.Domain.Reports;
using Gccs.Domain.Storage;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Storage;

public sealed class EfObjectWriteIntentRepository(GccsDbContext db, IApplicationTransaction transaction,
    IAuditEventWriter audit, IObjectCleanupQueue cleanup, bool allowNonrelationalDevelopment = false) : IObjectWriteIntentRepository
{
    public async Task<ObjectWriteIntentHandle> PrepareAsync(ObjectStorageWriteRequest request, Guid actorUserId,
        string resourceType, Guid resourceId, Guid? resourceLeaseId, CancellationToken cancellationToken)
    {
        RequireIndependentDurability();
        if (request.TenantId == Guid.Empty || actorUserId == Guid.Empty || resourceId == Guid.Empty ||
            !Enum.IsDefined(request.Container) || resourceType is not ("EvidenceItem" or "Contract" or "ReportExport"))
            throw new ArgumentException("An authoritative resource, tenant, actor and container are required.");
        if ((resourceType == "EvidenceItem" && request.Container != ObjectStorageContainer.Evidence) ||
            (resourceType == "Contract" && request.Container != ObjectStorageContainer.ContractDocuments) ||
            (resourceType == "ReportExport" && (request.Container != ObjectStorageContainer.Reports || resourceLeaseId is null)))
            throw new ArgumentException("The resource and object container do not match.");
        var now = DateTimeOffset.UtcNow;
        var item = new ObjectWriteIntentEntity
        {
            Id = Guid.NewGuid(), TenantId = request.TenantId, ActorUserId = actorUserId,
            Container = request.Container, ObjectName = ObjectStorageNames.NormalizeObjectName(request.ObjectName),
            State = ObjectWriteIntentState.Prepared, OwnerId = Guid.NewGuid(), Generation = 1,
            LeaseUntil = now.AddMinutes(2), ResourceType = resourceType, ResourceId = resourceId,
            ResourceLeaseId = resourceLeaseId, CreatedAt = now, UpdatedAt = now
        };
        return await transaction.ExecuteAsync(async token =>
        {
            if (!await ParentIsCurrentAsync(item, token))
                throw new InvalidOperationException("The object write resource is unavailable.");
            if (item.ResourceType == "ReportExport")
            {
                var reportLease = await db.ReportExports.Where(x => x.TenantId == item.TenantId && x.Id == item.ResourceId)
                    .Select(x => x.ProcessingLeaseUntil).SingleAsync(token);
                if (reportLease is null || reportLease <= DateTimeOffset.UtcNow)
                    throw new InvalidOperationException("The report export lease expired.");
                if (reportLease < item.LeaseUntil) item.LeaseUntil = reportLease.Value;
            }
            db.Set<ObjectWriteIntentEntity>().Add(item);
            await WriteTransitionAsync(item, "prepared", token);
            await db.SaveChangesAsync(token);
            return Handle(item);
        }, cancellationToken);
    }

    public Task StartAsync(ObjectWriteIntentHandle handle, CancellationToken cancellationToken) =>
        IndependentlyAsync(async token =>
        {
            var item = await LockOwnedAsync(handle, token);
            if (item.State != ObjectWriteIntentState.Prepared || item.LeaseUntil <= DateTimeOffset.UtcNow)
                throw new InvalidOperationException("The object write attempt cannot be issued.");
            item.State = ObjectWriteIntentState.Writing;
            await WriteTransitionAsync(item, "write_issued", token);
            return true;
        }, cancellationToken);

    public Task<bool> RecordWrittenAsync(ObjectWriteIntentHandle handle, string etag, CancellationToken cancellationToken) =>
        IndependentlyAsync(async token =>
        {
            var item = await LockOwnedAsync(handle, token);
            if (item.State != ObjectWriteIntentState.Writing)
                throw new InvalidOperationException("The object write attempt is no longer active.");
            if (string.IsNullOrWhiteSpace(etag) || etag.Length > 200 || item.LeaseUntil <= DateTimeOffset.UtcNow)
            {
                item.State = ObjectWriteIntentState.Ambiguous;
                await WriteTransitionAsync(item, "write_authority_lost", token);
                return false;
            }
            item.ETag = etag;
            item.State = ObjectWriteIntentState.Written;
            await WriteTransitionAsync(item, "write_terminal", token);
            return true;
        }, cancellationToken);

    public Task<T?> PublishAsync<T>(ObjectWriteIntentHandle handle, Func<CancellationToken, Task<T?>> publish,
        CancellationToken cancellationToken) where T : class =>
        IndependentlyAsync<T?>(async token =>
        {
            var item = await LockOwnedAsync(handle, token);
            if (item.State != ObjectWriteIntentState.Written || string.IsNullOrWhiteSpace(item.ETag) ||
                item.LeaseUntil <= DateTimeOffset.UtcNow || !await ParentIsCurrentAsync(item, token))
                throw new InvalidOperationException("The object write publication authority is unavailable.");
            var result = await publish(token);
            if (result is null) throw new ObjectWritePublicationUnavailableException();
            // Check expiry after the callback too: acquiring a lock does not extend the owner's lease.
            if (item.LeaseUntil <= DateTimeOffset.UtcNow)
                throw new InvalidOperationException("The object write publication authority expired.");
            await BindResourceAsync(item, token);
            item.State = ObjectWriteIntentState.Committed;
            await WriteTransitionAsync(item, "committed", token);
            // Do not depend on a callback's audit writer saving the final intent/link changes.
            await db.SaveChangesAsync(token);
            return result;
        }, cancellationToken);

    public Task MarkAmbiguousAsync(ObjectWriteIntentHandle handle, CancellationToken cancellationToken) =>
        IndependentlyAsync(async token =>
        {
            var item = await LockOwnedAsync(handle, token);
            if (item.State is ObjectWriteIntentState.Prepared or ObjectWriteIntentState.Writing or ObjectWriteIntentState.Written)
            {
                item.State = ObjectWriteIntentState.Ambiguous;
                await WriteTransitionAsync(item, "provider_outcome_unknown", token);
            }
            return true;
        }, cancellationToken);

    public Task<ObjectWriteIntentState?> ReconcileFailureAsync(ObjectWriteIntentHandle handle, string? etag,
        CancellationToken cancellationToken) => IndependentlyAsync<ObjectWriteIntentState?>(async token =>
        {
            var item = await LockOwnedAsync(handle, token);
            if (item.State == ObjectWriteIntentState.Committed) return item.State;
            if (item.State != ObjectWriteIntentState.Written || item.ETag != etag || string.IsNullOrWhiteSpace(etag))
                return item.State;
            if (await HasLiveReferenceAsync(db, item.TenantId, item.Container, item.ObjectName, token))
            {
                item.State = ObjectWriteIntentState.ConflictHold;
                await WriteTransitionAsync(item, "live_reference_conflict", token);
                return item.State;
            }
            // Only a positively completed write reaches this branch. Unknown writes never enqueue.
            await cleanup.EnqueueAsync(new(item.TenantId, item.Container, item.ObjectName, item.ETag), item.ActorUserId, token);
            await WriteTransitionAsync(item, "publication_failed", token);
            return item.State;
        }, cancellationToken);

    private Task<T> IndependentlyAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        RequireIndependentDurability();
        return transaction.ExecuteAsync(async token =>
        {
            var result = await operation(token);
            await db.SaveChangesAsync(token);
            return result;
        }, cancellationToken);
    }

    private void RequireIndependentDurability()
    {
        if ((!db.Database.IsRelational() && !allowNonrelationalDevelopment) ||
            db.Database.CurrentTransaction is not null || System.Transactions.Transaction.Current is not null)
            throw new InvalidOperationException("Object writes require independent durable persistence before storage I/O.");
        if (db.ChangeTracker.Entries().Any(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Object write transitions require a clean transaction boundary.");
    }

    private async Task<ObjectWriteIntentEntity> LockOwnedAsync(ObjectWriteIntentHandle handle, CancellationToken token)
    {
        ObjectWriteIntentEntity? item;
        if (db.Database.IsRelational())
            item = (await db.Set<ObjectWriteIntentEntity>().FromSqlInterpolated($"""
                SELECT * FROM gccs.object_write_intents WHERE id = {handle.Id} AND tenant_id = {handle.TenantId} FOR UPDATE
                """).ToArrayAsync(token)).SingleOrDefault();
        else item = await db.Set<ObjectWriteIntentEntity>().SingleOrDefaultAsync(x => x.Id == handle.Id && x.TenantId == handle.TenantId, token);
        if (item is not null) await db.Entry(item).ReloadAsync(token);
        if (item is null || item.OwnerId != handle.OwnerId || item.Generation != handle.Generation ||
            item.Container != handle.Container || item.ObjectName != handle.ObjectName)
            throw new InvalidOperationException("The object write owner is unavailable.");
        return item;
    }

    private Task<bool> ParentIsCurrentAsync(ObjectWriteIntentEntity item, CancellationToken token) => item.ResourceType switch
    {
        "EvidenceItem" => db.EvidenceItems.AnyAsync(x => x.TenantId == item.TenantId && x.Id == item.ResourceId, token),
        "Contract" => db.Contracts.AnyAsync(x => x.TenantId == item.TenantId && x.Id == item.ResourceId, token),
        "ReportExport" => db.ReportExports.AnyAsync(x => x.TenantId == item.TenantId && x.Id == item.ResourceId &&
            x.Status == ReportExportStatus.Processing && x.ProcessingLeaseId == item.ResourceLeaseId &&
            x.ProcessingLeaseUntil > DateTimeOffset.UtcNow, token),
        _ => Task.FromResult(false)
    };

    private async Task BindResourceAsync(ObjectWriteIntentEntity item, CancellationToken token)
    {
        switch (item.Container)
        {
            case ObjectStorageContainer.Evidence:
                var version = await db.EvidenceFileVersions.SingleAsync(x => x.StorageUri == item.ObjectName &&
                    x.EvidenceItemId == item.ResourceId && x.EvidenceItem!.TenantId == item.TenantId && x.DeletedAt == null, token);
                if (version.ObjectWriteIntentId is not null) throw new InvalidOperationException("The resource already has a write owner.");
                version.ObjectWriteIntentId = item.Id; version.ObjectWriteIntentTenantId = item.TenantId;
                break;
            case ObjectStorageContainer.ContractDocuments:
                var document = await db.Set<ContractDocumentEntity>().SingleAsync(x => x.StorageUri == item.ObjectName &&
                    x.ContractId == item.ResourceId && x.Contract!.TenantId == item.TenantId, token);
                if (document.ObjectWriteIntentId is not null) throw new InvalidOperationException("The resource already has a write owner.");
                document.ObjectWriteIntentId = item.Id; document.ObjectWriteIntentTenantId = item.TenantId;
                break;
            case ObjectStorageContainer.Reports:
                var export = await db.ReportExports.SingleAsync(x => x.TenantId == item.TenantId && x.Id == item.ResourceId &&
                    x.ObjectName == item.ObjectName && x.Status == ReportExportStatus.Ready, token);
                if (export.ObjectWriteIntentId is not null) throw new InvalidOperationException("The resource already has a write owner.");
                export.ObjectWriteIntentId = item.Id;
                break;
            default: throw new InvalidOperationException("Unsupported object write resource.");
        }
    }

    internal static Task<bool> HasLiveReferenceAsync(GccsDbContext db, Guid tenantId, ObjectStorageContainer container,
        string objectName, CancellationToken token) => container switch
    {
        ObjectStorageContainer.Evidence => db.EvidenceFileVersions.AnyAsync(x => x.EvidenceItem!.TenantId == tenantId &&
            x.StorageUri == objectName && x.DeletedAt == null, token),
        ObjectStorageContainer.ContractDocuments => db.Set<ContractDocumentEntity>().AnyAsync(x => x.Contract!.TenantId == tenantId && x.StorageUri == objectName, token),
        ObjectStorageContainer.Reports => db.ReportExports.AnyAsync(x => x.TenantId == tenantId && x.ObjectName == objectName && x.Status == ReportExportStatus.Ready, token),
        _ => Task.FromResult(true) // Unknown legacy families require operator review, never unsafe deletion.
    };

    private async Task WriteTransitionAsync(ObjectWriteIntentEntity item, string resultCode, CancellationToken token)
    {
        item.UpdatedAt = DateTimeOffset.UtcNow;
        item.ResultCode = resultCode;
        await audit.WriteAsync(item.TenantId, item.ActorUserId, AuditAction.Updated, "ObjectWriteIntent", item.Id.ToString(),
            "Private object write lifecycle transition recorded.", new Dictionary<string, string>
            { ["state"] = item.State.ToString(), ["resultCode"] = resultCode, ["container"] = item.Container.ToString() }, token);
    }

    private static ObjectWriteIntentHandle Handle(ObjectWriteIntentEntity item) =>
        new(item.Id, item.TenantId, item.OwnerId, item.Generation, item.Container, item.ObjectName);
}
