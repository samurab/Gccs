using Gccs.Application.Audit;
using Gccs.Domain.Audit;

namespace Gccs.Application.Labor;

public sealed class LaborComplianceReportService(
    ILaborApplicabilityRepository applicabilityRepository,
    ILaborClassificationRepository classificationRepository,
    IAuditEventWriter auditEventWriter)
{
    public const string WorkflowDisclaimer =
        "This report summarizes GCCS workflow status and source-backed records. It is not a final legal determination.";

    public async Task<LaborDashboardDto> GetDashboardAsync(
        LaborDashboardQuery query,
        CancellationToken cancellationToken = default)
    {
        var obligations = await applicabilityRepository.ListAsync(query.TenantId, query.ContractId, cancellationToken);
        var assignments = await classificationRepository.ListAssignmentsAsync(query.TenantId, query.ContractId, cancellationToken);
        var asOfDate = query.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var gaps = BuildGaps(obligations, assignments);
        return new LaborDashboardDto(
            query.TenantId,
            query.ContractId,
            obligations.Select(ToObligationDto).ToArray(),
            assignments.Select(assignment => ToAssignmentDto(assignment, includeSensitiveEmployeeData: false)).ToArray(),
            gaps,
            obligations.Count(obligation => obligation.ReviewTask?.DueAt is { } due && due < asOfDate && obligation.Status == LaborApplicabilityStatus.Active));
    }

    public async Task<LaborComplianceReportDto> GenerateAsync(
        LaborComplianceReportRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (!request.HasReportPermission)
        {
            throw new LaborComplianceReportException("Report permission is required.");
        }

        var obligations = await applicabilityRepository.ListAsync(request.TenantId, request.ContractId, cancellationToken);
        var categories = await classificationRepository.ListCategoriesAsync(request.TenantId, request.ContractId, cancellationToken);
        var assignments = await classificationRepository.ListAssignmentsAsync(request.TenantId, request.ContractId, cancellationToken);
        var report = new LaborComplianceReportDto(
            Guid.NewGuid(),
            request.TenantId,
            request.ContractId,
            DateTimeOffset.UtcNow,
            WorkflowDisclaimer,
            obligations.Select(ToObligationDto).ToArray(),
            categories.Select(ToCategoryDto).ToArray(),
            assignments.Select(assignment => ToAssignmentDto(assignment, request.IncludeSensitiveEmployeeData)).ToArray(),
            BuildGaps(obligations, assignments),
            obligations
                .Where(obligation => obligation.WageDeterminationEvidenceItemId.HasValue)
                .Select(obligation => new LaborEvidenceReferenceDto(obligation.Id, obligation.WageDeterminationEvidenceItemId!.Value, "WageDetermination"))
                .ToArray());
        await WriteAuditAsync(report, actorUserId, AuditAction.Created, "Labor compliance report was generated.", cancellationToken);
        return report;
    }

    public async Task<LaborComplianceReportExportDto> ExportAsync(
        LaborComplianceReportDto report,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var export = new LaborComplianceReportExportDto(
            report.Id,
            report.TenantId,
            "labor-compliance-report.csv",
            "text/csv",
            DateTimeOffset.UtcNow);
        await auditEventWriter.WriteAsync(
            report.TenantId,
            actorUserId,
            AuditAction.Exported,
            "LaborComplianceReport",
            report.Id.ToString(),
            "Labor compliance report was exported.",
            new Dictionary<string, string>
            {
                ["contractId"] = report.ContractId?.ToString() ?? string.Empty,
                ["fileName"] = export.FileName
            },
            cancellationToken);
        return export;
    }

    private static IReadOnlyList<LaborGapDto> BuildGaps(
        IReadOnlyList<LaborApplicabilityDto> obligations,
        IReadOnlyList<LaborEmployeeAssignmentDto> assignments)
    {
        var gaps = new List<LaborGapDto>();
        gaps.AddRange(obligations
            .Where(obligation => obligation.WageDeterminationEvidenceItemId is null)
            .Select(obligation => new LaborGapDto(obligation.ContractId, "Missing wage determination evidence", "Evidence")));
        if (obligations.Any(obligation => obligation.Status == LaborApplicabilityStatus.Active) &&
            !assignments.Any(assignment => assignment.Status == LaborAssignmentStatus.Active))
        {
            gaps.Add(new LaborGapDto(obligations.First().ContractId, "No active employee labor assignments", "Assignment"));
        }

        return gaps;
    }

    private static LaborObligationReportDto ToObligationDto(LaborApplicabilityDto obligation) =>
        new(
            obligation.Id,
            obligation.ContractId,
            obligation.LaborStandard,
            obligation.SourceClause,
            obligation.WageDeterminationReference,
            obligation.PlaceOfPerformance,
            obligation.Status,
            obligation.ReviewTask?.DueAt);

    private static LaborCategoryReportDto ToCategoryDto(LaborCategoryDto category) =>
        new(
            category.Id,
            category.ContractId,
            category.Title,
            category.WageDeterminationClassification,
            category.HourlyWage,
            category.FringeRate,
            category.SourceReference,
            category.IsActive);

    private static LaborAssignmentReportDto ToAssignmentDto(
        LaborEmployeeAssignmentDto assignment,
        bool includeSensitiveEmployeeData) =>
        new(
            assignment.Id,
            assignment.ContractId,
            assignment.EmployeeId,
            includeSensitiveEmployeeData ? assignment.EmployeeName : null,
            includeSensitiveEmployeeData ? assignment.EmployeeEmail : null,
            assignment.LaborCategoryTitle,
            assignment.WorkLocation,
            assignment.Status,
            assignment.SourceReference);

    private async Task WriteAuditAsync(
        LaborComplianceReportDto report,
        Guid actorUserId,
        AuditAction action,
        string summary,
        CancellationToken cancellationToken)
    {
        await auditEventWriter.WriteAsync(
            report.TenantId,
            actorUserId,
            action,
            "LaborComplianceReport",
            report.Id.ToString(),
            summary,
            new Dictionary<string, string>
            {
                ["contractId"] = report.ContractId?.ToString() ?? string.Empty,
                ["obligations"] = report.Obligations.Count.ToString(),
                ["assignments"] = report.Assignments.Count.ToString(),
                ["gaps"] = report.Gaps.Count.ToString()
            },
            cancellationToken);
    }
}

