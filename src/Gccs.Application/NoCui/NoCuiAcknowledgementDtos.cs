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
    string FileName);

public sealed record EvidenceUploadIntentDto(
    Guid Id,
    Guid EvidenceItemId,
    Guid TenantId,
    Guid CreatedByUserId,
    string Status,
    string Message,
    string NoticeVersion,
    DateTimeOffset ExpiresAt);

