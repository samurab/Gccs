using Gccs.Application.Tenancy;
using Gccs.Domain.Common;
using Gccs.Domain.Tenancy;

namespace Gccs.Application.Common;

public sealed class ContentClassificationPolicy(TenantDataHandlingModePolicyService tenantModePolicy)
{
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

        if (classification.Classification is ContentClassification.Unknown)
        {
            return;
        }

        await tenantModePolicy.EnsureAllowedAsync(
            new TenantDataHandlingModePolicyRequest(
                workflow,
                ContainsRealCui: classification.Classification is ContentClassification.Cui,
                ContainsSyntheticCui: classification.Classification is ContentClassification.SyntheticCui,
                ClassificationConfirmed: classification.Classification is not ContentClassification.Unknown,
                ApprovalChecksPassed: true,
                EntityType: entityType,
                EntityId: entityId),
            actorUserId,
            cancellationToken);

        if (classification.Classification is ContentClassification.SyntheticCui &&
            (!classification.IsApprovedDemoContent || classification.Source is not ContentClassificationSource.ImportedDemoSeed))
        {
            throw new ContentClassificationValidationException("SyntheticCui classification is allowed only for approved imported demo seed content.");
        }
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
}

public sealed class ContentClassificationValidationException(string message) : InvalidOperationException(message);
