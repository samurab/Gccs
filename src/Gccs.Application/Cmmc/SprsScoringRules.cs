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
        foreach (var ruleSet in ruleSets.Where(ruleSet => ruleSet.State is SprsScoringRuleSetState.Published))
        {
            ValidatePublished(ruleSet);
        }

        return ruleSets;
    }

    public async Task<SprsScoringRuleSetDto> GetUsableForCalculationAsync(
        string ruleSetId,
        CancellationToken cancellationToken = default)
    {
        var ruleSet = await repository.FindAsync(ruleSetId, cancellationToken) ??
            throw new SprsScoringRuleValidationException($"SPRS scoring rule set '{ruleSetId}' was not found.");

        if (ruleSet.State is SprsScoringRuleSetState.Retired)
        {
            throw new SprsScoringRuleValidationException("Retired SPRS scoring rules cannot be used for new calculations.");
        }

        if (ruleSet.State is not SprsScoringRuleSetState.Published)
        {
            throw new SprsScoringRuleValidationException("Only published SPRS scoring rules can be used for new calculations.");
        }

        ValidatePublished(ruleSet);
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
        var updatedCandidate = current with
        {
            State = request.State,
            Reviewer = string.IsNullOrWhiteSpace(request.Reviewer) ? current.Reviewer : request.Reviewer.Trim(),
            ReviewDate = request.ReviewDate ?? current.ReviewDate,
            LastReviewedAt = request.ReviewDate ?? current.LastReviewedAt
        };

        if (request.State is SprsScoringRuleSetState.Published)
        {
            ValidatePublished(updatedCandidate);
        }

        var updated = await repository.UpdateStateAsync(
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

    private static void ValidatePublished(SprsScoringRuleSetDto ruleSet)
    {
        if (string.IsNullOrWhiteSpace(ruleSet.Version) ||
            string.IsNullOrWhiteSpace(ruleSet.Owner) ||
            string.IsNullOrWhiteSpace(ruleSet.Reviewer) ||
            string.IsNullOrWhiteSpace(ruleSet.SourceUrl) ||
            ruleSet.EffectiveDate is null ||
            ruleSet.ReviewDate is null ||
            ruleSet.LastReviewedAt is null)
        {
            throw new SprsScoringRuleValidationException(
                "Published SPRS scoring rules require source URL, version, owner, reviewer, review date, last reviewed date, and effective date.");
        }

        if (ruleSet.Rules.Count == 0)
        {
            throw new SprsScoringRuleValidationException("Published SPRS scoring rules require at least one scored requirement.");
        }

        foreach (var rule in ruleSet.Rules)
        {
            if (string.IsNullOrWhiteSpace(rule.RequirementId) ||
                string.IsNullOrWhiteSpace(rule.Title) ||
                string.IsNullOrWhiteSpace(rule.SourceUrl))
            {
                throw new SprsScoringRuleValidationException(
                    "Each SPRS scoring rule requires requirement ID, title, deduction, and source URL.");
            }
        }
    }
}

public interface ISprsScoringRuleRepository
{
    Task<IReadOnlyList<SprsScoringRuleSetDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<SprsScoringRuleSetDto?> FindAsync(string ruleSetId, CancellationToken cancellationToken = default);

    Task<SprsScoringRuleSetDto> UpdateStateAsync(
        string ruleSetId,
        SprsScoringRuleSetState state,
        string? reviewer,
        DateOnly? reviewDate,
        CancellationToken cancellationToken = default);
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
    IReadOnlyList<SprsScoringRuleDto> Rules);

public sealed record SprsScoringRuleDto(
    string RequirementId,
    string Title,
    int Deduction,
    string AssessmentObjective,
    string SourceUrl);

public sealed record ChangeSprsScoringRuleSetStateRequest(
    SprsScoringRuleSetState State,
    string? Reviewer,
    DateOnly? ReviewDate);

public sealed record SprsCalculationRuleReferenceDto(
    string RuleSetId,
    string RuleSetVersion,
    string SourceUrl,
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
