using Gccs.Application.Audit;
using Gccs.Application.Security;
using Gccs.Domain.Audit;

namespace Gccs.Application.Compliance;

public sealed class FedRampControlMappingService(
    IFedRampControlMappingRepository repository,
    ICurrentTenantContext tenantContext,
    IAuditEventWriter auditEventWriter)
{
    public Task<IReadOnlyList<FedRampControlMappingDto>> ListAsync(FedRampGapReportFilter? filter = null, CancellationToken cancellationToken = default) =>
        repository.ListAsync(tenantContext.TenantId, filter, cancellationToken);

    public async Task<FedRampControlMappingDto> CreateAsync(CreateFedRampControlMappingRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        ValidateCreate(request);
        var created = await repository.CreateAsync(tenantContext.TenantId, request, actorUserId, cancellationToken);
        await WriteAuditAsync(created, actorUserId, AuditAction.Created, "FedRAMP readiness control mapping was created.", cancellationToken);
        return created;
    }

    public async Task<FedRampControlMappingDto?> LinkEvidenceAsync(Guid mappingId, FedRampEvidenceLinkRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        ValidateText(request.Label, nameof(request.Label), 200);
        ValidateText(request.Reference, nameof(request.Reference), 600);
        var updated = await repository.LinkEvidenceAsync(tenantContext.TenantId, mappingId, request, actorUserId, cancellationToken);
        if (updated is not null)
        {
            await WriteAuditAsync(updated, actorUserId, AuditAction.Updated, "FedRAMP control evidence link was added.", cancellationToken);
        }

        return updated;
    }

    public async Task<FedRampControlMappingDto?> AddGapAsync(Guid mappingId, FedRampGapRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        ValidateText(request.Rationale, nameof(request.Rationale), 1000);
        ValidateText(request.Owner, nameof(request.Owner), 200);
        var updated = await repository.AddGapAsync(tenantContext.TenantId, mappingId, request, actorUserId, cancellationToken);
        if (updated is not null)
        {
            await WriteAuditAsync(updated, actorUserId, AuditAction.Updated, "FedRAMP control gap was recorded.", cancellationToken);
        }

        return updated;
    }

    public async Task<FedRampControlMappingDto?> ChangeStateAsync(Guid mappingId, FedRampControlReviewRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        ValidateText(request.Reviewer, nameof(request.Reviewer), 200);
        ValidateText(request.ReviewNotes, nameof(request.ReviewNotes), 1200);
        var current = await repository.GetAsync(tenantContext.TenantId, mappingId, cancellationToken);
        if (current is null)
        {
            return null;
        }

        if (request.State is FedRampReviewState.Approved &&
            (string.IsNullOrWhiteSpace(current.Owner) ||
             string.IsNullOrWhiteSpace(request.Reviewer) ||
             request.ReviewDate == default ||
             string.IsNullOrWhiteSpace(current.SourceReference) ||
             (current.EvidenceLinks.Length == 0 && current.Gaps.Length == 0 && string.IsNullOrWhiteSpace(current.GapRationale))))
        {
            throw new FedRampControlMappingValidationException("Approval requires owner, reviewer, review date, source, and evidence or gap rationale.");
        }

        var updated = await repository.ChangeStateAsync(tenantContext.TenantId, mappingId, request, actorUserId, cancellationToken);
        if (updated is not null)
        {
            await WriteAuditAsync(updated, actorUserId, AuditAction.Updated, $"FedRAMP control mapping moved to {updated.ReviewState}.", cancellationToken);
        }

        return updated;
    }

    private static void ValidateCreate(CreateFedRampControlMappingRequest request)
    {
        ValidateText(request.ControlId, nameof(request.ControlId), 80);
        ValidateText(request.Family, nameof(request.Family), 120);
        ValidateText(request.Baseline, nameof(request.Baseline), 120);
        ValidateText(request.Owner, nameof(request.Owner), 200);
        ValidateText(request.ImplementationSummary, nameof(request.ImplementationSummary), 1200);
        ValidateText(request.SourceReference, nameof(request.SourceReference), 600);
        if (request.EvidenceLinks.Length == 0 && string.IsNullOrWhiteSpace(request.GapRationale))
        {
            throw new FedRampControlMappingValidationException("Evidence or gap rationale is required.");
        }
    }

    private static void ValidateText(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new FedRampControlMappingValidationException($"{fieldName} is required.");
        }

        if (value.Trim().Length > maxLength)
        {
            throw new FedRampControlMappingValidationException($"{fieldName} must be {maxLength} characters or fewer.");
        }
    }

    private Task WriteAuditAsync(FedRampControlMappingDto mapping, Guid actorUserId, AuditAction action, string summary, CancellationToken cancellationToken) =>
        auditEventWriter.WriteAsync(
            tenantContext.TenantId,
            actorUserId,
            action,
            "FedRampControlMapping",
            mapping.Id.ToString(),
            summary,
            new Dictionary<string, string> { ["controlId"] = mapping.ControlId, ["reviewState"] = mapping.ReviewState.ToString() },
            cancellationToken);
}

