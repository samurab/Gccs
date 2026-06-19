using Gccs.Application.Identity;
using Gccs.Application.Security;
using Gccs.Domain.Identity;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Identity;

public sealed class EfScimProvisioningRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : IScimProvisioningRepository
{
    public async Task<ScimProvisioningConfigurationDto?> GetConfigurationForCurrentTenantAsync(CancellationToken cancellationToken = default)
    {
        var configuration = await dbContext.ScimProvisioningConfigurations
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.TenantId == tenantContext.TenantId, cancellationToken);

        return configuration is null ? null : ToDto(configuration);
    }

    public async Task<ScimProvisioningConfigurationDto> UpsertConfigurationForCurrentTenantAsync(
        bool enabled,
        string tokenHash,
        string? endpointLabel,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var configuration = await dbContext.ScimProvisioningConfigurations
            .SingleOrDefaultAsync(candidate => candidate.TenantId == tenantContext.TenantId, cancellationToken);

        if (configuration is null)
        {
            configuration = new ScimProvisioningConfigurationEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantContext.TenantId,
                CreatedAt = now,
                CreatedByUserId = actorUserId
            };
            dbContext.ScimProvisioningConfigurations.Add(configuration);
        }
        else
        {
            configuration.UpdatedAt = now;
            configuration.UpdatedByUserId = actorUserId;
        }

        configuration.Enabled = enabled;
        configuration.TokenHash = tokenHash;
        configuration.EndpointLabel = endpointLabel;
        configuration.TokenRotatedAt = now;
        configuration.TokenRevokedAt = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(configuration);
    }

    public Task<ScimProvisioningConfigurationDto?> RotateTokenForCurrentTenantAsync(
        string tokenHash,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        UpdateConfigurationAsync(actorUserId, configuration =>
        {
            configuration.Enabled = true;
            configuration.TokenHash = tokenHash;
            configuration.TokenRotatedAt = DateTimeOffset.UtcNow;
            configuration.TokenRevokedAt = null;
        }, cancellationToken);

    public Task<ScimProvisioningConfigurationDto?> RevokeTokenForCurrentTenantAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        UpdateConfigurationAsync(actorUserId, configuration =>
        {
            configuration.TokenRevokedAt = DateTimeOffset.UtcNow;
            configuration.TokenHash = null;
        }, cancellationToken);

    public Task<bool> CurrentTenantTokenHashIsActiveAsync(
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        dbContext.ScimProvisioningConfigurations.AnyAsync(
            configuration =>
                configuration.TenantId == tenantContext.TenantId &&
                configuration.Enabled &&
                configuration.TokenRevokedAt == null &&
                configuration.TokenHash == tokenHash,
            cancellationToken);

    public async Task<ScimGroupMappingDto> UpsertGroupMappingForCurrentTenantAsync(
        string groupDisplayName,
        string roleName,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var mapping = await dbContext.ScimGroupMappings
            .SingleOrDefaultAsync(
                candidate => candidate.TenantId == tenantContext.TenantId && candidate.GroupDisplayName == groupDisplayName,
                cancellationToken);

        if (mapping is null)
        {
            mapping = new ScimGroupMappingEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantContext.TenantId,
                GroupDisplayName = groupDisplayName,
                CreatedAt = now,
                CreatedByUserId = actorUserId
            };
            dbContext.ScimGroupMappings.Add(mapping);
        }
        else
        {
            mapping.UpdatedAt = now;
            mapping.UpdatedByUserId = actorUserId;
        }

        mapping.RoleName = roleName;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(mapping);
    }

    public async Task<ScimGroupMappingDto?> GetGroupMappingForCurrentTenantAsync(
        string groupDisplayName,
        CancellationToken cancellationToken = default)
    {
        var mapping = await dbContext.ScimGroupMappings
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.TenantId == tenantContext.TenantId && candidate.GroupDisplayName == groupDisplayName,
                cancellationToken);

        return mapping is null ? null : ToDto(mapping);
    }

    public async Task<ScimProvisionedUserDto?> GetProvisionedIdentityForCurrentTenantAsync(
        string externalId,
        CancellationToken cancellationToken = default)
    {
        var identity = await QueryProvisionedIdentities()
            .SingleOrDefaultAsync(candidate => candidate.ExternalId == externalId && candidate.TenantId == tenantContext.TenantId, cancellationToken);

        return identity is null ? null : ToDto(identity);
    }

    public Task<bool> ExternalIdentityExistsOutsideCurrentTenantAsync(
        string externalId,
        CancellationToken cancellationToken = default) =>
        dbContext.ScimProvisionedIdentities.AnyAsync(
            identity => identity.ExternalId == externalId && identity.TenantId != tenantContext.TenantId,
            cancellationToken);

    public async Task<ScimProvisionedUserDto> UpsertProvisionedUserForCurrentTenantAsync(
        string externalId,
        string userName,
        string displayName,
        string roleName,
        UserStatus userStatus,
        MembershipStatus membershipStatus,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var identity = await dbContext.ScimProvisionedIdentities
            .Include(candidate => candidate.User)
            .Include(candidate => candidate.Membership)
            .SingleOrDefaultAsync(candidate => candidate.TenantId == tenantContext.TenantId && candidate.ExternalId == externalId, cancellationToken);

        if (identity is null)
        {
            var user = await dbContext.Users.SingleOrDefaultAsync(
                candidate => candidate.TenantId == tenantContext.TenantId && candidate.Email == userName,
                cancellationToken);
            if (user is not null)
            {
                var linkedToDifferentIdentity = await dbContext.ScimProvisionedIdentities.AnyAsync(
                    candidate => candidate.TenantId == tenantContext.TenantId && candidate.UserId == user.Id && candidate.ExternalId != externalId,
                    cancellationToken);
                if (linkedToDifferentIdentity)
                {
                    throw new ScimProvisioningValidationException("SCIM user email is already linked to another SCIM identity.");
                }
            }

            if (user is null)
            {
                user = new UserEntity
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantContext.TenantId,
                    CreatedAt = now,
                    CreatedByUserId = actorUserId
                };
                dbContext.Users.Add(user);
            }

            var membership = await dbContext.TenantMemberships.SingleOrDefaultAsync(
                candidate => candidate.TenantId == tenantContext.TenantId && candidate.UserId == user.Id,
                cancellationToken);
            if (membership is null)
            {
                membership = new TenantMembershipEntity
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantContext.TenantId,
                    UserId = user.Id,
                    CreatedAt = now,
                    CreatedByUserId = actorUserId
                };
                dbContext.TenantMemberships.Add(membership);
            }

            identity = new ScimProvisionedIdentityEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantContext.TenantId,
                ExternalId = externalId,
                UserId = user.Id,
                MembershipId = membership.Id,
                CreatedAt = now,
                CreatedByUserId = actorUserId,
                User = user,
                Membership = membership
            };
            dbContext.ScimProvisionedIdentities.Add(identity);
        }

        identity.UserName = userName;
        identity.LastProvisionedAt = now;
        identity.UpdatedAt = now;
        identity.UpdatedByUserId = actorUserId;
        identity.User!.Email = userName;
        identity.User.DisplayName = displayName;
        identity.User.Status = userStatus;
        identity.User.UpdatedAt = now;
        identity.User.UpdatedByUserId = actorUserId;
        identity.Membership!.Status = membershipStatus;
        identity.Membership.RoleName = roleName;
        identity.Membership.UpdatedAt = now;
        identity.Membership.UpdatedByUserId = actorUserId;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(identity);
    }

    public Task<ScimProvisionedUserDto?> UpdateProvisionedUserStatusForCurrentTenantAsync(
        string externalId,
        UserStatus userStatus,
        MembershipStatus membershipStatus,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        UpdateProvisionedIdentityAsync(externalId, actorUserId, identity =>
        {
            identity.User!.Status = userStatus;
            identity.Membership!.Status = membershipStatus;
            identity.LastProvisionedAt = DateTimeOffset.UtcNow;
        }, cancellationToken);

    public Task<ScimProvisionedUserDto?> UpdateProvisionedUserRoleForCurrentTenantAsync(
        string externalId,
        string roleName,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        UpdateProvisionedIdentityAsync(externalId, actorUserId, identity =>
        {
            identity.Membership!.RoleName = roleName;
            identity.LastProvisionedAt = DateTimeOffset.UtcNow;
        }, cancellationToken);

    public async Task TouchLastSyncForCurrentTenantAsync(Guid actorUserId, CancellationToken cancellationToken = default)
    {
        await UpdateConfigurationAsync(actorUserId, configuration => configuration.LastSyncAt = DateTimeOffset.UtcNow, cancellationToken);
    }

    private async Task<ScimProvisionedUserDto?> UpdateProvisionedIdentityAsync(
        string externalId,
        Guid actorUserId,
        Action<ScimProvisionedIdentityEntity> update,
        CancellationToken cancellationToken)
    {
        var identity = await dbContext.ScimProvisionedIdentities
            .Include(candidate => candidate.User)
            .Include(candidate => candidate.Membership)
            .SingleOrDefaultAsync(candidate => candidate.TenantId == tenantContext.TenantId && candidate.ExternalId == externalId, cancellationToken);

        if (identity is null)
        {
            return null;
        }

        update(identity);
        identity.UpdatedAt = DateTimeOffset.UtcNow;
        identity.UpdatedByUserId = actorUserId;
        identity.User!.UpdatedAt = identity.UpdatedAt;
        identity.User.UpdatedByUserId = actorUserId;
        identity.Membership!.UpdatedAt = identity.UpdatedAt;
        identity.Membership.UpdatedByUserId = actorUserId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(identity);
    }

    private async Task<ScimProvisioningConfigurationDto?> UpdateConfigurationAsync(
        Guid actorUserId,
        Action<ScimProvisioningConfigurationEntity> update,
        CancellationToken cancellationToken)
    {
        var configuration = await dbContext.ScimProvisioningConfigurations
            .SingleOrDefaultAsync(candidate => candidate.TenantId == tenantContext.TenantId, cancellationToken);
        if (configuration is null)
        {
            return null;
        }

        update(configuration);
        configuration.UpdatedAt = DateTimeOffset.UtcNow;
        configuration.UpdatedByUserId = actorUserId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(configuration);
    }

    private IQueryable<ScimProvisionedIdentityEntity> QueryProvisionedIdentities() =>
        dbContext.ScimProvisionedIdentities
            .AsNoTracking()
            .Include(identity => identity.User)
            .Include(identity => identity.Membership);

    private static ScimProvisioningConfigurationDto ToDto(ScimProvisioningConfigurationEntity configuration) =>
        new(
            configuration.Id,
            configuration.TenantId,
            configuration.Enabled,
            configuration.EndpointLabel,
            configuration.LastSyncAt,
            configuration.TokenRotatedAt,
            configuration.TokenRevokedAt,
            configuration.CreatedAt,
            configuration.UpdatedAt);

    private static ScimGroupMappingDto ToDto(ScimGroupMappingEntity mapping) =>
        new(mapping.Id, mapping.TenantId, mapping.GroupDisplayName, mapping.RoleName, mapping.CreatedAt, mapping.UpdatedAt);

    private static ScimProvisionedUserDto ToDto(ScimProvisionedIdentityEntity identity) =>
        new(
            identity.Id,
            identity.TenantId,
            identity.UserId,
            identity.MembershipId,
            identity.ExternalId,
            identity.UserName,
            identity.User?.DisplayName ?? string.Empty,
            identity.Membership?.RoleName ?? string.Empty,
            identity.User?.Status ?? UserStatus.Disabled,
            identity.Membership?.Status ?? MembershipStatus.Deactivated,
            identity.LastProvisionedAt,
            identity.CreatedAt,
            identity.UpdatedAt);
}
