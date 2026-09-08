using Gccs.Domain.Audit;
using Gccs.Application.Tenancy;

namespace Gccs.Application.Audit;

public static class Phase1ACuiAuditEvents
{
    public const string ModeChange = "mode-change";
    public const string ClassificationChange = "classification-create-update";
    public const string BlockedUpload = "blocked-upload";
    public const string BlockedExtraction = "blocked-extraction";
    public const string BlockedReport = "blocked-report";
    public const string FailedModeChange = "failed-mode-change";
    public const string FailedCuiApproval = "failed-cui-approval";
    public const string ChecklistApproval = "checklist-approval";
    public const string ChecklistRejection = "checklist-rejection";
    public const string MatrixAcknowledgement = "matrix-acknowledgement";
    public const string NoticeAcknowledgement = "notice-acknowledgement";
    public const string Download = "download";
    public const string Export = "export";
    public const string Deletion = "deletion";
    public const string EscalationCreate = "escalation-create";
    public const string EscalationUpdate = "escalation-update";
    public const string ExtractionStart = "extraction-start";
    public const string ExtractionStop = "extraction-stop";

    public static readonly IReadOnlyList<RequiredCuiAuditEvent> RequiredEvents =
    [
        new(ModeChange, "Tenant", AuditAction.Updated, true, false),
        new(ClassificationChange, "ContentClassification", AuditAction.Updated, true, false),
        new(BlockedUpload, "EvidenceUploadIntent", AuditAction.Rejected, true, true),
        new(BlockedExtraction, "ContractExtractionJob", AuditAction.Rejected, true, true),
        new(BlockedReport, "Report", AuditAction.Rejected, true, true),
        new(FailedModeChange, "TenantDataHandlingMode", AuditAction.Rejected, true, true),
        new(FailedCuiApproval, "CuiReadyApprovalChecklist", AuditAction.Rejected, true, true),
        new(ChecklistApproval, "CuiReadyApprovalChecklist", AuditAction.Approved, true, false),
        new(ChecklistRejection, "CuiReadyApprovalChecklist", AuditAction.Rejected, true, true),
        new(MatrixAcknowledgement, "SharedResponsibilityMatrixAcknowledgement", AuditAction.Created, true, false),
        new(NoticeAcknowledgement, "DataHandlingNoticeAcknowledgement", AuditAction.Created, true, false),
        new(Download, "EvidenceFileVersion", AuditAction.Downloaded, true, false),
        new(Export, "EvidencePackage", AuditAction.Exported, true, false),
        new(Deletion, "EvidenceFileVersion", AuditAction.Deleted, true, false),
        new(EscalationCreate, "CuiSupportEscalation", AuditAction.Created, true, false),
        new(EscalationUpdate, "CuiSupportEscalation", AuditAction.Updated, true, false),
        new(ExtractionStart, "ContractExtractionJob", AuditAction.Created, true, false),
        new(ExtractionStop, "ContractExtractionJob", AuditAction.Updated, true, false)
    ];

    public static CuiAuditDimensions ResolveDimensions(
        AuditAction action,
        string entityType,
        IReadOnlyDictionary<string, string> metadata)
    {
        var eventType = Value(metadata, "eventType") ?? InferEventType(action, entityType, metadata);
        return new CuiAuditDimensions(
            NormalizeEventType(eventType),
            Value(metadata, "classification"),
            Value(metadata, "mode") ?? Value(metadata, "afterDataHandlingMode") ?? Value(metadata, "dataHandlingMode"),
            (Value(metadata, "result") ?? (action == AuditAction.Rejected ? "rejected" : "succeeded")).Trim().ToLowerInvariant());
    }

    private static string InferEventType(AuditAction action, string entityType, IReadOnlyDictionary<string, string> metadata)
    {
        if (entityType == "Tenant" && action == AuditAction.Updated && metadata.ContainsKey("afterDataHandlingMode")) return ModeChange;
        if ((entityType == "ContentClassification" || metadata.ContainsKey("classification")) && action == AuditAction.Updated) return ClassificationChange;
        if (entityType == "EvidenceUploadIntent" && action == AuditAction.Rejected) return BlockedUpload;
        if (entityType == "TenantDataHandlingMode" && action == AuditAction.Rejected) return FailedModeChange;
        if (entityType == "CuiReadyApprovalChecklist" && action == AuditAction.Approved) return ChecklistApproval;
        if (entityType == "CuiReadyApprovalChecklist" && action == AuditAction.Rejected)
            return Value(metadata, "operation") is "approve" or "gate-evaluation" ? FailedCuiApproval : ChecklistRejection;
        if (entityType == "SharedResponsibilityMatrixAcknowledgement" && action == AuditAction.Created) return MatrixAcknowledgement;
        if (entityType == "DataHandlingNoticeAcknowledgement" && action is AuditAction.Created or AuditAction.Updated) return NoticeAcknowledgement;
        if (entityType == "EvidenceFileVersion" && action == AuditAction.Downloaded) return Download;
        if (action == AuditAction.Exported) return Export;
        if (entityType == "EvidenceFileVersion" && action == AuditAction.Deleted) return Deletion;
        if (entityType == "CuiSupportEscalation") return action == AuditAction.Created ? EscalationCreate : EscalationUpdate;
        if (entityType is "ExtractionJob" or "ContractExtractionJob")
            return action == AuditAction.Created ? ExtractionStart : action == AuditAction.Rejected ? BlockedExtraction : ExtractionStop;
        if (entityType == "TenantDataHandlingModePolicy" && action == AuditAction.Rejected)
            return Value(metadata, "workflow") switch
            {
                "EvidenceUpload" => BlockedUpload,
                "ExtractionJob" => BlockedExtraction,
                "Report" => BlockedReport,
                _ => FailedModeChange
            };
        return $"{entityType}-{action}";
    }

    private static string? Value(IReadOnlyDictionary<string, string> metadata, string key) =>
        metadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    public static string NormalizeEventType(string value)
    {
        var normalized = new string(value.Trim().Select(character => char.IsLetterOrDigit(character)
            ? char.ToLowerInvariant(character) : '-').ToArray());
        while (normalized.Contains("--", StringComparison.Ordinal)) normalized = normalized.Replace("--", "-", StringComparison.Ordinal);
        return normalized.Trim('-');
    }

    public static string BlockedEventType(TenantDataHandlingWorkflow workflow) => workflow switch
    {
        TenantDataHandlingWorkflow.EvidenceUpload or TenantDataHandlingWorkflow.ContractDocumentUpload => BlockedUpload,
        TenantDataHandlingWorkflow.ExtractionJob => BlockedExtraction,
        TenantDataHandlingWorkflow.Report => BlockedReport,
        _ => FailedModeChange
    };

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

public sealed record CuiAuditDimensions(string EventType, string? Classification, string? Mode, string Result);

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
