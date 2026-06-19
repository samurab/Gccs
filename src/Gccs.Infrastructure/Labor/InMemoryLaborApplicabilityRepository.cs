using Gccs.Application.Labor;

namespace Gccs.Infrastructure.Labor;

public sealed class InMemoryLaborApplicabilityRepository : ILaborApplicabilityRepository
{
    private readonly List<LaborApplicabilityDto> _records = [];

    public Task<LaborApplicabilityDto> SaveAsync(
        LaborApplicabilityRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var record = new LaborApplicabilityDto(
            Guid.NewGuid(),
            tenantId,
            request.ContractId,
            request.LaborStandard,
            request.PlaceOfPerformance,
            request.ContractPeriodStart,
            request.ContractPeriodEnd,
            request.WageDeterminationReference,
            request.WageDeterminationEvidenceItemId,
            request.SourceClause,
            request.Rationale,
            request.OwnerFunction ?? "Contracts/HR",
            LaborApplicabilityStatus.Draft,
            null,
            DateTimeOffset.UtcNow,
            null);
        _records.Add(record);
        return Task.FromResult(record);
    }

    public Task<LaborApplicabilityDto?> UpdateAsync(
        Guid applicabilityId,
        LaborApplicabilityRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = _records.SingleOrDefault(record => record.Id == applicabilityId);
        if (existing is null)
        {
            return Task.FromResult<LaborApplicabilityDto?>(null);
        }

        var updated = existing with
        {
            ContractId = request.ContractId,
            LaborStandard = request.LaborStandard,
            PlaceOfPerformance = request.PlaceOfPerformance,
            ContractPeriodStart = request.ContractPeriodStart,
            ContractPeriodEnd = request.ContractPeriodEnd,
            WageDeterminationReference = request.WageDeterminationReference,
            WageDeterminationEvidenceItemId = request.WageDeterminationEvidenceItemId,
            SourceClause = request.SourceClause,
            Rationale = request.Rationale,
            OwnerFunction = request.OwnerFunction ?? "Contracts/HR",
            UpdatedAt = DateTimeOffset.UtcNow
        };
        Replace(existing, updated);
        return Task.FromResult<LaborApplicabilityDto?>(updated);
    }

    public Task<LaborApplicabilityDto?> FindAsync(Guid applicabilityId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_records.SingleOrDefault(record => record.Id == applicabilityId));

    public Task<LaborApplicabilityDto?> UpdateStatusAsync(
        Guid applicabilityId,
        LaborApplicabilityStatus status,
        LaborReviewTaskDto? reviewTask,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = _records.SingleOrDefault(record => record.Id == applicabilityId);
        if (existing is null)
        {
            return Task.FromResult<LaborApplicabilityDto?>(null);
        }

        var task = reviewTask ?? existing.ReviewTask;
        if (status == LaborApplicabilityStatus.Active && existing.ReviewTask is not null && reviewTask is not null)
        {
            task = reviewTask with { Id = existing.ReviewTask.Id };
        }

        var updated = existing with
        {
            Status = status,
            ReviewTask = task,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        Replace(existing, updated);
        return Task.FromResult<LaborApplicabilityDto?>(updated);
    }

    private void Replace(LaborApplicabilityDto existing, LaborApplicabilityDto updated)
    {
        _records.Remove(existing);
        _records.Add(updated);
    }
}
