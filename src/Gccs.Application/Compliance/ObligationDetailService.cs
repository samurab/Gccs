using Gccs.Application.Audit;
using Gccs.Application.Notifications;
using Gccs.Domain.Audit;
using Gccs.Domain.Compliance;

namespace Gccs.Application.Compliance;

public sealed class ObligationDetailService(
    IObligationDetailRepository repository,
    IAuditEventWriter auditEventWriter,
    IEnumerable<IAssignmentNotificationRepository> assignmentNotificationRepositories)
{
    private IAssignmentNotificationRepository? AssignmentNotifications => assignmentNotificationRepositories.FirstOrDefault();

    public Task<IReadOnlyList<ObligationAssignmentCandidateDto>> ListAssignmentCandidatesAsync(
        CancellationToken cancellationToken = default) =>
        repository.ListAssignmentCandidatesAsync(cancellationToken);

    public async Task<ContractObligationDetailDto?> FindCurrentTenantAsync(
        Guid contractClauseId,
        string obligationId,
        CancellationToken cancellationToken = default)
    {
        var result = await repository.FindCurrentTenantAsync(contractClauseId, obligationId, cancellationToken);
        return result?.Detail;
    }

    public async Task<ContractObligationDetailDto?> UpdateStatusAsync(
        Guid contractClauseId,
        string obligationId,
        ComplianceTaskStatus status,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var before = await repository.FindCurrentTenantAsync(contractClauseId, obligationId, cancellationToken);
        var updated = await repository.UpdateStatusAsync(contractClauseId, obligationId, status, actorUserId, cancellationToken);

        if (updated is null)
        {
            return null;
        }

        await auditEventWriter.WriteAsync(
            updated.TenantId,
            actorUserId,
            AuditAction.Updated,
            "ContractObligation",
            updated.Detail.Id,
            $"Contract obligation status changed to {updated.Detail.Status}.",
            new Dictionary<string, string>
            {
                ["contractId"] = updated.Detail.ContractId.ToString(),
                ["contractClauseId"] = contractClauseId.ToString(),
                ["obligationId"] = obligationId,
                ["previousStatus"] = before?.Detail.Status ?? "NotStarted",
                ["status"] = updated.Detail.Status
            },
            cancellationToken);

        return updated.Detail;
    }

    public async Task<ContractObligationDetailDto?> AssignOwnerAsync(
        Guid contractClauseId,
        string obligationId,
        AssignContractObligationOwnerRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeAssignment(request);
        ValidateAssignment(normalized);
        var before = await repository.FindCurrentTenantAsync(contractClauseId, obligationId, cancellationToken);
        var updated = await repository.AssignOwnerAsync(contractClauseId, obligationId, normalized, actorUserId, cancellationToken);

        if (updated is null)
        {
            return null;
        }

        var notification = await EmitAssignmentNotificationAsync(updated, normalized.Notify, actorUserId, cancellationToken);

        await auditEventWriter.WriteAsync(
            updated.TenantId,
            actorUserId,
            AuditAction.Updated,
            "ContractObligation",
            updated.Detail.Id,
            $"Contract obligation owner changed to {updated.Detail.OwnerFunction}.",
            new Dictionary<string, string>
            {
                ["contractId"] = updated.Detail.ContractId.ToString(),
                ["contractClauseId"] = contractClauseId.ToString(),
                ["obligationId"] = obligationId,
                ["previousOwner"] = before?.Detail.OwnerFunction ?? string.Empty,
                ["owner"] = updated.Detail.OwnerFunction,
                ["assignmentType"] = updated.Detail.AssignedUserId.HasValue ? "user" : "role",
                ["inAppNotificationCreated"] = notification.InAppNotificationCreated.ToString(),
                ["inAppNotificationRecipientCount"] = notification.InAppRecipientCount.ToString(),
                ["emailNotificationRequested"] = normalized.Notify.ToString(),
                ["emailNotificationQueued"] = notification.EmailDeliveryQueued.ToString()
            },
            cancellationToken);

        return updated.Detail;
    }

    private async Task<AssignmentNotificationEmission> EmitAssignmentNotificationAsync(
        ContractObligationDetailResult updated,
        bool queueEmail,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (AssignmentNotifications is null ||
            updated.Detail.LinkedTasks.FirstOrDefault() is not { } task)
        {
            return new AssignmentNotificationEmission(false, false);
        }

        if (updated.Detail.AssignedUserId is { } assignedUserId)
        {
            return await AssignmentNotifications.EmitTaskAssignmentAsync(
                updated.TenantId,
                task.Id,
                assignedUserId,
                updated.Detail.Title,
                actorUserId,
                queueEmail: queueEmail,
                linkUrl: "/#/obligations",
                cancellationToken: cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(updated.Detail.AssignedRoleName))
        {
            return await AssignmentNotifications.EmitRoleTaskAssignmentAsync(
                updated.TenantId,
                task.Id,
                updated.Detail.AssignedRoleName,
                updated.Detail.Title,
                actorUserId,
                cancellationToken);
        }

        return new AssignmentNotificationEmission(false, false);
    }

    private static AssignContractObligationOwnerRequest NormalizeAssignment(AssignContractObligationOwnerRequest request) =>
        request with
        {
            RoleName = NormalizeRoleName(request.RoleName)
        };

    private static string? NormalizeRoleName(string? roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return null;
        }

        return Gccs.Domain.Identity.RoleCatalog.TryNormalizeRoleName(roleName, out var canonicalRoleName)
            ? canonicalRoleName
            : roleName.Trim();
    }

    private static void ValidateAssignment(AssignContractObligationOwnerRequest request)
    {
        if ((request.UserId.HasValue && !string.IsNullOrWhiteSpace(request.RoleName)) ||
            (!request.UserId.HasValue && string.IsNullOrWhiteSpace(request.RoleName)))
        {
            throw new ObligationAssignmentValidationException("Assign either a tenant user or a role, but not both.");
        }
    }
}

public sealed class ObligationAssignmentValidationException(string message) : InvalidOperationException(message);
