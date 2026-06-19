using Gccs.Application.Audit;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;

namespace Gccs.Application.Ai;

public sealed class AiRetrievalAssistantService(
    IAiRetrievalSourceRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public async Task<AiAssistantResponseDto> AnswerAsync(
        AiAssistantQuestionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.HasAssistantPermission)
        {
            throw new AiRetrievalPolicyException("Assistant permission is required.");
        }

        var sources = await repository.ListSourcesAsync(request.TenantId, cancellationToken);
        var decisions = sources
            .Select(source => EvaluateSource(source, request))
            .ToArray();
        var approved = decisions
            .Where(decision => decision.Decision == AiRetrievalPolicyDecision.Included)
            .Select(decision => decision.Source)
            .Where(source => IsRelevant(source, request.Question))
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
                string.Join(" ", approved.Select(source => $"{source.Summary} [{source.Id}]")),
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
                ["policyDecisions"] = string.Join("|", response.PolicyLogs.Select(log => $"{log.SourceId}:{log.Decision}:{log.Reason}")),
                ["responseStatus"] = response.Status
            },
            cancellationToken);

        return response;
    }

    private static AiRetrievalPolicyEvaluation EvaluateSource(AiRetrievalSourceDto source, AiAssistantQuestionRequest request)
    {
        if (source.TenantId is { } sourceTenantId && sourceTenantId != request.TenantId)
        {
            return new(source, AiRetrievalPolicyDecision.Excluded, "cross-tenant");
        }

        if (!source.IsApproved)
        {
            return new(source, AiRetrievalPolicyDecision.Excluded, "unapproved");
        }

        if (source.Classification is ContentClassification.Prohibited or ContentClassification.Unknown or ContentClassification.Cui)
        {
            return new(source, AiRetrievalPolicyDecision.Excluded, "unsafe-classification");
        }

        if (!source.IsPublishedLibraryContent && source.TenantId is null)
        {
            return new(source, AiRetrievalPolicyDecision.Excluded, "not-tenant-or-library");
        }

        return new(source, AiRetrievalPolicyDecision.Included, "approved-source");
    }

    private static bool IsRelevant(AiRetrievalSourceDto source, string question)
    {
        var normalizedQuestion = question.ToLowerInvariant();
        return source.Keywords.Any(keyword => normalizedQuestion.Contains(keyword.ToLowerInvariant(), StringComparison.Ordinal));
    }

    private static AiRetrievalPolicyLogDto ToPolicyLog(AiRetrievalPolicyEvaluation evaluation) =>
        new(evaluation.Source.Id, evaluation.Decision, evaluation.Reason);
}

public interface IAiRetrievalSourceRepository
{
    Task<IReadOnlyList<AiRetrievalSourceDto>> ListSourcesAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public sealed record AiAssistantQuestionRequest(
    Guid TenantId,
    Guid ActorUserId,
    string Question,
    string WorkflowContext,
    bool HasAssistantPermission = true);

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
    IReadOnlyList<string> Keywords);

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

public sealed class AiRetrievalPolicyException(string message) : InvalidOperationException(message);

internal sealed record AiRetrievalPolicyEvaluation(
    AiRetrievalSourceDto Source,
    AiRetrievalPolicyDecision Decision,
    string Reason);
