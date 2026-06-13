using System.Security.Cryptography;
using Gccs.Application.Audit;
using Gccs.Application.Security;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Domain.Identity;

namespace Gccs.Application.Identity;

public sealed class TenantInvitationService(
    ITenantInvitationRepository invitationRepository,
    ICurrentTenantContext tenantContext,
    IAuditEventWriter auditEventWriter)
{
    private const int MaximumExpirationDays = 30;

    public Task<IReadOnlyList<TenantInvitationDto>> ListCurrentTenantInvitationsAsync(
        CancellationToken cancellationToken = default) =>
        invitationRepository.ListCurrentTenantInvitationsAsync(cancellationToken);

    public async Task<TenantInvitationDto> CreateAsync(
        CreateTenantInvitationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateCreateRequest(request);

        var email = NormalizeEmail(request.Email);
        var roleName = NormalizeRoleName(request.RoleName);
        if (await invitationRepository.CurrentTenantPendingInvitationExistsAsync(email, cancellationToken))
        {
            throw new DuplicateInvitationException("A pending invitation already exists for this email in the current tenant.");
        }

        var now = DateTimeOffset.UtcNow;
        var invitationToken = GenerateToken();
        var notificationPlaceholder = $"Local invitation notification queued for {email} with token {invitationToken}.";
        var invitation = new TenantInvitation(
            Guid.NewGuid(),
            tenantContext.TenantId,
            email,
            roleName,
            invitationToken,
            TenantInvitationStatus.Pending,
            now.AddDays(request.ExpiresInDays),
            null,
            null,
            null,
            null,
            now,
            notificationPlaceholder,
            new EntityAudit(now, actorUserId, null, null));

        var createdInvitation = await invitationRepository.AddToCurrentTenantAsync(invitation, cancellationToken);

        await WriteInvitationAuditAsync(
            createdInvitation,
            actorUserId,
            AuditAction.Created,
            $"Invitation for '{createdInvitation.Email}' was created with role {createdInvitation.RoleName}.",
            cancellationToken);

        return createdInvitation;
    }

    public async Task<TenantInvitationDto?> AcceptAsync(
        string invitationToken,
        AcceptTenantInvitationRequest request,
        Guid actorUserId,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(invitationToken))
        {
            throw new ArgumentException("Invitation token is required.", nameof(invitationToken));
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName) || request.DisplayName.Trim().Length > 200)
        {
            throw new ArgumentException("Display name is required and must be 200 characters or fewer.", nameof(request));
        }

        var invitation = await invitationRepository.FindByTokenAsync(invitationToken.Trim(), cancellationToken);
        if (invitation is null)
        {
            return null;
        }

        if (!string.Equals(invitation.Email, NormalizeEmail(actorEmail), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidInvitationStateException("The authenticated email does not match this invitation.");
        }

        if (invitation.Status is TenantInvitationStatus.Revoked)
        {
            throw new InvalidInvitationStateException("Revoked invitations cannot be accepted.");
        }

        if (invitation.Status is TenantInvitationStatus.Expired || invitation.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new InvalidInvitationStateException("Expired invitations cannot be accepted.");
        }

        if (invitation.Status is not TenantInvitationStatus.Pending)
        {
            throw new InvalidInvitationStateException("Only pending invitations can be accepted.");
        }

        var acceptedInvitation = await invitationRepository.AcceptAsync(
            invitation.InvitationId,
            actorUserId,
            invitation.Email,
            request.DisplayName.Trim(),
            actorUserId,
            cancellationToken);

        if (acceptedInvitation is null)
        {
            return null;
        }

        await WriteInvitationAuditAsync(
            acceptedInvitation,
            actorUserId,
            AuditAction.Updated,
            $"Invitation for '{acceptedInvitation.Email}' was accepted.",
            cancellationToken);

        return acceptedInvitation;
    }

    public async Task<TenantInvitationDto?> ExpireAsync(
        Guid invitationId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var invitation = await invitationRepository.ExpireInCurrentTenantScopeAsync(
            invitationId,
            actorUserId,
            cancellationToken);

        if (invitation is null)
        {
            return null;
        }

        await WriteInvitationAuditAsync(
            invitation,
            actorUserId,
            AuditAction.Updated,
            $"Invitation for '{invitation.Email}' was expired.",
            cancellationToken);

        return invitation;
    }

    public async Task<TenantInvitationDto?> RevokeAsync(
        Guid invitationId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var invitation = await invitationRepository.RevokeInCurrentTenantScopeAsync(
            invitationId,
            actorUserId,
            cancellationToken);

        if (invitation is null)
        {
            return null;
        }

        await WriteInvitationAuditAsync(
            invitation,
            actorUserId,
            AuditAction.Updated,
            $"Invitation for '{invitation.Email}' was revoked.",
            cancellationToken);

        return invitation;
    }

    private Task WriteInvitationAuditAsync(
        TenantInvitationDto invitation,
        Guid actorUserId,
        AuditAction action,
        string summary,
        CancellationToken cancellationToken) =>
        auditEventWriter.WriteAsync(
            invitation.TenantId,
            actorUserId,
            action,
            "TenantInvitation",
            invitation.InvitationId.ToString(),
            summary,
            new Dictionary<string, string>
            {
                ["email"] = invitation.Email,
                ["roleName"] = invitation.RoleName,
                ["status"] = invitation.Status.ToString(),
                ["expiresAt"] = invitation.ExpiresAt.ToString("O"),
                ["notificationPlaceholder"] = invitation.NotificationPlaceholder
            },
            cancellationToken);

    private static void ValidateCreateRequest(CreateTenantInvitationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Trim().Length > 320 || !request.Email.Contains('@', StringComparison.Ordinal))
        {
            throw new ArgumentException("A valid invitation email is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.RoleName) || request.RoleName.Trim().Length > 120)
        {
            throw new ArgumentException("Invitation role name is required and must be 120 characters or fewer.", nameof(request));
        }

        NormalizeRoleName(request.RoleName);

        if (request.ExpiresInDays is < 1 or > MaximumExpirationDays)
        {
            throw new ArgumentException($"Invitation expiration must be between 1 and {MaximumExpirationDays} days.", nameof(request));
        }
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string NormalizeRoleName(string roleName)
    {
        if (!RoleCatalog.TryNormalizeRoleName(roleName, out var canonicalRoleName))
        {
            throw new ArgumentException(
                $"Invitation role must be one of: {string.Join(", ", RoleCatalog.Roles)}.",
                nameof(roleName));
        }

        return canonicalRoleName;
    }

    private static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}

public sealed class DuplicateInvitationException(string message) : InvalidOperationException(message);

public sealed class InvalidInvitationStateException(string message) : InvalidOperationException(message);
