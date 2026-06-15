using System.Text;
using Gccs.Application.Reports;
using Gccs.Application.Security;
using Gccs.Domain.Compliance;
using Gccs.Domain.Vendors;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Reports;

public sealed class EfContractObligationMatrixRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : IContractObligationMatrixRepository
{
    public async Task<IReadOnlyList<ContractObligationMatrixRowDto>?> ListCurrentTenantAsync(
        Guid contractId,
        CancellationToken cancellationToken = default)
    {
        var contract = await dbContext.Contracts
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == contractId && candidate.TenantId == tenantContext.TenantId, cancellationToken);
        if (contract is null)
        {
            return null;
        }

        var mappings = await dbContext.Set<ContractClauseObligationEntity>()
            .AsNoTracking()
            .Include(mapping => mapping.ContractClause)
            .Include(mapping => mapping.Obligation)
            .Where(mapping =>
                mapping.ContractClause != null &&
                mapping.ContractClause.ContractId == contractId &&
                mapping.ContractClause.RemovedAt == null &&
                mapping.Obligation != null)
            .ToArrayAsync(cancellationToken);
        var obligationIds = mappings.Select(mapping => mapping.ObligationId).Distinct().ToArray();
        var tasks = await dbContext.ComplianceTasks
            .AsNoTracking()
            .Where(task =>
                task.TenantId == tenantContext.TenantId &&
                task.ContractId == contractId &&
                task.ObligationId != null &&
                obligationIds.Contains(task.ObligationId))
            .OrderBy(task => task.DueAt ?? DateOnly.MaxValue)
            .ThenBy(task => task.CreatedAt)
            .ToArrayAsync(cancellationToken);
        var taskLookup = tasks
            .GroupBy(task => task.ObligationId!)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var evidenceLinks = await dbContext.Set<EvidenceObligationEntity>()
            .AsNoTracking()
            .Where(link => obligationIds.Contains(link.ObligationId))
            .Join(
                dbContext.Set<EvidenceContractEntity>().AsNoTracking().Where(link => link.ContractId == contractId),
                obligationLink => obligationLink.EvidenceItemId,
                contractLink => contractLink.EvidenceItemId,
                (obligationLink, _) => obligationLink)
            .Join(
                dbContext.EvidenceItems.AsNoTracking().Where(evidence => evidence.TenantId == tenantContext.TenantId),
                link => link.EvidenceItemId,
                evidence => evidence.Id,
                (link, evidence) => new { link.ObligationId, evidence.Id, evidence.Name })
            .ToArrayAsync(cancellationToken);
        var evidenceLookup = evidenceLinks
            .GroupBy(link => link.ObligationId)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

        var flowDowns = await dbContext.FlowDownClauses
            .AsNoTracking()
            .Include(flowDown => flowDown.Subcontractor)
            .Where(flowDown =>
                flowDown.Subcontractor != null &&
                flowDown.Subcontractor.TenantId == tenantContext.TenantId &&
                flowDown.ContractId == contractId &&
                flowDown.ObligationId != null &&
                obligationIds.Contains(flowDown.ObligationId))
            .ToArrayAsync(cancellationToken);
        var flowDownLookup = flowDowns
            .GroupBy(flowDown => flowDown.ObligationId!)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group.Select(flowDown => flowDown.Status.ToString()).Distinct().Order().ToArray(),
                StringComparer.Ordinal);

        return mappings
            .Select(mapping =>
            {
                var clause = mapping.ContractClause!;
                var obligation = mapping.Obligation!;
                taskLookup.TryGetValue(obligation.Id, out var task);
                evidenceLookup.TryGetValue(obligation.Id, out var evidence);
                flowDownLookup.TryGetValue(obligation.Id, out var flowDownStatuses);
                return new ContractObligationMatrixRowDto(
                    contract.Id,
                    contract.ContractNumber,
                    contract.Title,
                    clause.Id,
                    clause.ClauseNumber,
                    clause.Title,
                    clause.Source.ToString(),
                    clause.SourceUrl,
                    clause.LastReviewedAt,
                    obligation.Id,
                    obligation.Title,
                    obligation.RequiredAction,
                    task?.OwnerFunction ?? obligation.OwnerFunction,
                    task?.Status.ToString() ?? "NotStarted",
                    obligation.RiskLevel,
                    task?.DueAt,
                    evidence?.Select(item => item.Id).OrderBy(id => id).ToArray() ?? [],
                    evidence?.Select(item => item.Name).Order(StringComparer.Ordinal).ToArray() ?? [],
                    clause.RequiresFlowDown || obligation.RequiresFlowDown,
                    flowDownStatuses ?? [],
                    obligation.SourceUrl,
                    obligation.LastReviewedAt);
            })
            .OrderBy(row => row.ClauseNumber)
            .ThenBy(row => row.ObligationTitle)
            .ToArray();
    }

    public async Task<ContractObligationMatrixExportDto?> ExportCurrentTenantAsync(
        Guid contractId,
        CancellationToken cancellationToken = default)
    {
        var rows = await ListCurrentTenantAsync(contractId, cancellationToken);
        if (rows is null)
        {
            return null;
        }

        var csv = BuildCsv(rows);
        return new ContractObligationMatrixExportDto(
            contractId,
            $"contract-obligation-matrix-{contractId:N}.csv",
            "text/csv",
            rows,
            csv);
    }

    private static string BuildCsv(IReadOnlyList<ContractObligationMatrixRowDto> rows)
    {
        var csv = new StringBuilder();
        csv.AppendLine("contractId,contractNumber,contractTitle,contractClauseId,clauseNumber,clauseTitle,clauseSource,clauseSourceUrl,clauseLastReviewedAt,obligationId,obligationTitle,requiredAction,ownerFunction,status,riskLevel,dueAt,evidenceItemIds,evidenceNames,requiresFlowDown,flowDownStatuses,obligationSourceUrl,obligationLastReviewedAt");
        foreach (var row in rows)
        {
            var values = new[]
            {
                row.ContractId.ToString(),
                row.ContractNumber,
                row.ContractTitle,
                row.ContractClauseId.ToString(),
                row.ClauseNumber,
                row.ClauseTitle,
                row.ClauseSource,
                row.ClauseSourceUrl,
                row.ClauseLastReviewedAt.ToString("O"),
                row.ObligationId,
                row.ObligationTitle,
                row.RequiredAction,
                row.OwnerFunction,
                row.Status,
                row.RiskLevel.ToString(),
                row.DueAt?.ToString("O") ?? string.Empty,
                string.Join("|", row.EvidenceItemIds),
                string.Join("|", row.EvidenceNames),
                row.RequiresFlowDown.ToString(),
                string.Join("|", row.FlowDownStatuses),
                row.ObligationSourceUrl,
                row.ObligationLastReviewedAt.ToString("O")
            };
            csv.AppendLine(string.Join(",", values.Select(Escape)));
        }

        return csv.ToString();
    }

    private static string Escape(string value) =>
        value.Contains('"') || value.Contains(',') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
}
