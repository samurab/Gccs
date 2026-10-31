using System.Text.Json;
using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Tenancy;

public sealed class EfTenantSubscriptionRepository(
    GccsDbContext dbContext,
    IAuditRequestMetadata requestMetadata,
    IApplicationTransaction transaction) : ITenantSubscriptionRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<TenantSubscription?> FindByTenantIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        Map(await dbContext.TenantSubscriptions
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.TenantId == tenantId, cancellationToken));

    public async Task<PlatformPilotSubscriptionPage> ListPilotsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.PlatformTenantOnboardings
            .AsNoTracking()
            .Include(candidate => candidate.Tenant)
                .ThenInclude(tenant => tenant!.Subscription)
            .Where(candidate =>
                candidate.Status == TenantOnboardingStatus.Active &&
                candidate.OnboardingType == TenantOnboardingType.Pilot &&
                candidate.Tenant != null &&
                candidate.Tenant.Subscription != null &&
                candidate.Tenant.Subscription.Plan == SubscriptionPlan.PilotEvaluation &&
                (candidate.Tenant.Subscription.Status == SubscriptionStatus.Active ||
                    candidate.Tenant.Subscription.Status == SubscriptionStatus.GracePeriod ||
                    candidate.Tenant.Subscription.Status == SubscriptionStatus.Expired));

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(candidate => candidate.CreatedAt)
            .ThenBy(candidate => candidate.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PlatformPilotSubscriptionPage(
            items.Select(item => new PlatformPilotSubscription(
                item.Tenant!.Name,
                item.CustomerReference,
                Map(item.Tenant.Subscription)!)).ToArray(),
            page,
            pageSize,
            totalCount,
            page * pageSize < totalCount,
            page > 1);
    }

    public async Task<TenantSubscriptionTransitionResult?> FindReplayAsync(
        Guid tenantId,
        string idempotencyKey,
        string requestFingerprint,
        CancellationToken cancellationToken = default)
    {
        var transition = await dbContext.TenantSubscriptionTransitions
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate =>
                candidate.TenantId == tenantId && candidate.IdempotencyKey == idempotencyKey,
                cancellationToken);
        return Replay(transition, requestFingerprint);
    }

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
        transaction.ExecuteAsync(async operationCancellationToken =>
        {
            var existingTransition = await dbContext.TenantSubscriptionTransitions
                .AsNoTracking()
                .SingleOrDefaultAsync(candidate =>
                    candidate.TenantId == tenantId && candidate.IdempotencyKey == idempotencyKey,
                    operationCancellationToken);
            if (existingTransition is not null)
            {
                return Replay(existingTransition, requestFingerprint);
            }

            var entity = await dbContext.TenantSubscriptions
                .SingleOrDefaultAsync(candidate => candidate.TenantId == tenantId, operationCancellationToken);
            if (entity is null)
            {
                return null;
            }

            if (entity.Version != expectedVersion)
            {
                throw new TenantSubscriptionConflictException("The subscription was changed by another operation. Refresh and retry.");
            }

            if (entity.Plan is not SubscriptionPlan.PilotEvaluation ||
                entity.Status is SubscriptionStatus.Cancelled or SubscriptionStatus.Converted)
            {
                throw new TenantSubscriptionConflictException("Only a non-terminal pilot subscription can use this transition.");
            }

            var effectiveStatus = Map(entity)!.EffectiveStatus(now);
            if (effectiveStatus is not (SubscriptionStatus.Active or SubscriptionStatus.GracePeriod or SubscriptionStatus.Expired) ||
                (transition is SubscriptionTransition.Expire && effectiveStatus is not SubscriptionStatus.Active))
            {
                throw new TenantSubscriptionConflictException(
                    transition is SubscriptionTransition.Expire
                        ? "Only an active pilot can enter its grace period."
                        : "The pilot subscription is not in a lifecycle state that permits this transition.");
            }

            var before = Snapshot(entity);
            Apply(entity, transition, values, now);
            entity.StatusReason = values.Reason;
            entity.Version++;
            entity.UpdatedAt = now;
            entity.UpdatedByUserId = actorUserId;
            var result = Map(entity)!;

            dbContext.TenantSubscriptionTransitions.Add(new TenantSubscriptionTransitionEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                SubscriptionId = entity.Id,
                IdempotencyKey = idempotencyKey,
                RequestFingerprint = requestFingerprint,
                Transition = transition.ToString(),
                ResultJson = JsonSerializer.Serialize(result, JsonOptions),
                CreatedAt = now,
                ActorUserId = actorUserId
            });

            dbContext.AuditLogEntries.Add(new AuditLogEntryEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ActorUserId = actorUserId,
                Action = transition switch
                {
                    SubscriptionTransition.Expire => AuditAction.Expired,
                    SubscriptionTransition.Cancel => AuditAction.Archived,
                    _ => AuditAction.Updated
                },
                EntityType = "TenantSubscription",
                EntityId = entity.Id.ToString(),
                OccurredAt = now,
                IpAddress = requestMetadata.IpAddress,
                UserAgent = requestMetadata.UserAgent,
                CorrelationId = requestMetadata.CorrelationId,
                Summary = transition switch
                {
                    SubscriptionTransition.Extend => "Pilot subscription was extended.",
                    SubscriptionTransition.Expire => "Pilot subscription entered its read-only grace period.",
                    SubscriptionTransition.Cancel => "Pilot subscription was cancelled.",
                    SubscriptionTransition.Convert => "Pilot subscription was converted to a commercial subscription.",
                    _ => "Pilot subscription was updated."
                },
                OldValue = JsonSerializer.Serialize(before),
                NewValue = JsonSerializer.Serialize(Snapshot(entity)),
                MetadataJson = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["transition"] = transition.ToString(),
                    ["reason"] = values.Reason,
                    ["version"] = entity.Version.ToString()
                })
            });

            try
            {
                await dbContext.SaveChangesAsync(operationCancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new TenantSubscriptionConflictException(
                    "The subscription was changed by another operation. Refresh and retry.");
            }
            catch (DbUpdateException) when (transition is SubscriptionTransition.Convert)
            {
                throw new TenantSubscriptionConflictException(
                    "The subscription transition conflicts with an existing idempotency or external subscription reference.");
            }
            catch (DbUpdateException)
            {
                throw new TenantSubscriptionConflictException(
                    "The subscription transition conflicts with another completed request. Retry with the same idempotency key.");
            }

            return new TenantSubscriptionTransitionResult(result, false);
        }, cancellationToken);

    private static TenantSubscriptionTransitionResult? Replay(
        TenantSubscriptionTransitionEntity? transition,
        string requestFingerprint)
    {
        if (transition is null)
        {
            return null;
        }

        if (!string.Equals(transition.RequestFingerprint, requestFingerprint, StringComparison.Ordinal))
        {
            throw new TenantSubscriptionConflictException(
                "The idempotency key has already been used for a different subscription transition.");
        }

        var replay = JsonSerializer.Deserialize<TenantSubscription>(transition.ResultJson, JsonOptions)
            ?? throw new InvalidOperationException("The stored subscription transition result is invalid.");
        return new TenantSubscriptionTransitionResult(replay, true);
    }

    private static void Apply(
        TenantSubscriptionEntity entity,
        SubscriptionTransition transition,
        SubscriptionTransitionValues values,
        DateTimeOffset now)
    {
        switch (transition)
        {
            case SubscriptionTransition.Extend:
                if (values.EndsAt <= entity.EndsAt)
                {
                    throw new TenantSubscriptionConflictException("A pilot extension must move the end date later.");
                }
                entity.EndsAt = values.EndsAt;
                entity.GraceEndsAt = values.GraceEndsAt;
                entity.Status = SubscriptionStatus.Active;
                break;
            case SubscriptionTransition.Expire:
                entity.EndsAt = now;
                entity.GraceEndsAt = values.GraceEndsAt;
                entity.Status = SubscriptionStatus.GracePeriod;
                break;
            case SubscriptionTransition.Cancel:
                entity.EndsAt = now;
                entity.GraceEndsAt = null;
                entity.Status = SubscriptionStatus.Cancelled;
                break;
            case SubscriptionTransition.Convert:
                entity.Plan = SubscriptionPlan.CommercialStandard;
                entity.PlanCode = values.PlanCode!;
                entity.Status = SubscriptionStatus.Converted;
                entity.EndsAt = null;
                entity.GraceEndsAt = null;
                entity.ExternalSubscriptionReference = values.ExternalSubscriptionReference;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(transition));
        }
    }

    private static object Snapshot(TenantSubscriptionEntity entity) => new
    {
        entity.Plan,
        entity.PlanCode,
        entity.Status,
        entity.StartsAt,
        entity.EndsAt,
        entity.GraceEndsAt,
        entity.ExternalCustomerReference,
        entity.ExternalSubscriptionReference,
        entity.StatusReason,
        entity.Version
    };

    private static TenantSubscription? Map(TenantSubscriptionEntity? entity) =>
        entity is null
            ? null
            : new TenantSubscription(
                entity.Id,
                entity.TenantId,
                entity.TenantKind,
                entity.Plan,
                entity.PlanCode,
                entity.Status,
                entity.StartsAt,
                entity.EndsAt,
                entity.GraceEndsAt,
                entity.ExternalCustomerReference,
                entity.ExternalSubscriptionReference,
                entity.StatusReason,
                entity.Version);
}
