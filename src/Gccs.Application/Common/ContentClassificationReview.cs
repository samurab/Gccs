using Gccs.Application.Audit;
using Gccs.Application.Evidence;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;

namespace Gccs.Application.Common;

public sealed class ContentClassificationReviewService(
    IContentClassificationReviewRepository repository,
    IAuditEventWriter auditEventWriter,
    IEvidenceMetadataRepository evidenceRepository,
    IApplicationTransaction transaction)
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

        if (await evidenceRepository.FindCurrentTenantAsync(evidenceItemId, cancellationToken) is null) return null;
        // Classification review may quarantine content; it does not authorize its use.
        request = request with
        {
            Classification = request.Classification with
            {
                Source = Gccs.Domain.Common.ContentClassificationSource.AdminReviewed,
                ReviewedByUserId = actorUserId,
                ReviewedAt = DateTimeOffset.UtcNow,
                IsApprovedDemoContent = false
            }
        };
        ContentClassificationPolicy.Validate(request.Classification);
        if (request.Classification.Classification == Gccs.Domain.Common.ContentClassification.SyntheticCui)
            throw new ContentClassificationValidationException("Synthetic demo provenance can only be established by the demo seed workflow.");

        return await transaction.ExecuteAsync(async cancellationToken =>
        {
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
        }, cancellationToken);
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
