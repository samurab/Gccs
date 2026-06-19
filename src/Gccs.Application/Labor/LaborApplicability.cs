using Gccs.Application.Audit;
using Gccs.Application.NoCui;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;

namespace Gccs.Application.Labor;

public sealed class LaborApplicabilityService(
    ILaborApplicabilityRepository repository,
    ILaborWageDeterminationUploadGuard guard,
    IAuditEventWriter auditEventWriter)
{
    public async Task<LaborApplicabilityDto> RecordAsync(
        LaborApplicabilityRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        Validate(normalized, requireSource: false);
        var saved = await repository.SaveAsync(normalized, tenantId, actorUserId, cancellationToken);
        await WriteAuditAsync(saved, actorUserId, AuditAction.Created, "Labor applicability was recorded.", cancellationToken);
        return saved;
    }

    public async Task<LaborApplicabilityDto?> UpdateAsync(
        Guid applicabilityId,
        LaborApplicabilityRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = await repository.FindAsync(applicabilityId, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var normalized = Normalize(request);
        Validate(normalized, requireSource: false);
        var updated = await repository.UpdateAsync(applicabilityId, normalized, actorUserId, cancellationToken);
        if (updated is not null)
        {
            await WriteAuditAsync(updated, actorUserId, AuditAction.Updated, "Labor applicability was updated.", cancellationToken);
        }

        return updated;
    }

    public async Task<LaborApplicabilityDto?> ActivateAsync(
        Guid applicabilityId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = await repository.FindAsync(applicabilityId, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        Validate(existing.ToRequest(), requireSource: true);
        var task = new LaborReviewTaskDto(
            Guid.NewGuid(),
            existing.TenantId,
            existing.ContractId,
            "Review labor applicability and wage determination",
            "Confirm SCA/DBA/FAR Part 22 labor obligations, wage determination reference, and place of performance.",
            ComplianceTaskStatus.WaitingForReview.ToString(),
            existing.ContractPeriodEnd);
        var activated = await repository.UpdateStatusAsync(
            applicabilityId,
            LaborApplicabilityStatus.Active,
            task,
            actorUserId,
            cancellationToken);
        if (activated is not null)
        {
            await WriteAuditAsync(activated, actorUserId, AuditAction.Updated, "Labor applicability was activated.", cancellationToken);
        }

        return activated;
    }

    public async Task<LaborApplicabilityDto?> DeactivateAsync(
        Guid applicabilityId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var deactivated = await repository.UpdateStatusAsync(
            applicabilityId,
            LaborApplicabilityStatus.Inactive,
            null,
            actorUserId,
            cancellationToken);
        if (deactivated is not null)
        {
            await WriteAuditAsync(deactivated, actorUserId, AuditAction.Updated, "Labor applicability was deactivated.", cancellationToken);
        }

        return deactivated;
    }

    public async Task<WageDeterminationUploadDto> UploadWageDeterminationAsync(
        WageDeterminationUploadRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        await guard.EnsureAllowedAsync(request, actorUserId, cancellationToken);
        var upload = new WageDeterminationUploadDto(
            Guid.NewGuid(),
            tenantId,
            request.ContractId,
            request.FileName.Trim(),
            request.ContentType.Trim(),
            request.SizeBytes,
            EvidenceUploadGuardrails.AcceptedValidationStatus,
            EvidenceUploadGuardrails.PendingMalwareScanStatus,
            request.Classification,
            request.ContainsPotentialCui,
            DateTimeOffset.UtcNow);
        await WriteWageDeterminationAuditAsync(upload, actorUserId, cancellationToken);
        return upload;
    }

    private static LaborApplicabilityRequest Normalize(LaborApplicabilityRequest request) =>
        request with
        {
            LaborStandard = request.LaborStandard.Trim(),
            PlaceOfPerformance = request.PlaceOfPerformance.Trim(),
            WageDeterminationReference = string.IsNullOrWhiteSpace(request.WageDeterminationReference) ? null : request.WageDeterminationReference.Trim(),
            SourceClause = string.IsNullOrWhiteSpace(request.SourceClause) ? null : request.SourceClause.Trim(),
            Rationale = string.IsNullOrWhiteSpace(request.Rationale) ? null : request.Rationale.Trim(),
            OwnerFunction = string.IsNullOrWhiteSpace(request.OwnerFunction) ? "Contracts/HR" : request.OwnerFunction.Trim()
        };

    private static void Validate(LaborApplicabilityRequest request, bool requireSource)
    {
        if (request.ContractId == Guid.Empty)
        {
            throw new LaborApplicabilityValidationException("Contract is required.");
        }

        if (string.IsNullOrWhiteSpace(request.LaborStandard))
        {
            throw new LaborApplicabilityValidationException("Labor standard is required.");
        }

        if (string.IsNullOrWhiteSpace(request.PlaceOfPerformance))
        {
            throw new LaborApplicabilityValidationException("Place of performance is required.");
        }

        if (request.ContractPeriodEnd < request.ContractPeriodStart)
        {
            throw new LaborApplicabilityValidationException("Contract period end cannot be before start.");
        }

        if (requireSource && string.IsNullOrWhiteSpace(request.SourceClause) && string.IsNullOrWhiteSpace(request.Rationale))
        {
            throw new LaborApplicabilityValidationException("Labor obligation activation requires a source clause or documented rationale.");
        }
    }

    private async Task WriteAuditAsync(
        LaborApplicabilityDto applicability,
        Guid actorUserId,
        AuditAction action,
        string summary,
        CancellationToken cancellationToken)
    {
        await auditEventWriter.WriteAsync(
            applicability.TenantId,
            actorUserId,
            action,
            "LaborApplicability",
            applicability.Id.ToString(),
            summary,
            new Dictionary<string, string>
            {
                ["contractId"] = applicability.ContractId.ToString(),
                ["laborStandard"] = applicability.LaborStandard,
                ["status"] = applicability.Status.ToString(),
                ["sourceClause"] = applicability.SourceClause ?? string.Empty,
                ["wageDeterminationReference"] = applicability.WageDeterminationReference ?? string.Empty,
                ["reviewTaskId"] = applicability.ReviewTask?.Id.ToString() ?? string.Empty
            },
            cancellationToken);
    }

    private async Task WriteWageDeterminationAuditAsync(
        WageDeterminationUploadDto upload,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        await auditEventWriter.WriteAsync(
            upload.TenantId,
            actorUserId,
            AuditAction.Uploaded,
            "WageDetermination",
            upload.Id.ToString(),
            "Wage determination document upload was accepted for scanning.",
            new Dictionary<string, string>
            {
                ["contractId"] = upload.ContractId.ToString(),
                ["classification"] = upload.Classification.ToString(),
                ["malwareScanStatus"] = upload.MalwareScanStatus,
                ["validationStatus"] = upload.ValidationStatus
            },
            cancellationToken);
    }
}

public interface ILaborApplicabilityRepository
{
    Task<LaborApplicabilityDto> SaveAsync(
        LaborApplicabilityRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<LaborApplicabilityDto?> UpdateAsync(
        Guid applicabilityId,
        LaborApplicabilityRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<LaborApplicabilityDto?> FindAsync(Guid applicabilityId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LaborApplicabilityDto>> ListAsync(
        Guid tenantId,
        Guid? contractId = null,
        CancellationToken cancellationToken = default);

    Task<LaborApplicabilityDto?> UpdateStatusAsync(
        Guid applicabilityId,
        LaborApplicabilityStatus status,
        LaborReviewTaskDto? reviewTask,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

public interface ILaborWageDeterminationUploadGuard
{
    Task EnsureAllowedAsync(
        WageDeterminationUploadRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed record LaborApplicabilityRequest(
    Guid ContractId,
    string LaborStandard,
    string PlaceOfPerformance,
    DateOnly ContractPeriodStart,
    DateOnly ContractPeriodEnd,
    string? WageDeterminationReference,
    Guid? WageDeterminationEvidenceItemId,
    string? SourceClause,
    string? Rationale,
    string? OwnerFunction);

public sealed record LaborApplicabilityDto(
    Guid Id,
    Guid TenantId,
    Guid ContractId,
    string LaborStandard,
    string PlaceOfPerformance,
    DateOnly ContractPeriodStart,
    DateOnly ContractPeriodEnd,
    string? WageDeterminationReference,
    Guid? WageDeterminationEvidenceItemId,
    string? SourceClause,
    string? Rationale,
    string OwnerFunction,
    LaborApplicabilityStatus Status,
    LaborReviewTaskDto? ReviewTask,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public LaborApplicabilityRequest ToRequest() =>
        new(
            ContractId,
            LaborStandard,
            PlaceOfPerformance,
            ContractPeriodStart,
            ContractPeriodEnd,
            WageDeterminationReference,
            WageDeterminationEvidenceItemId,
            SourceClause,
            Rationale,
            OwnerFunction);
}

public sealed record LaborReviewTaskDto(
    Guid Id,
    Guid TenantId,
    Guid ContractId,
    string Title,
    string Description,
    string Status,
    DateOnly? DueAt);

public sealed record WageDeterminationUploadRequest(
    Guid ContractId,
    string FileName,
    string ContentType,
    long SizeBytes,
    bool ContainsPotentialCui,
    ContentClassification Classification);

public sealed record WageDeterminationUploadDto(
    Guid Id,
    Guid TenantId,
    Guid ContractId,
    string FileName,
    string ContentType,
    long SizeBytes,
    string ValidationStatus,
    string MalwareScanStatus,
    ContentClassification Classification,
    bool ContainsPotentialCui,
    DateTimeOffset UploadedAt);

public enum LaborApplicabilityStatus
{
    Draft,
    Active,
    Inactive
}

public sealed class LaborApplicabilityValidationException(string message) : InvalidOperationException(message);
