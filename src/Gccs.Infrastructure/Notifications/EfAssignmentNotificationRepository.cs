using Gccs.Application.Notifications;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Notifications;

public sealed class EfAssignmentNotificationRepository(GccsDbContext dbContext) : IAssignmentNotificationRepository
{
    public async Task EmitTaskAssignmentAsync(
        Guid tenantId,
        Guid taskId,
        Guid assignedUserId,
        string taskTitle,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.NotificationDeliveries.AnyAsync(
            delivery =>
                delivery.TenantId == tenantId &&
                delivery.SourceTaskId == taskId &&
                delivery.Category == "assignment" &&
                delivery.UserId == assignedUserId,
            cancellationToken);
        if (exists)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        dbContext.NotificationDeliveries.Add(new NotificationDeliveryEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = assignedUserId,
            SourceTaskId = taskId,
            SourceType = "ComplianceTask",
            LinkUrl = $"/tasks/{taskId}",
            Category = "assignment",
            Status = "Delivered",
            Placeholder = $"Task '{taskTitle}' was assigned to you.",
            AttemptedAt = now,
            CreatedAt = now,
            CreatedByUserId = actorUserId
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task EmitExpertReviewAssignmentAsync(
        Guid tenantId,
        Guid expertReviewItemId,
        Guid assignedUserId,
        string topic,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.NotificationDeliveries.AnyAsync(
            delivery =>
                delivery.TenantId == tenantId &&
                delivery.SourceTaskId == expertReviewItemId &&
                delivery.Category == "expert_review" &&
                delivery.UserId == assignedUserId,
            cancellationToken);
        if (exists)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        dbContext.NotificationDeliveries.Add(new NotificationDeliveryEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = assignedUserId,
            SourceTaskId = expertReviewItemId,
            SourceType = "ExpertReviewItem",
            LinkUrl = $"/expert-review/{expertReviewItemId}",
            Category = "expert_review",
            Status = "Delivered",
            Placeholder = $"Expert review '{topic}' was assigned to you.",
            AttemptedAt = now,
            CreatedAt = now,
            CreatedByUserId = actorUserId
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationCenterItemDto>> ListCurrentUserAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await dbContext.NotificationDeliveries
            .AsNoTracking()
            .Where(delivery => delivery.TenantId == tenantId && delivery.UserId == userId)
            .OrderByDescending(delivery => delivery.AttemptedAt)
            .Select(delivery => ToDto(delivery))
            .ToArrayAsync(cancellationToken);

    public async Task<NotificationCenterItemDto?> MarkReadAsync(
        Guid tenantId,
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.NotificationDeliveries.SingleOrDefaultAsync(
            delivery =>
                delivery.Id == notificationId &&
                delivery.TenantId == tenantId &&
                delivery.UserId == userId,
            cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.ReadAt ??= DateTimeOffset.UtcNow;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedByUserId = userId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    private static NotificationCenterItemDto ToDto(NotificationDeliveryEntity entity) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.UserId,
            entity.SourceTaskId,
            entity.SourceType,
            entity.LinkUrl,
            entity.Category,
            entity.Status,
            entity.Placeholder,
            entity.AttemptedAt,
            entity.ReadAt);
}
