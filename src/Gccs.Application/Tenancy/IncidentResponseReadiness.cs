namespace Gccs.Application.Tenancy;

public static class IncidentResponseReadiness
{
    public static readonly IReadOnlyList<string> RequiredPlaybooks =
    [
        "accidental-cui-upload",
        "suspected-cui-in-no-cui-tenant",
        "prohibited-data-upload",
        "cross-tenant-exposure-suspicion",
        "malware-detection",
        "failed-deletion-export-request"
    ];

    public static IReadOnlyList<string> ValidatePlaybooks(IReadOnlyList<IncidentResponsePlaybookDto> playbooks)
    {
        var errors = new List<string>();
        foreach (var key in RequiredPlaybooks)
        {
            if (!playbooks.Any(playbook => playbook.Key == key))
            {
                errors.Add($"Incident response playbook '{key}' is required.");
            }
        }

        foreach (var playbook in playbooks)
        {
            AddIf(errors, string.IsNullOrWhiteSpace(playbook.Trigger), $"{playbook.Key} trigger is required.");
            AddIf(errors, playbook.ContainmentSteps.Count == 0, $"{playbook.Key} containment steps are required.");
            AddIf(errors, string.IsNullOrWhiteSpace(playbook.NotificationPath), $"{playbook.Key} notification path is required.");
            AddIf(errors, playbook.EvidenceToCollect.Count == 0, $"{playbook.Key} evidence to collect is required.");
            AddIf(errors, string.IsNullOrWhiteSpace(playbook.Owner), $"{playbook.Key} owner is required.");
            AddIf(errors, string.IsNullOrWhiteSpace(playbook.ClosureCriteria), $"{playbook.Key} closure criteria is required.");
        }

        return errors;
    }

    public static IReadOnlyList<string> ValidateTabletop(IncidentReadinessTabletopDto tabletop)
    {
        var errors = new List<string>();
        AddIf(errors, tabletop.TabletopDate == default, "Tabletop date is required.");
        AddIf(errors, tabletop.Participants.Count == 0, "Tabletop participants are required.");
        AddIf(errors, tabletop.Findings.Count == 0, "Tabletop findings are required.");
        AddIf(errors, tabletop.FollowUpActions.Count == 0, "Tabletop follow-up actions are required.");
        return errors;
    }

    public static bool BlocksCuiReadyApproval(IReadOnlyList<IncidentResponseGapDto> gaps) =>
        gaps.Any(gap => gap.Status == IncidentResponseGapStatus.Open && gap.Severity == SecurityReviewFindingSeverity.Critical);

    private static void AddIf(ICollection<string> errors, bool condition, string message)
    {
        if (condition)
        {
            errors.Add(message);
        }
    }
}

public sealed record IncidentResponsePlaybookDto(
    string Key,
    string Trigger,
    IReadOnlyList<string> ContainmentSteps,
    string NotificationPath,
    IReadOnlyList<string> EvidenceToCollect,
    string Owner,
    string ClosureCriteria);

public sealed record IncidentReadinessTabletopDto(
    DateOnly TabletopDate,
    IReadOnlyList<string> Participants,
    IReadOnlyList<string> Findings,
    IReadOnlyList<string> FollowUpActions);

public sealed record IncidentResponseGapDto(
    string PlaybookKey,
    SecurityReviewFindingSeverity Severity,
    IncidentResponseGapStatus Status,
    string Summary);

public enum IncidentResponseGapStatus
{
    Open,
    Closed,
    AcceptedRisk
}
