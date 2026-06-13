using Gccs.Domain.Identity;

namespace Gccs.Application.Identity;

public sealed record TenantInvitationDto(
    Guid InvitationId,
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
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record CreateTenantInvitationRequest(
    string Email,
    string RoleName,
    int ExpiresInDays = 7);

public sealed record AcceptTenantInvitationRequest(string DisplayName);
