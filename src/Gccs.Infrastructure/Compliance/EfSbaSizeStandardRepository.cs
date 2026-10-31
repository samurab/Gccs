using Gccs.Application.Compliance;
using Gccs.Domain.Common;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Compliance;

public sealed class EfSbaSizeStandardRepository(GccsDbContext dbContext) : ISbaSizeStandardRepository
{
    public async Task<IReadOnlyList<SbaSizeStandardDto>> ImportAsync(
        IReadOnlyList<ImportSbaSizeStandardRequest> records,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entities = records.Select(record => new SbaSizeStandardEntity
        {
            Id = Guid.NewGuid(),
            NaicsCode = record.NaicsCode.Trim(),
            Metric = record.Metric.Trim(),
            Threshold = record.Threshold,
            Unit = record.Unit.Trim(),
            SourceUrl = record.SourceUrl.Trim(),
            EffectiveAt = record.EffectiveAt!.Value,
            LastReviewedAt = record.LastReviewedAt!.Value,
            Status = record.Status,
            ReviewedByUserId = record.Status is ReviewState.Approved or ReviewState.Published ? actorUserId : null,
            CreatedAt = now,
            CreatedByUserId = actorUserId
        }).ToArray();

        dbContext.SbaSizeStandards.AddRange(entities);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entities.Select(ToDto).OrderBy(record => record.NaicsCode).ToArray();
    }

    public async Task<IReadOnlyList<SbaSizeStandardDto>> ListApprovedAsync(CancellationToken cancellationToken = default)
    {
        var entities = await dbContext.SbaSizeStandards
            .AsNoTracking()
            .Where(record => record.Status == ReviewState.Approved || record.Status == ReviewState.Published)
            .OrderBy(record => record.NaicsCode)
            .ThenByDescending(record => record.EffectiveAt)
            .ToArrayAsync(cancellationToken);
        return entities.Select(ToDto).ToArray();
    }

    public async Task<IReadOnlyList<SbaSizeStandardDto>> ListForReviewAsync(CancellationToken cancellationToken = default)
    {
        var entities = await dbContext.SbaSizeStandards
            .AsNoTracking()
            .OrderBy(record => record.NaicsCode)
            .ThenBy(record => record.Status)
            .ToArrayAsync(cancellationToken);
        return entities.Select(ToDto).ToArray();
    }

    public async Task<SbaSizeStandardDto?> ChangeStatusAsync(
        Guid id,
        ReviewState status,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.SbaSizeStandards.SingleOrDefaultAsync(record => record.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Status = status;
        entity.ReviewedByUserId = status is ReviewState.Approved or ReviewState.Published ? actorUserId : entity.ReviewedByUserId;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedByUserId = actorUserId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    private static SbaSizeStandardDto ToDto(SbaSizeStandardEntity entity) =>
        new(
            entity.Id,
            entity.NaicsCode,
            entity.Metric,
            entity.Threshold,
            entity.Unit,
            entity.SourceUrl,
            entity.EffectiveAt,
            entity.LastReviewedAt,
            entity.Status,
            entity.ReviewedByUserId);
}
