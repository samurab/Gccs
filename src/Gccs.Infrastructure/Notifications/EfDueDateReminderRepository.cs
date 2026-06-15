using Gccs.Application.Notifications;
using Gccs.Domain.Compliance;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Notifications;

public sealed class EfDueDateReminderRepository(GccsDbContext dbContext) : IDueDateReminderRepository
{
    public async Task<DueDateReminderRunResult> RunAsync(
        Guid tenantId,
        Guid actorUserId,
        RunDueDateReminderRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var leadTimeDays = Math.Clamp(request.LeadTimeDays ?? 14, 0, 365);
        var leadDate = today.AddDays(leadTimeDays);
        var tasks = await dbContext.ComplianceTasks
            .AsNoTracking()
            .Where(task =>
                task.TenantId == tenantId &&
                task.DueAt.HasValue &&
                task.Status != ComplianceTaskStatus.Done &&
                task.Status != ComplianceTaskStatus.Canceled &&
                task.Status != ComplianceTaskStatus.Blocked &&
                task.DueAt.Value <= leadDate)
            .OrderBy(task => task.DueAt)
            .ToArrayAsync(cancellationToken);
        var existingKeys = await dbContext.NotificationDeliveries
            .AsNoTracking()
            .Where(delivery => delivery.TenantId == tenantId)
            .Select(delivery => delivery.SourceTaskId.ToString() + "|" + delivery.Category)
            .ToHashSetAsync(cancellationToken);
        var items = new List<DueDateReminderResultItem>();

        foreach (var task in tasks)
        {
            var category = task.DueAt!.Value < today ? "overdue" : "upcoming";
            var key = $"{task.Id}|{category}";
            if (existingKeys.Contains(key))
            {
                items.Add(new DueDateReminderResultItem(task.Id, task.Title, category, "Skipped", string.Empty, null));
                continue;
            }

            var failed = request.SimulatedFailureTaskId == task.Id;
            var placeholder = failed
                ? string.Empty
                : $"Local {category} reminder queued for task '{task.Title}' due {task.DueAt:O}.";
            var failureMessage = failed ? "Simulated reminder delivery failure." : null;
            dbContext.NotificationDeliveries.Add(new NotificationDeliveryEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = task.AssignedToUserId ?? actorUserId,
                SourceTaskId = task.Id,
                Category = category,
                Status = failed ? "Failed" : "Delivered",
                Placeholder = placeholder,
                FailureMessage = failureMessage,
                AttemptedAt = now,
                CreatedAt = now,
                CreatedByUserId = actorUserId
            });
            items.Add(new DueDateReminderResultItem(
                task.Id,
                task.Title,
                category,
                failed ? "Failed" : "Delivered",
                placeholder,
                failureMessage));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new DueDateReminderRunResult(
            tasks.Count(task => task.DueAt!.Value >= today),
            tasks.Count(task => task.DueAt!.Value < today),
            items.Count(item => item.Status == "Delivered"),
            items.Count(item => item.Status == "Skipped"),
            items.Count(item => item.Status == "Failed"),
            items);
    }
}
