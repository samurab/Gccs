using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Domain.Audit;

namespace Gccs.Application.Reports;

public sealed class EsrsApplicabilityService(
    IEsrsApplicabilityRepository repository,
    IAuditEventWriter auditEventWriter,
    IApplicationTransaction transaction)
{
    public Task<IReadOnlyList<EsrsApplicabilityDto>?> ListForContractAsync(Guid contractId, CancellationToken cancellationToken = default) =>
        repository.ListForContractAsync(contractId, cancellationToken);

    public async Task<EsrsApplicabilityDto?> ActivateAsync(EsrsApplicabilityRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        Validate(normalized);
        return await transaction.ExecuteAsync(async token =>
        {
            var saved = await repository.CreateAsync(normalized, actorUserId, token);
            if (saved is null) return null;
            await WriteAuditAsync(saved, actorUserId, AuditAction.Created, "eSRS applicability was activated.", token);
            return saved;
        }, cancellationToken);
    }

    public async Task<EsrsApplicabilityDto?> UpdateAsync(Guid applicabilityId, EsrsApplicabilityRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        Validate(normalized);
        return await transaction.ExecuteAsync(async token =>
        {
            var updated = await repository.UpdateAsync(applicabilityId, normalized, actorUserId, token);
            if (updated is null) return null;
            await WriteAuditAsync(updated, actorUserId, AuditAction.Updated, "eSRS applicability was updated.", token);
            return updated;
        }, cancellationToken);
    }

    public async Task<EsrsApplicabilityDto?> UpdateStatusAsync(Guid contractId, Guid applicabilityId, EsrsReportTaskStatus status, Guid actorUserId, CancellationToken cancellationToken = default) =>
        await transaction.ExecuteAsync(async token =>
        {
            if (!Enum.IsDefined(status)) throw new EsrsApplicabilityValidationException("eSRS task status is not supported.");
            var updated = await repository.UpdateStatusAsync(contractId, applicabilityId, status, actorUserId, token);
            if (updated is null) return null;
            await WriteAuditAsync(updated, actorUserId, AuditAction.Updated, "eSRS applicability status was updated.", token);
            return updated;
        }, cancellationToken);

    public static IReadOnlyList<EsrsScheduleTemplateDto> GetDefaultSchedule(int fiscalYear)
    {
        if (fiscalYear is < 2000 or > 2200)
            throw new EsrsApplicabilityValidationException("Fiscal year must be between 2000 and 2200.");
        return
        [
            new("isr-first-half", EsrsReportType.Isr, new(fiscalYear - 1, 10, 1), new(fiscalYear, 3, 31), new(fiscalYear, 4, 30), "FAR 52.219-9", "https://www.acquisition.gov/far/52.219-9", "Suggested ISR semiannual schedule; confirm against the governing subcontracting plan and agency instructions."),
            new("isr-second-half", EsrsReportType.Isr, new(fiscalYear, 4, 1), new(fiscalYear, 9, 30), new(fiscalYear, 10, 30), "FAR 52.219-9", "https://www.acquisition.gov/far/52.219-9", "Suggested ISR semiannual schedule; confirm against the governing subcontracting plan and agency instructions."),
            new("ssr-annual", EsrsReportType.Ssr, new(fiscalYear - 1, 10, 1), new(fiscalYear, 9, 30), new(fiscalYear, 10, 30), "FAR 52.219-9", "https://www.acquisition.gov/far/52.219-9", "Suggested annual SSR schedule. DoD, NASA, commercial-plan, and agency-specific instructions can differ; confirm applicability and dates before activation.")
        ];
    }

    private Task WriteAuditAsync(EsrsApplicabilityDto item, Guid actorUserId, AuditAction action, string summary, CancellationToken token) =>
        auditEventWriter.WriteAsync(item.TenantId, actorUserId, action, "EsrsApplicability", item.Id.ToString(), summary,
            new Dictionary<string, string>
            {
                ["contractId"] = item.ContractId.ToString(), ["taskId"] = item.TaskId.ToString(),
                ["reportType"] = item.ReportType.ToString(), ["periodStart"] = item.PeriodStart.ToString("O"),
                ["periodEnd"] = item.PeriodEnd.ToString("O"), ["dueDate"] = item.DueDate.ToString("O"),
                ["status"] = item.Status.ToString(), ["sourceBackedBy"] = item.SourceClause is not null ? "clause" : "rationale"
            }, token);

    private static EsrsApplicabilityRequest Normalize(EsrsApplicabilityRequest request) => request with
    {
        ContractType = (request.ContractType ?? string.Empty).Trim(), Agency = (request.Agency ?? string.Empty).Trim(),
        SubcontractingPlanType = (request.SubcontractingPlanType ?? string.Empty).Trim(), PrimeOrLowerTierRole = (request.PrimeOrLowerTierRole ?? string.Empty).Trim(),
        SourceClause = NullIfBlank(request.SourceClause), Rationale = NullIfBlank(request.Rationale),
        OwnerFunction = string.IsNullOrWhiteSpace(request.OwnerFunction) ? "Contracts" : request.OwnerFunction.Trim()
    };

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void Validate(EsrsApplicabilityRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.ContractId == Guid.Empty) errors["contractId"] = ["Contract is required for eSRS applicability."];
        if (!Enum.IsDefined(request.ReportType)) errors["reportType"] = ["Report type must be ISR or SSR."];
        Required(errors, "contractType", request.ContractType, 120); Required(errors, "agency", request.Agency, 240);
        Required(errors, "subcontractingPlanType", request.SubcontractingPlanType, 120);
        Required(errors, "primeOrLowerTierRole", request.PrimeOrLowerTierRole, 80); Required(errors, "ownerFunction", request.OwnerFunction!, 120);
        if (request.PeriodEnd < request.PeriodStart) errors["periodEnd"] = ["Reporting period end cannot be before period start."];
        if (request.DueDate < request.PeriodEnd) errors["dueDate"] = ["Due date cannot be before the reporting period ends."];
        if (request.SourceClause is null && request.Rationale is null) errors["source"] = ["A source clause or documented rationale is required before activation."];
        if (request.SourceClause?.Length > 240) errors["sourceClause"] = ["Source clause cannot exceed 240 characters."];
        if (request.Rationale?.Length > 2000) errors["rationale"] = ["Rationale cannot exceed 2000 characters."];
        if (errors.Count > 0) throw new EsrsApplicabilityValidationException(errors);
    }

    private static void Required(Dictionary<string, string[]> errors, string name, string value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) errors[name] = [$"{name} is required."];
        else if (value.Length > max) errors[name] = [$"{name} cannot exceed {max} characters."];
    }
}

