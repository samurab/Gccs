using Gccs.Application.Audit;
using Gccs.Domain.Audit;

namespace Gccs.Application.Reports;

public sealed class SubcontractorComplianceReportService(
    IReportRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public async Task<SubcontractorComplianceReportDto> GenerateAsync(
        Guid? contractId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var report = await repository.GenerateSubcontractorComplianceReportAsync(contractId, actorUserId, cancellationToken);
        await auditEventWriter.WriteAsync(
            report.TenantId,
            actorUserId,
            AuditAction.Created,
            "Report",
            report.Id.ToString(),
            "Subcontractor compliance report was generated.",
            new Dictionary<string, string>
            {
                ["reportType"] = report.Type.ToString(),
                ["contractId"] = contractId?.ToString() ?? string.Empty,
                ["subcontractors"] = report.Snapshot.TotalSubcontractors.ToString(),
                ["missingEvidenceRequests"] = report.Snapshot.MissingEvidenceRequests.ToString(),
                ["overdueEvidenceRequests"] = report.Snapshot.OverdueEvidenceRequests.ToString(),
                ["openFlowDowns"] = report.Snapshot.OpenFlowDowns.ToString()
            },
            cancellationToken);
        return report;
    }
}
