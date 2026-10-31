using Gccs.Domain.Identity;

namespace Gccs.Application.Notifications;

public static class NotificationPreferenceDefaults
{
    public static NotificationPreferenceUpdateRequest ForRole(string roleName)
    {
        var normalized = RoleCatalog.TryNormalizeRoleName(roleName, out var canonicalRoleName)
            ? canonicalRoleName
            : RoleCatalog.Contributor;

        return normalized switch
        {
            RoleCatalog.Auditor => new(false, true, true, false, false, false),
            RoleCatalog.Contributor => new(true, true, true, true, false, false),
            _ => new(true, true, true, true, true, true)
        };
    }
}

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
