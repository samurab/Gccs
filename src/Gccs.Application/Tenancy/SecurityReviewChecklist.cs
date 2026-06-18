namespace Gccs.Application.Tenancy;

public static class SecurityReviewChecklist
{
    public static readonly IReadOnlyList<string> RequiredAreas =
    [
        "tenant-isolation",
        "evidence-storage",
        "encryption",
        "malware-scanning",
        "retention",
        "backup",
        "restore",
        "admin-access",
        "support-access",
        "logging",
        "monitoring",
        "incident-response"
    ];

    public static IReadOnlyList<string> ValidateItems(IReadOnlyList<SecurityReviewChecklistItemDto> items)
    {
        var errors = new List<string>();
        foreach (var area in RequiredAreas)
        {
            if (!items.Any(item => item.Area == area))
            {
                errors.Add($"Security review area '{area}' is required.");
            }
        }

        foreach (var item in items.Where(item => item.Status is SecurityReviewItemStatus.Passed or SecurityReviewItemStatus.AcceptedRisk))
        {
            if (item.ReviewerUserId is null)
            {
                errors.Add($"{item.Area} requires reviewer.");
            }

            if (item.ReviewedAt is null)
            {
                errors.Add($"{item.Area} requires review date.");
            }

            if (string.IsNullOrWhiteSpace(item.EvidenceLink) && string.IsNullOrWhiteSpace(item.Rationale))
            {
                errors.Add($"{item.Area} requires evidence link or rationale.");
            }
        }

        return errors;
    }

    public static bool BlocksCuiReadyApproval(IReadOnlyList<SecurityReviewFindingDto> findings) =>
        findings.Any(finding =>
            finding.Status == SecurityReviewFindingStatus.Open &&
            finding.Severity is SecurityReviewFindingSeverity.High or SecurityReviewFindingSeverity.Critical);

    public static IReadOnlyList<string> ValidateAcceptedRisk(AcceptedSecurityRiskDto risk)
    {
        var errors = new List<string>();
        if (risk.ApproverUserId == Guid.Empty)
        {
            errors.Add("Accepted risk approver is required.");
        }

        if (risk.AcceptedAt == default)
        {
            errors.Add("Accepted risk date is required.");
        }

        if (string.IsNullOrWhiteSpace(risk.Scope))
        {
            errors.Add("Accepted risk scope is required.");
        }

        if (risk.ExpiresAt is null && risk.ReviewAt is null)
        {
            errors.Add("Accepted risk expiration or review date is required.");
        }

        if (string.IsNullOrWhiteSpace(risk.MitigationNote))
        {
            errors.Add("Accepted risk mitigation note is required.");
        }

        return errors;
    }
}

public sealed record SecurityReviewChecklistItemDto(
    string Area,
    SecurityReviewItemStatus Status,
    Guid? ReviewerUserId,
    DateOnly? ReviewedAt,
    string? EvidenceLink,
    string? Rationale);

public sealed record SecurityReviewFindingDto(
    string Area,
    SecurityReviewFindingSeverity Severity,
    SecurityReviewFindingStatus Status,
    string Summary);

public sealed record AcceptedSecurityRiskDto(
    Guid ApproverUserId,
    DateOnly AcceptedAt,
    string Scope,
    DateOnly? ExpiresAt,
    DateOnly? ReviewAt,
    string MitigationNote);

public enum SecurityReviewItemStatus
{
    NotStarted,
    InReview,
    Passed,
    FindingOpen,
    AcceptedRisk
}

public enum SecurityReviewFindingSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum SecurityReviewFindingStatus
{
    Open,
    Closed,
    AcceptedRisk
}
