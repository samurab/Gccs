using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;

namespace Gccs.Application.Tenancy;

public sealed class PlatformTenantProvisioningService(
    IPlatformTenantProvisioningRepository repository,
    TimeProvider timeProvider,
    TenantSubscriptionSettings subscriptionSettings)
{
    private const int MaximumPageSize = 100;

    public async Task<PlatformTenantProvisioningResultDto> ProvisionAsync(
        PlatformTenantProvisioningRequest request,
        string idempotencyKey,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeAndValidate(request, idempotencyKey);
        var fingerprint = ComputeFingerprint(normalized.Request);
        var existing = await repository.FindByIdempotencyKeyAsync(normalized.IdempotencyKey, cancellationToken);

        if (existing is not null)
        {
            if (!string.Equals(existing.RequestFingerprint, fingerprint, StringComparison.Ordinal))
            {
                throw new TenantProvisioningConflictException(
                    "The idempotency key has already been used for a different tenant provisioning request.");
            }

            return existing.Result with { IsReplay = true };
        }

        return await repository.ProvisionAsync(
            normalized.Request,
            normalized.IdempotencyKey,
            fingerprint,
            actorUserId,
            cancellationToken);
    }

    public Task<PlatformTenantOnboardingPageDto> ListAsync(
        int page,
        int pageSize,
        TenantOnboardingStatus? status,
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

        return repository.ListAsync(page, pageSize, status, cancellationToken);
    }

    public Task<PlatformTenantProvisioningResultDto?> CancelAsync(
        Guid onboardingId,
        CancelPlatformTenantOnboardingRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var reason = Required(request.Reason, "Cancellation reason", 600);
        return repository.CancelAsync(onboardingId, reason, actorUserId, cancellationToken);
    }

    private NormalizedRequest NormalizeAndValidate(
        PlatformTenantProvisioningRequest request,
        string idempotencyKey)
    {
        var normalizedKey = Required(idempotencyKey, "Idempotency key", 128);
        var onboardingType = NormalizeOnboardingType(request.OnboardingType);
        var customerReference = Required(request.CustomerReference, "Customer reference", 120).ToUpperInvariant();
        var displayName = Required(request.DisplayName, "Tenant display name", 240);
        var ownerEmail = NormalizeEmail(request.OwnerEmail);
        var ownerDisplayName = Required(request.OwnerDisplayName, "Owner display name", 200);
        var setupReason = Required(request.SetupReason, "Setup reason", 600);
        var planCode = Optional(request.PlanCode, 80);
        var subscriptionReference = Optional(request.SubscriptionReference, 160);

        if (!request.ConfirmsNoCui)
        {
            throw new ArgumentException("The No-CUI product boundary must be confirmed.", nameof(request));
        }

        if (onboardingType is TenantOnboardingType.Pilot)
        {
            if (request.TrialEndsAt is null)
            {
                throw new ArgumentException("Pilot onboarding requires a trial end date.", nameof(request));
            }

            if (planCode is not null || subscriptionReference is not null || request.CommercialApprovalConfirmed)
            {
                throw new ArgumentException("Pilot onboarding must not include paid subscription fields.", nameof(request));
            }

            var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
            if (request.TrialEndsAt <= today)
            {
                throw new ArgumentException("Pilot onboarding requires a future trial end date.", nameof(request));
            }

            if (request.TrialEndsAt > today.AddDays(subscriptionSettings.MaximumPilotDays))
            {
                throw new ArgumentException(
                    $"Pilot onboarding cannot exceed {subscriptionSettings.MaximumPilotDays} days.",
                    nameof(request));
            }
        }
        else
        {
            if (planCode is null || subscriptionReference is null)
            {
                throw new ArgumentException("Paid onboarding requires a plan code and subscription reference.", nameof(request));
            }

            if (!request.CommercialApprovalConfirmed)
            {
                throw new ArgumentException("Paid onboarding requires commercial approval confirmation.", nameof(request));
            }

            if (request.TrialEndsAt is not null)
            {
                throw new ArgumentException("Paid onboarding must not include a pilot trial end date.", nameof(request));
            }
        }

        return new NormalizedRequest(
            request with
            {
                OnboardingType = onboardingType.ToString(),
                CustomerReference = customerReference,
                DisplayName = displayName,
                OwnerEmail = ownerEmail,
                OwnerDisplayName = ownerDisplayName,
                PlanCode = planCode,
                SubscriptionReference = subscriptionReference,
                SetupReason = setupReason
            },
            normalizedKey);
    }

    private static TenantOnboardingType NormalizeOnboardingType(string value) =>
        Enum.TryParse<TenantOnboardingType>(value?.Trim(), true, out var onboardingType)
            ? onboardingType
            : throw new ArgumentException("Onboarding type must be Pilot or Paid.", nameof(value));

    private static string NormalizeEmail(string value)
    {
        var email = Required(value, "Owner email", 320).ToLowerInvariant();
        var separator = email.IndexOf('@', StringComparison.Ordinal);
        if (separator <= 0 || separator == email.Length - 1 || email.Contains(' '))
        {
            throw new ArgumentException("A valid owner email is required.", nameof(value));
        }

        return email;
    }

    private static string Required(string value, string fieldName, int maximumLength)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0 || normalized.Length > maximumLength)
        {
            throw new ArgumentException($"{fieldName} is required and must be {maximumLength} characters or fewer.");
        }

        return normalized;
    }

    private static string? Optional(string? value, int maximumLength)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (normalized?.Length > maximumLength)
        {
            throw new ArgumentException($"Value must be {maximumLength} characters or fewer.");
        }

        return normalized;
    }

    private static string ComputeFingerprint(PlatformTenantProvisioningRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }

    private sealed record NormalizedRequest(PlatformTenantProvisioningRequest Request, string IdempotencyKey);
}

