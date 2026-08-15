using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;

namespace Gccs.Application.Tenancy;

public sealed class PlatformCustomerService(
    IPlatformCustomerRepository repository,
    TimeProvider timeProvider)
{
    private const int MaximumPageSize = 100;
    private const int MaximumSearchLength = 320;

    public async Task<PlatformCustomerPageDto> ListAsync(
        int page,
        int pageSize,
        string? search,
        TenantOnboardingType? customerType,
        TenantStatus? tenantStatus,
        TenantOnboardingStatus? onboardingStatus,
        SubscriptionStatus? subscriptionStatus,
        PlatformCustomerAttention? attention,
        PlatformCustomerSort sort,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            throw new ArgumentException("Page must be at least 1.", nameof(page));
        }

        if (pageSize < 1 || pageSize > MaximumPageSize)
        {
            throw new ArgumentException($"Page size must be between 1 and {MaximumPageSize}.", nameof(pageSize));
        }

        var normalizedSearch = search?.Trim();
        if (normalizedSearch?.Length > MaximumSearchLength)
        {
            throw new ArgumentException($"Search must be {MaximumSearchLength} characters or fewer.", nameof(search));
        }

        var now = timeProvider.GetUtcNow();
        var result = await repository.ListAsync(
            new PlatformCustomerQuery(
                page,
                pageSize,
                string.IsNullOrWhiteSpace(normalizedSearch) ? null : normalizedSearch,
                customerType,
                tenantStatus,
                onboardingStatus,
                subscriptionStatus,
                attention,
                sort,
                now),
            cancellationToken);

        return new PlatformCustomerPageDto(
            result.Items.Select(item => MapSummary(item, now)).ToArray(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.HasNextPage,
            result.HasPreviousPage);
    }

    public async Task<PlatformCustomerDetailDto?> FindAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var result = await repository.FindAsync(tenantId, cancellationToken);
        return result is null
            ? null
            : new PlatformCustomerDetailDto(
                MapSummary(result.Customer, now),
                result.Customer.OwnerDisplayName,
                result.Customer.InvitationId,
                result.Customer.InvitationNotificationSentAt,
                result.Customer.InvitationExpiresAt,
                result.Customer.InvitationAcceptedAt,
                result.Customer.PlanCode,
                result.Customer.SubscriptionReference,
                result.Customer.SetupReason,
                result.Customer.CancelledAt,
                result.Customer.CancellationReason,
                result.Lifecycle);
    }

    private static PlatformCustomerSummaryDto MapSummary(PlatformCustomerRecord item, DateTimeOffset now)
    {
        var subscription = MapSubscription(item.Subscription, now);
        return new PlatformCustomerSummaryDto(
            item.TenantId,
            item.DisplayName,
            item.CustomerReference,
            item.CustomerType,
            item.TenantStatus,
            item.DataPosture,
            item.OnboardingStatus,
            item.OwnerEmail,
            item.InvitationStatus,
            item.InvitationDeliveryStatus,
            subscription,
            DetermineAttention(item, subscription, now),
            item.CreatedAt,
            item.UpdatedAt);
    }

    private static TenantSubscriptionDto? MapSubscription(TenantSubscription? subscription, DateTimeOffset now) =>
        subscription is null
            ? null
            : new TenantSubscriptionDto(
                subscription.Id,
                subscription.TenantId,
                subscription.TenantKind,
                subscription.Plan,
                subscription.PlanCode,
                subscription.Status,
                subscription.EffectiveStatus(now),
                subscription.AccessLevel(now),
                subscription.StartsAt,
                subscription.EndsAt,
                subscription.GraceEndsAt,
                subscription.ExternalCustomerReference,
                subscription.ExternalSubscriptionReference,
                subscription.StatusReason,
                subscription.Version,
                false);

    private static IReadOnlyList<PlatformCustomerAttention> DetermineAttention(
        PlatformCustomerRecord customer,
        TenantSubscriptionDto? subscription,
        DateTimeOffset now)
    {
        var attention = new List<PlatformCustomerAttention>();
        if (customer.OnboardingStatus is TenantOnboardingStatus.PendingOwnerAcceptance)
        {
            attention.Add(PlatformCustomerAttention.PendingOwnerAcceptance);
        }
        if (customer.InvitationDeliveryStatus is InvitationDeliveryStatus.Failed)
        {
            attention.Add(PlatformCustomerAttention.InvitationDeliveryFailed);
        }
        if (subscription?.EffectiveStatus is SubscriptionStatus.GracePeriod)
        {
            attention.Add(PlatformCustomerAttention.GracePeriod);
        }
        if (subscription?.EffectiveStatus is SubscriptionStatus.Expired)
        {
            attention.Add(PlatformCustomerAttention.Expired);
        }
        if (subscription is
            {
                Plan: SubscriptionPlan.PilotEvaluation,
                EffectiveStatus: SubscriptionStatus.Active,
                EndsAt: not null
            } && subscription.EndsAt <= now.AddDays(14))
        {
            attention.Add(PlatformCustomerAttention.PilotExpiring);
        }
        if (subscription is null)
        {
            attention.Add(PlatformCustomerAttention.SubscriptionMissing);
        }
        return attention;
    }
}

