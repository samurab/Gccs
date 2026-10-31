using Gccs.Application.Compliance;
using Gccs.Domain.Common;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ApplicabilityRuleEvaluatorTests
{
    private static readonly Guid TenantId = Guid.Parse("21221221-2212-2122-1221-2122122122a1");
    private static readonly Guid OtherTenantId = Guid.Parse("21221221-2212-2122-1221-2122122122b1");

    [Fact]
    public void TC_21_2_1_Returns_state_explanation_source_rule_and_facts_used()
    {
        var evaluator = new ApplicabilityRuleEvaluator();
        var rule = Rule("far-52-204-21-fci", "FAR 52.204-21 FCI safeguarding",
            Condition("company.data_type", ApplicabilityRuleOperator.AnyOf, "FciOnly", "FciAndCui"),
            Condition("clause.citation", ApplicabilityRuleOperator.Equals, "52.204-21"));

        var result = evaluator.Evaluate(TenantId, rule,
        [
            Fact("company.data_type", "FciOnly"),
            Fact("clause.citation", "52.204-21")
        ]);

        Assert.Equal(ApplicabilityRuleResultState.Applicable, result.State);
        Assert.Equal("far-52-204-21-fci", result.SourceRuleId);
        Assert.Contains("matched all required conditions", result.Explanation, StringComparison.Ordinal);
        Assert.Contains(result.FactsUsed, fact => fact.Key == "company.data_type" && fact.Value == "FciOnly");
        Assert.Equal("high", result.Metadata.Confidence);
        Assert.Equal(new DateOnly(2025, 11, 10), result.Metadata.EffectiveAt);
    }

    [Fact]
    public void TC_21_2_2_Missing_required_facts_return_insufficient_information()
    {
        var evaluator = new ApplicabilityRuleEvaluator();
        var rule = Rule("dfars-252-204-7012-cui", "DFARS CUI safeguarding",
            Condition("contract.data_type", ApplicabilityRuleOperator.AnyOf, "Cui", "FciAndCui"),
            Condition("contract.agency", ApplicabilityRuleOperator.Contains, "Defense"));

        var result = evaluator.Evaluate(TenantId, rule, [Fact("contract.agency", "Department of Defense")]);

        Assert.Equal(ApplicabilityRuleResultState.InsufficientInformation, result.State);
        Assert.Contains("Missing required fact 'contract.data_type'", result.Explanation, StringComparison.Ordinal);
        Assert.Equal(TenantId, result.TenantId);
    }

    [Fact]
    public void TC_21_2_3_Evaluation_is_repeatable_for_same_inputs()
    {
        var evaluator = new ApplicabilityRuleEvaluator();
        var rule = Rule("cmmc-level-2-cui", "CMMC Level 2 readiness",
            Condition("contract.data_type", ApplicabilityRuleOperator.AnyOf, "Cui", "FciAndCui"),
            Condition("contract.agency", ApplicabilityRuleOperator.Contains, "Defense"));
        var facts = new[]
        {
            Fact("contract.agency", "Department of Defense"),
            Fact("contract.data_type", "FciAndCui")
        };

        var first = evaluator.Evaluate(TenantId, rule, facts);
        var second = evaluator.Evaluate(TenantId, rule, facts.Reverse());

        Assert.Equal(first.TenantId, second.TenantId);
        Assert.Equal(first.SourceRuleId, second.SourceRuleId);
        Assert.Equal(first.State, second.State);
        Assert.Equal(first.Explanation, second.Explanation);
        Assert.Equal(first.Metadata, second.Metadata);
        Assert.Equal(
            first.FactsUsed.Select(fact => (fact.Key, fact.Value, fact.SourceId)),
            second.FactsUsed.Select(fact => (fact.Key, fact.Value, fact.SourceId)));
    }

    [Fact]
    public void TC_21_2_4_Evaluation_results_are_tenant_scoped()
    {
        var evaluator = new ApplicabilityRuleEvaluator();
        var rule = Rule("sam-sba-size-cert", "SAM and SBA profile readiness",
            Condition("company.naics", ApplicabilityRuleOperator.Equals, "541511"),
            Condition("company.certification", ApplicabilityRuleOperator.AnyOf, "Wosb", "Sdb"));

        var result = evaluator.Evaluate(TenantId, rule,
        [
            Fact("company.naics", "541511", OtherTenantId),
            Fact("company.certification", "Wosb", OtherTenantId)
        ]);

        Assert.Equal(ApplicabilityRuleResultState.InsufficientInformation, result.State);
        Assert.Empty(result.FactsUsed);
        Assert.Equal(TenantId, result.TenantId);
    }

    [Fact]
    public void TC_21_2_5_Covers_needs_review_not_applicable_and_flow_down_patterns()
    {
        var evaluator = new ApplicabilityRuleEvaluator();
        var flowDown = Rule(
            "flowdown-subcontractor-cui",
            "Flow-down for subcontractor CUI access",
            true,
            Condition("subcontractor.has_cui_access", ApplicabilityRuleOperator.Equals, "True"),
            Condition("clause.citation", ApplicabilityRuleOperator.Equals, "52.204-21"));
        var needsReview = evaluator.Evaluate(TenantId, flowDown,
        [
            Fact("subcontractor.has_cui_access", "True"),
            Fact("clause.citation", "52.204-21")
        ]);

        var notApplicable = evaluator.Evaluate(TenantId, flowDown,
        [
            Fact("subcontractor.has_cui_access", "False"),
            Fact("clause.citation", "52.204-21")
        ]);

        Assert.Equal(ApplicabilityRuleResultState.NeedsReview, needsReview.State);
        Assert.Contains("requires expert review", needsReview.Explanation, StringComparison.Ordinal);
        Assert.Equal(ApplicabilityRuleResultState.NotApplicable, notApplicable.State);
        Assert.Equal(ReviewState.Published, needsReview.Metadata.ReviewState);
    }

    private static ApplicabilityRuleDefinition Rule(
        string id,
        string name,
        params ApplicabilityRuleCondition[] conditions) =>
        Rule(id, name, false, conditions);

    private static ApplicabilityRuleDefinition Rule(
        string id,
        string name,
        bool requiresExpertReview,
        params ApplicabilityRuleCondition[] conditions) =>
        new(
            id,
            null,
            $"obligation-{id}",
            name,
            new ApplicabilityRuleMetadata(
                "GCCS test rule library",
                "https://www.acquisition.gov/",
                "high",
                new DateOnly(2025, 11, 10),
                new DateOnly(2026, 6, 17),
                Guid.Parse("21221221-2212-2122-1221-2122122122c1"),
                ReviewState.Published,
                requiresExpertReview),
            conditions);

    private static ApplicabilityRuleCondition Condition(
        string key,
        ApplicabilityRuleOperator ruleOperator,
        params string[] values) =>
        new(key, ruleOperator, values);

    private static ApplicabilityFactDto Fact(string key, string value, Guid? tenantId = null) =>
        new(
            tenantId ?? TenantId,
            key,
            value,
            string.Equals(value, "unknown", StringComparison.OrdinalIgnoreCase),
            "TestSource",
            Guid.NewGuid().ToString(),
            new DateTimeOffset(2026, 6, 17, 0, 0, 0, TimeSpan.Zero));
}
