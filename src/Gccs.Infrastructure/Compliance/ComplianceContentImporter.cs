using System.Text.Json;
using Gccs.Application.Compliance;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Compliance;

public sealed class ComplianceContentImporter(GccsDbContext dbContext) : IComplianceContentImporter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<ComplianceContentImportReport> ImportDirectoryAsync(string packageRoot, CancellationToken cancellationToken = default)
    {
        var obligationsDirectory = Path.Combine(packageRoot, "obligations");
        if (!Directory.Exists(obligationsDirectory))
        {
            return FailedReport(new ComplianceContentImportError(packageRoot, "$.obligations", "obligations", "Compliance content package must include an obligations directory."));
        }

        var aggregate = new ImportReportBuilder();
        var files = Directory.EnumerateFiles(obligationsDirectory, "*.json", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .ToArray();

        if (files.Length == 0)
        {
            aggregate.Errors.Add(new ComplianceContentImportError(obligationsDirectory, "$", "file", "No JSON obligation content files were found."));
            aggregate.Logs.Add("Import failed: no obligation content files found.");
            return aggregate.Build();
        }

        foreach (var file in files)
        {
            var report = await ImportFileAsync(file, cancellationToken);
            aggregate.Add(report);
        }

        return aggregate.Build();
    }

    public async Task<ComplianceContentImportReport> ImportFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var builder = new ImportReportBuilder();
        builder.FilesProcessed = 1;
        builder.Logs.Add($"Processing {filePath}.");

        JsonDocument document;
        try
        {
            await using var stream = File.OpenRead(filePath);
            document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        }
        catch (JsonException ex)
        {
            builder.Errors.Add(new ComplianceContentImportError(filePath, "$", "json", $"Invalid JSON: {ex.Message}"));
            builder.Logs.Add($"Import failed for {filePath}: invalid JSON.");
            return builder.Build();
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                builder.Errors.Add(new ComplianceContentImportError(filePath, "$", "root", "Obligation content must be a JSON array."));
                builder.Logs.Add($"Import failed for {filePath}: root element was not an array.");
                return builder.Build();
            }

            var items = new List<ContentObligation>();
            var index = 0;
            foreach (var element in document.RootElement.EnumerateArray())
            {
                var path = $"$[{index}]";
                if (TryReadObligation(filePath, path, element, builder.Errors, out var obligation))
                {
                    items.Add(obligation);
                }

                index++;
            }

            if (builder.Errors.Count > 0)
            {
                builder.Logs.Add($"Import failed for {filePath}: {builder.Errors.Count} validation error(s).");
                return builder.Build();
            }

            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var counts = await UpsertAsync(item, cancellationToken);
                builder.ClausesCreated += counts.ClausesCreated;
                builder.ClausesUpdated += counts.ClausesUpdated;
                builder.ClauseObligationMappingsCreated += counts.ClauseObligationMappingsCreated;
                builder.ClauseObligationMappingsUpdated += counts.ClauseObligationMappingsUpdated;
                builder.ObligationsCreated += counts.ObligationsCreated;
                builder.ObligationsUpdated += counts.ObligationsUpdated;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            builder.Logs.Add($"Imported {filePath}: clauses +{builder.ClausesCreated}/~{builder.ClausesUpdated}, mappings +{builder.ClauseObligationMappingsCreated}/~{builder.ClauseObligationMappingsUpdated}, obligations +{builder.ObligationsCreated}/~{builder.ObligationsUpdated}.");
        }

        return builder.Build();
    }

    private async Task<ComplianceContentImportCounts> UpsertAsync(ContentObligation item, CancellationToken cancellationToken)
    {
        var clauseId = item.Id;
        var clauseNumber = item.Source.Replace("FAR ", "", StringComparison.OrdinalIgnoreCase)
            .Replace("DFARS ", "", StringComparison.OrdinalIgnoreCase);
        var clause = await dbContext.Clauses.FindAsync([clauseId], cancellationToken);
        var clauseCreated = clause is null;
        clause ??= new ClauseEntity { Id = clauseId };

        clause.Source = item.Source;
        clause.Number = clauseNumber;
        clause.Title = item.Title;
        clause.PlainEnglishSummary = item.TriggerCondition;
        clause.ApplicabilityLogic = BuildApplicabilityLogic(item);
        clause.ClauseTextVersion = item.ClauseTextVersion;
        clause.ClauseEffectiveAt = item.EffectiveAt;
        clause.SourceHash = item.SourceHash;
        clause.SupersededByClauseId = item.SupersededBy;
        clause.RequiredActionIdsJson = JsonSerializer.Serialize(item.RequiredActions, SerializerOptions);
        clause.UsuallyRequiresFlowDown = !string.IsNullOrWhiteSpace(item.FlowDownRequirement);
        clause.SourceName = item.Source;
        clause.SourceUrl = item.SourceUrl.ToString();
        clause.SourceLastReviewedAt = item.LastReviewedAt;
        clause.SourceEffectiveAt = item.EffectiveAt;
        clause.SourceConfidence = item.Confidence;
        clause.SourceRequiresExpertReview = item.RequiresExpertReview;
        clause.LastReviewedAt = item.LastReviewedAt;
        clause.Confidence = item.Confidence;
        clause.RequiresExpertReview = item.RequiresExpertReview;
        clause.ReviewState = item.ReviewState;

        if (clauseCreated)
        {
            dbContext.Clauses.Add(clause);
        }

        var obligation = await dbContext.Obligations.FindAsync([item.Id], cancellationToken);
        var obligationCreated = obligation is null;
        obligation ??= new ObligationEntity { Id = item.Id };

        obligation.Source = item.Source;
        obligation.Title = item.Title;
        obligation.PlainEnglishSummary = item.Title;
        obligation.TriggerCondition = item.TriggerCondition;
        obligation.RequiredAction = string.Join(" ", item.RequiredActions);
        obligation.OwnerFunction = InferOwnerFunction(item);
        obligation.RiskLevel = item.RiskLevel;
        obligation.RequiresFlowDown = !string.IsNullOrWhiteSpace(item.FlowDownRequirement);
        obligation.FlowDownRequirement = item.FlowDownRequirement;
        obligation.ApplicabilityJson = JsonSerializer.Serialize(new
        {
            appliesTo = item.AppliesTo,
            contractTypes = item.ContractTypes,
            dataTypes = item.DataTypes,
            reportingDeadline = item.ReportingDeadline
        }, SerializerOptions);
        obligation.EvidenceExamplesJson = JsonSerializer.Serialize(
            item.EvidenceExamples.Select(example => new
            {
                name = example,
                description = $"Evidence supporting {item.Source}.",
                owner = InferOwnerFunction(item)
            }),
            SerializerOptions);
        obligation.SourceName = item.Source;
        obligation.SourceUrl = item.SourceUrl.ToString();
        obligation.SourceLastReviewedAt = item.LastReviewedAt;
        obligation.SourceEffectiveAt = item.EffectiveAt;
        obligation.SourceConfidence = item.Confidence;
        obligation.SourceRequiresExpertReview = item.RequiresExpertReview;
        obligation.LastReviewedAt = item.LastReviewedAt;
        obligation.Confidence = item.Confidence;
        obligation.RequiresExpertReview = item.RequiresExpertReview;
        obligation.ReviewState = item.ReviewState;

        if (obligationCreated)
        {
            dbContext.Obligations.Add(obligation);
        }

        var mappingId = StableMappingId(item.Id);
        var mapping = await dbContext.ClauseObligationMappings.FindAsync([mappingId], cancellationToken);
        var mappingCreated = mapping is null;
        mapping ??= new ClauseObligationMappingEntity
        {
            Id = mappingId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        mapping.TenantId = null;
        mapping.ClauseId = clause.Id;
        mapping.ObligationId = obligation.Id;
        mapping.TriggerCondition = item.TriggerCondition;
        mapping.RequiredAction = string.Join(" ", item.RequiredActions);
        mapping.SourceUrl = item.SourceUrl.ToString();
        mapping.Confidence = item.Confidence;
        mapping.RequiresExpertReview = item.RequiresExpertReview;
        mapping.ReviewState = item.ReviewState;
        mapping.LastReviewedAt = item.LastReviewedAt;

        if (mappingCreated)
        {
            dbContext.ClauseObligationMappings.Add(mapping);
        }

        return new ComplianceContentImportCounts(
            clauseCreated ? 1 : 0,
            clauseCreated ? 0 : 1,
            mappingCreated ? 1 : 0,
            mappingCreated ? 0 : 1,
            obligationCreated ? 1 : 0,
            obligationCreated ? 0 : 1);
    }

    private static Guid StableMappingId(string contentId)
    {
        Span<byte> bytes = stackalloc byte[16];
        System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes($"gccs-content-mapping:{contentId}"), bytes);
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x30);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes);
    }

    private static bool TryReadObligation(
        string filePath,
        string path,
        JsonElement element,
        ICollection<ComplianceContentImportError> errors,
        out ContentObligation obligation)
    {
        obligation = default!;
        var startingErrorCount = errors.Count;
        if (element.ValueKind != JsonValueKind.Object)
        {
            errors.Add(new ComplianceContentImportError(filePath, path, "item", "Each obligation entry must be an object."));
            return false;
        }

        var id = RequiredString(filePath, path, element, "id", errors);
        var source = RequiredString(filePath, path, element, "source", errors);
        var title = RequiredString(filePath, path, element, "title", errors);
        var triggerCondition = RequiredString(filePath, path, element, "trigger_condition", errors);
        var requiredActions = RequiredStringArray(filePath, path, element, "required_actions", errors);
        var evidenceExamples = RequiredStringArray(filePath, path, element, "evidence_examples", errors);
        var appliesTo = OptionalStringArray(element, "applies_to");
        var contractTypes = OptionalStringArray(element, "contract_types");
        var dataTypes = OptionalStringArray(element, "data_types");
        var flowDownRequirement = OptionalString(element, "flow_down_requirement") ?? string.Empty;
        var riskLevelValue = RequiredString(filePath, path, element, "risk_level", errors);
        var sourceUrlValue = RequiredString(filePath, path, element, "source_url", errors);
        var lastReviewedValue = RequiredString(filePath, path, element, "last_reviewed_at", errors);
        var confidence = RequiredString(filePath, path, element, "confidence", errors);
        var reviewOwner = RequiredString(filePath, path, element, "review_owner", errors);
        var reviewStateValue = RequiredString(filePath, path, element, "review_state", errors);

        var riskLevel = RiskLevel.Low;
        if (!Enum.TryParse(riskLevelValue, ignoreCase: true, out riskLevel))
        {
            errors.Add(new ComplianceContentImportError(filePath, $"{path}.risk_level", "risk_level", $"Unsupported risk level '{riskLevelValue}'."));
        }

        Uri? sourceUrl = null;
        if (!string.IsNullOrWhiteSpace(sourceUrlValue) && !Uri.TryCreate(sourceUrlValue, UriKind.Absolute, out sourceUrl))
        {
            errors.Add(new ComplianceContentImportError(filePath, $"{path}.source_url", "source_url", "Source URL must be an absolute URI."));
        }

        var lastReviewedAt = default(DateOnly);
        if (!string.IsNullOrWhiteSpace(lastReviewedValue) && !DateOnly.TryParse(lastReviewedValue, out lastReviewedAt))
        {
            errors.Add(new ComplianceContentImportError(filePath, $"{path}.last_reviewed_at", "last_reviewed_at", "Last reviewed date must use ISO yyyy-MM-dd format."));
        }

        if (errors.Count > startingErrorCount)
        {
            return false;
        }

        var reviewState = MapReviewState(reviewStateValue);
        var effectiveAt = OptionalDate(filePath, path, element, "effective_at", errors);
        var requiresExpertReview = OptionalBool(element, "requires_expert_review");

        obligation = new ContentObligation(
            id,
            source,
            title,
            triggerCondition,
            appliesTo,
            contractTypes,
            dataTypes,
            requiredActions,
            evidenceExamples,
            OptionalString(element, "reporting_deadline"),
            flowDownRequirement,
            riskLevel,
            sourceUrl!,
            effectiveAt,
            OptionalString(element, "clause_text_version") ?? "current",
            OptionalString(element, "source_hash"),
            lastReviewedAt,
            confidence,
            requiresExpertReview,
            reviewOwner,
            reviewState,
            OptionalString(element, "superseded_by"));

        if (obligation.ReviewState is ReviewState.Published)
        {
            ValidatePublishableObligation(filePath, path, obligation, errors);
        }

        return errors.Count == 0;
    }

    private static void ValidatePublishableObligation(
        string filePath,
        string path,
        ContentObligation item,
        ICollection<ComplianceContentImportError> errors)
    {
        var obligation = new Obligation(
            item.Id,
            item.Source,
            item.Title,
            item.Title,
            item.TriggerCondition,
            string.Join(" ", item.RequiredActions),
            InferOwnerFunction(item),
            item.RiskLevel,
            !string.IsNullOrWhiteSpace(item.FlowDownRequirement),
            item.FlowDownRequirement,
            new ApplicabilityDimension(
                string.Join(", ", item.AppliesTo),
                string.Join(", ", item.ContractTypes),
                string.Join(", ", item.DataTypes),
                "any",
                "any",
                item.TriggerCondition),
            item.EvidenceExamples.Select(example => new EvidenceExample(example, $"Evidence supporting {item.Source}.", InferOwnerFunction(item))).ToArray(),
            new ComplianceSource(item.Source, item.SourceUrl, item.LastReviewedAt, item.EffectiveAt, item.Confidence, item.RequiresExpertReview),
            new ReviewMetadata(item.LastReviewedAt, null, null, item.Confidence, item.RequiresExpertReview, item.ReviewState));

        foreach (var error in ObligationPublicationValidator.ValidateForPublication(obligation))
        {
            errors.Add(new ComplianceContentImportError(filePath, path, "obligation", error));
        }
    }

    private static string RequiredString(string filePath, string path, JsonElement element, string field, ICollection<ComplianceContentImportError> errors)
    {
        if (!element.TryGetProperty(field, out var value) || value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
        {
            errors.Add(new ComplianceContentImportError(filePath, $"{path}.{field}", field, $"Field '{field}' is required."));
            return string.Empty;
        }

        return value.GetString()!;
    }

    private static IReadOnlyList<string> RequiredStringArray(string filePath, string path, JsonElement element, string field, ICollection<ComplianceContentImportError> errors)
    {
        if (!element.TryGetProperty(field, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            errors.Add(new ComplianceContentImportError(filePath, $"{path}.{field}", field, $"Field '{field}' must be a non-empty string array."));
            return [];
        }

        var values = value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
            .Select(item => item.GetString()!)
            .ToArray();

        if (values.Length == 0)
        {
            errors.Add(new ComplianceContentImportError(filePath, $"{path}.{field}", field, $"Field '{field}' must include at least one value."));
        }

        return values;
    }

    private static IReadOnlyList<string> OptionalStringArray(JsonElement element, string field)
    {
        if (!element.TryGetProperty(field, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
            .Select(item => item.GetString()!)
            .ToArray();
    }

    private static string? OptionalString(JsonElement element, string field)
    {
        if (!element.TryGetProperty(field, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static bool OptionalBool(JsonElement element, string field)
    {
        if (!element.TryGetProperty(field, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return false;
        }

        return value.ValueKind == JsonValueKind.True;
    }

    private static DateOnly? OptionalDate(string filePath, string path, JsonElement element, string field, ICollection<ComplianceContentImportError> errors)
    {
        var value = OptionalString(element, field);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateOnly.TryParse(value, out var date))
        {
            return date;
        }

        errors.Add(new ComplianceContentImportError(filePath, $"{path}.{field}", field, $"Field '{field}' must use ISO yyyy-MM-dd format when supplied."));
        return null;
    }

    private static ReviewState MapReviewState(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "approved" => ReviewState.Approved,
            "published" => ReviewState.Published,
            "in_review" => ReviewState.InReview,
            "in-review" => ReviewState.InReview,
            "needs_review" => ReviewState.InReview,
            "needs-review" => ReviewState.InReview,
            "draft" => ReviewState.Draft,
            "retired" => ReviewState.Retired,
            _ => ReviewState.Draft
        };

    private static string BuildApplicabilityLogic(ContentObligation item) =>
        $"{item.TriggerCondition} Applies to {string.Join(", ", item.AppliesTo.DefaultIfEmpty("unspecified roles"))}; contract types: {string.Join(", ", item.ContractTypes.DefaultIfEmpty("unspecified"))}; data: {string.Join(", ", item.DataTypes.DefaultIfEmpty("unspecified"))}.";

    private static string InferOwnerFunction(ContentObligation item)
    {
        if (item.DataTypes.Any(value => value.Contains("CUI", StringComparison.OrdinalIgnoreCase) || value.Contains("FCI", StringComparison.OrdinalIgnoreCase)))
        {
            return "IT/security";
        }

        if (item.DataTypes.Any(value => value.Contains("labor", StringComparison.OrdinalIgnoreCase) || value.Contains("payroll", StringComparison.OrdinalIgnoreCase)))
        {
            return "HR/payroll";
        }

        return "Contracts";
    }

    private static ComplianceContentImportReport FailedReport(ComplianceContentImportError error) =>
        new(false, 0, 0, 0, 0, 0, 0, 0, [error], [$"Import failed: {error.Message}"]);

    private sealed class ImportReportBuilder
    {
        public int FilesProcessed { get; set; }
        public int ClausesCreated { get; set; }
        public int ClausesUpdated { get; set; }
        public int ClauseObligationMappingsCreated { get; set; }
        public int ClauseObligationMappingsUpdated { get; set; }
        public int ObligationsCreated { get; set; }
        public int ObligationsUpdated { get; set; }
        public List<ComplianceContentImportError> Errors { get; } = [];
        public List<string> Logs { get; } = [];

        public void Add(ComplianceContentImportReport report)
        {
            FilesProcessed += report.FilesProcessed;
            ClausesCreated += report.ClausesCreated;
            ClausesUpdated += report.ClausesUpdated;
            ClauseObligationMappingsCreated += report.ClauseObligationMappingsCreated;
            ClauseObligationMappingsUpdated += report.ClauseObligationMappingsUpdated;
            ObligationsCreated += report.ObligationsCreated;
            ObligationsUpdated += report.ObligationsUpdated;
            Errors.AddRange(report.Errors);
            Logs.AddRange(report.Logs);
        }

        public ComplianceContentImportReport Build() =>
            new(
                Errors.Count == 0,
                FilesProcessed,
                ClausesCreated,
                ClausesUpdated,
                ClauseObligationMappingsCreated,
                ClauseObligationMappingsUpdated,
                ObligationsCreated,
                ObligationsUpdated,
                Errors,
                Logs);
    }

    private sealed record ContentObligation(
        string Id,
        string Source,
        string Title,
        string TriggerCondition,
        IReadOnlyList<string> AppliesTo,
        IReadOnlyList<string> ContractTypes,
        IReadOnlyList<string> DataTypes,
        IReadOnlyList<string> RequiredActions,
        IReadOnlyList<string> EvidenceExamples,
        string? ReportingDeadline,
        string FlowDownRequirement,
        RiskLevel RiskLevel,
        Uri SourceUrl,
        DateOnly? EffectiveAt,
        string ClauseTextVersion,
        string? SourceHash,
        DateOnly LastReviewedAt,
        string Confidence,
        bool RequiresExpertReview,
        string ReviewOwner,
        ReviewState ReviewState,
        string? SupersededBy);
}
