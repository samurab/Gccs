using System.Security.Cryptography;
using System.Text;

namespace Gccs.Application.Marketing;

public static class DemoFollowUpCatalog
{
    public const string Pending = "Pending";
    public const string Responded = "Responded";
    public const string Expired = "Expired";
    public const string TemplateVersion = "demo-follow-up-2026-08-12";
    public const string NoCuiNoticeVersion = "demo-follow-up-no-cui-2026-08-12";

    public static readonly IReadOnlySet<string> WorkflowCodes = new HashSet<string>(StringComparer.Ordinal)
    {
        "ContractClauseIntake",
        "ObligationsDeadlines",
        "CmmcReadiness",
        "EvidenceManagement",
        "SubcontractorFlowDowns",
        "ReportingPreparation",
        "Other"
    };
}

public sealed record DemoFollowUpSecuritySettings(
    string PublicWebBaseUrl,
    byte[] SigningKey,
    TimeSpan TokenLifetime);

public sealed class DemoFollowUpTokenCodec(DemoFollowUpSecuritySettings settings)
{
    private const string Version = "v1";

    public string Create(Guid requestId, DateTimeOffset expiresAt)
    {
        var payload = $"{Version}.{requestId:N}.{expiresAt.ToUnixTimeSeconds()}";
        using var hmac = new HMACSHA256(settings.SigningKey);
        var signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return $"{payload}.{Base64UrlEncode(signature)}";
    }

    public bool TryValidate(string? token, out Guid requestId, out DateTimeOffset expiresAt)
    {
        requestId = Guid.Empty;
        expiresAt = default;
        if (string.IsNullOrWhiteSpace(token) || token.Length > 256) return false;

        var parts = token.Split('.', StringSplitOptions.None);
        if (parts.Length != 4 || parts[0] != Version ||
            !Guid.TryParseExact(parts[1], "N", out requestId) ||
            !long.TryParse(parts[2], out var expiresUnixSeconds)) return false;

        try
        {
            expiresAt = DateTimeOffset.FromUnixTimeSeconds(expiresUnixSeconds);
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }

        var payload = string.Join('.', parts[..3]);
        using var hmac = new HMACSHA256(settings.SigningKey);
        var expected = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        byte[] supplied;
        try
        {
            supplied = Base64UrlDecode(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        return supplied.Length == expected.Length && CryptographicOperations.FixedTimeEquals(supplied, expected);
    }

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - padded.Length % 4) % 4);
        return Convert.FromBase64String(padded);
    }
}

public sealed record DemoFollowUpQueueCommand(
    Guid FollowUpRequestId,
    Guid DemoRequestId,
    string TokenHash,
    string TemplateVersion,
    string NoCuiNoticeVersion,
    DateTimeOffset ExpiresAt,
    Guid RequestedByUserId,
    DateTimeOffset RequestedAt);

public enum DemoFollowUpQueueDisposition
{
    Queued,
    DemoRequestNotFound,
    AlreadyPending
}

public sealed record DemoFollowUpQueueWriteResult(
    DemoFollowUpQueueDisposition Disposition,
    Guid? FollowUpRequestId = null,
    DateTimeOffset? ExpiresAt = null);

public sealed record DemoFollowUpAccessRecord(
    Guid FollowUpRequestId,
    Guid DemoRequestId,
    string Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset RequestedAt,
    DateTimeOffset? RespondedAt);

public sealed record DemoFollowUpResponseCommand(
    Guid ResponseId,
    Guid FollowUpRequestId,
    Guid DemoRequestId,
    IReadOnlyList<string> Workflows,
    string? OtherWorkflow,
    string Goals,
    string Challenges,
    string? CurrentProcess,
    string? AdditionalContext,
    string NoCuiNoticeVersion,
    DateTimeOffset SubmittedAt);

public enum DemoFollowUpSubmissionDisposition
{
    Accepted,
    Invalid,
    Expired,
    AlreadyResponded
}

public interface IDemoFollowUpRepository
{
    Task<DemoFollowUpQueueWriteResult> QueueRequestAsync(
        DemoFollowUpQueueCommand command,
        CancellationToken cancellationToken = default);

