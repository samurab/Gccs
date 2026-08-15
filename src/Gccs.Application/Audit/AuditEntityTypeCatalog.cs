namespace Gccs.Application.Audit;

public static class AuditEntityTypeCatalog
{
    public static IReadOnlyList<string> FilterableEntityTypes { get; } =
    [
        "CmmcAssessment",
        "CmmcPoamItem",
        "CompanyProfile",
        "Contract",
        "ContractClause",
        "ContractDeliverable",
        "EvidenceItem",
        "Report"
    ];
}
