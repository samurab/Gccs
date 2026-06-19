using Gccs.Application.Reports;

namespace Gccs.Infrastructure.Reports;

public sealed class InMemorySubcontractingReportDataRepository : ISubcontractingReportDataRepository
{
    private readonly List<SubcontractingReportDataRowDto> _rows = [];

    public Task<SubcontractingReportDataRowDto> CreateAsync(
        SubcontractingReportDataRowRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var row = new SubcontractingReportDataRowDto(
            Guid.NewGuid(),
            tenantId,
            request.ContractId,
            request.SubcontractorId,
            request.ReportType,
            request.ReportPeriodStart,
            request.ReportPeriodEnd,
            request.RowPeriodStart,
            request.RowPeriodEnd,
            request.SocioeconomicCategory,
            request.PlanCategory,
            request.Amount,
            request.SupportingEvidenceItemIds.ToArray(),
            request.SourceReference,
            SubcontractingReportDataReviewStatus.Draft,
            null,
            DateTimeOffset.UtcNow,
            null);
        _rows.Add(row);
        return Task.FromResult(row);
    }

    public Task<SubcontractingReportDataRowDto?> UpdateAsync(
        Guid rowId,
        SubcontractingReportDataRowRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = _rows.SingleOrDefault(row => row.Id == rowId);
        if (existing is null)
        {
            return Task.FromResult<SubcontractingReportDataRowDto?>(null);
        }

        var updated = existing with
        {
            ContractId = request.ContractId,
            SubcontractorId = request.SubcontractorId,
            ReportType = request.ReportType,
            ReportPeriodStart = request.ReportPeriodStart,
            ReportPeriodEnd = request.ReportPeriodEnd,
            RowPeriodStart = request.RowPeriodStart,
            RowPeriodEnd = request.RowPeriodEnd,
            SocioeconomicCategory = request.SocioeconomicCategory,
            PlanCategory = request.PlanCategory,
            Amount = request.Amount,
            SupportingEvidenceItemIds = request.SupportingEvidenceItemIds.ToArray(),
            SourceReference = request.SourceReference,
            ReviewStatus = SubcontractingReportDataReviewStatus.PendingReview,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        Replace(existing, updated);
        return Task.FromResult<SubcontractingReportDataRowDto?>(updated);
    }

    public Task<SubcontractingReportDataRowDto?> FindAsync(Guid rowId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_rows.SingleOrDefault(row => row.Id == rowId));

    public Task<IReadOnlyList<SubcontractingReportDataRowDto>> ListAsync(
        SubcontractingReportDataQuery query,
        CancellationToken cancellationToken = default)
    {
        var rows = _rows
            .Where(row => row.TenantId == query.TenantId)
            .Where(row => query.ContractId is null || row.ContractId == query.ContractId)
            .Where(row => query.ReportType is null || row.ReportType == query.ReportType)
            .Where(row => query.ReportPeriodStart is null || row.ReportPeriodStart == query.ReportPeriodStart)
            .Where(row => query.ReportPeriodEnd is null || row.ReportPeriodEnd == query.ReportPeriodEnd)
            .OrderBy(row => row.SubcontractorId)
            .ThenBy(row => row.SocioeconomicCategory)
            .ToArray();
        return Task.FromResult<IReadOnlyList<SubcontractingReportDataRowDto>>(rows);
    }

    public Task<SubcontractingReportDataRowDto?> UpdateReviewStatusAsync(
        Guid rowId,
        SubcontractingReportDataReviewStatus status,
        string? reviewerNotes,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = _rows.SingleOrDefault(row => row.Id == rowId);
        if (existing is null)
        {
            return Task.FromResult<SubcontractingReportDataRowDto?>(null);
        }

        var updated = existing with
        {
            ReviewStatus = status,
            ReviewerNotes = reviewerNotes,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        Replace(existing, updated);
        return Task.FromResult<SubcontractingReportDataRowDto?>(updated);
    }

    public Task<bool> ExistsDuplicateAsync(
        Guid tenantId,
        SubcontractingReportDataRowRequest request,
        Guid? existingRowId,
        CancellationToken cancellationToken = default)
    {
        var exists = _rows.Any(row =>
            row.TenantId == tenantId &&
            row.Id != existingRowId &&
            row.ContractId == request.ContractId &&
            row.SubcontractorId == request.SubcontractorId &&
            row.ReportType == request.ReportType &&
            row.ReportPeriodStart == request.ReportPeriodStart &&
            row.ReportPeriodEnd == request.ReportPeriodEnd &&
            row.RowPeriodStart == request.RowPeriodStart &&
            row.RowPeriodEnd == request.RowPeriodEnd &&
            string.Equals(row.SocioeconomicCategory, request.SocioeconomicCategory, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(row.PlanCategory, request.PlanCategory, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(exists);
    }

    private void Replace(SubcontractingReportDataRowDto existing, SubcontractingReportDataRowDto updated)
    {
        _rows.Remove(existing);
        _rows.Add(updated);
    }
}
