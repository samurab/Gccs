using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace Gccs.Application.Marketing;

public sealed record SubmitDemoRequest(
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string Company,
    string? ReferralSource,
    string? EmployeeCount,
    string? Message,
    DateTimeOffset? PreferredStartAt,
    string? PreferredTimeZone,
    bool PrivacyConsent,
    string? Website);

public sealed record DemoRequestReceipt(string Status, DateTimeOffset ReceivedAt);

public sealed record DemoRequestRecord(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string Company,
    string? ReferralSource,
    string? EmployeeCount,
    string? Message,
    DateTimeOffset PreferredStartAt,
    string PreferredTimeZone,
    string ConsentNoticeVersion,
    DateTimeOffset ReceivedAt,
    string DeduplicationKey);

public sealed record ClaimedDemoRequestDelivery(
    Guid DeliveryId,
    Guid RequestId,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string Company,
    string? ReferralSource,
    string? EmployeeCount,
    string? Message,
    DateTimeOffset? PreferredStartAt,
    string? PreferredTimeZone,
    DateTimeOffset ReceivedAt,
    int AttemptNumber,
    string DeliveryKind);

public sealed record DemoRequestOperationsItem(
    Guid Id, string FirstName, string LastName, string Email, string? Phone, string Company,
    string? ReferralSource, string? EmployeeCount, string? Message, DateTimeOffset? PreferredStartAt,
    string? PreferredTimeZone, DateTimeOffset ReceivedAt,
    string DeliveryStatus, int DeliveryAttemptCount, DateTimeOffset? NextDeliveryAttemptAt,
    DateTimeOffset? SentAt, string? DeliveryFailureCode, string AcknowledgementStatus);

public sealed record DemoRequestOperationsPage(
    IReadOnlyList<DemoRequestOperationsItem> Items, int Page, int PageSize, int TotalCount,
    bool HasNextPage, bool HasPreviousPage);

public interface IDemoRequestRepository
{
    Task CreateIfNewAsync(DemoRequestRecord request, CancellationToken cancellationToken = default);
    Task<ClaimedDemoRequestDelivery?> TryClaimNextDeliveryAsync(DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken = default);
    Task MarkDeliverySentAsync(Guid deliveryId, string providerMessageId, DateTimeOffset sentAt, CancellationToken cancellationToken = default);
    Task MarkDeliveryFailedAsync(Guid deliveryId, string failureCode, DateTimeOffset attemptedAt, DateTimeOffset? retryAt, CancellationToken cancellationToken = default);
    Task<int> DeleteExpiredAsync(DateTimeOffset receivedBefore, CancellationToken cancellationToken = default);
    Task<DemoRequestOperationsPage> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<bool?> QueueOperatorResponseAsync(Guid requestId, string templateKey, Guid actorUserId, DateTimeOffset now, CancellationToken cancellationToken = default);
}

public sealed record QueueDemoRequestResponse(string TemplateKey);
public sealed record DemoRequestResponseReceipt(string Status, string TemplateKey, DateTimeOffset QueuedAt);

public sealed class DemoRequestResponseService(IDemoRequestRepository repository, TimeProvider timeProvider)
{
    public static readonly IReadOnlySet<string> AllowedTemplates = new HashSet<string>(StringComparer.Ordinal)
    { "ReviewingRequestedTime", "RequestMoreDetails", "RequestedTimeUnavailable" };

    public async Task<DemoRequestResponseReceipt?> QueueAsync(Guid requestId, QueueDemoRequestResponse request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        if (!AllowedTemplates.Contains(request.TemplateKey)) throw new ArgumentException("Select a supported response template.", nameof(request));
        var now = timeProvider.GetUtcNow();
        var created = await repository.QueueOperatorResponseAsync(requestId, request.TemplateKey, actorUserId, now, cancellationToken);
        return created is null ? null : new DemoRequestResponseReceipt(created.Value ? "Queued" : "AlreadyQueued", request.TemplateKey, now);
    }
}

