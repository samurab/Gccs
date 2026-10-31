namespace Gccs.Domain.Common;

public sealed record EntityAudit(
    DateTimeOffset CreatedAt,
    Guid? CreatedByUserId,
    DateTimeOffset? UpdatedAt = null,
    Guid? UpdatedByUserId = null);
