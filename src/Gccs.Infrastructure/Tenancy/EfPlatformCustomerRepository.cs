using Gccs.Application.Tenancy;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Tenancy;

public sealed class EfPlatformCustomerRepository(GccsDbContext dbContext) : IPlatformCustomerRepository
{
    public async Task<PlatformCustomerPage> ListAsync(
        PlatformCustomerQuery request,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilters(dbContext.Tenants.AsNoTracking(), request);
        var totalCount = await query.CountAsync(cancellationToken);
        query = ApplySort(query, request.Sort);

        var rows = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(tenant => new CustomerRow(
                tenant.Id,
                tenant.Name,
                tenant.PlatformOnboarding == null ? null : tenant.PlatformOnboarding.CustomerReference,
                tenant.PlatformOnboarding == null ? null : tenant.PlatformOnboarding.OnboardingType,
                tenant.Status,
                tenant.DataPosture,
                tenant.PlatformOnboarding == null ? null : tenant.PlatformOnboarding.Status,
                tenant.PlatformOnboarding == null ? null : tenant.PlatformOnboarding.OwnerEmail,
                tenant.PlatformOnboarding == null ? null : tenant.PlatformOnboarding.OwnerDisplayName,
                tenant.PlatformOnboarding == null ? null : tenant.PlatformOnboarding.InvitationId,
                tenant.PlatformOnboarding == null || tenant.PlatformOnboarding.Invitation == null ? null : tenant.PlatformOnboarding.Invitation.Status,
                tenant.PlatformOnboarding == null || tenant.PlatformOnboarding.Invitation == null ? null : tenant.PlatformOnboarding.Invitation.DeliveryStatus,
                tenant.PlatformOnboarding == null || tenant.PlatformOnboarding.Invitation == null ? null : tenant.PlatformOnboarding.Invitation.NotificationSentAt,
                tenant.PlatformOnboarding == null || tenant.PlatformOnboarding.Invitation == null ? null : tenant.PlatformOnboarding.Invitation.ExpiresAt,
                tenant.PlatformOnboarding == null || tenant.PlatformOnboarding.Invitation == null ? null : tenant.PlatformOnboarding.Invitation.AcceptedAt,
                tenant.PlatformOnboarding == null ? null : tenant.PlatformOnboarding.PlanCode,
                tenant.PlatformOnboarding == null ? null : tenant.PlatformOnboarding.SubscriptionReference,
                tenant.PlatformOnboarding == null ? null : tenant.PlatformOnboarding.SetupReason,
                tenant.PlatformOnboarding == null ? null : tenant.PlatformOnboarding.CancelledAt,
                tenant.PlatformOnboarding == null ? null : tenant.PlatformOnboarding.CancellationReason,
                tenant.Subscription == null ? null : tenant.Subscription.Id,
                tenant.Subscription == null ? null : tenant.Subscription.TenantKind,
                tenant.Subscription == null ? null : tenant.Subscription.Plan,
                tenant.Subscription == null ? null : tenant.Subscription.PlanCode,
                tenant.Subscription == null ? null : tenant.Subscription.Status,
                tenant.Subscription == null ? null : tenant.Subscription.StartsAt,
                tenant.Subscription == null ? null : tenant.Subscription.EndsAt,
                tenant.Subscription == null ? null : tenant.Subscription.GraceEndsAt,
                tenant.Subscription == null ? null : tenant.Subscription.ExternalCustomerReference,
                tenant.Subscription == null ? null : tenant.Subscription.ExternalSubscriptionReference,
                tenant.Subscription == null ? null : tenant.Subscription.StatusReason,
                tenant.Subscription == null ? null : tenant.Subscription.Version,
                tenant.CreatedAt,
                tenant.UpdatedAt,
                tenant.PlatformOnboarding == null ? null : tenant.PlatformOnboarding.UpdatedAt,
                tenant.PlatformOnboarding == null ? null : tenant.PlatformOnboarding.CreatedAt,
                tenant.Subscription == null ? null : tenant.Subscription.UpdatedAt,
                tenant.Subscription == null ? null : tenant.Subscription.CreatedAt))
            .ToArrayAsync(cancellationToken);

        return new PlatformCustomerPage(
            rows.Select(Map).ToArray(),
            request.Page,
            request.PageSize,
            totalCount,
            request.Page * request.PageSize < totalCount,
            request.Page > 1);
    }

