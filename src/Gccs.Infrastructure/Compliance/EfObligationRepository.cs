using System.Text.Json;
using Gccs.Application.Repositories;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Compliance;

public sealed class EfObligationRepository(GccsDbContext dbContext) : IObligationRepository
{
    public async Task<IReadOnlyList<Obligation>> ListAsync(CancellationToken cancellationToken = default)
    {
        var entities = await dbContext.Obligations
            .AsNoTracking()
            .Where(obligation => obligation.ReviewState == ReviewState.Published)
            .OrderBy(obligation => obligation.Source)
            .ThenBy(obligation => obligation.Title)
            .ToArrayAsync(cancellationToken);

        return entities.Select(ToDomain).ToArray();
    }

    public async Task<Obligation?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Obligations
            .AsNoTracking()
            .FirstOrDefaultAsync(
                obligation => obligation.Id == id && obligation.ReviewState == ReviewState.Published,
                cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    private static Obligation ToDomain(ObligationEntity entity) =>
        new(
            entity.Id,
            entity.Source,
            entity.Title,
            entity.PlainEnglishSummary,
            entity.TriggerCondition,
            entity.RequiredAction,
            entity.OwnerFunction,
            entity.RiskLevel,
            entity.RequiresFlowDown,
            entity.FlowDownRequirement,
            ReadApplicability(entity),
            ReadEvidenceExamples(entity).ToArray(),
            new ComplianceSource(
                entity.SourceName,
                new Uri(entity.SourceUrl),
                entity.SourceLastReviewedAt,
                entity.SourceEffectiveAt,
                entity.SourceConfidence,
                entity.SourceRequiresExpertReview),
            new ReviewMetadata(
                entity.LastReviewedAt,
                entity.ReviewedByUserId,
                entity.NextReviewDueAt,
                entity.Confidence,
                entity.RequiresExpertReview,
                entity.ReviewState));

    private static ApplicabilityDimension ReadApplicability(ObligationEntity entity)
    {
        try
        {
            using var document = JsonDocument.Parse(entity.ApplicabilityJson);
            var root = document.RootElement;
            return new ApplicabilityDimension(
                ReadStringOrJoinedArray(root, "appliesTo", "unspecified"),
                ReadStringOrJoinedArray(root, "contractTypes", "unspecified"),
                ReadStringOrJoinedArray(root, "dataTypes", "unspecified"),
                ReadString(root, "agency", "any"),
                ReadString(root, "placeOfPerformance", "any"),
                ReadString(root, "dataHandling", entity.TriggerCondition));
        }
        catch (JsonException)
        {
            return new ApplicabilityDimension("unspecified", "unspecified", "unspecified", "any", "any", entity.TriggerCondition);
        }
    }

    private static IEnumerable<EvidenceExample> ReadEvidenceExamples(ObligationEntity entity)
    {
        using var document = JsonDocument.Parse(entity.EvidenceExamplesJson);
        foreach (var item in document.RootElement.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var name = item.GetString();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    yield return new EvidenceExample(name, $"Evidence supporting {entity.Source}.", entity.OwnerFunction);
                }

                continue;
            }

            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var objectName = ReadString(item, "name", string.Empty);
            if (string.IsNullOrWhiteSpace(objectName))
            {
                continue;
            }

            yield return new EvidenceExample(
                objectName,
                ReadString(item, "description", $"Evidence supporting {entity.Source}."),
                ReadString(item, "owner", entity.OwnerFunction));
        }
    }

    private static string ReadStringOrJoinedArray(JsonElement element, string propertyName, string fallback)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return fallback;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            return value.GetString() ?? fallback;
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            var values = value.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                .Select(item => item.GetString())
                .ToArray();

            return values.Length == 0 ? fallback : string.Join(", ", values);
        }

        return fallback;
    }

    private static string ReadString(JsonElement element, string propertyName, string fallback)
    {
        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String)
        {
            return fallback;
        }

        return value.GetString() ?? fallback;
    }
}
