using Gccs.Domain.Evidence;
using Gccs.Domain.Reports;

namespace Gccs.Application.Reports;

public sealed record EvidencePackageGenerateRequest
{
    public string Title { get; init; } = string.Empty;
    public IReadOnlyList<string> ObligationIds { get; init; } = [];
    public IReadOnlyList<Guid> ContractIds { get; init; } = [];
    public IReadOnlyList<string> ControlIds { get; init; } = [];
    public IReadOnlyList<Guid> SubcontractorIds { get; init; } = [];
    public bool IncludeDraftOrRejectedEvidence { get; init; }
}

public sealed record EvidencePackageReportDto(
    Guid Id,
    Guid TenantId,
    ReportType Type,
    ReportStatus Status,
    string Title,
    DateTimeOffset GeneratedAt,
    Guid GeneratedByUserId,
    EvidencePackageManifestDto Manifest,
    string ExportHtml);

public sealed record EvidencePackageManifestDto(
    string Title,
    DateTimeOffset GeneratedAt,
    EvidencePackageScopeDto Scope,
    IReadOnlyList<EvidencePackageManifestItemDto> Items);

public sealed record EvidencePackageScopeDto(
    IReadOnlyList<string> ObligationIds,
    IReadOnlyList<Guid> ContractIds,
    IReadOnlyList<string> ControlIds,
    IReadOnlyList<Guid> SubcontractorIds,
    bool IncludesDraftOrRejectedEvidence);

public sealed record EvidencePackageManifestItemDto(
    Guid EvidenceItemId,
    string Title,
    EvidenceType Type,
    EvidenceStatus Status,
    DateTimeOffset? ApprovedAt,
    Guid? ApprovedByUserId,
    IReadOnlyList<string> ObligationIds,
    IReadOnlyList<Guid> ContractIds,
    IReadOnlyList<string> ControlIds,
    IReadOnlyList<Guid> SubcontractorIds,
    DateTimeOffset ManifestedAt);
