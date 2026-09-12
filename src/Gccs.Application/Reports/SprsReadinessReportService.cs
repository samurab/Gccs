using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Cmmc;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Reports;

namespace Gccs.Application.Reports;

public sealed class SprsReadinessReportService(
    SprsScoreCalculationService scoreCalculationService,
    ICmmcAssessmentRepository assessmentRepository,
    IReportRepository reportRepository,
    IAuditEventWriter auditEventWriter,
    TenantDataHandlingModePolicyService dataHandlingModePolicy,
    IApplicationTransaction transaction,
    ContentClassificationPolicy classificationPolicy)
{
    public Task<SprsReadinessReportDto?> GenerateAsync(
        Guid assessmentId,
        SprsReadinessReportRequest request,
        string idempotencyKey,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalizedRequest = NormalizeAndValidateRequest(request);
        var normalizedIdempotencyKey = NormalizeIdempotencyKey(idempotencyKey);
        var requestFingerprint = ComputeRequestFingerprint(assessmentId, normalizedRequest);
        return transaction.ExecuteAsync(async transactionCancellationToken =>
        {
            await reportRepository.AcquireSprsReadinessIdempotencyLockAsync(
                normalizedIdempotencyKey,
                transactionCancellationToken);
            var existing = await reportRepository.FindSprsReadinessByIdempotencyKeyAsync(
                normalizedIdempotencyKey,
                transactionCancellationToken);
            if (existing is not null)
            {
                if (!string.Equals(existing.RequestFingerprint, requestFingerprint, StringComparison.Ordinal))
                {
                    await auditEventWriter.WriteAsync(
                        existing.Report.TenantId,
                        actorUserId,
                        AuditAction.Rejected,
                        "Report",
                        existing.Report.Id.ToString(),
                        "SPRS readiness report idempotency key reuse was rejected.",
                        new Dictionary<string, string>
                        {
                            ["reportType"] = ReportType.SprsReadiness.ToString(),
                            ["assessmentId"] = existing.Report.Snapshot.AssessmentId.ToString(),
                            ["reason"] = "idempotency-conflict"
                        },
                        transactionCancellationToken);
                    throw new SprsReadinessIdempotencyConflictException(
                        "The idempotency key has already been used for a different SPRS readiness report request.");
                }

                return existing.Report;
            }

            var classification = normalizedRequest.Classification ??
                throw new ContentClassificationValidationException("Explicit report classification is required.");
            await RejectExplicitRestrictedMarkingsAsync(
                normalizedRequest.ReviewerNotes,
                classification,
                actorUserId,
                transactionCancellationToken);
            await ClassifiedWorkflowValidation.ConfirmAsync(
                classificationPolicy,
                classification,
                TenantDataHandlingWorkflow.Report,
                actorUserId,
                transactionCancellationToken);
            await dataHandlingModePolicy.EnsureAllowedAsync(
                new TenantDataHandlingModePolicyRequest(TenantDataHandlingWorkflow.Report, ContainsRealCui: false),
                actorUserId,
                transactionCancellationToken);

            var assessment = await assessmentRepository.FindCurrentTenantAsync(assessmentId, transactionCancellationToken);
            if (assessment is null)
            {
                return null;
            }

            var calculation = await scoreCalculationService.CalculateAsync(
                assessmentId,
                new SprsScoreCalculationRequest(
                    normalizedRequest.RuleSetId,
                    normalizedRequest.ReviewerNotes,
                    normalizedRequest.ConditionalDeductionSelections,
                    string.IsNullOrWhiteSpace(normalizedRequest.ReviewerNotes) ? null : classification),
                actorUserId,
                transactionCancellationToken);
            if (calculation is null)
            {
                return null;
            }

            var statuses = await assessmentRepository.ListControlStatusesAsync(assessmentId, transactionCancellationToken);
            if (statuses is null)
            {
                return null;
            }

            var statusByControlId = statuses.ToDictionary(status => status.ControlId, StringComparer.OrdinalIgnoreCase);
            var snapshot = new SprsReadinessSnapshotDto(
                calculation.AssessmentId,
                assessment.Name,
                assessment.CompletedAt ?? assessment.StartedAt,
                calculation.Id,
                calculation.Score,
                calculation.MaximumScore,
                calculation.TotalDeduction,
                calculation.RuleSetId,
                calculation.RuleSetVersion,
                calculation.RuleSetSourceUrl,
                calculation.RuleSetSourceSha256,
                calculation.GeneratedAt,
                "Draft",
                ReportArtifactLanguage.SprsReadinessDisclaimer,
                normalizedRequest.LeadershipReviewStatus,
                calculation.ManualNotes,
                BuildDeductions(calculation),
                BuildUnresolvedControls(calculation, statusByControlId));

            var report = await reportRepository.SaveSprsReadinessReportAsync(
                snapshot,
                assessment.Name,
                actorUserId,
                classification,
                normalizedIdempotencyKey,
                requestFingerprint,
                transactionCancellationToken);

            await auditEventWriter.WriteAsync(
                report.TenantId,
                actorUserId,
                AuditAction.Created,
                "Report",
                report.Id.ToString(),
                "SPRS readiness report was generated.",
                new Dictionary<string, string>
                {
                    ["reportType"] = report.Type.ToString(),
                    ["assessmentId"] = assessmentId.ToString(),
                    ["calculationId"] = calculation.Id.ToString(),
                    ["ruleSetVersion"] = calculation.RuleSetVersion,
                    ["score"] = calculation.Score.ToString(),
                    ["totalDeduction"] = calculation.TotalDeduction.ToString(),
                    ["generatedAt"] = report.GeneratedAt.ToString("O"),
                    ["submissionStatus"] = "not-submitted"
                },
                transactionCancellationToken);

            return report;
        }, cancellationToken);
    }

    private static SprsReadinessReportRequest NormalizeAndValidateRequest(SprsReadinessReportRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RuleSetId) || request.RuleSetId.Length > 200)
        {
            throw new SprsReadinessReportException("A valid SPRS scoring rule set ID is required.");
        }

        if (request.ReviewerNotes?.Length > 2_000)
        {
            throw new SprsReadinessReportException("Reviewer notes cannot exceed 2,000 characters.");
        }

        var conditionalSelections = request.ConditionalDeductionSelections?
            .Select(selection => new SprsConditionalDeductionSelection(
                selection.RequirementId?.Trim() ?? string.Empty,
                selection.OptionCode?.Trim() ?? string.Empty))
            .OrderBy(selection => selection.RequirementId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(selection => selection.OptionCode, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return request with
        {
            RuleSetId = request.RuleSetId.Trim(),
            ReviewerNotes = string.IsNullOrWhiteSpace(request.ReviewerNotes) ? null : request.ReviewerNotes.Trim(),
            LeadershipReviewStatus = NormalizeLeadershipReviewStatus(request.LeadershipReviewStatus),
            ConditionalDeductionSelections = conditionalSelections
        };
    }

    private static string NormalizeIdempotencyKey(string value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length is 0 or > 128)
        {
            throw new SprsReadinessReportException("Idempotency-Key is required and must be 128 characters or fewer.");
        }

        return normalized;
    }

    private static string ComputeRequestFingerprint(Guid assessmentId, SprsReadinessReportRequest request)
    {
        var canonicalRequest = JsonSerializer.Serialize(new { assessmentId, request });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest)));
    }

    private async Task RejectExplicitRestrictedMarkingsAsync(
        string? reviewerNotes,
        ContentClassificationRequest classification,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (classification.Classification is not (Gccs.Domain.Common.ContentClassification.Unclassified or
            Gccs.Domain.Common.ContentClassification.Fci) ||
            !SensitiveContentMarkerDetector.ContainsExplicitRestrictedMarking(reviewerNotes))
        {
            return;
        }

        await dataHandlingModePolicy.EnsureAllowedAsync(
            new TenantDataHandlingModePolicyRequest(
                TenantDataHandlingWorkflow.Report,
                ContainsRealCui: true,
                ClassificationConfirmed: false,
                EntityType: "SprsReadinessReportDraft",
                EntityId: "unpersisted"),
            actorUserId,
            cancellationToken);
        throw new ContentClassificationValidationException(
            "Reviewer notes contain an explicit restricted-data marking that conflicts with the selected classification.");
    }

    private static string? NormalizeLeadershipReviewStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized is not ("Pending" or "Reviewed" or "NeedsChanges"))
        {
            throw new SprsReadinessReportException(
                "Leadership review status must be Pending, Reviewed, or NeedsChanges.");
        }

        return normalized;
    }

    private static IReadOnlyList<SprsReadinessDeductionDto> BuildDeductions(SprsScoreCalculationDto calculation) =>
        calculation.LineItems
            .Where(item => item.AppliedDeduction > 0)
            .Select(item => new SprsReadinessDeductionDto(
                item.RequirementId,
                item.ControlId,
                item.Title,
                item.AppliedDeduction,
                item.Reason))
            .ToArray();

    private static IReadOnlyList<SprsReadinessUnresolvedControlDto> BuildUnresolvedControls(
        SprsScoreCalculationDto calculation,
        IReadOnlyDictionary<string, CmmcControlStatusDto> statusByControlId) =>
        calculation.UnresolvedGaps
            .Select(gap =>
            {
                var status = gap.ControlId is null || !statusByControlId.TryGetValue(gap.ControlId, out var matched)
                    ? null
                    : matched;
                return new SprsReadinessUnresolvedControlDto(
                    gap.RequirementId,
                    gap.ControlId,
                    gap.Title,
                    gap.Reason,
                    status?.PoamItemIds ?? [],
                    status?.EvidenceItemIds.Count > 0 ? "Linked" : "Missing");
            })
            .ToArray();
}

