using System.Text.Json;
using Gccs.Application.Identity;
using Gccs.Domain.Audit;
using Gccs.Domain.Identity;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Identity;

public sealed class EfInvitationDeliveryRepository(GccsDbContext dbContext) : IInvitationDeliveryRepository
{
    public async Task<ClaimedInvitationDelivery?> TryClaimNextAsync(
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        var candidateIds = await dbContext.TenantInvitations
            .AsNoTracking()
            .Where(invitation =>
                invitation.Status == TenantInvitationStatus.Pending &&
                invitation.ExpiresAt > now &&
                (invitation.DeliveryStatus == InvitationDeliveryStatus.Queued ||
                 invitation.DeliveryStatus == InvitationDeliveryStatus.RetryScheduled ||
                 (invitation.DeliveryStatus == InvitationDeliveryStatus.Processing && invitation.DeliveryLeaseUntil < now)) &&
                (invitation.NextDeliveryAttemptAt == null || invitation.NextDeliveryAttemptAt <= now))
            .OrderBy(invitation => invitation.NextDeliveryAttemptAt)
            .ThenBy(invitation => invitation.CreatedAt)
            .Select(invitation => invitation.Id)
            .Take(10)
            .ToListAsync(cancellationToken);

        foreach (var invitationId in candidateIds)
        {
            var claimed = await dbContext.TenantInvitations
                .Where(invitation =>
                    invitation.Id == invitationId &&
                    invitation.Status == TenantInvitationStatus.Pending &&
                    invitation.ExpiresAt > now &&
                    (invitation.DeliveryStatus == InvitationDeliveryStatus.Queued ||
                     invitation.DeliveryStatus == InvitationDeliveryStatus.RetryScheduled ||
                     (invitation.DeliveryStatus == InvitationDeliveryStatus.Processing && invitation.DeliveryLeaseUntil < now)) &&
                    (invitation.NextDeliveryAttemptAt == null || invitation.NextDeliveryAttemptAt <= now))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(invitation => invitation.DeliveryStatus, InvitationDeliveryStatus.Processing)
                    .SetProperty(invitation => invitation.DeliveryLeaseUntil, now.Add(leaseDuration))
                    .SetProperty(invitation => invitation.LastDeliveryAttemptAt, now)
                    .SetProperty(invitation => invitation.DeliveryAttemptCount, invitation => invitation.DeliveryAttemptCount + 1),
                    cancellationToken);
            if (claimed == 0)
            {
                continue;
            }

            return await dbContext.TenantInvitations
                .AsNoTracking()
                .Where(invitation => invitation.Id == invitationId)
                .Select(invitation => new ClaimedInvitationDelivery(
                    invitation.Id,
                    invitation.TenantId,
                    invitation.Tenant!.Name,
                    invitation.Email,
                    invitation.Tenant!.PlatformOnboarding != null
                        ? invitation.Tenant.PlatformOnboarding.OwnerDisplayName
                        : invitation.Email,
                    invitation.RoleName,
                    invitation.ExpiresAt,
                    invitation.DeliveryAttemptCount))
                .SingleAsync(cancellationToken);
        }

        return null;
    }

    public async Task SetTokenHashAsync(
        Guid invitationId,
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        var updated = await dbContext.TenantInvitations
            .Where(invitation => invitation.Id == invitationId && invitation.DeliveryStatus == InvitationDeliveryStatus.Processing)
            .ExecuteUpdateAsync(setters => setters.SetProperty(invitation => invitation.InvitationTokenHash, tokenHash), cancellationToken);
        if (updated != 1)
        {
            throw new InvalidOperationException("The claimed invitation delivery is no longer available.");
        }
    }

    public async Task MarkSentAsync(
        Guid invitationId,
        string providerMessageId,
        DateTimeOffset sentAt,
        CancellationToken cancellationToken = default)
    {
        var invitation = await LoadProcessingInvitationAsync(invitationId, cancellationToken);
        if (invitation is null)
        {
            return;
        }

        invitation.DeliveryStatus = InvitationDeliveryStatus.Sent;
        invitation.NotificationSentAt = sentAt;
        invitation.NotificationPlaceholder = $"{invitation.RoleName} invitation was accepted by the email provider; inbox delivery is not confirmed.";
        invitation.DeliveryProviderMessageId = Truncate(providerMessageId, 200);
        invitation.DeliveryFailureCode = null;
        invitation.DeliveryLeaseUntil = null;
        invitation.NextDeliveryAttemptAt = null;
        AddAudit(invitation, AuditAction.Updated, invitation.NotificationPlaceholder, sentAt);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(
        Guid invitationId,
        string failureCode,
        DateTimeOffset attemptedAt,
        DateTimeOffset? retryAt,
        CancellationToken cancellationToken = default)
    {
        var invitation = await LoadProcessingInvitationAsync(invitationId, cancellationToken);
        if (invitation is null)
        {
            return;
        }

        invitation.DeliveryStatus = retryAt.HasValue
            ? InvitationDeliveryStatus.RetryScheduled
            : InvitationDeliveryStatus.Failed;
        invitation.NotificationPlaceholder = retryAt.HasValue
            ? $"{invitation.RoleName} invitation delivery failed and is scheduled for retry."
            : $"{invitation.RoleName} invitation delivery failed after the maximum retry count.";
        invitation.DeliveryFailureCode = Truncate(failureCode, 120);
        invitation.DeliveryLeaseUntil = null;
        invitation.NextDeliveryAttemptAt = retryAt;
        AddAudit(invitation, AuditAction.Rejected, invitation.NotificationPlaceholder, attemptedAt);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<TenantInvitationEntity?> LoadProcessingInvitationAsync(Guid invitationId, CancellationToken cancellationToken) =>
        await dbContext.TenantInvitations.SingleOrDefaultAsync(
            invitation =>
                invitation.Id == invitationId &&
                invitation.Status == TenantInvitationStatus.Pending &&
                invitation.DeliveryStatus == InvitationDeliveryStatus.Processing,
            cancellationToken);

    private void AddAudit(TenantInvitationEntity invitation, AuditAction action, string summary, DateTimeOffset occurredAt)
    {
        dbContext.AuditLogEntries.Add(new AuditLogEntryEntity
        {
            Id = Guid.NewGuid(),
            TenantId = invitation.TenantId,
            ActorUserId = null,
            Action = action,
            EntityType = "TenantInvitationDelivery",
            EntityId = invitation.Id.ToString(),
            OccurredAt = occurredAt,
            Summary = summary,
            MetadataJson = JsonSerializer.Serialize(new
            {
                invitation.Email,
                invitation.DeliveryStatus,
                invitation.DeliveryAttemptCount,
                invitation.DeliveryFailureCode
            })
        });
    }

    private static string Truncate(string value, int maximumLength) =>
        value.Length <= maximumLength ? value : value[..maximumLength];
}
