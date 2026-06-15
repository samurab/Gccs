using Gccs.Domain.Evidence;

namespace Gccs.Application.Evidence;

public sealed record EvidenceMetadataDto(
    Guid Id,
    Guid TenantId,
    string Title,
    EvidenceType Type,
    string OwnerFunction,
    EvidenceStatus Status,
    DateOnly? EffectiveAt,
    DateOnly? ExpiresAt,
    IReadOnlyList<string> Tags,
    string Description,
    IReadOnlyList<string> ObligationIds,
    IReadOnlyList<string> ControlIds,
    IReadOnlyList<Guid> ContractIds,
    IReadOnlyList<Guid> VendorIds,
    IReadOnlyList<Guid> SubcontractorIds,
    IReadOnlyList<Guid> EmployeeIds,
    IReadOnlyList<Guid> ReportIds,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record UpsertEvidenceMetadataRequest(
    string Title,
    EvidenceType Type,
    string OwnerFunction,
    EvidenceStatus Status,
    DateOnly? EffectiveAt,
    DateOnly? ExpiresAt,
    IReadOnlyList<string> Tags,
    string Description,
    IReadOnlyList<string> ObligationIds,
    IReadOnlyList<string> ControlIds,
    IReadOnlyList<Guid> ContractIds,
    IReadOnlyList<Guid> VendorIds,
    IReadOnlyList<Guid> SubcontractorIds,
    IReadOnlyList<Guid> EmployeeIds,
    IReadOnlyList<Guid> ReportIds);

public sealed record EvidenceMetadataQuery(string? Tag);

public interface IEvidenceMetadataRepository
{
    Task<IReadOnlyList<EvidenceMetadataDto>> ListCurrentTenantAsync(
        EvidenceMetadataQuery query,
        CancellationToken cancellationToken = default);

    Task<EvidenceMetadataDto?> FindCurrentTenantAsync(
        Guid evidenceItemId,
        CancellationToken cancellationToken = default);

    Task<EvidenceMetadataDto> CreateCurrentTenantAsync(
        UpsertEvidenceMetadataRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<EvidenceMetadataDto?> UpdateCurrentTenantAsync(
        Guid evidenceItemId,
        UpsertEvidenceMetadataRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}
