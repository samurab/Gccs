using System.Security.Cryptography;
using System.Text.Json;
using Gccs.Application.Tasks;
using Microsoft.AspNetCore.WebUtilities;

namespace Gccs.Api;

public sealed class TaskSearchCursorOptions
{
    public const string SectionName = "TaskSearch";

    public string CursorSigningKey { get; set; } = string.Empty;
    public string PreviousCursorSigningKey { get; set; } = string.Empty;

    public static void Validate(TaskSearchCursorOptions options)
    {
        _ = DecodeKey(options.CursorSigningKey, "TaskSearch:CursorSigningKey");
        if (!string.IsNullOrWhiteSpace(options.PreviousCursorSigningKey))
        {
            _ = DecodeKey(options.PreviousCursorSigningKey, "TaskSearch:PreviousCursorSigningKey");
        }
    }

    internal static byte[] DecodeKey(string value, string settingName)
    {
        byte[] decoded;
        try
        {
            decoded = Convert.FromBase64String(value);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException($"{settingName} must be a base64-encoded key.", exception);
        }

        if (decoded.Length < 32)
        {
            throw new InvalidOperationException($"{settingName} must decode to at least 32 bytes.");
        }

        return decoded;
    }
}

public sealed class TaskSearchCursorProtector(
    TaskSearchCursorOptions options,
    TimeProvider timeProvider)
    : IComplianceTaskCursorCodec
{
    private const int CurrentVersion = 1;
    private static readonly TimeSpan CursorLifetime = TimeSpan.FromMinutes(15);
    private readonly byte[] _currentKey = TaskSearchCursorOptions.DecodeKey(
        options.CursorSigningKey,
        "TaskSearch:CursorSigningKey");
    private readonly byte[]? _previousKey = string.IsNullOrWhiteSpace(options.PreviousCursorSigningKey)
        ? null
        : TaskSearchCursorOptions.DecodeKey(options.PreviousCursorSigningKey, "TaskSearch:PreviousCursorSigningKey");

    public string Protect(Guid tenantId, string filterFingerprint, ComplianceTaskCursor cursor)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(new CursorPayload(
            CurrentVersion,
            tenantId,
            filterFingerprint,
            timeProvider.GetUtcNow().Add(CursorLifetime),
            cursor.DueAt,
            cursor.CreatedAt,
            cursor.Id));
        return $"{WebEncoders.Base64UrlEncode(payload)}.{WebEncoders.Base64UrlEncode(Sign(payload, _currentKey))}";
    }

    public bool TryUnprotect(
        string protectedCursor,
        Guid tenantId,
        string filterFingerprint,
        out ComplianceTaskCursor? cursor)
    {
        cursor = null;
        try
        {
            var parts = protectedCursor.Split('.', 2, StringSplitOptions.None);
            if (parts.Length != 2)
            {
                return false;
            }

            var payloadBytes = WebEncoders.Base64UrlDecode(parts[0]);
            var suppliedSignature = WebEncoders.Base64UrlDecode(parts[1]);
            if (!HasValidSignature(payloadBytes, suppliedSignature))
            {
                return false;
            }

            var payload = JsonSerializer.Deserialize<CursorPayload>(payloadBytes);
            if (payload is null ||
                payload.Version != CurrentVersion ||
                payload.TenantId != tenantId ||
                payload.ExpiresAt <= timeProvider.GetUtcNow() ||
                !string.Equals(payload.FilterFingerprint, filterFingerprint, StringComparison.Ordinal))
            {
                return false;
            }

            cursor = new ComplianceTaskCursor(payload.DueAt, payload.CreatedAt, payload.Id);
            return true;
        }
        catch (Exception exception) when (exception is FormatException or JsonException)
        {
            return false;
        }
    }

    private bool HasValidSignature(byte[] payload, byte[] suppliedSignature) =>
        CryptographicOperations.FixedTimeEquals(Sign(payload, _currentKey), suppliedSignature) ||
        (_previousKey is not null &&
         CryptographicOperations.FixedTimeEquals(Sign(payload, _previousKey), suppliedSignature));

    private static byte[] Sign(byte[] payload, byte[] key) => HMACSHA256.HashData(key, payload);

    private sealed record CursorPayload(
        int Version,
        Guid TenantId,
        string FilterFingerprint,
        DateTimeOffset ExpiresAt,
        DateOnly? DueAt,
        DateTimeOffset CreatedAt,
        Guid Id);
}