public interface IPlatformTenantProvisioningRepository
{
    Task<ExistingPlatformTenantProvisioningDto?> FindByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<PlatformTenantProvisioningResultDto> ProvisionAsync(
        PlatformTenantProvisioningRequest request,
        string idempotencyKey,
        string requestFingerprint,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<PlatformTenantOnboardingPageDto> ListAsync(
        int page,
        int pageSize,
        TenantOnboardingStatus? status,
        CancellationToken cancellationToken = default);

    Task<PlatformTenantProvisioningResultDto?> CancelAsync(
        Guid onboardingId,
        string reason,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed record PlatformTenantProvisioningRequest(
    string OnboardingType,
    string CustomerReference,
    string DisplayName,
    string OwnerEmail,
    string OwnerDisplayName,
    DateOnly? TrialEndsAt,
    string? PlanCode,
    string? SubscriptionReference,
    string SetupReason,
    bool ConfirmsNoCui,
    bool CommercialApprovalConfirmed);

public sealed record PlatformTenantProvisioningResultDto(
    Guid OnboardingId,
    Guid TenantId,
    string DisplayName,
    TenantOnboardingType OnboardingType,
    TenantOnboardingStatus OnboardingStatus,
    TenantStatus TenantStatus,
    TenantDataPosture DataHandlingMode,
    string CustomerReference,
    string OwnerEmail,
    string OwnerDisplayName,
    string OwnerRoleName,
    Guid InvitationId,
    TenantInvitationStatus InvitationStatus,
    InvitationDeliveryStatus InvitationDeliveryStatus,
    DateTimeOffset? InvitationNotificationSentAt,
    DateTimeOffset InvitationExpiresAt,
    DateOnly? TrialEndsAt,
    string? PlanCode,
    string? SubscriptionReference,
    string SetupReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CancelledAt,
    Guid? CancelledByUserId,
    string? CancellationReason,
    TenantSubscriptionDto? Subscription,
    bool IsReplay = false);

public sealed record CancelPlatformTenantOnboardingRequest(string Reason);

public sealed record PlatformTenantOnboardingPageDto(
    IReadOnlyList<PlatformTenantProvisioningResultDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    bool HasNextPage,
    bool HasPreviousPage);

public sealed record ExistingPlatformTenantProvisioningDto(
    string RequestFingerprint,
    PlatformTenantProvisioningResultDto Result);

public sealed class TenantProvisioningConflictException(string message) : InvalidOperationException(message);

public sealed class TenantOnboardingCancellationConflictException : InvalidOperationException
{
    public TenantOnboardingCancellationConflictException(string message)
        : base(message)
    {
    }

    public TenantOnboardingCancellationConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