    Task<DemoFollowUpAccessRecord?> GetAccessAsync(
        Guid followUpRequestId,
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<DemoFollowUpPreviewRecord?> GetPreviewAsync(
        Guid demoRequestId,
        Guid followUpRequestId,
        CancellationToken cancellationToken = default);

    Task<DemoFollowUpSubmissionDisposition> SubmitResponseAsync(
        string tokenHash,
        DemoFollowUpResponseCommand command,
        CancellationToken cancellationToken = default);
}

public sealed record DemoFollowUpQueueReceipt(
    string Status,
    Guid FollowUpRequestId,
    DateTimeOffset ExpiresAt,
    DateTimeOffset QueuedAt);

public sealed record DemoFollowUpPreviewRecord(
    Guid FollowUpRequestId,
    Guid DemoRequestId,
    string TokenHash,
    string Status,
    DateTimeOffset ExpiresAt);

public sealed record DemoFollowUpDevelopmentPreview(
    string Url,
    DateTimeOffset ExpiresAt);

public sealed record DemoFollowUpTokenRequest(string Token);

public sealed record DemoFollowUpContext(
    string Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset ExpiresAt,
    string NoCuiNoticeVersion);

public sealed record SubmitDemoFollowUpResponse(
    string Token,
    IReadOnlyList<string> Workflows,
    string? OtherWorkflow,
    string Goals,
    string Challenges,
    string? CurrentProcess,
    string? AdditionalContext,
    bool NoCuiConfirmed,
    string? Website);

public sealed record DemoFollowUpSubmissionReceipt(string Status, DateTimeOffset SubmittedAt);

public sealed record DemoFollowUpOperationsItem(
    Guid Id,
    string Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset ExpiresAt,
    Guid RequestedByUserId,
    string DeliveryStatus,
    DateTimeOffset? RespondedAt,
    IReadOnlyList<string> Workflows,
    string? OtherWorkflow,
    string? Goals,
    string? Challenges,
    string? CurrentProcess,
    string? AdditionalContext,
    string NoCuiNoticeVersion);

public sealed class DemoFollowUpValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("The demo follow-up response is invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}

public sealed class DemoFollowUpService(
    IDemoFollowUpRepository repository,
    DemoFollowUpTokenCodec tokenCodec,
    DemoFollowUpSecuritySettings securitySettings,
    TimeProvider timeProvider)
{
    public async Task<DemoFollowUpQueueReceipt?> QueueAsync(
        Guid demoRequestId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var requestId = Guid.NewGuid();
        var expiresAt = now.Add(securitySettings.TokenLifetime);
        var accessCode = tokenCodec.Create(requestId, expiresAt);
        var result = await repository.QueueRequestAsync(
            new DemoFollowUpQueueCommand(
                requestId,
                demoRequestId,
                DemoFollowUpTokenCodec.Hash(accessCode),
                DemoFollowUpCatalog.TemplateVersion,
                DemoFollowUpCatalog.NoCuiNoticeVersion,
                expiresAt,
                actorUserId,
                now),
            cancellationToken);

        return result.Disposition switch
        {
            DemoFollowUpQueueDisposition.DemoRequestNotFound => null,
            DemoFollowUpQueueDisposition.Queued => new DemoFollowUpQueueReceipt("Queued", requestId, expiresAt, now),
            DemoFollowUpQueueDisposition.AlreadyPending => new DemoFollowUpQueueReceipt(
                "AlreadyPending",
                result.FollowUpRequestId!.Value,
                result.ExpiresAt!.Value,
                now),
            _ => throw new InvalidOperationException("The follow-up repository returned an unsupported queue disposition.")
        };
    }

    public async Task<DemoFollowUpContext?> GetContextAsync(
        string? token,
        CancellationToken cancellationToken = default)
    {
        if (!tokenCodec.TryValidate(token, out var requestId, out var tokenExpiresAt)) return null;
        var access = await repository.GetAccessAsync(
            requestId,
            DemoFollowUpTokenCodec.Hash(token!),
            cancellationToken);
        if (access is null || access.ExpiresAt.ToUnixTimeSeconds() != tokenExpiresAt.ToUnixTimeSeconds()) return null;

        var status = access.Status == DemoFollowUpCatalog.Pending && access.ExpiresAt <= timeProvider.GetUtcNow()
            ? DemoFollowUpCatalog.Expired
            : access.Status;
        return new DemoFollowUpContext(
            status,
            access.RequestedAt,
            access.ExpiresAt,
            DemoFollowUpCatalog.NoCuiNoticeVersion);
    }

    public async Task<DemoFollowUpDevelopmentPreview?> CreateDevelopmentPreviewAsync(
        Guid demoRequestId,
        Guid followUpRequestId,
        CancellationToken cancellationToken = default)
    {
        var preview = await repository.GetPreviewAsync(demoRequestId, followUpRequestId, cancellationToken);
        if (preview is null ||
            preview.Status != DemoFollowUpCatalog.Pending ||
            preview.ExpiresAt <= timeProvider.GetUtcNow())
        {
            return null;
        }

        var accessCode = tokenCodec.Create(preview.FollowUpRequestId, preview.ExpiresAt);
        if (!CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(DemoFollowUpTokenCodec.Hash(accessCode)),
            Encoding.ASCII.GetBytes(preview.TokenHash)))
        {
            return null;
        }

        return new DemoFollowUpDevelopmentPreview(
            $"{securitySettings.PublicWebBaseUrl.TrimEnd('/')}/demo-request-details#token={Uri.EscapeDataString(accessCode)}",
            preview.ExpiresAt);
    }

    public async Task<DemoFollowUpSubmissionReceipt?> SubmitAsync(
        SubmitDemoFollowUpResponse request,
        CancellationToken cancellationToken = default)
    {
        if (!tokenCodec.TryValidate(request.Token, out var requestId, out var tokenExpiresAt)) return null;
        var tokenHash = DemoFollowUpTokenCodec.Hash(request.Token);
        var access = await repository.GetAccessAsync(requestId, tokenHash, cancellationToken);
        if (access is null || access.ExpiresAt.ToUnixTimeSeconds() != tokenExpiresAt.ToUnixTimeSeconds()) return null;

        var now = timeProvider.GetUtcNow();
        if (access.Status == DemoFollowUpCatalog.Responded)
            throw new DemoFollowUpStateException(DemoFollowUpSubmissionDisposition.AlreadyResponded);
        if (access.Status == DemoFollowUpCatalog.Expired || access.ExpiresAt <= now)
            throw new DemoFollowUpStateException(DemoFollowUpSubmissionDisposition.Expired);

        if (!string.IsNullOrWhiteSpace(request.Website))
            return new DemoFollowUpSubmissionReceipt("Received", now);

        var normalized = ValidateAndNormalize(request);
        var disposition = await repository.SubmitResponseAsync(
            tokenHash,
            new DemoFollowUpResponseCommand(
                Guid.NewGuid(),
                requestId,
                access.DemoRequestId,
                normalized.Workflows,
                normalized.OtherWorkflow,
                normalized.Goals,
                normalized.Challenges,
                normalized.CurrentProcess,
                normalized.AdditionalContext,
                DemoFollowUpCatalog.NoCuiNoticeVersion,
                now),
            cancellationToken);

        return disposition switch
        {
            DemoFollowUpSubmissionDisposition.Accepted => new DemoFollowUpSubmissionReceipt("Received", now),
            DemoFollowUpSubmissionDisposition.Invalid => null,
            DemoFollowUpSubmissionDisposition.Expired => throw new DemoFollowUpStateException(disposition),
            DemoFollowUpSubmissionDisposition.AlreadyResponded => throw new DemoFollowUpStateException(disposition),
            _ => throw new InvalidOperationException("The follow-up repository returned an unsupported submission disposition.")
        };
    }

    private static NormalizedResponse ValidateAndNormalize(SubmitDemoFollowUpResponse request)
    {
        var errors = new Dictionary<string, string[]>();
        var workflows = request.Workflows?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray() ?? [];
        if (workflows.Length == 0 || workflows.Length > DemoFollowUpCatalog.WorkflowCodes.Count ||
            workflows.Any(value => !DemoFollowUpCatalog.WorkflowCodes.Contains(value)))
            errors["workflows"] = ["Select at least one supported workflow."];

        var otherWorkflow = NormalizeOptional(request.OtherWorkflow, 200, "otherWorkflow", errors);
        if (workflows.Contains("Other", StringComparer.Ordinal) && otherWorkflow is null)
            errors["otherWorkflow"] = ["Describe the other workflow you want to discuss."];
        else if (!workflows.Contains("Other", StringComparer.Ordinal) && otherWorkflow is not null)
            errors["otherWorkflow"] = ["Select Other before providing another workflow."];

        var goals = NormalizeRequired(request.Goals, 2000, "goals", errors);
        var challenges = NormalizeRequired(request.Challenges, 2000, "challenges", errors);
        var currentProcess = NormalizeOptional(request.CurrentProcess, 1000, "currentProcess", errors);
        var additionalContext = NormalizeOptional(request.AdditionalContext, 2000, "additionalContext", errors);
        if (!request.NoCuiConfirmed)
            errors["noCuiConfirmed"] = ["Confirm that your response contains no CUI or other prohibited sensitive information."];

        if (errors.Count > 0) throw new DemoFollowUpValidationException(errors);
        return new NormalizedResponse(workflows, otherWorkflow, goals, challenges, currentProcess, additionalContext);
    }

    private static string NormalizeRequired(
        string? value,
        int maximumLength,
        string field,
        IDictionary<string, string[]> errors)
    {
        var normalized = NormalizeOptional(value, maximumLength, field, errors);
        if (normalized is null) errors[field] = ["This field is required."];
        return normalized ?? string.Empty;
    }

    private static string? NormalizeOptional(
        string? value,
        int maximumLength,
        string field,
        IDictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
            errors[field] = [$"Enter no more than {maximumLength} characters."];
        if (normalized.Any(character => character == '\0' || (char.IsControl(character) && character is not '\r' and not '\n' and not '\t')))
            errors[field] = ["Remove unsupported control characters."];
        return normalized;
    }

    private sealed record NormalizedResponse(
        IReadOnlyList<string> Workflows,
        string? OtherWorkflow,
        string Goals,
        string Challenges,
        string? CurrentProcess,
        string? AdditionalContext);
}

public sealed class DemoFollowUpStateException(DemoFollowUpSubmissionDisposition disposition)
    : Exception(disposition == DemoFollowUpSubmissionDisposition.Expired
        ? "This follow-up link has expired."
        : "This follow-up request has already been answered.")
{
    public DemoFollowUpSubmissionDisposition Disposition { get; } = disposition;
}
