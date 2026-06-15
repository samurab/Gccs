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
}
