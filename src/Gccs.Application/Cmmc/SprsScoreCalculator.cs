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

        var generatedAt = DateTimeOffset.UtcNow;
        var ruleSet = await GetPublishedRuleSetAsync(request.RuleSetId, generatedAt, cancellationToken);
        var normalizedNotes = request.ManualNotes?.Trim() ?? string.Empty;
        var conditionalSelections = NormalizeConditionalSelections(ruleSet, request.ConditionalDeductionSelections);
        var lineItems = CalculateLineItems(ruleSet, statuses, conditionalSelections);
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
            ruleSet.SourceSha256!,
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
                ["ruleSetSourceSha256"] = ruleSet.SourceSha256!,
                ["score"] = score.ToString(),
                ["totalDeduction"] = totalDeduction.ToString(),
                ["generatedAt"] = generatedAt.ToString("O")
            },
            cancellationToken);

        return calculation;
    }

    private async Task<SprsScoringRuleSetDto> GetPublishedRuleSetAsync(
        string ruleSetId,
        DateTimeOffset generatedAt,
        CancellationToken cancellationToken)
    {
        var ruleSet = await scoringRuleRepository.FindAsync(ruleSetId, cancellationToken) ??
            throw new SprsScoreCalculationException($"SPRS scoring rule set '{ruleSetId}' was not found.");

        try
        {
            SprsScoringRuleGovernance.EnsureUsableForCalculation(
                ruleSet,
                DateOnly.FromDateTime(generatedAt.UtcDateTime));
        }
        catch (SprsScoringRuleValidationException exception)
        {
            throw new SprsScoreCalculationException(exception.Message);
        }

        return ruleSet;
    }

    private static IReadOnlyList<SprsScoreCalculationLineItemDto> CalculateLineItems(
        SprsScoringRuleSetDto ruleSet,
        IReadOnlyList<CmmcControlStatusDto> statuses,
        IReadOnlyDictionary<string, string> conditionalSelections)
    {
        return ruleSet.Rules.Select(rule =>
        {
            var status = statuses.FirstOrDefault(candidate => MatchesRequirement(candidate.ControlId, rule.RequirementId));
            if (status is null)
            {
                EnsureAssessmentIsNotBlocked(rule, null);
                var deduction = ResolveDeduction(rule, conditionalSelections);
                return new SprsScoreCalculationLineItemDto(
                    rule.RequirementId,
                    null,
                    rule.Title,
                    rule.Deduction,
                    deduction,
                    "control-not-assessed",
                    null,
                    null);
            }

            if (status.Status is ControlImplementationStatus.NotApplicable ||
                status.Result is AssessmentResult.NotApplicable)
            {
                if (string.IsNullOrWhiteSpace(rule.NotApplicableWhen))
                {
                    throw new SprsScoreCalculationException(
                        $"SPRS requirement '{rule.RequirementId}' cannot be marked not applicable under the published scoring rule.");
                }

                if (string.IsNullOrWhiteSpace(status.Notes))
                {
                    throw new SprsScoreCalculationException(
                        $"SPRS requirement '{rule.RequirementId}' requires a documented not-applicable rationale matching the published rule condition.");
                }
                if (status.ReviewedBy is null ||
                    status.ReviewedAtUtc is null ||
                    !string.Equals(status.ReviewStatus, "approved", StringComparison.OrdinalIgnoreCase))
                {
                    throw new SprsScoreCalculationException(
                        $"SPRS requirement '{rule.RequirementId}' requires approved review metadata before a not-applicable exception can affect scoring.");
                }

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
            EnsureAssessmentIsNotBlocked(rule, isMet);
            var appliedDeduction = isMet ? 0 : ResolveDeduction(rule, conditionalSelections);
            var reason = isMet
                ? "implemented-and-met"
                : rule.RuleType is SprsScoringRuleType.ConditionalDeduction
                    ? $"conditional-{conditionalSelections[rule.RequirementId]}"
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
                appliedDeduction,
                reason,
                status.Status,
                status.Result);
        }).ToArray();
    }

    private static IReadOnlyDictionary<string, string> NormalizeConditionalSelections(
        SprsScoringRuleSetDto ruleSet,
        IReadOnlyList<SprsConditionalDeductionSelection>? selections)
    {
        selections ??= [];
        var duplicate = selections
            .GroupBy(selection => selection.RequirementId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new SprsScoreCalculationException(
                $"Multiple conditional deduction selections were supplied for SPRS requirement '{duplicate.Key}'.");
        }

        var conditionalRuleIds = ruleSet.Rules
            .Where(rule => rule.RuleType is SprsScoringRuleType.ConditionalDeduction)
            .Select(rule => rule.RequirementId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unsupported = selections.FirstOrDefault(selection =>
            string.IsNullOrWhiteSpace(selection.RequirementId) ||
            string.IsNullOrWhiteSpace(selection.OptionCode) ||
            !conditionalRuleIds.Contains(selection.RequirementId));
        if (unsupported is not null)
        {
            throw new SprsScoreCalculationException(
                $"Conditional deduction selection for SPRS requirement '{unsupported.RequirementId}' is not supported by the published rule set.");
        }

        return selections.ToDictionary(
            selection => selection.RequirementId,
            selection => selection.OptionCode,
            StringComparer.OrdinalIgnoreCase);
    }

    private static int ResolveDeduction(
        SprsScoringRuleDto rule,
        IReadOnlyDictionary<string, string> conditionalSelections)
    {
        if (rule.RuleType is not SprsScoringRuleType.ConditionalDeduction)
        {
            return rule.Deduction;
        }

        if (!conditionalSelections.TryGetValue(rule.RequirementId, out var optionCode))
        {
            throw new SprsScoreCalculationException(
                $"SPRS requirement '{rule.RequirementId}' requires an explicit conditional deduction selection.");
        }

        var option = rule.ConditionalDeductions?.FirstOrDefault(candidate =>
            string.Equals(candidate.Code, optionCode, StringComparison.OrdinalIgnoreCase));
        if (option is null)
        {
            throw new SprsScoreCalculationException(
                $"Conditional deduction option '{optionCode}' is invalid for SPRS requirement '{rule.RequirementId}'.");
        }

        return option.Deduction;
    }

    private static void EnsureAssessmentIsNotBlocked(SprsScoringRuleDto rule, bool? isMet)
    {
        if (rule.RuleType is SprsScoringRuleType.AssessmentBlocking && isMet is not true)
        {
            throw new SprsScoreCalculationException(
                $"SPRS assessment cannot be completed because assessment-blocking requirement '{rule.RequirementId}' is not met.");
        }
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
    string? ManualNotes,
    IReadOnlyList<SprsConditionalDeductionSelection>? ConditionalDeductionSelections = null);

public sealed record SprsConditionalDeductionSelection(
    string RequirementId,
    string OptionCode);

public sealed record SprsScoreCalculationDto(
    Guid Id,
    Guid TenantId,
    Guid AssessmentId,
    string RuleSetId,
    string RuleSetVersion,
    string RuleSetSourceSha256,
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
