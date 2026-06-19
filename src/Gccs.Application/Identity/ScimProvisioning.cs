using System.Security.Cryptography;
using System.Text;
using Gccs.Application.Audit;
using Gccs.Application.Security;
using Gccs.Domain.Audit;
using Gccs.Domain.Identity;

namespace Gccs.Application.Identity;

public sealed class ScimProvisioningService(
    IScimProvisioningRepository repository,
    ICurrentTenantContext tenantContext,
    IAuditEventWriter auditEventWriter)
{
    public Task<ScimProvisioningConfigurationDto?> GetConfigurationAsync(CancellationToken cancellationToken = default) =>
        repository.GetConfigurationForCurrentTenantAsync(cancellationToken);

    public async Task<ScimTokenLifecycleResult> EnableAsync(
        EnableScimProvisioningRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateConfirmation(request.ConfirmationText);
        var provisioningKey = CreateToken();
        var configuration = await repository.UpsertConfigurationForCurrentTenantAsync(
            true,
            HashToken(provisioningKey),
            NormalizeOptional(request.EndpointLabel, 160),
            actorUserId,
            cancellationToken);

        await WriteAuditAsync(actorUserId, AuditAction.Created, "ScimProvisioningConfiguration", configuration.Id, "SCIM provisioning was enabled.", "enabled", cancellationToken);
        return new ScimTokenLifecycleResult(configuration, provisioningKey);
    }

    public async Task<ScimTokenLifecycleResult?> RotateTokenAsync(Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var provisioningKey = CreateToken();
        var configuration = await repository.RotateTokenForCurrentTenantAsync(HashToken(provisioningKey), actorUserId, cancellationToken);
        if (configuration is null)
        {
            return null;
        }

        await WriteAuditAsync(actorUserId, AuditAction.Updated, "ScimProvisioningConfiguration", configuration.Id, "SCIM token was rotated.", "token_rotated", cancellationToken);
        return new ScimTokenLifecycleResult(configuration, provisioningKey);
    }

    public async Task<ScimProvisioningConfigurationDto?> RevokeTokenAsync(Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var configuration = await repository.RevokeTokenForCurrentTenantAsync(actorUserId, cancellationToken);
        if (configuration is not null)
        {
            await WriteAuditAsync(actorUserId, AuditAction.Updated, "ScimProvisioningConfiguration", configuration.Id, "SCIM token was revoked.", "token_revoked", cancellationToken);
        }

        return configuration;
    }

    public async Task<ScimGroupMappingDto> UpsertGroupMappingAsync(
        UpsertScimGroupMappingRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var groupName = NormalizeRequired(request.GroupDisplayName, nameof(request.GroupDisplayName), 200);
        if (!RoleCatalog.TryNormalizeRoleName(request.RoleName, out var roleName))
        {
            await WriteAuditAsync(actorUserId, AuditAction.Rejected, "ScimGroupMapping", groupName, "Invalid SCIM group mapping was rejected.", "invalid_group_mapping", cancellationToken);
            throw new ScimProvisioningValidationException("SCIM group mapping role is invalid.");
        }

        var mapping = await repository.UpsertGroupMappingForCurrentTenantAsync(groupName, roleName, actorUserId, cancellationToken);
        await WriteAuditAsync(actorUserId, AuditAction.Updated, "ScimGroupMapping", mapping.Id, "SCIM group mapping was saved.", "group_mapping_saved", cancellationToken);
        return mapping;
    }

    public async Task<ScimProvisionedUserDto> ProvisionUserAsync(
        ScimProvisionUserRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        await RequireActiveTokenAsync(request.Token, actorUserId, cancellationToken);
        var externalId = NormalizeRequired(request.ExternalId, nameof(request.ExternalId), 200);
        var email = NormalizeEmail(request.UserName);
        var displayName = NormalizeRequired(request.DisplayName, nameof(request.DisplayName), 200);
        var roleName = await ResolveRoleAsync(request.Groups ?? [], actorUserId, cancellationToken);

        if (await repository.ExternalIdentityExistsOutsideCurrentTenantAsync(externalId, cancellationToken))
        {
            await WriteAuditAsync(actorUserId, AuditAction.Rejected, "ScimProvisionedUser", externalId, "Cross-tenant SCIM identity was rejected.", "cross_tenant_identity", cancellationToken);
            throw new ScimProvisioningValidationException("SCIM external identity belongs to another tenant.");
        }

        var existingIdentity = await repository.GetProvisionedIdentityForCurrentTenantAsync(externalId, cancellationToken);
        if (existingIdentity is not null && !string.Equals(existingIdentity.UserName, email, StringComparison.OrdinalIgnoreCase))
        {
            await WriteAuditAsync(actorUserId, AuditAction.Rejected, "ScimProvisionedUser", externalId, "Duplicate SCIM identity was rejected.", "duplicate_identity", cancellationToken);
            throw new ScimProvisioningValidationException("SCIM external identity is already linked to a different user.");
        }

        var user = await repository.UpsertProvisionedUserForCurrentTenantAsync(
            externalId,
            email,
            displayName,
            roleName,
            request.Active ? UserStatus.Active : UserStatus.Disabled,
            request.Active ? MembershipStatus.Active : MembershipStatus.Deactivated,
            actorUserId,
            cancellationToken);

        await repository.TouchLastSyncForCurrentTenantAsync(actorUserId, cancellationToken);
        await WriteAuditAsync(
            actorUserId,
            request.Active ? AuditAction.Updated : AuditAction.Deleted,
            "ScimProvisionedUser",
            user.ExternalId,
            request.Active ? "SCIM user was provisioned or updated." : "SCIM user was deactivated.",
            request.Active ? "user_upserted" : "user_deactivated",
            cancellationToken);
        return user;
    }

    public async Task<ScimProvisionedUserDto> ReactivateUserAsync(
        ScimTokenRequest request,
        string externalId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        await RequireActiveTokenAsync(request.Token, actorUserId, cancellationToken);
        var user = await repository.UpdateProvisionedUserStatusForCurrentTenantAsync(
            NormalizeRequired(externalId, nameof(externalId), 200),
            UserStatus.Active,
            MembershipStatus.Active,
            actorUserId,
            cancellationToken);

        if (user is null)
        {
            await WriteAuditAsync(actorUserId, AuditAction.Rejected, "ScimProvisionedUser", externalId, "SCIM reactivation skipped because the identity was not found.", "skipped", cancellationToken);
            throw new ScimProvisioningValidationException("SCIM identity was not found.");
        }

        await WriteAuditAsync(actorUserId, AuditAction.Updated, "ScimProvisionedUser", user.ExternalId, "SCIM user was reactivated.", "user_reactivated", cancellationToken);
        return user;
    }

    public async Task<ScimProvisionedUserDto> AssignGroupAsync(
        ScimGroupAssignmentRequest request,
        string externalId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        await RequireActiveTokenAsync(request.Token, actorUserId, cancellationToken);
        var roleName = await ResolveRoleAsync([request.GroupDisplayName], actorUserId, cancellationToken);
        var user = await repository.UpdateProvisionedUserRoleForCurrentTenantAsync(externalId, roleName, actorUserId, cancellationToken);
        if (user is null)
        {
            throw new ScimProvisioningValidationException("SCIM identity was not found.");
        }

        await WriteAuditAsync(actorUserId, AuditAction.Updated, "ScimProvisionedUser", user.ExternalId, "SCIM group assignment changed the GCCS role.", "group_assigned", cancellationToken);
        return user;
    }

    public async Task<ScimProvisionedUserDto> RemoveGroupAsync(
        ScimGroupAssignmentRequest request,
        string externalId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        await RequireActiveTokenAsync(request.Token, actorUserId, cancellationToken);
        var user = await repository.UpdateProvisionedUserRoleForCurrentTenantAsync(externalId, RoleCatalog.Contributor, actorUserId, cancellationToken);
        if (user is null)
        {
            throw new ScimProvisioningValidationException("SCIM identity was not found.");
        }

        await WriteAuditAsync(actorUserId, AuditAction.Updated, "ScimProvisionedUser", user.ExternalId, "SCIM group removal reset the GCCS role.", "group_removed", cancellationToken);
        return user;
    }

    private async Task RequireActiveTokenAsync(string token, Guid actorUserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) ||
            !await repository.CurrentTenantTokenHashIsActiveAsync(HashToken(token), cancellationToken))
        {
            await WriteAuditAsync(actorUserId, AuditAction.Rejected, "ScimProvisioningConfiguration", tenantContext.TenantId, "SCIM token validation failed.", "token_invalid", cancellationToken);
            throw new ScimProvisioningValidationException("SCIM token is invalid or revoked.");
        }
    }

    private async Task<string> ResolveRoleAsync(
        IReadOnlyCollection<string> groups,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (groups.Count == 0)
        {
            return RoleCatalog.Contributor;
        }

        var roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in groups)
        {
            var mapping = await repository.GetGroupMappingForCurrentTenantAsync(NormalizeRequired(group, nameof(groups), 200), cancellationToken);
            if (mapping is null)
            {
                await WriteAuditAsync(actorUserId, AuditAction.Rejected, "ScimGroupMapping", group, "SCIM group mapping was not found.", "invalid_group_mapping", cancellationToken);
                throw new ScimProvisioningValidationException($"SCIM group '{group}' is not mapped to a GCCS role.");
            }

            roles.Add(mapping.RoleName);
        }

        if (roles.Count > 1)
        {
            await WriteAuditAsync(actorUserId, AuditAction.Rejected, "ScimGroupMapping", string.Join(",", groups), "Conflicting SCIM group mappings were rejected.", "conflict", cancellationToken);
            throw new ScimProvisioningValidationException("SCIM groups map to conflicting GCCS roles.");
        }

        return roles.Single();
    }

    private Task WriteAuditAsync(
        Guid actorUserId,
        AuditAction action,
        string entityType,
        object entityId,
        string summary,
        string eventType,
        CancellationToken cancellationToken) =>
        auditEventWriter.WriteAsync(
            tenantContext.TenantId,
            actorUserId,
            action,
            entityType,
            entityId.ToString() ?? string.Empty,
            summary,
            new Dictionary<string, string> { ["scimEventType"] = eventType },
            cancellationToken);

    private static void ValidateConfirmation(string confirmationText)
    {
        if (!string.Equals(confirmationText?.Trim(), "ENABLE SCIM", StringComparison.Ordinal))
        {
            throw new ScimProvisioningValidationException("Confirmation text must be 'ENABLE SCIM'.");
        }
    }

    private static string NormalizeEmail(string email)
    {
        var normalized = NormalizeRequired(email, nameof(email), 320).ToLowerInvariant();
        if (!normalized.Contains('@', StringComparison.Ordinal))
        {
            throw new ScimProvisioningValidationException("SCIM userName must be a valid email address.");
        }

        return normalized;
    }

    private static string NormalizeRequired(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ScimProvisioningValidationException($"{fieldName} is required.");
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ScimProvisioningValidationException($"{fieldName} must be {maxLength} characters or fewer.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ScimProvisioningValidationException($"Value must be {maxLength} characters or fewer.");
        }

        return normalized;
    }

    private static string CreateToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()));
        return Convert.ToHexString(hash);
    }
}

