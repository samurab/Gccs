namespace Gccs.Application.Notifications;

public sealed record NotificationPreferenceDto(
    Guid Id,
    Guid TenantId,
    Guid UserId,
    string RoleName,
    bool AssignmentNotificationsEnabled,
    bool DueSoonNotificationsEnabled,
    bool OverdueNotificationsEnabled,
    bool EvidenceRequestNotificationsEnabled,
    bool CertificationRenewalNotificationsEnabled,
    bool CmmcAffirmationNotificationsEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record NotificationPreferenceUpdateRequest(
    bool AssignmentNotificationsEnabled,
    bool DueSoonNotificationsEnabled,
    bool OverdueNotificationsEnabled,
    bool EvidenceRequestNotificationsEnabled,
    bool CertificationRenewalNotificationsEnabled,
    bool CmmcAffirmationNotificationsEnabled);

public interface INotificationPreferenceRepository
{
    Task<NotificationPreferenceDto> GetOrCreateAsync(
        Guid tenantId,
        Guid userId,
        string roleName,
        CancellationToken cancellationToken = default);

    Task<NotificationPreferenceDto> UpdateAsync(
        Guid tenantId,
        Guid userId,
        string roleName,
        NotificationPreferenceUpdateRequest request,
        CancellationToken cancellationToken = default);
}
