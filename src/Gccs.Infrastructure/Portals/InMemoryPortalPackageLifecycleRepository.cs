using Gccs.Application.Portals;

namespace Gccs.Infrastructure.Portals;

public sealed class InMemoryPortalPackageLifecycleRepository : IPortalPackageLifecycleRepository
{
    private readonly List<SharedPortalPackageDto> _packages = [];
    private readonly List<PortalPackageActivityDto> _activities = [];

    public Task<SharedPortalPackageDto> CreateAsync(
        SharedPortalPackageRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var package = new SharedPortalPackageDto(
            Guid.NewGuid(),
            tenantId,
            request.PackageId,
            request.InvitationId,
            SharedPortalPackageState.Active,
            request.ExpiresAt,
            null,
            null,
            DateTimeOffset.UtcNow,
            null);
        _packages.Add(package);
        _activities.Add(Activity(package.Id, PortalPackageActivityType.Access, actorUserId, "shared"));
        return Task.FromResult(package);
    }

    public Task<SharedPortalPackageDto?> SetStateAsync(
        Guid sharedPackageId,
        SharedPortalPackageState state,
        string? reason,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = _packages.SingleOrDefault(package => package.Id == sharedPackageId);
        if (existing is null)
        {
            return Task.FromResult<SharedPortalPackageDto?>(null);
        }

        var updated = existing with
        {
            State = state,
            RevocationReason = state == SharedPortalPackageState.Revoked ? reason : existing.RevocationReason,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        Replace(existing, updated);
        _activities.Add(Activity(sharedPackageId, ToActivity(state), actorUserId, reason));
        return Task.FromResult<SharedPortalPackageDto?>(updated);
    }

    public Task<SharedPortalPackageDto?> SupersedeAsync(
        Guid sharedPackageId,
        Guid replacementPackageId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = _packages.SingleOrDefault(package => package.Id == sharedPackageId);
        if (existing is null)
        {
            return Task.FromResult<SharedPortalPackageDto?>(null);
        }

        var updated = existing with
        {
            State = SharedPortalPackageState.Superseded,
            ReplacementPackageId = replacementPackageId,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        Replace(existing, updated);
        _activities.Add(Activity(sharedPackageId, PortalPackageActivityType.Supersede, actorUserId, replacementPackageId.ToString()));
        return Task.FromResult<SharedPortalPackageDto?>(updated);
    }

    public Task<SharedPortalPackageDto?> ReissueAsync(
        Guid sharedPackageId,
        DateTimeOffset expiresAt,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = _packages.SingleOrDefault(package => package.Id == sharedPackageId);
        if (existing is null)
        {
            return Task.FromResult<SharedPortalPackageDto?>(null);
        }

        var updated = existing with
        {
            State = SharedPortalPackageState.Active,
            ExpiresAt = expiresAt,
            RevocationReason = null,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        Replace(existing, updated);
        _activities.Add(Activity(sharedPackageId, PortalPackageActivityType.Reissue, actorUserId, expiresAt.ToString("O")));
        return Task.FromResult<SharedPortalPackageDto?>(updated);
    }

    public Task<bool> CanAccessAsync(Guid sharedPackageId, DateTimeOffset asOf, CancellationToken cancellationToken = default)
    {
        var package = _packages.SingleOrDefault(candidate => candidate.Id == sharedPackageId);
        return Task.FromResult(package is { State: SharedPortalPackageState.Active } && package.ExpiresAt > asOf);
    }

    public Task RecordActivityAsync(Guid sharedPackageId, PortalPackageActivityType type, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        _activities.Add(Activity(sharedPackageId, type, actorUserId, null));
        return Task.CompletedTask;
    }

    public Task<PortalPackageActivityReportDto> GenerateActivityReportAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var sharedIds = _packages.Where(package => package.TenantId == tenantId).Select(package => package.Id).ToHashSet();
        return Task.FromResult(new PortalPackageActivityReportDto(
            tenantId,
            _activities.Where(activity => sharedIds.Contains(activity.SharedPackageId)).ToArray()));
    }

    private static PortalPackageActivityDto Activity(Guid sharedPackageId, PortalPackageActivityType type, Guid actorUserId, string? detail) =>
        new(sharedPackageId, type, actorUserId, DateTimeOffset.UtcNow, detail);

    private static PortalPackageActivityType ToActivity(SharedPortalPackageState state) =>
        state switch
        {
            SharedPortalPackageState.Expired => PortalPackageActivityType.Expiration,
            SharedPortalPackageState.Revoked => PortalPackageActivityType.Revocation,
            SharedPortalPackageState.Archived => PortalPackageActivityType.Archive,
            _ => PortalPackageActivityType.Access
        };

    private void Replace(SharedPortalPackageDto existing, SharedPortalPackageDto updated)
    {
        _packages.Remove(existing);
        _packages.Add(updated);
    }
}
