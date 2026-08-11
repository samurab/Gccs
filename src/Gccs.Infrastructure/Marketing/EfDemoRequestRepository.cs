using Gccs.Application.Marketing;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gccs.Infrastructure.Marketing;

public sealed class EfDemoRequestRepository(GccsDbContext dbContext) : IDemoRequestRepository
{
    public async Task<bool?> QueueOperatorResponseAsync(Guid requestId, string templateKey, Guid actorUserId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.DemoRequests.AsNoTracking().AnyAsync(item => item.Id == requestId, cancellationToken)) return null;
        var deliveryKind = $"OperatorResponse:{templateKey}";
        if (await dbContext.DemoRequestDeliveries.AsNoTracking().AnyAsync(item => item.DemoRequestId == requestId && item.DeliveryKind == deliveryKind, cancellationToken)) return false;
        dbContext.DemoRequestDeliveries.Add(new DemoRequestDeliveryEntity { Id = Guid.NewGuid(), DemoRequestId = requestId, DeliveryKind = deliveryKind, Status = "Queued", RequestedByUserId = actorUserId, CreatedAt = now, UpdatedAt = now });
        try { await dbContext.SaveChangesAsync(cancellationToken); return true; }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { dbContext.ChangeTracker.Clear(); return false; }
    }

    public async Task<DemoRequestOperationsPage> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page));
        if (pageSize is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(pageSize));
        var query = dbContext.DemoRequests.AsNoTracking().OrderByDescending(item => item.ReceivedAt);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(request => new DemoRequestOperationsItem(
                request.Id, request.FirstName, request.LastName, request.Email, request.Phone, request.Company,
                request.ReferralSource, request.EmployeeCount, request.Message, request.PreferredStartAt, request.PreferredTimeZone, request.ReceivedAt,
                dbContext.DemoRequestDeliveries.Where(d => d.DemoRequestId == request.Id && d.DeliveryKind == "InternalNotification").Select(d => d.Status).Single(),
                dbContext.DemoRequestDeliveries.Where(d => d.DemoRequestId == request.Id && d.DeliveryKind == "InternalNotification").Select(d => d.AttemptCount).Single(),
                dbContext.DemoRequestDeliveries.Where(d => d.DemoRequestId == request.Id && d.DeliveryKind == "InternalNotification").Select(d => d.NextAttemptAt).Single(),
                dbContext.DemoRequestDeliveries.Where(d => d.DemoRequestId == request.Id && d.DeliveryKind == "InternalNotification").Select(d => d.SentAt).Single(),
                dbContext.DemoRequestDeliveries.Where(d => d.DemoRequestId == request.Id && d.DeliveryKind == "InternalNotification").Select(d => d.FailureCode).Single(),
                dbContext.DemoRequestDeliveries.Where(d => d.DemoRequestId == request.Id && d.DeliveryKind == "RequesterAcknowledgement").Select(d => d.Status).SingleOrDefault() ?? "NotQueued"))
            .ToListAsync(cancellationToken);
        return new DemoRequestOperationsPage(items, page, pageSize, totalCount, page * pageSize < totalCount, page > 1);
    }

    public async Task<IReadOnlyList<DemoRequestCalendarItem>> ListCalendarAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default) =>
        await dbContext.DemoRequests.AsNoTracking()
            .Where(request => request.PreferredStartAt >= from && request.PreferredStartAt < to)
            .OrderBy(request => request.PreferredStartAt)
            .ThenBy(request => request.ReceivedAt)
            .Select(request => new DemoRequestCalendarItem(
                request.Id,
                request.FirstName,
                request.LastName,
                request.Company,
                request.PreferredStartAt!.Value,
                request.PreferredTimeZone,
                request.ReceivedAt,
                dbContext.DemoRequestDeliveries
                    .Where(delivery => delivery.DemoRequestId == request.Id && delivery.DeliveryKind == "InternalNotification")
                    .Select(delivery => delivery.Status)
                    .Single(),
                "Requested"))
            .ToListAsync(cancellationToken);

    public async Task CreateIfNewAsync(DemoRequestRecord request, CancellationToken cancellationToken = default)
    {
        var entity = new DemoRequestEntity
        {
            Id = request.Id,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Company = request.Company,
            ReferralSource = request.ReferralSource,
            EmployeeCount = request.EmployeeCount,
            Message = request.Message,
            PreferredStartAt = request.PreferredStartAt,
            PreferredTimeZone = request.PreferredTimeZone,
            ConsentNoticeVersion = request.ConsentNoticeVersion,
            DeduplicationKey = request.DeduplicationKey,
            ReceivedAt = request.ReceivedAt
        };
        dbContext.DemoRequests.Add(entity);
        dbContext.DemoRequestDeliveries.Add(new DemoRequestDeliveryEntity
        {
            Id = Guid.NewGuid(),
            DemoRequestId = request.Id,
            Status = "Queued",
            DeliveryKind = "InternalNotification",
            CreatedAt = request.ReceivedAt,
            UpdatedAt = request.ReceivedAt
        });
        dbContext.DemoRequestDeliveries.Add(new DemoRequestDeliveryEntity { Id = Guid.NewGuid(), DemoRequestId = request.Id, Status = "Queued", DeliveryKind = "RequesterAcknowledgement", CreatedAt = request.ReceivedAt, UpdatedAt = request.ReceivedAt });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            dbContext.ChangeTracker.Clear();
        }
    }

    public async Task<ClaimedDemoRequestDelivery?> TryClaimNextDeliveryAsync(DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken = default)
    {
        var ids = await dbContext.DemoRequestDeliveries.AsNoTracking()
            .Where(delivery =>
                (delivery.Status == "Queued" || delivery.Status == "RetryScheduled" || (delivery.Status == "Processing" && delivery.LeaseUntil < now)) &&
                (delivery.NextAttemptAt == null || delivery.NextAttemptAt <= now))
            .OrderBy(delivery => delivery.NextAttemptAt).ThenBy(delivery => delivery.CreatedAt)
            .Select(delivery => delivery.Id).Take(10).ToListAsync(cancellationToken);

        foreach (var id in ids)
        {
            var claimed = await dbContext.DemoRequestDeliveries
                .Where(delivery => delivery.Id == id &&
                    (delivery.Status == "Queued" || delivery.Status == "RetryScheduled" || (delivery.Status == "Processing" && delivery.LeaseUntil < now)) &&
                    (delivery.NextAttemptAt == null || delivery.NextAttemptAt <= now))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(delivery => delivery.Status, "Processing")
                    .SetProperty(delivery => delivery.LeaseUntil, now.Add(leaseDuration))
                    .SetProperty(delivery => delivery.AttemptCount, delivery => delivery.AttemptCount + 1)
                    .SetProperty(delivery => delivery.UpdatedAt, now), cancellationToken);
            if (claimed == 0) continue;

            return await dbContext.DemoRequestDeliveries.AsNoTracking()
                .Where(delivery => delivery.Id == id)
                .Select(delivery => new ClaimedDemoRequestDelivery(
                    delivery.Id,
                    delivery.DemoRequestId,
                    delivery.DemoRequest!.FirstName,
                    delivery.DemoRequest.LastName,
                    delivery.DemoRequest.Email,
                    delivery.DemoRequest.Phone,
                    delivery.DemoRequest.Company,
                    delivery.DemoRequest.ReferralSource,
                    delivery.DemoRequest.EmployeeCount,
                    delivery.DemoRequest.Message,
                    delivery.DemoRequest.PreferredStartAt,
                    delivery.DemoRequest.PreferredTimeZone,
                    delivery.DemoRequest.ReceivedAt,
                    delivery.AttemptCount,
                    delivery.DeliveryKind))
                .SingleAsync(cancellationToken);
        }

        return null;
    }

    public Task MarkDeliveryCompletedAsync(
        Guid deliveryId,
        DemoRequestDeliveryResult result,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken = default) =>
        UpdateAsync(deliveryId, result.Disposition.ToString(), completedAt, null, result.ProviderMessageId, null, cancellationToken);

    public Task MarkDeliveryFailedAsync(Guid deliveryId, string failureCode, DateTimeOffset attemptedAt, DateTimeOffset? retryAt, CancellationToken cancellationToken = default) =>
        UpdateAsync(deliveryId, retryAt.HasValue ? "RetryScheduled" : "Failed", attemptedAt, retryAt, null, failureCode, cancellationToken);

    public async Task<int> DeleteExpiredAsync(DateTimeOffset receivedBefore, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var requestIds = await dbContext.DemoRequests
            .Where(request => request.ReceivedAt < receivedBefore &&
                dbContext.DemoRequestDeliveries.Any(delivery =>
                    delivery.DemoRequestId == request.Id) &&
                !dbContext.DemoRequestDeliveries.Any(delivery =>
                    delivery.DemoRequestId == request.Id && delivery.Status != "Sent" && delivery.Status != "Captured" && delivery.Status != "Failed"))
            .Select(request => request.Id)
            .ToListAsync(cancellationToken);
        await dbContext.DemoRequestDeliveries.Where(delivery => requestIds.Contains(delivery.DemoRequestId)).ExecuteDeleteAsync(cancellationToken);
        var deleted = await dbContext.DemoRequests.Where(request => requestIds.Contains(request.Id)).ExecuteDeleteAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return deleted;
    }

    private async Task UpdateAsync(Guid id, string status, DateTimeOffset at, DateTimeOffset? retryAt, string? providerId, string? failureCode, CancellationToken cancellationToken)
    {
        var delivery = await dbContext.DemoRequestDeliveries.SingleOrDefaultAsync(item => item.Id == id && item.Status == "Processing", cancellationToken);
        if (delivery is null) return;
        delivery.Status = status;
        delivery.UpdatedAt = at;
        delivery.NextAttemptAt = retryAt;
        delivery.LeaseUntil = null;
        delivery.SentAt = status == "Sent" ? at : null;
        delivery.ProviderMessageId = Truncate(providerId, 300);
        delivery.FailureCode = Truncate(failureCode, 120);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string? Truncate(string? value, int length) => value is null || value.Length <= length ? value : value[..length];
}
