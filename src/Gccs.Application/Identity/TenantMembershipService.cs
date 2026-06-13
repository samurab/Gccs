using Gccs.Application.Audit;
using Gccs.Application.Security;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Domain.Identity;

namespace Gccs.Application.Identity;

public sealed class TenantMembershipService(
    ITenantMembershipRepository membershipRepository,
    ICurrentTenantContext tenantContext,
    IAuditEventWriter auditEventWriter)
{
    public Task<IReadOnlyList<TenantMemberDto>> ListCurrentTenantMembersAsync(
        CancellationToken cancellationToken = default) =>
        membershipRepository.ListCurrentTenantMembersAsync(cancellationToken);

    public async Task<TenantMemberDto> AssignAsync(
        AssignTenantMemberRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        if (request.Status is MembershipStatus.Deactivated)
        {
            throw new ArgumentException("New memberships must be active or suspended.", nameof(request));
        }

        if (await membershipRepository.CurrentTenantMembershipExistsAsync(request.UserId, cancellationToken))
        {
            throw new DuplicateMembershipException("User is already a member of the current tenant.");
        }

        var roleName = NormalizeRoleName(request.RoleName);
        var now = DateTimeOffset.UtcNow;
        var user = new User(
            request.UserId,
            tenantContext.TenantId,
            request.Email.Trim().ToLowerInvariant(),
            request.DisplayName.Trim(),
            UserStatus.Active,
            false,
            [],
            null,
            new EntityAudit(now, actorUserId, null, null));
        var membership = new TenantMembership(
            Guid.NewGuid(),
            tenantContext.TenantId,
            request.UserId,
            request.Status,
            roleName,
            null,
            new EntityAudit(now, actorUserId, null, null));

        var member = await membershipRepository.AddToCurrentTenantAsync(user, membership, cancellationToken);

        await auditEventWriter.WriteAsync(
            tenantContext.TenantId,
            actorUserId,
            AuditAction.Created,
            "TenantMembership",
            member.MembershipId.ToString(),
            $"User '{member.Email}' was added to the tenant membership list.",
            new Dictionary<string, string>
            {
                ["userId"] = member.UserId.ToString(),
                ["membershipStatus"] = member.MembershipStatus.ToString(),
                ["roleName"] = member.RoleName
            },
            cancellationToken);

        return member;
    }

    public async Task<TenantMemberDto?> UpdateStatusAsync(
        Guid membershipId,
        UpdateTenantMembershipStatusRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var member = await membershipRepository.UpdateStatusInCurrentTenantScopeAsync(
            membershipId,
            request.Status,
            actorUserId,
            cancellationToken);

        if (member is null)
        {
            return null;
        }

        await auditEventWriter.WriteAsync(
            tenantContext.TenantId,
            actorUserId,
            AuditAction.Updated,
            "TenantMembership",
            member.MembershipId.ToString(),
            $"User '{member.Email}' membership status changed to {member.MembershipStatus}.",
            new Dictionary<string, string>
            {
                ["userId"] = member.UserId.ToString(),
                ["membershipStatus"] = member.MembershipStatus.ToString(),
                ["roleName"] = member.RoleName
            },
            cancellationToken);

        return member;
    }

    private static void ValidateRequest(AssignTenantMemberRequest request)
    {
        if (request.UserId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Trim().Length > 320 || !request.Email.Contains('@', StringComparison.Ordinal))
        {
            throw new ArgumentException("A valid member email is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName) || request.DisplayName.Trim().Length > 200)
        {
            throw new ArgumentException("Member display name is required and must be 200 characters or fewer.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.RoleName) || request.RoleName.Trim().Length > 120)
        {
            throw new ArgumentException("Membership role name is required and must be 120 characters or fewer.", nameof(request));
        }

        NormalizeRoleName(request.RoleName);
    }

    private static string NormalizeRoleName(string roleName)
    {
        if (!RoleCatalog.TryNormalizeRoleName(roleName, out var canonicalRoleName))
        {
            throw new ArgumentException(
                $"Membership role must be one of: {string.Join(", ", RoleCatalog.Roles)}.",
                nameof(roleName));
        }

        return canonicalRoleName;
    }
}

public sealed class DuplicateMembershipException(string message) : InvalidOperationException(message);
