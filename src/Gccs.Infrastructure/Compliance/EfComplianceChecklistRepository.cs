using Gccs.Application.Compliance;
using Gccs.Application.Security;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Compliance;

public sealed class EfComplianceChecklistRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : IComplianceChecklistRepository
{
    public async Task<IReadOnlyList<ComplianceChecklistInstanceDto>> ListCurrentTenantAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await QueryCurrentTenantChecklists()
            .OrderByDescending(checklist => checklist.CreatedAt)
            .ToArrayAsync(cancellationToken);

        return entities.Select(ToDto).ToArray();
    }

    public async Task<ComplianceChecklistInstanceDto?> CreateCurrentTenantAsync(
        ComplianceChecklistTemplateDto template,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var checklistId = Guid.NewGuid();
        var checklist = new ComplianceChecklistInstanceEntity
        {
            Id = checklistId,
            TenantId = tenantContext.TenantId,
            TemplateKey = template.Key,
            Name = template.Name,
            ChecklistType = template.ChecklistType,
            ReviewStatus = ComplianceChecklistReviewStatusValues.NotReviewed,
            CreatedAt = now,
            CreatedByUserId = actorUserId,
            Items = template.Items.Select(item => new ComplianceChecklistItemEntity
            {
                Id = Guid.NewGuid(),
                ChecklistId = checklistId,
                TemplateItemKey = item.Key,
                Title = item.Title,
                Description = item.Description,
                Status = ComplianceChecklistStatusValues.NotStarted,
                ReviewStatus = ComplianceChecklistReviewStatusValues.NotReviewed,
                ControlId = item.ControlId
            }).ToArray()
        };

        dbContext.ComplianceChecklistInstances.Add(checklist);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(checklist);
    }

    public async Task<ComplianceChecklistInstanceDto?> UpdateItemCurrentTenantAsync(
        Guid checklistId,
        Guid itemId,
        UpdateComplianceChecklistItemRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var checklist = await QueryCurrentTenantChecklists()
            .SingleOrDefaultAsync(candidate => candidate.Id == checklistId, cancellationToken);
        if (checklist is null)
        {
            return null;
        }

        var item = checklist.Items.SingleOrDefault(candidate => candidate.Id == itemId);
        if (item is null)
        {
            return null;
        }

        await ValidateTenantLinksAsync(request, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var normalizedStatus = Normalize(request.Status, ComplianceChecklistStatusValues.All);
        var normalizedReviewStatus = Normalize(request.ReviewStatus, ComplianceChecklistReviewStatusValues.All);

        item.Status = normalizedStatus;
        item.OwnerUserId = request.OwnerUserId;
        item.ReviewStatus = normalizedReviewStatus;
        item.ReviewedByUserId = request.ReviewedByUserId;
        item.ReviewedAt = request.ReviewedAt;
        item.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        item.ControlId = string.IsNullOrWhiteSpace(request.ControlId) ? null : request.ControlId.Trim();
        item.EvidenceItemId = request.EvidenceItemId;
        item.PoamItemId = request.PoamItemId;

        if (normalizedStatus == ComplianceChecklistStatusValues.Complete && item.CompletedAt is null)
        {
            item.CompletedAt = now;
            item.CompletedByUserId = actorUserId;
        }

        if (normalizedReviewStatus is ComplianceChecklistReviewStatusValues.Accepted or ComplianceChecklistReviewStatusValues.Rejected &&
            item.ReviewedByUserId is null)
        {
            item.ReviewedByUserId = actorUserId;
            item.ReviewedAt ??= now;
        }

        checklist.ReviewStatus = AggregateReviewStatus(checklist.Items);
        checklist.UpdatedAt = now;
        checklist.UpdatedByUserId = actorUserId;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(checklist);
    }

    private IQueryable<ComplianceChecklistInstanceEntity> QueryCurrentTenantChecklists() =>
        dbContext.ComplianceChecklistInstances
            .Include(checklist => checklist.Items)
            .Where(checklist => checklist.TenantId == tenantContext.TenantId);

    private async Task ValidateTenantLinksAsync(
        UpdateComplianceChecklistItemRequest request,
        CancellationToken cancellationToken)
    {
        if (request.EvidenceItemId is { } evidenceItemId)
        {
            var evidenceExists = await dbContext.EvidenceItems
                .AnyAsync(
                    evidence => evidence.TenantId == tenantContext.TenantId && evidence.Id == evidenceItemId,
                    cancellationToken);
            if (!evidenceExists)
            {
                throw new ComplianceChecklistValidationException("Linked evidence item was not found in the current tenant.");
            }
        }

        if (request.PoamItemId is { } poamItemId)
        {
            var poamExists = await dbContext.PoamItems
                .AnyAsync(
                    poam => poam.TenantId == tenantContext.TenantId && poam.Id == poamItemId,
                    cancellationToken);
            if (!poamExists)
            {
                throw new ComplianceChecklistValidationException("Linked POA&M item was not found in the current tenant.");
            }
        }
    }

    private static string Normalize(string value, IReadOnlyCollection<string> allowedValues) =>
        allowedValues.Single(allowed => string.Equals(allowed, value, StringComparison.OrdinalIgnoreCase));

    private static string AggregateReviewStatus(IEnumerable<ComplianceChecklistItemEntity> items)
    {
        var statuses = items.Select(item => item.ReviewStatus).ToArray();
        if (statuses.Any(status => status == ComplianceChecklistReviewStatusValues.Rejected))
        {
            return ComplianceChecklistReviewStatusValues.Rejected;
        }

        if (statuses.Length > 0 && statuses.All(status => status == ComplianceChecklistReviewStatusValues.Accepted))
        {
            return ComplianceChecklistReviewStatusValues.Accepted;
        }

        if (statuses.Any(status => status == ComplianceChecklistReviewStatusValues.PendingReview))
        {
            return ComplianceChecklistReviewStatusValues.PendingReview;
        }

        return ComplianceChecklistReviewStatusValues.NotReviewed;
    }

    private static ComplianceChecklistInstanceDto ToDto(ComplianceChecklistInstanceEntity entity) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.TemplateKey,
            entity.Name,
            entity.ChecklistType,
            entity.ReviewStatus,
            entity.CreatedAt,
            entity.CreatedByUserId ?? Guid.Empty,
            entity.Items
                .OrderBy(item => item.TemplateItemKey)
                .Select(ToDto)
                .ToArray());

    private static ComplianceChecklistItemDto ToDto(ComplianceChecklistItemEntity entity) =>
        new(
            entity.Id,
            entity.ChecklistId,
            entity.TemplateItemKey,
            entity.Title,
            entity.Description,
            entity.Status,
            entity.OwnerUserId,
            entity.ReviewStatus,
            entity.ReviewedByUserId,
            entity.ReviewedAt,
            entity.Notes,
            entity.ControlId,
            entity.EvidenceItemId,
            entity.PoamItemId,
            entity.CompletedAt,
            entity.CompletedByUserId);
}
