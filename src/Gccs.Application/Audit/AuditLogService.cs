using Gccs.Domain.Audit;

namespace Gccs.Application.Audit;

public sealed class AuditLogService(IAuditLogRepository repository)
{
    public async Task<PagedResultDto<AuditLogEntryDto>> ListCurrentTenantAsync(
        AuditLogQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = Validate(request);
        return await repository.ListCurrentTenantAsync(query, cancellationToken);
    }

    private static AuditLogQuery Validate(AuditLogQueryRequest request)
    {
        if (request.Page < 1)
        {
            throw new ArgumentException("Page must be 1 or greater.", nameof(request));
        }

        if (request.PageSize is < 1 or > 100)
        {
            throw new ArgumentException("Page size must be between 1 and 100.", nameof(request));
        }

        if (request.From is not null && request.To is not null && request.From > request.To)
        {
            throw new ArgumentException("The from date must be before the to date.", nameof(request));
        }

        AuditAction? action = null;
        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            if (!Enum.TryParse<AuditAction>(request.Action.Trim(), true, out var parsedAction))
            {
                throw new ArgumentException("Audit action filter is not recognized.", nameof(request));
            }

            action = parsedAction;
        }

        return new AuditLogQuery(
            request.Page,
            request.PageSize,
            request.ActorUserId,
            action,
            string.IsNullOrWhiteSpace(request.EntityType) ? null : request.EntityType.Trim(),
            request.From,
            request.To);
    }
}
