namespace Gccs.Application.NoCui;

public static class NoCuiNotice
{
    public const string CurrentVersion = "no-cui-mvp-v1";
    public const string Copy =
        "The GCCS MVP is compliance management only and is not ready to store CUI. Do not upload CUI, classified information, ITAR/export-controlled technical data, SSNs, payroll, bank or tax details, protected medical or disability data, passwords, secrets, private keys, unrestricted security logs, or other prohibited sensitive content.";
}

public sealed record NoCuiAcknowledgementStatusDto(
    bool IsAcknowledged,
    string NoticeVersion,
    string NoticeCopy,
    Guid TenantId,
    Guid? AcknowledgedByUserId,
    DateTimeOffset? AcknowledgedAt);

public sealed record AcknowledgeNoCuiRequest(
    bool Acknowledged,
    string NoticeVersion);

public sealed record EvidenceUploadIntentRequest(
    string FileName,
    string ContentType,
    long SizeBytes);

public sealed record EvidenceUploadIntentDto(
    Guid Id,
    Guid EvidenceItemId,
    Guid TenantId,
    Guid CreatedByUserId,
    string FileName,
    string ContentType,
    long SizeBytes,
    string Status,
    string ValidationStatus,
    string MalwareScanStatus,
    string Message,
    string NoticeVersion,
    DateTimeOffset ExpiresAt);

public static class EvidenceUploadGuardrails
{
    public const long MaxSizeBytes = 25L * 1024L * 1024L;
    public const string AcceptedValidationStatus = "accepted";
    public const string PendingMalwareScanStatus = "scan-pending";

    public static readonly IReadOnlyDictionary<string, string[]> AllowedContentTypesByExtension =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [".csv"] = ["text/csv", "application/csv", "application/vnd.ms-excel"],
            [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
            [".jpg"] = ["image/jpeg"],
            [".jpeg"] = ["image/jpeg"],
            [".pdf"] = ["application/pdf"],
            [".png"] = ["image/png"],
            [".txt"] = ["text/plain"],
            [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"]
        };

    public static string[] AllowedExtensions =>
        AllowedContentTypesByExtension.Keys.Order(StringComparer.OrdinalIgnoreCase).ToArray();
}
