namespace Gccs.Domain.Common;

public sealed record PointOfContact(
    string Name,
    string Email,
    string? Phone,
    string? Title);
