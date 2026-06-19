using Gccs.Application.Audit;
using Gccs.Application.Security;
using Gccs.Domain.Audit;

namespace Gccs.Application.Compliance;

public sealed class CuiEnclaveAccessControlService(
    ICuiEnclaveAccessControlRepository repository,
    ICurrentTenantContext tenantContext,
    IAuditEventWriter auditEventWriter)
{
    public async Task<CuiEnclaveAccessDecisionDto> RecordOperationAsync(CuiEnclaveOperationRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var decision = new CuiEnclaveAccessDecisionDto(Guid.NewGuid(), tenantContext.TenantId, request.EnclaveId, request.Operation, true, "Enclave operation permitted by server-side permission policy.", DateTimeOffset.UtcNow);
        await auditEventWriter.WriteAsync(tenantContext.TenantId, actorUserId, AuditAction.Approved, "CuiEnclaveAccess", decision.Id.ToString(), $"{request.Operation} operation was permitted.", Metadata(request.EnclaveId, request.Operation.ToString()), cancellationToken);
        return decision;
    }

    public async Task<CuiEnclaveSupportAccessDto> RequestSupportAccessAsync(CuiEnclaveSupportAccessRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason) || string.IsNullOrWhiteSpace(request.Scope) || string.IsNullOrWhiteSpace(request.Approver) || request.DurationMinutes <= 0)
        {
            throw new CuiEnclaveAccessValidationException("Support access requires reason, scope, approver, and positive duration.");
        }

        var access = await repository.CreateSupportAccessAsync(tenantContext.TenantId, request, cancellationToken);
        await auditEventWriter.WriteAsync(tenantContext.TenantId, actorUserId, AuditAction.Approved, "CuiEnclaveSupportAccess", access.Id.ToString(), "Just-in-time enclave support access was approved.", Metadata(request.EnclaveId, "support"), cancellationToken);
        return access;
    }

    public async Task<CuiEnclaveExportDto> CreateExportAsync(CuiEnclaveExportRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        if (!request.IsPolicyCompliant())
        {
            throw new CuiEnclaveAccessValidationException("Enclave export requires allowed package type, allowed recipient, watermarking, encryption, and approval.");
        }

        var export = await repository.CreateExportAsync(tenantContext.TenantId, request, cancellationToken);
        await auditEventWriter.WriteAsync(tenantContext.TenantId, actorUserId, AuditAction.Exported, "CuiEnclaveExport", export.Id.ToString(), "CUI enclave export package was generated.", Metadata(request.EnclaveId, request.PackageType), cancellationToken);
        return export;
    }

    public async Task<CuiEnclaveEmergencyAccessDto> RequestEmergencyAccessAsync(CuiEnclaveEmergencyAccessRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        if (!request.IsAllowed())
        {
            throw new CuiEnclaveAccessValidationException("Emergency access requires elevated approval, incident linkage, time limit, and post-access review.");
        }

        var access = await repository.CreateEmergencyAccessAsync(tenantContext.TenantId, request, cancellationToken);
        await auditEventWriter.WriteAsync(tenantContext.TenantId, actorUserId, AuditAction.Approved, "CuiEnclaveEmergencyAccess", access.Id.ToString(), "Emergency CUI enclave access was approved.", Metadata(request.EnclaveId, request.IncidentId), cancellationToken);
        return access;
    }

    public async Task<CuiEnclaveSupportAccessDto?> ExpireSupportAccessAsync(Guid accessId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var expired = await repository.ExpireSupportAccessAsync(tenantContext.TenantId, accessId, cancellationToken);
        if (expired is not null)
        {
            await auditEventWriter.WriteAsync(tenantContext.TenantId, actorUserId, AuditAction.Expired, "CuiEnclaveSupportAccess", expired.Id.ToString(), "Just-in-time enclave support access expired.", Metadata(expired.EnclaveId, "expired"), cancellationToken);
        }

        return expired;
    }

    public async Task<CuiEnclaveEmergencyAccessDto?> CompletePostAccessReviewAsync(Guid accessId, string reviewer, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var reviewed = await repository.CompleteEmergencyReviewAsync(tenantContext.TenantId, accessId, reviewer, cancellationToken);
        if (reviewed is not null)
        {
            await auditEventWriter.WriteAsync(tenantContext.TenantId, actorUserId, AuditAction.Updated, "CuiEnclaveEmergencyAccess", reviewed.Id.ToString(), "Emergency access post-access review was completed.", Metadata(reviewed.EnclaveId, reviewer), cancellationToken);
        }

        return reviewed;
    }

    private static Dictionary<string, string> Metadata(Guid enclaveId, string value) =>
        new() { ["enclaveId"] = enclaveId.ToString(), ["value"] = value };
}

