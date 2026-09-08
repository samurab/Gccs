using Gccs.Application.Audit;
using Gccs.Application.Security;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;

namespace Gccs.Application.Common;

public sealed record SaveClassifiedNoteRequest(string Title, string Body, ContentClassificationRequest? Classification, long Revision = 0);
public sealed record ClassifiedNoteDto(Guid Id, string Title, string Body, ContentClassificationDto Classification,
    long Revision, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public interface IClassifiedNoteRepository
{
    Task<IReadOnlyList<ClassifiedNoteDto>> ListAsync(CancellationToken cancellationToken);
    Task<ClassifiedNoteDto?> FindAsync(Guid id, CancellationToken cancellationToken);
    Task<ClassifiedNoteDto> SaveAsync(Guid id, SaveClassifiedNoteRequest request, Guid actor, CancellationToken cancellationToken);
}

public sealed class ClassifiedNoteService(IClassifiedNoteRepository repository, ContentClassificationPolicy policy,
    ICurrentTenantContext context, IApplicationTransaction transaction, IAuditEventWriter audit)
{
    public Task<IReadOnlyList<ClassifiedNoteDto>> ListAsync(CancellationToken cancellationToken) => repository.ListAsync(cancellationToken);
    public async Task<ClassifiedNoteDto?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        var note = await repository.FindAsync(id, cancellationToken);
        if (note is not null)
            await policy.EnsureUsableAsync(note.Classification, TenantDataHandlingWorkflow.Note, context.UserId, "ClassifiedNote", id.ToString(), cancellationToken);
        return note;
    }
    public async Task<ClassifiedNoteDto?> SaveAsync(Guid? id, SaveClassifiedNoteRequest request, CancellationToken cancellationToken)
    {
        var previous = id is null ? null : await repository.FindAsync(id.Value, cancellationToken);
        if (id is not null && previous is null) return null;
        var classification = request.Classification ?? throw new ContentClassificationValidationException("Explicit note classification is required.");
        ContentClassificationPolicy.ValidateUserSelection(classification);
        if (!ClassifiedNote.HasValidContent(request.Title, request.Body))
            throw new ContentClassificationValidationException("A title (up to 240 characters) and note text (up to 20,000 characters) are required.");
        if (previous is not null && previous.Classification.Classification != classification.Classification)
            throw new ContentClassificationValidationException("Use classification review to reclassify an existing note.");
        await policy.EnsureAllowedAsync(classification, TenantDataHandlingWorkflow.Note, context.UserId, "ClassifiedNote", id?.ToString(), cancellationToken);
        return await transaction.ExecuteAsync(async ct =>
        {
            var saved = await repository.SaveAsync(id ?? Guid.NewGuid(), request with { Title = request.Title.Trim() }, context.UserId, ct);
            await audit.WriteAsync(context.TenantId, context.UserId, id is null ? AuditAction.Created : AuditAction.Updated,
                "ClassifiedNote", saved.Id.ToString(), "Classified note saved.",
                new Dictionary<string, string> { ["classification"] = saved.Classification.Classification.ToString(), ["revision"] = saved.Revision.ToString() }, ct);
            return saved;
        }, cancellationToken);
    }
}
