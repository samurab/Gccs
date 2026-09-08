using Gccs.Domain.Common;

namespace Gccs.Infrastructure.Persistence.Models;

public interface IClassifiedContentEntity
{
    Guid Id { get; set; }
    long ClassificationRevision { get; set; }
    ContentClassification Classification { get; set; }
    ContentClassificationSource ClassificationSource { get; set; }
    decimal? ClassificationConfidence { get; set; }
    Guid? ClassificationReviewedByUserId { get; set; }
    DateTimeOffset? ClassificationReviewedAt { get; set; }
    string? ClassificationReason { get; set; }
    bool ClassificationIsApprovedDemoContent { get; set; }
}

// Current handling metadata is separate from the report's immutable generation snapshot.
public sealed class ReportClassificationEntity : IClassifiedContentEntity
{
    public Guid Id { get; set; }
    public long ClassificationRevision { get; set; }
    public ContentClassification Classification { get; set; }
    public ContentClassificationSource ClassificationSource { get; set; }
    public decimal? ClassificationConfidence { get; set; }
    public Guid? ClassificationReviewedByUserId { get; set; }
    public DateTimeOffset? ClassificationReviewedAt { get; set; }
    public string? ClassificationReason { get; set; }
    public bool ClassificationIsApprovedDemoContent { get; set; }
}
