using Gccs.Application.Portals;

namespace Gccs.Infrastructure.Portals;

public sealed class InMemoryPortalPackageLifecycleRepository : IPortalPackageLifecycleRepository
{
    private readonly Lock _gate = new();
    private readonly Dictionary<Guid, SharedPortalPackageDto> _packages = [];
    private readonly List<PortalPackageActivityDto> _activities = [];

    public Task<SharedPortalPackageDto> CreateAsync(
        SharedPortalPackageRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var version = _packages.Values
                .Where(package => package.TenantId == tenantId && package.InvitationId == request.InvitationId)
                .Select(package => package.Version)
                .DefaultIfEmpty()
                .Max() + 1;
            var now = DateTimeOffset.UtcNow;
            var package = new SharedPortalPackageDto(
                Guid.NewGuid(), tenantId, request.PackageId, request.InvitationId, version,
                SharedPortalPackageState.Active, request.ExpiresAt,
                request.ExpiresAt.AddDays(-request.ExpirationReminderDays), null,
                null, null, null, null, null, now, null);
            _packages.Add(package.Id, package);
            return Task.FromResult(package);
        }
    }

    public Task<IReadOnlyList<SharedPortalPackageDto>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
            return Task.FromResult<IReadOnlyList<SharedPortalPackageDto>>(
                _packages.Values.Where(package => package.TenantId == tenantId)
                    .OrderByDescending(package => package.CreatedAt).ToArray());
    }

    public Task<SharedPortalPackageDto?> FindAsync(Guid sharedPackageId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
            return Task.FromResult(_packages.TryGetValue(sharedPackageId, out var package) && package.TenantId == tenantId
                ? package
                : null);
    }

    public Task<IReadOnlyList<SharedPortalPackageDto>> ListAccessibleAsync(
        Guid tenantId,
        Guid invitationId,
        DateTimeOffset asOf,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
            return Task.FromResult<IReadOnlyList<SharedPortalPackageDto>>(
                _packages.Values.Where(package =>
                    package.TenantId == tenantId &&
                    package.InvitationId == invitationId &&
                    package.State == SharedPortalPackageState.Active &&
                    package.ExpiresAt > asOf).ToArray());
    }

    public Task<SharedPortalPackageDto?> SetStateAsync(
        Guid sharedPackageId,
        Guid tenantId,
        SharedPortalPackageState expectedState,
        SharedPortalPackageState state,
        string? reason,
        Guid actorUserId,
        DateTimeOffset changedAt,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (!_packages.TryGetValue(sharedPackageId, out var existing) || existing.TenantId != tenantId)
                return Task.FromResult<SharedPortalPackageDto?>(null);
            if (existing.State != expectedState)
                throw new PortalPackageLifecycleConflictException("The shared package changed. Reload it and try again.");

            var updated = existing with
            {
                State = state,
                RevocationReason = state == SharedPortalPackageState.Revoked ? reason : existing.RevocationReason,
                RevokedAt = state == SharedPortalPackageState.Revoked ? changedAt : existing.RevokedAt,
                UpdatedAt = changedAt
            };
            _packages[existing.Id] = updated;
            AddActivity(updated, ToActivity(state), actorUserId, reason, changedAt);
            return Task.FromResult<SharedPortalPackageDto?>(updated);
        }
    }

    public Task<SharedPortalPackageDto?> SupersedeAsync(
        Guid sharedPackageId,
        Guid tenantId,
        SharedPortalPackageState expectedState,
        SharedPortalPackageDto replacement,
        Guid actorUserId,
        DateTimeOffset changedAt,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (!_packages.TryGetValue(sharedPackageId, out var existing) || existing.TenantId != tenantId)
                return Task.FromResult<SharedPortalPackageDto?>(null);
            if (existing.State != expectedState)
                throw new PortalPackageLifecycleConflictException("The shared package changed. Reload it and try again.");

            var updated = existing with
            {
                State = SharedPortalPackageState.Superseded,
                ReplacementSharedPackageId = replacement.Id,
                ReplacementPackageId = replacement.PackageId,
                UpdatedAt = changedAt
            };
            _packages[existing.Id] = updated;
            AddActivity(updated, PortalPackageActivityType.Supersede, actorUserId, replacement.Id.ToString(), changedAt);
            return Task.FromResult<SharedPortalPackageDto?>(updated);
        }
    }

    public Task<SharedPortalPackageDto> ReissueAsync(
        SharedPortalPackageDto existing,
        ReissueSharedPortalPackageRequest request,
        Guid actorUserId,
        DateTimeOffset changedAt,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (!_packages.TryGetValue(existing.Id, out var current) ||
                current.TenantId != existing.TenantId ||
                current.State != existing.State)
                throw new PortalPackageLifecycleConflictException("The shared package changed. Reload it and try again.");

            var nextVersion = _packages.Values
                .Where(package => package.TenantId == current.TenantId && package.InvitationId == current.InvitationId)
                .Select(package => package.Version)
                .DefaultIfEmpty()
                .Max() + 1;
            var replacement = new SharedPortalPackageDto(
                Guid.NewGuid(), current.TenantId, request.ReplacementPackageId, current.InvitationId,
                nextVersion, SharedPortalPackageState.Active, request.ExpiresAt,
                request.ExpiresAt.AddDays(-request.ExpirationReminderDays), null,
                current.Id, null, null, null, null, changedAt, null);
            var oldState = current.State == SharedPortalPackageState.Active
                ? SharedPortalPackageState.Superseded
                : current.State;
            _packages[current.Id] = current with
            {
                State = oldState,
                ReplacementSharedPackageId = replacement.Id,
                ReplacementPackageId = replacement.PackageId,
                UpdatedAt = changedAt
            };
            _packages.Add(replacement.Id, replacement);
            AddActivity(current, PortalPackageActivityType.Reissue, actorUserId, replacement.Id.ToString(), changedAt);
            return Task.FromResult(replacement);
        }
    }

    public Task<bool> CanAccessAsync(
        Guid sharedPackageId,
        Guid tenantId,
        Guid invitationId,
        Guid packageId,
        DateTimeOffset asOf,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
            return Task.FromResult(_packages.TryGetValue(sharedPackageId, out var package) &&
                package.TenantId == tenantId &&
                package.InvitationId == invitationId &&
                package.PackageId == packageId &&
                package.State == SharedPortalPackageState.Active &&
                package.ExpiresAt > asOf);
    }

    public Task RecordActivityAsync(
        Guid sharedPackageId,
        Guid tenantId,
        PortalPackageActivityType type,
        Guid actorUserId,
        string? detail,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (!_packages.TryGetValue(sharedPackageId, out var package) || package.TenantId != tenantId)
                throw new PortalPackageAccessDeniedException("The shared package is unavailable.");
            AddActivity(package, type, actorUserId, detail, occurredAt);
            return Task.CompletedTask;
        }
    }

    public Task<PortalPackageActivityReportDto> GenerateActivityReportAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
            return Task.FromResult(new PortalPackageActivityReportDto(
                tenantId,
                _activities.Where(activity => activity.TenantId == tenantId)
                    .OrderByDescending(activity => activity.OccurredAt).ToArray()));
    }

    public Task<IReadOnlyList<SharedPortalPackageDto>> ListDueForMaintenanceAsync(
        DateTimeOffset asOf,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
            return Task.FromResult<IReadOnlyList<SharedPortalPackageDto>>(
                _packages.Values.Where(package => package.State == SharedPortalPackageState.Active &&
                    (package.ExpiresAt <= asOf || (package.ReminderSentAt is null && package.ReminderAt <= asOf)))
                    .ToArray());
    }

    public Task<bool> MarkReminderSentAsync(
        Guid sharedPackageId,
        Guid tenantId,
        DateTimeOffset sentAt,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (!_packages.TryGetValue(sharedPackageId, out var existing) ||
                existing.TenantId != tenantId ||
                existing.State != SharedPortalPackageState.Active ||
                existing.ReminderSentAt is not null)
                return Task.FromResult(false);
            var updated = existing with { ReminderSentAt = sentAt, UpdatedAt = sentAt };
            _packages[existing.Id] = updated;
            AddActivity(updated, PortalPackageActivityType.ExpirationReminder, Guid.Empty,
                $"expires:{updated.ExpiresAt:O}", sentAt);
            return Task.FromResult(true);
        }
    }

    private void AddActivity(
        SharedPortalPackageDto package,
        PortalPackageActivityType type,
        Guid actorUserId,
        string? detail,
        DateTimeOffset occurredAt) =>
        _activities.Add(new PortalPackageActivityDto(
            Guid.NewGuid(), package.Id, package.TenantId, type, actorUserId, occurredAt, detail));

    private static PortalPackageActivityType ToActivity(SharedPortalPackageState state) =>
        state switch
        {
            SharedPortalPackageState.Expired => PortalPackageActivityType.Expiration,
            SharedPortalPackageState.Revoked => PortalPackageActivityType.Revocation,
            SharedPortalPackageState.Superseded => PortalPackageActivityType.Supersede,
            SharedPortalPackageState.Archived => PortalPackageActivityType.Archive,
            _ => throw new PortalPackageLifecycleException("The requested state is not a lifecycle activity.")
        };
}
