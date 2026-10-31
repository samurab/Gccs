using Gccs.Domain.Tenancy;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Gccs.Application.Tenancy;

public sealed record TenantSubscriptionSettings(int MaximumPilotDays = 90, int GracePeriodDays = 7);

public sealed class TenantSubscriptionService(
    ITenantSubscriptionRepository repository,
    TimeProvider timeProvider,
    TenantSubscriptionSettings settings)
{
    private const int MaximumPageSize = 100;

    public Task<TenantSubscriptionDto?> FindAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        FindAndMapAsync(tenantId, cancellationToken);

    public async Task<PlatformPilotSubscriptionPageDto> ListPilotsAsync(
        int page,
        int pageSize,
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

        var result = await repository.ListPilotsAsync(page, pageSize, cancellationToken);
        var now = timeProvider.GetUtcNow();
        return new PlatformPilotSubscriptionPageDto(
            result.Items.Select(item => new PlatformPilotSubscriptionDto(
                item.Subscription.TenantId,
                item.DisplayName,
                item.CustomerReference,
                Map(item.Subscription, now)!)).ToArray(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.HasNextPage,
            result.HasPreviousPage);
    }

    public async Task<TenantSubscriptionDto?> ExtendPilotAsync(
        Guid tenantId,
        ExtendPilotSubscriptionRequest request,
        string idempotencyKey,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var reason = Required(request.Reason, "Extension reason", 600);
        var normalizedKey = NormalizeIdempotencyKey(idempotencyKey);
        var fingerprint = Fingerprint(SubscriptionTransition.Extend, request with { Reason = reason });
        var now = timeProvider.GetUtcNow();
        var replay = await repository.FindReplayAsync(tenantId, normalizedKey, fingerprint, cancellationToken);
        if (replay is not null)
        {
            return MapResult(replay, now);
        }

        var newEndsAt = EndExclusive(request.NewEndsOn);
        if (newEndsAt <= now)
        {
            throw new ArgumentException("The new pilot end date must be in the future.", nameof(request));
        }

        if (newEndsAt > now.AddDays(settings.MaximumPilotDays + 1))
        {
            throw new ArgumentException($"The pilot end date must be within {settings.MaximumPilotDays} days.", nameof(request));
        }

        return MapResult(await repository.TransitionAsync(
            tenantId,
            request.ExpectedVersion,
            SubscriptionTransition.Extend,
            new SubscriptionTransitionValues(
                newEndsAt,
                newEndsAt.AddDays(settings.GracePeriodDays),
                null,
                null,
                reason),
            normalizedKey,
            fingerprint,
            actorUserId,
            now,
            cancellationToken), now);
    }

    public Task<TenantSubscriptionDto?> ExpirePilotAsync(
        Guid tenantId,
        ChangePilotSubscriptionStatusRequest request,
        string idempotencyKey,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(tenantId, request, SubscriptionTransition.Expire, idempotencyKey, actorUserId, cancellationToken);

    public Task<TenantSubscriptionDto?> CancelPilotAsync(
        Guid tenantId,
        ChangePilotSubscriptionStatusRequest request,
        string idempotencyKey,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(tenantId, request, SubscriptionTransition.Cancel, idempotencyKey, actorUserId, cancellationToken);

    public async Task<TenantSubscriptionDto?> ConvertPilotAsync(
        Guid tenantId,
        ConvertPilotSubscriptionRequest request,
        string idempotencyKey,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var values = new SubscriptionTransitionValues(
            null,
            null,
            Required(request.PlanCode, "Plan code", 80),
            Required(request.ExternalSubscriptionReference, "External subscription reference", 160),
            Required(request.Reason, "Conversion reason", 600));
        var normalizedKey = NormalizeIdempotencyKey(idempotencyKey);
        var fingerprint = Fingerprint(SubscriptionTransition.Convert, request with
        {
            PlanCode = values.PlanCode!,
            ExternalSubscriptionReference = values.ExternalSubscriptionReference!,
            Reason = values.Reason
        });
        var replay = await repository.FindReplayAsync(tenantId, normalizedKey, fingerprint, cancellationToken);
        if (replay is not null)
        {
            return MapResult(replay, now);
        }

        return MapResult(await repository.TransitionAsync(
            tenantId,
            request.ExpectedVersion,
            SubscriptionTransition.Convert,
            values,
            normalizedKey,
            fingerprint,
            actorUserId,
            now,
            cancellationToken), now);
    }

    private async Task<TenantSubscriptionDto?> TransitionAsync(
        Guid tenantId,
        ChangePilotSubscriptionStatusRequest request,
        SubscriptionTransition transition,
        string idempotencyKey,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var reason = Required(request.Reason, "Status reason", 600);
        var normalizedKey = NormalizeIdempotencyKey(idempotencyKey);
        var fingerprint = Fingerprint(transition, request with { Reason = reason });
        var replay = await repository.FindReplayAsync(tenantId, normalizedKey, fingerprint, cancellationToken);
        if (replay is not null)
        {
            return MapResult(replay, now);
        }

        var values = new SubscriptionTransitionValues(
            transition is SubscriptionTransition.Expire ? now : null,
            transition is SubscriptionTransition.Expire ? now.AddDays(settings.GracePeriodDays) : null,
            null,
            null,
            reason);
        return MapResult(await repository.TransitionAsync(
            tenantId,
            request.ExpectedVersion,
            transition,
            values,
            normalizedKey,
            fingerprint,
            actorUserId,
            now,
            cancellationToken), now);
    }

    private async Task<TenantSubscriptionDto?> FindAndMapAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var subscription = await repository.FindByTenantIdAsync(tenantId, cancellationToken);
        return Map(subscription, timeProvider.GetUtcNow());
    }

    public static DateTimeOffset EndExclusive(DateOnly endDate)
    {
        if (endDate == DateOnly.MaxValue)
        {
            throw new ArgumentException("The pilot end date is outside the supported range.", nameof(endDate));
        }

        return new DateTimeOffset(endDate.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
    }

    public static TenantSubscriptionDto? Map(TenantSubscription? subscription, DateTimeOffset now) =>
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

    private static TenantSubscriptionDto? MapResult(
        TenantSubscriptionTransitionResult? result,
        DateTimeOffset now)
    {
        if (result is null)
        {
            return null;
        }

        return Map(result.Subscription, now)! with { IsReplay = result.IsReplay };
    }

    private static string NormalizeIdempotencyKey(string value) =>
        Required(value, "Idempotency key", 128);

    private static string Fingerprint<T>(SubscriptionTransition transition, T request)
    {
        var json = JsonSerializer.Serialize(new { transition, request });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }

    private static string Required(string? value, string fieldName, int maximumLength)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0 || normalized.Length > maximumLength)
        {
            throw new ArgumentException($"{fieldName} is required and must be {maximumLength} characters or fewer.");
        }

        return normalized;
    }
}

public interface ITenantSubscriptionRepository
{
    Task<TenantSubscription?> FindByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<PlatformPilotSubscriptionPage> ListPilotsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<TenantSubscriptionTransitionResult?> FindReplayAsync(
        Guid tenantId,
        string idempotencyKey,
        string requestFingerprint,
        CancellationToken cancellationToken = default);

    Task<TenantSubscriptionTransitionResult?> TransitionAsync(
        Guid tenantId,
        long expectedVersion,
        SubscriptionTransition transition,
        SubscriptionTransitionValues values,
        string idempotencyKey,
        string requestFingerprint,
        Guid actorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}

public enum SubscriptionTransition { Extend, Expire, Cancel, Convert }

public sealed record SubscriptionTransitionValues(
    DateTimeOffset? EndsAt,
    DateTimeOffset? GraceEndsAt,
    string? PlanCode,
    string? ExternalSubscriptionReference,
    string Reason);

public sealed record TenantSubscriptionTransitionResult(TenantSubscription Subscription, bool IsReplay);

public sealed record PlatformPilotSubscription(
    string DisplayName,
    string CustomerReference,
    TenantSubscription Subscription);

public sealed record PlatformPilotSubscriptionPage(
    IReadOnlyList<PlatformPilotSubscription> Items,
    int Page,
    int PageSize,
    int TotalCount,
    bool HasNextPage,
    bool HasPreviousPage);

public sealed record PlatformPilotSubscriptionDto(
    Guid TenantId,
    string DisplayName,
    string CustomerReference,
    TenantSubscriptionDto Subscription);

public sealed record PlatformPilotSubscriptionPageDto(
    IReadOnlyList<PlatformPilotSubscriptionDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    bool HasNextPage,
    bool HasPreviousPage);

public sealed record TenantSubscriptionDto(
    Guid Id,
    Guid TenantId,
    TenantKind TenantKind,
    SubscriptionPlan Plan,
    string PlanCode,
    SubscriptionStatus Status,
    SubscriptionStatus EffectiveStatus,
    SubscriptionAccessLevel AccessLevel,
    DateTimeOffset StartsAt,
    DateTimeOffset? EndsAt,
    DateTimeOffset? GraceEndsAt,
    string? ExternalCustomerReference,
    string? ExternalSubscriptionReference,
    string StatusReason,
    long Version,
    bool IsReplay = false);

public sealed record ExtendPilotSubscriptionRequest(DateOnly NewEndsOn, string Reason, long ExpectedVersion);
public sealed record ChangePilotSubscriptionStatusRequest(string Reason, long ExpectedVersion);
public sealed record ConvertPilotSubscriptionRequest(
    string PlanCode,
    string ExternalSubscriptionReference,
    string Reason,
    long ExpectedVersion);

public sealed class TenantSubscriptionConflictException(string message) : InvalidOperationException(message);
