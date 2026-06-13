using Gccs.Application.Audit;

namespace Gccs.Api.Security;

public sealed class HttpAuditRequestMetadata(IHttpContextAccessor httpContextAccessor) : IAuditRequestMetadata
{
    public string IpAddress => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

    public string UserAgent => httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString() ?? string.Empty;
}
