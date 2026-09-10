using Gccs.Application.Audit;
using Gccs.Domain.Audit;

namespace Gccs.Application.Cmmc;

public sealed class SprsScoringRuleService(
    ISprsScoringRuleRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public async Task<IReadOnlyList<SprsScoringRuleSetDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var ruleSets = await repository.ListAsync(cancellationToken);
        SprsScoringRuleGovernance.ValidatePackage(ruleSets);
        return ruleSets;
    }

    public async Task<SprsScoringRuleSetDto> GetUsableForCalculationAsync(
        string ruleSetId,
        CancellationToken cancellationToken = default)
    {
        var ruleSet = await repository.FindAsync(ruleSetId, cancellationToken) ??
            throw new SprsScoringRuleValidationException($"SPRS scoring rule set '{ruleSetId}' was not found.");

        SprsScoringRuleGovernance.EnsureUsableForCalculation(
            ruleSet,
            DateOnly.FromDateTime(DateTime.UtcNow));
        return ruleSet;
    }

    public async Task<SprsCalculationRuleReferenceDto> CreateCalculationRuleReferenceAsync(
        string ruleSetId,
        CancellationToken cancellationToken = default)
    {
        var ruleSet = await GetUsableForCalculationAsync(ruleSetId, cancellationToken);
        return new SprsCalculationRuleReferenceDto(
            ruleSet.Id,
            ruleSet.Version,
            ruleSet.SourceUrl,
            ruleSet.SourceSha256!,
            ruleSet.EffectiveDate!.Value,
            DateTimeOffset.UtcNow);
    }

    public async Task<SprsScoringRuleSetDto> ChangeStateAsync(
        string ruleSetId,
        ChangeSprsScoringRuleSetStateRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var current = await repository.FindAsync(ruleSetId, cancellationToken) ??
            throw new SprsScoringRuleValidationException($"SPRS scoring rule set '{ruleSetId}' was not found.");
        SprsScoringRuleGovernance.EnsureTransitionAllowed(current.State, request.State);

        var updatedCandidate = current with
        {
            State = request.State,
            Reviewer = string.IsNullOrWhiteSpace(request.Reviewer) ? current.Reviewer : request.Reviewer.Trim(),
            ReviewDate = request.ReviewDate ?? current.ReviewDate,
            LastReviewedAt = request.ReviewDate ?? current.LastReviewedAt
        };

        if (request.State is SprsScoringRuleSetState.Published)
        {
            SprsScoringRuleGovernance.ValidatePublished(updatedCandidate);
        }

        if (repository is not ISprsScoringRuleLifecycleRepository lifecycleRepository)
        {
            throw new SprsScoringRuleValidationException(
                "This SPRS scoring rule package is source-control governed and does not support runtime lifecycle changes.");
        }

        var updated = await lifecycleRepository.UpdateStateAsync(
            ruleSetId,
            request.State,
            updatedCandidate.Reviewer,
            updatedCandidate.ReviewDate,
            cancellationToken);

        await auditEventWriter.WriteAsync(
            tenantId,
            actorUserId,
            AuditAction.Updated,
            "SprsScoringRuleSet",
            ruleSetId,
            $"SPRS scoring rule set '{ruleSetId}' changed from {current.State} to {updated.State}.",
            new Dictionary<string, string>
            {
                ["beforeState"] = current.State.ToString(),
                ["afterState"] = updated.State.ToString(),
                ["version"] = updated.Version,
                ["sourceUrl"] = updated.SourceUrl,
                ["reviewer"] = updated.Reviewer ?? string.Empty,
                ["reviewDate"] = updated.ReviewDate?.ToString("O") ?? string.Empty
            },
            cancellationToken);

        return updated;
    }
}

public interface ISprsScoringRuleRepository
{
    Task<IReadOnlyList<SprsScoringRuleSetDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<SprsScoringRuleSetDto?> FindAsync(string ruleSetId, CancellationToken cancellationToken = default);
}

public interface ISprsScoringRuleLifecycleRepository
{
    Task<SprsScoringRuleSetDto> UpdateStateAsync(
        string ruleSetId,
        SprsScoringRuleSetState state,
        string? reviewer,
        DateOnly? reviewDate,
        CancellationToken cancellationToken = default);
}

