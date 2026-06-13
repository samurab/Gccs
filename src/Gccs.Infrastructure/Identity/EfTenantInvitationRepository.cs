using Gccs.Application.Identity;
using Gccs.Application.Security;
using Gccs.Domain.Identity;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Identity;

public sealed class EfTenantInvitationRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : ITenantInvitationRepository
{
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
            InvitationToken = invitation.InvitationToken,
            Status = invitation.Status,
            ExpiresAt = invitation.ExpiresAt,
            AcceptedAt = invitation.AcceptedAt,
            AcceptedByUserId = invitation.AcceptedByUserId,
            RevokedAt = invitation.RevokedAt,
            RevokedByUserId = invitation.RevokedByUserId,
            NotificationSentAt = invitation.NotificationSentAt,
            NotificationPlaceholder = invitation.NotificationPlaceholder,
            CreatedAt = invitation.Audit.CreatedAt,
            CreatedByUserId = invitation.Audit.CreatedByUserId
        };

        dbContext.TenantInvitations.Add(invitationEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(invitationEntity);
    }

    public async Task<TenantInvitationDto?> FindByTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var invitation = await dbContext.TenantInvitations
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.InvitationToken == token, cancellationToken);

        return invitation is null ? null : ToDto(invitation);
    }

    public async Task<TenantInvitationDto?> AcceptAsync(
        Guid invitationId,
        Guid userId,
        string email,
        string displayName,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var invitation = await dbContext.TenantInvitations
            .SingleOrDefaultAsync(candidate => candidate.Id == invitationId, cancellationToken);

        if (invitation is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        invitation.Status = TenantInvitationStatus.Accepted;
        invitation.AcceptedAt = now;
        invitation.AcceptedByUserId = actorUserId;
        invitation.UpdatedAt = now;
        invitation.UpdatedByUserId = actorUserId;

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

        await dbContext.SaveChangesAsync(cancellationToken);

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

    private static TenantInvitationDto ToDto(TenantInvitationEntity invitation) =>
        new(
            invitation.Id,
            invitation.TenantId,
            invitation.Email,
            invitation.RoleName,
            invitation.InvitationToken,
            invitation.Status,
            invitation.ExpiresAt,
            invitation.AcceptedAt,
            invitation.AcceptedByUserId,
            invitation.RevokedAt,
            invitation.RevokedByUserId,
            invitation.NotificationSentAt,
            invitation.NotificationPlaceholder,
            invitation.CreatedAt,
            invitation.UpdatedAt);
}
