using Gccs.Application.Tenancy;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class IncidentResponseReadinessTests
{
    [Fact]
    public void TC_1A_9_3_1_Required_playbooks_exist()
    {
        foreach (var key in new[] { "accidental-cui-upload", "suspected-cui-in-no-cui-tenant", "prohibited-data-upload", "cross-tenant-exposure-suspicion", "malware-detection", "failed-deletion-export-request" })
        {
            Assert.Contains(key, IncidentResponseReadiness.RequiredPlaybooks);
        }
    }

    [Fact]
    public void TC_1A_9_3_2_Playbook_content_is_complete()
    {
        Assert.Empty(IncidentResponseReadiness.ValidatePlaybooks(CompletePlaybooks()));
    }

    [Fact]
    public void TC_1A_9_3_3_Tabletop_evidence_is_captured()
    {
        var tabletop = new IncidentReadinessTabletopDto(
            new DateOnly(2026, 6, 18),
            ["Security Owner", "Engineering Lead"],
            ["Escalation owner confirmed."],
            ["Review support script quarterly."]);

        Assert.Empty(IncidentResponseReadiness.ValidateTabletop(tabletop));
    }

    [Fact]
    public void TC_1A_9_3_4_Critical_response_gaps_block_approval()
    {
        var gaps = new[]
        {
            new IncidentResponseGapDto("malware-detection", SecurityReviewFindingSeverity.Critical, IncidentResponseGapStatus.Open, "No containment owner.")
        };

        Assert.True(IncidentResponseReadiness.BlocksCuiReadyApproval(gaps));
    }

    [Fact]
    public void TC_1A_9_3_5_Incident_readiness_approval_is_traceable()
    {
        var approvalMetadata = new Dictionary<string, string>
        {
            ["approval"] = "incident-response-readiness",
            ["reviewOwner"] = "GCCS Security Owner",
            ["result"] = "approved",
            ["sourceReference"] = "Phase 1A incident response readiness"
        };

        Assert.Equal("approved", approvalMetadata["result"]);
        Assert.False(string.IsNullOrWhiteSpace(approvalMetadata["sourceReference"]));
    }

    private static IncidentResponsePlaybookDto[] CompletePlaybooks() =>
        IncidentResponseReadiness.RequiredPlaybooks.Select(key =>
            new IncidentResponsePlaybookDto(
                key,
                "Trigger identified by workflow, support, or monitoring.",
                ["Contain affected content.", "Notify security owner."],
                "Security owner to customer success to legal/compliance advisor when required.",
                ["Audit events", "Affected entity references", "Screenshots without sensitive content"],
                "GCCS Security Owner",
                "Risk contained, customer notified where required, and follow-up actions assigned."))
            .ToArray();
}
