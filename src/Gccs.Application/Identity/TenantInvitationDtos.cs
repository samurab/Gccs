using Gccs.Domain.Identity;

namespace Gccs.Application.Identity;

public sealed record TenantInvitationDto(
    Guid InvitationId,
    Guid TenantId,
    string Email,
    string RoleName,
    TenantInvitationStatus Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? AcceptedAt,
    Guid? AcceptedByUserId,
    DateTimeOffset? RevokedAt,
    Guid? RevokedByUserId,
    DateTimeOffset? NotificationSentAt,
    string NotificationPlaceholder,
    InvitationDeliveryStatus DeliveryStatus,
    int DeliveryAttemptCount,
    DateTimeOffset? NextDeliveryAttemptAt,
    DateTimeOffset? LastDeliveryAttemptAt,
    string? DeliveryFailureCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record CreateTenantInvitationRequest(
    string Email,
    string RoleName,
    int ExpiresInDays = 7);

public sealed record AcceptTenantInvitationRequest(string DisplayName);

public sealed record InvitationAcceptanceContextDto(
    Guid InvitationId,
    string TenantDisplayName,
    string Email,
    string RoleName,
    TenantInvitationStatus Status,
    DateTimeOffset ExpiresAt);
