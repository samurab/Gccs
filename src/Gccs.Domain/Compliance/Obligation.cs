namespace Gccs.Domain.Compliance;

public sealed record Obligation(
    string Id,
    string Source,
    string Title,
    string PlainEnglishSummary,
    string TriggerCondition,
    string RequiredAction,
    string OwnerFunction,
    RiskLevel RiskLevel,
    string FlowDownRequirement,
    ApplicabilityDimension Applicability,
    IReadOnlyList<EvidenceExample> EvidenceExamples,
    ComplianceSource SourceReference);