public sealed class DemoRequestValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("The demo request is invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}

public sealed class DemoRequestService(IDemoRequestRepository repository, TimeProvider timeProvider)
{
    public const string ConsentNoticeVersion = "demo-request-2026-08-02";
    private static readonly HashSet<string> AllowedEmployeeCounts = ["1-10", "11-50", "51-200", "201-500", "501+"];

    public async Task<DemoRequestReceipt> SubmitAsync(SubmitDemoRequest request, CancellationToken cancellationToken = default)
    {
        var receivedAt = timeProvider.GetUtcNow();
        if (!string.IsNullOrWhiteSpace(request.Website))
        {
            return new DemoRequestReceipt("Received", receivedAt);
        }

        var normalized = Normalize(request);
        Validate(normalized, receivedAt);
        var deduplicationKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{normalized.Email.ToLowerInvariant()}|{normalized.Company.ToLowerInvariant()}|{receivedAt:yyyy-MM-dd}")));

        await repository.CreateIfNewAsync(
            new DemoRequestRecord(
                Guid.NewGuid(),
                normalized.FirstName,
                normalized.LastName,
                normalized.Email,
                normalized.Phone,
                normalized.Company,
                normalized.ReferralSource,
                normalized.EmployeeCount,
                normalized.Message,
                normalized.PreferredStartAt!.Value,
                normalized.PreferredTimeZone!,
                ConsentNoticeVersion,
                receivedAt,
                deduplicationKey),
            cancellationToken);

        return new DemoRequestReceipt("Received", receivedAt);
    }

    private static SubmitDemoRequest Normalize(SubmitDemoRequest request) => request with
    {
        FirstName = Clean(request.FirstName),
        LastName = Clean(request.LastName),
        Email = request.Email.Trim(),
        Phone = CleanOptional(request.Phone),
        Company = Clean(request.Company),
        ReferralSource = CleanOptional(request.ReferralSource),
        EmployeeCount = CleanOptional(request.EmployeeCount),
        Message = CleanOptional(request.Message),
        PreferredTimeZone = CleanOptional(request.PreferredTimeZone)
    };

    private static void Validate(SubmitDemoRequest request, DateTimeOffset receivedAt)
    {
        var errors = new Dictionary<string, string[]>();
        Require(request.FirstName, "firstName", 100, errors);
        Require(request.LastName, "lastName", 100, errors);
        Require(request.Company, "company", 200, errors);
        if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Length > 320 || !IsValidEmail(request.Email))
        {
            errors["email"] = ["Enter a valid work email address."];
        }
        OptionalLength(request.Phone, "phone", 40, errors);
        OptionalLength(request.ReferralSource, "referralSource", 200, errors);
        OptionalLength(request.Message, "message", 2000, errors);
        if (request.PreferredStartAt is null || request.PreferredStartAt <= receivedAt.AddHours(2) || request.PreferredStartAt > receivedAt.AddDays(90))
            errors["preferredStartAt"] = ["Select a demo time between two hours and 90 days from now."];
        if (string.IsNullOrWhiteSpace(request.PreferredTimeZone) || !IsValidTimeZone(request.PreferredTimeZone))
            errors["preferredTimeZone"] = ["A valid IANA time zone is required."];
        if (request.EmployeeCount is not null && !AllowedEmployeeCounts.Contains(request.EmployeeCount))
        {
            errors["employeeCount"] = ["Select a valid company size."];
        }
        if (!request.PrivacyConsent)
        {
            errors["privacyConsent"] = ["Consent is required to submit a demo request."];
        }
        if (errors.Count > 0)
        {
            throw new DemoRequestValidationException(errors);
        }
    }

    private static bool IsValidEmail(string value)
    {
        try
        {
            var address = new MailAddress(value);
            return string.Equals(address.Address, value, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static void Require(string value, string field, int maximumLength, IDictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maximumLength)
        {
            errors[field] = [$"This field is required and must be {maximumLength} characters or fewer."];
        }
    }

    private static void OptionalLength(string? value, string field, int maximumLength, IDictionary<string, string[]> errors)
    {
        if (value?.Length > maximumLength)
        {
            errors[field] = [$"This field must be {maximumLength} characters or fewer."];
        }
    }

    private static string Clean(string value) => CollapseWhitespace(RemoveControlCharacters(value.Trim()));
    private static string? CleanOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : Clean(value);
    private static string RemoveControlCharacters(string value) => new(value.Where(character => !char.IsControl(character) || character is '\n' or '\r' or '\t').ToArray());
    private static string CollapseWhitespace(string value) => string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    private static bool IsValidTimeZone(string value) { try { _ = TimeZoneInfo.FindSystemTimeZoneById(value); return value.Length <= 100; } catch { return false; } }
}

public sealed record DemoRequestEmailSendResult(string ProviderMessageId);

public interface IDemoRequestEmailSender
{
    bool IsConfigured { get; }
    Task<DemoRequestEmailSendResult> SendAsync(ClaimedDemoRequestDelivery request, CancellationToken cancellationToken = default);
}

public sealed record DemoRequestDeliverySettings(TimeSpan LeaseDuration, int MaximumAttempts);

public sealed class DemoRequestDeliveryService(
    IDemoRequestRepository repository,
    IDemoRequestEmailSender sender,
    DemoRequestDeliverySettings settings,
    TimeProvider timeProvider)
{
    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken = default)
    {
        if (!sender.IsConfigured)
        {
            return false;
        }

        var request = await repository.TryClaimNextDeliveryAsync(timeProvider.GetUtcNow(), settings.LeaseDuration, cancellationToken);
        if (request is null)
        {
            return false;
        }

        try
        {
            var result = await sender.SendAsync(request, cancellationToken);
            await repository.MarkDeliverySentAsync(request.DeliveryId, result.ProviderMessageId, timeProvider.GetUtcNow(), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var attemptedAt = timeProvider.GetUtcNow();
            var retryAt = request.AttemptNumber >= settings.MaximumAttempts
                ? (DateTimeOffset?)null
                : attemptedAt.AddMinutes(Math.Min(Math.Pow(2, Math.Max(0, request.AttemptNumber - 1)), 60));
            await repository.MarkDeliveryFailedAsync(request.DeliveryId, exception.GetType().Name, attemptedAt, retryAt, cancellationToken);
        }

        return true;
    }
}
