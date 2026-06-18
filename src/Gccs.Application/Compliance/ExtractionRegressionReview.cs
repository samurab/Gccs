using Gccs.Application.Audit;
using Gccs.Application.Tasks;
using Gccs.Domain.Audit;
using Gccs.Domain.Compliance;

namespace Gccs.Application.Compliance;

public sealed class ExtractionRegressionReviewService(
    IExtractionRegressionReviewRepository repository,
    ComplianceTaskService taskService,
    IAuditEventWriter auditEventWriter)
{
    public async Task<ExtractionRegressionReviewRecordDto> ReviewFailureAsync(
        ReviewExtractionFailureRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        ValidateReview(normalized);

        var record = await repository.CreateAsync(normalized, tenantId, actorUserId, cancellationToken);
        Guid? taskId = null;
        if (normalized.CreateFollowUpTask)
        {
            var task = await taskService.CreateAsync(
                new CreateComplianceTaskRequest(
                    normalized.FollowUpTaskTitle ?? $"Review extraction {normalized.FailureType} for {normalized.Citation}",
                    $"Resolve extraction regression review record {record.Id} for {normalized.DocumentId}.",
                    "open",
                    normalized.FollowUpTaskPriority,
                    null,
                    normalized.Owner,
                    normalized.FollowUpTaskDueAt,
                    "extraction_regression_review",
                    record.Id.ToString()),
                actorUserId,
                cancellationToken);

            taskId = task.Id;
            record = await repository.LinkFollowUpTaskAsync(record.Id, task.Id, actorUserId, cancellationToken) ?? record;
        }

        await auditEventWriter.WriteAsync(
            record.TenantId,
            actorUserId,
            AuditAction.Created,
            "ExtractionRegressionReview",
            record.Id.ToString(),
            "Extraction regression failure was reviewed.",
            new Dictionary<string, string>
            {
                ["evaluationRunId"] = record.EvaluationRunId,
                ["documentId"] = record.DocumentId,
                ["failureType"] = record.FailureType.ToString(),
                ["citation"] = record.Citation,
                ["classification"] = record.Classification.ToString(),
                ["status"] = record.Status.ToString(),
                ["owner"] = record.Owner,
                ["followUpTaskId"] = taskId?.ToString() ?? string.Empty
            },
            cancellationToken);

        return record;
    }

    public async Task<ExtractionRegressionReviewRecordDto?> ResolveAsync(
        Guid reviewRecordId,
        ResolveExtractionRegressionReviewRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        ValidateResolution(normalized);
        var updated = await repository.ResolveAsync(reviewRecordId, normalized, actorUserId, cancellationToken);
        if (updated is null)
        {
            return null;
        }

        await auditEventWriter.WriteAsync(
            updated.TenantId,
            actorUserId,
            AuditAction.Updated,
            "ExtractionRegressionReview",
            updated.Id.ToString(),
            $"Extraction regression review was marked {updated.Status}.",
            new Dictionary<string, string>
            {
                ["status"] = updated.Status.ToString(),
                ["resolutionNote"] = updated.ResolutionNote,
                ["resolutionLinkCount"] = updated.ResolutionLinks.Count.ToString()
            },
            cancellationToken);

        return updated;
    }

    public async Task<ExtractionRegressionReleaseSummaryDto> GenerateReleaseSummaryAsync(
        GenerateExtractionRegressionReleaseSummaryRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (request.MetricTrends.Count == 0)
        {
            throw new ExtractionRegressionReviewValidationException("At least one metric trend is required.");
        }

        var openRecords = await repository.ListOpenRisksAsync(tenantId, cancellationToken);
        var openRisks = openRecords
            .Select(record => new ExtractionRegressionOpenRiskDto(
                record.Id,
                $"{record.FailureType} for {record.Citation} in {record.DocumentId}: {record.ResolutionNote}",
                record.Owner,
                record.Status))
            .ToArray();
        var latest = request.MetricTrends[^1];
        var readinessNote = openRisks.Length == 0 && latest.Precision >= request.MinimumPrecision && latest.Recall >= request.MinimumRecall
            ? "Extraction regression review has no open risks and current metrics meet release thresholds."
            : "Extraction regression review has open risks or metric trends below release thresholds.";
        var summary = new ExtractionRegressionReleaseSummaryDto(
            tenantId,
            DateTimeOffset.UtcNow,
            request.MetricTrends,
            openRisks,
            readinessNote);

        await auditEventWriter.WriteAsync(
            tenantId,
            actorUserId,
            AuditAction.Created,
            "ExtractionRegressionReleaseSummary",
            summary.GeneratedAt.ToUnixTimeMilliseconds().ToString(),
            "Extraction regression release readiness summary was generated.",
            new Dictionary<string, string>
            {
                ["metricTrendCount"] = summary.MetricTrends.Count.ToString(),
                ["openRiskCount"] = summary.OpenRisks.Count.ToString(),
                ["latestPrecision"] = latest.Precision.ToString("0.####"),
                ["latestRecall"] = latest.Recall.ToString("0.####")
            },
            cancellationToken);

        return summary;
    }

    private static ReviewExtractionFailureRequest Normalize(ReviewExtractionFailureRequest request) =>
        request with
        {
            EvaluationRunId = request.EvaluationRunId.Trim(),
            DocumentId = request.DocumentId.Trim(),
            Citation = request.Citation.Trim(),
            Owner = request.Owner.Trim(),
            ResolutionNote = request.ResolutionNote.Trim(),
            FollowUpTaskTitle = string.IsNullOrWhiteSpace(request.FollowUpTaskTitle) ? null : request.FollowUpTaskTitle.Trim()
        };

    private static ResolveExtractionRegressionReviewRequest Normalize(ResolveExtractionRegressionReviewRequest request) =>
        request with
        {
            ResolutionNote = request.ResolutionNote.Trim()
        };

    private static void ValidateReview(ReviewExtractionFailureRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EvaluationRunId))
        {
            throw new ExtractionRegressionReviewValidationException("Evaluation run id is required.");
        }

        if (string.IsNullOrWhiteSpace(request.DocumentId))
        {
            throw new ExtractionRegressionReviewValidationException("Document id is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Citation))
        {
            throw new ExtractionRegressionReviewValidationException("Clause citation is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Owner))
        {
            throw new ExtractionRegressionReviewValidationException("Review owner is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ResolutionNote))
        {
            throw new ExtractionRegressionReviewValidationException("Resolution note is required.");
        }
    }

    private static void ValidateResolution(ResolveExtractionRegressionReviewRequest request)
    {
        if (request.Status is not (ExtractionRegressionReviewStatus.Resolved or ExtractionRegressionReviewStatus.AcceptedRisk))
        {
            throw new ExtractionRegressionReviewValidationException("Resolved records must be marked resolved or accepted risk.");
        }

        if (string.IsNullOrWhiteSpace(request.ResolutionNote))
        {
            throw new ExtractionRegressionReviewValidationException("Resolution note is required.");
        }

        if (request.Status is ExtractionRegressionReviewStatus.Resolved &&
            request.ResolutionLinks.Count == 0 &&
            request.Classification is not (ExtractionFailureClassification.SourceQuality or ExtractionFailureClassification.ExpectedLimitation))
        {
            throw new ExtractionRegressionReviewValidationException("Resolved parser, matcher, library, and label failures require an update link.");
        }
    }
}

public interface IExtractionRegressionReviewRepository
{
    Task<ExtractionRegressionReviewRecordDto> CreateAsync(
        ReviewExtractionFailureRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<ExtractionRegressionReviewRecordDto?> LinkFollowUpTaskAsync(
        Guid reviewRecordId,
        Guid taskId,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<ExtractionRegressionReviewRecordDto?> ResolveAsync(
        Guid reviewRecordId,
        ResolveExtractionRegressionReviewRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExtractionRegressionReviewRecordDto>> ListOpenRisksAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

public sealed record ReviewExtractionFailureRequest(
    string EvaluationRunId,
    string DocumentId,
    ExtractionRegressionFailureType FailureType,
    string Citation,
    ExtractionFailureClassification Classification,
    string Owner,
    ExtractionRegressionReviewStatus Status,
    string ResolutionNote,
    bool CreateFollowUpTask,
    string? FollowUpTaskTitle,
    RiskLevel FollowUpTaskPriority,
    DateOnly? FollowUpTaskDueAt);

public sealed record ResolveExtractionRegressionReviewRequest(
    ExtractionFailureClassification Classification,
    ExtractionRegressionReviewStatus Status,
    string ResolutionNote,
    IReadOnlyList<ExtractionRegressionResolutionLinkDto> ResolutionLinks);

public sealed record GenerateExtractionRegressionReleaseSummaryRequest(
    IReadOnlyList<ExtractionRegressionMetricTrendDto> MetricTrends,
    decimal MinimumPrecision,
    decimal MinimumRecall);

public sealed record ExtractionRegressionReviewRecordDto(
    Guid Id,
    Guid TenantId,
    string EvaluationRunId,
    string DocumentId,
    ExtractionRegressionFailureType FailureType,
    string Citation,
    ExtractionFailureClassification Classification,
    string Owner,
    ExtractionRegressionReviewStatus Status,
    string ResolutionNote,
    Guid? FollowUpTaskId,
    IReadOnlyList<ExtractionRegressionResolutionLinkDto> ResolutionLinks,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    Guid? UpdatedByUserId,
    DateTimeOffset? UpdatedAt);

public sealed record ExtractionRegressionResolutionLinkDto(
    ExtractionRegressionUpdateLinkType Type,
    string Reference);

public sealed record ExtractionRegressionMetricTrendDto(
    string RunId,
    decimal Precision,
    decimal Recall,
    int FalsePositiveCount,
    int FalseNegativeCount);

public sealed record ExtractionRegressionOpenRiskDto(
    Guid RecordId,
    string Risk,
    string Owner,
    ExtractionRegressionReviewStatus Status);

public sealed record ExtractionRegressionReleaseSummaryDto(
    Guid TenantId,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<ExtractionRegressionMetricTrendDto> MetricTrends,
    IReadOnlyList<ExtractionRegressionOpenRiskDto> OpenRisks,
    string ReleaseReadinessNote);

public enum ExtractionRegressionFailureType
{
    MissedClause,
    FalsePositive
}

public enum ExtractionFailureClassification
{
    Parser,
    Matcher,
    Library,
    Label,
    SourceQuality,
    ExpectedLimitation
}

public enum ExtractionRegressionReviewStatus
{
    Open,
    InProgress,
    Resolved,
    AcceptedRisk
}

public enum ExtractionRegressionUpdateLinkType
{
    Matcher,
    Library,
    Parser,
    Label
}

public sealed class ExtractionRegressionReviewValidationException(string message) : InvalidOperationException(message);
