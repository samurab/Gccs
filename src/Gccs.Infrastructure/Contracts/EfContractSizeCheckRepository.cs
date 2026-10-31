using System.Text.Json;
using Gccs.Application.Contracts;
using Gccs.Application.Security;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Contracts;

public sealed class EfContractSizeCheckRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : IContractSizeCheckRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<ContractSizeCheckDto>?> ListCurrentTenantAsync(
        Guid contractId,
        CancellationToken cancellationToken = default)
    {
        var contractExists = await dbContext.Contracts.AnyAsync(
            contract => contract.Id == contractId && contract.TenantId == tenantContext.TenantId,
            cancellationToken);
        if (!contractExists)
        {
            return null;
        }

        var checks = await dbContext.ContractSizeChecks
            .AsNoTracking()
            .Where(check => check.TenantId == tenantContext.TenantId && check.ContractId == contractId)
            .OrderByDescending(check => check.RunAt)
            .ToArrayAsync(cancellationToken);
        return checks.Select(ToDto).ToArray();
    }

    public async Task<ContractSizeCheckDto?> RunCurrentTenantAsync(
        Guid contractId,
        ContractSizeCheckRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var contract = await dbContext.Contracts.SingleOrDefaultAsync(
            contract => contract.Id == contractId && contract.TenantId == tenantContext.TenantId,
            cancellationToken);
        if (contract is null)
        {
            return null;
        }

        var naicsCode = request.NaicsCode.Trim();
        var standard = await dbContext.SbaSizeStandards
            .AsNoTracking()
            .Where(record =>
                record.NaicsCode == naicsCode &&
                (record.Status == ReviewState.Approved || record.Status == ReviewState.Published))
            .OrderByDescending(record => record.EffectiveAt)
            .FirstOrDefaultAsync(cancellationToken);
        var missing = new List<string>();
        var result = "ExpertReviewRecommended";
        var metric = standard?.Metric ?? "Unknown";
        decimal? enteredValue = null;

        if (standard is null)
        {
            missing.Add("approvedSizeStandard");
        }
        else
        {
            var metricRequiresReceipts = standard.Metric.Contains("receipt", StringComparison.OrdinalIgnoreCase) ||
                standard.Metric.Contains("revenue", StringComparison.OrdinalIgnoreCase);
            enteredValue = metricRequiresReceipts ? request.AnnualReceipts : request.EmployeeCount;
            if (enteredValue is null)
            {
                result = "InsufficientInformation";
                missing.Add(metricRequiresReceipts ? "annualReceipts" : "employeeCount");
            }
            else
            {
                result = enteredValue <= standard.Threshold ? "LikelySmall" : "OtherThanSmall";
            }
        }

        Guid? expertReviewTaskId = null;
        if (request.CreateExpertReviewTask && result == "ExpertReviewRecommended")
        {
            var task = new ComplianceTaskEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantContext.TenantId,
                ContractId = contractId,
                Title = $"Review size status for NAICS {naicsCode}",
                Description = "Review opportunity size status because the automated size check requires expert review.",
                Type = ComplianceTaskType.ObligationAction,
                Status = ComplianceTaskStatus.Open,
                RiskLevel = RiskLevel.Medium,
                OwnerFunction = string.IsNullOrWhiteSpace(request.OwnerFunction) ? "Compliance" : request.OwnerFunction.Trim(),
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedByUserId = actorUserId
            };
            dbContext.ComplianceTasks.Add(task);
            expertReviewTaskId = task.Id;
        }

        var entity = new ContractSizeCheckEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            ContractId = contractId,
            NaicsCode = naicsCode,
            Result = result,
            Metric = metric,
            Threshold = standard?.Threshold,
            Unit = standard?.Unit,
            EnteredValue = enteredValue,
            MissingInformationJson = JsonSerializer.Serialize(missing.Order().ToArray(), JsonOptions),
            SourceUrl = standard?.SourceUrl,
            SourceEffectiveAt = standard?.EffectiveAt,
            SourceLastReviewedAt = standard?.LastReviewedAt,
            ExpertReviewTaskId = expertReviewTaskId,
            RunAt = DateTimeOffset.UtcNow,
            RunByUserId = actorUserId
        };
        dbContext.ContractSizeChecks.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    private static ContractSizeCheckDto ToDto(ContractSizeCheckEntity entity) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.ContractId,
            entity.NaicsCode,
            entity.Result,
            entity.Metric,
            entity.Threshold,
            entity.Unit,
            entity.EnteredValue,
            ReadStringArray(entity.MissingInformationJson),
            entity.SourceUrl,
            entity.SourceEffectiveAt,
            entity.SourceLastReviewedAt,
            entity.ExpertReviewTaskId,
            entity.RunAt,
            entity.RunByUserId);

    private static IReadOnlyList<string> ReadStringArray(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<string[]>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
