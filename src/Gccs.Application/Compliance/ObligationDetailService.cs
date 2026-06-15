using Gccs.Application.Audit;
using Gccs.Domain.Audit;
using Gccs.Domain.Compliance;

namespace Gccs.Application.Compliance;

public sealed class ObligationDetailService(
    IObligationDetailRepository repository,
    IAuditEventWriter auditEventWriter)
{
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
                ["notificationEmitted"] = normalized.Notify.ToString()
            },
            cancellationToken);

        return updated.Detail;
    }

    private static AssignContractObligationOwnerRequest NormalizeAssignment(AssignContractObligationOwnerRequest request) =>
        request with
        {
            RoleName = string.IsNullOrWhiteSpace(request.RoleName) ? null : request.RoleName.Trim()
        };

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
