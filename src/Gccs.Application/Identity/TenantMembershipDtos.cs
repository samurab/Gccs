using Gccs.Domain.Identity;

namespace Gccs.Application.Identity;

public sealed record TenantMemberDto(
    Guid MembershipId,
    Guid TenantId,
    Guid UserId,
    string Email,
    string DisplayName,
    UserStatus UserStatus,
    MembershipStatus MembershipStatus,
    string RoleName,
    bool MfaEnabled,
    DateTimeOffset? LastSignedInAt,
    DateTimeOffset? LastAccessedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record AssignTenantMemberRequest(
    Guid UserId,
    string Email,
    string DisplayName,
    string RoleName,
    MembershipStatus Status = MembershipStatus.Active);

public sealed record UpdateTenantMembershipStatusRequest(MembershipStatus Status);
