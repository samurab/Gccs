using System.Text.Json;
using Gccs.Application.Common;
using Gccs.Infrastructure.Persistence.Models;

namespace Gccs.Infrastructure.Common;

internal static class ClassificationMetadata
{
    public static ContentClassificationDto Read(IClassifiedContentEntity item) => new(item.Classification,
        item.ClassificationSource, item.ClassificationConfidence, item.ClassificationReviewedByUserId,
        item.ClassificationReviewedAt, item.ClassificationReason, item.ClassificationIsApprovedDemoContent);

    public static void Apply(IClassifiedContentEntity item, ContentClassificationRequest value)
    {
        item.Classification = value.Classification; item.ClassificationSource = value.Source;
        item.ClassificationConfidence = value.Confidence; item.ClassificationReviewedByUserId = value.ReviewedByUserId;
        item.ClassificationReviewedAt = value.ReviewedAt; item.ClassificationReason = value.Reason;
        item.ClassificationIsApprovedDemoContent = value.IsApprovedDemoContent;
    }

    public static ContentClassificationHistoryEntity History(IClassifiedContentEntity item, Guid tenant,
        string type, Guid actor, DateTimeOffset at, ContentClassificationDto? previous = null) => new()
    {
        Id = Guid.NewGuid(), TenantId = tenant, EntityType = type, EntityId = item.Id.ToString(),
        PreviousClassification = previous?.Classification, PreviousMetadataJson = previous is null ? null : JsonSerializer.Serialize(previous),
        NewClassification = item.Classification, Source = item.ClassificationSource, Confidence = item.ClassificationConfidence,
        ReviewedByUserId = item.ClassificationReviewedByUserId, ReviewedAt = item.ClassificationReviewedAt,
        Reason = item.ClassificationReason, ChangedByUserId = actor, ChangedAt = at, Revision = item.ClassificationRevision
    };
}
