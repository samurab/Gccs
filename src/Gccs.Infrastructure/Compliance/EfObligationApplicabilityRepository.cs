using System.Text.Json;
using Gccs.Application.Compliance;
using Gccs.Application.Security;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Compliance;

public sealed class EfObligationApplicabilityRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext,
    IApplicabilityFactRepository factRepository) : IObligationApplicabilityRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ObligationApplicabilityEvaluationDto?> ReevaluateCurrentTenantAsync(
        Guid contractClauseId,
        string obligationId,
        ApplicabilityRuleDefinition rule,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var mapping = await dbContext.Set<ContractClauseObligationEntity>()
            .Include(item => item.ContractClause)
            .ThenInclude(clause => clause!.Contract)
            .Include(item => item.Obligation)
            .SingleOrDefaultAsync(
                item =>
                    item.ContractClauseId == contractClauseId &&
                    item.ObligationId == obligationId &&
                    item.ContractClause != null &&
                    item.ContractClause.Contract != null &&
                    item.ContractClause.Contract.TenantId == tenantContext.TenantId &&
                    item.ContractClause.RemovedAt == null &&
                    item.Obligation != null,
                cancellationToken);
        if (mapping is null)
        {
            return null;
        }

        var facts = await factRepository.ListAsync(
            new ApplicabilityFactQuery(
                tenantContext.TenantId,
                mapping.ContractClause!.ContractId,
                mapping.ContractClauseId),
            cancellationToken);
        var evaluation = new ApplicabilityRuleEvaluator().Evaluate(tenantContext.TenantId, rule, facts);
        var previous = await dbContext.ObligationApplicabilityEvaluations
            .AsNoTracking()
            .Where(item =>
                item.TenantId == tenantContext.TenantId &&
                item.ContractClauseId == contractClauseId &&
                item.ObligationId == obligationId)
            .OrderByDescending(item => item.EvaluatedAt)
            .ThenByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var missingFacts = MissingFacts(rule, evaluation.FactsUsed);
        var entity = new ObligationApplicabilityEvaluationEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            ContractClauseId = contractClauseId,
            ObligationId = obligationId,
            PreviousEvaluationId = previous?.Id,
            SourceRuleId = evaluation.SourceRuleId,
            State = evaluation.State.ToString(),
            Explanation = evaluation.Explanation,
            FactsUsedJson = JsonSerializer.Serialize(evaluation.FactsUsed, JsonOptions),
            MissingFactsJson = JsonSerializer.Serialize(missingFacts, JsonOptions),
            MetadataJson = JsonSerializer.Serialize(evaluation.Metadata, JsonOptions),
            EvaluatedAt = DateTimeOffset.UtcNow,
            EvaluatedByUserId = actorUserId
        };

        dbContext.ObligationApplicabilityEvaluations.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(entity, evaluation.FactsUsed, missingFacts, previous?.State);
    }

    private static IReadOnlyList<string> MissingFacts(
        ApplicabilityRuleDefinition rule,
        IReadOnlyCollection<ApplicabilityFactDto> factsUsed)
    {
        var knownKeys = factsUsed
            .Where(fact => !fact.IsUnknown)
            .Select(fact => fact.Key)
            .ToHashSet(StringComparer.Ordinal);
        return rule.Conditions
            .Where(condition => condition.Required && !knownKeys.Contains(condition.FactKey))
            .Select(condition => condition.FactKey)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static ObligationApplicabilityEvaluationDto ToDto(
        ObligationApplicabilityEvaluationEntity entity,
        IReadOnlyList<ApplicabilityFactDto> factsUsed,
        IReadOnlyList<string> missingFacts,
        string? previousState) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.ContractClauseId,
            entity.ObligationId,
            entity.SourceRuleId,
            entity.State,
            entity.Explanation,
            factsUsed,
            missingFacts,
            entity.EvaluatedAt,
            entity.EvaluatedByUserId,
            entity.PreviousEvaluationId,
            previousState);
}
