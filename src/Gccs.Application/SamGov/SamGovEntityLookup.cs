namespace Gccs.Application.SamGov;

public interface ISamGovEntityLookupClient
{
    Task<SamGovEntityLookupResult> LookupByUeiAsync(
        string uei,
        CancellationToken cancellationToken = default);
}

public sealed record SamGovEntityLookupResult(
    bool IsSuccess,
    string? PayloadJson,
    SamGovLookupError? Error)
{
    public static SamGovEntityLookupResult Success(string payloadJson) => new(true, payloadJson, null);

    public static SamGovEntityLookupResult Failure(string code, string message) => new(false, null, new SamGovLookupError(code, message));
}

public sealed record SamGovLookupError(string Code, string Message);
