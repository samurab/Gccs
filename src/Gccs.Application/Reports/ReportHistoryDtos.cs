using System.Text.Json;
using Gccs.Domain.Reports;

namespace Gccs.Application.Reports;

public sealed record ReportHistoryItemDto(
    Guid Id,
    Guid TenantId,
    ReportType Type,
    ReportStatus Status,
    string Title,
    DateTimeOffset GeneratedAt,
    Guid GeneratedByUserId,
    DateTimeOffset? ArchivedAt,
    Guid? ArchivedByUserId,
    string? ArchiveReason)
{
    public string Disclaimer => ReportArtifactLanguage.WorkflowGuidanceDisclaimer;
}

public sealed record ReportArtifactDetailDto(
    Guid Id,
    Guid TenantId,
    ReportType Type,
    ReportStatus Status,
    string Title,
    DateTimeOffset GeneratedAt,
    Guid GeneratedByUserId,
    JsonElement Snapshot,
    DateTimeOffset? ArchivedAt,
    Guid? ArchivedByUserId,
    string? ArchiveReason)
{
    public string Disclaimer => ReportArtifactLanguage.WorkflowGuidanceDisclaimer;
}

public sealed record ReportLifecycleRequest(string Reason);

public sealed record ReportLifecycleTransitionDto(
    ReportArtifactDetailDto Report,
    ReportStatus PreviousStatus,
    bool Changed);

public sealed class ReportLifecycleValidationException(string message) : ArgumentException(message);
