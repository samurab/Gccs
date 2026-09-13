using Gccs.Application.Audit;
using Gccs.Application.Ai;
using Gccs.Application.Common;
using Gccs.Application.Reports;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;
using Gccs.Domain.Reports;

namespace Gccs.Application.Labor;

public sealed class LaborComplianceReportService(
    ILaborComplianceReportRepository repository,
    IAuditEventWriter auditEventWriter,
    TenantDataHandlingModePolicyService dataHandlingModePolicy,
    ContentClassificationPolicy classificationPolicy,
    IApplicationTransaction transaction,
    AiOutputReviewService? aiOutputReview = null)
{
    public const string WorkflowDisclaimer =
        "Workflow guidance only. This labor report summarizes source-backed records and review status. " +
        "It is not legal advice, a wage determination, a certification decision, a contracting-officer determination, or a government endorsement.";

    public Task<LaborDashboardDto?> GetDashboardAsync(
        LaborDashboardQuery query,
        bool includeSensitiveEmployeeData,
        CancellationToken cancellationToken = default)
    {
        ValidateQuery(query);
        return repository.GetDashboardAsync(query, includeSensitiveEmployeeData, cancellationToken);
    }

    public Task<LaborComplianceReportDto?> GenerateAsync(
        LaborComplianceReportRequest request,
        Guid actorUserId,
        bool includeSensitiveEmployeeData,
        CancellationToken cancellationToken = default) =>
        transaction.ExecuteAsync(async transactionCancellationToken =>
        {
            ValidateRequest(request);
            var classification = await ClassifiedWorkflowValidation.ConfirmAsync(
                classificationPolicy, request.Classification, TenantDataHandlingWorkflow.Report,
                actorUserId, transactionCancellationToken);
            await dataHandlingModePolicy.EnsureAllowedAsync(
                new TenantDataHandlingModePolicyRequest(TenantDataHandlingWorkflow.Report, ContainsRealCui: false),
                actorUserId, transactionCancellationToken);

            var report = await repository.GenerateAsync(
                request, actorUserId, includeSensitiveEmployeeData, classification, transactionCancellationToken);
            if (report is null) return null;

            await auditEventWriter.WriteAsync(
                report.TenantId, actorUserId, AuditAction.Created, "Report", report.Id.ToString(),
                "Labor compliance workflow report was generated.",
                new Dictionary<string, string>
                {
                    ["reportType"] = ReportType.LaborCompliance.ToString(),
                    ["contractId"] = report.Snapshot.ContractId?.ToString() ?? string.Empty,
                    ["obligations"] = report.Snapshot.Obligations.Count.ToString(),
                    ["assignments"] = report.Snapshot.Assignments.Count.ToString(),
                    ["gaps"] = report.Snapshot.Gaps.Count.ToString(),
                    ["includedSensitiveEmployeeData"] = includeSensitiveEmployeeData.ToString()
                }, transactionCancellationToken);
            if (request.AiOutputId is Guid outputId)
                await RequiredAiReview().RequireDeliverableLinkAsync(outputId, report.TenantId,
                    AiDeliverableType.Report, report.Id, actorUserId, transactionCancellationToken);
            return report;
        }, cancellationToken);

    private AiOutputReviewService RequiredAiReview() => aiOutputReview ??
        throw new AiOutputReviewValidationException("aiOutputId", "AI output provenance processing is unavailable.");

    private static void ValidateRequest(LaborComplianceReportRequest request)
    {
        ValidateQuery(request.Filters ?? new LaborDashboardQuery(ContractId: request.ContractId));
        if (request.ContractId.HasValue && request.Filters?.ContractId.HasValue == true &&
            request.ContractId != request.Filters.ContractId)
            throw new LaborComplianceReportException("The report contract and dashboard filter contract must match.");
        if (request.ReviewerNotes?.Length > 2_000)
            throw new LaborComplianceReportException("Reviewer notes cannot exceed 2,000 characters.");
    }

    private static void ValidateQuery(LaborDashboardQuery query)
    {
        if (query.Location?.Length > 240) throw new LaborComplianceReportException("Location cannot exceed 240 characters.");
        if (query.DueFrom.HasValue && query.DueTo.HasValue && query.DueTo < query.DueFrom)
            throw new LaborComplianceReportException("Due-to date cannot be before due-from date.");
        if (!string.IsNullOrWhiteSpace(query.Status) && !Enum.TryParse<LaborDashboardStatus>(query.Status, true, out _))
            throw new LaborComplianceReportException("Dashboard status must be Active, Inactive, Draft, PendingReview, Reviewed, Rejected, Gap, or Overdue.");
    }
}

