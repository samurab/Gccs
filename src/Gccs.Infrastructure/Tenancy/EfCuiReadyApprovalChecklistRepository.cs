using Gccs.Application.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Tenancy;

public sealed class EfCuiReadyApprovalChecklistRepository(GccsDbContext dbContext) : ICuiReadyApprovalChecklistRepository
{
    public async Task<CuiReadyApprovalChecklistDto> CreateAsync(
        Guid tenantId,
        Guid actorUserId,
        IReadOnlyList<CreateCuiReadyChecklistItem> items,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new CuiReadyApprovalChecklistEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Version = 1,
            State = CuiReadyChecklistState.Draft,
            CreatedAt = now,
            CreatedByUserId = actorUserId,
            Items = items.Select(item => new CuiReadyApprovalChecklistItemEntity
            {
                Id = Guid.NewGuid(),
                ItemKey = item.ItemKey,
                Section = item.Section,
                Description = item.Description,
                IsRequired = item.IsRequired,
                Status = CuiReadyChecklistItemStatus.NotStarted
            }).ToArray()
        };

        dbContext.CuiReadyApprovalChecklists.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<CuiReadyApprovalChecklistDto?> FindAsync(Guid tenantId, Guid checklistId, CancellationToken cancellationToken = default)
    {
        var entity = await Query(tenantId)
            .SingleOrDefaultAsync(checklist => checklist.Id == checklistId, cancellationToken);

        return entity is null ? null : ToDto(entity);
    }

    public async Task<IReadOnlyList<CuiReadyApprovalChecklistDto>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var items = await Query(tenantId)
            .OrderByDescending(checklist => checklist.CreatedAt)
            .ToArrayAsync(cancellationToken);
        return items.Select(ToDto).ToArray();
    }

    public async Task<CuiReadyApprovalChecklistDto?> UpdateItemAsync(
        Guid tenantId,
        Guid checklistId,
        string itemKey,
        UpdateCuiReadyChecklistItemRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = await Query(tenantId).SingleOrDefaultAsync(checklist => checklist.Id == checklistId, cancellationToken);
        var item = entity?.Items.SingleOrDefault(candidate => candidate.ItemKey == itemKey);
        if (entity is null || item is null)
        {
            return null;
        }

        item.Status = request.Status;
        item.Owner = Normalize(request.Owner);
        item.EvidenceLink = Normalize(request.EvidenceLink);
        item.ReviewerUserId = request.ReviewerUserId;
        item.ReviewedAt = request.ReviewedAt;
        item.Notes = Normalize(request.Notes);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedByUserId = actorUserId;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<CuiReadyApprovalChecklistDto?> SetStateAsync(
        Guid tenantId,
        Guid checklistId,
        CuiReadyChecklistState state,
        Guid actorUserId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var entity = await Query(tenantId).SingleOrDefaultAsync(checklist => checklist.Id == checklistId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.State = state;
        entity.RejectionReason = state is CuiReadyChecklistState.Rejected ? Normalize(reason) : entity.RejectionReason;
        entity.ReviewNotes = state is CuiReadyChecklistState.Approved or CuiReadyChecklistState.Superseded
            ? Normalize(reason)
            : entity.ReviewNotes;
        entity.ReviewedByUserId = actorUserId;
        entity.ReviewedAt = DateTimeOffset.UtcNow;
        entity.UpdatedAt = entity.ReviewedAt;
        entity.UpdatedByUserId = actorUserId;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    private IQueryable<CuiReadyApprovalChecklistEntity> Query(Guid tenantId) =>
        dbContext.CuiReadyApprovalChecklists
            .Include(checklist => checklist.Items)
            .Where(checklist => checklist.TenantId == tenantId);

    private static CuiReadyApprovalChecklistDto ToDto(CuiReadyApprovalChecklistEntity entity) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.Version,
            entity.State,
            entity.RejectionReason,
            entity.ReviewNotes,
            entity.ReviewedByUserId,
            entity.ReviewedAt,
            entity.CreatedAt,
            entity.CreatedByUserId,
            entity.UpdatedAt,
            entity.UpdatedByUserId,
            entity.Items.OrderBy(item => item.Section).Select(ToItemDto).ToArray());

    private static CuiReadyApprovalChecklistItemDto ToItemDto(CuiReadyApprovalChecklistItemEntity item) =>
        new(
            item.Id,
            item.ChecklistId,
            item.ItemKey,
            item.Section,
            item.Description,
            item.IsRequired,
            item.Status,
            item.Owner,
            item.EvidenceLink,
            item.ReviewerUserId,
            item.ReviewedAt,
            item.Notes);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
