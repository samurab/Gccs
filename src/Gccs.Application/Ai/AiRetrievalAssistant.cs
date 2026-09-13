using Gccs.Application.Audit;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Domain.Tenancy;

namespace Gccs.Application.Ai;

public sealed class AiRetrievalAssistantService(
    IAiRetrievalSourceRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public const int MaximumCandidateCount = 128;
    public const int MaximumCitationCount = 8;
    private const int MaximumLoggedDecisionCount = 32;
    public const string PolicyVersion = "approved-retrieval-v2";

    public async Task<AiAssistantResponseDto> AnswerAsync(
        AiAssistantQuestionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.HasAssistantPermission)
        {
            throw new AiRetrievalPolicyException("Assistant permission is required.");
        }

        var batch = await repository.SearchSourcesAsync(
            new AiRetrievalSourceQuery(
                request.TenantId,
                request.Question,
                request.WorkflowContext,
                request.SourceAccess,
                MaximumCandidateCount),
            cancellationToken);
        var decisions = batch.Sources
            .Take(MaximumCandidateCount)
            .Select(source => EvaluateSource(source, request, batch.DataPosture))
            .ToArray();
        var approved = decisions
            .Where(decision => decision.Decision == AiRetrievalPolicyDecision.Included)
            .Select(decision => decision.Source)
            .Take(MaximumCitationCount)
            .ToArray();

        AiAssistantResponseDto response;
        if (approved.Length == 0)
        {
            response = new AiAssistantResponseDto(
                request.TenantId,
                "NeedsReview",
                "I do not have an approved source that supports an answer. Please route this question for human review.",
                [],
                decisions.Select(ToPolicyLog).ToArray());
        }
        else
        {
            response = new AiAssistantResponseDto(
                request.TenantId,
                "Draft",
                string.Join("\n", approved.Select(source => $"- [{source.Id}] {NormalizeStatement(source.Summary)}")),
                approved.Select(source => new AiCitationDto(
                    source.Id,
                    source.Title,
                    source.SourceType,
                    source.SourceUrl,
                    source.TenantRecordReference,
                    source.ExcerptPointer,
                    source.Version,
                    source.LastReviewedAt)).ToArray(),
                decisions.Select(ToPolicyLog).ToArray());
        }

        await auditEventWriter.WriteAsync(
            request.TenantId,
            request.ActorUserId,
            AuditAction.Viewed,
            "AiRetrieval",
            request.WorkflowContext,
            "AI retrieval sources were evaluated.",
            new Dictionary<string, string>
            {
                ["workflowContext"] = request.WorkflowContext,
                ["retrievedSourceIds"] = string.Join("|", response.Citations.Select(citation => citation.SourceId)),
                ["policyDecisions"] = string.Join("|", response.PolicyLogs.Take(MaximumLoggedDecisionCount)
                    .Select(log => $"{log.SourceId}:{log.Decision}:{log.Reason}")),
                ["policyDecisionCounts"] = string.Join("|", response.PolicyLogs
                    .GroupBy(log => $"{log.Decision}:{log.Reason}", StringComparer.Ordinal)
                    .OrderBy(group => group.Key, StringComparer.Ordinal)
                    .Select(group => $"{group.Key}:{group.Count()}")),
                ["policyVersion"] = PolicyVersion,
                ["retrievalStrategy"] = batch.RetrievalStrategy,
                ["tenantDataPosture"] = batch.DataPosture.ToString(),
                ["responseStatus"] = response.Status
            },
            cancellationToken);

        return response;
    }

    private static AiRetrievalPolicyEvaluation EvaluateSource(
        AiRetrievalSourceDto source,
        AiAssistantQuestionRequest request,
        TenantDataPosture dataPosture)
    {
        if (source.TenantId is { } sourceTenantId && sourceTenantId != request.TenantId)
        {
            return new(source, AiRetrievalPolicyDecision.Excluded, "cross-tenant");
        }

        if (!source.IsApproved)
        {
            return new(source, AiRetrievalPolicyDecision.Excluded, "unapproved");
        }

        if ((request.SourceAccess & source.SourceKind.ToAccessFlag()) == 0)
        {
            return new(source, AiRetrievalPolicyDecision.Excluded, "rbac-source-family");
        }

        if (source.IsUseBlocked)
        {
            return new(source, AiRetrievalPolicyDecision.Excluded, "data-handling-blocked");
        }

        if (source.IsExpired)
        {
            return new(source, AiRetrievalPolicyDecision.Excluded, "expired");
        }

        if (source.TenantId is not null && dataPosture == TenantDataPosture.DemoSandbox &&
            source.Classification != ContentClassification.Unclassified)
        {
            return new(source, AiRetrievalPolicyDecision.Excluded, "tenant-posture");
        }

        if (source.Classification is ContentClassification.Prohibited or ContentClassification.Unknown or
            ContentClassification.Cui or ContentClassification.SyntheticCui)
        {
            return new(source, AiRetrievalPolicyDecision.Excluded, "unsafe-classification");
        }

        if (source.SourceKind == AiRetrievalSourceKind.ComplianceLibrary && !source.IsPublishedLibraryContent)
        {
            return new(source, AiRetrievalPolicyDecision.Excluded, "library-not-published");
        }

        if (source.SourceKind != AiRetrievalSourceKind.ComplianceLibrary && source.TenantId is null)
        {
            return new(source, AiRetrievalPolicyDecision.Excluded, "not-tenant-or-library");
        }

        return new(source, AiRetrievalPolicyDecision.Included, "approved-source");
    }

    private static string NormalizeStatement(string value) =>
        string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    private static AiRetrievalPolicyLogDto ToPolicyLog(AiRetrievalPolicyEvaluation evaluation) =>
        new(evaluation.Reason == "cross-tenant" ? "[cross-tenant-redacted]" : evaluation.Source.Id,
            evaluation.Decision, evaluation.Reason);
}

