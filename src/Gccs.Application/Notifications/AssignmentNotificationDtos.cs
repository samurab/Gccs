namespace Gccs.Application.Notifications;

public sealed record NotificationCenterItemDto(
    Guid Id,
    Guid TenantId,
    Guid UserId,
    Guid SourceTaskId,
    string SourceType,
    string LinkUrl,
    string Category,
    string Status,
    string Placeholder,
    DateTimeOffset AttemptedAt,
    DateTimeOffset? ReadAt);

public interface IAssignmentNotificationRepository
{
    Task EmitTaskAssignmentAsync(
        Guid tenantId,
        Guid taskId,
        Guid assignedUserId,
        string taskTitle,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationCenterItemDto>> ListCurrentUserAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<NotificationCenterItemDto?> MarkReadAsync(
        Guid tenantId,
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken = default);
}
