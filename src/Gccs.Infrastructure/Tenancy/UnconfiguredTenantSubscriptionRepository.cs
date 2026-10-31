using Gccs.Application.Tenancy;
using Gccs.Domain.Tenancy;

namespace Gccs.Infrastructure.Tenancy;

internal sealed class UnconfiguredTenantSubscriptionRepository : ITenantSubscriptionRepository
{
    public Task<TenantSubscription?> FindByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult<TenantSubscription?>(null);

    public Task<PlatformPilotSubscriptionPage> ListPilotsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Tenant subscription persistence requires ConnectionStrings:GccsDatabase to be configured.");

    public Task<TenantSubscriptionTransitionResult?> FindReplayAsync(
        Guid tenantId,
        string idempotencyKey,
        string requestFingerprint,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<TenantSubscriptionTransitionResult?>(null);

    public Task<TenantSubscriptionTransitionResult?> TransitionAsync(
        Guid tenantId,
        long expectedVersion,
        SubscriptionTransition transition,
        SubscriptionTransitionValues values,
        string idempotencyKey,
        string requestFingerprint,
        Guid actorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Tenant subscription persistence requires ConnectionStrings:GccsDatabase to be configured.");
}