public interface IFedRampControlMappingRepository
{
    Task<IReadOnlyList<FedRampControlMappingDto>> ListAsync(Guid tenantId, FedRampGapReportFilter? filter = null, CancellationToken cancellationToken = default);
    Task<FedRampControlMappingDto?> GetAsync(Guid tenantId, Guid mappingId, CancellationToken cancellationToken = default);
    Task<FedRampControlMappingDto> CreateAsync(Guid tenantId, CreateFedRampControlMappingRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<FedRampControlMappingDto?> LinkEvidenceAsync(Guid tenantId, Guid mappingId, FedRampEvidenceLinkRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<FedRampControlMappingDto?> AddGapAsync(Guid tenantId, Guid mappingId, FedRampGapRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<FedRampControlMappingDto?> ChangeStateAsync(Guid tenantId, Guid mappingId, FedRampControlReviewRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
}

public sealed record CreateFedRampControlMappingRequest(string ControlId, string Family, string Baseline, string Owner, FedRampImplementationStatus ImplementationStatus, string ImplementationSummary, string? InheritedProvider, FedRampEvidenceLinkDto[] EvidenceLinks, string? GapRationale, string SourceReference);
public sealed record FedRampEvidenceLinkRequest(string Label, string Reference, FedRampEvidenceType EvidenceType);
public sealed record FedRampGapRequest(string Rationale, FedRampGapSeverity Severity, string Owner, DateOnly TargetDate);
public sealed record FedRampControlReviewRequest(FedRampReviewState State, string Reviewer, DateOnly ReviewDate, string ReviewNotes);
public sealed record FedRampGapReportFilter(string? Family = null, FedRampGapSeverity? Severity = null, string? Owner = null, DateOnly? TargetDate = null);
public sealed record FedRampEvidenceLinkDto(string Label, string Reference, FedRampEvidenceType EvidenceType);
public sealed record FedRampGapDto(string Rationale, FedRampGapSeverity Severity, string Owner, DateOnly TargetDate, bool IsOpen);

public sealed record FedRampControlMappingDto(Guid Id, Guid TenantId, string ControlId, string Family, string Baseline, string Owner, FedRampImplementationStatus ImplementationStatus, string ImplementationSummary, string? InheritedProvider, FedRampEvidenceLinkDto[] EvidenceLinks, FedRampGapDto[] Gaps, string? GapRationale, string SourceReference, FedRampReviewState ReviewState, string? Reviewer, DateOnly? ReviewDate, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);

public enum FedRampImplementationStatus { Planned, PartiallyImplemented, Implemented, Inherited, Gap }
public enum FedRampReviewState { Draft, InReview, Approved, GapIdentified, AcceptedRisk, Superseded, Archived }
public enum FedRampGapSeverity { Low, Moderate, High, Critical }
public enum FedRampEvidenceType { SecurityControl, OperationsEvidence, AuditLog, EvidenceStorage, Identity, Encryption, IncidentResponse, VulnerabilityManagement }

public sealed class FedRampControlMappingValidationException(string message) : InvalidOperationException(message);
