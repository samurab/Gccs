using Gccs.Application.Audit;
using Gccs.Domain.Audit;
using Gccs.Domain.Cmmc;

namespace Gccs.Application.Cmmc;

public sealed class SprsScoreCalculationService(
    ICmmcAssessmentRepository assessmentRepository,
    ISprsScoringRuleRepository scoringRuleRepository,
    ISprsScoreCalculationHistoryRepository historyRepository,
    IAuditEventWriter auditEventWriter)
{
    public async Task<SprsScoreCalculationDto?> CalculateAsync(
        Guid assessmentId,
        SprsScoreCalculationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var assessment = await assessmentRepository.FindCurrentTenantAsync(assessmentId, cancellationToken);
        if (assessment is null)
        {
            return null;
        }

        if (assessment.Level is not CmmcLevel.Level2)
        {
            throw new SprsScoreCalculationException("SPRS score calculation requires a CMMC Level 2 / NIST SP 800-171 assessment.");
        }

        var statuses = await assessmentRepository.ListControlStatusesAsync(assessmentId, cancellationToken);
        if (statuses is null)
        {
            return null;
        }

        var ruleSet = await GetPublishedRuleSetAsync(request.RuleSetId, cancellationToken);
        var generatedAt = DateTimeOffset.UtcNow;
        var normalizedNotes = request.ManualNotes?.Trim() ?? string.Empty;
        var lineItems = CalculateLineItems(ruleSet, statuses);
        var totalDeduction = lineItems.Sum(item => item.AppliedDeduction);
        var score = Math.Max(0, ruleSet.MaximumScore - totalDeduction);
        var unresolvedGaps = lineItems
            .Where(item => item.AppliedDeduction > 0)
            .Select(item => new SprsUnresolvedGapDto(
                item.RequirementId,
                item.ControlId,
                item.Title,
                item.Reason))
            .ToArray();
        var calculation = new SprsScoreCalculationDto(
            Guid.NewGuid(),
            assessment.TenantId,
            assessment.Id,
            ruleSet.Id,
            ruleSet.Version,
            ruleSet.MaximumScore,
            score,
            totalDeduction,
            lineItems,
            unresolvedGaps,
            normalizedNotes,
            generatedAt);

        await historyRepository.SaveAsync(calculation, cancellationToken);
        await auditEventWriter.WriteAsync(
            assessment.TenantId,
            actorUserId,
            AuditAction.Created,
            "SprsScoreCalculation",
            calculation.Id.ToString(),
            "Draft SPRS score calculation was generated.",
            new Dictionary<string, string>
            {
                ["assessmentId"] = assessment.Id.ToString(),
                ["ruleSetId"] = ruleSet.Id,
                ["ruleSetVersion"] = ruleSet.Version,
                ["score"] = score.ToString(),
                ["totalDeduction"] = totalDeduction.ToString(),
                ["generatedAt"] = generatedAt.ToString("O")
            },
            cancellationToken);

        return calculation;
    }

    private async Task<SprsScoringRuleSetDto> GetPublishedRuleSetAsync(
        string ruleSetId,
        CancellationToken cancellationToken)
    {
        var ruleSet = await scoringRuleRepository.FindAsync(ruleSetId, cancellationToken) ??
            throw new SprsScoreCalculationException($"SPRS scoring rule set '{ruleSetId}' was not found.");

        if (ruleSet.State is SprsScoringRuleSetState.Retired)
        {
            throw new SprsScoreCalculationException("Retired SPRS scoring rules cannot be used for new calculations.");
        }

        if (ruleSet.State is not SprsScoringRuleSetState.Published)
        {
            throw new SprsScoreCalculationException("Only published SPRS scoring rules can be used for calculations.");
        }

        return ruleSet;
    }

    private static IReadOnlyList<SprsScoreCalculationLineItemDto> CalculateLineItems(
        SprsScoringRuleSetDto ruleSet,
        IReadOnlyList<CmmcControlStatusDto> statuses)
    {
        return ruleSet.Rules.Select(rule =>
        {
            var status = statuses.FirstOrDefault(candidate => MatchesRequirement(candidate.ControlId, rule.RequirementId));
            if (status is null)
            {
                return new SprsScoreCalculationLineItemDto(
                    rule.RequirementId,
                    null,
                    rule.Title,
                    rule.Deduction,
                    rule.Deduction,
                    "control-not-assessed",
                    null,
                    null);
            }

            if (status.Status is ControlImplementationStatus.NotApplicable ||
                status.Result is AssessmentResult.NotApplicable)
            {
                return new SprsScoreCalculationLineItemDto(
                    rule.RequirementId,
                    status.ControlId,
                    rule.Title,
                    rule.Deduction,
                    0,
                    "not-applicable",
                    status.Status,
                    status.Result);
            }

            var isMet = status.Status is ControlImplementationStatus.Implemented &&
                status.Result is AssessmentResult.Met;
            var reason = isMet
                ? "implemented-and-met"
                : status.Status switch
                {
                    ControlImplementationStatus.NotStarted => "control-not-implemented",
                    ControlImplementationStatus.PartiallyImplemented => "control-partially-implemented",
                    ControlImplementationStatus.NeedsReview => "control-needs-review",
                    _ => status.Result is AssessmentResult.NotMet ? "assessment-not-met" : "control-gap"
                };

            return new SprsScoreCalculationLineItemDto(
                rule.RequirementId,
                status.ControlId,
                rule.Title,
                rule.Deduction,
                isMet ? 0 : rule.Deduction,
                reason,
                status.Status,
                status.Result);
        }).ToArray();
    }

    private static bool MatchesRequirement(string controlId, string requirementId) =>
        string.Equals(controlId, requirementId, StringComparison.OrdinalIgnoreCase) ||
        controlId.EndsWith(requirementId, StringComparison.OrdinalIgnoreCase);
}

public interface ISprsScoreCalculationHistoryRepository
{
    Task SaveAsync(SprsScoreCalculationDto calculation, CancellationToken cancellationToken = default);
}

public sealed record SprsScoreCalculationRequest(
    string RuleSetId,
    string? ManualNotes);

public sealed record SprsScoreCalculationDto(
    Guid Id,
    Guid TenantId,
    Guid AssessmentId,
    string RuleSetId,
    string RuleSetVersion,
    int MaximumScore,
    int Score,
    int TotalDeduction,
    IReadOnlyList<SprsScoreCalculationLineItemDto> LineItems,
    IReadOnlyList<SprsUnresolvedGapDto> UnresolvedGaps,
    string ManualNotes,
    DateTimeOffset GeneratedAt);

public sealed record SprsScoreCalculationLineItemDto(
    string RequirementId,
    string? ControlId,
    string Title,
    int RuleDeduction,
    int AppliedDeduction,
    string Reason,
    ControlImplementationStatus? ControlStatus,
    AssessmentResult? AssessmentResult);

public sealed record SprsUnresolvedGapDto(
    string RequirementId,
    string? ControlId,
    string Title,
    string Reason);

public sealed class SprsScoreCalculationException(string message) : InvalidOperationException(message);
