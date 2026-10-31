using Gccs.Application.Notifications;
using Gccs.Domain.Compliance;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Gccs.Infrastructure.Notifications;

public sealed class EfDueDateReminderRepository(GccsDbContext dbContext) : IDueDateReminderRepository
{
    public async Task<DueDateReminderRunResult> RunAsync(
        Guid tenantId,
        Guid actorUserId,
        RunDueDateReminderRequest request,
        CancellationToken cancellationToken = default)
    {
        return await RunCoreAsync(tenantId, actorUserId, request, null, cancellationToken);
    }

    public async Task<int> RunAutomatedAsync(int leadTimeDays, int batchSize, CancellationToken cancellationToken = default)
    {
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        if (!await TryAcquireSchedulerLockAsync(cancellationToken)) return 0;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var leadDate = today.AddDays(Math.Clamp(leadTimeDays, 0, 365));
        var boundedBatchSize = Math.Clamp(batchSize, 1, 1000);
        var candidates = await dbContext.ComplianceTasks.AsNoTracking()
            .Where(task => task.DueAt.HasValue && task.DueAt.Value <= leadDate && task.AssignedToUserId.HasValue &&
                task.Status != ComplianceTaskStatus.Done && task.Status != ComplianceTaskStatus.Canceled && task.Status != ComplianceTaskStatus.Blocked &&
                !dbContext.NotificationDeliveries.Any(delivery => delivery.TenantId == task.TenantId && delivery.SourceTaskId == task.Id &&
                    delivery.UserId == task.AssignedToUserId.Value && delivery.Category == (task.DueAt.Value < today ? "overdue" : "upcoming")))
            .OrderBy(task => task.DueAt)
            .Select(task => new { TaskId = task.Id, task.TenantId, ActorUserId = task.AssignedToUserId!.Value })
            .Take(boundedBatchSize)
            .ToArrayAsync(cancellationToken);
        var created = 0;
        foreach (var tenantCandidates in candidates.GroupBy(candidate => candidate.TenantId))
        {
            var result = await RunCoreAsync(
                tenantCandidates.Key,
                tenantCandidates.First().ActorUserId,
                new(leadTimeDays, null),
                tenantCandidates.Select(candidate => candidate.TaskId).ToArray(),
                cancellationToken);
            created += result.Created;
        }
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return created;
    }

    private async Task<DueDateReminderRunResult> RunCoreAsync(
        Guid tenantId,
        Guid actorUserId,
        RunDueDateReminderRequest request,
        IReadOnlyCollection<Guid>? eligibleTaskIds,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var leadTimeDays = Math.Clamp(request.LeadTimeDays ?? 14, 0, 365);
        var leadDate = today.AddDays(leadTimeDays);
        IQueryable<ComplianceTaskEntity> taskQuery = dbContext.ComplianceTasks
            .AsNoTracking()
            .Where(task =>
                task.TenantId == tenantId &&
                (eligibleTaskIds == null || eligibleTaskIds.Contains(task.Id)) &&
                task.DueAt.HasValue &&
                task.Status != ComplianceTaskStatus.Done &&
                task.Status != ComplianceTaskStatus.Canceled &&
                task.Status != ComplianceTaskStatus.Blocked &&
                task.DueAt.Value <= leadDate)
            .OrderBy(task => task.DueAt);
        var tasks = await taskQuery.ToArrayAsync(cancellationToken);
        var taskIds = tasks.Select(task => task.Id).ToArray();
        var existingKeys = await dbContext.NotificationDeliveries
            .AsNoTracking()
            .Where(delivery => delivery.TenantId == tenantId && taskIds.Contains(delivery.SourceTaskId))
            .Select(delivery => delivery.SourceTaskId.ToString() + "|" + delivery.Category + "|" + delivery.UserId.ToString())
            .ToHashSetAsync(cancellationToken);
        var items = new List<DueDateReminderResultItem>();

        foreach (var task in tasks)
        {
            var category = task.DueAt!.Value < today ? "overdue" : "upcoming";
            var recipientUserId = task.AssignedToUserId ?? actorUserId;
            var key = $"{task.Id}|{category}|{recipientUserId}";
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
                UserId = recipientUserId,
                SourceTaskId = task.Id,
                SourceType = "ComplianceTask",
                LinkUrl = $"/tasks/{task.Id}",
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

    private async Task<bool> TryAcquireSchedulerLockAsync(CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsNpgsql()) return true;
        var connection = dbContext.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = dbContext.Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText = "SELECT pg_try_advisory_xact_lock(684322, 1202)";
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }
}
