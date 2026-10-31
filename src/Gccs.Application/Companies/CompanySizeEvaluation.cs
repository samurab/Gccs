using Gccs.Application.Audit;
using Gccs.Domain.Audit;

namespace Gccs.Application.Companies;

public sealed class CompanySizeEvaluationService(
    ICompanySizeEvaluationRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public Task<CompanySizeEvaluationResultDto> EvaluateCurrentTenantAsync(
        CompanySizeEvaluationRequest request,
        CancellationToken cancellationToken = default) =>
        repository.EvaluateCurrentTenantAsync(request, cancellationToken);

    public async Task<CompanySizeEvaluationResultDto?> SaveCurrentTenantAsync(
        CompanySizeEvaluationResultDto result,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var saved = await repository.SaveCurrentTenantAsync(result, actorUserId, cancellationToken);
        if (saved is null)
        {
            return null;
        }

        await auditEventWriter.WriteAsync(
            saved.TenantId,
            actorUserId,
            AuditAction.Updated,
            "CompanySizeEvaluation",
            saved.NaicsCode,
            $"Saved company size evaluation for NAICS {saved.NaicsCode}.",
            new Dictionary<string, string>
            {
                ["naicsCode"] = saved.NaicsCode,
                ["result"] = saved.Result,
                ["sourceUrl"] = saved.SourceUrl ?? string.Empty,
                ["runAt"] = saved.RunAt.ToString("O")
            },
            cancellationToken);

        return saved;
    }
}

public interface ICompanySizeEvaluationRepository
{
    Task<CompanySizeEvaluationResultDto> EvaluateCurrentTenantAsync(
        CompanySizeEvaluationRequest request,
        CancellationToken cancellationToken = default);

    Task<CompanySizeEvaluationResultDto?> SaveCurrentTenantAsync(
        CompanySizeEvaluationResultDto result,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed record CompanySizeEvaluationRequest(
    string NaicsCode,
    decimal? AnnualReceipts,
    int? EmployeeCount);

public sealed record CompanySizeEvaluationResultDto(
    Guid TenantId,
    string NaicsCode,
    string Metric,
    decimal? Threshold,
    string? Unit,
    decimal? EnteredValue,
    string EnteredValueLabel,
    string Result,
    string Explanation,
    string? SourceUrl,
    DateOnly? SourceEffectiveAt,
    DateOnly? SourceLastReviewedAt,
    DateTimeOffset RunAt);
