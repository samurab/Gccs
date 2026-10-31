using Gccs.Domain.Tenancy;

namespace Gccs.Application.Tenancy;

public interface ITenantRepository
{
    Task<Tenant?> FindInCurrentTenantScopeAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<TenantDataPosture?> FindCurrentTenantDataHandlingModeAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TenantDataHandlingModeHistoryDto>> ListDataHandlingModeHistoryInCurrentTenantScopeAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default);

    Task AddDataHandlingModeHistoryAsync(
        Guid tenantId,
        TenantDataPosture? previousMode,
        TenantDataPosture newMode,
        Guid actorUserId,
        string reason,
        string? approvalRecordReference,
        CancellationToken cancellationToken = default);

    Task<Tenant?> UpdateStatusInCurrentTenantScopeAsync(Guid tenantId, TenantStatus status, CancellationToken cancellationToken = default);

    Task<Tenant?> UpdateDataHandlingModeInCurrentTenantScopeAsync(
        Guid tenantId,
        TenantDataPosture dataHandlingMode,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}
