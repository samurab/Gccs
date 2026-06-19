using Gccs.Application.Audit;
using Gccs.Domain.Audit;

namespace Gccs.Application.Reports;

public sealed class SubcontractingReportDataService(
    ISubcontractingReportDataRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public async Task<SubcontractingReportDataRowDto> CreateAsync(
        SubcontractingReportDataRowRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        await ValidateAsync(normalized, tenantId, null, cancellationToken);
        var row = await repository.CreateAsync(normalized, tenantId, actorUserId, cancellationToken);
        await WriteAuditAsync(row, actorUserId, AuditAction.Created, "Subcontracting report data row was created.", cancellationToken);
        return row;
    }

    public async Task<SubcontractingReportDataRowDto?> UpdateAsync(
        Guid rowId,
        SubcontractingReportDataRowRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = await repository.FindAsync(rowId, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var normalized = Normalize(request);
        await ValidateAsync(normalized, existing.TenantId, rowId, cancellationToken);
        var updated = await repository.UpdateAsync(rowId, normalized, actorUserId, cancellationToken);
        if (updated is not null)
        {
            await WriteAuditAsync(updated, actorUserId, AuditAction.Updated, "Subcontracting report data row was updated.", cancellationToken);
        }

        return updated;
    }

    public Task<SubcontractingReportDataRowDto?> FindAsync(Guid rowId, CancellationToken cancellationToken = default) =>
        repository.FindAsync(rowId, cancellationToken);

    public Task<IReadOnlyList<SubcontractingReportDataRowDto>> ListAsync(
        SubcontractingReportDataQuery query,
        CancellationToken cancellationToken = default) =>
        repository.ListAsync(query, cancellationToken);

    public async Task<SubcontractingReportDataRowDto?> UpdateReviewStatusAsync(
        Guid rowId,
        SubcontractingReportDataReviewStatus status,
        Guid actorUserId,
        string? reviewerNotes = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedNotes = string.IsNullOrWhiteSpace(reviewerNotes) ? null : reviewerNotes.Trim();
        var updated = await repository.UpdateReviewStatusAsync(rowId, status, normalizedNotes, actorUserId, cancellationToken);
        if (updated is not null)
        {
            await WriteAuditAsync(updated, actorUserId, AuditAction.Updated, $"Subcontracting report data row was {status}.", cancellationToken);
        }

        return updated;
    }

    public async Task<IReadOnlyList<SubcontractingReportPackageRowDto>> PreparePackageRowsAsync(
        SubcontractingReportPackageRowsRequest request,
        CancellationToken cancellationToken = default)
    {
        var rows = await repository.ListAsync(
            new SubcontractingReportDataQuery(
                request.TenantId,
                request.ContractId,
                request.ReportType,
                request.PeriodStart,
                request.PeriodEnd),
            cancellationToken);
        var blocked = rows
            .Where(row => row.ReviewStatus is not (SubcontractingReportDataReviewStatus.Reviewed or SubcontractingReportDataReviewStatus.Accepted))
            .ToArray();
        if (request.FinalPackage && blocked.Length > 0)
        {
            throw new SubcontractingReportDataValidationException("Final eSRS packages can include only reviewed or accepted subcontracting report data rows.");
        }

        return rows
            .Where(row => row.ReviewStatus is SubcontractingReportDataReviewStatus.Reviewed or SubcontractingReportDataReviewStatus.Accepted)
            .Select(row => new SubcontractingReportPackageRowDto(
                row.Id,
                row.ContractId,
                row.SubcontractorId,
                row.SocioeconomicCategory,
                row.PlanCategory,
                row.ReportType,
                row.RowPeriodStart,
                row.RowPeriodEnd,
                row.Amount,
                row.SupportingEvidenceItemIds,
                row.ReviewStatus))
            .ToArray();
    }

    public static SubcontractingReportDataImportTemplateDto GetImportTemplate() =>
        new(
            "subcontracting-report-data-template.csv",
            [
                "contractId",
                "subcontractorId",
                "reportType",
                "reportPeriodStart",
                "reportPeriodEnd",
                "rowPeriodStart",
                "rowPeriodEnd",
                "socioeconomicCategory",
                "planCategory",
                "amount",
                "supportingEvidenceItemIds"
            ]);

    private async Task ValidateAsync(
        SubcontractingReportDataRowRequest request,
        Guid tenantId,
        Guid? existingRowId,
        CancellationToken cancellationToken)
    {
        if (request.ContractId == Guid.Empty)
        {
            throw new SubcontractingReportDataValidationException("Contract is required.");
        }

        if (request.SubcontractorId == Guid.Empty)
        {
            throw new SubcontractingReportDataValidationException("Subcontractor is required.");
        }

        if (string.IsNullOrWhiteSpace(request.SocioeconomicCategory))
        {
            throw new SubcontractingReportDataValidationException("Socioeconomic category is required.");
        }

        if (string.IsNullOrWhiteSpace(request.PlanCategory))
        {
            throw new SubcontractingReportDataValidationException("Plan category is required.");
        }

        if (request.Amount < 0)
        {
            throw new SubcontractingReportDataValidationException("Amount cannot be negative.");
        }

        if (request.ReportPeriodEnd < request.ReportPeriodStart || request.RowPeriodEnd < request.RowPeriodStart)
        {
            throw new SubcontractingReportDataValidationException("Report and row periods must have valid start and end dates.");
        }

        if (request.RowPeriodStart < request.ReportPeriodStart || request.RowPeriodEnd > request.ReportPeriodEnd)
        {
            throw new SubcontractingReportDataValidationException("Row period must fall within the report period.");
        }

        if (await repository.ExistsDuplicateAsync(tenantId, request, existingRowId, cancellationToken))
        {
            throw new SubcontractingReportDataValidationException("Duplicate subcontracting report data row.");
        }
    }

    private static SubcontractingReportDataRowRequest Normalize(SubcontractingReportDataRowRequest request) =>
        request with
        {
            SocioeconomicCategory = request.SocioeconomicCategory.Trim(),
            PlanCategory = request.PlanCategory.Trim(),
            SourceReference = string.IsNullOrWhiteSpace(request.SourceReference) ? null : request.SourceReference.Trim(),
            SupportingEvidenceItemIds = request.SupportingEvidenceItemIds.Distinct().ToArray()
        };

    private async Task WriteAuditAsync(
        SubcontractingReportDataRowDto row,
        Guid actorUserId,
        AuditAction action,
        string summary,
        CancellationToken cancellationToken)
    {
        await auditEventWriter.WriteAsync(
            row.TenantId,
            actorUserId,
            action,
            "SubcontractingReportDataRow",
            row.Id.ToString(),
            summary,
            new Dictionary<string, string>
            {
                ["contractId"] = row.ContractId.ToString(),
                ["subcontractorId"] = row.SubcontractorId.ToString(),
                ["reportType"] = row.ReportType.ToString(),
                ["socioeconomicCategory"] = row.SocioeconomicCategory,
                ["planCategory"] = row.PlanCategory,
                ["amount"] = row.Amount.ToString("0.00"),
                ["reviewStatus"] = row.ReviewStatus.ToString(),
                ["evidenceCount"] = row.SupportingEvidenceItemIds.Count.ToString()
            },
            cancellationToken);
    }
}

public interface ISubcontractingReportDataRepository
{
    Task<SubcontractingReportDataRowDto> CreateAsync(
        SubcontractingReportDataRowRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<SubcontractingReportDataRowDto?> UpdateAsync(
        Guid rowId,
        SubcontractingReportDataRowRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<SubcontractingReportDataRowDto?> FindAsync(Guid rowId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubcontractingReportDataRowDto>> ListAsync(
        SubcontractingReportDataQuery query,
        CancellationToken cancellationToken = default);

    Task<SubcontractingReportDataRowDto?> UpdateReviewStatusAsync(
        Guid rowId,
        SubcontractingReportDataReviewStatus status,
        string? reviewerNotes,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsDuplicateAsync(
        Guid tenantId,
        SubcontractingReportDataRowRequest request,
        Guid? existingRowId,
        CancellationToken cancellationToken = default);
}

public sealed record SubcontractingReportDataRowRequest(
    Guid ContractId,
    Guid SubcontractorId,
    EsrsReportType ReportType,
    DateOnly ReportPeriodStart,
    DateOnly ReportPeriodEnd,
    DateOnly RowPeriodStart,
    DateOnly RowPeriodEnd,
    string SocioeconomicCategory,
    string PlanCategory,
    decimal Amount,
    IReadOnlyList<Guid> SupportingEvidenceItemIds,
    string? SourceReference);

public sealed record SubcontractingReportDataRowDto(
    Guid Id,
    Guid TenantId,
    Guid ContractId,
    Guid SubcontractorId,
    EsrsReportType ReportType,
    DateOnly ReportPeriodStart,
    DateOnly ReportPeriodEnd,
    DateOnly RowPeriodStart,
    DateOnly RowPeriodEnd,
    string SocioeconomicCategory,
    string PlanCategory,
    decimal Amount,
    IReadOnlyList<Guid> SupportingEvidenceItemIds,
    string? SourceReference,
    SubcontractingReportDataReviewStatus ReviewStatus,
    string? ReviewerNotes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record SubcontractingReportDataQuery(
    Guid TenantId,
    Guid? ContractId = null,
    EsrsReportType? ReportType = null,
    DateOnly? ReportPeriodStart = null,
    DateOnly? ReportPeriodEnd = null);

public sealed record SubcontractingReportPackageRowsRequest(
    Guid TenantId,
    Guid ContractId,
    EsrsReportType ReportType,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    bool FinalPackage);

public sealed record SubcontractingReportPackageRowDto(
    Guid RowId,
    Guid ContractId,
    Guid SubcontractorId,
    string SocioeconomicCategory,
    string PlanCategory,
    EsrsReportType ReportType,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal Amount,
    IReadOnlyList<Guid> SupportingEvidenceItemIds,
    SubcontractingReportDataReviewStatus ReviewStatus);

public sealed record SubcontractingReportDataImportTemplateDto(
    string FileName,
    IReadOnlyList<string> Columns);

public enum SubcontractingReportDataReviewStatus
{
    Draft,
    PendingReview,
    Reviewed,
    Accepted,
    Rejected
}

public sealed class SubcontractingReportDataValidationException(string message) : InvalidOperationException(message);
