using Gccs.Application.Audit;
using Gccs.Application.Cmmc;
using Gccs.Domain.Audit;
using Gccs.Domain.Cmmc;

namespace Gccs.Application.Reports;

public sealed class SprsReadinessReportService(
    SprsScoreCalculationService scoreCalculationService,
    ICmmcAssessmentRepository assessmentRepository,
    IAuditEventWriter auditEventWriter)
{
    public const string NotSubmittedDisclaimer =
        "GCCS has not submitted this score to SPRS. This report is draft readiness tracking for customer review.";

    public async Task<SprsReadinessReportDto?> GenerateAsync(
        Guid assessmentId,
        SprsReadinessReportRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (!request.HasReportPermission)
        {
            throw new SprsReadinessReportException("User is not authorized to generate or view SPRS readiness reports.");
        }

        var calculation = await scoreCalculationService.CalculateAsync(
            assessmentId,
            new SprsScoreCalculationRequest(request.RuleSetId, request.ReviewerNotes),
            actorUserId,
            cancellationToken);
        if (calculation is null)
        {
            return null;
        }

        var statuses = await assessmentRepository.ListControlStatusesAsync(assessmentId, cancellationToken);
        if (statuses is null)
        {
            return null;
        }

        var statusByControlId = statuses.ToDictionary(status => status.ControlId, StringComparer.OrdinalIgnoreCase);
        var report = new SprsReadinessReportDto(
            Guid.NewGuid(),
            calculation.TenantId,
            calculation.AssessmentId,
            calculation.Score,
            calculation.MaximumScore,
            calculation.TotalDeduction,
            calculation.RuleSetId,
            calculation.RuleSetVersion,
            calculation.GeneratedAt,
            NotSubmittedDisclaimer,
            BuildDeductions(calculation),
            BuildUnresolvedControls(calculation, statusByControlId),
            calculation.ManualNotes);

        await auditEventWriter.WriteAsync(
            calculation.TenantId,
            actorUserId,
            AuditAction.Created,
            "SprsReadinessReport",
            report.Id.ToString(),
            "SPRS readiness report was generated.",
            new Dictionary<string, string>
            {
                ["assessmentId"] = assessmentId.ToString(),
                ["ruleSetVersion"] = calculation.RuleSetVersion,
                ["score"] = calculation.Score.ToString(),
                ["totalDeduction"] = calculation.TotalDeduction.ToString(),
                ["generatedAt"] = report.GeneratedAt.ToString("O")
            },
            cancellationToken);

        return report;
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
                    status?.EvidenceItemIds.Count > 0 ? "linked" : "missing");
            })
            .ToArray();
}

public sealed record SprsReadinessReportRequest(
    string RuleSetId,
    string? ReviewerNotes,
    bool HasReportPermission = true);

public sealed record SprsReadinessReportDto(
    Guid Id,
    Guid TenantId,
    Guid AssessmentId,
    int Score,
    int MaximumScore,
    int TotalDeduction,
    string RuleSetId,
    string RuleSetVersion,
    DateTimeOffset GeneratedAt,
    string SubmissionDisclaimer,
    IReadOnlyList<SprsReadinessDeductionDto> Deductions,
    IReadOnlyList<SprsReadinessUnresolvedControlDto> UnresolvedControls,
    string ReviewerNotes);

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

public sealed class SprsReadinessReportException(string message) : InvalidOperationException(message);
