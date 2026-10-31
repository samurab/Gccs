using System.Text.Json;
using Gccs.Application.Compliance;
using Gccs.Application.Security;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Compliance;

public sealed class EfSuggestedObligationRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : ISuggestedObligationRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<SuggestedObligationDto> CreateAsync(
        CreateSuggestedObligationRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = new SuggestedObligationEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Source = request.Source,
            SourceUrl = request.SourceUrl,
            GeneratedSummary = request.GeneratedSummary,
            ProposedTitle = request.ProposedTitle,
            ProposedOwnerFunction = request.ProposedOwnerFunction,
            RequiredAction = request.RequiredAction,
            RiskLevel = request.RiskLevel,
            EvidenceSuggestionsJson = Serialize(request.EvidenceSuggestions),
            SourceCitationsJson = Serialize(request.SourceCitations),
            Confidence = request.Confidence,
            PromptVersion = request.PromptVersion,
            ModelIdentifier = request.ModelIdentifier,
            RetrievedSourceReferencesJson = Serialize(request.RetrievedSourceReferences),
            ReviewStatus = "draft",
            CreatedByUserId = actorUserId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.SuggestedObligations.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<IReadOnlyList<SuggestedObligationDto>> ListAsync(
        string? reviewStatus,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.SuggestedObligations
            .AsNoTracking()
            .Where(suggestion => suggestion.TenantId == tenantContext.TenantId);

        if (!string.IsNullOrWhiteSpace(reviewStatus))
        {
            var normalized = reviewStatus.Trim();
            query = query.Where(suggestion => suggestion.ReviewStatus == normalized);
        }

        return await query
            .OrderByDescending(suggestion => suggestion.CreatedAt)
            .Select(suggestion => ToDto(suggestion))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<SuggestedObligationDto?> FindAsync(
        Guid suggestionId,
        CancellationToken cancellationToken = default)
    {
        var entity = await FindForCurrentTenantAsync(suggestionId, cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    public Task<SuggestedObligationDto?> ApproveAsync(
        Guid suggestionId,
        SuggestedObligationReviewRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        ChangeReviewStatusAsync(suggestionId, "approved", request, actorUserId, cancellationToken);

    public async Task<SuggestedObligationDto?> ReviseAsync(
        Guid suggestionId,
        ReviseSuggestedObligationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = await FindForCurrentTenantAsync(suggestionId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.GeneratedSummary = request.GeneratedSummary;
        entity.ProposedTitle = request.ProposedTitle;
        entity.ProposedOwnerFunction = request.ProposedOwnerFunction;
        entity.RequiredAction = request.RequiredAction;
        entity.RiskLevel = request.RiskLevel;
        entity.EvidenceSuggestionsJson = Serialize(request.EvidenceSuggestions);
        entity.SourceCitationsJson = Serialize(request.SourceCitations);
        entity.Confidence = request.Confidence;
        entity.RetrievedSourceReferencesJson = Serialize(request.RetrievedSourceReferences);
        entity.ReviewStatus = "draft";
        entity.ReviewedByUserId = null;
        entity.ReviewedAt = null;
        entity.ReviewReason = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public Task<SuggestedObligationDto?> RejectAsync(
        Guid suggestionId,
        SuggestedObligationReviewRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        ChangeReviewStatusAsync(suggestionId, "rejected", request, actorUserId, cancellationToken);

    public Task<SuggestedObligationDto?> EscalateAsync(
        Guid suggestionId,
        SuggestedObligationReviewRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        ChangeReviewStatusAsync(suggestionId, "escalated", request, actorUserId, cancellationToken);

    private async Task<SuggestedObligationDto?> ChangeReviewStatusAsync(
        Guid suggestionId,
        string status,
        SuggestedObligationReviewRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var entity = await FindForCurrentTenantAsync(suggestionId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (status == "approved" && await HasOpenExpertReviewAsync(entity.Id, cancellationToken))
        {
            entity.ReviewStatus = "escalated";
            await dbContext.SaveChangesAsync(cancellationToken);
            return ToDto(entity);
        }

        entity.ReviewStatus = status;
        entity.SourceCitationsJson = Serialize(request.SourceCitations);
        entity.ReviewedByUserId = actorUserId;
        entity.ReviewedAt = DateTimeOffset.UtcNow;
        entity.ReviewReason = request.Reason;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    private Task<SuggestedObligationEntity?> FindForCurrentTenantAsync(
        Guid suggestionId,
        CancellationToken cancellationToken) =>
        dbContext.SuggestedObligations.FirstOrDefaultAsync(
            suggestion => suggestion.Id == suggestionId && suggestion.TenantId == tenantContext.TenantId,
            cancellationToken);

    private Task<bool> HasOpenExpertReviewAsync(Guid suggestionId, CancellationToken cancellationToken) =>
        dbContext.ExpertReviewItems.AnyAsync(
            item =>
                item.TenantId == tenantContext.TenantId &&
                item.SourceType == "suggested_obligation" &&
                item.SourceId == suggestionId &&
                item.Status != "resolved",
            cancellationToken);

    private static SuggestedObligationDto ToDto(SuggestedObligationEntity entity) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.Source,
            entity.SourceUrl,
            entity.GeneratedSummary,
            entity.ProposedTitle,
            entity.ProposedOwnerFunction,
            entity.RequiredAction,
            entity.RiskLevel,
            Deserialize(entity.EvidenceSuggestionsJson),
            Deserialize(entity.SourceCitationsJson),
            entity.Confidence,
            entity.PromptVersion,
            entity.ModelIdentifier,
            Deserialize(entity.RetrievedSourceReferencesJson),
            entity.ReviewStatus,
            entity.CreatedByUserId,
            entity.CreatedAt,
            entity.ReviewedByUserId,
            entity.ReviewedAt,
            entity.ReviewReason);

    private static string Serialize(IReadOnlyList<string> values) =>
        JsonSerializer.Serialize(values, JsonOptions);

    private static IReadOnlyList<string> Deserialize(string json) =>
        JsonSerializer.Deserialize<string[]>(json, JsonOptions) ?? [];
}
