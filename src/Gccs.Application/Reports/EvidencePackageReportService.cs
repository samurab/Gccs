using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;

namespace Gccs.Application.Reports;

public sealed class EvidencePackageReportService(
    IReportRepository repository,
    IAuditEventWriter auditEventWriter,
    TenantDataHandlingModePolicyService dataHandlingModePolicy,
    IApplicationTransaction transaction)
{
    public Task<EvidencePackageReportDto> GenerateAsync(
        EvidencePackageGenerateRequest request,
        Guid actorUserId,
        bool includeDraftOrRejectedEvidence,
        CancellationToken cancellationToken = default) =>
        transaction.ExecuteAsync(async transactionCancellationToken =>
    {
        await dataHandlingModePolicy.EnsureAllowedAsync(
            new TenantDataHandlingModePolicyRequest(TenantDataHandlingWorkflow.Report, ContainsRealCui: false),
            actorUserId,
            transactionCancellationToken);

        var report = await repository.GenerateEvidencePackageAsync(
            request,
            actorUserId,
            includeDraftOrRejectedEvidence,
            transactionCancellationToken);
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
            transactionCancellationToken);
        return report;
    }, cancellationToken);

    public Task<EvidencePackageReportDto?> GetAsync(
        Guid reportId,
        CancellationToken cancellationToken = default) =>
        repository.GetEvidencePackageAsync(reportId, cancellationToken);
}