public interface IAiRetrievalSourceRepository
{
    Task<AiRetrievalSourceBatch> SearchSourcesAsync(
        AiRetrievalSourceQuery query,
        CancellationToken cancellationToken = default);
}

public sealed record AiAssistantQuestionRequest(
    Guid TenantId,
    Guid ActorUserId,
    string Question,
    string WorkflowContext,
    bool HasAssistantPermission = false,
    AiRetrievalSourceAccess SourceAccess = AiRetrievalSourceAccess.None);

public sealed record AiAssistantResponseDto(
    Guid TenantId,
    string Status,
    string Answer,
    IReadOnlyList<AiCitationDto> Citations,
    IReadOnlyList<AiRetrievalPolicyLogDto> PolicyLogs);

public sealed record AiRetrievalSourceDto(
    string Id,
    Guid? TenantId,
    string Title,
    string SourceType,
    string? SourceUrl,
    string? TenantRecordReference,
    string ExcerptPointer,
    string Version,
    DateOnly? LastReviewedAt,
    ContentClassification Classification,
    bool IsApproved,
    bool IsPublishedLibraryContent,
    string Summary,
    IReadOnlyList<string> Keywords,
    AiRetrievalSourceKind SourceKind = AiRetrievalSourceKind.ComplianceLibrary,
    bool IsUseBlocked = false,
    bool IsExpired = false);

public sealed record AiCitationDto(
    string SourceId,
    string Title,
    string SourceType,
    string? SourceUrl,
    string? TenantRecordReference,
    string ExcerptPointer,
    string Version,
    DateOnly? LastReviewedAt);

public sealed record AiRetrievalPolicyLogDto(
    string SourceId,
    AiRetrievalPolicyDecision Decision,
    string Reason);

public enum AiRetrievalPolicyDecision
{
    Included,
    Excluded
}

[Flags]
public enum AiRetrievalSourceAccess
{
    None = 0,
    ComplianceLibrary = 1,
    TenantDocument = 2,
    ApprovedReport = 4,
    EvidenceMetadata = 8,
    All = ComplianceLibrary | TenantDocument | ApprovedReport | EvidenceMetadata
}

public enum AiRetrievalSourceKind
{
    ComplianceLibrary,
    TenantDocument,
    ApprovedReport,
    EvidenceMetadata
}

public static class AiRetrievalSourceKindExtensions
{
    public static AiRetrievalSourceAccess ToAccessFlag(this AiRetrievalSourceKind sourceKind) => sourceKind switch
    {
        AiRetrievalSourceKind.ComplianceLibrary => AiRetrievalSourceAccess.ComplianceLibrary,
        AiRetrievalSourceKind.TenantDocument => AiRetrievalSourceAccess.TenantDocument,
        AiRetrievalSourceKind.ApprovedReport => AiRetrievalSourceAccess.ApprovedReport,
        AiRetrievalSourceKind.EvidenceMetadata => AiRetrievalSourceAccess.EvidenceMetadata,
        _ => AiRetrievalSourceAccess.None
    };
}

public sealed record AiRetrievalSourceQuery(
    Guid TenantId,
    string Question,
    string WorkflowContext,
    AiRetrievalSourceAccess SourceAccess,
    int MaximumCandidates);

public sealed record AiRetrievalSourceBatch(
    TenantDataPosture DataPosture,
    IReadOnlyList<AiRetrievalSourceDto> Sources,
    string RetrievalStrategy = "unspecified");

public sealed class AiRetrievalPolicyException(string message) : InvalidOperationException(message);

internal sealed record AiRetrievalPolicyEvaluation(
    AiRetrievalSourceDto Source,
    AiRetrievalPolicyDecision Decision,
    string Reason);
