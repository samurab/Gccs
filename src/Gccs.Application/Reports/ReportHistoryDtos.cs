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
    Guid GeneratedByUserId)
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
    JsonElement Snapshot)
{
    public string Disclaimer => ReportArtifactLanguage.WorkflowGuidanceDisclaimer;
}
