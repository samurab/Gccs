using Gccs.Domain.Common;

namespace Gccs.Application.Compliance;

public sealed class ApplicabilityRuleEvaluator
{
    public ApplicabilityRuleEvaluationResult Evaluate(
        Guid tenantId,
        ApplicabilityRuleDefinition rule,
        IEnumerable<ApplicabilityFactDto> facts)
    {
        if (rule.TenantId is { } ruleTenantId && ruleTenantId != tenantId)
        {
            return new ApplicabilityRuleEvaluationResult(
                tenantId,
                rule.RuleId,
                ApplicabilityRuleResultState.NotApplicable,
                $"Rule '{rule.RuleId}' is scoped to a different tenant.",
                [],
                rule.Metadata);
        }

        var tenantFacts = facts
            .Where(fact => fact.TenantId == tenantId)
            .OrderBy(fact => fact.Key, StringComparer.Ordinal)
            .ThenBy(fact => fact.Value, StringComparer.Ordinal)
            .ToArray();
        var factsUsed = new List<ApplicabilityFactDto>();

        foreach (var condition in rule.Conditions)
        {
            var matchingFacts = tenantFacts
                .Where(fact => string.Equals(fact.Key, condition.FactKey, StringComparison.Ordinal))
                .ToArray();
            factsUsed.AddRange(matchingFacts);

            var knownValues = matchingFacts
                .Where(fact => !fact.IsUnknown)
                .Select(fact => fact.Value)
                .ToArray();
            if (knownValues.Length == 0)
            {
                if (condition.Required)
                {
                    return Result(
                        tenantId,
                        rule,
                        ApplicabilityRuleResultState.InsufficientInformation,
                        $"Missing required fact '{condition.FactKey}' for rule '{rule.RuleId}'.",
                        factsUsed);
                }

                continue;
            }

            if (!ConditionMatches(condition, knownValues))
            {
                return Result(
                    tenantId,
                    rule,
                    ApplicabilityRuleResultState.NotApplicable,
                    $"Fact '{condition.FactKey}' did not satisfy rule '{rule.RuleId}'.",
                    factsUsed);
            }
        }

        var state = rule.Metadata.RequiresExpertReview
            ? ApplicabilityRuleResultState.NeedsReview
            : ApplicabilityRuleResultState.Applicable;
        var explanation = state == ApplicabilityRuleResultState.NeedsReview
            ? $"Rule '{rule.RuleId}' matched but requires expert review."
            : $"Rule '{rule.RuleId}' matched all required conditions.";
        return Result(tenantId, rule, state, explanation, factsUsed);
    }

    private static ApplicabilityRuleEvaluationResult Result(
        Guid tenantId,
        ApplicabilityRuleDefinition rule,
        ApplicabilityRuleResultState state,
        string explanation,
        IEnumerable<ApplicabilityFactDto> factsUsed) =>
        new(
            tenantId,
            rule.RuleId,
            state,
            explanation,
            factsUsed
                .DistinctBy(fact => new { fact.Key, fact.Value, fact.SourceType, fact.SourceId })
                .OrderBy(fact => fact.Key, StringComparer.Ordinal)
                .ThenBy(fact => fact.Value, StringComparer.Ordinal)
                .ToArray(),
            rule.Metadata);

    private static bool ConditionMatches(ApplicabilityRuleCondition condition, IReadOnlyCollection<string> values) =>
        condition.Operator switch
        {
            ApplicabilityRuleOperator.Exists => values.Count > 0,
            ApplicabilityRuleOperator.Equals => values.Any(value => AnyExpectedValueEquals(condition.Values, value)),
            ApplicabilityRuleOperator.AnyOf => values.Any(value => AnyExpectedValueEquals(condition.Values, value)),
            ApplicabilityRuleOperator.Contains => values.Any(value => condition.Values.Any(expected => value.Contains(expected, StringComparison.OrdinalIgnoreCase))),
            ApplicabilityRuleOperator.NotEquals => values.All(value => !AnyExpectedValueEquals(condition.Values, value)),
            _ => false
        };

    private static bool AnyExpectedValueEquals(IReadOnlyCollection<string> expectedValues, string value) =>
        expectedValues.Any(expected => string.Equals(expected, value, StringComparison.OrdinalIgnoreCase));
}

public sealed record ApplicabilityRuleDefinition(
    string RuleId,
    Guid? TenantId,
    string ObligationId,
    string Name,
    ApplicabilityRuleMetadata Metadata,
    IReadOnlyList<ApplicabilityRuleCondition> Conditions);

public sealed record ApplicabilityRuleCondition(
    string FactKey,
    ApplicabilityRuleOperator Operator,
    IReadOnlyList<string> Values,
    bool Required = true);

public sealed record ApplicabilityRuleMetadata(
    string Source,
    string SourceUrl,
    string Confidence,
    DateOnly? EffectiveAt,
    DateOnly LastReviewedAt,
    Guid? ReviewedByUserId,
    ReviewState ReviewState,
    bool RequiresExpertReview);

public sealed record ApplicabilityRuleEvaluationResult(
    Guid TenantId,
    string SourceRuleId,
    ApplicabilityRuleResultState State,
    string Explanation,
    IReadOnlyList<ApplicabilityFactDto> FactsUsed,
    ApplicabilityRuleMetadata Metadata);

public enum ApplicabilityRuleResultState
{
    Applicable,
    NotApplicable,
    NeedsReview,
    InsufficientInformation
}

public enum ApplicabilityRuleOperator
{
    Exists,
    Equals,
    AnyOf,
    Contains,
    NotEquals
}
