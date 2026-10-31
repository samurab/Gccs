using Gccs.Domain.Tenancy;

namespace Gccs.Application.Tenancy;

public sealed record TenantDto(
    Guid Id,
    string DisplayName,
    TenantStatus Status,
    TenantDataPosture DataPosture,
    TenantDataPosture DataHandlingMode,
    DateOnly? TrialEndsAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record CreateTenantRequest(
    string DisplayName,
    TenantStatus Status = TenantStatus.Active,
    DateOnly? TrialEndsAt = null,
    TenantDataPosture? DataHandlingMode = null,
    string? DataHandlingModeReason = null,
    string? ApprovalRecordReference = null);

public sealed record UpdateTenantStatusRequest(TenantStatus Status);

public sealed record UpdateTenantDataHandlingModeRequest(
    TenantDataPosture DataHandlingMode,
    string Reason,
    string? ApprovalRecordReference = null);

public sealed record TenantDataHandlingModeHistoryDto(
    Guid Id,
    Guid TenantId,
    TenantDataPosture? PreviousMode,
    TenantDataPosture NewMode,
    Guid ActorUserId,
    DateTimeOffset ChangedAt,
    string Reason,
    string? ApprovalRecordReference);
