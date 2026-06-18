using Gccs.Application.Audit;
using Gccs.Application.Evidence;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;

namespace Gccs.Application.Common;

public sealed class ContentClassificationReviewService(
    IContentClassificationReviewRepository repository,
    ContentClassificationPolicy classificationPolicy,
    IAuditEventWriter auditEventWriter)
{
    public Task<IReadOnlyList<ContentClassificationReviewItemDto>> ListAsync(CancellationToken cancellationToken = default) =>
        repository.ListAsync(cancellationToken);

    public async Task<EvidenceMetadataDto?> ReclassifyEvidenceAsync(
        Guid evidenceItemId,
        ReclassifyContentRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Classification.Reason))
        {
            throw new ContentClassificationValidationException("A classification update reason is required.");
        }

        await classificationPolicy.EnsureAllowedAsync(
            request.Classification,
            TenantDataHandlingWorkflow.EvidenceUpload,
            actorUserId,
            "EvidenceItem",
            evidenceItemId.ToString(),
            cancellationToken);

        var updated = await repository.ReclassifyEvidenceAsync(evidenceItemId, request, actorUserId, cancellationToken);
        if (updated is not null)
        {
            await auditEventWriter.WriteAsync(
                updated.TenantId,
                actorUserId,
                AuditAction.Updated,
                "EvidenceItem",
                updated.Id.ToString(),
                "Evidence item classification was updated by an authorized reviewer.",
                new Dictionary<string, string>
                {
                    ["classification"] = updated.Classification.Classification.ToString(),
                    ["classificationSource"] = updated.Classification.Source.ToString(),
                    ["reason"] = updated.Classification.Reason ?? string.Empty
                },
                cancellationToken);
        }

        return updated;
    }
}

public interface IContentClassificationReviewRepository
{
    Task<IReadOnlyList<ContentClassificationReviewItemDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<EvidenceMetadataDto?> ReclassifyEvidenceAsync(
        Guid evidenceItemId,
        ReclassifyContentRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed record ReclassifyContentRequest(ContentClassificationRequest Classification);

public sealed record ContentClassificationReviewItemDto(
    Guid TenantId,
    string EntityType,
    string EntityId,
    string Title,
    ContentClassificationDto Classification,
    DateTimeOffset CreatedAt,
    string ReviewRoute);
