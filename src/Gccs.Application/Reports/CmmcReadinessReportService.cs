using Gccs.Application.Audit;
using Gccs.Application.Ai;
using Gccs.Application.Common;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;

namespace Gccs.Application.Reports;

public sealed class CmmcReadinessReportService(
    IReportRepository repository,
    IAuditEventWriter auditEventWriter,
    TenantDataHandlingModePolicyService dataHandlingModePolicy,
    IApplicationTransaction transaction, ContentClassificationPolicy classificationPolicy,
    AiOutputReviewService? aiOutputReview = null)
{
    public Task<CmmcReadinessReportDto?> GenerateAsync(
        Guid assessmentId,
        Guid actorUserId,
        bool includeEvidenceLinks,
        CancellationToken cancellationToken = default, ContentClassificationRequest? classification = null,
        Guid? aiOutputId = null) =>
        transaction.ExecuteAsync(async transactionCancellationToken =>
    {
        await ClassifiedWorkflowValidation.ConfirmAsync(classificationPolicy, classification, TenantDataHandlingWorkflow.Report, actorUserId, transactionCancellationToken);
        await dataHandlingModePolicy.EnsureAllowedAsync(
            new TenantDataHandlingModePolicyRequest(TenantDataHandlingWorkflow.Report, ContainsRealCui: false),
            actorUserId,
            transactionCancellationToken);

        var report = await repository.GenerateCmmcReadinessReportAsync(
            assessmentId,
            actorUserId,
            includeEvidenceLinks,
            transactionCancellationToken, classification);
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
            transactionCancellationToken);
        if (aiOutputId is Guid outputId)
            await RequiredAiReview().RequireDeliverableLinkAsync(outputId, report.TenantId,
                AiDeliverableType.Report, report.Id, actorUserId, transactionCancellationToken);
        return report;
    }, cancellationToken);

    private AiOutputReviewService RequiredAiReview() => aiOutputReview ??
        throw new AiOutputReviewValidationException("aiOutputId", "AI output provenance processing is unavailable.");
}
