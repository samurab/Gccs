using System.Text.Json;
using Gccs.Application.Audit;
using Gccs.Application.Identity;
using Gccs.Application.Security;
using Gccs.Domain.Audit;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Identity;

public sealed class EfTenantInvitationRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext,
    IAuditRequestMetadata requestMetadata) : ITenantInvitationRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<TenantInvitationDto>> ListCurrentTenantInvitationsAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.TenantInvitations
            .AsNoTracking()
            .Where(invitation => invitation.TenantId == tenantContext.TenantId)
            .OrderByDescending(invitation => invitation.CreatedAt)
            .ThenBy(invitation => invitation.Email)
            .Select(invitation => ToDto(invitation))
            .ToListAsync(cancellationToken);

    public Task<bool> CurrentTenantPendingInvitationExistsAsync(
        string email,
        CancellationToken cancellationToken = default) =>
        dbContext.TenantInvitations.AnyAsync(
            invitation =>
                invitation.TenantId == tenantContext.TenantId &&
                invitation.Email == email &&
                invitation.Status == TenantInvitationStatus.Pending,
            cancellationToken);

    public async Task<TenantInvitationDto> AddToCurrentTenantAsync(
        TenantInvitation invitation,
        CancellationToken cancellationToken = default)
    {
        var invitationEntity = new TenantInvitationEntity
        {
            Id = invitation.Id,
            TenantId = tenantContext.TenantId,
            Email = invitation.Email,
            RoleName = invitation.RoleName,
            InvitationTokenHash = invitation.InvitationTokenHash,
            Status = invitation.Status,
            ExpiresAt = invitation.ExpiresAt,
            AcceptedAt = invitation.AcceptedAt,
            AcceptedByUserId = invitation.AcceptedByUserId,
            RevokedAt = invitation.RevokedAt,
            RevokedByUserId = invitation.RevokedByUserId,
            NotificationSentAt = invitation.NotificationSentAt,
            NotificationPlaceholder = invitation.NotificationPlaceholder,
            DeliveryStatus = invitation.DeliveryStatus,
            DeliveryAttemptCount = invitation.DeliveryAttemptCount,
            NextDeliveryAttemptAt = invitation.NextDeliveryAttemptAt,
            DeliveryLeaseUntil = invitation.DeliveryLeaseUntil,
            LastDeliveryAttemptAt = invitation.LastDeliveryAttemptAt,
            DeliveryProviderMessageId = invitation.DeliveryProviderMessageId,
            DeliveryFailureCode = invitation.DeliveryFailureCode,
            CreatedAt = invitation.Audit.CreatedAt,
            CreatedByUserId = invitation.Audit.CreatedByUserId
        };

        dbContext.TenantInvitations.Add(invitationEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(invitationEntity);
    }

    public async Task<TenantInvitationDto?> FindByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        var invitation = await dbContext.TenantInvitations
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.InvitationTokenHash == tokenHash, cancellationToken);

        return invitation is null ? null : ToDto(invitation);
    }

    public async Task<InvitationAcceptanceContextDto?> FindAcceptanceContextByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        await dbContext.TenantInvitations
            .AsNoTracking()
            .Where(candidate => candidate.InvitationTokenHash == tokenHash)
            .Select(candidate => new InvitationAcceptanceContextDto(
                candidate.Id,
                candidate.Tenant!.Name,
                candidate.Email,
                candidate.RoleName,
                candidate.Status,
                candidate.ExpiresAt))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<TenantInvitationDto?> AcceptAsync(
        Guid invitationId,
        Guid userId,
        string email,
        string displayName,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        TenantInvitationEntity? invitation;
        var now = DateTimeOffset.UtcNow;

        if (dbContext.Database.IsRelational())
        {
            var claimed = await dbContext.TenantInvitations
                .Where(candidate =>
                    candidate.Id == invitationId &&
                    candidate.Status == TenantInvitationStatus.Pending &&
                    candidate.ExpiresAt > now)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(candidate => candidate.Status, TenantInvitationStatus.Accepted)
                    .SetProperty(candidate => candidate.AcceptedAt, now)
                    .SetProperty(candidate => candidate.AcceptedByUserId, actorUserId)
                    .SetProperty(candidate => candidate.InvitationTokenHash, (string?)null)
                    .SetProperty(candidate => candidate.UpdatedAt, now)
                    .SetProperty(candidate => candidate.UpdatedByUserId, actorUserId),
                    cancellationToken);
            if (claimed != 1)
            {
                throw new InvalidInvitationStateException("Only an unexpired pending invitation can be accepted.");
            }

            invitation = await dbContext.TenantInvitations.SingleAsync(candidate => candidate.Id == invitationId, cancellationToken);
        }
        else
        {
            invitation = await dbContext.TenantInvitations
                .SingleOrDefaultAsync(candidate => candidate.Id == invitationId, cancellationToken);
        }

        if (invitation is null)
        {
            return null;
        }

        if (!dbContext.Database.IsRelational())
        {
            if (invitation.Status != TenantInvitationStatus.Pending || invitation.ExpiresAt <= now)
            {
                throw new InvalidInvitationStateException("Only an unexpired pending invitation can be accepted.");
            }

            invitation.Status = TenantInvitationStatus.Accepted;
            invitation.AcceptedAt = now;
            invitation.AcceptedByUserId = actorUserId;
            invitation.InvitationTokenHash = null;
            invitation.UpdatedAt = now;
            invitation.UpdatedByUserId = actorUserId;
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Id == userId,
            cancellationToken);

        if (user is null)
        {
            user = new UserEntity
            {
                Id = userId,
                TenantId = invitation.TenantId,
                Email = email,
                DisplayName = displayName,
                Status = UserStatus.Active,
                MfaEnabled = false,
                CreatedAt = now,
                CreatedByUserId = actorUserId
            };
            dbContext.Users.Add(user);
        }
        else
        {
            user.Email = email;
            user.DisplayName = displayName;
            user.Status = UserStatus.Active;
            user.UpdatedAt = now;
            user.UpdatedByUserId = actorUserId;
        }

        var membershipExists = await dbContext.TenantMemberships.AnyAsync(
            membership => membership.TenantId == invitation.TenantId && membership.UserId == userId,
            cancellationToken);

        if (!membershipExists)
        {
            dbContext.TenantMemberships.Add(new TenantMembershipEntity
            {
                Id = Guid.NewGuid(),
                TenantId = invitation.TenantId,
                UserId = userId,
                Status = MembershipStatus.Active,
                RoleName = invitation.RoleName,
                CreatedAt = now,
                CreatedByUserId = actorUserId
            });
        }

        var onboarding = await dbContext.PlatformTenantOnboardings
            .SingleOrDefaultAsync(candidate => candidate.InvitationId == invitation.Id, cancellationToken);
        if (onboarding is not null)
        {
            var tenant = await dbContext.Tenants
                .SingleAsync(candidate => candidate.Id == invitation.TenantId, cancellationToken);
            onboarding.Status = TenantOnboardingStatus.Active;
            onboarding.UpdatedAt = now;
            onboarding.UpdatedByUserId = actorUserId;
            tenant.Status = onboarding.OnboardingType is TenantOnboardingType.Pilot
                ? TenantStatus.Trialing
                : TenantStatus.Active;
            tenant.UpdatedAt = now;
            tenant.UpdatedByUserId = actorUserId;

            var metadata = new Dictionary<string, string>
            {
                ["onboardingId"] = onboarding.Id.ToString(),
                ["onboardingType"] = onboarding.OnboardingType.ToString(),
                ["tenantStatus"] = tenant.Status.ToString(),
                ["ownerUserId"] = userId.ToString(),
                ["roleName"] = invitation.RoleName
            };
            if (!string.IsNullOrWhiteSpace(requestMetadata.CorrelationId))
            {
                metadata["correlationId"] = requestMetadata.CorrelationId;
            }

            dbContext.AuditLogEntries.Add(new AuditLogEntryEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                ActorUserId = actorUserId,
                Action = AuditAction.Updated,
                EntityType = "PlatformTenantOnboarding",
                EntityId = onboarding.Id.ToString(),
                OccurredAt = now,
                IpAddress = requestMetadata.IpAddress,
                UserAgent = requestMetadata.UserAgent,
                CorrelationId = requestMetadata.CorrelationId,
                Summary = "The initial Owner accepted the invitation and tenant onboarding was activated.",
                MetadataJson = JsonSerializer.Serialize(metadata, JsonOptions)
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return ToDto(invitation);
    }

    public async Task<TenantInvitationDto?> ExpireInCurrentTenantScopeAsync(
        Guid invitationId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var invitation = await dbContext.TenantInvitations
            .SingleOrDefaultAsync(
                candidate => candidate.Id == invitationId && candidate.TenantId == tenantContext.TenantId,
                cancellationToken);

        if (invitation is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        invitation.Status = TenantInvitationStatus.Expired;
        invitation.UpdatedAt = now;
        invitation.UpdatedByUserId = actorUserId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(invitation);
    }

    public async Task<TenantInvitationDto?> RevokeInCurrentTenantScopeAsync(
        Guid invitationId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var invitation = await dbContext.TenantInvitations
            .SingleOrDefaultAsync(
                candidate => candidate.Id == invitationId && candidate.TenantId == tenantContext.TenantId,
                cancellationToken);

        if (invitation is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        invitation.Status = TenantInvitationStatus.Revoked;
        invitation.RevokedAt = now;
        invitation.RevokedByUserId = actorUserId;
        invitation.UpdatedAt = now;
        invitation.UpdatedByUserId = actorUserId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(invitation);
    }

    public Task<TenantInvitationDto?> QueueCurrentTenantDeliveryAsync(
        Guid invitationId,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        QueueDeliveryAsync(invitationId, tenantContext.TenantId, actorUserId, cancellationToken);

    public Task<TenantInvitationDto?> QueuePlatformDeliveryAsync(
        Guid invitationId,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        QueueDeliveryAsync(invitationId, null, actorUserId, cancellationToken);

    private async Task<TenantInvitationDto?> QueueDeliveryAsync(
        Guid invitationId,
        Guid? requiredTenantId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var invitation = await dbContext.TenantInvitations.SingleOrDefaultAsync(
            candidate => candidate.Id == invitationId &&
                         (!requiredTenantId.HasValue || candidate.TenantId == requiredTenantId.Value),
            cancellationToken);
        if (invitation is null)
        {
            return null;
        }

        if (invitation.Status is not TenantInvitationStatus.Pending || invitation.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new InvalidInvitationStateException("Only unexpired pending invitations can be resent.");
        }

        if (invitation.DeliveryStatus is InvitationDeliveryStatus.Processing)
        {
            throw new InvalidInvitationStateException("An invitation cannot be resent while delivery is in progress.");
        }

        var now = DateTimeOffset.UtcNow;
        invitation.DeliveryStatus = InvitationDeliveryStatus.Queued;
        invitation.NextDeliveryAttemptAt = now;
        invitation.DeliveryLeaseUntil = null;
        invitation.DeliveryFailureCode = null;
        invitation.DeliveryProviderMessageId = null;
        invitation.NotificationSentAt = null;
        invitation.NotificationPlaceholder = "Owner invitation is queued for delivery.";
        invitation.UpdatedAt = now;
        invitation.UpdatedByUserId = actorUserId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(invitation);
    }

    private static TenantInvitationDto ToDto(TenantInvitationEntity invitation) =>
        new(
            invitation.Id,
            invitation.TenantId,
            invitation.Email,
            invitation.RoleName,
            invitation.Status,
            invitation.ExpiresAt,
            invitation.AcceptedAt,
            invitation.AcceptedByUserId,
            invitation.RevokedAt,
            invitation.RevokedByUserId,
            invitation.NotificationSentAt,
            invitation.NotificationPlaceholder,
            invitation.DeliveryStatus,
            invitation.DeliveryAttemptCount,
            invitation.NextDeliveryAttemptAt,
            invitation.LastDeliveryAttemptAt,
            invitation.DeliveryFailureCode,
            invitation.CreatedAt,
            invitation.UpdatedAt);
}
