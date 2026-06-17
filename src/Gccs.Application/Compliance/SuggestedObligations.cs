using Gccs.Application.Audit;
using Gccs.Domain.Audit;
using Gccs.Domain.Compliance;

namespace Gccs.Application.Compliance;

public sealed class SuggestedObligationService(
    ISuggestedObligationRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public async Task<SuggestedObligationDto> CreateAsync(
        CreateSuggestedObligationRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        Validate(normalized);
        var suggestion = await repository.CreateAsync(normalized, tenantId, actorUserId, cancellationToken);
        await WriteAuditAsync(suggestion, actorUserId, AuditAction.Created, "AI-suggested obligation was created.", cancellationToken);
        return suggestion;
    }

    public Task<IReadOnlyList<SuggestedObligationDto>> ListAsync(
        string? reviewStatus,
        CancellationToken cancellationToken = default) =>
        repository.ListAsync(reviewStatus, cancellationToken);

    public Task<SuggestedObligationDto?> FindAsync(
        Guid suggestionId,
        CancellationToken cancellationToken = default) =>
        repository.FindAsync(suggestionId, cancellationToken);

    public async Task<SuggestedObligationDto?> ApproveAsync(
        Guid suggestionId,
        SuggestedObligationReviewRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        ValidateReview(normalized);
        var suggestion = await repository.ApproveAsync(suggestionId, normalized, actorUserId, cancellationToken);
        if (suggestion is not null)
        {
            await WriteAuditAsync(suggestion, actorUserId, AuditAction.Approved, "AI-suggested obligation was approved.", cancellationToken);
        }

        return suggestion;
    }

    public async Task<SuggestedObligationDto?> ReviseAsync(
        Guid suggestionId,
        ReviseSuggestedObligationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        Validate(normalized);
        var suggestion = await repository.ReviseAsync(suggestionId, normalized, actorUserId, cancellationToken);
        if (suggestion is not null)
        {
            await WriteAuditAsync(suggestion, actorUserId, AuditAction.Updated, "AI-suggested obligation was revised.", cancellationToken);
        }

        return suggestion;
    }

    public async Task<SuggestedObligationDto?> RejectAsync(
        Guid suggestionId,
        SuggestedObligationReviewRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        ValidateReview(normalized);
        var suggestion = await repository.RejectAsync(suggestionId, normalized, actorUserId, cancellationToken);
        if (suggestion is not null)
        {
            await WriteAuditAsync(suggestion, actorUserId, AuditAction.Rejected, "AI-suggested obligation was rejected.", cancellationToken);
        }

        return suggestion;
    }

    public async Task<SuggestedObligationDto?> EscalateAsync(
        Guid suggestionId,
        SuggestedObligationReviewRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        ValidateReview(normalized);
        var suggestion = await repository.EscalateAsync(suggestionId, normalized, actorUserId, cancellationToken);
        if (suggestion is not null)
        {
            await WriteAuditAsync(suggestion, actorUserId, AuditAction.Updated, "AI-suggested obligation was escalated for expert review.", cancellationToken);
        }

        return suggestion;
    }

    private static CreateSuggestedObligationRequest Normalize(CreateSuggestedObligationRequest request) =>
        request with
        {
            Source = request.Source.Trim(),
            SourceUrl = request.SourceUrl.Trim(),
            GeneratedSummary = request.GeneratedSummary.Trim(),
            ProposedTitle = request.ProposedTitle.Trim(),
            ProposedOwnerFunction = request.ProposedOwnerFunction.Trim(),
            RequiredAction = request.RequiredAction.Trim(),
            Confidence = request.Confidence.Trim(),
            PromptVersion = request.PromptVersion.Trim(),
            ModelIdentifier = request.ModelIdentifier.Trim(),
            SourceCitations = request.SourceCitations.Select(item => item.Trim()).Where(item => item.Length > 0).Distinct(StringComparer.Ordinal).ToArray(),
            RetrievedSourceReferences = request.RetrievedSourceReferences.Select(item => item.Trim()).Where(item => item.Length > 0).Distinct(StringComparer.Ordinal).ToArray(),
            EvidenceSuggestions = request.EvidenceSuggestions.Select(item => item.Trim()).Where(item => item.Length > 0).Distinct(StringComparer.Ordinal).ToArray()
        };

    private static ReviseSuggestedObligationRequest Normalize(ReviseSuggestedObligationRequest request) =>
        request with
        {
            GeneratedSummary = request.GeneratedSummary.Trim(),
            ProposedTitle = request.ProposedTitle.Trim(),
            ProposedOwnerFunction = request.ProposedOwnerFunction.Trim(),
            RequiredAction = request.RequiredAction.Trim(),
            Confidence = request.Confidence.Trim(),
            SourceCitations = request.SourceCitations.Select(item => item.Trim()).Where(item => item.Length > 0).Distinct(StringComparer.Ordinal).ToArray(),
            RetrievedSourceReferences = request.RetrievedSourceReferences.Select(item => item.Trim()).Where(item => item.Length > 0).Distinct(StringComparer.Ordinal).ToArray(),
            EvidenceSuggestions = request.EvidenceSuggestions.Select(item => item.Trim()).Where(item => item.Length > 0).Distinct(StringComparer.Ordinal).ToArray()
        };

    private static SuggestedObligationReviewRequest Normalize(SuggestedObligationReviewRequest request) =>
        request with
        {
            Reason = request.Reason.Trim(),
            SourceCitations = request.SourceCitations.Select(item => item.Trim()).Where(item => item.Length > 0).Distinct(StringComparer.Ordinal).ToArray()
        };

    private static void Validate(CreateSuggestedObligationRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        AddIf(errors, string.IsNullOrWhiteSpace(request.Source), "source", "Source is required.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.SourceUrl), "sourceUrl", "Source URL is required.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.GeneratedSummary), "generatedSummary", "Generated summary is required.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.ProposedTitle), "proposedTitle", "Proposed title is required.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.ProposedOwnerFunction), "proposedOwnerFunction", "Proposed owner is required.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.RequiredAction), "requiredAction", "Required action is required.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.Confidence), "confidence", "Confidence is required.");
        AddIf(errors, request.SourceCitations.Count == 0, "sourceCitations", "At least one source citation is required.");
        AddIf(errors, request.RetrievedSourceReferences.Count == 0, "retrievedSourceReferences", "At least one retrieved source reference is required.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.PromptVersion), "promptVersion", "Prompt version is required.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.ModelIdentifier), "modelIdentifier", "Model identifier is required.");
        ThrowIfInvalid(errors);
    }

    private static void Validate(ReviseSuggestedObligationRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        AddIf(errors, string.IsNullOrWhiteSpace(request.GeneratedSummary), "generatedSummary", "Generated summary is required.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.ProposedTitle), "proposedTitle", "Proposed title is required.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.ProposedOwnerFunction), "proposedOwnerFunction", "Proposed owner is required.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.RequiredAction), "requiredAction", "Required action is required.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.Confidence), "confidence", "Confidence is required.");
        AddIf(errors, request.SourceCitations.Count == 0, "sourceCitations", "At least one source citation is required.");
        ThrowIfInvalid(errors);
    }

    private static void ValidateReview(SuggestedObligationReviewRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        AddIf(errors, string.IsNullOrWhiteSpace(request.Reason), "reason", "Review reason is required.");
        AddIf(errors, request.SourceCitations.Count == 0, "sourceCitations", "At least one source citation is required.");
        ThrowIfInvalid(errors);
    }

    private static void AddIf(Dictionary<string, string[]> errors, bool condition, string key, string message)
    {
        if (condition)
        {
            errors[key] = [message];
        }
    }

    private static void ThrowIfInvalid(Dictionary<string, string[]> errors)
    {
        if (errors.Count > 0)
        {
            throw new SuggestedObligationValidationException(errors);
        }
    }

    private Task WriteAuditAsync(
        SuggestedObligationDto suggestion,
        Guid actorUserId,
        AuditAction action,
        string summary,
        CancellationToken cancellationToken) =>
        auditEventWriter.WriteAsync(
            suggestion.TenantId,
            actorUserId,
            action,
            "SuggestedObligation",
            suggestion.Id.ToString(),
            summary,
            new Dictionary<string, string>
            {
                ["reviewStatus"] = suggestion.ReviewStatus,
                ["source"] = suggestion.Source,
                ["confidence"] = suggestion.Confidence,
                ["reviewedByUserId"] = suggestion.ReviewedByUserId?.ToString() ?? string.Empty,
                ["reviewedAt"] = suggestion.ReviewedAt?.ToString("O") ?? string.Empty,
                ["sourceCitations"] = string.Join("|", suggestion.SourceCitations)
            },
            cancellationToken);
}

