using Gccs.Application.Audit;
using Gccs.Domain.Audit;

namespace Gccs.Application.Notifications;

public sealed class DueDateReminderService(
    IDueDateReminderRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public async Task<DueDateReminderRunResult> RunAsync(
        Guid tenantId,
        Guid actorUserId,
        RunDueDateReminderRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await repository.RunAsync(tenantId, actorUserId, request, cancellationToken);
        await auditEventWriter.WriteAsync(
            tenantId,
            actorUserId,
            AuditAction.Created,
            "DueDateReminderRun",
            Guid.NewGuid().ToString(),
            "Due-date reminder job was run.",
            new Dictionary<string, string>
            {
                ["upcomingSelected"] = result.UpcomingSelected.ToString(),
                ["overdueSelected"] = result.OverdueSelected.ToString(),
                ["created"] = result.Created.ToString(),
                ["skipped"] = result.Skipped.ToString(),
                ["failed"] = result.Failed.ToString()
            },
            cancellationToken);
        return result;
    }
}
