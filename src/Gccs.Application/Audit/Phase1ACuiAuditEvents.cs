using Gccs.Domain.Audit;

namespace Gccs.Application.Audit;

public static class Phase1ACuiAuditEvents
{
    public static readonly IReadOnlyList<RequiredCuiAuditEvent> RequiredEvents =
    [
        new("mode-change", "Tenant", AuditAction.Updated, true, true),
        new("classification-create-update", "ContentClassification", AuditAction.Updated, true, true),
        new("blocked-upload", "EvidenceUploadIntent", AuditAction.Rejected, true, true),
        new("blocked-extraction", "ContractExtractionJob", AuditAction.Rejected, true, true),
        new("blocked-report", "Report", AuditAction.Rejected, true, true),
        new("failed-mode-change", "TenantDataHandlingMode", AuditAction.Rejected, true, true),
        new("failed-cui-approval", "CuiReadyApprovalChecklist", AuditAction.Rejected, true, true),
        new("checklist-approval", "CuiReadyApprovalChecklist", AuditAction.Approved, true, false),
        new("checklist-rejection", "CuiReadyApprovalChecklist", AuditAction.Rejected, true, true),
        new("matrix-acknowledgement", "SharedResponsibilityMatrixAcknowledgement", AuditAction.Created, true, false),
        new("notice-acknowledgement", "DataHandlingNoticeAcknowledgement", AuditAction.Created, true, false),
        new("download", "EvidenceFileVersion", AuditAction.Downloaded, true, false),
        new("export", "EvidencePackage", AuditAction.Exported, true, false),
        new("deletion", "EvidenceFileVersion", AuditAction.Deleted, true, false),
        new("escalation-create", "CuiSupportEscalation", AuditAction.Created, true, false),
        new("escalation-update", "CuiSupportEscalation", AuditAction.Updated, true, false),
        new("extraction-start", "ContractExtractionJob", AuditAction.Created, true, false),
        new("extraction-stop", "ContractExtractionJob", AuditAction.Updated, true, false)
    ];

    private static readonly string[] SensitiveSummaryTerms =
    [
        "secret access key",
        "private key",
        "classified paragraph",
        "controlled technical data",
        "social security number",
        "bank account"
    ];

    public static IReadOnlyList<string> Validate(IReadOnlyList<CuiAuditEventSnapshot> events)
    {
        var errors = new List<string>();
        foreach (var required in RequiredEvents)
        {
            if (!events.Any(candidate => candidate.EventType == required.EventType && candidate.EntityType == required.EntityType && candidate.Action == required.Action))
            {
                errors.Add($"Required CUI audit event '{required.EventType}' was not emitted.");
            }
        }

        foreach (var auditEvent in events)
        {
            if (auditEvent.TenantId == Guid.Empty)
            {
                errors.Add($"{auditEvent.EventType} is missing tenant ID.");
            }

            if (auditEvent.ActorUserId == Guid.Empty)
            {
                errors.Add($"{auditEvent.EventType} is missing actor ID.");
            }

            if (string.IsNullOrWhiteSpace(auditEvent.EntityType) || string.IsNullOrWhiteSpace(auditEvent.EntityId))
            {
                errors.Add($"{auditEvent.EventType} is missing entity reference.");
            }

            if (auditEvent.OccurredAt == default)
            {
                errors.Add($"{auditEvent.EventType} is missing timestamp.");
            }

            if (!auditEvent.Metadata.ContainsKey("result"))
            {
                errors.Add($"{auditEvent.EventType} is missing result metadata.");
            }

            if (SensitiveSummaryTerms.Any(term => auditEvent.Summary.Contains(term, StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add($"{auditEvent.EventType} summary contains sensitive content.");
            }
        }

        return errors;
    }
}

public sealed record RequiredCuiAuditEvent(
    string EventType,
    string EntityType,
    AuditAction Action,
    bool RequiresResult,
    bool IsBlockedPath);

public sealed record CuiAuditEventSnapshot(
    string EventType,
    Guid TenantId,
    Guid ActorUserId,
    AuditAction Action,
    string EntityType,
    string EntityId,
    DateTimeOffset OccurredAt,
    string Summary,
    IReadOnlyDictionary<string, string> Metadata);