public interface IScimProvisioningRepository
{
    Task<ScimProvisioningConfigurationDto?> GetConfigurationForCurrentTenantAsync(CancellationToken cancellationToken = default);
    Task<ScimProvisioningConfigurationDto> UpsertConfigurationForCurrentTenantAsync(bool enabled, string tokenHash, string? endpointLabel, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<ScimProvisioningConfigurationDto?> RotateTokenForCurrentTenantAsync(string tokenHash, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<ScimProvisioningConfigurationDto?> RevokeTokenForCurrentTenantAsync(Guid actorUserId, CancellationToken cancellationToken = default);
    Task<bool> CurrentTenantTokenHashIsActiveAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<ScimGroupMappingDto> UpsertGroupMappingForCurrentTenantAsync(string groupDisplayName, string roleName, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<ScimGroupMappingDto?> GetGroupMappingForCurrentTenantAsync(string groupDisplayName, CancellationToken cancellationToken = default);
    Task<ScimProvisionedUserDto?> GetProvisionedIdentityForCurrentTenantAsync(string externalId, CancellationToken cancellationToken = default);
    Task<bool> ExternalIdentityExistsOutsideCurrentTenantAsync(string externalId, CancellationToken cancellationToken = default);
    Task<ScimProvisionedUserDto> UpsertProvisionedUserForCurrentTenantAsync(string externalId, string userName, string displayName, string roleName, UserStatus userStatus, MembershipStatus membershipStatus, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<ScimProvisionedUserDto?> UpdateProvisionedUserStatusForCurrentTenantAsync(string externalId, UserStatus userStatus, MembershipStatus membershipStatus, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<ScimProvisionedUserDto?> UpdateProvisionedUserRoleForCurrentTenantAsync(string externalId, string roleName, Guid actorUserId, CancellationToken cancellationToken = default);
    Task TouchLastSyncForCurrentTenantAsync(Guid actorUserId, CancellationToken cancellationToken = default);
}

public sealed record EnableScimProvisioningRequest(string ConfirmationText, string? EndpointLabel = null);
public sealed record UpsertScimGroupMappingRequest(string GroupDisplayName, string RoleName);
public sealed record ScimProvisionUserRequest(string Token, string ExternalId, string UserName, string DisplayName, bool Active = true, IReadOnlyList<string>? Groups = null);
public sealed record ScimTokenRequest(string Token);
public sealed record ScimGroupAssignmentRequest(string Token, string GroupDisplayName);
public sealed record ScimTokenLifecycleResult(ScimProvisioningConfigurationDto Configuration, string Token);

public sealed record ScimProvisioningConfigurationDto(
    Guid Id,
    Guid TenantId,
    bool Enabled,
    string? EndpointLabel,
    DateTimeOffset? LastSyncAt,
    DateTimeOffset? TokenRotatedAt,
    DateTimeOffset? TokenRevokedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record ScimGroupMappingDto(Guid Id, Guid TenantId, string GroupDisplayName, string RoleName, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);

public sealed record ScimProvisionedUserDto(
    Guid Id,
    Guid TenantId,
    Guid UserId,
    Guid MembershipId,
    string ExternalId,
    string UserName,
    string DisplayName,
    string RoleName,
    UserStatus UserStatus,
    MembershipStatus MembershipStatus,
    DateTimeOffset? LastProvisionedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed class ScimProvisioningValidationException(string message) : InvalidOperationException(message);