    public async Task<PlatformCustomerDetail?> FindAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var row = await dbContext.Tenants
            .AsNoTracking()
            .Where(tenant => tenant.Id == tenantId)
            .Select(tenant => new CustomerRow(
                tenant.Id,
                tenant.Name,
                tenant.PlatformOnboarding == null ? null : tenant.PlatformOnboarding.CustomerReference,
                tenant.PlatformOnboarding == null ? null : tenant.PlatformOnboarding.OnboardingType,
                tenant.Status,
                tenant.DataPosture,
                tenant.PlatformOnboarding == null ? null : tenant.PlatformOnboarding.Status,
                tenant.PlatformOnboarding == null ? null : tenant.PlatformOnboarding.OwnerEmail,
                tenant.PlatformOnboarding == null ? null : tenant.PlatformOnboarding.OwnerDisplayName,
                tenant.PlatformOnboarding == null ? null : tenant.PlatformOnboarding.InvitationId,
                tenant.PlatformOnboarding == null || tenant.PlatformOnboarding.Invitation == null ? null : tenant.PlatformOnboarding.Invitation.Status,
                tenant.PlatformOnboarding == null || tenant.PlatformOnboarding.Invitation == null ? null : tenant.PlatformOnboarding.Invitation.DeliveryStatus,
                tenant.PlatformOnboarding == null || tenant.PlatformOnboarding.Invitation == null ? null : tenant.PlatformOnboarding.Invitation.NotificationSentAt,
                tenant.PlatformOnboarding == null || tenant.PlatformOnboarding.Invitation == null ? null : tenant.PlatformOnboarding.Invitation.ExpiresAt,
                tenant.PlatformOnboarding == null || tenant.PlatformOnboarding.Invitation == null ? null : tenant.PlatformOnboarding.Invitation.AcceptedAt,
                tenant.PlatformOnboarding == null ? null : tenant.PlatformOnboarding.PlanCode,
                tenant.PlatformOnboarding == null ? null : tenant.PlatformOnboarding.SubscriptionReference,
                tenant.PlatformOnboarding == null ? null : tenant.PlatformOnboarding.SetupReason,
                tenant.PlatformOnboarding == null ? null : tenant.PlatformOnboarding.CancelledAt,
                tenant.PlatformOnboarding == null ? null : tenant.PlatformOnboarding.CancellationReason,
                tenant.Subscription == null ? null : tenant.Subscription.Id,
                tenant.Subscription == null ? null : tenant.Subscription.TenantKind,
                tenant.Subscription == null ? null : tenant.Subscription.Plan,
                tenant.Subscription == null ? null : tenant.Subscription.PlanCode,
                tenant.Subscription == null ? null : tenant.Subscription.Status,
                tenant.Subscription == null ? null : tenant.Subscription.StartsAt,
                tenant.Subscription == null ? null : tenant.Subscription.EndsAt,
                tenant.Subscription == null ? null : tenant.Subscription.GraceEndsAt,
                tenant.Subscription == null ? null : tenant.Subscription.ExternalCustomerReference,
                tenant.Subscription == null ? null : tenant.Subscription.ExternalSubscriptionReference,
                tenant.Subscription == null ? null : tenant.Subscription.StatusReason,
                tenant.Subscription == null ? null : tenant.Subscription.Version,
                tenant.CreatedAt,
                tenant.UpdatedAt,
                tenant.PlatformOnboarding == null ? null : tenant.PlatformOnboarding.UpdatedAt,
                tenant.PlatformOnboarding == null ? null : tenant.PlatformOnboarding.CreatedAt,
                tenant.Subscription == null ? null : tenant.Subscription.UpdatedAt,
                tenant.Subscription == null ? null : tenant.Subscription.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            return null;
        }

        var lifecycle = new List<PlatformCustomerLifecycleItemDto>();
        if (row.OnboardingCreatedAt is not null)
        {
            lifecycle.Add(new("OnboardingCreated", "Platform tenant onboarding was created.", row.OnboardingCreatedAt.Value, null));
        }
        if (row.InvitationNotificationSentAt is not null)
        {
            lifecycle.Add(new("InvitationSent", "The initial Owner invitation was sent.", row.InvitationNotificationSentAt.Value, null));
        }
        if (row.InvitationAcceptedAt is not null)
        {
            lifecycle.Add(new("OwnerActivated", "The initial Owner accepted the invitation.", row.InvitationAcceptedAt.Value, null));
        }
        if (row.CancelledAt is not null)
        {
            lifecycle.Add(new("OnboardingCancelled", "Pending onboarding was cancelled.", row.CancelledAt.Value, null));
        }

        var transitions = await dbContext.TenantSubscriptionTransitions
            .AsNoTracking()
            .Where(item => item.TenantId == tenantId)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new PlatformCustomerLifecycleItemDto(
                $"Subscription{item.Transition}",
                item.Transition == "Extend" ? "Pilot subscription was extended." :
                item.Transition == "Expire" ? "Pilot subscription entered its read-only grace period." :
                item.Transition == "Cancel" ? "Pilot subscription was cancelled." :
                item.Transition == "Convert" ? "Pilot subscription was converted to commercial." :
                "Subscription lifecycle changed.",
                item.CreatedAt,
                item.ActorUserId))
            .ToArrayAsync(cancellationToken);
        lifecycle.AddRange(transitions);

