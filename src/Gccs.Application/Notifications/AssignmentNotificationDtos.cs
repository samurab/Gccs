namespace Gccs.Application.Notifications;

public static class AssignmentNotificationRoutes
{
    public const string Calendar = "/app#/calendar";
    public const string Obligations = "/app#/obligations";

    public static string NormalizeWorkspaceLink(string linkUrl) =>
        linkUrl.StartsWith("/#/", StringComparison.Ordinal)
            ? $"/app{linkUrl[1..]}"
            : linkUrl;
}

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

public sealed record AssignmentNotificationEmission(
    bool InAppNotificationCreated,
    bool EmailDeliveryQueued,
    int InAppRecipientCount = 0);

public interface IAssignmentNotificationRepository
{
    Task<AssignmentNotificationEmission> EmitTaskAssignmentAsync(
        Guid tenantId,
        Guid taskId,
        Guid assignedUserId,
        string taskTitle,
        Guid actorUserId,
        bool queueEmail = false,
        string linkUrl = AssignmentNotificationRoutes.Calendar,
        CancellationToken cancellationToken = default);

    Task<AssignmentNotificationEmission> EmitRoleTaskAssignmentAsync(
        Guid tenantId,
        Guid taskId,
        string roleName,
        string taskTitle,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task EmitExpertReviewAssignmentAsync(
        Guid tenantId,
        Guid expertReviewItemId,
        Guid assignedUserId,
        string topic,
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
