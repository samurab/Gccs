using Gccs.Application.Reports;

namespace Gccs.Infrastructure.Reports;

public sealed class InMemoryEsrsApplicabilityRepository : IEsrsApplicabilityRepository
{
    private readonly List<EsrsApplicabilityDto> _records = [];

    public Task<EsrsApplicabilityDto> SaveAsync(
        EsrsApplicabilityRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var saved = new EsrsApplicabilityDto(
            Guid.NewGuid(),
            tenantId,
            request.ContractId,
            request.Agency,
            request.SubcontractingPlanType,
            request.PrimeOrLowerTierRole,
            request.ReportType,
            request.PeriodStart,
            request.PeriodEnd,
            request.DueDate,
            request.SourceClause,
            request.Rationale,
            EsrsReportTaskStatus.Open,
            request.OwnerFunction ?? "Contracts",
            now,
            null);
        _records.Add(saved);
        return Task.FromResult(saved);
    }

    public Task<EsrsApplicabilityDto?> UpdateStatusAsync(
        Guid applicabilityId,
        EsrsReportTaskStatus status,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = _records.FirstOrDefault(record => record.Id == applicabilityId);
        if (existing is null)
        {
            return Task.FromResult<EsrsApplicabilityDto?>(null);
        }

        var updated = existing with
        {
            Status = status,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _records.Remove(existing);
        _records.Add(updated);
        return Task.FromResult<EsrsApplicabilityDto?>(updated);
    }

    public Task<IReadOnlyList<EsrsCalendarItemDto>> ListCalendarItemsAsync(
        Guid tenantId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default)
    {
        var items = _records
            .Where(record => record.TenantId == tenantId)
            .Select(record => new EsrsCalendarItemDto(
                $"esrs:{record.Id}",
                record.TenantId,
                record.ContractId,
                $"{record.ReportType} eSRS report due",
                record.DueDate,
                record.ReportType,
                record.Status,
                record.OwnerFunction,
                record.DueDate < asOfDate && record.Status is not (EsrsReportTaskStatus.Completed or EsrsReportTaskStatus.Canceled)))
            .OrderBy(item => item.DueDate)
            .ToArray();
        return Task.FromResult<IReadOnlyList<EsrsCalendarItemDto>>(items);
    }
}
