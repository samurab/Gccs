using System.Text.Json;
using Gccs.Application.Cmmc;
using Gccs.Application.Security;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Compliance;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Cmmc;

public sealed class EfCmmcPoamRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : ICmmcPoamRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<CmmcPoamItemDto>?> ListCurrentTenantAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        var assessmentExists = await dbContext.Assessments
            .AsNoTracking()
            .AnyAsync(
                assessment => assessment.TenantId == tenantContext.TenantId && assessment.Id == assessmentId,
                cancellationToken);
        if (!assessmentExists)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var items = await QueryPoamItems(assessmentId)
            .OrderBy(item => item.TargetCompletionAt)
            .ThenByDescending(item => item.CreatedAt)
            .ToArrayAsync(cancellationToken);
        return items.Select(item => ToDto(item, today)).ToArray();
    }

    public async Task<CmmcPoamItemDto?> CreateAsync(
        Guid assessmentId,
        UpsertCmmcPoamItemRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var assessment = await FindAssessmentAsync(assessmentId, cancellationToken);
        if (assessment is null || !await ControlIsInAssessmentScopeAsync(assessment.Level, request.ControlId, cancellationToken))
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var item = new PoamItemEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            AssessmentId = assessmentId,
            ControlId = request.ControlId,
            Weakness = request.Weakness,
            PlannedRemediation = request.PlannedRemediation,
            RiskLevel = request.RiskLevel,
            Status = request.Status,
            OwnerUserId = request.OwnerUserId,
            OwnerFunction = request.OwnerFunction,
            TargetCompletionAt = request.TargetCompletionAt,
            CompletedAt = request.CompletedAt,
            CreatedAt = now,
            CreatedByUserId = actorUserId
        };

        dbContext.PoamItems.Add(item);
        ReplaceEvidenceLinks(item, request.EvidenceItemIds);
        var task = await CreateOrUpdateTaskAsync(item, request.RemediationTaskId, actorUserId, now, cancellationToken);
        if (task is null)
        {
            return null;
        }

        item.RemediationTaskId = task.Id;
        SyncControlLinks(assessment, request.ControlId, item.Id, task.Id);
        assessment.UpdatedAt = now;
        assessment.UpdatedByUserId = actorUserId;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await FindPoamDtoAsync(assessmentId, item.Id, cancellationToken);
    }

    public async Task<CmmcPoamItemDto?> UpdateAsync(
        Guid assessmentId,
        Guid poamItemId,
        UpsertCmmcPoamItemRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var assessment = await FindAssessmentAsync(assessmentId, cancellationToken);
        var item = await QueryPoamItems(assessmentId)
            .SingleOrDefaultAsync(candidate => candidate.Id == poamItemId, cancellationToken);
        if (assessment is null ||
            item is null ||
            !await ControlIsInAssessmentScopeAsync(assessment.Level, request.ControlId, cancellationToken))
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var previousControlId = item.ControlId;
        item.ControlId = request.ControlId;
        item.Weakness = request.Weakness;
        item.PlannedRemediation = request.PlannedRemediation;
        item.RiskLevel = request.RiskLevel;
        item.Status = request.Status;
        item.OwnerUserId = request.OwnerUserId;
        item.OwnerFunction = request.OwnerFunction;
        item.TargetCompletionAt = request.TargetCompletionAt;
        item.CompletedAt = request.CompletedAt;
        item.UpdatedAt = now;
        item.UpdatedByUserId = actorUserId;
        ReplaceEvidenceLinks(item, request.EvidenceItemIds);

        var task = await CreateOrUpdateTaskAsync(item, request.RemediationTaskId ?? item.RemediationTaskId, actorUserId, now, cancellationToken);
        if (task is null)
        {
            return null;
        }

        item.RemediationTaskId = task.Id;
        if (!string.Equals(previousControlId, request.ControlId, StringComparison.OrdinalIgnoreCase))
        {
            RemoveControlLinks(assessment, previousControlId, item.Id, task.Id);
        }

        SyncControlLinks(assessment, request.ControlId, item.Id, task.Id);
        assessment.UpdatedAt = now;
        assessment.UpdatedByUserId = actorUserId;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await FindPoamDtoAsync(assessmentId, item.Id, cancellationToken);
    }

    private IQueryable<PoamItemEntity> QueryPoamItems(Guid assessmentId) =>
        dbContext.PoamItems
            .Include(item => item.EvidenceItems)
            .Where(item => item.TenantId == tenantContext.TenantId && item.AssessmentId == assessmentId);

    private async Task<AssessmentEntity?> FindAssessmentAsync(Guid assessmentId, CancellationToken cancellationToken) =>
        await dbContext.Assessments
            .Include(assessment => assessment.Controls)
            .SingleOrDefaultAsync(
                assessment => assessment.TenantId == tenantContext.TenantId && assessment.Id == assessmentId,
                cancellationToken);

    private async Task<bool> ControlIsInAssessmentScopeAsync(
        CmmcLevel level,
        string controlId,
        CancellationToken cancellationToken)
    {
        var controls = dbContext.Controls.AsNoTracking();
        return level switch
        {
            CmmcLevel.Level1 => await controls.AnyAsync(
                control => control.Id == controlId && control.CmmcLevel == CmmcLevel.Level1,
                cancellationToken),
            CmmcLevel.Level2 => await controls.AnyAsync(
                control => control.Id == controlId && (control.CmmcLevel == CmmcLevel.Level1 || control.CmmcLevel == CmmcLevel.Level2),
                cancellationToken),
            _ => await controls.AnyAsync(control => control.Id == controlId, cancellationToken)
        };
    }

    private async Task<ComplianceTaskEntity?> CreateOrUpdateTaskAsync(
        PoamItemEntity item,
        Guid? taskId,
        Guid actorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ComplianceTaskEntity? task = null;
        if (taskId.HasValue)
        {
            task = await dbContext.ComplianceTasks.SingleOrDefaultAsync(
                candidate => candidate.TenantId == tenantContext.TenantId && candidate.Id == taskId.Value,
                cancellationToken);
            if (task is null)
            {
                return null;
            }
        }

        if (task is null)
        {
            task = new ComplianceTaskEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantContext.TenantId,
                Type = ComplianceTaskType.CorrectiveAction,
                CreatedAt = now,
                CreatedByUserId = actorUserId
            };
            dbContext.ComplianceTasks.Add(task);
        }
        else
        {
            task.UpdatedAt = now;
            task.UpdatedByUserId = actorUserId;
        }

        task.Title = Limit($"POA&M: {item.Weakness}", 240);
        task.Description = item.PlannedRemediation;
        task.Status = ToTaskStatus(item.Status);
        task.RiskLevel = item.RiskLevel;
        task.AssignedToUserId = item.OwnerUserId;
        task.OwnerFunction = item.OwnerFunction;
        task.DueAt = item.TargetCompletionAt;
        task.ControlId = item.ControlId;
        task.ContractId = null;
        task.ObligationId = null;
        task.EvidenceItemId = null;
        return task;
    }

    private void ReplaceEvidenceLinks(PoamItemEntity item, IReadOnlyList<Guid> evidenceItemIds)
    {
        item.EvidenceItems.Clear();
        foreach (var evidenceItemId in evidenceItemIds)
        {
            item.EvidenceItems.Add(new PoamEvidenceEntity
            {
                PoamItemId = item.Id,
                EvidenceItemId = evidenceItemId
            });
        }
    }

    private static void SyncControlLinks(AssessmentEntity assessment, string controlId, Guid poamItemId, Guid taskId)
    {
        var control = assessment.Controls.SingleOrDefault(candidate => string.Equals(candidate.ControlId, controlId, StringComparison.OrdinalIgnoreCase));
        if (control is null)
        {
            control = new ControlAssessmentEntity
            {
                AssessmentId = assessment.Id,
                ControlId = controlId
            };
            assessment.Controls.Add(control);
        }

        control.PoamItemIdsJson = AddGuid(control.PoamItemIdsJson, poamItemId);
        control.TaskIdsJson = AddGuid(control.TaskIdsJson, taskId);
    }

    private static void RemoveControlLinks(AssessmentEntity assessment, string controlId, Guid poamItemId, Guid taskId)
    {
        var control = assessment.Controls.SingleOrDefault(candidate => string.Equals(candidate.ControlId, controlId, StringComparison.OrdinalIgnoreCase));
        if (control is null)
        {
            return;
        }

        control.PoamItemIdsJson = RemoveGuid(control.PoamItemIdsJson, poamItemId);
        control.TaskIdsJson = RemoveGuid(control.TaskIdsJson, taskId);
    }

    private async Task<CmmcPoamItemDto?> FindPoamDtoAsync(Guid assessmentId, Guid itemId, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var item = await QueryPoamItems(assessmentId)
            .SingleOrDefaultAsync(candidate => candidate.Id == itemId, cancellationToken);
        return item is null ? null : ToDto(item, today);
    }

    private static CmmcPoamItemDto ToDto(PoamItemEntity entity, DateOnly today) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.AssessmentId,
            entity.ControlId,
            entity.Weakness,
            entity.PlannedRemediation,
            entity.RiskLevel,
            entity.Status,
            entity.OwnerUserId,
            entity.OwnerFunction,
            entity.TargetCompletionAt,
            entity.CompletedAt,
            entity.RemediationTaskId,
            entity.EvidenceItems.Select(link => link.EvidenceItemId).OrderBy(id => id).ToArray(),
            entity.TargetCompletionAt < today && entity.Status is not PoamStatus.Closed and not PoamStatus.AcceptedRisk,
            entity.CreatedAt,
            entity.UpdatedAt);

    private static ComplianceTaskStatus ToTaskStatus(PoamStatus status) =>
        status switch
        {
            PoamStatus.InProgress => ComplianceTaskStatus.InProgress,
            PoamStatus.WaitingForValidation => ComplianceTaskStatus.WaitingForReview,
            PoamStatus.Closed or PoamStatus.AcceptedRisk => ComplianceTaskStatus.Done,
            _ => ComplianceTaskStatus.Open
        };

    private static string AddGuid(string json, Guid value) =>
        JsonSerializer.Serialize(ReadGuidArray(json).Append(value).Distinct().OrderBy(id => id).ToArray(), JsonOptions);

    private static string RemoveGuid(string json, Guid value) =>
        JsonSerializer.Serialize(ReadGuidArray(json).Where(id => id != value).OrderBy(id => id).ToArray(), JsonOptions);

    private static IReadOnlyList<Guid> ReadGuidArray(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Guid[]>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string Limit(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
