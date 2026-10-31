using Gccs.Application.Audit;
using Gccs.Domain.Audit;

namespace Gccs.Application.Notifications;

public sealed class AssignmentNotificationService(
    IAssignmentNotificationRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public Task<IReadOnlyList<NotificationCenterItemDto>> ListCurrentUserAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        repository.ListCurrentUserAsync(tenantId, userId, cancellationToken);

    public async Task<NotificationCenterItemDto?> MarkReadAsync(
        Guid tenantId,
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var item = await repository.MarkReadAsync(tenantId, userId, notificationId, cancellationToken);
        if (item is not null)
        {
            await auditEventWriter.WriteAsync(
                tenantId,
                userId,
                AuditAction.Updated,
                "Notification",
                notificationId.ToString(),
                "Notification was marked read.",
                new Dictionary<string, string>
                {
                    ["category"] = item.Category,
                    ["sourceType"] = item.SourceType,
                    ["sourceTaskId"] = item.SourceTaskId.ToString()
                },
                cancellationToken);
        }

        return item;
    }
}
