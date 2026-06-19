using Gccs.Application.Audit;
using Gccs.Application.Security;
using Gccs.Domain.Audit;
using Gccs.Domain.Tenancy;

namespace Gccs.Application.Compliance;

public sealed class CuiEnclaveBoundaryService(
    ICuiEnclaveBoundaryRepository repository,
    ICurrentTenantContext tenantContext,
    IAuditEventWriter auditEventWriter)
{
    public async Task<CuiEnclaveBoundaryDto> CreateAsync(CreateCuiEnclaveBoundaryRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        if (request.ApprovedWorkflows.Length == 0)
        {
            throw new CuiEnclaveBoundaryValidationException("At least one approved enclave workflow is required.");
        }

        var enclave = await repository.CreateAsync(tenantContext.TenantId, request, actorUserId, cancellationToken);
        await WriteAuditAsync(enclave, actorUserId, AuditAction.Created, "CUI enclave boundary record was created.", cancellationToken);
        return enclave;
    }

    public async Task<CuiEnclaveBoundaryDto?> ChangeStatusAsync(Guid enclaveId, CuiEnclaveStatusRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        if (request.Status == CuiEnclaveStatus.Active && !request.Readiness.IsActivationReady())
        {
            throw new CuiEnclaveBoundaryValidationException("Enclave activation requires a CuiReady tenant, completed checklist, incident readiness, and current shared responsibility matrix acknowledgement.");
        }

        var updated = await repository.ChangeStatusAsync(tenantContext.TenantId, enclaveId, request, actorUserId, cancellationToken);
        if (updated is not null)
        {
            var action = request.Status is CuiEnclaveStatus.Retired or CuiEnclaveStatus.Revoked ? AuditAction.Archived : AuditAction.Updated;
            await WriteAuditAsync(updated, actorUserId, action, $"CUI enclave boundary moved to {updated.Status}.", cancellationToken);
        }

        return updated;
    }

    public async Task<CuiProcessingDecisionDto?> EvaluateProcessingAsync(Guid enclaveId, CuiProcessingRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var enclave = await repository.GetAsync(tenantContext.TenantId, enclaveId, cancellationToken);
        if (enclave is null)
        {
            return null;
        }

        var allowed = !request.ContainsRealCui ||
            (enclave.Status == CuiEnclaveStatus.Active && enclave.ApprovedWorkflows.Contains(request.Workflow, StringComparer.OrdinalIgnoreCase));
        var reason = allowed
            ? "CUI processing is allowed for the requested enclave workflow."
            : "Real CUI processing is limited to active enclaves and approved enclave workflows.";

        await auditEventWriter.WriteAsync(
            tenantContext.TenantId,
            actorUserId,
            allowed ? AuditAction.Approved : AuditAction.Rejected,
            "CuiEnclaveProcessing",
            enclave.Id.ToString(),
            reason,
            new Dictionary<string, string>
            {
                ["workflow"] = request.Workflow,
                ["status"] = enclave.Status.ToString(),
                ["containsRealCui"] = request.ContainsRealCui.ToString(),
                ["allowed"] = allowed.ToString()
            },
            cancellationToken);

        return new CuiProcessingDecisionDto(allowed, reason, enclave.Status, request.Workflow);
    }

    private Task WriteAuditAsync(CuiEnclaveBoundaryDto enclave, Guid actorUserId, AuditAction action, string summary, CancellationToken cancellationToken) =>
        auditEventWriter.WriteAsync(
            tenantContext.TenantId,
            actorUserId,
            action,
            "CuiEnclaveBoundary",
            enclave.Id.ToString(),
            summary,
            new Dictionary<string, string>
            {
                ["status"] = enclave.Status.ToString(),
                ["environment"] = enclave.Environment,
                ["dataHandlingMode"] = enclave.DataHandlingMode.ToString()
            },
            cancellationToken);
}

public interface ICuiEnclaveBoundaryRepository
{
    Task<CuiEnclaveBoundaryDto?> GetAsync(Guid tenantId, Guid enclaveId, CancellationToken cancellationToken = default);
    Task<CuiEnclaveBoundaryDto> CreateAsync(Guid tenantId, CreateCuiEnclaveBoundaryRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<CuiEnclaveBoundaryDto?> ChangeStatusAsync(Guid tenantId, Guid enclaveId, CuiEnclaveStatusRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
}

public sealed record CreateCuiEnclaveBoundaryRequest(
    string Environment,
    string BoundaryDescription,
    TenantDataPosture DataHandlingMode,
    string[] ApprovedWorkflows,
    string StorageLocation,
    string ComputeBoundary,
    string NetworkRestrictions,
    string LoggingDestination,
    string BackupPolicy,
    string SupportAccessModel,
    string Reviewer);

public sealed record CuiEnclaveReadinessRequest(
    TenantDataPosture TenantDataHandlingMode,
    bool ChecklistApproved,
    bool IncidentReadinessApproved,
    bool SharedResponsibilityMatrixAcknowledged)
{
    public bool IsActivationReady() =>
        TenantDataHandlingMode == TenantDataPosture.CuiReady &&
        ChecklistApproved &&
        IncidentReadinessApproved &&
        SharedResponsibilityMatrixAcknowledged;
}

public sealed record CuiEnclaveStatusRequest(CuiEnclaveStatus Status, string ActorName, CuiEnclaveReadinessRequest Readiness, string? Notes = null);
public sealed record CuiProcessingRequest(string Workflow, bool ContainsRealCui);
public sealed record CuiProcessingDecisionDto(bool Allowed, string Reason, CuiEnclaveStatus EnclaveStatus, string Workflow);

public sealed record CuiEnclaveBoundaryDto(
    Guid Id,
    Guid TenantId,
    string Environment,
    string BoundaryDescription,
    TenantDataPosture DataHandlingMode,
    string[] ApprovedWorkflows,
    string StorageLocation,
    string ComputeBoundary,
    string NetworkRestrictions,
    string LoggingDestination,
    string BackupPolicy,
    string SupportAccessModel,
    string Reviewer,
    CuiEnclaveStatus Status,
    string? LastActor,
    string? LastNotes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public enum CuiEnclaveStatus { Draft, UnderReview, Approved, Active, Suspended, Retired, Revoked }

public sealed class CuiEnclaveBoundaryValidationException(string message) : InvalidOperationException(message);
