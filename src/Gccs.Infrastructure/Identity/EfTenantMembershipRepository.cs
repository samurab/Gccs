using Gccs.Application.Identity;
using Gccs.Application.Security;
using Gccs.Domain.Identity;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Identity;

public sealed class EfTenantMembershipRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : ITenantMembershipRepository
{
    public async Task<IReadOnlyList<TenantMemberDto>> ListCurrentTenantMembersAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.TenantMemberships
            .AsNoTracking()
            .Include(membership => membership.User)
            .Where(membership => membership.TenantId == tenantContext.TenantId)
            .OrderBy(membership => membership.User!.DisplayName)
            .ThenBy(membership => membership.User!.Email)
            .Select(membership => new TenantMemberDto(
                membership.Id,
                membership.TenantId,
                membership.UserId,
                membership.User!.Email,
                membership.User.DisplayName,
                membership.User.Status,
                membership.Status,
                membership.RoleName,
                membership.User.MfaEnabled,
                membership.User.LastSignedInAt,
                membership.LastAccessedAt,
                membership.CreatedAt,
                membership.UpdatedAt))
            .ToListAsync(cancellationToken);

    public Task<bool> CurrentTenantMembershipExistsAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        dbContext.TenantMemberships
            .AnyAsync(
                membership => membership.TenantId == tenantContext.TenantId && membership.UserId == userId,
                cancellationToken);

    public async Task<TenantMemberDto?> FindActiveCurrentUserMembershipAsync(CancellationToken cancellationToken = default)
    {
        var membership = await dbContext.TenantMemberships
            .AsNoTracking()
            .Include(candidate => candidate.User)
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.TenantId == tenantContext.TenantId &&
                    candidate.UserId == tenantContext.UserId &&
                    candidate.Status == MembershipStatus.Active &&
                    candidate.User != null &&
                    candidate.User.Status == UserStatus.Active,
                cancellationToken);

        return membership is null ? null : ToDto(membership);
    }

    public async Task<TenantMemberDto> AddToCurrentTenantAsync(
        User user,
        TenantMembership membership,
        CancellationToken cancellationToken = default)
    {
        var userEntity = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Id == user.Id,
            cancellationToken);

        if (userEntity is null)
        {
            userEntity = new UserEntity
            {
                Id = user.Id,
                TenantId = tenantContext.TenantId,
                Email = user.Email,
                DisplayName = user.DisplayName,
                Status = user.Status,
                MfaEnabled = user.MfaEnabled,
                LastSignedInAt = user.LastSignedInAt,
                CreatedAt = user.Audit.CreatedAt,
                CreatedByUserId = user.Audit.CreatedByUserId
            };
            dbContext.Users.Add(userEntity);
        }
        else
        {
            userEntity.Email = user.Email;
            userEntity.DisplayName = user.DisplayName;
            userEntity.Status = UserStatus.Active;
            userEntity.UpdatedAt = membership.Audit.CreatedAt;
            userEntity.UpdatedByUserId = membership.Audit.CreatedByUserId;
        }

        var membershipEntity = new TenantMembershipEntity
        {
            Id = membership.Id,
            TenantId = tenantContext.TenantId,
            UserId = membership.UserId,
            Status = membership.Status,
            RoleName = membership.RoleName,
            LastAccessedAt = membership.LastAccessedAt,
            CreatedAt = membership.Audit.CreatedAt,
            CreatedByUserId = membership.Audit.CreatedByUserId
        };
        dbContext.TenantMemberships.Add(membershipEntity);

        await dbContext.SaveChangesAsync(cancellationToken);

        membershipEntity.User = userEntity;
        return ToDto(membershipEntity);
    }

    public async Task<TenantMemberDto?> UpdateStatusInCurrentTenantScopeAsync(
        Guid membershipId,
        MembershipStatus status,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var membership = await dbContext.TenantMemberships
            .Include(candidate => candidate.User)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == membershipId && candidate.TenantId == tenantContext.TenantId,
                cancellationToken);

        if (membership is null)
        {
            return null;
        }

        membership.Status = status;
        membership.UpdatedAt = DateTimeOffset.UtcNow;
        membership.UpdatedByUserId = actorUserId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(membership);
    }

    private static TenantMemberDto ToDto(TenantMembershipEntity membership) =>
        new(
            membership.Id,
            membership.TenantId,
            membership.UserId,
            membership.User?.Email ?? string.Empty,
            membership.User?.DisplayName ?? string.Empty,
            membership.User?.Status ?? UserStatus.Disabled,
            membership.Status,
            membership.RoleName,
            membership.User?.MfaEnabled ?? false,
            membership.User?.LastSignedInAt,
            membership.LastAccessedAt,
            membership.CreatedAt,
            membership.UpdatedAt);
}
