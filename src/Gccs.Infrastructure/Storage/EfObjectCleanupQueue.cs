using Gccs.Application.Audit;
using Gccs.Application.Storage;
using Gccs.Domain.Audit;
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
        db.Set<ObjectCleanupEntity>().Add(new ObjectCleanupEntity
        {
            Id = Guid.NewGuid(), TenantId = target.TenantId, ActorUserId = actorUserId,
            Container = target.Container, ObjectName = ObjectStorageNames.NormalizeObjectName(target.ObjectName),
            CreatedAt = DateTimeOffset.UtcNow, NextAttemptAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken = default)
    {
        // Hold one row lock across the bounded idempotent delete. A crash or audit failure rolls
        // back completion; retrying an already absent object is a successful cleanup.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var pending = await db.Set<ObjectCleanupEntity>().FromSqlInterpolated($"""
            SELECT * FROM gccs.object_cleanup
            WHERE completed_at IS NULL AND next_attempt_at <= {now}
            ORDER BY next_attempt_at, id LIMIT 1 FOR UPDATE SKIP LOCKED
            """).ToArrayAsync(cancellationToken);
        var item = pending.SingleOrDefault();
        if (item is null) return false;
        item.Attempts++;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            await storage.DeleteAsync(new(item.TenantId, item.Container, item.ObjectName), timeout.Token);
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
        item.CompletedAt = DateTimeOffset.UtcNow;
        item.LastErrorCode = null;
        await audit.WriteAsync(item.TenantId, item.ActorUserId, AuditAction.Deleted, "ObjectCleanup", item.Id.ToString(),
            "Private object cleanup completed (object deleted or already absent).",
            new Dictionary<string, string> { ["container"] = item.Container.ToString(), ["result"] = "completed" }, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
