using Gccs.Domain.Common;

namespace Gccs.Domain.Compliance;

public sealed record Clause(
    string Id,
    string Source,
    string Number,
    string Title,
    string PlainEnglishSummary,
    string ApplicabilityLogic,
    string ClauseTextVersion,
    DateOnly? ClauseEffectiveAt,
    string? SourceHash,
    string? SupersededByClauseId,
    IReadOnlyList<string> RequiredActionIds,
    bool UsuallyRequiresFlowDown,
    ComplianceSource SourceReference,
    ReviewMetadata Review);
