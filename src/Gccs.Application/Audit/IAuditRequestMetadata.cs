namespace Gccs.Application.Audit;

public interface IAuditRequestMetadata
{
    string IpAddress { get; }
    string UserAgent { get; }
}
