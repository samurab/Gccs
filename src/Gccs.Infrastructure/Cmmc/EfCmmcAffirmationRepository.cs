using System.Text.Json;
using Gccs.Application.Cmmc;
using Gccs.Application.Security;
using Gccs.Domain.Cmmc;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Cmmc;

public sealed class EfCmmcAffirmationRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : ICmmcAffirmationRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<CmmcAffirmationDto>> ListCurrentTenantAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var affirmations = await dbContext.AnnualAffirmations
            .AsNoTracking()
            .Where(affirmation => affirmation.TenantId == tenantContext.TenantId)
            .OrderBy(affirmation => affirmation.DueAt)
            .ToArrayAsync(cancellationToken);
        return affirmations.Select(affirmation => ToDto(affirmation, today)).ToArray();
    }

    public async Task<CmmcAffirmationDto> CreateAsync(
        UpsertCmmcAffirmationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new AnnualAffirmationEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            Level = request.Level,
            DueAt = request.DueAt,
            SubmittedAt = request.SubmittedAt,
            SubmittedByUserId = request.SubmittedByUserId,
            ConfirmationReference = request.ConfirmationReference,
            EvidenceItemIdsJson = JsonSerializer.Serialize(request.EvidenceItemIds, JsonOptions),
            Status = request.Status,
            CreatedAt = now,
            CreatedByUserId = actorUserId
        };

        dbContext.AnnualAffirmations.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity, DateOnly.FromDateTime(DateTime.UtcNow));
    }

    public async Task<CmmcAffirmationDto?> UpdateAsync(
        Guid affirmationId,
        UpsertCmmcAffirmationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.AnnualAffirmations.SingleOrDefaultAsync(
            affirmation => affirmation.TenantId == tenantContext.TenantId && affirmation.Id == affirmationId,
            cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Level = request.Level;
        entity.DueAt = request.DueAt;
        entity.SubmittedAt = request.SubmittedAt;
        entity.SubmittedByUserId = request.SubmittedByUserId;
        entity.ConfirmationReference = request.ConfirmationReference;
        entity.EvidenceItemIdsJson = JsonSerializer.Serialize(request.EvidenceItemIds, JsonOptions);
        entity.Status = request.Status;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedByUserId = actorUserId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity, DateOnly.FromDateTime(DateTime.UtcNow));
    }

    private static CmmcAffirmationDto ToDto(AnnualAffirmationEntity entity, DateOnly today)
    {
        var isOpen = entity.Status is not AffirmationStatus.Submitted and not AffirmationStatus.NotRequired;
        return new CmmcAffirmationDto(
            entity.Id,
            entity.TenantId,
            entity.Level,
            entity.DueAt,
            entity.SubmittedAt,
            entity.SubmittedByUserId,
            entity.ConfirmationReference,
            ReadGuidArray(entity.EvidenceItemIdsJson),
            entity.Status,
            isOpen && entity.DueAt >= today && entity.DueAt <= today.AddDays(45),
            isOpen && entity.DueAt < today,
            entity.CreatedAt,
            entity.UpdatedAt);
    }

    private static IReadOnlyList<Guid> ReadGuidArray(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Guid[]>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