public interface ICuiEnclaveAccessControlRepository
{
    Task<CuiEnclaveSupportAccessDto> CreateSupportAccessAsync(Guid tenantId, CuiEnclaveSupportAccessRequest request, CancellationToken cancellationToken = default);
    Task<CuiEnclaveSupportAccessDto?> ExpireSupportAccessAsync(Guid tenantId, Guid accessId, CancellationToken cancellationToken = default);
    Task<CuiEnclaveExportDto> CreateExportAsync(Guid tenantId, CuiEnclaveExportRequest request, CancellationToken cancellationToken = default);
    Task<CuiEnclaveEmergencyAccessDto> CreateEmergencyAccessAsync(Guid tenantId, CuiEnclaveEmergencyAccessRequest request, CancellationToken cancellationToken = default);
    Task<CuiEnclaveEmergencyAccessDto?> CompleteEmergencyReviewAsync(Guid tenantId, Guid accessId, string reviewer, CancellationToken cancellationToken = default);
}

public sealed record CuiEnclaveOperationRequest(Guid EnclaveId, CuiEnclaveOperation Operation);
public sealed record CuiEnclaveAccessDecisionDto(Guid Id, Guid TenantId, Guid EnclaveId, CuiEnclaveOperation Operation, bool Allowed, string Reason, DateTimeOffset OccurredAt);

public sealed record CuiEnclaveSupportAccessRequest(Guid EnclaveId, string Reason, string Scope, string Approver, int DurationMinutes, string SessionLog);
public sealed record CuiEnclaveSupportAccessDto(Guid Id, Guid TenantId, Guid EnclaveId, string Reason, string Scope, string Approver, DateTimeOffset GrantedAt, DateTimeOffset ExpiresAt, string SessionLog, bool Expired);

public sealed record CuiEnclaveExportRequest(Guid EnclaveId, string PackageType, string Recipient, bool RecipientAllowed, bool Watermarked, bool Encrypted, bool ApprovalGranted)
{
    private static readonly HashSet<string> AllowedPackageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "EvidencePackage",
        "AuditTrail",
        "ReadinessPackage"
    };

    public bool IsPolicyCompliant() =>
        AllowedPackageTypes.Contains(PackageType) &&
        !string.IsNullOrWhiteSpace(Recipient) &&
        RecipientAllowed &&
        Watermarked &&
        Encrypted &&
        ApprovalGranted;
}

public sealed record CuiEnclaveExportDto(Guid Id, Guid TenantId, Guid EnclaveId, string PackageType, string Recipient, bool Watermarked, bool Encrypted, DateTimeOffset GeneratedAt);

public sealed record CuiEnclaveEmergencyAccessRequest(Guid EnclaveId, bool ElevatedApproval, string IncidentId, int DurationMinutes, bool PostAccessReviewRequired, string Approver)
{
    public bool IsAllowed() =>
        ElevatedApproval &&
        !string.IsNullOrWhiteSpace(IncidentId) &&
        DurationMinutes is > 0 and <= 240 &&
        PostAccessReviewRequired &&
        !string.IsNullOrWhiteSpace(Approver);
}

public sealed record CuiEnclaveEmergencyAccessDto(Guid Id, Guid TenantId, Guid EnclaveId, string IncidentId, string Approver, DateTimeOffset GrantedAt, DateTimeOffset ExpiresAt, bool PostAccessReviewRequired, string? PostAccessReviewer);
public sealed record CuiEnclavePostAccessReviewRequest(string Reviewer);

public enum CuiEnclaveOperation { View, Upload, Download, Export, Approve, SupportAccess, EmergencyAccess }

public sealed class CuiEnclaveAccessValidationException(string message) : InvalidOperationException(message);
