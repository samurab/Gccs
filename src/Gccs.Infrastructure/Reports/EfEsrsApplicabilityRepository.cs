using Gccs.Application.Reports;
using Gccs.Application.Security;
using Gccs.Domain.Compliance;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Reports;

public sealed class EfEsrsApplicabilityRepository(GccsDbContext dbContext, ICurrentTenantContext tenantContext) : IEsrsApplicabilityRepository
{
    public async Task<IReadOnlyList<EsrsApplicabilityDto>?> ListForContractAsync(Guid contractId, CancellationToken cancellationToken = default)
    {
        if (!await ContractExistsAsync(contractId, cancellationToken)) return null;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var rows = await Query().Include(x => x.Task).Where(x => x.ContractId == contractId).OrderBy(x => x.DueDate)
            .ToArrayAsync(cancellationToken);
        return rows.Select(x => ToDto(x, today)).ToArray();
    }

    public async Task<EsrsApplicabilityDto?> CreateAsync(EsrsApplicabilityRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        if (!await ContractExistsAsync(request.ContractId, cancellationToken)) return null;
        await ValidateReferencesAsync(request, null, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var task = new ComplianceTaskEntity
        {
            Id = Guid.NewGuid(), TenantId = tenantContext.TenantId, Title = TaskTitle(request.ReportType),
            Description = TaskDescription(request), Type = ComplianceTaskType.Report, Status = ComplianceTaskStatus.Open,
            RiskLevel = RiskLevel.High, AssignedToUserId = request.AssignedToUserId ?? actorUserId,
            OwnerFunction = request.OwnerFunction ?? "Contracts", DueAt = request.DueDate, ContractId = request.ContractId,
            CreatedAt = now, CreatedByUserId = actorUserId
        };
        var entity = new EsrsApplicabilityEntity
        {
            Id = Guid.NewGuid(), TenantId = tenantContext.TenantId, ContractId = request.ContractId, TaskId = task.Id,
            ContractType = request.ContractType, Agency = request.Agency, SubcontractingPlanType = request.SubcontractingPlanType,
            PrimeOrLowerTierRole = request.PrimeOrLowerTierRole, ReportType = request.ReportType,
            PeriodStart = request.PeriodStart, PeriodEnd = request.PeriodEnd, DueDate = request.DueDate,
            SourceClause = request.SourceClause, Rationale = request.Rationale,
            OwnerFunction = request.OwnerFunction ?? "Contracts", AssignedToUserId = task.AssignedToUserId,
            ReviewedByUserId = actorUserId, ReviewedAt = now, CreatedAt = now, CreatedByUserId = actorUserId, Task = task
        };
        dbContext.EsrsApplicabilities.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity, DateOnly.FromDateTime(DateTime.UtcNow));
    }

    public async Task<EsrsApplicabilityDto?> UpdateAsync(Guid id, EsrsApplicabilityRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var entity = await Query(true).Include(x => x.Task).SingleOrDefaultAsync(x => x.Id == id && x.ContractId == request.ContractId, cancellationToken);
        if (entity is null) return null;
        await ValidateReferencesAsync(request, id, cancellationToken);
        Apply(entity, request, actorUserId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity, DateOnly.FromDateTime(DateTime.UtcNow));
    }

