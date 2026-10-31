namespace Gccs.Domain.Common;

public sealed record PhysicalLocation(
    string Name,
    string Street1,
    string? Street2,
    string City,
    string StateOrProvince,
    string PostalCode,
    string Country,
    bool IsPlaceOfPerformance = false);
