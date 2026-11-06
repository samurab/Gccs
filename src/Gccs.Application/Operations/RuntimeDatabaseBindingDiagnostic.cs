namespace Gccs.Application.Operations;

public interface IRuntimeDatabaseBindingDiagnostic
{
    RuntimeDatabaseBindingResult Observe(RuntimeDatabaseBindingExpectation expectation);
}

public sealed record RuntimeDatabaseBindingExpectation(string ServerSha256, string DatabaseSha256)
{
    public bool IsValid => IsDigest(ServerSha256) && IsDigest(DatabaseSha256);

    private static bool IsDigest(string value) =>
        value is { Length: 64 } && value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');
}

public enum RuntimeDatabaseBindingStatus
{
    Matched,
    Mismatch,
    Unavailable,
    InvalidConfiguration,
    Expired,
    Failed
}

public sealed record RuntimeDatabaseBindingResult(
    RuntimeDatabaseBindingStatus Status,
    bool? ProviderMatches = null,
    bool? ServerMatches = null,
    bool? PortMatches = null,
    bool? DatabaseMatches = null);
