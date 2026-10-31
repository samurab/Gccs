namespace Gccs.Domain.Common;

public enum ContentClassification
{
    Unclassified,
    Fci,
    Cui,
    SyntheticCui,
    Prohibited,
    Unknown
}

public enum ContentClassificationSource
{
    UserSelected,
    SystemSuggested,
    AdminReviewed,
    ImportedDemoSeed
}