    public async Task<EsrsApplicabilityDto?> UpdateStatusAsync(Guid contractId, Guid id, EsrsReportTaskStatus status, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var entity = await Query(true).Include(x => x.Task).SingleOrDefaultAsync(x => x.Id == id && x.ContractId == contractId, cancellationToken);
        if (entity is null) return null;
        entity.Task!.Status = status switch
        {
            EsrsReportTaskStatus.Open => ComplianceTaskStatus.Open,
            EsrsReportTaskStatus.InProgress => ComplianceTaskStatus.InProgress,
            EsrsReportTaskStatus.Completed => ComplianceTaskStatus.Done,
            _ => ComplianceTaskStatus.Canceled
        };
        entity.UpdatedAt = entity.Task.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedByUserId = entity.Task.UpdatedByUserId = actorUserId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity, DateOnly.FromDateTime(DateTime.UtcNow));
    }

    private async Task ValidateReferencesAsync(EsrsApplicabilityRequest request, Guid? excludingId, CancellationToken token)
    {
        if (request.AssignedToUserId.HasValue && !await IsActiveMemberAsync(request.AssignedToUserId.Value, token))
            throw new EsrsApplicabilityValidationException("Assigned user must be an active member of the current tenant.");
        if (await Query().AnyAsync(x => x.Id != excludingId && x.ContractId == request.ContractId &&
                x.ReportType == request.ReportType && x.PeriodStart == request.PeriodStart && x.PeriodEnd == request.PeriodEnd, token))
            throw new EsrsApplicabilityValidationException("A SAM.gov subcontracting plan reporting obligation already exists for this contract, report type, and reporting period.");
    }

    private static void Apply(EsrsApplicabilityEntity entity, EsrsApplicabilityRequest request, Guid actorUserId)
    {
        entity.ContractType = request.ContractType; entity.Agency = request.Agency; entity.SubcontractingPlanType = request.SubcontractingPlanType;
        entity.PrimeOrLowerTierRole = request.PrimeOrLowerTierRole; entity.ReportType = request.ReportType;
        entity.PeriodStart = request.PeriodStart; entity.PeriodEnd = request.PeriodEnd; entity.DueDate = request.DueDate;
        entity.SourceClause = request.SourceClause; entity.Rationale = request.Rationale; entity.OwnerFunction = request.OwnerFunction ?? "Contracts";
        entity.AssignedToUserId = request.AssignedToUserId ?? entity.AssignedToUserId; entity.ReviewedByUserId = actorUserId;
        entity.ReviewedAt = DateTimeOffset.UtcNow; entity.UpdatedAt = DateTimeOffset.UtcNow; entity.UpdatedByUserId = actorUserId;
        entity.Task!.Title = TaskTitle(request.ReportType); entity.Task.Description = TaskDescription(request);
        entity.Task.DueAt = request.DueDate; entity.Task.OwnerFunction = entity.OwnerFunction; entity.Task.AssignedToUserId = entity.AssignedToUserId;
        entity.Task.UpdatedAt = entity.UpdatedAt; entity.Task.UpdatedByUserId = actorUserId;
    }

    private IQueryable<EsrsApplicabilityEntity> Query(bool tracking = false) =>
        (tracking ? dbContext.EsrsApplicabilities : dbContext.EsrsApplicabilities.AsNoTracking()).Where(x => x.TenantId == tenantContext.TenantId);
    private Task<bool> ContractExistsAsync(Guid id, CancellationToken token) =>
        dbContext.Contracts.AsNoTracking().AnyAsync(x => x.Id == id && x.TenantId == tenantContext.TenantId, token);
    private async Task<bool> IsActiveMemberAsync(Guid userId, CancellationToken token) =>
        await dbContext.TenantMemberships.AsNoTracking().AnyAsync(x => x.TenantId == tenantContext.TenantId && x.UserId == userId && x.Status == Gccs.Domain.Identity.MembershipStatus.Active, token) &&
        await dbContext.Users.AsNoTracking().AnyAsync(x => x.Id == userId && x.Status == Gccs.Domain.Identity.UserStatus.Active, token);
    private static string TaskTitle(EsrsReportType type) => $"{type.ToString().ToUpperInvariant()} SAM.gov SPR due";
    private static string TaskDescription(EsrsApplicabilityRequest request) =>
        $"Prepare the {request.ReportType.ToString().ToUpperInvariant()} SAM.gov subcontracting plan report for {request.PeriodStart:yyyy-MM-dd} through {request.PeriodEnd:yyyy-MM-dd}.";
    private static EsrsApplicabilityDto ToDto(EsrsApplicabilityEntity x, DateOnly today) => new(x.Id, x.TenantId, x.ContractId, x.TaskId,
        x.ContractType, x.Agency, x.SubcontractingPlanType, x.PrimeOrLowerTierRole, x.ReportType, x.PeriodStart, x.PeriodEnd,
        x.DueDate, x.SourceClause, x.Rationale, ToEsrsStatus(x.Task?.Status ?? ComplianceTaskStatus.Open), x.OwnerFunction, x.AssignedToUserId, x.ReviewedByUserId,
        x.ReviewedAt, x.CreatedAt, x.UpdatedAt, x.DueDate < today && x.Task?.Status is not ComplianceTaskStatus.Done and not ComplianceTaskStatus.Canceled);

    private static EsrsReportTaskStatus ToEsrsStatus(ComplianceTaskStatus status) => status switch
    {
        ComplianceTaskStatus.InProgress or ComplianceTaskStatus.Blocked => EsrsReportTaskStatus.InProgress,
        ComplianceTaskStatus.Done => EsrsReportTaskStatus.Completed,
        ComplianceTaskStatus.Canceled => EsrsReportTaskStatus.Canceled,
        _ => EsrsReportTaskStatus.Open
    };
}