public interface ILaborComplianceReportRepository
{
    Task<LaborDashboardDto?> GetDashboardAsync(LaborDashboardQuery query, bool includeSensitiveEmployeeData, CancellationToken cancellationToken = default);
    Task<LaborComplianceReportDto?> GenerateAsync(LaborComplianceReportRequest request, Guid actorUserId,
        bool includeSensitiveEmployeeData, ContentClassificationRequest classification, CancellationToken cancellationToken = default);
}

public enum LaborDashboardStatus { Active, Inactive, Draft, PendingReview, Reviewed, Rejected, Gap, Overdue }
public enum LaborEvidenceType { WageDetermination, PayrollSupport, FringeDocumentation, ClassificationReview, Training, CorrectiveAction }

public sealed record LaborDashboardQuery(Guid? ContractId = null, Guid? EmployeeId = null, Guid? LaborCategoryId = null,
    string? Location = null, string? Status = null, DateOnly? DueFrom = null, DateOnly? DueTo = null,
    bool MissingEvidenceOnly = false, DateOnly? AsOfDate = null);

public sealed record LaborComplianceReportRequest(Guid? ContractId, string? ReviewerNotes,
    LaborDashboardQuery? Filters, ContentClassificationRequest? Classification, Guid? AiOutputId = null);

public sealed record LaborDashboardDto(Guid TenantId, LaborDashboardQuery Filters,
    IReadOnlyList<LaborObligationReportDto> Obligations, IReadOnlyList<LaborCategoryReportDto> Categories,
    IReadOnlyList<LaborAssignmentReportDto> Assignments, IReadOnlyList<LaborGapDto> Gaps,
    IReadOnlyList<LaborOverdueItemDto> OverdueItems, IReadOnlyDictionary<string, int> EvidenceStatusCounts);

public sealed record LaborComplianceReportDto(Guid Id, Guid TenantId, ReportType Type, ReportStatus Status,
    string Title, DateTimeOffset GeneratedAt, Guid GeneratedByUserId, LaborComplianceSnapshotDto Snapshot,
    ContentClassificationDto Classification)
{
    public string Disclaimer => ReportArtifactLanguage.For(Type);
}

public sealed record LaborComplianceSnapshotDto(DateTimeOffset GeneratedAt, Guid? ContractId, string WorkflowStatus,
    string WorkflowDisclaimer, string? ReviewerNotes, LaborDashboardQuery Filters,
    IReadOnlyList<LaborObligationReportDto> Obligations, IReadOnlyList<LaborCategoryReportDto> Categories,
    IReadOnlyList<LaborAssignmentReportDto> Assignments, IReadOnlyList<LaborGapDto> Gaps,
    IReadOnlyList<LaborOverdueItemDto> OverdueItems, IReadOnlyList<LaborEvidenceReferenceDto> EvidenceReferences,
    IReadOnlyDictionary<string, int> EvidenceStatusCounts, bool IncludesSensitiveEmployeeData);

public sealed record LaborObligationReportDto(Guid Id, Guid ContractId, string LaborStandard, string? SourceClause,
    string? WageDeterminationReference, string PlaceOfPerformance, LaborApplicabilityStatus Status,
    LaborApplicabilityReviewStatus ReviewStatus, string? ReviewNotes, DateOnly? ReviewDueAt,
    Guid? WageDeterminationEvidenceItemId);

public sealed record LaborCategoryReportDto(Guid Id, Guid ContractId, string Title,
    string WageDeterminationClassification, decimal HourlyWage, decimal FringeRate, string SourceReference, bool IsActive);

public sealed record LaborAssignmentReportDto(Guid Id, Guid ContractId, Guid EmployeeId, string? EmployeeName,
    string? EmployeeEmail, Guid LaborCategoryId, string LaborCategoryTitle, string WorkLocation,
    LaborAssignmentStatus Status, LaborClassificationReviewStatus ReviewStatus, string? ReviewNotes,
    string SourceReference, IReadOnlyList<LaborEvidenceReferenceDto> EvidenceReferences);

public sealed record LaborGapDto(Guid ContractId, Guid? SourceRecordId, string Description, string GapType);
public sealed record LaborOverdueItemDto(Guid ContractId, Guid SourceRecordId, string ItemType, string Description, DateOnly DueDate);
public sealed record LaborEvidenceReferenceDto(Guid SourceRecordId, Guid EvidenceItemId, LaborEvidenceType EvidenceType,
    string Title, string Status);

public sealed class LaborComplianceReportException(string message) : InvalidOperationException(message);
