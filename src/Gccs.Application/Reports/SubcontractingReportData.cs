using System.Globalization;
using System.Text;
using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Domain.Audit;

namespace Gccs.Application.Reports;

public sealed class SubcontractingReportDataService(
    ISubcontractingReportDataRepository repository,
    IAuditEventWriter auditEventWriter,
    IApplicationTransaction transaction)
{
    private const int MaximumImportRows = 1_000;
    public static readonly IReadOnlySet<string> SprSocioeconomicCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Small Business Concerns (SB)", "Other Than Small Business Concerns (OTSB)",
        "Small Disadvantaged Business (SDB)", "Women-Owned Small Business (WOSB)",
        "HBCU/MSI", "HUBZone Small Business", "Veteran-Owned Small Business (VOSB)",
        "Service-Disabled Veteran-Owned Small Business (SDVOSB)", "ANC/Indian Tribe"
    };

    public Task<SubcontractingReportDataRowDto> CreateAsync(
        SubcontractingReportDataRowRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        CreateCoreAsync(request, actorUserId, requireSprMetadata: false, cancellationToken);

    public Task<SubcontractingReportDataRowDto> CreateSprAsync(
        SubcontractingReportDataRowRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        CreateCoreAsync(request, actorUserId, requireSprMetadata: true, cancellationToken);

    private Task<SubcontractingReportDataRowDto> CreateCoreAsync(
        SubcontractingReportDataRowRequest request,
        Guid actorUserId,
        bool requireSprMetadata,
        CancellationToken cancellationToken) =>
        transaction.ExecuteAsync(async token =>
        {
            var normalized = Normalize(request);
            await ValidateAsync(normalized, null, requireSprMetadata, token);
            var row = await repository.CreateAsync(normalized, actorUserId, token);
            await WriteAuditAsync(row, actorUserId, AuditAction.Created, "Subcontracting report data row was created.", token);
            return row;
        }, cancellationToken);

    public async Task<SubcontractingReportDataRowDto?> UpdateAsync(
        Guid rowId,
        SubcontractingReportDataRowRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        await UpdateCoreAsync(rowId, request, actorUserId, requireSprMetadata: false, cancellationToken);

    public async Task<SubcontractingReportDataRowDto?> UpdateSprAsync(
        Guid rowId,
        SubcontractingReportDataRowRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        await UpdateCoreAsync(rowId, request, actorUserId, requireSprMetadata: true, cancellationToken);

    private async Task<SubcontractingReportDataRowDto?> UpdateCoreAsync(
        Guid rowId,
        SubcontractingReportDataRowRequest request,
        Guid actorUserId,
        bool requireSprMetadata,
        CancellationToken cancellationToken) =>
        await transaction.ExecuteAsync(async token =>
        {
            var existing = await repository.FindCurrentTenantAsync(rowId, token);
            if (existing is null) return null;
            if (request.ExpectedVersion is null || request.ExpectedVersion != existing.Version)
                throw Validation("expectedVersion", "The report data row changed. Reload it and try again.");
            if (!requireSprMetadata && !HasSprMetadata(request) && existing.SprReadinessStatus == SprReadinessStatus.Ready)
                request = request with
                {
                    ReportingRole = existing.ReportingRole, ReportingFiscalYear = existing.ReportingFiscalYear,
                    ReportingPeriod = existing.ReportingPeriod, ReportingEntityUei = existing.ReportingEntityUei,
                    PrimeContractPiid = existing.PrimeContractPiid, SubcontractNumber = existing.SubcontractNumber,
                    SprEligibilityConfirmed = existing.SprEligibilityConfirmed, SprEligibilityBasis = existing.SprEligibilityBasis
                };
            var normalized = Normalize(request);
            await ValidateAsync(normalized, rowId, requireSprMetadata, token);
            var updated = await repository.UpdateAsync(rowId, normalized, actorUserId, token);
            if (updated is not null)
                await WriteAuditAsync(updated, actorUserId, AuditAction.Updated, "Subcontracting report data row was updated and requires review.", token);
            return updated;
        }, cancellationToken);

    public Task<SubcontractingReportDataRowDto?> FindCurrentTenantAsync(Guid rowId, CancellationToken cancellationToken = default) =>
        repository.FindCurrentTenantAsync(rowId, cancellationToken);

    public Task<IReadOnlyList<SubcontractingReportDataRowDto>> ListCurrentTenantAsync(
        SubcontractingReportDataQuery query,
        CancellationToken cancellationToken = default) => repository.ListCurrentTenantAsync(query, cancellationToken);

    public Task<bool> ContractExistsCurrentTenantAsync(Guid contractId, CancellationToken cancellationToken = default) =>
        repository.ContractExistsCurrentTenantAsync(contractId, cancellationToken);

    public async Task<SubcontractingReportDataRowDto?> UpdateReviewStatusAsync(
        Guid rowId,
        SubcontractingReportDataReviewRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (request.Status is not (SubcontractingReportDataReviewStatus.Reviewed or
            SubcontractingReportDataReviewStatus.Accepted or SubcontractingReportDataReviewStatus.Rejected))
            throw Validation("status", "Review status must be Reviewed, Accepted, or Rejected.");
        if (request.Status == SubcontractingReportDataReviewStatus.Rejected && string.IsNullOrWhiteSpace(request.ReviewerNotes))
            throw Validation("reviewerNotes", "Reviewer notes are required when a row is rejected.");

        return await transaction.ExecuteAsync(async token =>
        {
            var existing = await repository.FindCurrentTenantAsync(rowId, token);
            if (existing is null) return null;
            if (request.ExpectedVersion != existing.Version)
                throw Validation("expectedVersion", "The report data row changed. Reload it and try again.");
            if (existing.ReviewStatus == request.Status)
                throw Validation("status", $"The row is already {request.Status}.");
            var updated = await repository.UpdateReviewStatusAsync(rowId, request.Status, NormalizeOptional(request.ReviewerNotes), actorUserId, token);
            if (updated is not null)
            {
                var action = request.Status switch
                {
                    SubcontractingReportDataReviewStatus.Accepted => AuditAction.Approved,
                    SubcontractingReportDataReviewStatus.Rejected => AuditAction.Rejected,
                    _ => AuditAction.Updated
                };
                await WriteAuditAsync(updated, actorUserId, action, $"Subcontracting report data row was {request.Status}.", token);
            }
            return updated;
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<SubcontractingReportPackageRowDto>> PreparePackageRowsAsync(
        SubcontractingReportPackageRowsRequest request,
        CancellationToken cancellationToken = default)
    {
        var rows = await repository.ListCurrentTenantAsync(
            new SubcontractingReportDataQuery(request.ContractId, request.ReportType, request.PeriodStart, request.PeriodEnd),
            cancellationToken);
        var blocked = rows.Where(row => !row.IsPackageEligible).ToArray();
        if (request.FinalPackage && blocked.Length > 0)
            throw Validation("rows", "Final SAM.gov SPR preparation packages can include only rows that are SPR-ready and reviewed or explicitly accepted.");
        return rows.Where(row => row.IsPackageEligible).Select(row => new SubcontractingReportPackageRowDto(
            row.Id, row.ContractId, row.SubcontractorId, row.SocioeconomicCategory, row.PlanCategory,
            row.ReportType, row.RowPeriodStart, row.RowPeriodEnd, row.Amount,
            row.SupportingEvidenceItemIds, row.ReviewStatus)).ToArray();
    }

    public async Task<IReadOnlyList<SubcontractingReportDataRowDto>> ImportCsvAsync(
        string csvContent,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
        => await ImportCsvCoreAsync(csvContent, actorUserId, requireSprMetadata: false, cancellationToken);

    public async Task<IReadOnlyList<SubcontractingReportDataRowDto>> ImportSprCsvAsync(
        string csvContent,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
        => await ImportCsvCoreAsync(csvContent, actorUserId, requireSprMetadata: true, cancellationToken);

    private async Task<IReadOnlyList<SubcontractingReportDataRowDto>> ImportCsvCoreAsync(
        string csvContent,
        Guid actorUserId,
        bool requireSprMetadata,
        CancellationToken cancellationToken)
    {
        var requests = ParseCsv(csvContent, requireSprMetadata);
        return await transaction.ExecuteAsync(async token =>
        {
            var imported = new List<SubcontractingReportDataRowDto>(requests.Count);
            foreach (var request in requests)
            {
                var normalized = Normalize(request);
                await ValidateAsync(normalized, null, requireSprMetadata, token);
                var row = await repository.CreateAsync(normalized, actorUserId, token);
                await WriteAuditAsync(row, actorUserId, AuditAction.Created, "Subcontracting report data row was imported.", token);
                imported.Add(row);
            }
            return imported;
        }, cancellationToken);
    }

    public static SubcontractingReportDataImportTemplateDto GetImportTemplate(bool spr = false)
    {
        string[] legacyColumns = ["contractId", "subcontractorId", "reportType", "reportPeriodStart", "reportPeriodEnd",
            "rowPeriodStart", "rowPeriodEnd", "socioeconomicCategory", "planCategory", "amount",
            "supportingEvidenceItemIds", "sourceReference"];
        string[] sprColumns = [.. legacyColumns, "reportingRole", "reportingFiscalYear", "reportingPeriod",
            "reportingEntityUei", "primeContractPiid", "subcontractNumber", "sprEligibilityConfirmed", "sprEligibilityBasis"];
        var columns = spr ? sprColumns : legacyColumns;
        return new(spr ? "sam-gov-spr-report-data-template.csv" : "subcontracting-report-data-template.csv",
            columns, string.Join(',', columns) + Environment.NewLine);
    }

    private async Task ValidateAsync(SubcontractingReportDataRowRequest request, Guid? existingRowId, bool requireSprMetadata, CancellationToken token)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        if (request.ContractId == Guid.Empty) errors["contractId"] = ["Contract is required."];
        if (request.SubcontractorId == Guid.Empty) errors["subcontractorId"] = ["Subcontractor is required."];
        if (!Enum.IsDefined(request.ReportType)) errors["reportType"] = ["Report type must be ISR or SSR."];
        if (string.IsNullOrWhiteSpace(request.SocioeconomicCategory)) errors["socioeconomicCategory"] = ["Socioeconomic category is required."];
        else if (request.SocioeconomicCategory.Length > 120) errors["socioeconomicCategory"] = ["Socioeconomic category cannot exceed 120 characters."];
        if (string.IsNullOrWhiteSpace(request.PlanCategory)) errors["planCategory"] = ["Plan category is required."];
        else if (request.PlanCategory.Length > 120) errors["planCategory"] = ["Plan category cannot exceed 120 characters."];
        if (request.Amount < 0) errors["amount"] = ["Amount cannot be negative."];
        else if (request.Amount > 999_999_999_999.99m) errors["amount"] = ["Amount exceeds the supported maximum."];
        if (request.ReportPeriodEnd < request.ReportPeriodStart || request.RowPeriodEnd < request.RowPeriodStart)
            errors["period"] = ["Report and row periods must have valid start and end dates."];
        else if (request.RowPeriodStart < request.ReportPeriodStart || request.RowPeriodEnd > request.ReportPeriodEnd)
            errors["rowPeriod"] = ["Row period must fall within the report period."];
        if (string.IsNullOrWhiteSpace(request.SourceReference)) errors["sourceReference"] = ["A source reference is required for traceability."];
        else if (request.SourceReference.Length > 500) errors["sourceReference"] = ["Source reference cannot exceed 500 characters."];
        if (request.SupportingEvidenceItemIds.Count > 100) errors["supportingEvidenceItemIds"] = ["A row cannot link more than 100 evidence items."];
        ValidateSprMetadata(request, requireSprMetadata, errors);
        if (errors.Count > 0) throw new SubcontractingReportDataValidationException(errors);
        var referenceErrors = await repository.ValidateReferencesCurrentTenantAsync(request, token);
        if (referenceErrors.Keys.Any(key => key is "contractId" or "subcontractorId" or "supportingEvidenceItemIds"))
            throw new SubcontractingReportDataReferenceNotFoundException();
        if (referenceErrors.Count > 0) throw new SubcontractingReportDataValidationException(referenceErrors);
        if (await repository.ExistsDuplicateCurrentTenantAsync(request, existingRowId, token))
            throw Validation("duplicate", "A duplicate subcontracting report data row already exists.");
    }

    private static SubcontractingReportDataRowRequest Normalize(SubcontractingReportDataRowRequest request) => request with
    {
        SocioeconomicCategory = request.SocioeconomicCategory.Trim(),
        PlanCategory = request.PlanCategory.Trim(),
        SourceReference = NormalizeOptional(request.SourceReference),
        ReportingEntityUei = NormalizeOptional(request.ReportingEntityUei)?.ToUpperInvariant(),
        PrimeContractPiid = NormalizeOptional(request.PrimeContractPiid),
        SubcontractNumber = NormalizeOptional(request.SubcontractNumber),
        SprEligibilityBasis = NormalizeOptional(request.SprEligibilityBasis),
        SupportingEvidenceItemIds = (request.SupportingEvidenceItemIds ?? []).Where(id => id != Guid.Empty).Distinct().ToArray()
    };

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidateSprMetadata(SubcontractingReportDataRowRequest request, bool required, IDictionary<string, string[]> errors)
    {
        var anySprMetadata = HasSprMetadata(request);
        if (!required && !anySprMetadata) return;

        if (request.ReportingRole is null || !Enum.IsDefined(request.ReportingRole.Value))
            errors["reportingRole"] = ["SAM.gov SPR reporting role is required and must be supported."];
        if (request.ReportingFiscalYear is null) errors["reportingFiscalYear"] = ["SAM.gov SPR reporting fiscal year is required."];
        else
        {
            var currentFiscalYear = DateTimeOffset.UtcNow.Month >= 10 ? DateTimeOffset.UtcNow.Year + 1 : DateTimeOffset.UtcNow.Year;
            if (request.ReportingFiscalYear < currentFiscalYear - 9 || request.ReportingFiscalYear > currentFiscalYear)
                errors["reportingFiscalYear"] = [$"Reporting fiscal year must be between {currentFiscalYear - 9} and {currentFiscalYear}."];
        }
        if (request.ReportingPeriod is null || !Enum.IsDefined(request.ReportingPeriod.Value))
            errors["reportingPeriod"] = ["SAM.gov SPR reporting period is required and must be supported."];
        else if (request.ReportingPeriod == SprReportingPeriod.March31 && (request.ReportPeriodEnd.Month != 3 || request.ReportPeriodEnd.Day != 31) ||
                 request.ReportingPeriod == SprReportingPeriod.September30 && (request.ReportPeriodEnd.Month != 9 || request.ReportPeriodEnd.Day != 30))
            errors["reportingPeriod"] = ["The selected SAM.gov SPR reporting period does not match the report period end date."];
        if (request.ReportingFiscalYear is not null && request.ReportingPeriod is not null && request.ReportingPeriod != SprReportingPeriod.Final &&
            request.ReportPeriodEnd.Year != request.ReportingFiscalYear)
            errors["reportingFiscalYear"] = ["Reporting fiscal year must match the report period end year."];
        if (string.IsNullOrWhiteSpace(request.ReportingEntityUei) || request.ReportingEntityUei.Length != 12 ||
            request.ReportingEntityUei.Any(character => !char.IsAsciiLetterOrDigit(character)))
            errors["reportingEntityUei"] = ["Reporting entity UEI must contain exactly 12 letters or digits."];
        if (string.IsNullOrWhiteSpace(request.PrimeContractPiid) || request.PrimeContractPiid.Length > 64)
            errors["primeContractPiid"] = ["Prime contract PIID is required and cannot exceed 64 characters."];
        if (request.ReportingRole == SprReportingRole.Subcontractor && string.IsNullOrWhiteSpace(request.SubcontractNumber))
            errors["subcontractNumber"] = ["Subcontract number is required for subcontractor reporting."];
        if (request.SubcontractNumber?.Length > 64) errors["subcontractNumber"] = ["Subcontract number cannot exceed 64 characters."];
        if (request.Amount != decimal.Truncate(request.Amount)) errors["amount"] = ["SAM.gov SPR amounts must be entered in whole dollars."];
        if (!SprSocioeconomicCategories.Contains(request.SocioeconomicCategory))
            errors["socioeconomicCategory"] = ["Select a socioeconomic category defined by the current SAM.gov SPR data dictionary."];
        if (!request.SprEligibilityConfirmed) errors["sprEligibilityConfirmed"] = ["Confirm the external SAM.gov SPR eligibility basis before this row can be SPR-ready."];
        if (string.IsNullOrWhiteSpace(request.SprEligibilityBasis)) errors["sprEligibilityBasis"] = ["Document the SAM.gov SPR eligibility basis."];
        else if (request.SprEligibilityBasis.Length > 500) errors["sprEligibilityBasis"] = ["SPR eligibility basis cannot exceed 500 characters."];
    }

    private static bool HasSprMetadata(SubcontractingReportDataRowRequest request) =>
        request.ReportingRole is not null || request.ReportingFiscalYear is not null || request.ReportingPeriod is not null ||
        request.ReportingEntityUei is not null || request.PrimeContractPiid is not null || request.SubcontractNumber is not null ||
        request.SprEligibilityConfirmed || request.SprEligibilityBasis is not null;

    private static IReadOnlyList<SubcontractingReportDataRowRequest> ParseCsv(string csvContent, bool spr)
    {
        if (string.IsNullOrWhiteSpace(csvContent)) throw Validation("file", "The CSV import is empty.");
        if (csvContent.Length > 2_000_000) throw Validation("file", "The CSV import cannot exceed 2 MB.");
        var records = ParseCsvRecords(csvContent);
        if (records.Count < 2) throw Validation("file", "The CSV import must contain a header and at least one data row.");
        if (records.Count - 1 > MaximumImportRows) throw Validation("file", $"The CSV import cannot exceed {MaximumImportRows} data rows.");
        var expected = GetImportTemplate(spr).Columns;
        if (!records[0].SequenceEqual(expected, StringComparer.OrdinalIgnoreCase))
            throw Validation("header", "The CSV header does not match the current import template.");
        var requests = new List<SubcontractingReportDataRowRequest>();
        for (var index = 1; index < records.Count; index++)
        {
            var cells = records[index]; var rowNumber = index + 1;
            if (cells.Count != expected.Count) throw Validation("file", $"CSV row {rowNumber} has {cells.Count} columns; expected {expected.Count}.");
            if (!Guid.TryParse(cells[0], out var contractId) || !Guid.TryParse(cells[1], out var subcontractorId) ||
                !Enum.TryParse<EsrsReportType>(cells[2], true, out var reportType) ||
                !DateOnly.TryParse(cells[3], CultureInfo.InvariantCulture, DateTimeStyles.None, out var reportStart) ||
                !DateOnly.TryParse(cells[4], CultureInfo.InvariantCulture, DateTimeStyles.None, out var reportEnd) ||
                !DateOnly.TryParse(cells[5], CultureInfo.InvariantCulture, DateTimeStyles.None, out var rowStart) ||
                !DateOnly.TryParse(cells[6], CultureInfo.InvariantCulture, DateTimeStyles.None, out var rowEnd) ||
                !decimal.TryParse(cells[9], NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
                throw Validation("file", $"CSV row {rowNumber} contains an invalid identifier, report type, date, or amount.");
            var evidence = string.IsNullOrWhiteSpace(cells[10]) ? [] : cells[10].Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(value => Guid.TryParse(value, out var id) ? id : throw Validation("file", $"CSV row {rowNumber} contains an invalid evidence identifier.")).ToArray();
            SprReportingRole? reportingRole = null; int? reportingFiscalYear = null; SprReportingPeriod? reportingPeriod = null;
            var parsedRole = default(SprReportingRole); var parsedFiscalYear = 0; var parsedPeriod = default(SprReportingPeriod);
            bool sprEligibilityConfirmed = false;
            if (spr && (!Enum.TryParse<SprReportingRole>(cells[12], true, out parsedRole) ||
                !int.TryParse(cells[13], NumberStyles.None, CultureInfo.InvariantCulture, out parsedFiscalYear) ||
                !Enum.TryParse<SprReportingPeriod>(cells[14], true, out parsedPeriod) ||
                !bool.TryParse(cells[18], out sprEligibilityConfirmed)))
                throw Validation("file", $"CSV row {rowNumber} contains invalid SAM.gov SPR reporting metadata.");
            if (spr) { reportingRole = parsedRole; reportingFiscalYear = parsedFiscalYear; reportingPeriod = parsedPeriod; }
            requests.Add(new(contractId, subcontractorId, reportType, reportStart, reportEnd, rowStart, rowEnd,
                cells[7], cells[8], amount, evidence, cells[11], null, reportingRole, reportingFiscalYear, reportingPeriod,
                spr ? cells[15] : null, spr ? cells[16] : null, spr ? cells[17] : null,
                sprEligibilityConfirmed, spr ? cells[19] : null));
        }
        return requests;
    }

    private static List<IReadOnlyList<string>> ParseCsvRecords(string input)
    {
        var records = new List<IReadOnlyList<string>>(); var row = new List<string>(); var field = new StringBuilder(); var quoted = false;
        for (var i = 0; i < input.Length; i++)
        {
            var character = input[i];
            if (character == '"')
            {
                if (quoted && i + 1 < input.Length && input[i + 1] == '"') { field.Append('"'); i++; }
                else quoted = !quoted;
            }
            else if (character == ',' && !quoted) { row.Add(field.ToString().Trim()); field.Clear(); }
            else if ((character == '\n' || character == '\r') && !quoted)
            {
                if (character == '\r' && i + 1 < input.Length && input[i + 1] == '\n') i++;
                row.Add(field.ToString().Trim()); field.Clear();
                if (row.Any(cell => cell.Length > 0)) records.Add(row.ToArray());
                row = [];
            }
            else field.Append(character);
        }
        if (quoted) throw Validation("file", "The CSV import contains an unterminated quoted field.");
        row.Add(field.ToString().Trim());
        if (row.Any(cell => cell.Length > 0)) records.Add(row.ToArray());
        return records;
    }

    private async Task WriteAuditAsync(SubcontractingReportDataRowDto row, Guid actorUserId, AuditAction action, string summary, CancellationToken token) =>
        await auditEventWriter.WriteAsync(row.TenantId, actorUserId, action, "SubcontractingReportDataRow", row.Id.ToString(), summary,
            new Dictionary<string, string> { ["contractId"] = row.ContractId.ToString(), ["subcontractorId"] = row.SubcontractorId.ToString(),
                ["reportType"] = row.ReportType.ToString(), ["socioeconomicCategory"] = row.SocioeconomicCategory,
                ["planCategory"] = row.PlanCategory, ["amount"] = row.Amount.ToString("0.00", CultureInfo.InvariantCulture),
                ["sprReadinessStatus"] = row.SprReadinessStatus.ToString(),
                ["reviewStatus"] = row.ReviewStatus.ToString(), ["evidenceCount"] = row.SupportingEvidenceItemIds.Count.ToString(CultureInfo.InvariantCulture),
                ["version"] = row.Version.ToString(CultureInfo.InvariantCulture) }, token);

    private static SubcontractingReportDataValidationException Validation(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}

public interface ISubcontractingReportDataRepository
{
    Task<SubcontractingReportDataRowDto> CreateAsync(SubcontractingReportDataRowRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<SubcontractingReportDataRowDto?> UpdateAsync(Guid rowId, SubcontractingReportDataRowRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<SubcontractingReportDataRowDto?> FindCurrentTenantAsync(Guid rowId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SubcontractingReportDataRowDto>> ListCurrentTenantAsync(SubcontractingReportDataQuery query, CancellationToken cancellationToken = default);
    Task<SubcontractingReportDataRowDto?> UpdateReviewStatusAsync(Guid rowId, SubcontractingReportDataReviewStatus status, string? reviewerNotes, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<bool> ExistsDuplicateCurrentTenantAsync(SubcontractingReportDataRowRequest request, Guid? existingRowId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, string[]>> ValidateReferencesCurrentTenantAsync(SubcontractingReportDataRowRequest request, CancellationToken cancellationToken = default);
    Task<bool> ContractExistsCurrentTenantAsync(Guid contractId, CancellationToken cancellationToken = default);
}

public sealed record SubcontractingReportDataRowRequest(Guid ContractId, Guid SubcontractorId, EsrsReportType ReportType,
    DateOnly ReportPeriodStart, DateOnly ReportPeriodEnd, DateOnly RowPeriodStart, DateOnly RowPeriodEnd,
    string SocioeconomicCategory, string PlanCategory, decimal Amount, IReadOnlyList<Guid> SupportingEvidenceItemIds, string? SourceReference,
    int? ExpectedVersion = null, SprReportingRole? ReportingRole = null, int? ReportingFiscalYear = null,
    SprReportingPeriod? ReportingPeriod = null, string? ReportingEntityUei = null, string? PrimeContractPiid = null,
    string? SubcontractNumber = null, bool SprEligibilityConfirmed = false, string? SprEligibilityBasis = null);
public sealed record SubcontractingReportDataReviewRequest(SubcontractingReportDataReviewStatus Status, string? ReviewerNotes, int ExpectedVersion);
public sealed record SubcontractingReportDataImportRequest(string CsvContent);
public sealed record SubcontractingReportDataRowDto(Guid Id, Guid TenantId, Guid ContractId, Guid SubcontractorId,
    EsrsReportType ReportType, DateOnly ReportPeriodStart, DateOnly ReportPeriodEnd, DateOnly RowPeriodStart, DateOnly RowPeriodEnd,
    string SocioeconomicCategory, string PlanCategory, decimal Amount, IReadOnlyList<Guid> SupportingEvidenceItemIds,
    string? SourceReference, SubcontractingReportDataReviewStatus ReviewStatus, Guid? ReviewedByUserId,
    DateTimeOffset? ReviewedAt, string? ReviewerNotes, int Version, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt,
    SprReportingRole? ReportingRole = null, int? ReportingFiscalYear = null, SprReportingPeriod? ReportingPeriod = null,
    string? ReportingEntityUei = null, string? PrimeContractPiid = null, string? SubcontractNumber = null,
    bool SprEligibilityConfirmed = false, string? SprEligibilityBasis = null)
{
    public SprReadinessStatus SprReadinessStatus => ReportingRole is not null && ReportingFiscalYear is not null && ReportingPeriod is not null &&
        !string.IsNullOrWhiteSpace(ReportingEntityUei) && !string.IsNullOrWhiteSpace(PrimeContractPiid) &&
        (ReportingRole != SprReportingRole.Subcontractor || !string.IsNullOrWhiteSpace(SubcontractNumber)) &&
        SprEligibilityConfirmed && !string.IsNullOrWhiteSpace(SprEligibilityBasis)
            ? SprReadinessStatus.Ready
            : SprReadinessStatus.NeedsVerification;
    public bool IsPackageEligible => SprReadinessStatus == SprReadinessStatus.Ready &&
        ReviewStatus is SubcontractingReportDataReviewStatus.Reviewed or SubcontractingReportDataReviewStatus.Accepted;
}
public sealed record SubcontractingReportDataQuery(Guid? ContractId = null, EsrsReportType? ReportType = null,
    DateOnly? ReportPeriodStart = null, DateOnly? ReportPeriodEnd = null);
public sealed record SubcontractingReportPackageRowsRequest(Guid ContractId, EsrsReportType ReportType, DateOnly PeriodStart, DateOnly PeriodEnd, bool FinalPackage);
public sealed record SubcontractingReportPackageRowDto(Guid RowId, Guid ContractId, Guid SubcontractorId, string SocioeconomicCategory,
    string PlanCategory, EsrsReportType ReportType, DateOnly PeriodStart, DateOnly PeriodEnd, decimal Amount,
    IReadOnlyList<Guid> SupportingEvidenceItemIds, SubcontractingReportDataReviewStatus ReviewStatus);
public sealed record SubcontractingReportDataImportTemplateDto(string FileName, IReadOnlyList<string> Columns, string CsvContent);
public enum SubcontractingReportDataReviewStatus { Draft, PendingReview, Reviewed, Accepted, Rejected }
public enum SprReportingRole { PrimeContractor, Subcontractor }
public enum SprReportingPeriod { March31, September30, Final }
public enum SprReadinessStatus { NeedsVerification, Ready }

public sealed class SubcontractingReportDataValidationException : InvalidOperationException
{
    public SubcontractingReportDataValidationException(IReadOnlyDictionary<string, string[]> errors) : base("Subcontracting report data is invalid.") => Errors = errors;
    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
public sealed class SubcontractingReportDataReferenceNotFoundException : InvalidOperationException;
