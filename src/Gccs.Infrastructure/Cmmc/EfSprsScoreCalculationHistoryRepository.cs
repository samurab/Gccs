using System.Text.Json;
using Gccs.Application.Cmmc;
using Gccs.Application.Security;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Cmmc;

public sealed class EfSprsScoreCalculationHistoryRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : ISprsScoreCalculationHistoryRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task SaveAsync(SprsScoreCalculationDto calculation, CancellationToken cancellationToken = default)
    {
        if (calculation.TenantId != tenantContext.TenantId)
        {
            throw new InvalidOperationException("SPRS calculation tenant scope does not match the current tenant.");
        }

        var entity = new SprsScoreCalculationEntity
        {
            Id = calculation.Id,
            TenantId = calculation.TenantId,
            AssessmentId = calculation.AssessmentId,
            RuleSetId = calculation.RuleSetId,
            RuleSetVersion = calculation.RuleSetVersion,
            RuleSetSourceUrl = calculation.RuleSetSourceUrl,
            RuleSetSourceSha256 = calculation.RuleSetSourceSha256,
            MaximumScore = calculation.MaximumScore,
            Score = calculation.Score,
            TotalDeduction = calculation.TotalDeduction,
            LineItemsJson = JsonSerializer.Serialize(calculation.LineItems, JsonOptions),
            UnresolvedGapsJson = JsonSerializer.Serialize(calculation.UnresolvedGaps, JsonOptions),
            GeneratedByUserId = calculation.GeneratedByUserId,
            GeneratedAt = calculation.GeneratedAt
        };

        if (!string.IsNullOrEmpty(calculation.ManualNotes))
        {
            entity.ReviewerNotes.Add(new SprsScoreCalculationNoteEntity
            {
                Id = Guid.NewGuid(),
                TenantId = calculation.TenantId,
                CalculationId = calculation.Id,
                Note = calculation.ManualNotes,
                Classification = calculation.ManualNotesClassification!.Classification,
                ClassificationSource = calculation.ManualNotesClassification.Source,
                ClassificationConfidence = calculation.ManualNotesClassification.Confidence,
                ClassificationReviewedByUserId = calculation.ManualNotesClassification.ReviewedByUserId,
                ClassificationReviewedAt = calculation.ManualNotesClassification.ReviewedAt,
                ClassificationReason = calculation.ManualNotesClassification.Reason,
                ClassificationIsApprovedDemoContent = calculation.ManualNotesClassification.IsApprovedDemoContent,
                CreatedByUserId = calculation.GeneratedByUserId,
                CreatedAt = calculation.GeneratedAt
            });
        }

        dbContext.SprsScoreCalculations.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SprsScoreCalculationDto>?> ListCurrentTenantAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        var assessmentExists = await dbContext.Assessments.AsNoTracking().AnyAsync(
            assessment => assessment.Id == assessmentId && assessment.TenantId == tenantContext.TenantId,
            cancellationToken);
        if (!assessmentExists)
        {
            return null;
        }

        var calculations = await dbContext.SprsScoreCalculations
            .AsNoTracking()
            .Include(calculation => calculation.ReviewerNotes)
            .Where(calculation => calculation.TenantId == tenantContext.TenantId && calculation.AssessmentId == assessmentId)
            .OrderByDescending(calculation => calculation.GeneratedAt)
            .ThenByDescending(calculation => calculation.Id)
            .Take(50)
            .ToArrayAsync(cancellationToken);

        return calculations.Select(ToDto).ToArray();
    }

    private static SprsScoreCalculationDto ToDto(SprsScoreCalculationEntity calculation) => new(
        calculation.Id,
        calculation.TenantId,
        calculation.AssessmentId,
        calculation.RuleSetId,
        calculation.RuleSetVersion,
        calculation.RuleSetSourceUrl,
        calculation.RuleSetSourceSha256,
        calculation.MaximumScore,
        calculation.Score,
        calculation.TotalDeduction,
        Deserialize<SprsScoreCalculationLineItemDto>(calculation.LineItemsJson),
        Deserialize<SprsUnresolvedGapDto>(calculation.UnresolvedGapsJson),
        calculation.ReviewerNotes.OrderBy(note => note.CreatedAt).Select(note => note.Note).LastOrDefault() ?? string.Empty,
        ToClassification(calculation.ReviewerNotes.OrderBy(note => note.CreatedAt).LastOrDefault()),
        calculation.GeneratedByUserId,
        calculation.GeneratedAt);

    private static Gccs.Application.Common.ContentClassificationDto? ToClassification(SprsScoreCalculationNoteEntity? note) =>
        note is null ? null : new(
            note.Classification,
            note.ClassificationSource,
            note.ClassificationConfidence,
            note.ClassificationReviewedByUserId,
            note.ClassificationReviewedAt,
            note.ClassificationReason,
            note.ClassificationIsApprovedDemoContent);

    private static IReadOnlyList<T> Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T[]>(json, JsonOptions) ?? [];
}
