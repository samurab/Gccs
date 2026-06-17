using System.Text.Json;
using Gccs.Application.Compliance;
using Gccs.Application.Security;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Compliance;

public sealed class EfObligationDashboardRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : IObligationDashboardRepository
{
    public async Task<IReadOnlyList<ObligationDashboardItemDto>> ListCurrentTenantAsync(
        ObligationDashboardQuery query,
        CancellationToken cancellationToken = default)
    {
        var mappings = await dbContext.Set<ContractClauseObligationEntity>()
            .AsNoTracking()
            .Include(mapping => mapping.ContractClause)
            .ThenInclude(clause => clause!.Contract)
            .Include(mapping => mapping.Obligation)
            .Where(mapping =>
                mapping.ContractClause != null &&
                mapping.ContractClause.Contract != null &&
                mapping.ContractClause.Contract.TenantId == tenantContext.TenantId &&
                mapping.ContractClause.RemovedAt == null &&
                mapping.Obligation != null &&
                mapping.Obligation.ReviewState == ReviewState.Published)
            .ToArrayAsync(cancellationToken);

        var contractIds = mappings
            .Select(mapping => mapping.ContractClause!.ContractId)
            .Distinct()
            .ToArray();
        var contractClauseIds = mappings
            .Select(mapping => mapping.ContractClauseId)
            .Distinct()
            .ToArray();
        var obligationIds = mappings
            .Select(mapping => mapping.ObligationId)
            .Distinct()
            .ToArray();
        var evaluations = await dbContext.ObligationApplicabilityEvaluations
            .AsNoTracking()
            .Where(evaluation =>
                evaluation.TenantId == tenantContext.TenantId &&
                contractClauseIds.Contains(evaluation.ContractClauseId) &&
                obligationIds.Contains(evaluation.ObligationId))
            .ToArrayAsync(cancellationToken);
        var evaluationLookup = evaluations
            .GroupBy(evaluation => (evaluation.ContractClauseId, evaluation.ObligationId))
            .ToDictionary(
                group => group.Key,
                group => (
                    Latest: group
                        .OrderByDescending(evaluation => evaluation.EvaluatedAt)
                        .ThenByDescending(evaluation => evaluation.Id)
                        .First(),
                    HistoryCount: group.Count()));
        var tasks = await dbContext.ComplianceTasks
            .AsNoTracking()
            .Where(task =>
                task.TenantId == tenantContext.TenantId &&
                task.ContractId.HasValue &&
                contractIds.Contains(task.ContractId.Value) &&
                task.ObligationId != null &&
                obligationIds.Contains(task.ObligationId) &&
                task.Type == ComplianceTaskType.ObligationAction)
            .OrderBy(task => task.DueAt ?? DateOnly.MaxValue)
            .ThenBy(task => task.CreatedAt)
            .ToArrayAsync(cancellationToken);
        var taskLookup = tasks
            .GroupBy(task => (ContractId: task.ContractId!.Value, ObligationId: task.ObligationId!))
            .ToDictionary(group => group.Key, group => group.First());
        var assignedUserIds = tasks
            .Where(task => task.AssignedToUserId.HasValue)
            .Select(task => task.AssignedToUserId!.Value)
            .Distinct()
            .ToArray();
        var assignedUsers = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.TenantId == tenantContext.TenantId && assignedUserIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => string.IsNullOrWhiteSpace(user.DisplayName) ? user.Email : user.DisplayName, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var items = mappings.Select(mapping =>
        {
            var clause = mapping.ContractClause!;
            var contract = clause.Contract!;
            var obligation = mapping.Obligation!;
            taskLookup.TryGetValue((contract.Id, obligation.Id), out var task);
            var dueAt = task?.DueAt;
            var module = InferModule(obligation.Source, obligation.Title);
            var status = task?.Status.ToString() ?? "NotStarted";
            var assignedUserId = task?.AssignedToUserId;
            var assignedUserDisplayName = assignedUserId.HasValue && assignedUsers.TryGetValue(assignedUserId.Value, out var displayName)
                ? displayName
                : null;
            var assignedRoleName = task is not null &&
                !assignedUserId.HasValue &&
                !string.Equals(task.OwnerFunction, obligation.OwnerFunction, StringComparison.OrdinalIgnoreCase)
                    ? task.OwnerFunction
                    : null;
            var ownerDisplayName = assignedUserDisplayName ?? assignedRoleName ?? task?.OwnerFunction ?? obligation.OwnerFunction;

            return new ObligationDashboardItemDto(
                $"{clause.Id:N}:{obligation.Id}",
                contract.Id,
                contract.ContractNumber,
                contract.Title,
                clause.Id,
                clause.ClauseNumber,
                obligation.Id,
                obligation.Source,
                obligation.SourceUrl,
                obligation.Title,
                obligation.PlainEnglishSummary,
                obligation.RequiredAction,
                ownerDisplayName,
                assignedUserId,
                assignedUserDisplayName,
                assignedRoleName,
                obligation.RiskLevel,
                status,
                dueAt,
                module,
                dueAt.HasValue && dueAt.Value < today && status is not "Done" and not "Canceled",
                obligation.RiskLevel is RiskLevel.High or RiskLevel.Critical,
                ReadEvidenceExamples(obligation.EvidenceExamplesJson),
                obligation.Confidence,
                obligation.LastReviewedAt,
                obligation.RequiresExpertReview,
                evaluationLookup.TryGetValue((clause.Id, obligation.Id), out var evaluation)
                    ? ToApplicabilitySummary(evaluation.Latest, evaluation.HistoryCount)
                    : null);
        });

        items = ApplyFilters(items, query, today);

        return items
            .OrderByDescending(item => item.IsOverdue)
            .ThenByDescending(item => item.IsHighRisk)
            .ThenBy(item => item.DueAt ?? DateOnly.MaxValue)
            .ThenBy(item => item.ContractNumber)
            .ThenBy(item => item.Source)
            .ToArray();
    }

