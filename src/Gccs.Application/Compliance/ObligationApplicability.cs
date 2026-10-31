using Gccs.Application.Audit;
using Gccs.Domain.Audit;

namespace Gccs.Application.Compliance;

public sealed class ObligationApplicabilityService(
    IObligationApplicabilityRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public async Task<ObligationApplicabilityEvaluationDto?> ReevaluateAsync(
        Guid contractClauseId,
        string obligationId,
        ApplicabilityRuleDefinition rule,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var result = await repository.ReevaluateCurrentTenantAsync(
            contractClauseId,
            obligationId,
            rule,
            actorUserId,
            cancellationToken);
        if (result is null)
        {
            return null;
        }

        if (IsMaterialChange(result.PreviousState, result.State))
        {
            await auditEventWriter.WriteAsync(
                result.TenantId,
                actorUserId,
                AuditAction.Updated,
                "ObligationApplicability",
                result.Id.ToString(),
                $"Obligation applicability changed from {result.PreviousState} to {result.State}.",
                new Dictionary<string, string>
                {
                    ["contractClauseId"] = result.ContractClauseId.ToString(),
                    ["obligationId"] = result.ObligationId,
                    ["sourceRuleId"] = result.SourceRuleId,
                    ["previousState"] = result.PreviousState ?? string.Empty,
                    ["state"] = result.State
                },
                cancellationToken);
        }

        return result;
    }

    private static bool IsMaterialChange(string? previousState, string state) =>
        string.Equals(previousState, ApplicabilityRuleResultState.Applicable.ToString(), StringComparison.Ordinal) &&
        (string.Equals(state, ApplicabilityRuleResultState.NotApplicable.ToString(), StringComparison.Ordinal) ||
            string.Equals(state, ApplicabilityRuleResultState.NeedsReview.ToString(), StringComparison.Ordinal));
}

public interface IObligationApplicabilityRepository
{
    Task<ObligationApplicabilityEvaluationDto?> ReevaluateCurrentTenantAsync(
        Guid contractClauseId,
        string obligationId,
        ApplicabilityRuleDefinition rule,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed record ObligationApplicabilityEvaluationDto(
    Guid Id,
    Guid TenantId,
    Guid ContractClauseId,
    string ObligationId,
    string SourceRuleId,
    string State,
    string Explanation,
    IReadOnlyList<ApplicabilityFactDto> FactsUsed,
    IReadOnlyList<string> MissingFacts,
    DateTimeOffset EvaluatedAt,
    Guid EvaluatedByUserId,
    Guid? PreviousEvaluationId,
    string? PreviousState);

public sealed record ObligationApplicabilitySummaryDto(
    string State,
    string Explanation,
    string SourceRuleId,
    IReadOnlyList<string> FactsUsed,
    IReadOnlyList<string> MissingFacts,
    DateTimeOffset EvaluatedAt,
    int HistoryCount);