public interface ISuggestedObligationRepository
{
    Task<SuggestedObligationDto> CreateAsync(CreateSuggestedObligationRequest request, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SuggestedObligationDto>> ListAsync(string? reviewStatus, CancellationToken cancellationToken = default);
    Task<SuggestedObligationDto?> FindAsync(Guid suggestionId, CancellationToken cancellationToken = default);
    Task<SuggestedObligationDto?> ApproveAsync(Guid suggestionId, SuggestedObligationReviewRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<SuggestedObligationDto?> ReviseAsync(Guid suggestionId, ReviseSuggestedObligationRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<SuggestedObligationDto?> RejectAsync(Guid suggestionId, SuggestedObligationReviewRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<SuggestedObligationDto?> EscalateAsync(Guid suggestionId, SuggestedObligationReviewRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
}

public sealed record SuggestedObligationDto(
    Guid Id,
    Guid TenantId,
    string Source,
    string SourceUrl,
    string GeneratedSummary,
    string ProposedTitle,
    string ProposedOwnerFunction,
    string RequiredAction,
    RiskLevel RiskLevel,
    IReadOnlyList<string> EvidenceSuggestions,
    IReadOnlyList<string> SourceCitations,
    string Confidence,
    string PromptVersion,
    string ModelIdentifier,
    IReadOnlyList<string> RetrievedSourceReferences,
    string ReviewStatus,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    Guid? ReviewedByUserId,
    DateTimeOffset? ReviewedAt,
    string? ReviewReason);

public sealed record CreateSuggestedObligationRequest(
    string Source,
    string SourceUrl,
    string GeneratedSummary,
    string ProposedTitle,
    string ProposedOwnerFunction,
    string RequiredAction,
    RiskLevel RiskLevel,
    IReadOnlyList<string> EvidenceSuggestions,
    IReadOnlyList<string> SourceCitations,
    string Confidence,
    string PromptVersion,
    string ModelIdentifier,
    IReadOnlyList<string> RetrievedSourceReferences);

public sealed record ReviseSuggestedObligationRequest(
    string GeneratedSummary,
    string ProposedTitle,
    string ProposedOwnerFunction,
    string RequiredAction,
    RiskLevel RiskLevel,
    IReadOnlyList<string> EvidenceSuggestions,
    IReadOnlyList<string> SourceCitations,
    string Confidence,
    IReadOnlyList<string> RetrievedSourceReferences);

public sealed record SuggestedObligationReviewRequest(
    string Reason,
    IReadOnlyList<string> SourceCitations);

public sealed class SuggestedObligationValidationException(IReadOnlyDictionary<string, string[]> errors)
    : InvalidOperationException("Suggested obligation input is invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
