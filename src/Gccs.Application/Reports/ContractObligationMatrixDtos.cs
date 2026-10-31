using Gccs.Domain.Common;
using Gccs.Domain.Compliance;

namespace Gccs.Application.Reports;

public sealed record ContractObligationMatrixRowDto(
    Guid ContractId,
    string ContractNumber,
    string ContractTitle,
    Guid ContractClauseId,
    string ClauseNumber,
    string ClauseTitle,
    string ClauseSource,
    string ClauseSourceUrl,
    DateOnly ClauseLastReviewedAt,
    string ObligationId,
    string ObligationTitle,
    string RequiredAction,
    string OwnerFunction,
    string Status,
    RiskLevel RiskLevel,
    DateOnly? DueAt,
    IReadOnlyList<Guid> EvidenceItemIds,
    IReadOnlyList<string> EvidenceNames,
    bool RequiresFlowDown,
    IReadOnlyList<string> FlowDownStatuses,
    string ObligationSourceUrl,
    DateOnly ObligationLastReviewedAt);

public sealed record ContractObligationMatrixExportDto(
    Guid ContractId,
    string FileName,
    string ContentType,
    IReadOnlyList<ContractObligationMatrixRowDto> Rows,
    string Csv);

public interface IContractObligationMatrixRepository
{
    Task<IReadOnlyList<ContractObligationMatrixRowDto>?> ListCurrentTenantAsync(
        Guid contractId,
        CancellationToken cancellationToken = default);

    Task<ContractObligationMatrixExportDto?> ExportCurrentTenantAsync(
        Guid contractId,
        CancellationToken cancellationToken = default);
}