public interface IPlatformCustomerRepository
{
    Task<PlatformCustomerPage> ListAsync(
        PlatformCustomerQuery query,
        CancellationToken cancellationToken = default);

    Task<PlatformCustomerDetail?> FindAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

public sealed record PlatformCustomerQuery(
    int Page,
    int PageSize,
    string? Search,
    TenantOnboardingType? CustomerType,
    TenantStatus? TenantStatus,
    TenantOnboardingStatus? OnboardingStatus,
    SubscriptionStatus? SubscriptionStatus,
    PlatformCustomerAttention? Attention,
    PlatformCustomerSort Sort,
    DateTimeOffset Now);

public sealed record PlatformCustomerRecord(
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
    TenantSubscription? Subscription,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record PlatformCustomerPage(
    IReadOnlyList<PlatformCustomerRecord> Items,
    int Page,
    int PageSize,
    int TotalCount,
    bool HasNextPage,
    bool HasPreviousPage);

public sealed record PlatformCustomerDetail(
    PlatformCustomerRecord Customer,
    IReadOnlyList<PlatformCustomerLifecycleItemDto> Lifecycle);

public sealed record PlatformCustomerPageDto(
    IReadOnlyList<PlatformCustomerSummaryDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    bool HasNextPage,
    bool HasPreviousPage);

public sealed record PlatformCustomerSummaryDto(
    Guid TenantId,
    string DisplayName,
    string? CustomerReference,
    TenantOnboardingType? CustomerType,
    TenantStatus TenantStatus,
    TenantDataPosture DataPosture,
    TenantOnboardingStatus? OnboardingStatus,
    string? OwnerEmail,
    TenantInvitationStatus? InvitationStatus,
    InvitationDeliveryStatus? InvitationDeliveryStatus,
    TenantSubscriptionDto? Subscription,
    IReadOnlyList<PlatformCustomerAttention> Attention,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record PlatformCustomerDetailDto(
    PlatformCustomerSummaryDto Customer,
    string? OwnerDisplayName,
    Guid? InvitationId,
    DateTimeOffset? InvitationNotificationSentAt,
    DateTimeOffset? InvitationExpiresAt,
    DateTimeOffset? InvitationAcceptedAt,
    string? PlanCode,
    string? SubscriptionReference,
    string? SetupReason,
    DateTimeOffset? CancelledAt,
    string? CancellationReason,
    IReadOnlyList<PlatformCustomerLifecycleItemDto> Lifecycle);

public sealed record PlatformCustomerLifecycleItemDto(
    string EventType,
    string Summary,
    DateTimeOffset OccurredAt,
    Guid? ActorUserId);

public enum PlatformCustomerAttention
{
    PendingOwnerAcceptance,
    InvitationDeliveryFailed,
    PilotExpiring,
    GracePeriod,
    Expired,
    SubscriptionMissing
}

public enum PlatformCustomerSort
{
    UpdatedDescending,
    NameAscending,
    CreatedDescending,
    PilotEndAscending
}
