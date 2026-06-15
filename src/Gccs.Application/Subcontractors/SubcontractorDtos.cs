using Gccs.Domain.Vendors;

namespace Gccs.Application.Subcontractors;

public sealed record SubcontractorDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string? Uei,
    string? CageCode,
    SubcontractorStatus Status,
    string RoleDescription,
    string SmallBusinessStatus,
    string CmmcStatus,
    DateOnly? InsuranceExpiresAt,
    string NdaStatus,
    string WorkshareDescription,
    decimal? WorksharePercentage,
    bool HasFciAccess,
    bool HasCuiAccess,
    bool HasExportControlledAccess,
    string? RequiredCmmcLevel,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string? ContactTitle,
    IReadOnlyList<Guid> ContractIds,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record UpsertSubcontractorRequest(
    string Name,
    string? Uei,
    string? CageCode,
    SubcontractorStatus Status,
    string RoleDescription,
    string SmallBusinessStatus,
    string CmmcStatus,
    DateOnly? InsuranceExpiresAt,
    string NdaStatus,
    string WorkshareDescription,
    decimal? WorksharePercentage,
    bool HasFciAccess,
    bool HasCuiAccess,
    bool HasExportControlledAccess,
    string? RequiredCmmcLevel,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string? ContactTitle,
    IReadOnlyList<Guid> ContractIds);

public interface ISubcontractorRepository
{
    Task<IReadOnlyList<SubcontractorDto>> ListCurrentTenantAsync(CancellationToken cancellationToken = default);

    Task<SubcontractorDto?> FindCurrentTenantAsync(Guid subcontractorId, CancellationToken cancellationToken = default);

    Task<SubcontractorDto> CreateAsync(
        UpsertSubcontractorRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<SubcontractorDto?> UpdateAsync(
        Guid subcontractorId,
        UpsertSubcontractorRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}
