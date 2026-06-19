using Gccs.Application.Audit;
using Gccs.Application.Security;
using Gccs.Domain.Audit;

namespace Gccs.Application.Compliance;

public sealed class CustomerManagedKeyPolicyService(
    ICustomerManagedKeyPolicyRepository repository,
    ICurrentTenantContext tenantContext,
    IAuditEventWriter auditEventWriter)
{
    public async Task<CustomerManagedKeyPolicyDto> RegisterAsync(RegisterCustomerManagedKeyPolicyRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var policy = await repository.CreateAsync(tenantContext.TenantId, request, actorUserId, cancellationToken);
        await WriteAuditAsync(policy, actorUserId, AuditAction.Created, "Customer-managed key policy was registered.", cancellationToken);
        return policy;
    }

    public async Task<CustomerManagedKeyPolicyDto?> ValidateAsync(Guid policyId, CustomerManagedKeyValidationRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var valid = request.IsValid();
        var updated = await repository.RecordValidationAsync(tenantContext.TenantId, policyId, request, valid, actorUserId, cancellationToken);
        if (updated is not null)
        {
            await WriteAuditAsync(updated, actorUserId, valid ? AuditAction.Approved : AuditAction.Rejected, valid ? "Customer-managed key policy validation succeeded." : "Customer-managed key policy validation failed.", cancellationToken);
        }

        return updated;
    }

    public async Task<CustomerManagedKeyPolicyDto?> ActivateAsync(Guid policyId, CustomerManagedKeyValidationRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        if (!request.IsValid())
        {
            var failed = await repository.RecordValidationAsync(tenantContext.TenantId, policyId, request, false, actorUserId, cancellationToken);
            if (failed is not null)
            {
                await WriteAuditAsync(failed, actorUserId, AuditAction.Rejected, "Customer-managed key policy activation failed validation.", cancellationToken);
            }

            throw new CustomerManagedKeyPolicyValidationException("Key activation requires availability, permissions, region match, encryption compatibility, and backup implication validation.");
        }

        await repository.RecordValidationAsync(tenantContext.TenantId, policyId, request, true, actorUserId, cancellationToken);
        var activated = await repository.ChangeStatusAsync(tenantContext.TenantId, policyId, new CustomerManagedKeyStatusRequest(CustomerManagedKeyPolicyStatus.Active, request.Reviewer, "Activated after validation."), actorUserId, cancellationToken);
        if (activated is not null)
        {
            await WriteAuditAsync(activated, actorUserId, AuditAction.Approved, "Customer-managed key policy was activated.", cancellationToken);
        }

        return activated;
    }

    public async Task<CustomerManagedKeyPolicyDto?> ChangeStatusAsync(Guid policyId, CustomerManagedKeyStatusRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var updated = await repository.ChangeStatusAsync(tenantContext.TenantId, policyId, request, actorUserId, cancellationToken);
        if (updated is not null)
        {
            var action = request.Status is CustomerManagedKeyPolicyStatus.Revoked ? AuditAction.Archived : AuditAction.Updated;
            await WriteAuditAsync(updated, actorUserId, action, $"Customer-managed key policy moved to {updated.Status}.", cancellationToken);
        }

        return updated;
    }

    public async Task<CustomerManagedKeyWorkflowDecisionDto?> EvaluateWorkflowAsync(Guid policyId, CustomerManagedKeyWorkflowRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var policy = await repository.GetAsync(tenantContext.TenantId, policyId, cancellationToken);
        if (policy is null)
        {
            return null;
        }

        var allowed = policy.Status is CustomerManagedKeyPolicyStatus.Active or CustomerManagedKeyPolicyStatus.Rotated &&
            policy.LastValidation is { KeyAvailable: true, PermissionsGranted: true, RegionMatches: true, EncryptionCompatible: true, BackupImplicationsAccepted: true };
        var reason = allowed
            ? "Dependent workflow can use the active customer-managed key policy."
            : "Dependent workflow is blocked because the customer-managed key policy is not operational.";

        await auditEventWriter.WriteAsync(
            tenantContext.TenantId,
            actorUserId,
            allowed ? AuditAction.Approved : AuditAction.Rejected,
            "CustomerManagedKeyWorkflow",
            policy.Id.ToString(),
            reason,
            new Dictionary<string, string>
            {
                ["workflow"] = request.Workflow,
                ["status"] = policy.Status.ToString(),
                ["allowed"] = allowed.ToString()
            },
            cancellationToken);

        return new CustomerManagedKeyWorkflowDecisionDto(allowed, reason, policy.Status, request.Workflow);
    }

    private Task WriteAuditAsync(CustomerManagedKeyPolicyDto policy, Guid actorUserId, AuditAction action, string summary, CancellationToken cancellationToken) =>
        auditEventWriter.WriteAsync(
            tenantContext.TenantId,
            actorUserId,
            action,
            "CustomerManagedKeyPolicy",
            policy.Id.ToString(),
            summary,
            new Dictionary<string, string>
            {
                ["status"] = policy.Status.ToString(),
                ["provider"] = policy.Provider,
                ["environment"] = policy.Environment
            },
            cancellationToken);
}

