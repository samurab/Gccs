using Gccs.Application.Audit;
using Gccs.Domain.Audit;

namespace Gccs.Application.Portals;

public sealed class ExternalPortalAccessService(
    IExternalPortalAccessRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public async Task<ExternalPortalInvitationDto> InviteAsync(
        ExternalPortalInvitationRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        Validate(request);
        var invitation = await repository.CreateInvitationAsync(request, tenantId, actorUserId, cancellationToken);
        await WriteAuditAsync(invitation, actorUserId, AuditAction.Created, "External portal invitation was created.", cancellationToken);
        return invitation;
    }

    public async Task<ExternalPortalAccessResultDto> ValidateAccessAsync(
        Guid invitationId,
        Guid packageId,
        Guid? contractId,
        DateTimeOffset asOf,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var invitation = await repository.FindInvitationAsync(invitationId, cancellationToken);
        if (invitation is null || invitation.Status == ExternalPortalInvitationStatus.Revoked || invitation.ExpiresAt <= asOf)
        {
            return new(false, "Invitation expired or revoked.", null);
        }

        var allowed = invitation.PackageIds.Contains(packageId) &&
            (contractId is null || invitation.ContractIds.Contains(contractId.Value));
        if (allowed)
        {
            await WriteAuditAsync(invitation, actorUserId, AuditAction.Viewed, "External portal package access was granted.", cancellationToken);
        }

        return new(allowed, allowed ? "Access granted." : "Package or contract is outside portal scope.", invitation);
    }

    public Task EnsureReadOnlyAsync() =>
        throw new ExternalPortalAccessException("External portal users have read-only access and cannot modify tenant workspace data.");

    public async Task<ExternalPortalInvitationDto?> ResendAsync(Guid invitationId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var invitation = await repository.UpdateStatusAsync(invitationId, ExternalPortalInvitationStatus.Pending, null, actorUserId, cancellationToken);
        if (invitation is not null)
        {
            await WriteAuditAsync(invitation, actorUserId, AuditAction.Updated, "External portal invitation was resent.", cancellationToken);
        }

        return invitation;
    }

    public async Task<ExternalPortalInvitationDto?> ExtendAsync(Guid invitationId, DateTimeOffset expiresAt, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var invitation = await repository.UpdateStatusAsync(invitationId, ExternalPortalInvitationStatus.Pending, expiresAt, actorUserId, cancellationToken);
        if (invitation is not null)
        {
            await WriteAuditAsync(invitation, actorUserId, AuditAction.Updated, "External portal invitation was extended.", cancellationToken);
        }

        return invitation;
    }

    public async Task<ExternalPortalInvitationDto?> RevokeAsync(Guid invitationId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var invitation = await repository.UpdateStatusAsync(invitationId, ExternalPortalInvitationStatus.Revoked, null, actorUserId, cancellationToken);
        if (invitation is not null)
        {
            await WriteAuditAsync(invitation, actorUserId, AuditAction.PermissionChanged, "External portal invitation was revoked.", cancellationToken);
        }

        return invitation;
    }

    private static void Validate(ExternalPortalInvitationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || request.PackageIds.Count == 0)
        {
            throw new ExternalPortalAccessException("Portal invitation requires an email and at least one package.");
        }

        if (request.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new ExternalPortalAccessException("Portal invitation expiration must be in the future.");
        }
    }

    private async Task WriteAuditAsync(ExternalPortalInvitationDto invitation, Guid actorUserId, AuditAction action, string summary, CancellationToken cancellationToken)
    {
        await auditEventWriter.WriteAsync(
            invitation.TenantId,
            actorUserId,
            action,
            "ExternalPortalInvitation",
            invitation.Id.ToString(),
            summary,
            new Dictionary<string, string>
            {
                ["role"] = invitation.Role.ToString(),
                ["status"] = invitation.Status.ToString(),
                ["packages"] = string.Join("|", invitation.PackageIds),
                ["contracts"] = string.Join("|", invitation.ContractIds)
            },
            cancellationToken);
    }
}

public interface IExternalPortalAccessRepository
{
    Task<ExternalPortalInvitationDto> CreateInvitationAsync(ExternalPortalInvitationRequest request, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<ExternalPortalInvitationDto?> FindInvitationAsync(Guid invitationId, CancellationToken cancellationToken = default);
    Task<ExternalPortalInvitationDto?> UpdateStatusAsync(Guid invitationId, ExternalPortalInvitationStatus status, DateTimeOffset? expiresAt, Guid actorUserId, CancellationToken cancellationToken = default);
}

public sealed record ExternalPortalInvitationRequest(
    string Email,
    ExternalPortalRole Role,
    IReadOnlyList<Guid> PackageIds,
    IReadOnlyList<Guid> ContractIds,
    DateTimeOffset ExpiresAt,
    bool CanDownload,
    bool StrongAuthenticationRequired);

public sealed record ExternalPortalInvitationDto(
    Guid Id,
    Guid TenantId,
    string Email,
    ExternalPortalRole Role,
    IReadOnlyList<Guid> PackageIds,
    IReadOnlyList<Guid> ContractIds,
    DateTimeOffset ExpiresAt,
    bool CanDownload,
    bool StrongAuthenticationRequired,
    ExternalPortalInvitationStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record ExternalPortalAccessResultDto(
    bool Allowed,
    string Reason,
    ExternalPortalInvitationDto? Invitation);

public enum ExternalPortalRole
{
    PrimeReviewer,
    Auditor
}

public enum ExternalPortalInvitationStatus
{
    Pending,
    Accepted,
    Revoked
}

public sealed class ExternalPortalAccessException(string message) : InvalidOperationException(message);
