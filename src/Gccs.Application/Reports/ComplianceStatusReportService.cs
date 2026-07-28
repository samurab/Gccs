using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;

namespace Gccs.Application.Reports;

public sealed class ComplianceStatusReportService(
    IReportRepository repository,
    IAuditEventWriter auditEventWriter,
    TenantDataHandlingModePolicyService dataHandlingModePolicy,
    IApplicationTransaction transaction)
{
    public Task<ComplianceStatusReportDto> GenerateAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        transaction.ExecuteAsync(async transactionCancellationToken =>
    {
        await dataHandlingModePolicy.EnsureAllowedAsync(
            new TenantDataHandlingModePolicyRequest(TenantDataHandlingWorkflow.Report, ContainsRealCui: false),
            actorUserId,
            transactionCancellationToken);

        var report = await repository.GenerateComplianceStatusReportAsync(actorUserId, transactionCancellationToken);
        await auditEventWriter.WriteAsync(
            report.TenantId,
            actorUserId,
            AuditAction.Created,
            "Report",
            report.Id.ToString(),
            "Compliance status report was generated.",
            new Dictionary<string, string>
            {
                ["reportType"] = report.Type.ToString(),
                ["status"] = report.Status.ToString(),
                ["generatedAt"] = report.GeneratedAt.ToString("O"),
                ["highRiskItems"] = report.Snapshot.HighRiskItems.Count.ToString(),
                ["overdueTasks"] = report.Snapshot.OverdueTasks.ToString()
            },
            transactionCancellationToken);
        return report;
    }, cancellationToken);
}