public static class SprsScoringRuleGovernance
{
    public static void ValidatePackage(IReadOnlyList<SprsScoringRuleSetDto> ruleSets)
    {
        var duplicateId = ruleSets
            .GroupBy(ruleSet => ruleSet.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateId is not null)
        {
            throw new SprsScoringRuleValidationException(
                $"SPRS scoring rule package contains duplicate rule set ID '{duplicateId.Key}'.");
        }

        var duplicatePublishedVersion = ruleSets
            .Where(ruleSet => ruleSet.State is SprsScoringRuleSetState.Published)
            .GroupBy(ruleSet => ruleSet.Version, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicatePublishedVersion is not null)
        {
            throw new SprsScoringRuleValidationException(
                $"SPRS scoring rule package contains duplicate published version '{duplicatePublishedVersion.Key}'.");
        }

        foreach (var ruleSet in ruleSets.Where(ruleSet => ruleSet.State is SprsScoringRuleSetState.Published))
        {
            ValidatePublished(ruleSet);
        }
    }

    public static void EnsureUsableForCalculation(SprsScoringRuleSetDto ruleSet, DateOnly calculationDate)
    {
        if (ruleSet.State is SprsScoringRuleSetState.Retired)
        {
            throw new SprsScoringRuleValidationException("Retired SPRS scoring rules cannot be used for new calculations.");
        }

        if (ruleSet.State is not SprsScoringRuleSetState.Published)
        {
            throw new SprsScoringRuleValidationException("Only published SPRS scoring rules can be used for new calculations.");
        }

        ValidatePublished(ruleSet);
        if (ruleSet.EffectiveDate > calculationDate)
        {
            throw new SprsScoringRuleValidationException(
                $"SPRS scoring rule version '{ruleSet.Version}' is not effective until {ruleSet.EffectiveDate:O}.");
        }
    }

    public static void EnsureTransitionAllowed(
        SprsScoringRuleSetState current,
        SprsScoringRuleSetState requested)
    {
        var allowed = current switch
        {
            SprsScoringRuleSetState.Draft => requested is SprsScoringRuleSetState.Approved,
            SprsScoringRuleSetState.Approved => requested is SprsScoringRuleSetState.Draft or SprsScoringRuleSetState.Published,
            SprsScoringRuleSetState.Published => requested is SprsScoringRuleSetState.Superseded or SprsScoringRuleSetState.Retired,
            SprsScoringRuleSetState.Superseded => requested is SprsScoringRuleSetState.Retired,
            SprsScoringRuleSetState.Retired => false,
            _ => false
        };

        if (!allowed)
        {
            throw new SprsScoringRuleValidationException(
                $"SPRS scoring rule lifecycle transition from {current} to {requested} is not allowed.");
        }
    }

