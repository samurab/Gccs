namespace Gccs.Application.Notifications;

public sealed record RunDueDateReminderRequest(
    int? LeadTimeDays,
    Guid? SimulatedFailureTaskId);

public sealed record DueDateReminderRunResult(
    int UpcomingSelected,
    int OverdueSelected,
    int Created,
    int Skipped,
    int Failed,
    IReadOnlyList<DueDateReminderResultItem> Items);

public sealed record DueDateReminderResultItem(
    Guid TaskId,
    string Title,
    string Category,
    string Status,
    string Placeholder,
    string? FailureMessage);

public interface IDueDateReminderRepository
{
    Task<DueDateReminderRunResult> RunAsync(
        Guid tenantId,
        Guid actorUserId,
        RunDueDateReminderRequest request,
        CancellationToken cancellationToken = default);
}
