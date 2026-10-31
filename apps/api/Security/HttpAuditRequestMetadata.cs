using Gccs.Application.Audit;

namespace Gccs.Api.Security;

public sealed class HttpAuditRequestMetadata(IHttpContextAccessor httpContextAccessor) : IAuditRequestMetadata
{
    public string IpAddress =>
        httpContextAccessor.HttpContext is { } httpContext
            ? httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim() ??
              httpContext.Connection.RemoteIpAddress?.ToString() ??
              string.Empty
            : string.Empty;

    public string UserAgent => httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString() ?? string.Empty;

    public string CorrelationId =>
        httpContextAccessor.HttpContext is { } httpContext
            ? ApiCorrelation.Get(httpContext)
            : string.Empty;
}
