namespace Gccs.Application.Audit;

public interface IAuditLogRepository
{
    Task<PagedResultDto<AuditLogEntryDto>> ListCurrentTenantAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken = default);
}
