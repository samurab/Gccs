using Gccs.Domain.Compliance;
using Gccs.Domain.Common;

namespace Gccs.Infrastructure.Persistence.Models;

public sealed class ClauseEntity
{
    public string Id { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
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

public sealed class SuggestedObligationEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Source { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string GeneratedSummary { get; set; } = string.Empty;
    public string ProposedTitle { get; set; } = string.Empty;
    public string ProposedOwnerFunction { get; set; } = string.Empty;
    public string RequiredAction { get; set; } = string.Empty;
    public RiskLevel RiskLevel { get; set; }
    public string EvidenceSuggestionsJson { get; set; } = "[]";
    public string SourceCitationsJson { get; set; } = "[]";
    public string Confidence { get; set; } = "unknown";
    public string PromptVersion { get; set; } = string.Empty;
    public string ModelIdentifier { get; set; } = string.Empty;
    public string RetrievedSourceReferencesJson { get; set; } = "[]";
    public string ReviewStatus { get; set; } = "draft";
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewReason { get; set; }
}

public sealed class ExpertReviewItemEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public Guid SourceId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public Guid? AssignedExpertUserId { get; set; }
    public DateOnly? DueAt { get; set; }
    public string Status { get; set; } = "open";
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public string? ResolutionDecision { get; set; }
    public string? ResolutionNotes { get; set; }
}

public sealed class MvpModuleEntity
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
