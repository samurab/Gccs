using System.Text.Json;
using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Domain.Audit;

namespace Gccs.Application.Reports;

public sealed class ReportLifecycleService(
    IReportRepository repository,
    IAuditEventWriter auditEventWriter,
    IApplicationTransaction transaction)
{
    public Task<ReportArtifactDetailDto?> ArchiveAsync(
        Guid reportId,
        ReportLifecycleRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        SetArchiveStateAsync(reportId, request, actorUserId, archived: true, cancellationToken: cancellationToken);

    public Task<ReportArtifactDetailDto?> RestoreAsync(
        Guid reportId,
        ReportLifecycleRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        SetArchiveStateAsync(reportId, request, actorUserId, archived: false, cancellationToken: cancellationToken);

    private Task<ReportArtifactDetailDto?> SetArchiveStateAsync(
        Guid reportId,
        ReportLifecycleRequest request,
        Guid actorUserId,
        bool archived,
        CancellationToken cancellationToken)
    {
        var reason = ValidateReason(request);
        return transaction.ExecuteAsync(async transactionCancellationToken =>
        {
            var transition = await repository.SetArchiveStateAsync(
                reportId,
                archived,
                actorUserId,
                reason,
                transactionCancellationToken);
            if (transition is null || !transition.Changed)
            {
                return transition?.Report;
            }

            var action = archived ? AuditAction.Archived : AuditAction.Updated;
            var operation = archived ? "archived" : "restored";
            await auditEventWriter.WriteChangeAsync(
                transition.Report.TenantId,
                actorUserId,
                action,
                "Report",
                transition.Report.Id.ToString(),
                $"Report was {operation}.",
                JsonSerializer.Serialize(new { status = transition.PreviousStatus.ToString() }),
                JsonSerializer.Serialize(new { status = transition.Report.Status.ToString() }),
                new Dictionary<string, string>
                {
                    ["reason"] = reason,
                    ["reportType"] = transition.Report.Type.ToString(),
                    ["operation"] = operation
                },
                transactionCancellationToken);
            return transition.Report;
        }, cancellationToken);
    }

    private static string ValidateReason(ReportLifecycleRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ReportLifecycleValidationException("A reason is required.");
        }

        var reason = request.Reason.Trim();
        if (reason.Length > 500)
        {
            throw new ReportLifecycleValidationException("The reason must be 500 characters or fewer.");
        }

        return reason;
    }
}
