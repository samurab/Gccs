using System.Text.Json;
using Gccs.Application.Notifications;
using Gccs.Domain.Audit;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Notifications;

public sealed class EfAssignmentEmailDeliveryRepository(GccsDbContext dbContext) : IAssignmentEmailDeliveryRepository
{
    public async Task<ClaimedAssignmentEmailDelivery?> TryClaimNextAsync(
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        var candidateIds = await dbContext.AssignmentEmailDeliveries
            .AsNoTracking()
            .Where(delivery =>
                (delivery.Status == "Queued" ||
                 delivery.Status == "RetryScheduled" ||
                 (delivery.Status == "Processing" && delivery.LeaseUntil < now)) &&
                (delivery.NextAttemptAt == null || delivery.NextAttemptAt <= now))
            .OrderBy(delivery => delivery.NextAttemptAt)
            .ThenBy(delivery => delivery.CreatedAt)
            .Select(delivery => delivery.Id)
            .Take(10)
            .ToListAsync(cancellationToken);

        foreach (var deliveryId in candidateIds)
        {
            var claimed = await dbContext.AssignmentEmailDeliveries
                .Where(delivery =>
                    delivery.Id == deliveryId &&
                    (delivery.Status == "Queued" ||
                     delivery.Status == "RetryScheduled" ||
                     (delivery.Status == "Processing" && delivery.LeaseUntil < now)) &&
                    (delivery.NextAttemptAt == null || delivery.NextAttemptAt <= now))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(delivery => delivery.Status, "Processing")
                    .SetProperty(delivery => delivery.LeaseUntil, now.Add(leaseDuration))
                    .SetProperty(delivery => delivery.AttemptCount, delivery => delivery.AttemptCount + 1),
                    cancellationToken);
            if (claimed == 0)
            {
                continue;
            }

            return await dbContext.AssignmentEmailDeliveries
                .AsNoTracking()
                .Where(delivery => delivery.Id == deliveryId)
                .Select(delivery => new ClaimedAssignmentEmailDelivery(
                    delivery.Id,
                    delivery.TenantId,
                    delivery.UserId,
                    delivery.RecipientEmail,
                    delivery.RecipientDisplayName,
                    delivery.LinkUrl,
                    delivery.AttemptCount))
                .SingleAsync(cancellationToken);
        }

        return null;
    }

    public async Task MarkSentAsync(
        Guid deliveryId,
        string providerMessageId,
        DateTimeOffset sentAt,
        CancellationToken cancellationToken = default)
    {
        var delivery = await LoadProcessingAsync(deliveryId, cancellationToken);
        if (delivery is null)
        {
            return;
        }

        delivery.Status = "Sent";
        delivery.SentAt = sentAt;
        delivery.ProviderMessageId = Truncate(providerMessageId, 300);
        delivery.FailureCode = null;
        delivery.LeaseUntil = null;
        delivery.NextAttemptAt = null;
        delivery.UpdatedAt = sentAt;
        AddAudit(delivery, AuditAction.Updated, "Assignment notification email was sent.", sentAt);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(
        Guid deliveryId,
        string failureCode,
        DateTimeOffset attemptedAt,
        DateTimeOffset? retryAt,
        CancellationToken cancellationToken = default)
    {
        var delivery = await LoadProcessingAsync(deliveryId, cancellationToken);
        if (delivery is null)
        {
            return;
        }

        delivery.Status = retryAt.HasValue ? "RetryScheduled" : "Failed";
        delivery.FailureCode = Truncate(failureCode, 120);
        delivery.LeaseUntil = null;
        delivery.NextAttemptAt = retryAt;
        delivery.UpdatedAt = attemptedAt;
        AddAudit(
            delivery,
            AuditAction.Rejected,
            retryAt.HasValue
                ? "Assignment notification email delivery failed and was scheduled for retry."
                : "Assignment notification email delivery failed after the maximum retry count.",
            attemptedAt);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private Task<AssignmentEmailDeliveryEntity?> LoadProcessingAsync(Guid deliveryId, CancellationToken cancellationToken) =>
        dbContext.AssignmentEmailDeliveries.SingleOrDefaultAsync(
            delivery => delivery.Id == deliveryId && delivery.Status == "Processing",
            cancellationToken);

    private void AddAudit(
        AssignmentEmailDeliveryEntity delivery,
        AuditAction action,
        string summary,
        DateTimeOffset occurredAt)
    {
        dbContext.AuditLogEntries.Add(new AuditLogEntryEntity
        {
            Id = Guid.NewGuid(),
            TenantId = delivery.TenantId,
            ActorUserId = null,
            Action = action,
            EntityType = "AssignmentEmailDelivery",
            EntityId = delivery.Id.ToString(),
            OccurredAt = occurredAt,
            Summary = summary,
            MetadataJson = JsonSerializer.Serialize(new
            {
                delivery.UserId,
                delivery.Status,
                delivery.AttemptCount,
                delivery.FailureCode
            })
        });
    }

    private static string Truncate(string value, int maximumLength) =>
        value.Length <= maximumLength ? value : value[..maximumLength];
}
