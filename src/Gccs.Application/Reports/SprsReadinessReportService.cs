using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Cmmc;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Reports;

namespace Gccs.Application.Reports;

public sealed class SprsReadinessReportService(
    SprsScoreCalculationService scoreCalculationService,
    ICmmcAssessmentRepository assessmentRepository,
    IReportRepository reportRepository,
    IAuditEventWriter auditEventWriter,
    TenantDataHandlingModePolicyService dataHandlingModePolicy,
    IApplicationTransaction transaction,
    ContentClassificationPolicy classificationPolicy)
{
    public Task<SprsReadinessReportDto?> GenerateAsync(
        Guid assessmentId,
        SprsReadinessReportRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        transaction.ExecuteAsync(async transactionCancellationToken =>
        {
            ValidateRequest(request);
            var classification = request.Classification ??
                throw new ContentClassificationValidationException("Explicit report classification is required.");
            await ClassifiedWorkflowValidation.ConfirmAsync(
                classificationPolicy,
                classification,
                TenantDataHandlingWorkflow.Report,
                actorUserId,
                transactionCancellationToken);
            await dataHandlingModePolicy.EnsureAllowedAsync(
                new TenantDataHandlingModePolicyRequest(TenantDataHandlingWorkflow.Report, ContainsRealCui: false),
                actorUserId,
                transactionCancellationToken);

            var assessment = await assessmentRepository.FindCurrentTenantAsync(assessmentId, transactionCancellationToken);
            if (assessment is null)
            {
                return null;
            }

            var calculation = await scoreCalculationService.CalculateAsync(
                assessmentId,
                new SprsScoreCalculationRequest(
                    request.RuleSetId,
                    request.ReviewerNotes,
                    request.ConditionalDeductionSelections,
                    string.IsNullOrWhiteSpace(request.ReviewerNotes) ? null : classification),
                actorUserId,
                transactionCancellationToken);
            if (calculation is null)
            {
                return null;
            }

            var statuses = await assessmentRepository.ListControlStatusesAsync(assessmentId, transactionCancellationToken);
            if (statuses is null)
            {
                return null;
            }

            var statusByControlId = statuses.ToDictionary(status => status.ControlId, StringComparer.OrdinalIgnoreCase);
            var snapshot = new SprsReadinessSnapshotDto(
                calculation.AssessmentId,
                assessment.Name,
                assessment.CompletedAt ?? assessment.StartedAt,
                calculation.Id,
                calculation.Score,
                calculation.MaximumScore,
                calculation.TotalDeduction,
                calculation.RuleSetId,
                calculation.RuleSetVersion,
                calculation.RuleSetSourceUrl,
                calculation.RuleSetSourceSha256,
                calculation.GeneratedAt,
                "Draft",
                ReportArtifactLanguage.SprsReadinessDisclaimer,
                NormalizeLeadershipReviewStatus(request.LeadershipReviewStatus),
                calculation.ManualNotes,
                BuildDeductions(calculation),
                BuildUnresolvedControls(calculation, statusByControlId));

            var report = await reportRepository.SaveSprsReadinessReportAsync(
                snapshot,
                assessment.Name,
                actorUserId,
                classification,
                transactionCancellationToken);

            await auditEventWriter.WriteAsync(
                report.TenantId,
                actorUserId,
                AuditAction.Created,
                "Report",
                report.Id.ToString(),
                "SPRS readiness report was generated.",
                new Dictionary<string, string>
                {
                    ["reportType"] = report.Type.ToString(),
                    ["assessmentId"] = assessmentId.ToString(),
                    ["calculationId"] = calculation.Id.ToString(),
                    ["ruleSetVersion"] = calculation.RuleSetVersion,
                    ["score"] = calculation.Score.ToString(),
                    ["totalDeduction"] = calculation.TotalDeduction.ToString(),
                    ["generatedAt"] = report.GeneratedAt.ToString("O"),
                    ["submissionStatus"] = "not-submitted"
                },
                transactionCancellationToken);

            return report;
        }, cancellationToken);

    private static void ValidateRequest(SprsReadinessReportRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RuleSetId) || request.RuleSetId.Length > 200)
        {
            throw new SprsReadinessReportException("A valid SPRS scoring rule set ID is required.");
        }

        if (request.ReviewerNotes?.Length > 2_000)
        {
            throw new SprsReadinessReportException("Reviewer notes cannot exceed 2,000 characters.");
        }

        _ = NormalizeLeadershipReviewStatus(request.LeadershipReviewStatus);
    }

    private static string? NormalizeLeadershipReviewStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized is not ("Pending" or "Reviewed" or "NeedsChanges"))
        {
            throw new SprsReadinessReportException(
                "Leadership review status must be Pending, Reviewed, or NeedsChanges.");
        }

        return normalized;
    }

    private static IReadOnlyList<SprsReadinessDeductionDto> BuildDeductions(SprsScoreCalculationDto calculation) =>
        calculation.LineItems
            .Where(item => item.AppliedDeduction > 0)
            .Select(item => new SprsReadinessDeductionDto(
                item.RequirementId,
                item.ControlId,
                item.Title,
                item.AppliedDeduction,
                item.Reason))
            .ToArray();

    private static IReadOnlyList<SprsReadinessUnresolvedControlDto> BuildUnresolvedControls(
        SprsScoreCalculationDto calculation,
        IReadOnlyDictionary<string, CmmcControlStatusDto> statusByControlId) =>
        calculation.UnresolvedGaps
            .Select(gap =>
            {
                var status = gap.ControlId is null || !statusByControlId.TryGetValue(gap.ControlId, out var matched)
                    ? null
                    : matched;
                return new SprsReadinessUnresolvedControlDto(
                    gap.RequirementId,
                    gap.ControlId,
                    gap.Title,
                    gap.Reason,
                    status?.PoamItemIds ?? [],
                    status?.EvidenceItemIds.Count > 0 ? "Linked" : "Missing");
            })
            .ToArray();
}