    public static void ValidatePublished(SprsScoringRuleSetDto ruleSet)
    {
        if (string.IsNullOrWhiteSpace(ruleSet.Id) ||
            string.IsNullOrWhiteSpace(ruleSet.Version) ||
            string.IsNullOrWhiteSpace(ruleSet.SourceName) ||
            string.IsNullOrWhiteSpace(ruleSet.Owner) ||
            string.IsNullOrWhiteSpace(ruleSet.Reviewer) ||
            !IsSha256(ruleSet.SourceSha256) ||
            !IsValidSourceUrl(ruleSet.SourceUrl) ||
            ruleSet.EffectiveDate is null ||
            ruleSet.ReviewDate is null ||
            ruleSet.LastReviewedAt is null)
        {
            throw new SprsScoringRuleValidationException(
                "Published SPRS scoring rules require ID, source name, HTTPS source URL, source SHA-256, version, owner, reviewer, review date, last reviewed date, and effective date.");
        }

        if (string.Equals(ruleSet.Owner.Trim(), ruleSet.Reviewer.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new SprsScoringRuleValidationException(
                "Published SPRS scoring rules require a reviewer distinct from the content owner.");
        }

        if (ruleSet.LastReviewedAt < ruleSet.ReviewDate)
        {
            throw new SprsScoringRuleValidationException(
                "SPRS scoring rule last reviewed date cannot precede its publication review date.");
        }

        if (ruleSet.MaximumScore <= 0)
        {
            throw new SprsScoringRuleValidationException("Published SPRS scoring rules require a positive maximum score.");
        }

        if (ruleSet.Rules.Count == 0)
        {
            throw new SprsScoringRuleValidationException("Published SPRS scoring rules require at least one scored requirement.");
        }

        if (ruleSet.ExpectedRequirementCount is null or <= 0 ||
            ruleSet.Rules.Count != ruleSet.ExpectedRequirementCount)
        {
            throw new SprsScoringRuleValidationException(
                "Published SPRS scoring rules require a positive expected requirement count that matches the governed rule inventory.");
        }

        var duplicateRequirement = ruleSet.Rules
            .GroupBy(rule => rule.RequirementId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateRequirement is not null)
        {
            throw new SprsScoringRuleValidationException(
                $"SPRS scoring rule set contains duplicate requirement ID '{duplicateRequirement.Key}'.");
        }

        foreach (var rule in ruleSet.Rules)
        {
            if (string.IsNullOrWhiteSpace(rule.RequirementId) ||
                string.IsNullOrWhiteSpace(rule.Title) ||
                string.IsNullOrWhiteSpace(rule.AssessmentObjective) ||
                !IsValidSourceUrl(rule.SourceUrl))
            {
                throw new SprsScoringRuleValidationException(
                    "Each published SPRS scoring rule requires requirement ID, title, assessment objective, and an HTTPS source URL.");
            }

            ValidateDeductionPolicy(rule, ruleSet.MaximumScore);
        }
    }

    private static void ValidateDeductionPolicy(SprsScoringRuleDto rule, int maximumScore)
    {
        var options = rule.ConditionalDeductions ?? [];
        switch (rule.RuleType)
        {
            case SprsScoringRuleType.FixedDeduction:
                if (rule.Deduction <= 0 || rule.Deduction > maximumScore || options.Count != 0)
                {
                    throw new SprsScoringRuleValidationException(
                        $"Fixed SPRS rule '{rule.RequirementId}' requires one positive bounded deduction and no conditional options.");
                }
                break;

            case SprsScoringRuleType.ConditionalDeduction:
                if (rule.Deduction <= 0 || rule.Deduction > maximumScore || options.Count < 2)
                {
                    throw new SprsScoringRuleValidationException(
                        $"Conditional SPRS rule '{rule.RequirementId}' requires a positive maximum deduction and at least two options.");
                }

                if (options.GroupBy(option => option.Code, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1) ||
                    options.Any(option => string.IsNullOrWhiteSpace(option.Code) ||
                        string.IsNullOrWhiteSpace(option.When) ||
                        option.Deduction <= 0 ||
                        option.Deduction > rule.Deduction) ||
                    options.Max(option => option.Deduction) != rule.Deduction)
                {
                    throw new SprsScoringRuleValidationException(
                        $"Conditional SPRS rule '{rule.RequirementId}' has invalid or duplicate deduction options.");
                }
                break;

            case SprsScoringRuleType.AssessmentBlocking:
                if (rule.Deduction != 0 || options.Count != 0)
                {
                    throw new SprsScoringRuleValidationException(
                        $"Assessment-blocking SPRS rule '{rule.RequirementId}' cannot define point deductions.");
                }
                break;

            default:
                throw new SprsScoringRuleValidationException(
                    $"SPRS rule '{rule.RequirementId}' has an unsupported deduction policy.");
        }

        if (rule.NotApplicableWhen is not null && string.IsNullOrWhiteSpace(rule.NotApplicableWhen))
        {
            throw new SprsScoringRuleValidationException(
                $"SPRS rule '{rule.RequirementId}' has an empty not-applicable condition.");
        }
    }

    private static bool IsValidSourceUrl(string? sourceUrl) =>
        Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps &&
        !string.IsNullOrWhiteSpace(uri.Host);

    private static bool IsSha256(string? value) =>
        value is { Length: 64 } && value.All(Uri.IsHexDigit);
}

public sealed record SprsScoringRuleSetDto(
    string Id,
    string Version,
    SprsScoringRuleSetState State,
    string SourceName,
    string SourceUrl,
    DateOnly? EffectiveDate,
    DateOnly? LastReviewedAt,
    string Owner,
    string? Reviewer,
    DateOnly? ReviewDate,
    int MaximumScore,
    IReadOnlyList<SprsScoringRuleDto> Rules,
    int? ExpectedRequirementCount = null,
    string? SourceSha256 = null);

public sealed record SprsScoringRuleDto(
    string RequirementId,
    string Title,
    int Deduction,
    string AssessmentObjective,
    string SourceUrl,
    SprsScoringRuleType RuleType = SprsScoringRuleType.FixedDeduction,
    IReadOnlyList<SprsConditionalDeductionOptionDto>? ConditionalDeductions = null,
    string? NotApplicableWhen = null,
    string? SourceComment = null,
    bool IsBasicSafeguardingRequirement = false);

public sealed record SprsConditionalDeductionOptionDto(
    string Code,
    int Deduction,
    string When);

public enum SprsScoringRuleType
{
    FixedDeduction,
    ConditionalDeduction,
    AssessmentBlocking
}

public sealed record ChangeSprsScoringRuleSetStateRequest(
    SprsScoringRuleSetState State,
    string? Reviewer,
    DateOnly? ReviewDate);

public sealed record SprsCalculationRuleReferenceDto(
    string RuleSetId,
    string RuleSetVersion,
    string SourceUrl,
    string SourceSha256,
    DateOnly EffectiveDate,
    DateTimeOffset GeneratedAt);

public enum SprsScoringRuleSetState
{
    Draft,
    Approved,
    Published,
    Retired,
    Superseded
}

public sealed class SprsScoringRuleValidationException(string message) : InvalidOperationException(message);
