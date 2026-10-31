using Gccs.Application.Audit;
using Gccs.Domain.Audit;

namespace Gccs.Application.Contracts;

public sealed class ContractSizeCheckService(
    IContractSizeCheckRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public Task<IReadOnlyList<ContractSizeCheckDto>?> ListCurrentTenantAsync(
        Guid contractId,
        CancellationToken cancellationToken = default) =>
        repository.ListCurrentTenantAsync(contractId, cancellationToken);

    public async Task<ContractSizeCheckDto?> RunCurrentTenantAsync(
        Guid contractId,
        ContractSizeCheckRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var result = await repository.RunCurrentTenantAsync(contractId, request, actorUserId, cancellationToken);
        if (result is null)
        {
            return null;
        }

        await auditEventWriter.WriteAsync(
            result.TenantId,
            actorUserId,
            AuditAction.Created,
            "ContractSizeCheck",
            result.Id.ToString(),
            $"Ran contract size check for NAICS {result.NaicsCode}.",
            new Dictionary<string, string>
            {
                ["contractId"] = result.ContractId.ToString(),
                ["naicsCode"] = result.NaicsCode,
                ["result"] = result.Result
            },
            cancellationToken);

        return result;
    }
}

public interface IContractSizeCheckRepository
{
    Task<IReadOnlyList<ContractSizeCheckDto>?> ListCurrentTenantAsync(
        Guid contractId,
        CancellationToken cancellationToken = default);

    Task<ContractSizeCheckDto?> RunCurrentTenantAsync(
        Guid contractId,
        ContractSizeCheckRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed record ContractSizeCheckRequest(
    string NaicsCode,
    decimal? AnnualReceipts,
    int? EmployeeCount,
    bool CreateExpertReviewTask = false,
    string? OwnerFunction = null);

public sealed record ContractSizeCheckDto(
    Guid Id,
    Guid TenantId,
    Guid ContractId,
    string NaicsCode,
    string Result,
    string Metric,
    decimal? Threshold,
    string? Unit,
    decimal? EnteredValue,
    IReadOnlyList<string> MissingInformation,
    string? SourceUrl,
    DateOnly? SourceEffectiveAt,
    DateOnly? SourceLastReviewedAt,
    Guid? ExpertReviewTaskId,
    DateTimeOffset RunAt,
    Guid RunByUserId);
