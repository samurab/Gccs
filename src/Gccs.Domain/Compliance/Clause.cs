using Gccs.Domain.Common;

namespace Gccs.Domain.Compliance;

public sealed record Clause(
    string Id,
    string Source,
    string Number,
    string Title,
    string PlainEnglishSummary,
    string ApplicabilityLogic,
    IReadOnlyList<string> RequiredActionIds,
    bool UsuallyRequiresFlowDown,
    ComplianceSource SourceReference,
    ReviewMetadata Review);
