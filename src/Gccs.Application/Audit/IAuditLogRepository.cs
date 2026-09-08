namespace Gccs.Application.Audit;

public interface IAuditLogRepository
{
    async IAsyncEnumerable<AuditLogEntryDto> ReadCurrentTenantExportAsync(
        AuditLogQuery query,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var pageNumber = 1;
        PagedResultDto<AuditLogEntryDto> page;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            page = await ListCurrentTenantAsync(query with { Page = pageNumber++ }, cancellationToken);
            foreach (var item in page.Items) yield return item;
        } while (page.HasNextPage);
    }

    Task<IReadOnlyList<string>> ListEntityTypesCurrentTenantAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<AuditLogEntryDto>> ListCurrentTenantAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken = default);
}
