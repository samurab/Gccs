using Gccs.Application.Notifications;
using Gccs.Domain.Identity;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Notifications;

public sealed class EfAssignmentNotificationRepository(GccsDbContext dbContext) : IAssignmentNotificationRepository
{
    public async Task<AssignmentNotificationEmission> EmitTaskAssignmentAsync(
        Guid tenantId,
        Guid taskId,
        Guid assignedUserId,
        string taskTitle,
        Guid actorUserId,
        bool queueEmail = false,
        string linkUrl = AssignmentNotificationRoutes.Calendar,
        CancellationToken cancellationToken = default)
    {
        var notification = await dbContext.NotificationDeliveries.SingleOrDefaultAsync(
            delivery =>
                delivery.TenantId == tenantId &&
                delivery.SourceTaskId == taskId &&
                delivery.Category == "assignment" &&
                delivery.UserId == assignedUserId,
            cancellationToken);
        var notificationCreated = notification is null;
        if (notification is null)
        {
            var now = DateTimeOffset.UtcNow;
            notification = new NotificationDeliveryEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = assignedUserId,
                SourceTaskId = taskId,
                SourceType = "ComplianceTask",
                LinkUrl = linkUrl,
                Category = "assignment",
                Status = "Delivered",
                Placeholder = $"Task '{taskTitle}' was assigned to you.",
                AttemptedAt = now,
                CreatedAt = now,
                CreatedByUserId = actorUserId
            };
            dbContext.NotificationDeliveries.Add(notification);
        }

        var emailQueued = false;
        if (queueEmail && !await dbContext.AssignmentEmailDeliveries.AnyAsync(
                delivery => delivery.NotificationDeliveryId == notification.Id,
                cancellationToken))
        {
            var recipient = await dbContext.TenantMemberships
                .AsNoTracking()
                .Where(membership =>
                    membership.TenantId == tenantId &&
                    membership.UserId == assignedUserId &&
                    membership.Status == MembershipStatus.Active &&
                    membership.User != null &&
                    membership.User.Status == UserStatus.Active)
                .Select(membership => new
                {
                    membership.RoleName,
                    membership.User!.Email,
                    membership.User.DisplayName
                })
                .SingleOrDefaultAsync(cancellationToken);
            var preference = await dbContext.NotificationPreferences
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    candidate => candidate.TenantId == tenantId && candidate.UserId == assignedUserId,
                    cancellationToken);
            var emailEnabled = preference?.AssignmentNotificationsEnabled ??
                !string.Equals(recipient?.RoleName, RoleCatalog.Auditor, StringComparison.OrdinalIgnoreCase);

            if (recipient is not null && emailEnabled && !string.IsNullOrWhiteSpace(recipient.Email))
            {
                var now = DateTimeOffset.UtcNow;
                dbContext.AssignmentEmailDeliveries.Add(new AssignmentEmailDeliveryEntity
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    NotificationDeliveryId = notification.Id,
                    UserId = assignedUserId,
                    RecipientEmail = recipient.Email,
                    RecipientDisplayName = string.IsNullOrWhiteSpace(recipient.DisplayName)
                        ? recipient.Email
                        : recipient.DisplayName,
                    LinkUrl = linkUrl,
                    Status = "Queued",
                    NextAttemptAt = now,
                    CreatedAt = now,
                    CreatedByUserId = actorUserId
                });
                emailQueued = true;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new AssignmentNotificationEmission(notificationCreated, emailQueued, notificationCreated ? 1 : 0);
    }

    public async Task<AssignmentNotificationEmission> EmitRoleTaskAssignmentAsync(
        Guid tenantId,
        Guid taskId,
        string roleName,
        string taskTitle,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (!RoleCatalog.TryNormalizeRoleName(roleName, out var canonicalRoleName))
        {
            return new AssignmentNotificationEmission(false, false, 0);
        }

        var matchingRoleNames = string.Equals(
            canonicalRoleName,
            RoleCatalog.ComplianceManager,
            StringComparison.OrdinalIgnoreCase)
            ? new[] { RoleCatalog.ComplianceManager, "ComplianceManager" }
            : new[] { canonicalRoleName };
        var recipientUserIds = await dbContext.TenantMemberships
            .AsNoTracking()
            .Where(membership =>
                membership.TenantId == tenantId &&
                membership.Status == MembershipStatus.Active &&
                matchingRoleNames.Contains(membership.RoleName) &&
                membership.User != null &&
                membership.User.Status == UserStatus.Active)
            .Select(membership => membership.UserId)
            .Distinct()
            .ToArrayAsync(cancellationToken);
        if (recipientUserIds.Length == 0)
        {
            return new AssignmentNotificationEmission(false, false, 0);
        }

        var existingUserIds = await dbContext.NotificationDeliveries
            .AsNoTracking()
            .Where(delivery =>
                delivery.TenantId == tenantId &&
                delivery.SourceTaskId == taskId &&
                delivery.Category == "role_assignment" &&
                recipientUserIds.Contains(delivery.UserId))
            .Select(delivery => delivery.UserId)
            .ToArrayAsync(cancellationToken);
        var existing = existingUserIds.ToHashSet();
        var now = DateTimeOffset.UtcNow;
        var createdCount = 0;
        foreach (var recipientUserId in recipientUserIds)
        {
            if (existing.Contains(recipientUserId))
            {
                continue;
            }

            dbContext.NotificationDeliveries.Add(new NotificationDeliveryEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = recipientUserId,
                SourceTaskId = taskId,
                SourceType = "ComplianceTask",
                LinkUrl = AssignmentNotificationRoutes.Obligations,
                Category = "role_assignment",
                Status = "Delivered",
                Placeholder = $"New obligation assigned to the {canonicalRoleName} queue.",
                AttemptedAt = now,
                CreatedAt = now,
                CreatedByUserId = actorUserId
            });
            createdCount++;
        }

        if (createdCount > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new AssignmentNotificationEmission(createdCount > 0, false, createdCount);
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
