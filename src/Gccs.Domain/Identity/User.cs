using Gccs.Domain.Common;

namespace Gccs.Domain.Identity;

public sealed record User(
    Guid Id,
    Guid TenantId,
    string Email,
    string DisplayName,
    UserStatus Status,
    bool MfaEnabled,
    IReadOnlyList<Guid> RoleIds,
    DateTimeOffset? LastSignedInAt,
    EntityAudit Audit);

public enum UserStatus
{
    Invited,
    Active,
    Disabled
}

public sealed record TenantMembership(
    Guid Id,
    Guid TenantId,
    Guid UserId,
    MembershipStatus Status,
    string RoleName,
    DateTimeOffset? LastAccessedAt,
    EntityAudit Audit);

public enum MembershipStatus
{
    Active,
    Suspended,
    Deactivated
}

public sealed record TenantInvitation(
    Guid Id,
    Guid TenantId,
    string Email,
    string RoleName,
    string InvitationToken,
    TenantInvitationStatus Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? AcceptedAt,
    Guid? AcceptedByUserId,
    DateTimeOffset? RevokedAt,
    Guid? RevokedByUserId,
    DateTimeOffset? NotificationSentAt,
    string NotificationPlaceholder,
    EntityAudit Audit);

public enum TenantInvitationStatus
{
    Pending,
    Accepted,
    Expired,
    Revoked
}
