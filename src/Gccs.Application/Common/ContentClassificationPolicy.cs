using Gccs.Application.Tenancy;
using Gccs.Domain.Common;
using Gccs.Domain.Tenancy;

namespace Gccs.Application.Common;

public sealed class ContentClassificationPolicy(TenantDataHandlingModePolicyService tenantModePolicy)
{
    public async Task EnsureUsableAsync(ContentClassificationDto classification, TenantDataHandlingWorkflow workflow,
        Guid actorUserId, string entityType, string entityId, CancellationToken cancellationToken = default)
    {
        EnsureProcessable(classification.Classification, workflow.ToString());
        await EnsureAllowedAsync(new ContentClassificationRequest(classification.Classification, classification.Source,
            classification.Confidence, classification.ReviewedByUserId, classification.ReviewedAt,
            classification.Reason, classification.IsApprovedDemoContent), workflow, actorUserId, entityType, entityId, cancellationToken);
    }
    public async Task EnsureAllowedAsync(
        ContentClassificationRequest classification,
        TenantDataHandlingWorkflow workflow,
        Guid actorUserId,
        string? entityType = null,
        string? entityId = null,
        CancellationToken cancellationToken = default)
    {
        Validate(classification);

        if (classification.Classification is ContentClassification.Prohibited)
        {
            throw new ContentClassificationValidationException("Prohibited content cannot be stored or processed.");
        }

        if (classification.Classification is ContentClassification.SyntheticCui &&
            (!classification.IsApprovedDemoContent || classification.Source is not ContentClassificationSource.ImportedDemoSeed))
        {
            throw new ContentClassificationValidationException("SyntheticCui classification is allowed only for approved imported demo seed content.");
        }

        await tenantModePolicy.EnsureAllowedAsync(
            new TenantDataHandlingModePolicyRequest(
                workflow,
                ContainsRealCui: classification.Classification is ContentClassification.Cui,
                ContainsSyntheticCui: classification.Classification is ContentClassification.SyntheticCui,
                ClassificationConfirmed: classification.Classification is not ContentClassification.Unknown,
                ApprovalChecksPassed: classification.Classification is not ContentClassification.SyntheticCui ||
                    (classification.IsApprovedDemoContent && classification.Source is ContentClassificationSource.ImportedDemoSeed),
                EntityType: entityType,
                EntityId: entityId),
            actorUserId,
            cancellationToken);
    }

    public static void EnsureProcessable(ContentClassification classification, string workflow)
    {
        if (classification is ContentClassification.Unknown)
        {
            throw new ContentClassificationValidationException($"{workflow} is blocked until Unknown classification is reviewed or reclassified.");
        }

        if (classification is ContentClassification.Prohibited)
        {
            throw new ContentClassificationValidationException($"{workflow} is blocked for Prohibited content.");
        }
    }

    public static ContentClassificationRequest DefaultUnclassified() =>
        new(ContentClassification.Unclassified);

    public static ContentClassificationRequest FromLegacyCuiFlag(bool containsPotentialCui) =>
        containsPotentialCui
            ? new ContentClassificationRequest(ContentClassification.Cui)
            : DefaultUnclassified();

    public static void Validate(ContentClassificationRequest classification)
    {
        if (!Enum.IsDefined(classification.Classification) || !Enum.IsDefined(classification.Source))
        {
            throw new ContentClassificationValidationException("A defined classification and source are required.");
        }
        if (classification.Confidence is < 0m or > 1m)
        {
            throw new ContentClassificationValidationException("Classification confidence must be between 0 and 1.");
        }

        if (classification.Source is ContentClassificationSource.AdminReviewed &&
            (classification.ReviewedByUserId is null || classification.ReviewedAt is null))
        {
            throw new ContentClassificationValidationException("Admin-reviewed classification requires reviewer and review date metadata.");
        }

        if (classification.Reason?.Length > 600)
        {
            throw new ContentClassificationValidationException("Classification reason must be 600 characters or fewer.");
        }
    }

    public static void ValidateUserSelection(ContentClassificationRequest classification)
    {
        Validate(classification);
        if (classification.Source != ContentClassificationSource.UserSelected || classification.IsApprovedDemoContent ||
            classification.ReviewedByUserId is not null || classification.ReviewedAt is not null)
            throw new ContentClassificationValidationException("Reviewer identity and approved demo provenance cannot be supplied by an upload or metadata request.");
    }
}

public sealed class ContentClassificationValidationException(string message) : InvalidOperationException(message);
