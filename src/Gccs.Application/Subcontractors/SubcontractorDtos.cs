using Gccs.Domain.Evidence;
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

public sealed record SubcontractorFlowDownDto(
    Guid Id,
    Guid SubcontractorId,
    Guid? ContractId,
    Guid? ContractClauseId,
    string? ObligationId,
    string ClauseNumber,
    string Title,
    FlowDownStatus Status,
    DateOnly? SentAt,
    DateOnly? AcknowledgedAt,
    DateOnly? SignedAt,
    DateOnly? WaivedAt,
    Guid? SignedEvidenceItemId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record UpsertSubcontractorFlowDownRequest(
    Guid? ContractId,
    Guid? ContractClauseId,
    string? ObligationId,
    string ClauseNumber,
    string Title,
    FlowDownStatus Status,
    DateOnly? SentAt,
    DateOnly? AcknowledgedAt,
    DateOnly? SignedAt,
    DateOnly? WaivedAt,
    Guid? SignedEvidenceItemId);

public sealed record SubcontractorEvidenceRequestDto(
    Guid Id,
    Guid TenantId,
    Guid SubcontractorId,
    string RequestedItem,
    IReadOnlyList<EvidenceType> RequestedEvidenceTypes,
    DateOnly DueDate,
    SubcontractorEvidenceRequestStatus Status,
    string? RecipientName,
    string? RecipientEmail,
    string? ObligationId,
    Guid? RelatedFlowDownClauseId,
    Guid? ReceivedEvidenceItemId,
    DateTimeOffset? CompletedAt,
    bool IsOverdue,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record UpsertSubcontractorEvidenceRequestRequest(
    string RequestedItem,
    IReadOnlyList<EvidenceType> RequestedEvidenceTypes,
    DateOnly DueDate,
    SubcontractorEvidenceRequestStatus Status,
    string? RecipientName,
    string? RecipientEmail,
    string? ObligationId,
    Guid? RelatedFlowDownClauseId,
    Guid? ReceivedEvidenceItemId);

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

    Task<IReadOnlyList<SubcontractorFlowDownDto>?> ListFlowDownsAsync(
        Guid subcontractorId,
        Guid? contractId,
        CancellationToken cancellationToken = default);

    Task<SubcontractorFlowDownDto?> CreateFlowDownAsync(
        Guid subcontractorId,
        UpsertSubcontractorFlowDownRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<SubcontractorFlowDownDto?> UpdateFlowDownAsync(
        Guid subcontractorId,
        Guid flowDownId,
        UpsertSubcontractorFlowDownRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubcontractorEvidenceRequestDto>?> ListEvidenceRequestsAsync(
        Guid subcontractorId,
        CancellationToken cancellationToken = default);

    Task<SubcontractorEvidenceRequestDto?> CreateEvidenceRequestAsync(
        Guid subcontractorId,
        UpsertSubcontractorEvidenceRequestRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<SubcontractorEvidenceRequestDto?> UpdateEvidenceRequestAsync(
        Guid subcontractorId,
        Guid evidenceRequestId,
        UpsertSubcontractorEvidenceRequestRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}
