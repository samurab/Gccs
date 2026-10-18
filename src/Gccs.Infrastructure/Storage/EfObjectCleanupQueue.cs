using Gccs.Application.Audit;
using Gccs.Application.Storage;
using Gccs.Domain.Audit;
using Gccs.Domain.Storage;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Storage;

public sealed class EfObjectCleanupQueue(GccsDbContext db, IObjectStorageService storage,
    IAuditEventWriter audit) : IObjectCleanupQueue
{
    public async Task EnqueueAsync(ObjectStorageReadRequest target, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        if (db.Database.IsRelational() && db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Object cleanup requires the resource transaction.");
        if (target.TenantId == Guid.Empty || actorUserId == Guid.Empty || !Enum.IsDefined(target.Container))
            throw new ArgumentException("An authoritative tenant, actor, and container are required.");
        var objectName = ObjectStorageNames.NormalizeObjectName(target.ObjectName);
        var intent = await FindIntentAsync(target.TenantId, target.Container, objectName, cancellationToken);
        if (intent is not null)
        {
            if (intent.ActorUserId != actorUserId && intent.State != ObjectWriteIntentState.Committed)
                throw new InvalidOperationException("The object cleanup owner is unavailable.");
            if (intent.State is not (ObjectWriteIntentState.Written or ObjectWriteIntentState.Committed or ObjectWriteIntentState.CleanupPending) ||
                string.IsNullOrWhiteSpace(intent.ETag) || (target.ExpectedETag is not null && target.ExpectedETag != intent.ETag) ||
                await EfObjectWriteIntentRepository.HasLiveReferenceAsync(db, target.TenantId, target.Container, objectName, cancellationToken))
                throw new InvalidOperationException("The object is not eligible for cleanup.");
            intent.State = ObjectWriteIntentState.CleanupPending;
            intent.UpdatedAt = DateTimeOffset.UtcNow;
            intent.ResultCode = "cleanup_pending";
            if (await db.Set<ObjectCleanupEntity>().AnyAsync(x => x.ObjectWriteIntentId == intent.Id && x.CompletedAt == null, cancellationToken))
                return;
        }
        db.Set<ObjectCleanupEntity>().Add(new ObjectCleanupEntity
        {
            Id = Guid.NewGuid(), TenantId = target.TenantId, ActorUserId = actorUserId,
            Container = target.Container, ObjectName = objectName,
            ObjectWriteIntentId = intent?.Id, ExpectedETag = intent?.ETag ?? target.ExpectedETag,
            CreatedAt = DateTimeOffset.UtcNow, NextAttemptAt = DateTimeOffset.UtcNow
        });
        if (intent is not null)
            await audit.WriteAsync(intent.TenantId, actorUserId, AuditAction.Updated, "ObjectWriteIntent", intent.Id.ToString(),
                "Terminal private object write became eligible for cleanup.",
                new Dictionary<string, string> { ["state"] = intent.State.ToString(), ["resultCode"] = "cleanup_pending" }, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken = default)
    {
        // Lock intent before job, matching publication/normal-deletion lock ordering.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var candidate = await db.Set<ObjectCleanupEntity>().AsNoTracking()
            .Where(x => x.CompletedAt == null && x.NextAttemptAt <= now)
            .OrderBy(x => x.NextAttemptAt).ThenBy(x => x.Id).FirstOrDefaultAsync(cancellationToken);
        if (candidate is null) return false;
        var intent = await FindIntentAsync(candidate.TenantId, candidate.Container, candidate.ObjectName, cancellationToken);
        var item = (await db.Set<ObjectCleanupEntity>().FromSqlInterpolated($"""
            SELECT * FROM gccs.object_cleanup WHERE id = {candidate.Id}
            AND completed_at IS NULL AND next_attempt_at <= {now} FOR UPDATE SKIP LOCKED
            """).ToArrayAsync(cancellationToken)).SingleOrDefault();
        if (item is null) return false;
        await db.Entry(item).ReloadAsync(cancellationToken);
        var unsafeIntent = item.ObjectWriteIntentId is not null &&
            (intent is null || item.ObjectWriteIntentId != intent.Id || intent.TenantId != item.TenantId ||
             intent.Container != item.Container || intent.ObjectName != item.ObjectName ||
             intent.State != ObjectWriteIntentState.CleanupPending || string.IsNullOrWhiteSpace(item.ExpectedETag) ||
             item.ExpectedETag != intent.ETag);
        // A legacy job cannot bypass a subsequently introduced owner for the same private key.
        unsafeIntent |= item.ObjectWriteIntentId is null && intent is not null;
        if (unsafeIntent || await EfObjectWriteIntentRepository.HasLiveReferenceAsync(db, item.TenantId, item.Container, item.ObjectName, cancellationToken))
        {
            item.LastErrorCode = "cleanup_safety_hold";
            item.NextAttemptAt = now.AddHours(1);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        item.Attempts++;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            await storage.DeleteAsync(new(item.TenantId, item.Container, item.ObjectName, item.ExpectedETag), timeout.Token);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Never persist provider exception messages, which can contain signed URLs or keys.
            item.LastErrorCode = exception is OperationCanceledException ? "cleanup_timeout" : "cleanup_provider_failure";
            item.NextAttemptAt = now.AddSeconds(Math.Min(3600, 15 * Math.Pow(2, Math.Min(item.Attempts, 8))));
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        if (intent is not null)
        {
            intent.State = ObjectWriteIntentState.Cleaned;
            intent.UpdatedAt = DateTimeOffset.UtcNow;
            intent.ResultCode = "cleanup_completed";
        }
        item.CompletedAt = DateTimeOffset.UtcNow;
        item.LastErrorCode = null;
        await audit.WriteAsync(item.TenantId, item.ActorUserId, AuditAction.Deleted, "ObjectCleanup", item.Id.ToString(),
            "Private object cleanup completed (object deleted or already absent).",
            new Dictionary<string, string> { ["container"] = item.Container.ToString(), ["result"] = "completed" }, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<ObjectWriteIntentEntity?> FindIntentAsync(Guid tenantId, ObjectStorageContainer container,
        string objectName, CancellationToken token)
    {
        ObjectWriteIntentEntity? intent;
        if (db.Database.IsRelational())
            intent = (await db.Set<ObjectWriteIntentEntity>().FromSqlInterpolated($"""
                SELECT * FROM gccs.object_write_intents WHERE tenant_id = {tenantId}
                AND container = {container.ToString()} AND object_name = {objectName} FOR UPDATE
                """).ToArrayAsync(token)).SingleOrDefault();
        else intent = await db.Set<ObjectWriteIntentEntity>().SingleOrDefaultAsync(x => x.TenantId == tenantId &&
            x.Container == container && x.ObjectName == objectName, token);
        if (intent is not null && db.Entry(intent).State == EntityState.Unchanged) await db.Entry(intent).ReloadAsync(token);
        return intent;
    }
}