public interface IEsrsApplicabilityRepository
{
    Task<IReadOnlyList<EsrsApplicabilityDto>?> ListForContractAsync(Guid contractId, CancellationToken cancellationToken = default);
    Task<EsrsApplicabilityDto?> CreateAsync(EsrsApplicabilityRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<EsrsApplicabilityDto?> UpdateAsync(Guid applicabilityId, EsrsApplicabilityRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<EsrsApplicabilityDto?> UpdateStatusAsync(Guid contractId, Guid applicabilityId, EsrsReportTaskStatus status, Guid actorUserId, CancellationToken cancellationToken = default);
}

public sealed record EsrsApplicabilityRequest(Guid ContractId, string ContractType, string Agency, string SubcontractingPlanType,
    string PrimeOrLowerTierRole, EsrsReportType ReportType, DateOnly PeriodStart, DateOnly PeriodEnd, DateOnly DueDate,
    string? SourceClause, string? Rationale, string? OwnerFunction, Guid? AssignedToUserId = null);

public sealed record EsrsApplicabilityDto(Guid Id, Guid TenantId, Guid ContractId, Guid TaskId, string ContractType,
    string Agency, string SubcontractingPlanType, string PrimeOrLowerTierRole, EsrsReportType ReportType,
    DateOnly PeriodStart, DateOnly PeriodEnd, DateOnly DueDate, string? SourceClause, string? Rationale,
    EsrsReportTaskStatus Status, string OwnerFunction, Guid? AssignedToUserId, Guid ReviewedByUserId,
    DateTimeOffset ReviewedAt, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt, bool IsOverdue);

public sealed record EsrsScheduleTemplateDto(string Key, EsrsReportType ReportType, DateOnly PeriodStart, DateOnly PeriodEnd, DateOnly DueDate, string SourceCitation, string SourceUrl, string Guidance);
public sealed record UpdateEsrsStatusRequest(EsrsReportTaskStatus Status);
public enum EsrsReportType { Isr, Ssr }
public enum EsrsReportTaskStatus { Open, InProgress, Completed, Canceled }

public sealed class EsrsApplicabilityValidationException : InvalidOperationException
{
    public EsrsApplicabilityValidationException(string message) : this(new Dictionary<string, string[]> { ["esrsApplicability"] = [message] }) { }
    public EsrsApplicabilityValidationException(IReadOnlyDictionary<string, string[]> errors) : base(errors.Values.SelectMany(x => x).First()) => Errors = errors;
    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