    private static IEnumerable<ObligationDashboardItemDto> ApplyFilters(
        IEnumerable<ObligationDashboardItemDto> items,
        ObligationDashboardQuery query,
        DateOnly today)
    {
        if (query.ContractId.HasValue)
        {
            items = items.Where(item => item.ContractId == query.ContractId.Value);
        }

        if (query.RiskLevel.HasValue)
        {
            items = items.Where(item => item.RiskLevel == query.RiskLevel.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Owner))
        {
            items = items.Where(item => item.OwnerFunction.Contains(query.Owner.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (query.Status.HasValue)
        {
            items = items.Where(item => string.Equals(item.Status, query.Status.Value.ToString(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Module))
        {
            items = items.Where(item => string.Equals(item.Module, query.Module.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Source))
        {
            items = items.Where(item => item.Source.Contains(query.Source.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.DueDate))
        {
            items = ApplyDueDateFilter(items, query.DueDate.Trim(), today);
        }

        return items;
    }

    private static IEnumerable<ObligationDashboardItemDto> ApplyDueDateFilter(
        IEnumerable<ObligationDashboardItemDto> items,
        string dueDate,
        DateOnly today)
    {
        if (string.Equals(dueDate, "overdue", StringComparison.OrdinalIgnoreCase))
        {
            return items.Where(item => item.IsOverdue);
        }

        if (string.Equals(dueDate, "next30", StringComparison.OrdinalIgnoreCase))
        {
            var end = today.AddDays(30);
            return items.Where(item => item.DueAt.HasValue && item.DueAt.Value >= today && item.DueAt.Value <= end);
        }

        if (string.Equals(dueDate, "none", StringComparison.OrdinalIgnoreCase))
        {
            return items.Where(item => !item.DueAt.HasValue);
        }

        return DateOnly.TryParse(dueDate, out var exactDate)
            ? items.Where(item => item.DueAt == exactDate)
            : items;
    }

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

    private static ObligationApplicabilitySummaryDto ToApplicabilitySummary(
        ObligationApplicabilityEvaluationEntity evaluation,
        int historyCount) =>
        new(
            evaluation.State,
            evaluation.Explanation,
            evaluation.SourceRuleId,
            ReadFactLabels(evaluation.FactsUsedJson),
            ReadStringArray(evaluation.MissingFactsJson),
            evaluation.EvaluatedAt,
            historyCount);

    private static IReadOnlyList<string> ReadFactLabels(string value)
    {
        try
        {
            return JsonSerializer.Deserialize<ApplicabilityFactDto[]>(value, new JsonSerializerOptions(JsonSerializerDefaults.Web))?
                .Select(fact => $"{fact.Key}={fact.Value}")
                .ToArray() ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IReadOnlyList<string> ReadStringArray(string value)
    {
        try
        {
            return JsonSerializer.Deserialize<string[]>(value, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? [];
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
