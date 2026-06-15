using Gccs.Domain.Common;
using Gccs.Domain.Compliance;

namespace Gccs.Application.Compliance;

public sealed record ObligationDashboardQuery(
    Guid? ContractId,
    RiskLevel? RiskLevel,
    string? Owner,
    ComplianceTaskStatus? Status,
    string? Module,
    string? DueDate,
    string? Source);

public sealed record ObligationDashboardItemDto(
    string Id,
    Guid ContractId,
    string ContractNumber,
    string ContractTitle,
    Guid ContractClauseId,
    string ClauseNumber,
    string ObligationId,
    string Source,
    string SourceUrl,
    string Title,
    string PlainEnglishSummary,
    string RequiredAction,
    string OwnerFunction,
    RiskLevel RiskLevel,
    string Status,
    DateOnly? DueAt,
    string Module,
    bool IsOverdue,
    bool IsHighRisk,
    IReadOnlyList<string> EvidenceExamples,
    string Confidence,
    DateOnly LastReviewedAt,
    bool RequiresExpertReview);

public interface IObligationDashboardRepository
{
    Task<IReadOnlyList<ObligationDashboardItemDto>> ListCurrentTenantAsync(
        ObligationDashboardQuery query,
        CancellationToken cancellationToken = default);
}
