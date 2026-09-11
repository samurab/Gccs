using Gccs.Application.Labor;
using Gccs.Application.Security;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Labor;

public sealed class EfLaborApplicabilityRepository(GccsDbContext db, ICurrentTenantContext tenantContext)
    : ILaborApplicabilityRepository
{
    public async Task<IReadOnlyList<LaborApplicabilityDto>?> ListForContractAsync(Guid contractId, CancellationToken cancellationToken = default)
    {
        if (!await ContractExistsAsync(contractId, cancellationToken)) return null;
        var rows = await Query().Include(x => x.Task).Include(x => x.SourceContractClause)
            .Where(x => x.ContractId == contractId)
            .OrderBy(x => x.ContractPeriodEnd).ThenBy(x => x.CreatedAt)
            .ToArrayAsync(cancellationToken);
        return rows.Select(ToDto).ToArray();
    }

    public async Task<LaborApplicabilityDto?> CreateAsync(LaborApplicabilityRequest request, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        if (!await ContractExistsAsync(request.ContractId, cancellationToken)) return null;
        await ValidateReferencesAsync(request, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var entity = new LaborApplicabilityEntity
        {
            Id = Guid.NewGuid(), TenantId = tenantContext.TenantId, ContractId = request.ContractId,
            Status = LaborApplicabilityStatus.Draft, CreatedAt = now, CreatedByUserId = actorUserId
        };
        Apply(entity, request, actorUserId, now, isCreate: true);
        db.LaborApplicabilities.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<LaborApplicabilityDto?> UpdateAsync(Guid contractId, Guid applicabilityId, LaborApplicabilityRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var entity = await Query(tracking: true).Include(x => x.Task).Include(x => x.SourceContractClause)
            .SingleOrDefaultAsync(x => x.Id == applicabilityId && x.ContractId == contractId, cancellationToken);
        if (entity is null) return null;
        await ValidateReferencesAsync(request, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        Apply(entity, request, actorUserId, now, isCreate: false);
        if (entity.Status == LaborApplicabilityStatus.Active)
            LaborApplicabilityRules.Validate(ToRequest(entity), requireActivationSource: true);
        if (entity.Task is not null) ApplyTask(entity.Task, entity, actorUserId, now);
        await SaveWithConflictAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<LaborApplicabilityDto?> FindAsync(Guid contractId, Guid applicabilityId, CancellationToken cancellationToken = default)
    {
        var entity = await Query().Include(x => x.Task).Include(x => x.SourceContractClause)
            .SingleOrDefaultAsync(x => x.Id == applicabilityId && x.ContractId == contractId, cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<IReadOnlyList<LaborApplicabilityDto>> ListAsync(Guid tenantId, Guid? contractId = null, CancellationToken cancellationToken = default)
    {
        var rows = await Query().Include(x => x.Task).Include(x => x.SourceContractClause)
            .Where(x => contractId == null || x.ContractId == contractId)
            .OrderBy(x => x.ContractPeriodEnd).ToArrayAsync(cancellationToken);
        return rows.Select(ToDto).ToArray();
    }

    public async Task<LaborApplicabilityDto?> UpdateStatusAsync(Guid contractId, Guid applicabilityId, LaborApplicabilityStatus status, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var entity = await Query(tracking: true).Include(x => x.Task).Include(x => x.SourceContractClause)
            .SingleOrDefaultAsync(x => x.Id == applicabilityId && x.ContractId == contractId, cancellationToken);
        if (entity is null) return null;
        if (!Enum.IsDefined(status)) throw new LaborApplicabilityValidationException("Labor applicability status is not supported.");
        if (status == LaborApplicabilityStatus.Active)
        {
            LaborApplicabilityRules.Validate(ToRequest(entity), requireActivationSource: true);
            await ValidateReferencesAsync(ToRequest(entity), cancellationToken);
            if (entity.Task is null)
            {
                entity.Task = new ComplianceTaskEntity
                {
                    Id = Guid.NewGuid(), TenantId = tenantContext.TenantId, Type = ComplianceTaskType.ObligationAction,
                    RiskLevel = RiskLevel.High, CreatedAt = DateTimeOffset.UtcNow, CreatedByUserId = actorUserId
                };
                entity.TaskId = entity.Task.Id;
                db.ComplianceTasks.Add(entity.Task);
            }
            entity.Task.Status = ComplianceTaskStatus.WaitingForReview;
            ApplyTask(entity.Task, entity, actorUserId, DateTimeOffset.UtcNow);
        }
        else if (entity.Task is not null)
        {
            entity.Task.Status = ComplianceTaskStatus.Canceled;
            entity.Task.UpdatedAt = DateTimeOffset.UtcNow;
            entity.Task.UpdatedByUserId = actorUserId;
        }
        entity.Status = status;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedByUserId = actorUserId;
        await SaveWithConflictAsync(cancellationToken);
        return ToDto(entity);
    }

    private async Task SaveWithConflictAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new LaborApplicabilityConflictException();
        }
    }

    private IQueryable<LaborApplicabilityEntity> Query(bool tracking = false) =>
        (tracking ? db.LaborApplicabilities : db.LaborApplicabilities.AsNoTracking())
            .Where(x => x.TenantId == tenantContext.TenantId);

    private Task<bool> ContractExistsAsync(Guid contractId, CancellationToken token) =>
        db.Contracts.AsNoTracking().AnyAsync(x => x.Id == contractId && x.TenantId == tenantContext.TenantId, token);

    private async Task ValidateReferencesAsync(LaborApplicabilityRequest request, CancellationToken token)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.SourceContractClauseId.HasValue && !await db.Set<ContractClauseEntity>().AsNoTracking()
            .AnyAsync(x => x.Id == request.SourceContractClauseId && x.ContractId == request.ContractId &&
                x.Contract!.TenantId == tenantContext.TenantId && x.RemovedAt == null, token))
            errors["sourceContractClauseId"] = ["The source clause was not found on the current-tenant contract."];

        if (request.WageDeterminationEvidenceItemId.HasValue)
        {
            var evidenceId = request.WageDeterminationEvidenceItemId.Value;
            var validEvidence = await db.EvidenceItems.AsNoTracking().AnyAsync(x => x.Id == evidenceId && x.TenantId == tenantContext.TenantId, token) &&
                await db.Set<EvidenceContractEntity>().AsNoTracking().AnyAsync(x => x.EvidenceItemId == evidenceId && x.ContractId == request.ContractId && x.Contract!.TenantId == tenantContext.TenantId, token);
            if (!validEvidence) errors["wageDeterminationEvidenceItemId"] = ["Wage determination evidence must belong to the current tenant and be linked to this contract."];
        }
        if (errors.Count > 0) throw new LaborApplicabilityValidationException(errors);
    }

    private static void Apply(LaborApplicabilityEntity entity, LaborApplicabilityRequest request, Guid actorUserId, DateTimeOffset now, bool isCreate)
    {
        entity.ScaApplicable = request.ScaApplicable; entity.DbaApplicable = request.DbaApplicable;
        entity.OtherFarPart22Obligations = request.OtherFarPart22Obligations;
        entity.PlaceOfPerformance = request.PlaceOfPerformance; entity.ContractPeriodStart = request.ContractPeriodStart;
        entity.ContractPeriodEnd = request.ContractPeriodEnd; entity.WageDeterminationReference = request.WageDeterminationReference;
        entity.WageDeterminationEvidenceItemId = request.WageDeterminationEvidenceItemId;
        entity.SourceContractClauseId = request.SourceContractClauseId; entity.SourceClause = request.SourceClause;
        entity.Rationale = request.Rationale; entity.OwnerFunction = request.OwnerFunction ?? "Contracts/HR";
        entity.ReviewStatus = request.ReviewStatus; entity.ReviewNotes = request.ReviewNotes;
        var reviewed = request.ReviewStatus is LaborApplicabilityReviewStatus.Reviewed or LaborApplicabilityReviewStatus.Rejected;
        entity.ReviewedByUserId = reviewed ? actorUserId : null; entity.ReviewedAt = reviewed ? now : null;
        if (!isCreate) { entity.UpdatedAt = now; entity.UpdatedByUserId = actorUserId; }
    }

    private static void ApplyTask(ComplianceTaskEntity task, LaborApplicabilityEntity entity, Guid actorUserId, DateTimeOffset now)
    {
        task.Title = "Review labor applicability and wage determination";
        task.Description = $"Confirm {LaborStandard(entity)} labor obligations, wage determination reference, source, and place of performance.";
        task.OwnerFunction = entity.OwnerFunction; task.DueAt = entity.ContractPeriodEnd; task.ContractId = entity.ContractId;
        task.EvidenceItemId = entity.WageDeterminationEvidenceItemId; task.UpdatedAt = now; task.UpdatedByUserId = actorUserId;
    }

    private static LaborApplicabilityRequest ToRequest(LaborApplicabilityEntity x) => new(x.ContractId, x.ScaApplicable, x.DbaApplicable,
        x.OtherFarPart22Obligations, x.PlaceOfPerformance, x.ContractPeriodStart, x.ContractPeriodEnd,
        x.WageDeterminationReference, x.WageDeterminationEvidenceItemId, x.SourceContractClauseId,
        x.SourceClause, x.Rationale, x.OwnerFunction, x.ReviewStatus, x.ReviewNotes);

    private static string LaborStandard(LaborApplicabilityEntity x) => string.Join(", ", new[]
    {
        x.ScaApplicable ? "SCA" : null, x.DbaApplicable ? "DBA" : null,
        x.OtherFarPart22Obligations is not null ? "other FAR Part 22" : null
    }.Where(value => value is not null));

    private static LaborApplicabilityDto ToDto(LaborApplicabilityEntity x) => new(x.Id, x.TenantId, x.ContractId, x.TaskId,
        x.ScaApplicable, x.DbaApplicable, x.OtherFarPart22Obligations, x.PlaceOfPerformance,
        x.ContractPeriodStart, x.ContractPeriodEnd, x.WageDeterminationReference, x.WageDeterminationEvidenceItemId,
        x.SourceContractClauseId, x.SourceClause ?? x.SourceContractClause?.ClauseNumber, x.Rationale, x.OwnerFunction,
        x.Status, x.ReviewStatus, x.ReviewNotes, x.ReviewedByUserId, x.ReviewedAt,
        x.Task is null ? null : new LaborReviewTaskDto(x.Task.Id, x.Task.TenantId, x.ContractId, x.Task.Title,
            x.Task.Description, x.Task.Status.ToString(), x.Task.DueAt), x.CreatedAt, x.UpdatedAt);
}
