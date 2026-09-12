using Gccs.Application.Compliance;
using Gccs.Application.Portals;
using Gccs.Application.Reports;
using Gccs.Domain.Common;
using Gccs.Domain.Reports;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Portals;

public sealed class EfExternalPortalAccessRepository(GccsDbContext dbContext) : IExternalPortalAccessRepository
{
    public async Task<ExternalPortalInvitationDto> CreateInvitationAsync(
        ExternalPortalInvitationRequest request, Guid tenantId, Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var invitationId = Guid.NewGuid();
        var entity = new ExternalPortalInvitationEntity
        {
            Id = invitationId, TenantId = tenantId, Email = request.Email, Role = request.Role,
            ExpiresAt = request.ExpiresAt, CanDownload = request.CanDownload,
            StrongAuthenticationRequired = request.StrongAuthenticationRequired,
            Status = ExternalPortalInvitationStatus.Pending, Version = 1,
            CreatedAt = now, CreatedByUserId = actorUserId,
            PackageScopes = request.PackageIds.Select(id => new ExternalPortalInvitationPackageScopeEntity
                { TenantId = tenantId, InvitationId = invitationId, PackageId = id }).ToArray(),
            ContractScopes = request.ContractIds.Select(id => new ExternalPortalInvitationContractScopeEntity
                { TenantId = tenantId, InvitationId = invitationId, ContractId = id }).ToArray()
        };
        dbContext.ExternalPortalInvitations.Add(entity);
        await SaveAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<IReadOnlyList<ExternalPortalInvitationDto>> ListInvitationsAsync(
        Guid tenantId, CancellationToken cancellationToken = default) =>
        (await Query().Where(item => item.TenantId == tenantId)
            .OrderByDescending(item => item.CreatedAt).ToArrayAsync(cancellationToken)).Select(ToDto).ToArray();

    public async Task<ExternalPortalInvitationDto?> FindInvitationAsync(
        Guid invitationId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var entity = await Query().SingleOrDefaultAsync(
            item => item.Id == invitationId && item.TenantId == tenantId, cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<ExternalPortalInvitationDto?> FindInvitationForAccessAsync(
        Guid invitationId, CancellationToken cancellationToken = default)
    {
        var entity = await Query().SingleOrDefaultAsync(item => item.Id == invitationId, cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    public Task<ExternalPortalInvitationDto?> ResendAsync(
        Guid invitationId, Guid tenantId, long expectedVersion, Guid actorUserId, DateTimeOffset changedAt,
        CancellationToken cancellationToken = default) =>
        UpdateAsync(invitationId, tenantId, expectedVersion, entity =>
        {
            entity.ResendCount++;
            entity.LastResentAt = changedAt;
        }, actorUserId, changedAt, cancellationToken);

    public Task<ExternalPortalInvitationDto?> ExtendAsync(
        Guid invitationId, Guid tenantId, long expectedVersion, DateTimeOffset expiresAt, Guid actorUserId,
        DateTimeOffset changedAt, CancellationToken cancellationToken = default) =>
        UpdateAsync(invitationId, tenantId, expectedVersion, entity => entity.ExpiresAt = expiresAt,
            actorUserId, changedAt, cancellationToken);

    public Task<ExternalPortalInvitationDto?> RevokeAsync(
        Guid invitationId, Guid tenantId, long expectedVersion, string reason, Guid actorUserId,
        DateTimeOffset changedAt, CancellationToken cancellationToken = default) =>
        UpdateAsync(invitationId, tenantId, expectedVersion, entity =>
        {
            entity.Status = ExternalPortalInvitationStatus.Revoked;
            entity.RevokedAt = changedAt;
            entity.RevocationReason = reason;
        }, actorUserId, changedAt, cancellationToken);

    public async Task<ExternalPortalInvitationDto?> RecordAccessAsync(
        Guid invitationId, Guid tenantId, Guid actorUserId, Guid packageId, Guid? contractId, bool allowed,
        string resultCode, DateTimeOffset occurredAt, bool bindExternalUser,
        CancellationToken cancellationToken = default)
    {
        ExternalPortalInvitationEntity? entity;
        if (allowed)
        {
            entity = await dbContext.ExternalPortalInvitations
                .Include(item => item.PackageScopes).Include(item => item.ContractScopes)
                .SingleOrDefaultAsync(item => item.Id == invitationId && item.TenantId == tenantId &&
                    item.Status != ExternalPortalInvitationStatus.Revoked && item.ExpiresAt > occurredAt &&
                    (item.ExternalUserId == null || item.ExternalUserId == actorUserId), cancellationToken);
            if (entity is null) return null;
            entity.Status = ExternalPortalInvitationStatus.Accepted;
            if (bindExternalUser) entity.ExternalUserId ??= actorUserId;
            entity.LastAccessedAt = occurredAt;
            entity.UpdatedAt = occurredAt;
            entity.Version++;
        }
        else
        {
            entity = await dbContext.ExternalPortalInvitations.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == invitationId && item.TenantId == tenantId, cancellationToken);
            if (entity is null) return null;
        }

        dbContext.ExternalPortalAccessHistory.Add(new ExternalPortalAccessHistoryEntity
        {
            Id = Guid.NewGuid(), TenantId = tenantId, InvitationId = invitationId,
            ActorUserId = actorUserId, PackageId = packageId, ContractId = contractId,
            Allowed = allowed, ResultCode = resultCode, OccurredAt = occurredAt
        });
        await SaveAsync(cancellationToken);
        return allowed ? ToDto(entity) : await FindInvitationAsync(invitationId, tenantId, cancellationToken);
    }

    public async Task<IReadOnlyList<ExternalPortalAccessHistoryDto>> ListAccessHistoryAsync(
        Guid invitationId, Guid tenantId, CancellationToken cancellationToken = default) =>
        await dbContext.ExternalPortalAccessHistory.AsNoTracking()
            .Where(item => item.InvitationId == invitationId && item.TenantId == tenantId)
            .OrderByDescending(item => item.OccurredAt)
            .Select(item => new ExternalPortalAccessHistoryDto(
                item.Id, item.InvitationId, item.TenantId, item.ActorUserId, item.PackageId,
                item.ContractId, item.Allowed, item.ResultCode, item.OccurredAt))
            .ToArrayAsync(cancellationToken);

    private IQueryable<ExternalPortalInvitationEntity> Query() =>
        dbContext.ExternalPortalInvitations.AsNoTracking()
            .Include(item => item.PackageScopes).Include(item => item.ContractScopes);

    private async Task<ExternalPortalInvitationDto?> UpdateAsync(
        Guid invitationId, Guid tenantId, long expectedVersion, Action<ExternalPortalInvitationEntity> update,
        Guid actorUserId, DateTimeOffset changedAt, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ExternalPortalInvitations
            .Include(item => item.PackageScopes).Include(item => item.ContractScopes)
            .SingleOrDefaultAsync(item => item.Id == invitationId && item.TenantId == tenantId && item.Version == expectedVersion,
                cancellationToken);
        if (entity is null) return null;
        update(entity);
        entity.Version++;
        entity.UpdatedAt = changedAt;
        entity.UpdatedByUserId = actorUserId;
        await SaveAsync(cancellationToken);
        return ToDto(entity);
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ExternalPortalAccessConflictException("The portal invitation changed. Reload it and try again.");
        }
    }

    private static ExternalPortalInvitationDto ToDto(ExternalPortalInvitationEntity entity) => new(
        entity.Id, entity.TenantId, entity.Email, entity.Role,
        entity.PackageScopes.Select(item => item.PackageId).Order().ToArray(),
        entity.ContractScopes.Select(item => item.ContractId).Order().ToArray(),
        entity.ExpiresAt, entity.CanDownload, entity.StrongAuthenticationRequired, entity.Status,
        entity.ExternalUserId, entity.LastAccessedAt, entity.RevokedAt, entity.RevocationReason,
        entity.ResendCount, entity.LastResentAt, entity.Version, entity.CreatedAt, entity.UpdatedAt);
}

public sealed class EfExternalPortalScopeValidator(GccsDbContext dbContext) : IExternalPortalScopeValidator
{
    public async Task ValidateAsync(
        Guid tenantId, IReadOnlyList<Guid> packageIds, IReadOnlyList<Guid> contractIds,
        CancellationToken cancellationToken = default)
    {
        var contracts = await dbContext.Contracts.AsNoTracking()
            .Where(item => item.TenantId == tenantId && contractIds.Contains(item.Id))
            .Select(item => item.Id).ToArrayAsync(cancellationToken);
        if (contracts.Length != contractIds.Count)
            throw new ExternalPortalAccessException("One or more contracts are unavailable for this tenant.");

        var reportIds = await dbContext.Reports.AsNoTracking()
            .Where(item => item.TenantId == tenantId && packageIds.Contains(item.Id) &&
                item.Status == ReportStatus.Complete && !item.IsUseBlocked &&
                (item.Classification == ContentClassification.Unclassified || item.Classification == ContentClassification.Fci))
            .Select(item => item.Id).ToArrayAsync(cancellationToken);
        var sspIds = await dbContext.SspExportPackages.AsNoTracking()
            .Where(item => item.TenantId == tenantId && packageIds.Contains(item.Id) &&
                (item.Status == SspExportPackageStatus.ExternalShareApproved.ToString() ||
                 item.Status == SspExportPackageStatus.Shared.ToString()))
            .Select(item => item.Id).ToArrayAsync(cancellationToken);
        var sprIds = await dbContext.SprReportPackages.AsNoTracking()
            .Where(item => item.TenantId == tenantId && packageIds.Contains(item.Id) &&
                item.Status == EsrsReportPackageStatus.Approved)
            .Select(item => item.Id).ToArrayAsync(cancellationToken);
        var eligible = reportIds.Concat(sspIds).Concat(sprIds).ToHashSet();
        if (eligible.Count != packageIds.Count)
            throw new ExternalPortalAccessException("One or more packages are unavailable or not approved for external review.");
    }
}