public sealed record LaborDashboardQuery(
    Guid TenantId,
    Guid? ContractId = null,
    DateOnly? AsOfDate = null);

public sealed record LaborComplianceReportRequest(
    Guid TenantId,
    Guid? ContractId = null,
    bool IncludeSensitiveEmployeeData = false,
    bool HasReportPermission = true);

public sealed record LaborDashboardDto(
    Guid TenantId,
    Guid? ContractId,
    IReadOnlyList<LaborObligationReportDto> Obligations,
    IReadOnlyList<LaborAssignmentReportDto> Assignments,
    IReadOnlyList<LaborGapDto> Gaps,
    int OverdueItems);

public sealed record LaborComplianceReportDto(
    Guid Id,
    Guid TenantId,
    Guid? ContractId,
    DateTimeOffset GeneratedAt,
    string WorkflowDisclaimer,
    IReadOnlyList<LaborObligationReportDto> Obligations,
    IReadOnlyList<LaborCategoryReportDto> Categories,
    IReadOnlyList<LaborAssignmentReportDto> Assignments,
    IReadOnlyList<LaborGapDto> Gaps,
    IReadOnlyList<LaborEvidenceReferenceDto> EvidenceReferences);

public sealed record LaborComplianceReportExportDto(
    Guid ReportId,
    Guid TenantId,
    string FileName,
    string ContentType,
    DateTimeOffset ExportedAt);

public sealed record LaborObligationReportDto(
    Guid Id,
    Guid ContractId,
    string LaborStandard,
    string? SourceClause,
    string? WageDeterminationReference,
    string PlaceOfPerformance,
    LaborApplicabilityStatus Status,
    DateOnly? ReviewDueAt);

public sealed record LaborCategoryReportDto(
    Guid Id,
    Guid ContractId,
    string Title,
    string WageDeterminationClassification,
    decimal HourlyWage,
    decimal FringeRate,
    string SourceReference,
    bool IsActive);

public sealed record LaborAssignmentReportDto(
    Guid Id,
    Guid ContractId,
    Guid EmployeeId,
    string? EmployeeName,
    string? EmployeeEmail,
    string LaborCategoryTitle,
    string WorkLocation,
    LaborAssignmentStatus Status,
    string SourceReference);

public sealed record LaborGapDto(
    Guid ContractId,
    string Description,
    string GapType);

public sealed record LaborEvidenceReferenceDto(
    Guid SourceRecordId,
    Guid EvidenceItemId,
    string EvidenceType);

public sealed class LaborComplianceReportException(string message) : InvalidOperationException(message);
