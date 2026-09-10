using System.Text.RegularExpressions;
using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Security;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;

namespace Gccs.Application.Compliance;

public sealed partial class SspNarrativeService(
    ISspSectionRepository sectionRepository,
    ISspNarrativeRepository narrativeRepository,
    ISspNarrativeSourceResolver sourceResolver,
    ISspNarrativeAiGenerator aiGenerator,
    ICurrentTenantContext tenantContext,
    ContentClassificationPolicy classificationPolicy,
    IAuditEventWriter auditEventWriter,
    IApplicationTransaction transaction)
{
    private const int MaximumSources = 20;
    private const int MaximumNarrativeLength = 20_000;
    private const int MaximumReviewerNotesLength = 4_000;

    public async Task<IReadOnlyList<SspNarrativeDto>?> ListAsync(Guid sectionId, CancellationToken cancellationToken = default)
    {
        if (await sectionRepository.GetAsync(tenantContext.TenantId, sectionId, cancellationToken) is null) return null;
        return await narrativeRepository.ListNarrativesAsync(tenantContext.TenantId, sectionId, cancellationToken);
    }

    public Task<SspNarrativeDto?> GetAsync(Guid sectionId, Guid narrativeId, CancellationToken cancellationToken = default) =>
        narrativeRepository.GetNarrativeAsync(tenantContext.TenantId, sectionId, narrativeId, cancellationToken);

    public async Task<SspNarrativeDto?> GenerateAsync(
        Guid sectionId,
        GenerateSspNarrativeDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        if (await sectionRepository.GetAsync(tenantContext.TenantId, sectionId, cancellationToken) is null) return null;
        var sourceLinks = request?.Sources;
        ValidateSourceLinks(sourceLinks);

        var sources = await sourceResolver.ResolveAsync(tenantContext.TenantId, sourceLinks!, cancellationToken);
        var classification = DerivedClassification(sources);
        await EnsureDerivedClassificationAllowedAsync(classification, cancellationToken);
        var generationMode = request!.GenerationMode;
        if (!Enum.IsDefined(generationMode))
            throw new SspNarrativeValidationException("A supported narrative generation mode is required.");
        var aiAssisted = generationMode == SspNarrativeGenerationMode.AiAssisted;
        var generatedText = aiAssisted
            ? await aiGenerator.GenerateAsync(sources, cancellationToken)
            : BuildDraft(sources);
        if (string.IsNullOrWhiteSpace(generatedText) || generatedText.Trim().Length > MaximumNarrativeLength)
            throw new SspNarrativeValidationException("Generated narrative text is empty or exceeds the narrative limit.");

        return await transaction.ExecuteAsync(async token =>
        {
            var narrative = await narrativeRepository.CreateDraftAsync(
                tenantContext.TenantId,
                sectionId,
                generatedText.Trim(),
                aiAssisted,
                null,
                classification,
                sources,
                tenantContext.UserId,
                token);
            await WriteAuditAsync(narrative, AuditAction.Created, "SSP narrative draft was generated.", token);
            return narrative;
        }, cancellationToken);
    }

    public async Task<SspNarrativeDto?> EditAsync(
        Guid sectionId,
        Guid narrativeId,
        EditSspNarrativeDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateEditableText(request);
        var current = await narrativeRepository.GetNarrativeAsync(tenantContext.TenantId, sectionId, narrativeId, cancellationToken);
        if (current is null) return null;
        if (current.Status != SspNarrativeStatus.Draft)
            throw new SspNarrativeValidationException("Only draft SSP narratives can be edited.");
        if (request.ExpectedVersion != current.Version) throw new ContentRevisionConflictException();

        var selected = request.Classification!;
        ContentClassificationPolicy.ValidateUserSelection(selected);
        ContentClassificationPolicy.EnsureProcessable(selected.Classification, TenantDataHandlingWorkflow.SspNarrative.ToString());
        EnsureNotDowngraded(current.Classification.Classification, selected.Classification);
        await classificationPolicy.EnsureAllowedAsync(
            selected,
            TenantDataHandlingWorkflow.SspNarrative,
            tenantContext.UserId,
            "SspNarrative",
            narrativeId.ToString(),
            cancellationToken);
        var classification = new ContentClassificationDto(
            selected.Classification,
            selected.Source,
            selected.Confidence,
            selected.ReviewedByUserId,
            selected.ReviewedAt,
            selected.Reason,
            selected.IsApprovedDemoContent);

        return await transaction.ExecuteAsync(async token =>
        {
            var narrative = await narrativeRepository.UpdateDraftAsync(
                tenantContext.TenantId,
                sectionId,
                narrativeId,
                request,
                classification,
                tenantContext.UserId,
                token);
            if (narrative is not null)
                await WriteAuditAsync(narrative, AuditAction.Updated, "SSP narrative draft was edited.", token);
            return narrative;
        }, cancellationToken);
    }

    public async Task<SspNarrativeDto?> ApproveAsync(
        Guid sectionId,
        Guid narrativeId,
        ApproveSspNarrativeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null || !request.ReviewDate.HasValue)
            throw new SspNarrativeValidationException("Narrative approval requires a review date.");
        if (request.ReviewDate > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new SspNarrativeValidationException("Narrative review date cannot be in the future.");
        if (string.IsNullOrWhiteSpace(tenantContext.UserEmail))
            throw new SspNarrativeValidationException("Authenticated reviewer identity is required.");

        return await transaction.ExecuteAsync(async token =>
        {
            var narrative = await narrativeRepository.GetNarrativeAsync(tenantContext.TenantId, sectionId, narrativeId, token);
            if (narrative is null) return null;
            if (narrative.Status != SspNarrativeStatus.Draft)
                throw new SspNarrativeValidationException("Only draft SSP narratives can be approved.");
            if (request.ExpectedVersion != narrative.Version) throw new ContentRevisionConflictException();
            ValidateApprovalText(narrative.EditedText ?? narrative.GeneratedText);
            if (narrative.SourceRecords.Length == 0)
                throw new SspNarrativeValidationException("Narrative approval requires source links.");

            var sourceLinks = narrative.SourceRecords
                .Select(source => new SspNarrativeSourceLinkRequest(source.SourceType, source.RecordId))
                .ToArray();
            var currentSources = await sourceResolver.ResolveAsync(tenantContext.TenantId, sourceLinks, token);
            if (currentSources.Count != narrative.SourceRecords.Length ||
                currentSources.Any(current => narrative.SourceRecords.All(saved =>
                    saved.SourceType != current.SourceType ||
                    !string.Equals(saved.RecordId, current.RecordId, StringComparison.Ordinal) ||
                    !string.Equals(saved.Fingerprint, current.Fingerprint, StringComparison.Ordinal))))
            {
                throw new SspNarrativeValidationException("Narrative approval is blocked because one or more source records changed after draft generation.");
            }
            await EnsureDerivedClassificationAllowedAsync(DerivedClassification(currentSources), token);
            await classificationPolicy.EnsureUsableAsync(
                narrative.Classification,
                TenantDataHandlingWorkflow.SspNarrative,
                tenantContext.UserId,
                "SspNarrative",
                narrative.Id.ToString(),
                token);

            var approved = await narrativeRepository.ApproveAsync(
                tenantContext.TenantId,
                sectionId,
                narrativeId,
                request,
                tenantContext.UserId,
                tenantContext.UserEmail.Trim(),
                token);
            if (approved is not null)
                await WriteAuditAsync(approved, AuditAction.Approved, "SSP narrative was approved.", token);
            return approved;
        }, cancellationToken);
    }

    public async Task<SspNarrativeComparisonDto?> CompareAsync(Guid sectionId, Guid narrativeId, CancellationToken cancellationToken = default)
    {
        var draft = await narrativeRepository.GetNarrativeAsync(tenantContext.TenantId, sectionId, narrativeId, cancellationToken);
        if (draft is null) return null;
        var approved = await narrativeRepository.GetCurrentApprovedNarrativeAsync(tenantContext.TenantId, sectionId, cancellationToken);
        if (approved?.Id == draft.Id) approved = null;
        return new SspNarrativeComparisonDto(
            sectionId,
            approved?.Id,
            draft.Id,
            approved?.ApprovedText,
            draft.EditedText ?? draft.GeneratedText,
            approved?.ReviewerUserId,
            approved?.Reviewer,
            approved?.ReviewDate,
            draft.ReviewerNotes,
            draft.SourceRecords,
            approved?.SourceRecords ?? []);
    }

    private async Task EnsureDerivedClassificationAllowedAsync(ContentClassificationDto classification, CancellationToken cancellationToken)
    {
        ContentClassificationPolicy.EnsureProcessable(classification.Classification, TenantDataHandlingWorkflow.SspNarrative.ToString());
        await classificationPolicy.EnsureAllowedAsync(
            new ContentClassificationRequest(
                classification.Classification,
                classification.Source,
                classification.Confidence,
                classification.ReviewedByUserId,
                classification.ReviewedAt,
                classification.Reason,
                classification.IsApprovedDemoContent),
            TenantDataHandlingWorkflow.SspNarrative,
            tenantContext.UserId,
            cancellationToken: cancellationToken);
    }

    private static void ValidateSourceLinks(SspNarrativeSourceLinkRequest[]? sources)
    {
        if (sources is null || sources.Length == 0)
            throw new SspNarrativeValidationException("At least one source record is required.");
        if (sources.Length > MaximumSources)
            throw new SspNarrativeValidationException($"A narrative can use at most {MaximumSources} source records.");
        foreach (var source in sources)
        {
            if (source is null || !Enum.IsDefined(source.SourceType) || string.IsNullOrWhiteSpace(source.RecordId) || source.RecordId.Trim().Length > 120)
                throw new SspNarrativeValidationException("Every narrative source requires a supported type and record ID of 120 characters or fewer.");
        }
        if (sources.GroupBy(source => new { source.SourceType, Id = source.RecordId.Trim() }).Any(group => group.Count() > 1))
            throw new SspNarrativeValidationException("Duplicate narrative sources are not allowed.");
    }

    private static void ValidateEditableText(EditSspNarrativeDraftRequest? request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.EditedText) || request.EditedText.Trim().Length > MaximumNarrativeLength)
            throw new SspNarrativeValidationException($"Narrative text is required and must be {MaximumNarrativeLength} characters or fewer.");
        if (request.ReviewerNotes?.Trim().Length > MaximumReviewerNotesLength)
            throw new SspNarrativeValidationException($"Reviewer notes must be {MaximumReviewerNotesLength} characters or fewer.");
        if (request.Classification is null)
            throw new SspNarrativeValidationException("Explicit narrative classification is required for edited text.");
        if (request.ExpectedVersion < 1)
            throw new SspNarrativeValidationException("A valid current narrative version is required.");
    }

    private static void ValidateApprovalText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new SspNarrativeValidationException("Narrative approval requires narrative text.");
        if (PlaceholderPattern().IsMatch(text))
            throw new SspNarrativeValidationException("Narrative approval is blocked while unresolved placeholders remain.");
    }

    private static void EnsureNotDowngraded(ContentClassification source, ContentClassification selected)
    {
        static int Rank(ContentClassification value) => value switch
        {
            ContentClassification.Unclassified => 0,
            ContentClassification.Fci => 1,
            ContentClassification.Cui or ContentClassification.SyntheticCui => 2,
            ContentClassification.Unknown or ContentClassification.Prohibited => 3,
            _ => 3
        };
        if (Rank(selected) < Rank(source) ||
            (source == ContentClassification.SyntheticCui && selected != ContentClassification.SyntheticCui))
            throw new SspNarrativeValidationException("Edited narrative classification cannot be less restrictive than its source records.");
    }

    private static ContentClassificationDto DerivedClassification(IReadOnlyList<ResolvedSspNarrativeSource> sources)
    {
        var classification = sources.Select(source => source.Classification).OrderByDescending(value => value switch
        {
            ContentClassification.Prohibited => 6,
            ContentClassification.Unknown => 5,
            ContentClassification.Cui => 4,
            ContentClassification.SyntheticCui => 3,
            ContentClassification.Fci => 2,
            _ => 1
        }).First();
        return new ContentClassificationDto(
            classification,
            classification == ContentClassification.SyntheticCui ? ContentClassificationSource.ImportedDemoSeed : ContentClassificationSource.SystemSuggested,
            null,
            null,
            null,
            "Derived from approved SSP narrative sources.",
            classification == ContentClassification.SyntheticCui);
    }

    private static string BuildDraft(IReadOnlyList<ResolvedSspNarrativeSource> sources)
    {
        var text = string.Join(Environment.NewLine + Environment.NewLine,
            sources.Select(source => $"{source.Label}: {source.Summary}"));
        if (text.Length > MaximumNarrativeLength)
            throw new SspNarrativeValidationException($"Resolved source summaries exceed the {MaximumNarrativeLength}-character narrative limit.");
        return text;
    }

    private Task WriteAuditAsync(SspNarrativeDto narrative, AuditAction action, string summary, CancellationToken cancellationToken) =>
        auditEventWriter.WriteAsync(
            tenantContext.TenantId,
            tenantContext.UserId,
            action,
            "SspNarrative",
            narrative.Id.ToString(),
            summary,
            new Dictionary<string, string>
            {
                ["sectionId"] = narrative.SectionId.ToString(),
                ["status"] = narrative.Status.ToString(),
                ["draftOnly"] = narrative.DraftOnly.ToString(),
                ["aiAssisted"] = narrative.AiAssisted.ToString(),
                ["version"] = narrative.Version.ToString(),
                ["classification"] = narrative.Classification.Classification.ToString(),
                ["sourceCount"] = narrative.SourceRecords.Length.ToString()
            },
            cancellationToken);

    [GeneratedRegex(@"\{\{[^{}]+\}\}|\[\[[^\[\]]+\]\]|\[(?:PLACEHOLDER|INSERT[^\]]*)\]|\b(?:TBD|TODO)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderPattern();
}
