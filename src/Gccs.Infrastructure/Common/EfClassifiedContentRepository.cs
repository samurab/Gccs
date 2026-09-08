using System.Text.Json;
using Gccs.Application.Common;
using Gccs.Application.Security;
using Gccs.Domain.Common;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gccs.Infrastructure.Common;

public sealed class EfClassifiedContentRepository(GccsDbContext db, ICurrentTenantContext context) : IClassifiedContentRepository
{
    private IQueryable<EvidenceItemEntity> Evidence => db.EvidenceItems.Where(e => e.TenantId == context.TenantId);
    private IQueryable<EvidenceFileVersionEntity> Versions => db.EvidenceFileVersions.Where(e => e.EvidenceItem!.TenantId == context.TenantId);
    private IQueryable<ContractDocumentEntity> Documents => db.Set<ContractDocumentEntity>().Where(e => e.Contract!.TenantId == context.TenantId);
    private IQueryable<ExtractionJobEntity> Jobs => db.Set<ExtractionJobEntity>().Where(e => e.TenantId == context.TenantId);
    private IQueryable<ClassifiedNoteEntity> Notes => db.Set<ClassifiedNoteEntity>().Where(e => e.TenantId == context.TenantId);
    private IQueryable<ReportEntity> Reports => db.Reports.Where(e => e.TenantId == context.TenantId).Include(e => e.CurrentClassification);

    public async Task<IReadOnlyList<ClassifiedContentDto>> ListAsync(string type, bool reviewOnly, int offset, CancellationToken ct)
    {
        // Every branch scopes in SQL, orders before paging, and never returns file/note/report bodies.
        return type switch
        {
            "EvidenceItem" => (await Review(Evidence.AsNoTracking(), reviewOnly).OrderBy(e => e.Id).Skip(offset).Take(100).ToArrayAsync(ct)).Select(e => Dto(type, e)).ToArray(),
            "EvidenceFileVersion" => (await Review(Versions.AsNoTracking(), reviewOnly).OrderBy(e => e.Id).Skip(offset).Take(100).ToArrayAsync(ct)).Select(e => Dto(type, e)).ToArray(),
            "ContractDocument" => (await Review(Documents.AsNoTracking(), reviewOnly).OrderBy(e => e.Id).Skip(offset).Take(100).ToArrayAsync(ct)).Select(e => Dto(type, e)).ToArray(),
            "ExtractionJob" => (await Review(Jobs.AsNoTracking(), reviewOnly).OrderBy(e => e.Id).Skip(offset).Take(100).ToArrayAsync(ct)).Select(e => Dto(type, e)).ToArray(),
            "ClassifiedNote" => await Review(Notes.AsNoTracking(), reviewOnly).OrderBy(e => e.Id).Skip(offset).Take(100)
                .Select(e => new ClassifiedContentDto(type, e.Id, e.Title,
                    new(e.Classification, e.ClassificationSource, e.ClassificationConfidence, e.ClassificationReviewedByUserId,
                        e.ClassificationReviewedAt, e.ClassificationReason, e.ClassificationIsApprovedDemoContent), e.ClassificationRevision, e.CreatedAt)).ToArrayAsync(ct),
            "Report" => await Reports.AsNoTracking().Where(e => !reviewOnly ||
                (e.CurrentClassification == null ? e.Classification : e.CurrentClassification.Classification) == ContentClassification.Unknown ||
                (e.CurrentClassification == null ? e.Classification : e.CurrentClassification.Classification) == ContentClassification.Prohibited ||
                (e.CurrentClassification == null ? e.Classification : e.CurrentClassification.Classification) == ContentClassification.Cui)
                .OrderBy(e => e.Id).Skip(offset).Take(100)
                .Select(e => new ClassifiedContentDto(type, e.Id, e.Title, new(
                    e.CurrentClassification == null ? e.Classification : e.CurrentClassification.Classification,
                    e.CurrentClassification == null ? e.ClassificationSource : e.CurrentClassification.ClassificationSource,
                    e.CurrentClassification == null ? e.ClassificationConfidence : e.CurrentClassification.ClassificationConfidence,
                    e.CurrentClassification == null ? e.ClassificationReviewedByUserId : e.CurrentClassification.ClassificationReviewedByUserId,
                    e.CurrentClassification == null ? e.ClassificationReviewedAt : e.CurrentClassification.ClassificationReviewedAt,
                    e.CurrentClassification == null ? e.ClassificationReason : e.CurrentClassification.ClassificationReason,
                    e.CurrentClassification == null ? e.ClassificationIsApprovedDemoContent : e.CurrentClassification.ClassificationIsApprovedDemoContent),
                    e.CurrentClassification == null ? e.ClassificationRevision : e.CurrentClassification.ClassificationRevision, e.GeneratedAt)).ToArrayAsync(ct),
            _ => throw new ContentClassificationValidationException("Unsupported classified content type.")
        };
    }
    private static IQueryable<T> Review<T>(IQueryable<T> query, bool required) where T : class, IClassifiedContentEntity =>
        required ? query.Where(e => e.Classification == ContentClassification.Unknown || e.Classification == ContentClassification.Prohibited ||
            e.Classification == ContentClassification.Cui) : query;

