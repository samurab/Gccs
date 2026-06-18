using Gccs.Application.Audit;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;

namespace Gccs.Application.Reports;

public sealed class ComplianceStatusReportService(
    IReportRepository repository,
    IAuditEventWriter auditEventWriter,
    TenantDataHandlingModePolicyService dataHandlingModePolicy)
{
    public async Task<ComplianceStatusReportDto> GenerateAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        await dataHandlingModePolicy.EnsureAllowedAsync(
            new TenantDataHandlingModePolicyRequest(TenantDataHandlingWorkflow.Report, ContainsRealCui: false),
            actorUserId,
            cancellationToken);

        var report = await repository.GenerateComplianceStatusReportAsync(actorUserId, cancellationToken);
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
            cancellationToken);
        return report;
    }
}
