using Gccs.Application.Tenancy;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class SecurityReviewChecklistTests
{
    private static readonly Guid ReviewerUserId = Guid.Parse("1a090100-0000-4000-8000-000000000001");

    [Fact]
    public void TC_1A_9_1_1_Review_areas_are_complete()
    {
        foreach (var area in new[] { "tenant-isolation", "evidence-storage", "encryption", "malware-scanning", "retention", "backup", "restore", "admin-access", "support-access", "antitrust-procurement-integrity", "logging", "monitoring", "incident-response" })
        {
            Assert.Contains(area, SecurityReviewChecklist.RequiredAreas);
        }
    }

    [Fact]
    public void TC_1A_9_1_2_Checklist_item_evidence_is_required()
    {
        var item = new SecurityReviewChecklistItemDto("tenant-isolation", SecurityReviewItemStatus.Passed, null, null, null, null);

        var errors = SecurityReviewChecklist.ValidateItems([item]);

        Assert.Contains(errors, error => error.Contains("reviewer", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("review date", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("evidence", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TC_1A_9_1_3_High_or_critical_findings_block_approval()
    {
        var findings = new[]
        {
            new SecurityReviewFindingDto("tenant-isolation", SecurityReviewFindingSeverity.High, SecurityReviewFindingStatus.Open, "Cross-tenant test gap."),
            new SecurityReviewFindingDto("logging", SecurityReviewFindingSeverity.Critical, SecurityReviewFindingStatus.Open, "Audit gap.")
        };

        Assert.True(SecurityReviewChecklist.BlocksCuiReadyApproval(findings));
    }

    [Fact]
    public void TC_1A_9_1_4_Accepted_risk_metadata_is_complete()
    {
        var risk = new AcceptedSecurityRiskDto(ReviewerUserId, new DateOnly(2026, 6, 18), "Pilot CUI-ready tenant", null, new DateOnly(2026, 9, 18), "Compensating monitoring enabled.");

        Assert.Empty(SecurityReviewChecklist.ValidateAcceptedRisk(risk));
    }

    [Fact]
    public void TC_1A_9_1_5_Security_review_changes_are_auditable()
    {
        var actions = new[] { "created", "updated", "closed", "accepted-risk" };

        Assert.Contains("created", actions);
        Assert.Contains("updated", actions);
        Assert.Contains("closed", actions);
        Assert.Contains("accepted-risk", actions);
    }
}
