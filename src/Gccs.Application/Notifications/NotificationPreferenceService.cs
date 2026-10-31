using Gccs.Application.Audit;
using Gccs.Domain.Audit;

namespace Gccs.Application.Notifications;

public sealed class NotificationPreferenceService(
    INotificationPreferenceRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public Task<NotificationPreferenceDto> GetOrCreateAsync(
        Guid tenantId,
        Guid userId,
        string roleName,
        CancellationToken cancellationToken = default) =>
        repository.GetOrCreateAsync(tenantId, userId, roleName, cancellationToken);

    public async Task<NotificationPreferenceDto> UpdateAsync(
        Guid tenantId,
        Guid userId,
        string roleName,
        NotificationPreferenceUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var preferences = await repository.UpdateAsync(tenantId, userId, roleName, request, cancellationToken);
        await auditEventWriter.WriteAsync(
            tenantId,
            userId,
            AuditAction.Updated,
            "NotificationPreference",
            preferences.Id.ToString(),
            "Notification preferences were updated.",
            new Dictionary<string, string>
            {
                ["assignmentNotificationsEnabled"] = preferences.AssignmentNotificationsEnabled.ToString(),
                ["dueSoonNotificationsEnabled"] = preferences.DueSoonNotificationsEnabled.ToString(),
                ["overdueNotificationsEnabled"] = preferences.OverdueNotificationsEnabled.ToString(),
                ["evidenceRequestNotificationsEnabled"] = preferences.EvidenceRequestNotificationsEnabled.ToString(),
                ["certificationRenewalNotificationsEnabled"] = preferences.CertificationRenewalNotificationsEnabled.ToString(),
                ["cmmcAffirmationNotificationsEnabled"] = preferences.CmmcAffirmationNotificationsEnabled.ToString()
            },
            cancellationToken);
        return preferences;
    }
}
