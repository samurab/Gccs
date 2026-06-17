using Gccs.Application.Audit;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;

namespace Gccs.Application.Compliance;

public sealed class SbaSizeStandardService(
    ISbaSizeStandardRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public async Task<IReadOnlyList<SbaSizeStandardDto>> ImportAsync(
        IReadOnlyList<ImportSbaSizeStandardRequest> records,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        Validate(records);
        var imported = await repository.ImportAsync(records, actorUserId, cancellationToken);
        await auditEventWriter.WriteAsync(
            Guid.Empty,
            actorUserId,
            AuditAction.Created,
            "SbaSizeStandard",
            "import",
            $"Imported {imported.Count} SBA size standard records.",
            new Dictionary<string, string>
            {
                ["recordCount"] = imported.Count.ToString(),
                ["naicsCodes"] = string.Join(",", imported.Select(record => record.NaicsCode).Distinct().Order())
            },
            cancellationToken);
        return imported;
    }

    public Task<IReadOnlyList<SbaSizeStandardDto>> ListApprovedAsync(CancellationToken cancellationToken = default) =>
        repository.ListApprovedAsync(cancellationToken);

    public Task<IReadOnlyList<SbaSizeStandardDto>> ListForReviewAsync(CancellationToken cancellationToken = default) =>
        repository.ListForReviewAsync(cancellationToken);

    public async Task<SbaSizeStandardDto?> ChangeStatusAsync(
        Guid id,
        ReviewState status,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var updated = await repository.ChangeStatusAsync(id, status, actorUserId, cancellationToken);
        if (updated is null)
        {
            return null;
        }

        await auditEventWriter.WriteAsync(
            Guid.Empty,
            actorUserId,
            AuditAction.Updated,
            "SbaSizeStandard",
            id.ToString(),
            $"SBA size standard {updated.NaicsCode} status changed to {updated.Status}.",
            new Dictionary<string, string>
            {
                ["naicsCode"] = updated.NaicsCode,
                ["status"] = updated.Status.ToString()
            },
            cancellationToken);
        return updated;
    }

    private static void Validate(IReadOnlyList<ImportSbaSizeStandardRequest> records)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        for (var index = 0; index < records.Count; index++)
        {
            var record = records[index];
            AddIf(errors, string.IsNullOrWhiteSpace(record.NaicsCode), $"records[{index}].naicsCode", "NAICS code is required.");
            AddIf(errors, string.IsNullOrWhiteSpace(record.Metric), $"records[{index}].metric", "Metric is required.");
            AddIf(errors, record.Threshold <= 0, $"records[{index}].threshold", "Threshold must be greater than zero.");
            AddIf(errors, string.IsNullOrWhiteSpace(record.Unit), $"records[{index}].unit", "Unit is required.");
            AddIf(errors, !Uri.TryCreate(record.SourceUrl, UriKind.Absolute, out _), $"records[{index}].sourceUrl", "Source URL must be absolute.");
            AddIf(errors, record.EffectiveAt is null, $"records[{index}].effectiveAt", "Effective date is required.");
            AddIf(errors, record.LastReviewedAt is null, $"records[{index}].lastReviewedAt", "Last reviewed date is required.");
        }

        if (errors.Count > 0)
        {
            throw new SbaSizeStandardImportValidationException(errors);
        }
    }

    private static void AddIf(IDictionary<string, string[]> errors, bool condition, string key, string message)
    {
        if (condition)
        {
            errors[key] = [message];
        }
    }
}

public interface ISbaSizeStandardRepository
{
    Task<IReadOnlyList<SbaSizeStandardDto>> ImportAsync(
        IReadOnlyList<ImportSbaSizeStandardRequest> records,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SbaSizeStandardDto>> ListApprovedAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SbaSizeStandardDto>> ListForReviewAsync(CancellationToken cancellationToken = default);

    Task<SbaSizeStandardDto?> ChangeStatusAsync(
        Guid id,
        ReviewState status,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed record ImportSbaSizeStandardRequest(
    string NaicsCode,
    string Metric,
    decimal Threshold,
    string Unit,
    string SourceUrl,
    DateOnly? EffectiveAt,
    DateOnly? LastReviewedAt,
    ReviewState Status = ReviewState.Draft);

public sealed record SbaSizeStandardDto(
    Guid Id,
    string NaicsCode,
    string Metric,
    decimal Threshold,
    string Unit,
    string SourceUrl,
    DateOnly EffectiveAt,
    DateOnly LastReviewedAt,
    ReviewState Status,
    Guid? ReviewedByUserId);

public sealed class SbaSizeStandardImportValidationException(IReadOnlyDictionary<string, string[]> errors) : InvalidOperationException("SBA size standard import is invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
