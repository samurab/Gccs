using Gccs.Application.Common;
using Gccs.Domain.Common;

namespace Gccs.Application.Demo;

public sealed class SyntheticDemoDatasetService(ISyntheticDemoDatasetRepository repository)
{
    public async Task<SyntheticDemoDatasetDefinition> GetAsync(string packageRoot, CancellationToken cancellationToken = default)
    {
        var dataset = await repository.LoadAsync(packageRoot, cancellationToken);
        var precheck = SyntheticDemoDatasetPrecheck.Validate(dataset);
        if (!precheck.Allowed)
        {
            throw new SyntheticDemoDatasetValidationException(precheck.Errors);
        }

        return dataset;
    }

    public async Task<SyntheticDemoDatasetPrecheckResult> PrecheckAsync(string packageRoot, CancellationToken cancellationToken = default)
    {
        var dataset = await repository.LoadAsync(packageRoot, cancellationToken);
        return SyntheticDemoDatasetPrecheck.Validate(dataset);
    }
}

public interface ISyntheticDemoDatasetRepository
{
    Task<SyntheticDemoDatasetDefinition> LoadAsync(string packageRoot, CancellationToken cancellationToken = default);
}

public static class SyntheticDemoDatasetPrecheck
{
    private static readonly string[] RequiredRecordTypes = ["Company", "Contract", "Evidence", "Cmmc", "Subcontractor", "Report"];
    private static readonly string[] BlockedSensitiveTerms = ["classified", "export-controlled technical data", "customer proprietary", "payroll", "secret", "private key"];

    public static SyntheticDemoDatasetPrecheckResult Validate(SyntheticDemoDatasetDefinition dataset)
    {
        var errors = new List<string>();
        var metadata = dataset.Metadata;

        Require(!string.IsNullOrWhiteSpace(metadata.DatasetId), "Dataset metadata must include datasetId.", errors);
        Require(!string.IsNullOrWhiteSpace(metadata.Version), "Dataset metadata must include version.", errors);
        Require(!string.IsNullOrWhiteSpace(metadata.Purpose), "Dataset metadata must include purpose.", errors);
        Require(metadata.Limitations.Count > 0, "Dataset metadata must include limitations.", errors);
        Require(!string.IsNullOrWhiteSpace(metadata.Owner), "Dataset metadata must include owner.", errors);
        Require(!string.IsNullOrWhiteSpace(metadata.SourceBasis), "Dataset metadata must include source basis.", errors);
        Require(metadata.ReviewedAt is not null, "Dataset metadata must include review date.", errors);
        Require(!string.IsNullOrWhiteSpace(metadata.ApprovedReviewer), "Dataset metadata must include approved reviewer.", errors);
        Require(string.Equals(metadata.ReviewStatus, "Approved", StringComparison.OrdinalIgnoreCase), "Dataset review status must be Approved before import.", errors);
        Require(metadata.ApprovedForImport, "Dataset must be approved for import before seed import can run.", errors);

        foreach (var requiredType in RequiredRecordTypes)
        {
            Require(dataset.Records.Any(record => string.Equals(record.RecordType, requiredType, StringComparison.OrdinalIgnoreCase)),
                $"Dataset must include a synthetic {requiredType} example.",
                errors);
        }

        foreach (var record in dataset.Records)
        {
            Require(!string.IsNullOrWhiteSpace(record.RecordId), "Each synthetic record must include recordId.", errors);
            Require(string.Equals(record.DatasetVersion, metadata.Version, StringComparison.Ordinal),
                $"Record '{record.RecordId}' must use dataset version '{metadata.Version}'.",
                errors);
            Require(string.Equals(record.SyntheticLabel, "Synthetic demo data", StringComparison.OrdinalIgnoreCase),
                $"Record '{record.RecordId}' must include the visible synthetic demo label.",
                errors);
            Require(record.Classification.Classification is ContentClassification.SyntheticCui,
                $"Record '{record.RecordId}' must be classified SyntheticCui.",
                errors);
            Require(record.Classification.Source is ContentClassificationSource.ImportedDemoSeed,
                $"Record '{record.RecordId}' must use ImportedDemoSeed classification source.",
                errors);
            Require(record.Classification.IsApprovedDemoContent,
                $"Record '{record.RecordId}' must be marked as approved demo content.",
                errors);
            Require(!ContainsBlockedSensitiveTerm(record.SampleText),
                $"Record '{record.RecordId}' sample text must not contain blocked sensitive-data terms.",
                errors);
        }

        return new SyntheticDemoDatasetPrecheckResult(
            errors.Count == 0,
            metadata.DatasetId,
            metadata.Version,
            metadata.ReviewStatus,
            metadata.ApprovedReviewer,
            metadata.ReviewedAt,
            dataset.Records.Count,
            dataset.Records.Select(record => record.RecordType).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            errors);
    }

    private static bool ContainsBlockedSensitiveTerm(string sampleText) =>
        BlockedSensitiveTerms.Any(term => sampleText.Contains(term, StringComparison.OrdinalIgnoreCase));

    private static void Require(bool condition, string message, List<string> errors)
    {
        if (!condition)
        {
            errors.Add(message);
        }
    }
}

public sealed class SyntheticDemoDatasetValidationException(IReadOnlyList<string> errors)
    : InvalidOperationException("Synthetic demo dataset precheck failed.")
{
    public IReadOnlyList<string> Errors { get; } = errors;
}

public sealed record SyntheticDemoDatasetDefinition(
    SyntheticDemoDatasetMetadata Metadata,
    IReadOnlyList<SyntheticDemoDatasetRecord> Records);

public sealed record SyntheticDemoDatasetMetadata(
    string DatasetId,
    string Version,
    string Name,
    string Purpose,
    IReadOnlyList<string> Limitations,
    string Owner,
    string SourceBasis,
    DateOnly? ReviewedAt,
    string ApprovedReviewer,
    string ReviewStatus,
    bool ApprovedForImport);

public sealed record SyntheticDemoDatasetRecord(
    string RecordId,
    string RecordType,
    string Title,
    string DatasetVersion,
    string SyntheticLabel,
    ContentClassificationRequest Classification,
    string SampleText);

public sealed record SyntheticDemoDatasetPrecheckResult(
    bool Allowed,
    string DatasetId,
    string Version,
    string ReviewStatus,
    string ApprovedReviewer,
    DateOnly? ReviewedAt,
    int RecordCount,
    IReadOnlyList<string> RecordTypes,
    IReadOnlyList<string> Errors);
