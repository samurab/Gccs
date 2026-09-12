using Gccs.Application.Portals;

namespace Gccs.Infrastructure.Portals;

public sealed class InMemoryExternalPortalAccessRepository : IExternalPortalAccessRepository
{
    private readonly object _gate = new();
    private readonly List<ExternalPortalInvitationDto> _invitations = [];
    private readonly List<ExternalPortalAccessHistoryDto> _history = [];

    public Task<ExternalPortalInvitationDto> CreateInvitationAsync(
        ExternalPortalInvitationRequest request, Guid tenantId, Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var invitation = new ExternalPortalInvitationDto(
            Guid.NewGuid(), tenantId, request.Email.Trim(), request.Role,
            request.PackageIds.Distinct().ToArray(), request.ContractIds.Distinct().ToArray(), request.ExpiresAt,
            request.CanDownload, request.StrongAuthenticationRequired, ExternalPortalInvitationStatus.Pending,
            null, null, null, null, 0, null, 1, DateTimeOffset.UtcNow, null);
        lock (_gate) _invitations.Add(invitation);
        return Task.FromResult(invitation);
    }

    public Task<IReadOnlyList<ExternalPortalInvitationDto>> ListInvitationsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        lock (_gate) return Task.FromResult<IReadOnlyList<ExternalPortalInvitationDto>>(
            _invitations.Where(item => item.TenantId == tenantId).OrderByDescending(item => item.CreatedAt).ToArray());
    }

    public Task<ExternalPortalInvitationDto?> FindInvitationAsync(Guid invitationId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        lock (_gate) return Task.FromResult(_invitations.SingleOrDefault(item => item.Id == invitationId && item.TenantId == tenantId));
    }

    public Task<ExternalPortalInvitationDto?> FindInvitationForAccessAsync(Guid invitationId, CancellationToken cancellationToken = default)
    {
        lock (_gate) return Task.FromResult(_invitations.SingleOrDefault(item => item.Id == invitationId));
    }

    public Task<ExternalPortalInvitationDto?> FindInvitationAsync(Guid invitationId, CancellationToken cancellationToken = default) =>
        FindInvitationForAccessAsync(invitationId, cancellationToken);

    public Task<ExternalPortalInvitationDto?> ResendAsync(
        Guid invitationId, Guid tenantId, long expectedVersion, Guid actorUserId, DateTimeOffset changedAt,
        CancellationToken cancellationToken = default) =>
        UpdateAsync(invitationId, tenantId, expectedVersion, item => item with
        {
            ResendCount = item.ResendCount + 1,
            LastResentAt = changedAt,
            UpdatedAt = changedAt,
            Version = item.Version + 1
        });

    public Task<ExternalPortalInvitationDto?> ExtendAsync(
        Guid invitationId, Guid tenantId, long expectedVersion, DateTimeOffset expiresAt, Guid actorUserId,
        DateTimeOffset changedAt, CancellationToken cancellationToken = default) =>
        UpdateAsync(invitationId, tenantId, expectedVersion, item => item with
        {
            ExpiresAt = expiresAt,
            UpdatedAt = changedAt,
            Version = item.Version + 1
        });

    public Task<ExternalPortalInvitationDto?> RevokeAsync(
        Guid invitationId, Guid tenantId, long expectedVersion, string reason, Guid actorUserId,
        DateTimeOffset changedAt, CancellationToken cancellationToken = default) =>
        UpdateAsync(invitationId, tenantId, expectedVersion, item => item with
        {
            Status = ExternalPortalInvitationStatus.Revoked,
            RevokedAt = changedAt,
            RevocationReason = reason,
            UpdatedAt = changedAt,
            Version = item.Version + 1
        });

    public Task<ExternalPortalInvitationDto?> RecordAccessAsync(
        Guid invitationId, Guid tenantId, Guid actorUserId, Guid packageId, Guid? contractId, bool allowed,
        string resultCode, DateTimeOffset occurredAt, bool bindExternalUser,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var index = _invitations.FindIndex(item => item.Id == invitationId && item.TenantId == tenantId);
            if (index < 0) return Task.FromResult<ExternalPortalInvitationDto?>(null);
            var existing = _invitations[index];
            var updated = existing;
            if (allowed)
            {
                if (existing.Status == ExternalPortalInvitationStatus.Revoked ||
                    existing.ExpiresAt <= occurredAt ||
                    existing.ExternalUserId is not null && existing.ExternalUserId != actorUserId)
                    return Task.FromResult<ExternalPortalInvitationDto?>(null);
                updated = existing with
                {
                    Status = ExternalPortalInvitationStatus.Accepted,
                    ExternalUserId = bindExternalUser ? existing.ExternalUserId ?? actorUserId : existing.ExternalUserId,
                    LastAccessedAt = occurredAt,
                    UpdatedAt = occurredAt,
                    Version = existing.Version + 1
                };
                _invitations[index] = updated;
            }
            _history.Add(new ExternalPortalAccessHistoryDto(
                Guid.NewGuid(), invitationId, tenantId, actorUserId, packageId, contractId, allowed, resultCode, occurredAt));
            return Task.FromResult<ExternalPortalInvitationDto?>(updated);
        }
    }

    public Task<IReadOnlyList<ExternalPortalAccessHistoryDto>> ListAccessHistoryAsync(
        Guid invitationId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        lock (_gate) return Task.FromResult<IReadOnlyList<ExternalPortalAccessHistoryDto>>(
            _history.Where(item => item.InvitationId == invitationId && item.TenantId == tenantId)
                .OrderByDescending(item => item.OccurredAt).ToArray());
    }

    private Task<ExternalPortalInvitationDto?> UpdateAsync(
        Guid invitationId, Guid tenantId, long expectedVersion,
        Func<ExternalPortalInvitationDto, ExternalPortalInvitationDto> update)
    {
        lock (_gate)
        {
            var index = _invitations.FindIndex(item =>
                item.Id == invitationId && item.TenantId == tenantId && item.Version == expectedVersion);
            if (index < 0) return Task.FromResult<ExternalPortalInvitationDto?>(null);
            var updated = update(_invitations[index]);
            _invitations[index] = updated;
            return Task.FromResult<ExternalPortalInvitationDto?>(updated);
        }
    }
}
