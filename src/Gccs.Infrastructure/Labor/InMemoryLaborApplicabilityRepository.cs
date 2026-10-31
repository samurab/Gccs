using Gccs.Application.Labor;

namespace Gccs.Infrastructure.Labor;

// Retained for isolated Story 32.2/32.3 unit tests. Runtime DI uses EfLaborApplicabilityRepository.
public sealed class InMemoryLaborApplicabilityRepository : ILaborApplicabilityRepository
{
    private readonly List<LaborApplicabilityDto> records = [];

    public Task<IReadOnlyList<LaborApplicabilityDto>?> ListForContractAsync(Guid contractId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<LaborApplicabilityDto>?>(records.Where(x => x.ContractId == contractId).ToArray());

    public Task<LaborApplicabilityDto?> CreateAsync(LaborApplicabilityRequest request, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var record = new LaborApplicabilityDto(Guid.NewGuid(), tenantId, request.ContractId, null,
            request.ScaApplicable, request.DbaApplicable, request.OtherFarPart22Obligations, request.PlaceOfPerformance,
            request.ContractPeriodStart, request.ContractPeriodEnd, request.WageDeterminationReference,
            request.WageDeterminationEvidenceItemId, request.SourceContractClauseId, request.SourceClause, request.Rationale,
            request.OwnerFunction ?? "Contracts/HR", LaborApplicabilityStatus.Draft, request.ReviewStatus,
            request.ReviewNotes, IsReviewed(request.ReviewStatus) ? actorUserId : null,
            IsReviewed(request.ReviewStatus) ? now : null, null, now, null);
        records.Add(record);
        return Task.FromResult<LaborApplicabilityDto?>(record);
    }

    public Task<LaborApplicabilityDto?> UpdateAsync(Guid contractId, Guid applicabilityId, LaborApplicabilityRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var existing = records.SingleOrDefault(x => x.Id == applicabilityId && x.ContractId == contractId);
        if (existing is null) return Task.FromResult<LaborApplicabilityDto?>(null);
        var now = DateTimeOffset.UtcNow;
        var updated = existing with
        {
            ScaApplicable = request.ScaApplicable, DbaApplicable = request.DbaApplicable,
            OtherFarPart22Obligations = request.OtherFarPart22Obligations, PlaceOfPerformance = request.PlaceOfPerformance,
            ContractPeriodStart = request.ContractPeriodStart, ContractPeriodEnd = request.ContractPeriodEnd,
            WageDeterminationReference = request.WageDeterminationReference,
            WageDeterminationEvidenceItemId = request.WageDeterminationEvidenceItemId,
            SourceContractClauseId = request.SourceContractClauseId, SourceClause = request.SourceClause,
            Rationale = request.Rationale, OwnerFunction = request.OwnerFunction ?? "Contracts/HR",
            ReviewStatus = request.ReviewStatus, ReviewNotes = request.ReviewNotes,
            ReviewedByUserId = IsReviewed(request.ReviewStatus) ? actorUserId : null,
            ReviewedAt = IsReviewed(request.ReviewStatus) ? now : null, UpdatedAt = now
        };
        Replace(existing, updated);
        return Task.FromResult<LaborApplicabilityDto?>(updated);
    }

    public Task<LaborApplicabilityDto?> FindAsync(Guid contractId, Guid applicabilityId, CancellationToken cancellationToken = default) =>
        Task.FromResult(records.SingleOrDefault(x => x.Id == applicabilityId && x.ContractId == contractId));

    public Task<IReadOnlyList<LaborApplicabilityDto>> ListAsync(Guid tenantId, Guid? contractId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<LaborApplicabilityDto>>(records.Where(x => x.TenantId == tenantId).Where(x => contractId is null || x.ContractId == contractId).ToArray());

    public Task<LaborApplicabilityDto?> UpdateStatusAsync(Guid contractId, Guid applicabilityId, LaborApplicabilityStatus status, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var existing = records.SingleOrDefault(x => x.Id == applicabilityId && x.ContractId == contractId);
        if (existing is null) return Task.FromResult<LaborApplicabilityDto?>(null);
        var task = existing.ReviewTask;
        if (status == LaborApplicabilityStatus.Active)
            task = new LaborReviewTaskDto(task?.Id ?? Guid.NewGuid(), existing.TenantId, contractId,
                "Review labor applicability and wage determination",
                "Confirm SCA/DBA/FAR Part 22 labor obligations, wage determination reference, and place of performance.",
                "WaitingForReview", existing.ContractPeriodEnd);
        else if (task is not null)
            task = task with { Status = "Canceled" };
        var updated = existing with { TaskId = task?.Id, ReviewTask = task, Status = status, UpdatedAt = DateTimeOffset.UtcNow };
        Replace(existing, updated);
        return Task.FromResult<LaborApplicabilityDto?>(updated);
    }

    private static bool IsReviewed(LaborApplicabilityReviewStatus status) => status is LaborApplicabilityReviewStatus.Reviewed or LaborApplicabilityReviewStatus.Rejected;
    private void Replace(LaborApplicabilityDto existing, LaborApplicabilityDto updated) { records.Remove(existing); records.Add(updated); }
}
