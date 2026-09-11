using Gccs.Application.Reports;

namespace Gccs.Infrastructure.Reports;

public sealed class InMemorySubcontractingReportDataRepository(Guid tenantId) : ISubcontractingReportDataRepository
{
    private readonly object gate = new();
    private readonly List<SubcontractingReportDataRowDto> rows = [];

    public Task<SubcontractingReportDataRowDto> CreateAsync(SubcontractingReportDataRowRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var row = new SubcontractingReportDataRowDto(Guid.NewGuid(), tenantId, request.ContractId, request.SubcontractorId,
                request.ReportType, request.ReportPeriodStart, request.ReportPeriodEnd, request.RowPeriodStart, request.RowPeriodEnd,
                request.SocioeconomicCategory, request.PlanCategory, request.Amount, request.SupportingEvidenceItemIds.ToArray(),
                request.SourceReference, SubcontractingReportDataReviewStatus.Draft, null, null, null, 1, DateTimeOffset.UtcNow, null,
                request.ReportingRole, request.ReportingFiscalYear, request.ReportingPeriod, request.ReportingEntityUei,
                request.PrimeContractPiid, request.SubcontractNumber, request.SprEligibilityConfirmed, request.SprEligibilityBasis);
            rows.Add(row); return Task.FromResult(row);
        }
    }

    public Task<SubcontractingReportDataRowDto?> UpdateAsync(Guid rowId, SubcontractingReportDataRowRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var existing = rows.SingleOrDefault(row => row.Id == rowId && row.TenantId == tenantId);
            if (existing is null) return Task.FromResult<SubcontractingReportDataRowDto?>(null);
            var updated = existing with { ContractId = request.ContractId, SubcontractorId = request.SubcontractorId,
                ReportType = request.ReportType, ReportPeriodStart = request.ReportPeriodStart, ReportPeriodEnd = request.ReportPeriodEnd,
                RowPeriodStart = request.RowPeriodStart, RowPeriodEnd = request.RowPeriodEnd,
                SocioeconomicCategory = request.SocioeconomicCategory, PlanCategory = request.PlanCategory, Amount = request.Amount,
                SupportingEvidenceItemIds = request.SupportingEvidenceItemIds.ToArray(), SourceReference = request.SourceReference,
                ReportingRole = request.ReportingRole, ReportingFiscalYear = request.ReportingFiscalYear,
                ReportingPeriod = request.ReportingPeriod, ReportingEntityUei = request.ReportingEntityUei,
                PrimeContractPiid = request.PrimeContractPiid, SubcontractNumber = request.SubcontractNumber,
                SprEligibilityConfirmed = request.SprEligibilityConfirmed, SprEligibilityBasis = request.SprEligibilityBasis,
                ReviewStatus = SubcontractingReportDataReviewStatus.PendingReview, ReviewedByUserId = null, ReviewedAt = null,
                ReviewerNotes = null, Version = existing.Version + 1, UpdatedAt = DateTimeOffset.UtcNow };
            Replace(existing, updated); return Task.FromResult<SubcontractingReportDataRowDto?>(updated);
        }
    }

    public Task<SubcontractingReportDataRowDto?> FindCurrentTenantAsync(Guid rowId, CancellationToken cancellationToken = default)
    { lock (gate) return Task.FromResult(rows.SingleOrDefault(row => row.Id == rowId && row.TenantId == tenantId)); }

    public Task<IReadOnlyList<SubcontractingReportDataRowDto>> ListCurrentTenantAsync(SubcontractingReportDataQuery query, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var result = rows.Where(row => row.TenantId == tenantId)
                .Where(row => query.ContractId is null || row.ContractId == query.ContractId)
                .Where(row => query.ReportType is null || row.ReportType == query.ReportType)
                .Where(row => query.ReportPeriodStart is null || row.ReportPeriodStart == query.ReportPeriodStart)
                .Where(row => query.ReportPeriodEnd is null || row.ReportPeriodEnd == query.ReportPeriodEnd).ToArray();
            return Task.FromResult<IReadOnlyList<SubcontractingReportDataRowDto>>(result);
        }
    }

    public Task<SubcontractingReportDataRowDto?> UpdateReviewStatusAsync(Guid rowId, SubcontractingReportDataReviewStatus status, string? reviewerNotes, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var existing = rows.SingleOrDefault(row => row.Id == rowId && row.TenantId == tenantId);
            if (existing is null) return Task.FromResult<SubcontractingReportDataRowDto?>(null);
            var now = DateTimeOffset.UtcNow;
            var updated = existing with { ReviewStatus = status, ReviewerNotes = reviewerNotes, ReviewedByUserId = actorUserId,
                ReviewedAt = now, Version = existing.Version + 1, UpdatedAt = now };
            Replace(existing, updated); return Task.FromResult<SubcontractingReportDataRowDto?>(updated);
        }
    }

    public Task<bool> ExistsDuplicateCurrentTenantAsync(SubcontractingReportDataRowRequest request, Guid? existingRowId, CancellationToken cancellationToken = default)
    {
        lock (gate) return Task.FromResult(rows.Any(row => row.TenantId == tenantId && row.Id != existingRowId &&
            row.ContractId == request.ContractId && row.SubcontractorId == request.SubcontractorId && row.ReportType == request.ReportType &&
            row.ReportPeriodStart == request.ReportPeriodStart && row.ReportPeriodEnd == request.ReportPeriodEnd &&
            row.RowPeriodStart == request.RowPeriodStart && row.RowPeriodEnd == request.RowPeriodEnd &&
            string.Equals(row.SocioeconomicCategory, request.SocioeconomicCategory, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(row.PlanCategory, request.PlanCategory, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<IReadOnlyDictionary<string, string[]>> ValidateReferencesCurrentTenantAsync(SubcontractingReportDataRowRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<string, string[]>>(new Dictionary<string, string[]>());

    public Task<bool> ContractExistsCurrentTenantAsync(Guid contractId, CancellationToken cancellationToken = default) => Task.FromResult(true);

    private void Replace(SubcontractingReportDataRowDto existing, SubcontractingReportDataRowDto updated) { rows.Remove(existing); rows.Add(updated); }
}
