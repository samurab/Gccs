using Gccs.Application.Portals;

namespace Gccs.Infrastructure.Portals;

public sealed class InMemoryExternalPortalAccessRepository : IExternalPortalAccessRepository
{
    private readonly List<ExternalPortalInvitationDto> _invitations = [];

    public Task<ExternalPortalInvitationDto> CreateInvitationAsync(
        ExternalPortalInvitationRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var invitation = new ExternalPortalInvitationDto(
            Guid.NewGuid(),
            tenantId,
            request.Email.Trim(),
            request.Role,
            request.PackageIds.Distinct().ToArray(),
            request.ContractIds.Distinct().ToArray(),
            request.ExpiresAt,
            request.CanDownload,
            request.StrongAuthenticationRequired,
            ExternalPortalInvitationStatus.Pending,
            DateTimeOffset.UtcNow,
            null);
        _invitations.Add(invitation);
        return Task.FromResult(invitation);
    }

    public Task<ExternalPortalInvitationDto?> FindInvitationAsync(Guid invitationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_invitations.SingleOrDefault(invitation => invitation.Id == invitationId));

    public Task<ExternalPortalInvitationDto?> UpdateStatusAsync(
        Guid invitationId,
        ExternalPortalInvitationStatus status,
        DateTimeOffset? expiresAt,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = _invitations.SingleOrDefault(invitation => invitation.Id == invitationId);
        if (existing is null)
        {
            return Task.FromResult<ExternalPortalInvitationDto?>(null);
        }

        var updated = existing with
        {
            Status = status,
            ExpiresAt = expiresAt ?? existing.ExpiresAt,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _invitations.Remove(existing);
        _invitations.Add(updated);
        return Task.FromResult<ExternalPortalInvitationDto?>(updated);
    }
}
