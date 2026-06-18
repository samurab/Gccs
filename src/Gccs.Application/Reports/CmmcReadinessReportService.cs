using Gccs.Application.Audit;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;

namespace Gccs.Application.Reports;

public sealed class CmmcReadinessReportService(
    IReportRepository repository,
    IAuditEventWriter auditEventWriter,
    TenantDataHandlingModePolicyService dataHandlingModePolicy)
{
    public async Task<CmmcReadinessReportDto?> GenerateAsync(
        Guid assessmentId,
        Guid actorUserId,
        bool includeEvidenceLinks,
        CancellationToken cancellationToken = default)
    {
        await dataHandlingModePolicy.EnsureAllowedAsync(
            new TenantDataHandlingModePolicyRequest(TenantDataHandlingWorkflow.Report, ContainsRealCui: false),
            actorUserId,
            cancellationToken);

        var report = await repository.GenerateCmmcReadinessReportAsync(
            assessmentId,
            actorUserId,
            includeEvidenceLinks,
            cancellationToken);
        if (report is null)
        {
            return null;
        }

        await auditEventWriter.WriteAsync(
            report.TenantId,
            actorUserId,
            AuditAction.Created,
            "Report",
            report.Id.ToString(),
            "CMMC readiness report was generated.",
            new Dictionary<string, string>
            {
                ["reportType"] = report.Type.ToString(),
                ["assessmentId"] = report.Snapshot.AssessmentId.ToString(),
                ["targetLevel"] = report.Snapshot.TargetLevel.ToString(),
                ["openPoamItems"] = report.Snapshot.OpenPoamItems.Count.ToString(),
                ["evidenceLinksIncluded"] = includeEvidenceLinks.ToString()
            },
            cancellationToken);
        return report;
    }
}
