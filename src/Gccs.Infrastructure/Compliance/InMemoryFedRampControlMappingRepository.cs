using System.Collections.Concurrent;
using Gccs.Application.Compliance;

namespace Gccs.Infrastructure.Compliance;

public sealed class InMemoryFedRampControlMappingRepository : IFedRampControlMappingRepository
{
    private readonly ConcurrentDictionary<Guid, List<FedRampControlMappingDto>> _records = new();

    public Task<IReadOnlyList<FedRampControlMappingDto>> ListAsync(Guid tenantId, FedRampGapReportFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var records = _records.GetOrAdd(tenantId, _ => []);
        IEnumerable<FedRampControlMappingDto> query = records;
        if (filter is not null)
        {
            query = query.Where(record =>
                (!filter.Family.HasValue() || string.Equals(record.Family, filter.Family, StringComparison.OrdinalIgnoreCase)) &&
                (!filter.Severity.HasValue || record.Gaps.Any(gap => gap.IsOpen && gap.Severity == filter.Severity.Value)) &&
                (!filter.Owner.HasValue() || record.Gaps.Any(gap => gap.IsOpen && string.Equals(gap.Owner, filter.Owner, StringComparison.OrdinalIgnoreCase))) &&
                (!filter.TargetDate.HasValue || record.Gaps.Any(gap => gap.IsOpen && gap.TargetDate <= filter.TargetDate.Value)));
        }

        return Task.FromResult<IReadOnlyList<FedRampControlMappingDto>>(query.OrderBy(record => record.ControlId).ToArray());
    }

    public Task<FedRampControlMappingDto?> GetAsync(Guid tenantId, Guid mappingId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_records.GetOrAdd(tenantId, _ => []).SingleOrDefault(record => record.Id == mappingId));

    public Task<FedRampControlMappingDto> CreateAsync(Guid tenantId, CreateFedRampControlMappingRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var record = new FedRampControlMappingDto(Guid.NewGuid(), tenantId, request.ControlId.Trim(), request.Family.Trim(), request.Baseline.Trim(), request.Owner.Trim(), request.ImplementationStatus, request.ImplementationSummary.Trim(), request.InheritedProvider?.Trim(), request.EvidenceLinks, [], request.GapRationale?.Trim(), request.SourceReference.Trim(), FedRampReviewState.Draft, null, null, DateTimeOffset.UtcNow, null);
        _records.GetOrAdd(tenantId, _ => []).Add(record);
        return Task.FromResult(record);
    }

    public Task<FedRampControlMappingDto?> LinkEvidenceAsync(Guid tenantId, Guid mappingId, FedRampEvidenceLinkRequest request, Guid actorUserId, CancellationToken cancellationToken = default) =>
        UpdateAsync(tenantId, mappingId, record => record with
        {
            EvidenceLinks = record.EvidenceLinks.Append(new FedRampEvidenceLinkDto(request.Label.Trim(), request.Reference.Trim(), request.EvidenceType)).ToArray(),
            UpdatedAt = DateTimeOffset.UtcNow
        });

    public Task<FedRampControlMappingDto?> AddGapAsync(Guid tenantId, Guid mappingId, FedRampGapRequest request, Guid actorUserId, CancellationToken cancellationToken = default) =>
        UpdateAsync(tenantId, mappingId, record => record with
        {
            Gaps = record.Gaps.Append(new FedRampGapDto(request.Rationale.Trim(), request.Severity, request.Owner.Trim(), request.TargetDate, true)).ToArray(),
            ReviewState = FedRampReviewState.GapIdentified,
            UpdatedAt = DateTimeOffset.UtcNow
        });

    public Task<FedRampControlMappingDto?> ChangeStateAsync(Guid tenantId, Guid mappingId, FedRampControlReviewRequest request, Guid actorUserId, CancellationToken cancellationToken = default) =>
        UpdateAsync(tenantId, mappingId, record => record with
        {
            ReviewState = request.State,
            Reviewer = request.Reviewer.Trim(),
            ReviewDate = request.ReviewDate,
            UpdatedAt = DateTimeOffset.UtcNow
        });

    private Task<FedRampControlMappingDto?> UpdateAsync(Guid tenantId, Guid mappingId, Func<FedRampControlMappingDto, FedRampControlMappingDto> update)
    {
        var records = _records.GetOrAdd(tenantId, _ => []);
        var index = records.FindIndex(record => record.Id == mappingId);
        if (index < 0)
        {
            return Task.FromResult<FedRampControlMappingDto?>(null);
        }

        records[index] = update(records[index]);
        return Task.FromResult<FedRampControlMappingDto?>(records[index]);
    }
}

internal static class FedRampFilterExtensions
{
    public static bool HasValue(this string? value) => !string.IsNullOrWhiteSpace(value);
}