public interface ICustomerManagedKeyPolicyRepository
{
    Task<CustomerManagedKeyPolicyDto?> GetAsync(Guid tenantId, Guid policyId, CancellationToken cancellationToken = default);
    Task<CustomerManagedKeyPolicyDto> CreateAsync(Guid tenantId, RegisterCustomerManagedKeyPolicyRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<CustomerManagedKeyPolicyDto?> RecordValidationAsync(Guid tenantId, Guid policyId, CustomerManagedKeyValidationRequest request, bool valid, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<CustomerManagedKeyPolicyDto?> ChangeStatusAsync(Guid tenantId, Guid policyId, CustomerManagedKeyStatusRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
}

public sealed record RegisterCustomerManagedKeyPolicyRequest(
    string Provider,
    string KeyId,
    string Environment,
    int RotationCadenceDays,
    DateOnly? LastRotationDate,
    DateOnly NextRotationDate,
    string Owner,
    string Approver,
    string EmergencyContact);

public sealed record CustomerManagedKeyValidationRequest(
    bool KeyAvailable,
    bool PermissionsGranted,
    bool RegionMatches,
    bool EncryptionCompatible,
    bool BackupImplicationsAccepted,
    string Reviewer)
{
    public bool IsValid() => KeyAvailable && PermissionsGranted && RegionMatches && EncryptionCompatible && BackupImplicationsAccepted;
}

public sealed record CustomerManagedKeyStatusRequest(CustomerManagedKeyPolicyStatus Status, string Reviewer, string? Notes = null);
public sealed record CustomerManagedKeyWorkflowRequest(string Workflow);
public sealed record CustomerManagedKeyWorkflowDecisionDto(bool Allowed, string Reason, CustomerManagedKeyPolicyStatus Status, string Workflow);
public sealed record CustomerManagedKeyPolicyEventDto(DateTimeOffset OccurredAt, CustomerManagedKeyPolicyStatus Status, string Reviewer, string Summary);

public sealed record CustomerManagedKeyPolicyDto(
    Guid Id,
    Guid TenantId,
    string Provider,
    string KeyId,
    string Environment,
    CustomerManagedKeyPolicyStatus Status,
    int RotationCadenceDays,
    DateOnly? LastRotationDate,
    DateOnly NextRotationDate,
    string Owner,
    string Approver,
    string EmergencyContact,
    CustomerManagedKeyValidationRequest? LastValidation,
    CustomerManagedKeyPolicyEventDto[] History);

public enum CustomerManagedKeyPolicyStatus { Draft, Validated, Active, Rotated, Suspended, Revoked, ValidationFailed }

public sealed class CustomerManagedKeyPolicyValidationException(string message) : InvalidOperationException(message);
