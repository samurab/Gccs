namespace Gccs.Domain.Compliance;

public sealed record ApplicabilityDimension(
    string EntityRole,
    string ContractType,
    string DataType,
    string Agency,
    string PlaceOfPerformance,
    string SubcontractorRole);
