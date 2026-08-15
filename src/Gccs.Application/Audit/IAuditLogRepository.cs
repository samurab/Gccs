namespace Gccs.Application.Audit;

public interface IAuditLogRepository
{
    Task<IReadOnlyList<string>> ListEntityTypesCurrentTenantAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<AuditLogEntryDto>> ListCurrentTenantAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken = default);
}