        return new PlatformCustomerDetail(
            Map(row),
            lifecycle.OrderByDescending(item => item.OccurredAt).ToArray());
    }

    private static IQueryable<TenantEntity> ApplyFilters(
        IQueryable<TenantEntity> query,
        PlatformCustomerQuery request)
    {
        if (request.Search is not null)
        {
            var search = request.Search.ToUpperInvariant();
            query = query.Where(tenant =>
                tenant.Name.ToUpper().StartsWith(search) ||
                (tenant.PlatformOnboarding != null &&
                    (tenant.PlatformOnboarding.CustomerReference.ToUpper().StartsWith(search) ||
                     tenant.PlatformOnboarding.OwnerEmail.ToUpper().StartsWith(search))) ||
                (tenant.Subscription != null && tenant.Subscription.ExternalSubscriptionReference != null &&
                    tenant.Subscription.ExternalSubscriptionReference.ToUpper().StartsWith(search)));
        }
        if (request.CustomerType is not null)
        {
            query = query.Where(tenant => tenant.PlatformOnboarding != null && tenant.PlatformOnboarding.OnboardingType == request.CustomerType);
        }
        if (request.TenantStatus is not null)
        {
            query = query.Where(tenant => tenant.Status == request.TenantStatus);
        }
        if (request.OnboardingStatus is not null)
        {
            query = query.Where(tenant => tenant.PlatformOnboarding != null && tenant.PlatformOnboarding.Status == request.OnboardingStatus);
        }
        if (request.SubscriptionStatus is not null)
        {
            query = ApplyEffectiveSubscriptionStatus(query, request.SubscriptionStatus.Value, request.Now);
        }
        if (request.Attention is not null)
        {
            query = ApplyAttention(query, request.Attention.Value, request.Now);
        }
        return query;
    }

    private static IQueryable<TenantEntity> ApplyEffectiveSubscriptionStatus(
        IQueryable<TenantEntity> query,
        SubscriptionStatus status,
        DateTimeOffset now) => status switch
    {
        SubscriptionStatus.Pending => query.Where(tenant => tenant.Subscription != null &&
            (tenant.Subscription.Status == SubscriptionStatus.Pending || tenant.Subscription.StartsAt > now)),
        SubscriptionStatus.Active => query.Where(tenant => tenant.Subscription != null &&
            tenant.Subscription.Status == SubscriptionStatus.Active && tenant.Subscription.StartsAt <= now &&
            (tenant.Subscription.EndsAt == null || tenant.Subscription.EndsAt > now)),
        SubscriptionStatus.GracePeriod => query.Where(tenant => tenant.Subscription != null &&
            tenant.Subscription.GraceEndsAt != null && tenant.Subscription.GraceEndsAt > now &&
            (tenant.Subscription.Status == SubscriptionStatus.GracePeriod ||
             (tenant.Subscription.Status == SubscriptionStatus.Active && tenant.Subscription.EndsAt <= now))),
        SubscriptionStatus.Expired => query.Where(tenant => tenant.Subscription != null &&
            (tenant.Subscription.Status == SubscriptionStatus.Expired ||
             (tenant.Subscription.Status == SubscriptionStatus.GracePeriod && tenant.Subscription.GraceEndsAt <= now) ||
             (tenant.Subscription.Status == SubscriptionStatus.Active && tenant.Subscription.EndsAt != null &&
              tenant.Subscription.EndsAt <= now &&
              (tenant.Subscription.GraceEndsAt == null || tenant.Subscription.GraceEndsAt <= now)))),
        _ => query.Where(tenant => tenant.Subscription != null && tenant.Subscription.Status == status)
    };

    private static IQueryable<TenantEntity> ApplyAttention(
        IQueryable<TenantEntity> query,
        PlatformCustomerAttention attention,
        DateTimeOffset now) => attention switch
    {
        PlatformCustomerAttention.PendingOwnerAcceptance => query.Where(tenant => tenant.PlatformOnboarding != null &&
            tenant.PlatformOnboarding.Status == TenantOnboardingStatus.PendingOwnerAcceptance),
        PlatformCustomerAttention.InvitationDeliveryFailed => query.Where(tenant => tenant.PlatformOnboarding != null &&
            tenant.PlatformOnboarding.Invitation != null && tenant.PlatformOnboarding.Invitation.DeliveryStatus == InvitationDeliveryStatus.Failed),
        PlatformCustomerAttention.PilotExpiring => query.Where(tenant => tenant.Subscription != null &&
            tenant.Subscription.Plan == SubscriptionPlan.PilotEvaluation &&
            tenant.Subscription.Status == SubscriptionStatus.Active &&
            tenant.Subscription.EndsAt > now && tenant.Subscription.EndsAt <= now.AddDays(14)),
        PlatformCustomerAttention.GracePeriod => ApplyEffectiveSubscriptionStatus(query, SubscriptionStatus.GracePeriod, now),
        PlatformCustomerAttention.Expired => ApplyEffectiveSubscriptionStatus(query, SubscriptionStatus.Expired, now),
        PlatformCustomerAttention.SubscriptionMissing => query.Where(tenant => tenant.Subscription == null),
        _ => query
    };

    private static IQueryable<TenantEntity> ApplySort(IQueryable<TenantEntity> query, PlatformCustomerSort sort) => sort switch
    {
        PlatformCustomerSort.NameAscending => query.OrderBy(tenant => tenant.Name).ThenBy(tenant => tenant.Id),
        PlatformCustomerSort.CreatedDescending => query.OrderByDescending(tenant => tenant.CreatedAt).ThenBy(tenant => tenant.Id),
        PlatformCustomerSort.PilotEndAscending => query
            .OrderBy(tenant => tenant.Subscription == null || tenant.Subscription.EndsAt == null)
            .ThenBy(tenant => tenant.Subscription!.EndsAt)
            .ThenBy(tenant => tenant.Name),
        _ => query
            .OrderByDescending(tenant =>
                tenant.Subscription != null &&
                (tenant.Subscription.UpdatedAt ?? tenant.Subscription.CreatedAt) > (tenant.UpdatedAt ?? tenant.CreatedAt)
                    ? tenant.PlatformOnboarding != null &&
                      (tenant.PlatformOnboarding.UpdatedAt ?? tenant.PlatformOnboarding.CreatedAt) >
                      (tenant.Subscription.UpdatedAt ?? tenant.Subscription.CreatedAt)
                        ? tenant.PlatformOnboarding.UpdatedAt ?? tenant.PlatformOnboarding.CreatedAt
                        : tenant.Subscription.UpdatedAt ?? tenant.Subscription.CreatedAt
                    : tenant.PlatformOnboarding != null &&
                      (tenant.PlatformOnboarding.UpdatedAt ?? tenant.PlatformOnboarding.CreatedAt) >
                      (tenant.UpdatedAt ?? tenant.CreatedAt)
                        ? tenant.PlatformOnboarding.UpdatedAt ?? tenant.PlatformOnboarding.CreatedAt
                        : tenant.UpdatedAt ?? tenant.CreatedAt)
            .ThenBy(tenant => tenant.Id)
    };

    private static PlatformCustomerRecord Map(CustomerRow row)
    {
        var subscription = row.SubscriptionId is null
            ? null
            : new TenantSubscription(
                row.SubscriptionId.Value,
                row.TenantId,
                row.TenantKind!.Value,
                row.SubscriptionPlan!.Value,
                row.SubscriptionPlanCode!,
                row.SubscriptionStatus!.Value,
                row.SubscriptionStartsAt!.Value,
                row.SubscriptionEndsAt,
                row.SubscriptionGraceEndsAt,
                row.ExternalCustomerReference,
                row.ExternalSubscriptionReference,
                row.SubscriptionStatusReason!,
                row.SubscriptionVersion!.Value);

        var updatedAt = new[]
        {
            row.TenantUpdatedAt,
            row.OnboardingUpdatedAt,
            row.OnboardingCreatedAt,
            row.SubscriptionUpdatedAt,
            row.SubscriptionCreatedAt,
            row.TenantCreatedAt
        }.Where(value => value is not null).Max()!.Value;

        return new PlatformCustomerRecord(
            row.TenantId,
            row.DisplayName,
            row.CustomerReference,
            row.CustomerType,
            row.TenantStatus,
            row.DataPosture,
            row.OnboardingStatus,
            row.OwnerEmail,
            row.OwnerDisplayName,
            row.InvitationId,
            row.InvitationStatus,
            row.InvitationDeliveryStatus,
            row.InvitationNotificationSentAt,
            row.InvitationExpiresAt,
            row.InvitationAcceptedAt,
            row.PlanCode,
            row.SubscriptionReference,
            row.SetupReason,
            row.CancelledAt,
            row.CancellationReason,
            subscription,
            row.TenantCreatedAt,
            updatedAt);
    }

    private sealed record CustomerRow(
        Guid TenantId,
        string DisplayName,
        string? CustomerReference,
        TenantOnboardingType? CustomerType,
        TenantStatus TenantStatus,
        TenantDataPosture DataPosture,
        TenantOnboardingStatus? OnboardingStatus,
        string? OwnerEmail,
        string? OwnerDisplayName,
        Guid? InvitationId,
        TenantInvitationStatus? InvitationStatus,
        InvitationDeliveryStatus? InvitationDeliveryStatus,
        DateTimeOffset? InvitationNotificationSentAt,
        DateTimeOffset? InvitationExpiresAt,
        DateTimeOffset? InvitationAcceptedAt,
        string? PlanCode,
        string? SubscriptionReference,
        string? SetupReason,
        DateTimeOffset? CancelledAt,
        string? CancellationReason,
        Guid? SubscriptionId,
        TenantKind? TenantKind,
        SubscriptionPlan? SubscriptionPlan,
        string? SubscriptionPlanCode,
        SubscriptionStatus? SubscriptionStatus,
        DateTimeOffset? SubscriptionStartsAt,
        DateTimeOffset? SubscriptionEndsAt,
        DateTimeOffset? SubscriptionGraceEndsAt,
        string? ExternalCustomerReference,
        string? ExternalSubscriptionReference,
        string? SubscriptionStatusReason,
        long? SubscriptionVersion,
        DateTimeOffset TenantCreatedAt,
        DateTimeOffset? TenantUpdatedAt,
        DateTimeOffset? OnboardingUpdatedAt,
        DateTimeOffset? OnboardingCreatedAt,
        DateTimeOffset? SubscriptionUpdatedAt,
        DateTimeOffset? SubscriptionCreatedAt);
}