    private async Task<IClassifiedContentEntity?> LoadAsync(string type, Guid id, CancellationToken ct) => type switch
    {
        "EvidenceItem" => await Evidence.SingleOrDefaultAsync(e => e.Id == id, ct),
        "EvidenceFileVersion" => await Versions.SingleOrDefaultAsync(e => e.Id == id, ct),
        "ContractDocument" => await Documents.SingleOrDefaultAsync(e => e.Id == id, ct),
        "ExtractionJob" => await Jobs.SingleOrDefaultAsync(e => e.Id == id, ct),
        "ClassifiedNote" => await Notes.SingleOrDefaultAsync(e => e.Id == id, ct),
        "Report" => await Reports.SingleOrDefaultAsync(e => e.Id == id, ct),
        _ => throw new ContentClassificationValidationException("Unsupported classified content type.")
    };
    public async Task<ClassifiedContentDto?> FindAsync(string type, Guid id, CancellationToken ct) =>
        await LoadAsync(type, id, ct) is { } item ? Dto(type, item, (item as ReportEntity)?.CurrentClassification) : null;

    public async Task<IReadOnlyList<ContentClassificationHistoryDto>?> HistoryAsync(string type, Guid id, int offset, CancellationToken ct)
    {
        if (await LoadAsync(type, id, ct) is null) return null;
        var rows = await db.ContentClassificationHistory.AsNoTracking().Where(e => e.TenantId == context.TenantId &&
            e.EntityType == type && e.EntityId == id.ToString()).OrderByDescending(e => e.ChangedAt).ThenByDescending(e => e.Id)
            .Skip(offset).Take(100).ToArrayAsync(ct);
        return rows.Select(e => new ContentClassificationHistoryDto(e.Id, e.TenantId, e.EntityType, e.EntityId,
            e.PreviousClassification, e.NewClassification, e.Source, e.Confidence, e.ReviewedByUserId, e.ReviewedAt,
            e.Reason, e.ChangedByUserId, e.ChangedAt, e.Revision,
            e.PreviousMetadataJson is null ? null : JsonSerializer.Deserialize<ContentClassificationDto>(e.PreviousMetadataJson))).ToArray();
    }
    public async Task<ClassifiedContentDto?> ReclassifyAsync(string type, Guid id, ContentClassificationRequest classification,
        long? expectedRevision, Guid actor, CancellationToken ct)
    {
        var content = await LoadAsync(type, id, ct); if (content is null) return null;
        IClassifiedContentEntity current = (content as ReportEntity)?.CurrentClassification ?? content;
        if (expectedRevision.HasValue && expectedRevision != current.ClassificationRevision) throw new ContentRevisionConflictException();
        var previous = ClassificationMetadata.Read(current);
        if (content is ReportEntity report && report.CurrentClassification is null)
        {
            current = new ReportClassificationEntity { Id = report.Id };
            report.CurrentClassification = (ReportClassificationEntity)current;
            db.Add(current);
        }
        ClassificationMetadata.Apply(current, classification);
        current.ClassificationRevision++;
        if (content is ClassifiedNoteEntity note) note.Revision++;
        if (content is ContractDocumentEntity document) document.ContainsPotentialCui = classification.Classification == ContentClassification.Cui;
        db.ContentClassificationHistory.Add(ClassificationMetadata.History(current, context.TenantId, type, actor, DateTimeOffset.UtcNow, previous));
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new ContentRevisionConflictException(); }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { throw new ContentRevisionConflictException(); }
        return Dto(type, content, current);
    }
    private static ClassifiedContentDto Dto(string type, IClassifiedContentEntity content, IClassifiedContentEntity? current = null)
    {
        var (title, created) = content switch
        {
            EvidenceItemEntity e => (e.Name, e.CreatedAt), EvidenceFileVersionEntity e => (e.FileName, e.UploadedAt),
            ContractDocumentEntity e => (e.FileName, e.UploadedAt), ExtractionJobEntity e => ($"Extraction job {e.Id}", e.RequestedAt),
            ClassifiedNoteEntity e => (e.Title, e.CreatedAt), ReportEntity e => (e.Title, e.GeneratedAt),
            _ => throw new ContentClassificationValidationException("Unsupported classified content type.")
        };
        current ??= content;
        return new(type, content.Id, title, ClassificationMetadata.Read(current), current.ClassificationRevision, created);
    }
}
