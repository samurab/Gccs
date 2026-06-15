using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Infrastructure.Compliance;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ObligationSchemaTests
{
    [Fact]
    public void TC_6_1_1_Published_obligation_requires_source_url()
    {
        var obligation = CreateValidObligation() with
        {
            SourceReference = CreateSource(null)
        };

        var errors = ObligationPublicationValidator.ValidateForPublication(obligation);

        Assert.Contains(errors, error => error.Contains("sourceUrl", StringComparison.OrdinalIgnoreCase));
        Assert.Throws<ObligationPublicationValidationException>(() => ObligationPublicationValidator.EnsureCanPublish(obligation));
    }

    [Fact]
    public void TC_6_1_2_Published_obligation_requires_last_reviewed_date()
    {
        var obligation = CreateValidObligation() with
        {
            SourceReference = CreateSource(new Uri("https://www.acquisition.gov/far/52.204-21"), default(DateOnly))
        };

        var errors = ObligationPublicationValidator.ValidateForPublication(obligation);

        Assert.Contains(errors, error => error.Contains("lastReviewedAt", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TC_6_1_3_Published_obligation_requires_core_metadata_and_review_state()
    {
        var obligation = CreateValidObligation() with
        {
            TriggerCondition = "",
            RequiredAction = "",
            OwnerFunction = "",
            FlowDownRequirement = "",
            SourceReference = CreateSource(new Uri("https://www.acquisition.gov/far/52.204-21"), new DateOnly(2026, 6, 3), ""),
            Review = new ReviewMetadata(default, null, null, "", false, ReviewState.Draft)
        };

        var errors = ObligationPublicationValidator.ValidateForPublication(obligation);

        Assert.Contains(errors, error => error.Contains("triggerCondition", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("requiredAction", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("ownerFunction", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("flowDownRequirement", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("sourceConfidence", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("reviewConfidence", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("reviewState", StringComparison.OrdinalIgnoreCase));

        var validObligation = CreateValidObligation();
        Assert.Equal(RiskLevel.High, validObligation.RiskLevel);
        Assert.True(validObligation.RequiresFlowDown);
        Assert.Empty(ObligationPublicationValidator.ValidateForPublication(validObligation));
    }

    [Fact]
    public async Task TC_6_1_4_Evidence_examples_are_linked_and_returned_with_obligation()
    {
        var repository = new InMemoryObligationRepository();

        var obligation = await repository.FindByIdAsync("far-52-204-21");

        Assert.NotNull(obligation);
        Assert.NotEmpty(obligation.EvidenceExamples);
        Assert.Contains(obligation.EvidenceExamples, example =>
            example.Name == "Access control policy" &&
            example.Owner == "IT/security");
        Assert.Equal(ReviewState.Published, obligation.Review.State);
    }

    private static Obligation CreateValidObligation() =>
        new(
            "far-52-204-21",
            "FAR 52.204-21",
            "Basic Safeguarding",
            "Apply baseline safeguards.",
            "Contract involves FCI.",
            "Implement safeguards and retain evidence.",
            "IT/security",
            RiskLevel.High,
            true,
            "Flow down when subcontractors may handle FCI.",
            new ApplicabilityDimension("prime/sub", "federal contract", "FCI", "any", "any", "FCI access"),
            [new EvidenceExample("Access control policy", "Policy describing authorized access.", "IT/security")],
            CreateSource(new Uri("https://www.acquisition.gov/far/52.204-21")),
            new ReviewMetadata(new DateOnly(2026, 6, 3), null, new DateOnly(2026, 9, 3), "high", false, ReviewState.Published));

    private static ComplianceSource CreateSource(Uri? uri, DateOnly? lastReviewedAt = null, string confidence = "high") =>
        new("FAR 52.204-21", uri!, lastReviewedAt ?? new DateOnly(2026, 6, 3), null, confidence, false);
}
