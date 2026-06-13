using Gccs.Domain.Evidence;
using Gccs.Domain.Reports;

namespace Gccs.Application.Reports;

public sealed record ApprovedEvidencePackageDto(
    Guid ReportId,
    Guid TenantId,
    ReportType Type,
    string Title,
    ReportStatus Status,
    DateTimeOffset GeneratedAt,
    Guid GeneratedByUserId,
    IReadOnlyList<ApprovedEvidencePackageItemDto> EvidenceItems);

public sealed record ApprovedEvidencePackageItemDto(
    Guid EvidenceItemId,
    string Name,
    EvidenceType Type,
    EvidenceStatus Status,
    DateTimeOffset? ApprovedAt,
    Guid? ApprovedByUserId);
