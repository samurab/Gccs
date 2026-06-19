using Gccs.Application.Security;
using Gccs.Application.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Tenancy;

public sealed class EfGovernmentCloudReleaseReadinessRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : IGovernmentCloudReleaseReadinessRepository
{
    public async Task<GovernmentCloudEnvironmentDto?> GetEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken = default)
    {
        var environment = await dbContext.GovernmentCloudEnvironments.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == environmentId && candidate.TenantId == tenantContext.TenantId, cancellationToken);
        return environment is null ? null : ToEnvironmentDto(environment);
    }

    public async Task<GovernmentCloudReleaseReadinessDto?> GetAsync(Guid readinessId, CancellationToken cancellationToken = default)
    {
        var entity = await Query().SingleOrDefaultAsync(candidate => candidate.Id == readinessId && candidate.TenantId == tenantContext.TenantId, cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<GovernmentCloudReleaseReadinessDto> CreateAsync(CreateGovernmentCloudReleaseReadinessRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var entity = new GovernmentCloudReleaseReadinessEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            EnvironmentId = request.EnvironmentId,
            Version = request.Version.Trim(),
            ReleaseWindow = request.ReleaseWindow.Trim(),
            Owner = request.Owner.Trim(),
            Status = GovernmentCloudReleaseStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = actorUserId
        };
        dbContext.GovernmentCloudReleaseReadiness.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public Task<GovernmentCloudReleaseReadinessDto?> CompleteChecklistAsync(Guid readinessId, GovernmentCloudReleaseChecklistItem item, string evidenceReference, Guid actorUserId, CancellationToken cancellationToken = default) =>
        UpdateAsync(readinessId, actorUserId, entity =>
        {
            var row = entity.Checklist.SingleOrDefault(candidate => candidate.Item == item);
            if (row is null)
            {
                row = new GovernmentCloudReleaseChecklistEntity { Id = Guid.NewGuid(), TenantId = tenantContext.TenantId, ReadinessId = entity.Id, Item = item };
                dbContext.GovernmentCloudReleaseChecklist.Add(row);
                entity.Checklist.Add(row);
            }

            row.EvidenceReference = evidenceReference;
            row.CompletedAt = DateTimeOffset.UtcNow;
            row.CompletedByUserId = actorUserId;
        }, cancellationToken);

    public Task<GovernmentCloudReleaseReadinessDto?> LinkEvidenceAsync(Guid readinessId, GovernmentCloudReleaseEvidenceType evidenceType, string link, Guid actorUserId, CancellationToken cancellationToken = default) =>
        UpdateAsync(readinessId, actorUserId, entity =>
        {
            var row = entity.Evidence.SingleOrDefault(candidate => candidate.EvidenceType == evidenceType);
            if (row is null)
            {
                row = new GovernmentCloudReleaseEvidenceEntity { Id = Guid.NewGuid(), TenantId = tenantContext.TenantId, ReadinessId = entity.Id, EvidenceType = evidenceType };
                dbContext.GovernmentCloudReleaseEvidence.Add(row);
                entity.Evidence.Add(row);
            }

            row.Link = link;
            row.LinkedAt = DateTimeOffset.UtcNow;
            row.LinkedByUserId = actorUserId;
        }, cancellationToken);

    public Task<GovernmentCloudReleaseReadinessDto?> AddGapAsync(Guid readinessId, GovernmentCloudReleaseGapArea area, GovernmentCloudReleaseGapSeverity severity, string description, Guid actorUserId, CancellationToken cancellationToken = default) =>
        UpdateAsync(readinessId, actorUserId, entity =>
        {
            var gap = new GovernmentCloudReleaseGapEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantContext.TenantId,
                ReadinessId = entity.Id,
                Area = area,
                Severity = severity,
                Description = description,
                IsOpen = true,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedByUserId = actorUserId
            };
            dbContext.GovernmentCloudReleaseGaps.Add(gap);
            entity.Gaps.Add(gap);
        }, cancellationToken);

    public Task<GovernmentCloudReleaseReadinessDto?> ApproveAsync(Guid readinessId, string approverName, string approvalNotes, Guid actorUserId, CancellationToken cancellationToken = default) =>
        UpdateAsync(readinessId, actorUserId, entity =>
        {
            entity.Status = GovernmentCloudReleaseStatus.Approved;
            entity.ApproverName = approverName;
            entity.ApprovalNotes = approvalNotes;
            entity.ApprovedAt = DateTimeOffset.UtcNow;
        }, cancellationToken);

    public Task<GovernmentCloudReleaseReadinessDto?> DeployAsync(Guid readinessId, string result, string rollbackStatus, Guid actorUserId, CancellationToken cancellationToken = default) =>
        UpdateAsync(readinessId, actorUserId, entity =>
        {
            entity.Status = GovernmentCloudReleaseStatus.Deployed;
            entity.Result = result;
            entity.RollbackStatus = rollbackStatus;
            entity.DeployedAt = DateTimeOffset.UtcNow;
        }, cancellationToken);

    private async Task<GovernmentCloudReleaseReadinessDto?> UpdateAsync(Guid readinessId, Guid actorUserId, Action<GovernmentCloudReleaseReadinessEntity> update, CancellationToken cancellationToken)
    {
        var entity = await Query().SingleOrDefaultAsync(candidate => candidate.Id == readinessId && candidate.TenantId == tenantContext.TenantId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        update(entity);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedByUserId = actorUserId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    private IQueryable<GovernmentCloudReleaseReadinessEntity> Query() =>
        dbContext.GovernmentCloudReleaseReadiness.Include(x => x.Checklist).Include(x => x.Evidence).Include(x => x.Gaps);

    private static GovernmentCloudReleaseReadinessDto ToDto(GovernmentCloudReleaseReadinessEntity entity) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.EnvironmentId,
            entity.Version,
            entity.ReleaseWindow,
            entity.Owner,
            entity.Status,
            entity.ApproverName,
            entity.ApprovedAt,
            entity.Result,
            entity.RollbackStatus,
            entity.DeployedAt,
            entity.Checklist.Select(item => item.Item).Distinct().ToArray(),
            entity.Evidence.Select(item => new GovernmentCloudReleaseEvidenceLinkDto(item.EvidenceType, item.Link)).ToArray(),
            entity.Gaps.Where(gap => gap.IsOpen && gap.Severity == GovernmentCloudReleaseGapSeverity.Critical).Select(gap => gap.Area).Distinct().ToArray(),
            entity.CreatedAt,
            entity.UpdatedAt);

    private static GovernmentCloudEnvironmentDto ToEnvironmentDto(GovernmentCloudEnvironmentEntity environment) =>
        new(environment.Id, environment.TenantId, environment.Name, environment.EnvironmentType, environment.Region, environment.Boundary, environment.NetworkSegment, environment.StorageAccount, environment.DatabaseService, environment.KeyManagementService, environment.LoggingWorkspace, environment.BackupPolicy, environment.PrivateNetworkingEnabled, environment.StorageEncryptionEnabled, environment.DatabaseEncryptionEnabled, environment.CustomerManagedKeysEnabled, environment.AuditLoggingEnabled, environment.ImmutableLoggingEnabled, environment.BackupEnabled, environment.RestoreTested, environment.Status, environment.ReviewerName, environment.ReviewNotes, environment.ReviewedAt, environment.CreatedAt, environment.UpdatedAt);
}