public sealed record SprsReadinessReportRequest(
    string RuleSetId,
    string? ReviewerNotes,
    string? LeadershipReviewStatus,
    IReadOnlyList<SprsConditionalDeductionSelection>? ConditionalDeductionSelections,
    [property: System.Text.Json.Serialization.JsonRequired] ContentClassificationRequest? Classification);

public sealed record SprsReadinessReportDto(
    Guid Id,
    Guid TenantId,
    ReportType Type,
    ReportStatus Status,
    string Title,
    DateTimeOffset GeneratedAt,
    Guid GeneratedByUserId,
    SprsReadinessSnapshotDto Snapshot,
    string ExportHtml,
    bool IsReplay = false)
{
    public ContentClassificationDto? Classification { get; init; }
    public string Disclaimer => ReportArtifactLanguage.SprsReadinessDisclaimer;
}

public sealed record SprsReadinessSnapshotDto(
    Guid AssessmentId,
    string AssessmentName,
    DateOnly AssessmentDate,
    Guid CalculationId,
    int Score,
    int MaximumScore,
    int TotalDeduction,
    string RuleSetId,
    string RuleSetVersion,
    string RuleSetSourceUrl,
    string RuleSetSourceSha256,
    DateTimeOffset GeneratedAt,
    string ArtifactStatus,
    string SubmissionStatement,
    string? LeadershipReviewStatus,
    string ReviewerNotes,
    IReadOnlyList<SprsReadinessDeductionDto> Deductions,
    IReadOnlyList<SprsReadinessUnresolvedControlDto> UnresolvedControls);

public sealed record SprsReadinessDeductionDto(
    string RequirementId,
    string? ControlId,
    string Title,
    int Deduction,
    string Reason);

public sealed record SprsReadinessUnresolvedControlDto(
    string RequirementId,
    string? ControlId,
    string Title,
    string Reason,
    IReadOnlyList<Guid> PoamItemIds,
    string EvidenceStatus);

public sealed class SprsReadinessReportException(string message) : ArgumentException(message);

public sealed class SprsReadinessIdempotencyConflictException(string message) : InvalidOperationException(message);
