using Gccs.Application.Audit;
using Gccs.Application.Security;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;

namespace Gccs.Application.Common;

public sealed record ClassifiedContentDto(string EntityType, Guid Id, string Title,
    ContentClassificationDto Classification, long Revision, DateTimeOffset CreatedAt);
public sealed record ReviewClassifiedContentRequest(ContentClassificationRequest Classification,
    [property: System.Text.Json.Serialization.JsonRequired] long ExpectedRevision);
public interface IClassifiedContentRepository
{
    Task<IReadOnlyList<ClassifiedContentDto>> ListAsync(string type, bool reviewOnly, int offset, CancellationToken ct);
    Task<ClassifiedContentDto?> FindAsync(string type, Guid id, CancellationToken ct);
    Task<IReadOnlyList<ContentClassificationHistoryDto>?> HistoryAsync(string type, Guid id, int offset, CancellationToken ct);
    Task<ClassifiedContentDto?> ReclassifyAsync(string type, Guid id, ContentClassificationRequest classification,
        long? expectedRevision, Guid actor, CancellationToken ct);
}

public sealed class ClassifiedContentService(IClassifiedContentRepository repository, IApplicationTransaction transaction,
    ICurrentTenantContext context, IAuditEventWriter audit)
{
    public Task<IReadOnlyList<ClassifiedContentDto>> ListAsync(string type, bool reviewOnly, int offset, CancellationToken ct) =>
        repository.ListAsync(type, reviewOnly, ValidateOffset(offset), ct);
    public Task<ClassifiedContentDto?> FindAsync(string type, Guid id, CancellationToken ct) => repository.FindAsync(type, id, ct);
    public Task<IReadOnlyList<ContentClassificationHistoryDto>?> HistoryAsync(string type, Guid id, int offset, CancellationToken ct) =>
        repository.HistoryAsync(type, id, ValidateOffset(offset), ct);
    public async Task<ClassifiedContentDto?> ReclassifyAsync(string type, Guid id, ReviewClassifiedContentRequest request, CancellationToken ct)
    {
        if (await repository.FindAsync(type, id, ct) is null) return null;
        if (request.Classification is null || string.IsNullOrWhiteSpace(request.Classification.Reason))
            throw new ContentClassificationValidationException("A classification review reason is required.");
        if (request.ExpectedRevision < 0) throw new ContentClassificationValidationException("A valid current revision is required.");
        var classification = request.Classification with { Source = ContentClassificationSource.AdminReviewed,
            ReviewedByUserId = context.UserId, ReviewedAt = DateTimeOffset.UtcNow,
            IsApprovedDemoContent = false, Reason = request.Classification.Reason.Trim() };
        ContentClassificationPolicy.Validate(classification);
        if (classification.Classification == ContentClassification.SyntheticCui)
            throw new ContentClassificationValidationException("Synthetic provenance can only be established by the trusted demo seed workflow.");
        // Review identifies/quarantines existing content. It never grants permission to process CUI.
        return await transaction.ExecuteAsync(async token =>
        {
            var result = await repository.ReclassifyAsync(type, id, classification, request.ExpectedRevision, context.UserId, token);
            if (result is not null)
                await audit.WriteAsync(context.TenantId, context.UserId, AuditAction.Updated, type, id.ToString(),
                    "Content classification reviewed.", new Dictionary<string, string> {
                        ["classification"] = result.Classification.Classification.ToString(),
                        ["classificationRevision"] = result.Revision.ToString(), ["reason"] = classification.Reason! }, token);
            return result;
        }, ct);
    }
    private static int ValidateOffset(int offset) => offset is >= 0 and <= 100_000 ? offset :
        throw new ContentClassificationValidationException("Offset must be between 0 and 100000.");
}
