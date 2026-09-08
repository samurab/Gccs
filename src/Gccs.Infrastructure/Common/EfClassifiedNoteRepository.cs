using Gccs.Application.Common;
using Gccs.Application.Security;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Common;

public sealed class EfClassifiedNoteRepository(GccsDbContext db, ICurrentTenantContext context) : IClassifiedNoteRepository
{
    private IQueryable<ClassifiedNoteEntity> Query => db.Set<ClassifiedNoteEntity>().Where(n => n.TenantId == context.TenantId);
    // Listing returns metadata only; note text is retrieved through the policy-checked detail path.
    public async Task<IReadOnlyList<ClassifiedNoteDto>> ListAsync(CancellationToken ct) =>
        await Query.AsNoTracking().OrderByDescending(n => n.UpdatedAt).ThenByDescending(n => n.Id).Take(200)
            .Select(n => new ClassifiedNoteDto(n.Id, n.Title, "",
                new(n.Classification, n.ClassificationSource, n.ClassificationConfidence, n.ClassificationReviewedByUserId,
                    n.ClassificationReviewedAt, n.ClassificationReason, n.ClassificationIsApprovedDemoContent),
                n.Revision, n.CreatedAt, n.UpdatedAt)).ToArrayAsync(ct);
    public async Task<ClassifiedNoteDto?> FindAsync(Guid id, CancellationToken ct) =>
        await Query.AsNoTracking().SingleOrDefaultAsync(n => n.Id == id, ct) is { } note ? ToDto(note) : null;
    public async Task<ClassifiedNoteDto> SaveAsync(Guid id, SaveClassifiedNoteRequest request, Guid actor, CancellationToken ct)
    {
        var note = await Query.SingleOrDefaultAsync(n => n.Id == id, ct);
        if (note is null)
        {
            if (request.Revision != 0) throw new ContentRevisionConflictException();
            note = new() { Id = id, TenantId = context.TenantId, CreatedAt = DateTimeOffset.UtcNow,
                Classification = request.Classification!.Classification, ClassificationSource = request.Classification.Source,
                ClassificationReason = request.Classification.Reason, ClassificationConfidence = request.Classification.Confidence };
            db.Add(note);
            db.ContentClassificationHistory.Add(new() { Id = Guid.NewGuid(), TenantId = context.TenantId,
                EntityType = "ClassifiedNote", EntityId = id.ToString(), NewClassification = note.Classification,
                Source = note.ClassificationSource, Confidence = note.ClassificationConfidence, Reason = note.ClassificationReason,
                ChangedAt = note.CreatedAt, ChangedByUserId = actor });
        }
        else if (request.Revision != note.Revision) throw new ContentRevisionConflictException();
        note.Title = request.Title; note.Body = request.Body; note.Revision++; note.UpdatedAt = DateTimeOffset.UtcNow;
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new ContentRevisionConflictException(); }
        return ToDto(note);
    }
    private static ClassifiedNoteDto ToDto(ClassifiedNoteEntity n) => new(n.Id, n.Title, n.Body,
        new(n.Classification, n.ClassificationSource, n.ClassificationConfidence, n.ClassificationReviewedByUserId,
            n.ClassificationReviewedAt, n.ClassificationReason, n.ClassificationIsApprovedDemoContent), n.Revision, n.CreatedAt, n.UpdatedAt);
}
