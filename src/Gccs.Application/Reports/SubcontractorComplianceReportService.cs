using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;

namespace Gccs.Application.Reports;

public sealed class SubcontractorComplianceReportService(
    IReportRepository repository,
    IAuditEventWriter auditEventWriter,
    TenantDataHandlingModePolicyService dataHandlingModePolicy,
    IApplicationTransaction transaction, ContentClassificationPolicy classificationPolicy)
{
    public Task<SubcontractorComplianceReportDto> GenerateAsync(
        Guid? contractId,
        Guid actorUserId,
        CancellationToken cancellationToken = default, ContentClassificationRequest? classification = null) =>
        transaction.ExecuteAsync(async transactionCancellationToken =>
    {
        await ClassifiedWorkflowValidation.ConfirmAsync(classificationPolicy, classification, TenantDataHandlingWorkflow.Report, actorUserId, transactionCancellationToken);
        await dataHandlingModePolicy.EnsureAllowedAsync(
            new TenantDataHandlingModePolicyRequest(
                TenantDataHandlingWorkflow.Report,
                ContainsRealCui: false,
                EntityType: "Contract",
                EntityId: contractId?.ToString()),
            actorUserId,
            transactionCancellationToken);

        var report = await repository.GenerateSubcontractorComplianceReportAsync(contractId, actorUserId, transactionCancellationToken, classification);
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
            transactionCancellationToken);
        return report;
    }, cancellationToken);
}
