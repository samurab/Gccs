using Gccs.Application.Security;
using Gccs.Application.Tasks;
using Gccs.Domain.Compliance;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Tasks;

public sealed class EfComplianceTaskRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : IComplianceTaskRepository
{
    public async Task<IReadOnlyList<ComplianceTaskDto>> ListCurrentTenantAsync(CancellationToken cancellationToken = default) =>
        await dbContext.ComplianceTasks
            .AsNoTracking()
            .Where(task => task.TenantId == tenantContext.TenantId)
            .OrderBy(task => task.DueAt ?? DateOnly.MaxValue)
            .ThenByDescending(task => task.CreatedAt)
            .Select(task => ToDto(task))
            .ToArrayAsync(cancellationToken);

    public async Task<ComplianceTaskDto?> CreateAsync(
        CreateComplianceTaskRequest request,
        ComplianceTaskStatus status,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = new ComplianceTaskEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            Title = request.Title,
            Description = request.Description,
            Type = ComplianceTaskType.ObligationAction,
            Status = status,
            RiskLevel = request.Priority,
            AssignedToUserId = request.AssignedToUserId,
            OwnerFunction = request.OwnerFunction,
            DueAt = request.DueAt,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = actorUserId
        };

        ApplyLink(entity, request.LinkedEntityType, request.LinkedEntityId);
        dbContext.ComplianceTasks.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<ComplianceTaskDto?> UpdateAsync(
        Guid taskId,
        UpdateComplianceTaskRequest request,
        ComplianceTaskStatus? status,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ComplianceTasks.SingleOrDefaultAsync(
            task => task.TenantId == tenantContext.TenantId && task.Id == taskId,
            cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.Title = request.Title ?? entity.Title;
        entity.Description = request.Description ?? entity.Description;
        entity.Status = status ?? entity.Status;
        entity.RiskLevel = request.Priority ?? entity.RiskLevel;
        entity.AssignedToUserId = request.AssignedToUserId ?? entity.AssignedToUserId;
        entity.OwnerFunction = request.OwnerFunction ?? entity.OwnerFunction;
        entity.DueAt = request.DueAt ?? entity.DueAt;

        if (request.LinkedEntityType is not null)
        {
            ApplyLink(entity, request.LinkedEntityType, request.LinkedEntityId);
        }

        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedByUserId = actorUserId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    private static void ApplyLink(ComplianceTaskEntity entity, string linkedEntityType, string? linkedEntityId)
    {
        entity.ContractId = null;
        entity.ObligationId = null;
        entity.ControlId = null;
        entity.EvidenceItemId = null;

        switch (linkedEntityType.ToLowerInvariant())
        {
            case "contract":
                entity.ContractId = Guid.Parse(linkedEntityId!);
                break;
            case "obligation":
                entity.ObligationId = linkedEntityId;
                break;
            case "control":
                entity.ControlId = linkedEntityId;
                break;
            case "evidence":
                entity.EvidenceItemId = Guid.Parse(linkedEntityId!);
                break;
            case "subcontractor":
                entity.ControlId = $"subcontractor:{linkedEntityId}";
                break;
            case "certification":
                entity.ControlId = $"certification:{linkedEntityId}";
                break;
        }
    }

    private static ComplianceTaskDto ToDto(ComplianceTaskEntity entity)
    {
        var (linkedEntityType, linkedEntityId) = ReadLink(entity);
        return new ComplianceTaskDto(
            entity.Id,
            entity.TenantId,
            entity.Title,
            entity.Description,
            entity.Type,
            ToStatus(entity.Status),
            entity.RiskLevel,
            entity.AssignedToUserId,
            entity.OwnerFunction,
            entity.DueAt,
            linkedEntityType,
            linkedEntityId,
            entity.CreatedAt,
            entity.UpdatedAt);
    }

    private static (string Type, string? Id) ReadLink(ComplianceTaskEntity entity)
    {
        if (entity.ContractId.HasValue)
        {
            return ("contract", entity.ContractId.Value.ToString());
        }

        if (!string.IsNullOrWhiteSpace(entity.ObligationId))
        {
            return ("obligation", entity.ObligationId);
        }

        if (!string.IsNullOrWhiteSpace(entity.ControlId))
        {
            if (entity.ControlId.StartsWith("subcontractor:", StringComparison.OrdinalIgnoreCase))
            {
                return ("subcontractor", entity.ControlId["subcontractor:".Length..]);
            }

            if (entity.ControlId.StartsWith("certification:", StringComparison.OrdinalIgnoreCase))
            {
                return ("certification", entity.ControlId["certification:".Length..]);
            }

            return ("control", entity.ControlId);
        }

        return entity.EvidenceItemId.HasValue
            ? ("evidence", entity.EvidenceItemId.Value.ToString())
            : ("general", null);
    }

    private static string ToStatus(ComplianceTaskStatus status) =>
        status switch
        {
            ComplianceTaskStatus.InProgress => "in_progress",
            ComplianceTaskStatus.WaitingForReview => "waiting_for_review",
            ComplianceTaskStatus.Done => "completed",
            ComplianceTaskStatus.Canceled => "canceled",
            _ => status.ToString().ToLowerInvariant()
        };
}
