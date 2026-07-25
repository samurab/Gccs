using Gccs.Application.Identity;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Identity;

public sealed class EfTenantWorkspaceSelectionRepository(GccsDbContext dbContext)
    : ITenantWorkspaceSelectionRepository
{
    public async Task<TenantWorkspaceListDto> ListAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null || user.Status is not UserStatus.Active)
        {
            return new TenantWorkspaceListDto(null, []);
        }

        var memberships = await dbContext.TenantMemberships
            .AsNoTracking()
            .Include(candidate => candidate.Tenant)
            .Where(candidate => candidate.UserId == userId)
            .OrderBy(candidate => candidate.Tenant!.Name)
            .Select(candidate => new
            {
                candidate.Id,
                candidate.TenantId,
                TenantName = candidate.Tenant!.Name,
                TenantStatus = candidate.Tenant.Status,
                candidate.Tenant.DataPosture,
                MembershipStatus = candidate.Status,
                candidate.RoleName,
                candidate.LastAccessedAt
            })
            .ToListAsync(cancellationToken);

        var workspaces = memberships.Select(candidate =>
        {
            var reason = GetUnavailableReason(candidate.MembershipStatus, candidate.TenantStatus);
            return new TenantWorkspaceDto(
                candidate.Id,
                candidate.TenantId,
                candidate.TenantName,
                candidate.TenantStatus,
                candidate.DataPosture,
                candidate.MembershipStatus,
                candidate.RoleName,
                candidate.LastAccessedAt,
                reason is null,
                reason);
        }).ToArray();

        var preferredTenantId = workspaces.Any(candidate =>
            candidate.TenantId == user.PreferredTenantId && candidate.IsSelectable)
                ? user.PreferredTenantId
                : null;

        return new TenantWorkspaceListDto(preferredTenantId, workspaces);
    }

    public async Task<TenantWorkspaceSelectionPersistenceResult?> StageSelectionAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .SingleOrDefaultAsync(
                candidate => candidate.Id == userId && candidate.Status == UserStatus.Active,
                cancellationToken);
        if (user is null)
        {
            return null;
        }

        var membership = await dbContext.TenantMemberships
            .Include(candidate => candidate.Tenant)
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.UserId == userId &&
                    candidate.TenantId == tenantId &&
                    candidate.Status == MembershipStatus.Active &&
                    candidate.Tenant != null &&
                    (candidate.Tenant.Status == TenantStatus.Active ||
                     candidate.Tenant.Status == TenantStatus.Trialing),
                cancellationToken);
        if (membership?.Tenant is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var previousTenantId = user.PreferredTenantId;
        user.PreferredTenantId = tenantId;
        user.UpdatedAt = now;
        user.UpdatedByUserId = userId;
        membership.LastAccessedAt = now;
        membership.UpdatedAt = now;
        membership.UpdatedByUserId = userId;

        return new TenantWorkspaceSelectionPersistenceResult(
            previousTenantId,
            new TenantWorkspaceDto(
                membership.Id,
                membership.TenantId,
                membership.Tenant.Name,
                membership.Tenant.Status,
                membership.Tenant.DataPosture,
                membership.Status,
                membership.RoleName,
                membership.LastAccessedAt,
                true,
                null));
    }

    private static string? GetUnavailableReason(MembershipStatus membershipStatus, TenantStatus tenantStatus)
    {
        if (membershipStatus is not MembershipStatus.Active)
        {
            return "Your membership is not active.";
        }

        return tenantStatus switch
        {
            TenantStatus.Active or TenantStatus.Trialing => null,
            TenantStatus.PendingActivation => "The tenant is pending activation.",
            TenantStatus.Suspended => "The tenant is suspended.",
            TenantStatus.Archived => "The tenant is archived.",
            _ => "The tenant is unavailable."
        };
    }
}