public sealed record SprsReadinessReportRequest(
    string RuleSetId,
    string? ReviewerNotes,
    string? LeadershipReviewStatus,
    IReadOnlyList<SprsConditionalDeductionSelection>? ConditionalDeductionSelections,
    [property: System.Text.Json.Serialization.JsonRequired] ContentClassificationRequest? Classification);

public sealed record SprsReadinessReportDto(
    Guid Id,
    Guid TenantId,
    ReportType Type,
    ReportStatus Status,
    string Title,
    DateTimeOffset GeneratedAt,
    Guid GeneratedByUserId,
    SprsReadinessSnapshotDto Snapshot,
    string ExportHtml)
{
    public ContentClassificationDto? Classification { get; init; }
    public string Disclaimer => ReportArtifactLanguage.SprsReadinessDisclaimer;
}

public sealed record SprsReadinessSnapshotDto(
    Guid AssessmentId,
    string AssessmentName,
    DateOnly AssessmentDate,
    Guid CalculationId,
    int Score,
    int MaximumScore,
    int TotalDeduction,
    string RuleSetId,
    string RuleSetVersion,
    string RuleSetSourceUrl,
    string RuleSetSourceSha256,
    DateTimeOffset GeneratedAt,
    string ArtifactStatus,
    string SubmissionStatement,
    string? LeadershipReviewStatus,
    string ReviewerNotes,
    IReadOnlyList<SprsReadinessDeductionDto> Deductions,
    IReadOnlyList<SprsReadinessUnresolvedControlDto> UnresolvedControls);

public sealed record SprsReadinessDeductionDto(
    string RequirementId,
    string? ControlId,
    string Title,
    int Deduction,
    string Reason);

public sealed record SprsReadinessUnresolvedControlDto(
    string RequirementId,
    string? ControlId,
    string Title,
    string Reason,
    IReadOnlyList<Guid> PoamItemIds,
    string EvidenceStatus);

public sealed class SprsReadinessReportException(string message) : ArgumentException(message);
