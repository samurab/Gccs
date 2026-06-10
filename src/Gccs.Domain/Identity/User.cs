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
