using Gccs.Application.Audit;
using Gccs.Domain.Audit;
using Gccs.Domain.Compliance;

namespace Gccs.Application.Reports;

public sealed class EsrsApplicabilityService(
    IEsrsApplicabilityRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public async Task<EsrsApplicabilityDto> ActivateAsync(
        EsrsApplicabilityRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        Validate(normalized);
        var saved = await repository.SaveAsync(normalized, tenantId, actorUserId, cancellationToken);
        await WriteAuditAsync(saved, actorUserId, "eSRS applicability was activated.", cancellationToken);
        return saved;
    }

    public async Task<EsrsApplicabilityDto?> UpdateStatusAsync(
        Guid applicabilityId,
        EsrsReportTaskStatus status,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var updated = await repository.UpdateStatusAsync(applicabilityId, status, actorUserId, cancellationToken);
        if (updated is null)
        {
            return null;
        }

        await WriteAuditAsync(updated, actorUserId, "eSRS applicability status was updated.", cancellationToken);
        return updated;
    }

    public Task<IReadOnlyList<EsrsCalendarItemDto>> ListCalendarItemsAsync(
        Guid tenantId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default) =>
        repository.ListCalendarItemsAsync(tenantId, asOfDate, cancellationToken);

    private async Task WriteAuditAsync(
        EsrsApplicabilityDto applicability,
        Guid actorUserId,
        string summary,
        CancellationToken cancellationToken)
    {
        await auditEventWriter.WriteAsync(
            applicability.TenantId,
            actorUserId,
            AuditAction.Updated,
            "EsrsApplicability",
            applicability.Id.ToString(),
            summary,
            new Dictionary<string, string>
            {
                ["contractId"] = applicability.ContractId.ToString(),
                ["reportType"] = applicability.ReportType.ToString(),
                ["periodStart"] = applicability.PeriodStart.ToString("O"),
                ["periodEnd"] = applicability.PeriodEnd.ToString("O"),
                ["dueDate"] = applicability.DueDate.ToString("O"),
                ["status"] = applicability.Status.ToString(),
                ["sourceClause"] = applicability.SourceClause ?? string.Empty
            },
            cancellationToken);
    }

    private static EsrsApplicabilityRequest Normalize(EsrsApplicabilityRequest request) =>
        request with
        {
            Agency = request.Agency.Trim(),
            SubcontractingPlanType = request.SubcontractingPlanType.Trim(),
            PrimeOrLowerTierRole = request.PrimeOrLowerTierRole.Trim(),
            SourceClause = string.IsNullOrWhiteSpace(request.SourceClause) ? null : request.SourceClause.Trim(),
            Rationale = string.IsNullOrWhiteSpace(request.Rationale) ? null : request.Rationale.Trim(),
            OwnerFunction = string.IsNullOrWhiteSpace(request.OwnerFunction) ? "Contracts" : request.OwnerFunction.Trim()
        };

    private static void Validate(EsrsApplicabilityRequest request)
    {
        if (request.ContractId == Guid.Empty)
        {
            throw new EsrsApplicabilityValidationException("Contract is required for eSRS applicability.");
        }

        if (request.PeriodEnd < request.PeriodStart)
        {
            throw new EsrsApplicabilityValidationException("eSRS reporting period end cannot be before period start.");
        }

        if (string.IsNullOrWhiteSpace(request.SourceClause) && string.IsNullOrWhiteSpace(request.Rationale))
        {
            throw new EsrsApplicabilityValidationException("eSRS applicability requires a source clause or documented rationale.");
        }
    }
}

public interface IEsrsApplicabilityRepository
{
    Task<EsrsApplicabilityDto> SaveAsync(
        EsrsApplicabilityRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<EsrsApplicabilityDto?> UpdateStatusAsync(
        Guid applicabilityId,
        EsrsReportTaskStatus status,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EsrsCalendarItemDto>> ListCalendarItemsAsync(
        Guid tenantId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default);
}

public sealed record EsrsApplicabilityRequest(
    Guid ContractId,
    string Agency,
    string SubcontractingPlanType,
    string PrimeOrLowerTierRole,
    EsrsReportType ReportType,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    DateOnly DueDate,
    string? SourceClause,
    string? Rationale,
    string? OwnerFunction);

public sealed record EsrsApplicabilityDto(
    Guid Id,
    Guid TenantId,
    Guid ContractId,
    string Agency,
    string SubcontractingPlanType,
    string PrimeOrLowerTierRole,
    EsrsReportType ReportType,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    DateOnly DueDate,
    string? SourceClause,
    string? Rationale,
    EsrsReportTaskStatus Status,
    string OwnerFunction,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record EsrsCalendarItemDto(
    string Id,
    Guid TenantId,
    Guid ContractId,
    string Title,
    DateOnly DueDate,
    EsrsReportType ReportType,
    EsrsReportTaskStatus Status,
    string OwnerFunction,
    bool IsOverdue);

public enum EsrsReportType
{
    Isr,
    Ssr
}

public enum EsrsReportTaskStatus
{
    Open,
    InProgress,
    Completed,
    Canceled
}

public sealed class EsrsApplicabilityValidationException(string message) : InvalidOperationException(message);
