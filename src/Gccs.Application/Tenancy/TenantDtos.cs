using Gccs.Domain.Tenancy;

namespace Gccs.Application.Tenancy;

public sealed record TenantDto(
    Guid Id,
    string DisplayName,
    TenantStatus Status,
    TenantDataPosture DataPosture,
    DateOnly? TrialEndsAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record CreateTenantRequest(
    string DisplayName,
    TenantStatus Status = TenantStatus.Active,
    DateOnly? TrialEndsAt = null);

public sealed record UpdateTenantStatusRequest(TenantStatus Status);
