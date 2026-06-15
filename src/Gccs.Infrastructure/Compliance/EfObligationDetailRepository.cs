using System.Text.Json;
using Gccs.Application.Compliance;
using Gccs.Application.Security;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Compliance;

public sealed class EfObligationDetailRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : IObligationDetailRepository
{
    public async Task<ContractObligationDetailResult?> FindCurrentTenantAsync(
        Guid contractClauseId,
        string obligationId,
        CancellationToken cancellationToken = default) =>
        await BuildDetailAsync(contractClauseId, obligationId, cancellationToken);

    public async Task<ContractObligationDetailResult?> UpdateStatusAsync(
        Guid contractClauseId,
        string obligationId,
        ComplianceTaskStatus status,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var mapping = await FindMappingAsync(contractClauseId, obligationId, cancellationToken);
        if (mapping is null)
        {
            return null;
        }

        var contract = mapping.ContractClause!.Contract!;
        var obligation = mapping.Obligation!;
        var task = await dbContext.ComplianceTasks
            .Where(task =>
                task.TenantId == tenantContext.TenantId &&
                task.ContractId == contract.Id &&
                task.ObligationId == obligationId &&
                task.Type == ComplianceTaskType.ObligationAction)
            .OrderBy(task => task.DueAt ?? DateOnly.MaxValue)
            .ThenBy(task => task.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        if (task is null)
        {
            task = new ComplianceTaskEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantContext.TenantId,
                Title = obligation.Title,
                Description = obligation.RequiredAction,
                Type = ComplianceTaskType.ObligationAction,
                RiskLevel = obligation.RiskLevel,
                OwnerFunction = obligation.OwnerFunction,
                ContractId = contract.Id,
                ObligationId = obligationId,
                CreatedAt = now,
                CreatedByUserId = actorUserId
            };
            dbContext.ComplianceTasks.Add(task);
        }

        task.Status = status;
        task.UpdatedAt = now;
        task.UpdatedByUserId = actorUserId;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await BuildDetailAsync(contractClauseId, obligationId, cancellationToken);
    }

    private async Task<ContractObligationDetailResult?> BuildDetailAsync(
        Guid contractClauseId,
        string obligationId,
        CancellationToken cancellationToken)
    {
        var mapping = await FindMappingAsync(contractClauseId, obligationId, cancellationToken);
        if (mapping is null)
        {
            return null;
        }

        var clause = mapping.ContractClause!;
        var contract = clause.Contract!;
        var obligation = mapping.Obligation!;
        var tasks = await dbContext.ComplianceTasks
            .AsNoTracking()
            .Where(task =>
                task.TenantId == tenantContext.TenantId &&
                task.ContractId == contract.Id &&
                task.ObligationId == obligationId &&
                task.Type == ComplianceTaskType.ObligationAction)
            .OrderBy(task => task.DueAt ?? DateOnly.MaxValue)
            .ThenBy(task => task.CreatedAt)
            .ToArrayAsync(cancellationToken);
        var linkedEvidence = await dbContext.Set<EvidenceObligationEntity>()
            .AsNoTracking()
            .Include(link => link.EvidenceItem)
            .Where(link =>
                link.ObligationId == obligationId &&
                link.EvidenceItem != null &&
                link.EvidenceItem.TenantId == tenantContext.TenantId)
            .Select(link => link.EvidenceItem!)
            .OrderBy(evidence => evidence.ExpiresAt ?? DateOnly.MaxValue)
            .ThenBy(evidence => evidence.Name)
            .ToArrayAsync(cancellationToken);
        var primaryTask = tasks.FirstOrDefault();
        var status = primaryTask?.Status.ToString() ?? "NotStarted";

        var detail = new ContractObligationDetailDto(
            $"{clause.Id:N}:{obligation.Id}",
            contract.Id,
            contract.ContractNumber,
            contract.Title,
            clause.Id,
            clause.ClauseNumber,
            clause.Title,
            obligation.Id,
            obligation.Source,
            obligation.SourceUrl,
            obligation.Title,
            obligation.PlainEnglishSummary,
            obligation.TriggerCondition,
            obligation.RequiredAction,
            obligation.OwnerFunction,
            obligation.RiskLevel,
            status,
            primaryTask?.DueAt,
            InferModule(obligation.Source, obligation.Title),
            obligation.RequiresFlowDown,
            obligation.FlowDownRequirement,
            ReadEvidenceExamples(obligation.EvidenceExamplesJson),
            obligation.Confidence,
            obligation.LastReviewedAt,
            obligation.RequiresExpertReview,
            tasks.Select(task => new LinkedObligationTaskDto(
                task.Id,
                task.Title,
                task.Status.ToString(),
                task.DueAt,
                task.OwnerFunction,
                task.RiskLevel)).ToArray(),
            linkedEvidence.Select(evidence => new LinkedObligationEvidenceDto(
                evidence.Id,
                evidence.Name,
                evidence.Status,
                evidence.Type,
                evidence.ExpiresAt,
                evidence.OriginalFileName)).ToArray());

        return new ContractObligationDetailResult(contract.TenantId, detail);
    }

    private async Task<ContractClauseObligationEntity?> FindMappingAsync(
        Guid contractClauseId,
        string obligationId,
        CancellationToken cancellationToken) =>
        await dbContext.Set<ContractClauseObligationEntity>()
            .Include(mapping => mapping.ContractClause)
            .ThenInclude(clause => clause!.Contract)
            .Include(mapping => mapping.Obligation)
            .SingleOrDefaultAsync(
                mapping =>
                    mapping.ContractClauseId == contractClauseId &&
                    mapping.ObligationId == obligationId &&
                    mapping.ContractClause != null &&
                    mapping.ContractClause.Contract != null &&
                    mapping.ContractClause.Contract.TenantId == tenantContext.TenantId &&
                    mapping.ContractClause.RemovedAt == null &&
                    mapping.Obligation != null &&
                    mapping.Obligation.ReviewState == ReviewState.Published,
                cancellationToken);

    private static string InferModule(string source, string title)
    {
        var value = $"{source} {title}";
        if (value.Contains("CMMC", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("32 CFR", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("NIST", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("204-21", StringComparison.OrdinalIgnoreCase))
        {
            return "Cybersecurity";
        }

        if (value.Contains("222-", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("labor", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("wage", StringComparison.OrdinalIgnoreCase))
        {
            return "Labor";
        }

        if (value.Contains("204-25", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("204-27", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("telecom", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("ByteDance", StringComparison.OrdinalIgnoreCase))
        {
            return "Supply chain";
        }

        return "Contract";
    }

    private static IReadOnlyList<string> ReadEvidenceExamples(string value)
    {
        try
        {
            using var document = JsonDocument.Parse(value);
            return document.RootElement.EnumerateArray()
                .Select(ReadEvidenceExample)
                .Where(example => !string.IsNullOrWhiteSpace(example))
                .ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string ReadEvidenceExample(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            return element.GetString() ?? string.Empty;
        }

        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty("name", out var name) &&
            name.ValueKind == JsonValueKind.String)
        {
            return name.GetString() ?? string.Empty;
        }

        return string.Empty;
    }
}
