using System.Globalization;

namespace Gccs.Application.Marketing;

public static class DemoAppointmentCatalog
{
    public const string Confirmed = "Confirmed";
    public const int DurationMinutes = 30;
    public static readonly IReadOnlySet<string> MeetingMethods = new HashSet<string>(StringComparer.Ordinal)
    {
        "ConnectionDetailsToFollow",
        "MicrosoftTeams",
        "Zoom",
        "GoogleMeet",
        "Phone"
    };

    public static bool RequiresJoinUrl(string meetingMethod) =>
        meetingMethod is "MicrosoftTeams" or "Zoom" or "GoogleMeet";
}

public sealed record ConfirmDemoAppointment(
    string ConfirmedLocalStart,
    string TimeZone,
    string MeetingMethod,
    string? MeetingJoinUrl);

public sealed record DemoAppointmentConfirmationCommand(
    Guid AppointmentId,
    Guid EventId,
    Guid DemoRequestId,
    DateTimeOffset ConfirmedStartAt,
    DateTimeOffset ConfirmedEndAt,
    string TimeZone,
    int DurationMinutes,
    Guid HostUserId,
    string MeetingMethod,
    string? MeetingJoinUrl,
    DateTimeOffset ConfirmedAt);

public enum DemoAppointmentConfirmationDisposition
{
    Confirmed,
    DemoRequestNotFound,
    AlreadyConfirmed,
    HostConflict
}

public sealed record DemoAppointmentConfirmationWriteResult(
    DemoAppointmentConfirmationDisposition Disposition,
    Guid? AppointmentId = null);

public sealed record DemoAppointmentConfirmationReceipt(
    Guid AppointmentId,
    Guid DemoRequestId,
    string SchedulingStatus,
    DateTimeOffset ConfirmedStartAt,
    DateTimeOffset ConfirmedEndAt,
    string TimeZone,
    int DurationMinutes,
    string MeetingMethod,
    string EmailStatus,
    DateTimeOffset ConfirmedAt);

public interface IDemoAppointmentRepository
{
    Task<DemoAppointmentConfirmationWriteResult> ConfirmAsync(
        DemoAppointmentConfirmationCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class DemoAppointmentValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("The appointment confirmation is invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}

public sealed class DemoAppointmentConflictException(string message) : Exception(message);

public sealed class DemoAppointmentService(
    IDemoAppointmentRepository repository,
    TimeProvider timeProvider)
{
    private static readonly string[] LocalStartFormats = ["yyyy-MM-dd'T'HH:mm", "yyyy-MM-dd'T'HH:mm:ss"];

    public async Task<DemoAppointmentConfirmationReceipt?> ConfirmAsync(
        Guid demoRequestId,
        ConfirmDemoAppointment request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var normalizedTimeZone = request.TimeZone?.Trim() ?? string.Empty;
        var normalizedMethod = request.MeetingMethod?.Trim() ?? string.Empty;
        var normalizedUrl = string.IsNullOrWhiteSpace(request.MeetingJoinUrl) ? null : request.MeetingJoinUrl.Trim();
        var errors = new Dictionary<string, string[]>();

        var zone = ResolveTimeZone(normalizedTimeZone, errors);
        var localStart = ParseLocalStart(request.ConfirmedLocalStart, errors);
        DateTimeOffset? confirmedStart = null;
        if (zone is not null && localStart is not null)
        {
            if (zone.IsInvalidTime(localStart.Value))
                errors["confirmedLocalStart"] = ["The selected time does not exist in this time zone because of a daylight-saving transition."];
            else if (zone.IsAmbiguousTime(localStart.Value))
                errors["confirmedLocalStart"] = ["The selected time is ambiguous in this time zone. Select a different time."];
            else
                confirmedStart = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localStart.Value, zone), TimeSpan.Zero);
        }

        if (confirmedStart is not null && confirmedStart <= now)
            errors["confirmedLocalStart"] = ["Select a confirmation time in the future."];
        else if (confirmedStart is not null && confirmedStart > now.AddDays(365))
            errors["confirmedLocalStart"] = ["Select a confirmation time no more than 365 days from now."];

        if (!DemoAppointmentCatalog.MeetingMethods.Contains(normalizedMethod))
            errors["meetingMethod"] = ["Select a supported meeting method."];

        if (DemoAppointmentCatalog.RequiresJoinUrl(normalizedMethod))
        {
            if (!IsSafeHttpsUrl(normalizedUrl))
                errors["meetingJoinUrl"] = ["Enter a valid HTTPS meeting link without embedded credentials."];
        }
        else if (normalizedUrl is not null)
        {
            errors["meetingJoinUrl"] = ["A meeting link is allowed only for an online meeting method."];
        }

        if (errors.Count > 0) throw new DemoAppointmentValidationException(errors);

        var start = confirmedStart!.Value;
        var result = await repository.ConfirmAsync(
            new DemoAppointmentConfirmationCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                demoRequestId,
                start,
                start.AddMinutes(DemoAppointmentCatalog.DurationMinutes),
                normalizedTimeZone,
                DemoAppointmentCatalog.DurationMinutes,
                actorUserId,
                normalizedMethod,
                normalizedUrl,
                now),
            cancellationToken);

        return result.Disposition switch
        {
            DemoAppointmentConfirmationDisposition.DemoRequestNotFound => null,
            DemoAppointmentConfirmationDisposition.AlreadyConfirmed => throw new DemoAppointmentConflictException("This demo request already has a confirmed appointment."),
            DemoAppointmentConfirmationDisposition.HostConflict => throw new DemoAppointmentConflictException("You already host another confirmed demo during this time."),
            DemoAppointmentConfirmationDisposition.Confirmed => new DemoAppointmentConfirmationReceipt(
                result.AppointmentId!.Value,
                demoRequestId,
                DemoAppointmentCatalog.Confirmed,
                start,
                start.AddMinutes(DemoAppointmentCatalog.DurationMinutes),
                normalizedTimeZone,
                DemoAppointmentCatalog.DurationMinutes,
                normalizedMethod,
                "Queued",
                now),
            _ => throw new InvalidOperationException("The appointment repository returned an unsupported confirmation disposition.")
        };
    }

    private static TimeZoneInfo? ResolveTimeZone(string value, IDictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 100)
        {
            errors["timeZone"] = ["A valid IANA time zone is required."];
            return null;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(value);
        }
        catch (TimeZoneNotFoundException)
        {
            errors["timeZone"] = ["A valid IANA time zone is required."];
            return null;
        }
        catch (InvalidTimeZoneException)
        {
            errors["timeZone"] = ["A valid IANA time zone is required."];
            return null;
        }
    }

    private static DateTime? ParseLocalStart(string? value, IDictionary<string, string[]> errors)
    {
        if (!DateTime.TryParseExact(value?.Trim(), LocalStartFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            errors["confirmedLocalStart"] = ["Select a complete confirmation date and time."];
            return null;
        }

        return DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified);
    }

    private static bool IsSafeHttpsUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 2048 ||
            !Uri.TryCreate(value, UriKind.Absolute, out var uri)) return false;

        return uri.Scheme == Uri.UriSchemeHttps &&
            !string.IsNullOrWhiteSpace(uri.Host) &&
            string.IsNullOrEmpty(uri.UserInfo);
    }
}
