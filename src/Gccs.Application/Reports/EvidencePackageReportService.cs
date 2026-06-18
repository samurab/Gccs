using Gccs.Application.Audit;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;

namespace Gccs.Application.Reports;

public sealed class EvidencePackageReportService(
    IReportRepository repository,
    IAuditEventWriter auditEventWriter,
    TenantDataHandlingModePolicyService dataHandlingModePolicy)
{
    public async Task<EvidencePackageReportDto> GenerateAsync(
        EvidencePackageGenerateRequest request,
        Guid actorUserId,
        bool includeDraftOrRejectedEvidence,
        CancellationToken cancellationToken = default)
    {
        await dataHandlingModePolicy.EnsureAllowedAsync(
            new TenantDataHandlingModePolicyRequest(TenantDataHandlingWorkflow.Report, ContainsRealCui: false),
            actorUserId,
            cancellationToken);

        var report = await repository.GenerateEvidencePackageAsync(
            request,
            actorUserId,
            includeDraftOrRejectedEvidence,
            cancellationToken);
        await auditEventWriter.WriteAsync(
            report.TenantId,
            actorUserId,
            AuditAction.Created,
            "Report",
            report.Id.ToString(),
            "Evidence package was generated.",
            new Dictionary<string, string>
            {
                ["reportType"] = report.Type.ToString(),
                ["evidenceItems"] = report.Manifest.Items.Count.ToString(),
                ["includeDraftOrRejectedEvidence"] = includeDraftOrRejectedEvidence.ToString(),
                ["obligationScopeCount"] = report.Manifest.Scope.ObligationIds.Count.ToString(),
                ["contractScopeCount"] = report.Manifest.Scope.ContractIds.Count.ToString(),
                ["controlScopeCount"] = report.Manifest.Scope.ControlIds.Count.ToString(),
                ["subcontractorScopeCount"] = report.Manifest.Scope.SubcontractorIds.Count.ToString()
            },
            cancellationToken);
        return report;
    }

    public Task<EvidencePackageReportDto?> GetAsync(
        Guid reportId,
        CancellationToken cancellationToken = default) =>
        repository.GetEvidencePackageAsync(reportId, cancellationToken);
}
