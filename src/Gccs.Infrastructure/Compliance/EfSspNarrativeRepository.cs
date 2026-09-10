using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gccs.Application.Common;
using Gccs.Application.Compliance;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Domain.Evidence;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Compliance;

public sealed class EfSspNarrativeRepository(GccsDbContext dbContext) : ISspNarrativeRepository
{
    public async Task<IReadOnlyList<SspNarrativeDto>> ListNarrativesAsync(Guid tenantId, Guid sectionId, CancellationToken cancellationToken = default) =>
        (await Query(tenantId, sectionId).OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt).ToArrayAsync(cancellationToken))
            .Select(ToDto).ToArray();

    public async Task<SspNarrativeDto?> GetNarrativeAsync(Guid tenantId, Guid sectionId, Guid narrativeId, CancellationToken cancellationToken = default)
    {
        var entity = await Query(tenantId, sectionId).SingleOrDefaultAsync(x => x.Id == narrativeId, cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<SspNarrativeDto?> GetCurrentApprovedNarrativeAsync(Guid tenantId, Guid sectionId, CancellationToken cancellationToken = default)
    {
        var entity = await Query(tenantId, sectionId)
            .Where(x => x.Status == SspNarrativeStatus.Approved)
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<IReadOnlyDictionary<Guid, SspNarrativeDto>> ListCurrentApprovedNarrativesAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> sectionIds,
        CancellationToken cancellationToken = default)
    {
        if (sectionIds.Count == 0) return new Dictionary<Guid, SspNarrativeDto>();
        var entities = await dbContext.SspNarratives.AsNoTracking()
            .Where(item => item.TenantId == tenantId && sectionIds.Contains(item.SectionId) && item.Status == SspNarrativeStatus.Approved)
            .Include(item => item.Sources)
            .ToArrayAsync(cancellationToken);
        return entities.ToDictionary(item => item.SectionId, ToDto);
    }

    public async Task<SspNarrativeDto> CreateDraftAsync(
        Guid tenantId,
        Guid sectionId,
        string generatedText,
        bool aiAssisted,
        string? reviewerNotes,
        ContentClassificationDto classification,
        IReadOnlyList<ResolvedSspNarrativeSource> sources,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();
        var entity = new SspNarrativeEntity
        {
            Id = id,
            TenantId = tenantId,
            SectionId = sectionId,
            GeneratedText = generatedText,
            ReviewerNotes = reviewerNotes,
            Status = SspNarrativeStatus.Draft,
            AiAssisted = aiAssisted,
            DraftOnly = true,
            Version = 1,
            Classification = classification.Classification,
            ClassificationSource = classification.Source,
            ClassificationConfidence = classification.Confidence,
            ClassificationReviewedByUserId = classification.ReviewedByUserId,
            ClassificationReviewedAt = classification.ReviewedAt,
            ClassificationReason = classification.Reason,
            ClassificationIsApprovedDemoContent = classification.IsApprovedDemoContent,
            CreatedAt = now,
            CreatedByUserId = actorUserId,
            Sources = sources.Select(source => new SspNarrativeSourceEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                NarrativeId = id,
                SourceType = source.SourceType,
                RecordId = source.RecordId,
                Label = source.Label,
                Summary = source.Summary,
                SourceUrl = source.SourceUrl,
                Fingerprint = source.Fingerprint,
                Classification = source.Classification
            }).ToList()
        };
        dbContext.SspNarratives.Add(entity);
        await SaveAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<SspNarrativeDto?> UpdateDraftAsync(
        Guid tenantId,
        Guid sectionId,
        Guid narrativeId,
        EditSspNarrativeDraftRequest request,
        ContentClassificationDto classification,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = await Tracked(tenantId, sectionId, narrativeId, cancellationToken);
        if (entity is null) return null;
        EnsureDraftAndVersion(entity, request.ExpectedVersion);
        entity.EditedText = request.EditedText.Trim();
        entity.ReviewerNotes = request.ReviewerNotes?.Trim();
        entity.Classification = classification.Classification;
        entity.ClassificationSource = classification.Source;
        entity.ClassificationConfidence = classification.Confidence;
        entity.ClassificationReviewedByUserId = classification.ReviewedByUserId;
        entity.ClassificationReviewedAt = classification.ReviewedAt;
        entity.ClassificationReason = classification.Reason;
        entity.ClassificationIsApprovedDemoContent = classification.IsApprovedDemoContent;
        Touch(entity, actorUserId);
        await SaveAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<SspNarrativeDto?> ApproveAsync(
        Guid tenantId,
        Guid sectionId,
        Guid narrativeId,
        ApproveSspNarrativeRequest request,
        Guid reviewerUserId,
        string reviewerName,
        CancellationToken cancellationToken = default)
    {
        var entity = await Tracked(tenantId, sectionId, narrativeId, cancellationToken);
        if (entity is null) return null;
        EnsureDraftAndVersion(entity, request.ExpectedVersion);
        var ownsTransaction = dbContext.Database.IsRelational() && dbContext.Database.CurrentTransaction is null;
        await using var localTransaction = ownsTransaction
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        try
        {
            var now = DateTimeOffset.UtcNow;
            var existing = await dbContext.SspNarratives
                .Where(x => x.TenantId == tenantId && x.SectionId == sectionId && x.Id != narrativeId && x.Status == SspNarrativeStatus.Approved)
                .ToArrayAsync(cancellationToken);
            foreach (var approved in existing)
            {
                approved.Status = SspNarrativeStatus.Superseded;
                approved.DraftOnly = false;
                Touch(approved, reviewerUserId, now);
            }

            // PostgreSQL checks the partial unique index while statements execute. Persist
            // supersession before promoting the replacement, inside the same transaction.
            if (existing.Length > 0) await SaveAsync(cancellationToken);

            entity.ApprovedText = entity.EditedText ?? entity.GeneratedText;
            entity.Status = SspNarrativeStatus.Approved;
            entity.DraftOnly = false;
            entity.ReviewerUserId = reviewerUserId;
            entity.Reviewer = reviewerName;
            entity.ReviewDate = request.ReviewDate;
            Touch(entity, reviewerUserId, now);
            await SaveAsync(cancellationToken);
            if (localTransaction is not null) await localTransaction.CommitAsync(cancellationToken);
            return ToDto(entity);
        }
        catch
        {
            if (localTransaction is not null) await localTransaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private IQueryable<SspNarrativeEntity> Query(Guid tenantId, Guid sectionId) => dbContext.SspNarratives.AsNoTracking()
        .Where(x => x.TenantId == tenantId && x.SectionId == sectionId)
        .Include(x => x.Sources).AsSplitQuery();

    private Task<SspNarrativeEntity?> Tracked(Guid tenantId, Guid sectionId, Guid id, CancellationToken cancellationToken) =>
        dbContext.SspNarratives
            .Where(x => x.TenantId == tenantId && x.SectionId == sectionId && x.Id == id)
            .Include(x => x.Sources).AsSplitQuery()
            .SingleOrDefaultAsync(cancellationToken);

    private static void EnsureDraftAndVersion(SspNarrativeEntity entity, long expectedVersion)
    {
        if (entity.Status != SspNarrativeStatus.Draft)
            throw new SspNarrativeValidationException("Only draft SSP narratives can be changed.");
        if (entity.Version != expectedVersion) throw new ContentRevisionConflictException();
    }

    private static void Touch(SspNarrativeEntity entity, Guid actorUserId, DateTimeOffset? now = null)
    {
        entity.Version++;
        entity.UpdatedAt = now ?? DateTimeOffset.UtcNow;
        entity.UpdatedByUserId = actorUserId;
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ContentRevisionConflictException();
        }
        catch (DbUpdateException exception) when (exception.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true)
        {
            throw new ContentRevisionConflictException();
        }
    }

    private static SspNarrativeDto ToDto(SspNarrativeEntity entity) => new(
        entity.Id,
        entity.TenantId,
        entity.SectionId,
        entity.GeneratedText,
        entity.EditedText,
        entity.ApprovedText,
        entity.Status,
        entity.AiAssisted,
        entity.DraftOnly,
        entity.ReviewerNotes,
        entity.ReviewerUserId,
        entity.Reviewer,
        entity.ReviewDate,
        entity.Version,
        new ContentClassificationDto(
            entity.Classification,
            entity.ClassificationSource,
            entity.ClassificationConfidence,
            entity.ClassificationReviewedByUserId,
            entity.ClassificationReviewedAt,
            entity.ClassificationReason,
            entity.ClassificationIsApprovedDemoContent),
        entity.Sources.OrderBy(source => source.SourceType).ThenBy(source => source.RecordId)
            .Select(source => new SspNarrativeSourceRecordDto(
                source.SourceType,
                source.RecordId,
                source.Label,
                source.Summary,
                source.SourceUrl,
                source.Fingerprint,
                source.Classification))
            .ToArray(),
        entity.CreatedAt,
        entity.UpdatedAt ?? entity.CreatedAt);
}

public sealed class EfSspNarrativeSourceResolver(GccsDbContext dbContext) : ISspNarrativeSourceResolver
{
    public async Task<IReadOnlyList<ResolvedSspNarrativeSource>> ResolveAsync(
        Guid tenantId,
        IReadOnlyList<SspNarrativeSourceLinkRequest> sources,
        CancellationToken cancellationToken = default)
    {
        var resolved = new List<ResolvedSspNarrativeSource>(sources.Count);
        foreach (var source in sources.OrderBy(item => item.SourceType).ThenBy(item => item.RecordId, StringComparer.Ordinal))
            await LockAuthoritativeRowIfApprovingAsync(tenantId, source, cancellationToken);
        foreach (var source in sources)
        {
            var record = source.SourceType switch
            {
                SspNarrativeSourceType.Evidence => await ResolveEvidenceAsync(tenantId, source.RecordId.Trim(), cancellationToken),
                SspNarrativeSourceType.GeneratedPolicy => await ResolvePolicyAsync(tenantId, source.RecordId.Trim(), cancellationToken),
                SspNarrativeSourceType.Clause => await ResolveClauseAsync(tenantId, source.RecordId.Trim(), cancellationToken),
                SspNarrativeSourceType.Obligation => await ResolveObligationAsync(source.RecordId.Trim(), cancellationToken),
                _ => null
            };
            if (record is null)
                throw new SspNarrativeValidationException($"The requested {source.SourceType} source was not found in the current tenant scope or is not approved and current.");
            resolved.Add(record);
        }
        return resolved;
    }

    private async Task LockAuthoritativeRowIfApprovingAsync(
        Guid tenantId,
        SspNarrativeSourceLinkRequest source,
        CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational() || dbContext.Database.CurrentTransaction is null) return;

        switch (source.SourceType)
        {
            case SspNarrativeSourceType.Evidence when Guid.TryParse(source.RecordId, out var evidenceId):
                await dbContext.EvidenceItems
                    .FromSqlInterpolated($"SELECT * FROM gccs.evidence_items WHERE tenant_id = {tenantId} AND id = {evidenceId} FOR SHARE")
                    .AsNoTracking().LoadAsync(cancellationToken);
                break;
            case SspNarrativeSourceType.GeneratedPolicy when Guid.TryParse(source.RecordId, out var policyId):
                await dbContext.GeneratedPolicies
                    .FromSqlInterpolated($"SELECT * FROM gccs.generated_policies WHERE tenant_id = {tenantId} AND id = {policyId} FOR SHARE")
                    .AsNoTracking().LoadAsync(cancellationToken);
                break;
            case SspNarrativeSourceType.Clause:
                await dbContext.Clauses
                    .FromSqlInterpolated($"SELECT * FROM gccs.clauses WHERE id = {source.RecordId} AND (tenant_id IS NULL OR tenant_id = {tenantId}) FOR SHARE")
                    .AsNoTracking().LoadAsync(cancellationToken);
                break;
            case SspNarrativeSourceType.Obligation:
                await dbContext.Obligations
                    .FromSqlInterpolated($"SELECT * FROM gccs.obligations WHERE id = {source.RecordId} FOR SHARE")
                    .AsNoTracking().LoadAsync(cancellationToken);
                break;
        }
    }

    private async Task<ResolvedSspNarrativeSource?> ResolveEvidenceAsync(Guid tenantId, string recordId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(recordId, out var id)) return null;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var entity = await dbContext.EvidenceItems.AsNoTracking().SingleOrDefaultAsync(x =>
            x.TenantId == tenantId && x.Id == id && x.Status == EvidenceStatus.Approved &&
            x.ApprovedAt != null && x.ApprovedByUserId != null && !x.IsUseBlocked &&
            (x.ExpiresAt == null || x.ExpiresAt >= today), cancellationToken);
        if (entity is null || entity.Classification is ContentClassification.Unknown or ContentClassification.Prohibited) return null;
        if (entity.Classification == ContentClassification.SyntheticCui && !entity.ClassificationIsApprovedDemoContent) return null;
        var summary = string.IsNullOrWhiteSpace(entity.Description) ? entity.Name : entity.Description;
        return Resolved(SspNarrativeSourceType.Evidence, recordId, entity.Name, summary,
            $"/evidence?item={entity.Id}", entity.Classification,
            entity.Id, entity.Status, entity.ApprovedAt, entity.UpdatedAt, entity.ExpiresAt, entity.Classification, entity.ClassificationRevision);
    }

    private async Task<ResolvedSspNarrativeSource?> ResolvePolicyAsync(Guid tenantId, string recordId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(recordId, out var id)) return null;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var entity = await dbContext.GeneratedPolicies.AsNoTracking().SingleOrDefaultAsync(x =>
            x.TenantId == tenantId && x.Id == id && x.Status == "Approved" &&
            x.ApprovedAt != null && x.ApprovedByUserId != null &&
            (x.ReviewDueAt == null || x.ReviewDueAt >= today), cancellationToken);
        if (entity is null || HasMissingPlaceholders(entity.MissingPlaceholdersJson) ||
            entity.Classification is ContentClassification.Unknown or ContentClassification.Prohibited) return null;
        if (entity.Classification == ContentClassification.SyntheticCui && !entity.ClassificationIsApprovedDemoContent) return null;
        return Resolved(SspNarrativeSourceType.GeneratedPolicy, recordId, entity.Title,
            $"Approved policy {entity.Title}, version {entity.SourceTemplateVersion}.",
            $"/policies?policy={entity.Id}", entity.Classification,
            entity.Id, entity.Status, entity.SourceTemplateVersion, entity.ApprovedAt, entity.UpdatedAt, entity.ReviewDueAt,
            entity.Classification, entity.ClassificationRevision);
    }

    private async Task<ResolvedSspNarrativeSource?> ResolveClauseAsync(Guid tenantId, string recordId, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var entity = await dbContext.Clauses.AsNoTracking().SingleOrDefaultAsync(x =>
            x.Id == recordId && (x.TenantId == null || x.TenantId == tenantId) &&
            x.ReviewState == ReviewState.Published && x.SupersededByClauseId == null &&
            (x.NextReviewDueAt == null || x.NextReviewDueAt >= today), cancellationToken);
        if (entity is null) return null;
        return Resolved(SspNarrativeSourceType.Clause, recordId, $"{entity.Number} {entity.Title}",
            entity.PlainEnglishSummary, entity.SourceUrl, ContentClassification.Unclassified,
            entity.Id, entity.ClauseTextVersion, entity.SourceHash, entity.LastReviewedAt, entity.NextReviewDueAt, entity.ReviewState);
    }

    private async Task<ResolvedSspNarrativeSource?> ResolveObligationAsync(string recordId, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var entity = await dbContext.Obligations.AsNoTracking().SingleOrDefaultAsync(x =>
            x.Id == recordId && x.ReviewState == ReviewState.Published &&
            (x.NextReviewDueAt == null || x.NextReviewDueAt >= today), cancellationToken);
        if (entity is null) return null;
        return Resolved(SspNarrativeSourceType.Obligation, recordId, entity.Title,
            entity.PlainEnglishSummary, entity.SourceUrl, ContentClassification.Unclassified,
            entity.Id, entity.LastReviewedAt, entity.NextReviewDueAt, entity.ReviewState, entity.RequiredAction);
    }

    private static bool HasMissingPlaceholders(string json)
    {
        try { return JsonSerializer.Deserialize<string[]>(json)?.Length > 0; }
        catch (JsonException) { return true; }
    }

    private static ResolvedSspNarrativeSource Resolved(
        SspNarrativeSourceType type,
        string recordId,
        string label,
        string summary,
        string sourceUrl,
        ContentClassification classification,
        params object?[] fingerprintValues) => new(
            type,
            recordId,
            Trim(label, 300),
            Trim(summary, 1_000),
            Trim(sourceUrl, 1_000),
            Fingerprint(fingerprintValues),
            classification);

    private static string Trim(string? value, int maximum) =>
        string.IsNullOrWhiteSpace(value) ? "Approved source record" : value.Trim()[..Math.Min(value.Trim().Length, maximum)];

    private static string Fingerprint(IEnumerable<object?> values)
    {
        var value = string.Join('|', values.Select(item => item?.ToString() ?? string.Empty));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}

public sealed class UnavailableSspNarrativeSourceResolver : ISspNarrativeSourceResolver
{
    public Task<IReadOnlyList<ResolvedSspNarrativeSource>> ResolveAsync(Guid tenantId, IReadOnlyList<SspNarrativeSourceLinkRequest> sources, CancellationToken cancellationToken = default) =>
        throw new SspNarrativeValidationException("SSP narrative source resolution requires configured relational persistence.");
}

public sealed class UnavailableSspNarrativeAiGenerator : ISspNarrativeAiGenerator
{
    public Task<string> GenerateAsync(IReadOnlyList<ResolvedSspNarrativeSource> sources, CancellationToken cancellationToken = default) =>
        throw new SspNarrativeValidationException("AI-assisted SSP narrative generation is not enabled for this deployment.");
}
