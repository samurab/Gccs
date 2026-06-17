using Gccs.Application.Companies;
using Gccs.Application.Security;
using Gccs.Domain.Common;
using Gccs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Companies;

public sealed class EfCompanySizeEvaluationRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : ICompanySizeEvaluationRepository
{
    public async Task<CompanySizeEvaluationResultDto> EvaluateCurrentTenantAsync(
        CompanySizeEvaluationRequest request,
        CancellationToken cancellationToken = default)
    {
        var naicsCode = request.NaicsCode.Trim();
        var standard = await dbContext.SbaSizeStandards
            .AsNoTracking()
            .Where(record =>
                record.NaicsCode == naicsCode &&
                (record.Status == ReviewState.Approved || record.Status == ReviewState.Published))
            .OrderByDescending(record => record.EffectiveAt)
            .FirstOrDefaultAsync(cancellationToken);
        var runAt = DateTimeOffset.UtcNow;
        if (standard is null)
        {
            return new CompanySizeEvaluationResultDto(
                tenantContext.TenantId,
                naicsCode,
                "Unknown",
                null,
                null,
                null,
                "No approved size standard",
                "ExpertReviewRecommended",
                "No approved SBA size standard is available for this NAICS code.",
                null,
                null,
                null,
                runAt);
        }

        var metricRequiresReceipts = standard.Metric.Contains("receipt", StringComparison.OrdinalIgnoreCase) ||
            standard.Metric.Contains("revenue", StringComparison.OrdinalIgnoreCase);
        decimal? enteredValue = metricRequiresReceipts ? request.AnnualReceipts : request.EmployeeCount;
        if (enteredValue is null)
        {
            return ToResult("InsufficientInformation", $"Enter {(metricRequiresReceipts ? "annual receipts" : "employee count")} to evaluate NAICS {naicsCode}.", standard, enteredValue, runAt);
        }

        var result = enteredValue <= standard.Threshold ? "LikelySmall" : "OtherThanSmall";
        return ToResult(result, $"Entered value {enteredValue} compared with SBA threshold {standard.Threshold} {standard.Unit}.", standard, enteredValue, runAt);
    }

    public async Task<CompanySizeEvaluationResultDto?> SaveCurrentTenantAsync(
        CompanySizeEvaluationResultDto result,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.CompanyProfiles
            .Include(profile => profile.NaicsCodes)
            .SingleOrDefaultAsync(profile => profile.TenantId == tenantContext.TenantId, cancellationToken);
        if (profile is null)
        {
            return null;
        }

        var naics = profile.NaicsCodes.FirstOrDefault(item => item.Code == result.NaicsCode);
        if (naics is null)
        {
            return null;
        }

        naics.SizeStandard = result.Threshold is null || result.Unit is null ? null : $"{result.Threshold:0.##} {result.Unit}";
        naics.QualifiesAsSmall = result.Result switch
        {
            "LikelySmall" => true,
            "OtherThanSmall" => false,
            _ => null
        };
        naics.LastCheckedAt = DateOnly.FromDateTime(result.RunAt.UtcDateTime);
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        profile.UpdatedByUserId = actorUserId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return result;
    }

    private CompanySizeEvaluationResultDto ToResult(
        string result,
        string explanation,
        Persistence.Models.SbaSizeStandardEntity standard,
        decimal? enteredValue,
        DateTimeOffset runAt) =>
        new(
            tenantContext.TenantId,
            standard.NaicsCode,
            standard.Metric,
            standard.Threshold,
            standard.Unit,
            enteredValue,
            enteredValue is null ? "missing" : enteredValue.Value.ToString("0.##"),
            result,
            explanation,
            standard.SourceUrl,
            standard.EffectiveAt,
            standard.LastReviewedAt,
            runAt);
}
