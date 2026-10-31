using Gccs.Application.Common;
using Gccs.Domain.Common;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ClassificationBoundaryValidationTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(999)]
    public void Undefined_classification_and_source_are_rejected(int value)
    {
        Assert.Throws<ContentClassificationValidationException>(() => ContentClassificationPolicy.Validate(
            new ContentClassificationRequest((ContentClassification)value)));
        Assert.Throws<ContentClassificationValidationException>(() => ContentClassificationPolicy.Validate(
            new ContentClassificationRequest(ContentClassification.Unclassified, (ContentClassificationSource)value)));
    }

    [Fact]
    public void Uploads_cannot_assert_demo_provenance_or_reviewer_identity()
    {
        Assert.Throws<ContentClassificationValidationException>(() => ContentClassificationPolicy.ValidateUserSelection(
            new ContentClassificationRequest(ContentClassification.SyntheticCui, ContentClassificationSource.ImportedDemoSeed,
                IsApprovedDemoContent: true)));
        Assert.Throws<ContentClassificationValidationException>(() => ContentClassificationPolicy.ValidateUserSelection(
            new ContentClassificationRequest(ContentClassification.Unclassified, ReviewedByUserId: Guid.NewGuid())));
        ContentClassificationPolicy.ValidateUserSelection(new ContentClassificationRequest(ContentClassification.Unclassified));
    }
}
