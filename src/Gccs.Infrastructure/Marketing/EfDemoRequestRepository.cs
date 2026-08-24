using Gccs.Application.Marketing;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Text.Json;

namespace Gccs.Infrastructure.Marketing;

public sealed class EfDemoRequestRepository(
    GccsDbContext dbContext,
    IOptions<DemoRequestOptions>? demoRequestOptions = null) : IDemoRequestRepository
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
        var now = DateTimeOffset.UtcNow;
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
                dbContext.DemoRequestDeliveries.Where(d => d.DemoRequestId == request.Id && d.DeliveryKind == "RequesterAcknowledgement").Select(d => d.Status).SingleOrDefault() ?? "NotQueued",
                dbContext.DemoAppointments.Where(a => a.DemoRequestId == request.Id).Select(a => a.Status).SingleOrDefault() ?? "Requested",
                dbContext.DemoAppointments.Where(a => a.DemoRequestId == request.Id).Select(a => (DateTimeOffset?)a.ConfirmedStartAt).SingleOrDefault(),
                dbContext.DemoAppointments.Where(a => a.DemoRequestId == request.Id).Select(a => (DateTimeOffset?)a.ConfirmedEndAt).SingleOrDefault(),
                dbContext.DemoAppointments.Where(a => a.DemoRequestId == request.Id).Select(a => a.ConfirmedTimeZone).SingleOrDefault(),
                dbContext.DemoAppointments.Where(a => a.DemoRequestId == request.Id).Select(a => (int?)a.DurationMinutes).SingleOrDefault(),
                dbContext.DemoAppointments.Where(a => a.DemoRequestId == request.Id).Select(a => a.MeetingMethod).SingleOrDefault(),
                dbContext.DemoAppointments.Where(a => a.DemoRequestId == request.Id).Select(a => a.MeetingJoinUrl).SingleOrDefault(),
                dbContext.DemoRequestDeliveries.Where(d => d.DemoRequestId == request.Id && d.DemoAppointmentEventId != null).OrderByDescending(d => d.CreatedAt).Select(d => d.Status).FirstOrDefault() ?? "NotQueued"))
            .ToListAsync(cancellationToken);
        var requestIds = items.Select(item => item.Id).ToArray();
        var followUpRows = await dbContext.DemoFollowUpRequests.AsNoTracking()
            .Where(request => requestIds.Contains(request.DemoRequestId))
            .OrderByDescending(request => request.RequestedAt)
            .Select(request => new FollowUpRow(
                request.Id,
                request.DemoRequestId,
                request.Status == DemoFollowUpCatalog.Pending && request.ExpiresAt <= now
                    ? DemoFollowUpCatalog.Expired
                    : request.Status,
                request.RequestedAt,
                request.ExpiresAt,
                request.RequestedByUserId,
                dbContext.DemoRequestDeliveries
                    .Where(delivery => delivery.DemoFollowUpRequestId == request.Id)
                    .Select(delivery => delivery.Status)
                    .SingleOrDefault() ?? "NotQueued",
                request.RespondedAt,
                dbContext.DemoFollowUpResponses.Where(response => response.DemoFollowUpRequestId == request.Id).Select(response => response.WorkflowsJson).SingleOrDefault(),
                dbContext.DemoFollowUpResponses.Where(response => response.DemoFollowUpRequestId == request.Id).Select(response => response.OtherWorkflow).SingleOrDefault(),
                dbContext.DemoFollowUpResponses.Where(response => response.DemoFollowUpRequestId == request.Id).Select(response => response.Goals).SingleOrDefault(),
                dbContext.DemoFollowUpResponses.Where(response => response.DemoFollowUpRequestId == request.Id).Select(response => response.Challenges).SingleOrDefault(),
                dbContext.DemoFollowUpResponses.Where(response => response.DemoFollowUpRequestId == request.Id).Select(response => response.CurrentProcess).SingleOrDefault(),
                dbContext.DemoFollowUpResponses.Where(response => response.DemoFollowUpRequestId == request.Id).Select(response => response.AdditionalContext).SingleOrDefault(),
                request.NoCuiNoticeVersion))
            .ToListAsync(cancellationToken);
        var followUpsByRequest = followUpRows
            .GroupBy(row => row.DemoRequestId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<DemoFollowUpOperationsItem>)group.Select(ToOperationsItem).ToArray());
        var enrichedItems = items
            .Select(item => item with
            {
                FollowUpRequests = followUpsByRequest.GetValueOrDefault(item.Id) ?? []
            })
            .ToArray();
        return new DemoRequestOperationsPage(enrichedItems, page, pageSize, totalCount, page * pageSize < totalCount, page > 1);
    }

    public async Task<IReadOnlyList<DemoRequestCalendarItem>> ListCalendarAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default) =>
        await dbContext.DemoRequests.AsNoTracking()
            .Where(request =>
                dbContext.DemoAppointments.Any(appointment =>
                    appointment.DemoRequestId == request.Id &&
                    appointment.ConfirmedStartAt >= from && appointment.ConfirmedStartAt < to) ||
                (!dbContext.DemoAppointments.Any(appointment => appointment.DemoRequestId == request.Id) &&
                    request.PreferredStartAt >= from && request.PreferredStartAt < to))
            .OrderBy(request => dbContext.DemoAppointments
                .Where(appointment => appointment.DemoRequestId == request.Id)
                .Select(appointment => (DateTimeOffset?)appointment.ConfirmedStartAt)
                .SingleOrDefault() ?? request.PreferredStartAt)
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
                dbContext.DemoAppointments.Where(appointment => appointment.DemoRequestId == request.Id).Select(appointment => appointment.Status).SingleOrDefault() ?? "Requested",
                dbContext.DemoAppointments.Where(appointment => appointment.DemoRequestId == request.Id).Select(appointment => (DateTimeOffset?)appointment.ConfirmedStartAt).SingleOrDefault(),
                dbContext.DemoAppointments.Where(appointment => appointment.DemoRequestId == request.Id).Select(appointment => (DateTimeOffset?)appointment.ConfirmedEndAt).SingleOrDefault(),
                dbContext.DemoAppointments.Where(appointment => appointment.DemoRequestId == request.Id).Select(appointment => appointment.ConfirmedTimeZone).SingleOrDefault(),
                dbContext.DemoAppointments.Where(appointment => appointment.DemoRequestId == request.Id).Select(appointment => (int?)appointment.DurationMinutes).SingleOrDefault(),
                dbContext.DemoAppointments.Where(appointment => appointment.DemoRequestId == request.Id).Select(appointment => appointment.MeetingMethod).SingleOrDefault()))
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
        if (demoRequestOptions?.Value.HubSpot.Enabled == true)
        {
            dbContext.DemoRequestDeliveries.Add(new DemoRequestDeliveryEntity
            {
                Id = Guid.NewGuid(),
                DemoRequestId = request.Id,
                Status = "Queued",
                DeliveryKind = "HubSpotSync",
                CreatedAt = request.ReceivedAt,
                UpdatedAt = request.ReceivedAt
            });
        }

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
                    delivery.DeliveryKind,
                    delivery.DemoAppointmentEvent == null ? null : delivery.DemoAppointmentEvent.ConfirmedStartAt,
                    delivery.DemoAppointmentEvent == null ? null : delivery.DemoAppointmentEvent.ConfirmedEndAt,
                    delivery.DemoAppointmentEvent == null ? null : delivery.DemoAppointmentEvent.ConfirmedTimeZone,
                    delivery.DemoAppointmentEvent == null ? null : delivery.DemoAppointmentEvent.DurationMinutes,
                    delivery.DemoAppointmentEvent == null ? null : delivery.DemoAppointmentEvent.MeetingMethod,
                    delivery.DemoAppointmentEvent == null ? null : delivery.DemoAppointmentEvent.MeetingJoinUrl,
                    delivery.DemoFollowUpRequestId,
                    delivery.DemoFollowUpRequest == null ? null : delivery.DemoFollowUpRequest.ExpiresAt))
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
        await dbContext.DemoFollowUpResponses.Where(item => requestIds.Contains(item.DemoRequestId)).ExecuteDeleteAsync(cancellationToken);
        await dbContext.DemoFollowUpRequests.Where(item => requestIds.Contains(item.DemoRequestId)).ExecuteDeleteAsync(cancellationToken);
        await dbContext.DemoAppointmentEvents.Where(item => requestIds.Contains(item.DemoRequestId)).ExecuteDeleteAsync(cancellationToken);
        await dbContext.DemoAppointments.Where(item => requestIds.Contains(item.DemoRequestId)).ExecuteDeleteAsync(cancellationToken);
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

    private static DemoFollowUpOperationsItem ToOperationsItem(FollowUpRow row)
    {
        IReadOnlyList<string> workflows = [];
        if (!string.IsNullOrWhiteSpace(row.WorkflowsJson))
        {
            try
            {
                workflows = JsonSerializer.Deserialize<string[]>(row.WorkflowsJson) ?? [];
            }
            catch (JsonException)
            {
                workflows = ["Unavailable"];
            }
        }

        return new DemoFollowUpOperationsItem(
            row.Id,
            row.Status,
            row.RequestedAt,
            row.ExpiresAt,
            row.RequestedByUserId,
            row.DeliveryStatus,
            row.RespondedAt,
            workflows,
            row.OtherWorkflow,
            row.Goals,
            row.Challenges,
            row.CurrentProcess,
            row.AdditionalContext,
            row.NoCuiNoticeVersion);
    }

    private sealed record FollowUpRow(
        Guid Id,
        Guid DemoRequestId,
        string Status,
        DateTimeOffset RequestedAt,
        DateTimeOffset ExpiresAt,
        Guid RequestedByUserId,
        string DeliveryStatus,
        DateTimeOffset? RespondedAt,
        string? WorkflowsJson,
        string? OtherWorkflow,
        string? Goals,
        string? Challenges,
        string? CurrentProcess,
        string? AdditionalContext,
        string NoCuiNoticeVersion);
}
