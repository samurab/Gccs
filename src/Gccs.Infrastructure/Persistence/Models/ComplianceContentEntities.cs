using Gccs.Domain.Compliance;
using Gccs.Domain.Common;

namespace Gccs.Infrastructure.Persistence.Models;

public sealed class ClauseEntity
{
    public string Id { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string PlainEnglishSummary { get; set; } = string.Empty;
    public string ApplicabilityLogic { get; set; } = string.Empty;
    public string ClauseTextVersion { get; set; } = "current";
    public DateOnly? ClauseEffectiveAt { get; set; }
    public string? SourceHash { get; set; }
    public string? SupersededByClauseId { get; set; }
    public DateOnly? SupersededAt { get; set; }
    public string RequiredActionIdsJson { get; set; } = "[]";
    public bool UsuallyRequiresFlowDown { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public DateOnly SourceLastReviewedAt { get; set; }
    public DateOnly? SourceEffectiveAt { get; set; }
    public string SourceConfidence { get; set; } = "unknown";
    public bool SourceRequiresExpertReview { get; set; }
    public DateOnly LastReviewedAt { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateOnly? NextReviewDueAt { get; set; }
    public string Confidence { get; set; } = "unknown";
    public bool RequiresExpertReview { get; set; }
    public ReviewState ReviewState { get; set; } = ReviewState.Draft;
}

public sealed class ObligationEntity
{
    public string Id { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string PlainEnglishSummary { get; set; } = string.Empty;
    public string TriggerCondition { get; set; } = string.Empty;
    public string RequiredAction { get; set; } = string.Empty;
    public string OwnerFunction { get; set; } = string.Empty;
    public RiskLevel RiskLevel { get; set; }
    public bool RequiresFlowDown { get; set; }
    public string FlowDownRequirement { get; set; } = string.Empty;
    public string ApplicabilityJson { get; set; } = "{}";
    public string EvidenceExamplesJson { get; set; } = "[]";
    public string SourceName { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public DateOnly SourceLastReviewedAt { get; set; }
    public DateOnly? SourceEffectiveAt { get; set; }
    public string SourceConfidence { get; set; } = "unknown";
    public bool SourceRequiresExpertReview { get; set; }
    public DateOnly LastReviewedAt { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateOnly? NextReviewDueAt { get; set; }
    public string Confidence { get; set; } = "unknown";
    public bool RequiresExpertReview { get; set; }
    public ReviewState ReviewState { get; set; } = ReviewState.Draft;

    public ICollection<ContractClauseObligationEntity> ContractClauses { get; set; } = [];
    public ICollection<EvidenceObligationEntity> EvidenceItems { get; set; } = [];
}

public sealed class MvpModuleEntity
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
