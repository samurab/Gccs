using Gccs.Application.Labor;
using Gccs.Application.Security;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Labor;

public sealed class EfLaborClassificationRepository(GccsDbContext db, ICurrentTenantContext tenantContext)
    : ILaborClassificationRepository
{
    public async Task<LaborCategoryDto> CreateCategoryAsync(LaborCategoryRequest request, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        EnsureCurrentTenant(tenantId);
        await RequireContractAsync(request.ContractId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var entity = new LaborCategoryEntity
        {
            Id = Guid.NewGuid(), TenantId = tenantContext.TenantId, ContractId = request.ContractId,
            Title = request.Title, WageDeterminationClassification = request.WageDeterminationClassification,
            HourlyWage = request.HourlyWage, FringeRate = request.FringeRate,
            FringeDescription = request.FringeDescription, Currency = "USD",
            EffectiveStart = request.EffectiveStart, EffectiveEnd = request.EffectiveEnd,
            SourceReference = request.SourceReference!, IsActive = true,
            CreatedAt = now, CreatedByUserId = actorUserId
        };
        db.LaborCategories.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<LaborCategoryDto?> UpdateCategoryAsync(Guid categoryId, LaborCategoryRequest request, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        EnsureCurrentTenant(tenantId);
        var entity = await CategoryQuery(true).SingleOrDefaultAsync(x => x.Id == categoryId, cancellationToken);
        if (entity is null) return null;
        await RequireContractAsync(request.ContractId, cancellationToken);
        entity.ContractId = request.ContractId; entity.Title = request.Title;
        entity.WageDeterminationClassification = request.WageDeterminationClassification; entity.HourlyWage = request.HourlyWage;
        entity.FringeRate = request.FringeRate; entity.FringeDescription = request.FringeDescription;
        entity.EffectiveStart = request.EffectiveStart; entity.EffectiveEnd = request.EffectiveEnd;
        entity.SourceReference = request.SourceReference!; entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedByUserId = actorUserId;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<LaborCategoryDto?> SetCategoryActiveAsync(Guid categoryId, bool isActive, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        EnsureCurrentTenant(tenantId);
        var entity = await CategoryQuery(true).SingleOrDefaultAsync(x => x.Id == categoryId, cancellationToken);
        if (entity is null) return null;
        entity.IsActive = isActive; entity.UpdatedAt = DateTimeOffset.UtcNow; entity.UpdatedByUserId = actorUserId;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<LaborCategoryDto?> FindCategoryAsync(Guid categoryId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        EnsureCurrentTenant(tenantId);
        var entity = await CategoryQuery().SingleOrDefaultAsync(x => x.Id == categoryId, cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<IReadOnlyList<LaborCategoryDto>> ListCategoriesAsync(Guid tenantId, Guid? contractId = null, CancellationToken cancellationToken = default)
    {
        EnsureCurrentTenant(tenantId);
        var rows = await CategoryQuery().Where(x => contractId == null || x.ContractId == contractId)
            .OrderBy(x => x.Title).ToArrayAsync(cancellationToken);
        return rows.Select(ToDto).ToArray();
    }

    public Task<bool> HasActiveAssignmentsAsync(Guid categoryId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        EnsureCurrentTenant(tenantId);
        return AssignmentBaseQuery().AnyAsync(
            x => x.LaborCategoryId == categoryId && x.Status == LaborAssignmentStatus.Active,
            cancellationToken);
    }

    public Task<bool> WouldInvalidateAssignmentsAsync(Guid categoryId, LaborCategoryRequest request, Guid tenantId, CancellationToken cancellationToken = default)
    {
        EnsureCurrentTenant(tenantId);
        var requestedEnd = request.EffectiveEnd ?? DateOnly.MaxValue;
        return AssignmentBaseQuery().AnyAsync(
            x => x.LaborCategoryId == categoryId && x.Status == LaborAssignmentStatus.Active &&
                 (x.ContractId != request.ContractId || x.EffectiveStart < request.EffectiveStart ||
                  (x.EffectiveEnd ?? DateOnly.MaxValue) > requestedEnd),
            cancellationToken);
    }

    public async Task<LaborEmployeeAssignmentDto> CreateAssignmentAsync(LaborEmployeeAssignmentRequest request, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        EnsureCurrentTenant(tenantId);
        var refs = await ResolveReferencesAsync(request, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var entity = new LaborEmployeeAssignmentEntity
        {
            Id = Guid.NewGuid(), TenantId = tenantContext.TenantId, EmployeeId = request.EmployeeId,
            ContractId = request.ContractId, LaborCategoryId = refs.Category.Id,
            WorkLocation = request.WorkLocation, EffectiveStart = request.EffectiveStart,
            EffectiveEnd = request.EffectiveEnd, Status = LaborAssignmentStatus.Active,
            SourceReference = request.SourceReference!, CreatedAt = now, CreatedByUserId = actorUserId,
            Employee = refs.Employee, Category = refs.Category
        };
        foreach (var link in refs.EvidenceLinks)
            entity.EvidenceLinks.Add(new LaborClassificationEvidenceEntity { TenantId = tenantContext.TenantId, AssignmentId = entity.Id, EvidenceItemId = link.EvidenceItemId, EvidenceType = link.EvidenceType });
        db.LaborEmployeeAssignments.Add(entity);
        await SaveAssignmentAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<LaborEmployeeAssignmentDto?> UpdateAssignmentAsync(Guid assignmentId, LaborEmployeeAssignmentRequest request, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        EnsureCurrentTenant(tenantId);
        var entity = await AssignmentQuery(true).SingleOrDefaultAsync(x => x.Id == assignmentId, cancellationToken);
        if (entity is null) return null;
        var refs = await ResolveReferencesAsync(request, cancellationToken);
        entity.EmployeeId = request.EmployeeId; entity.Employee = refs.Employee; entity.ContractId = request.ContractId;
        entity.LaborCategoryId = refs.Category.Id; entity.Category = refs.Category; entity.SourceReference = request.SourceReference!;
        entity.WorkLocation = request.WorkLocation; entity.EffectiveStart = request.EffectiveStart; entity.EffectiveEnd = request.EffectiveEnd;
        entity.UpdatedAt = DateTimeOffset.UtcNow; entity.UpdatedByUserId = actorUserId;
        ResetReview(entity);
        db.LaborClassificationEvidence.RemoveRange(entity.EvidenceLinks);
        entity.EvidenceLinks.Clear();
        foreach (var link in refs.EvidenceLinks)
            entity.EvidenceLinks.Add(new LaborClassificationEvidenceEntity { TenantId = tenantContext.TenantId, AssignmentId = entity.Id, EvidenceItemId = link.EvidenceItemId, EvidenceType = link.EvidenceType });
        await SaveAssignmentAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<LaborEmployeeAssignmentDto?> SetAssignmentStatusAsync(Guid assignmentId, LaborAssignmentStatus status, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        EnsureCurrentTenant(tenantId);
        var entity = await AssignmentQuery(true).SingleOrDefaultAsync(x => x.Id == assignmentId, cancellationToken);
        if (entity is null) return null;
        entity.Status = status; entity.UpdatedAt = DateTimeOffset.UtcNow; entity.UpdatedByUserId = actorUserId;
        await SaveAssignmentAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<LaborEmployeeAssignmentDto?> ReclassifyAsync(Guid assignmentId, Guid newCategoryId, string reason, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        EnsureCurrentTenant(tenantId);
        var entity = await AssignmentQuery(true).SingleOrDefaultAsync(x => x.Id == assignmentId, cancellationToken);
        if (entity is null) return null;
        var category = await CategoryQuery(true).SingleOrDefaultAsync(x => x.Id == newCategoryId && x.ContractId == entity.ContractId, cancellationToken);
        if (category is null) throw new LaborClassificationValidationException("The new labor category was not found on the assignment contract.");
        if (!category.IsActive) throw new LaborClassificationValidationException("Inactive labor categories cannot be assigned.");
        var history = new LaborClassificationHistoryEntity
        {
            Id = Guid.NewGuid(), TenantId = tenantContext.TenantId, AssignmentId = entity.Id,
            PriorCategoryId = entity.LaborCategoryId, PriorCategoryTitle = entity.Category?.Title,
            NewCategoryId = category.Id, NewCategoryTitle = category.Title,
            ActorUserId = actorUserId, ChangedAt = DateTimeOffset.UtcNow, Reason = reason
        };
        entity.LaborCategoryId = category.Id; entity.Category = category;
        entity.UpdatedAt = history.ChangedAt; entity.UpdatedByUserId = actorUserId;
        ResetReview(entity);
        db.LaborClassificationHistory.Add(history);
        await SaveAssignmentAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<LaborEmployeeAssignmentDto?> ReviewAssignmentAsync(Guid assignmentId, LaborClassificationReviewStatus status, string notes, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        EnsureCurrentTenant(tenantId);
        var entity = await AssignmentQuery(true).SingleOrDefaultAsync(x => x.Id == assignmentId, cancellationToken);
        if (entity is null) return null;
        entity.ReviewStatus = status;
        entity.ReviewNotes = notes;
        entity.ReviewedByUserId = actorUserId;
        entity.ReviewedAt = DateTimeOffset.UtcNow;
        entity.UpdatedAt = entity.ReviewedAt;
        entity.UpdatedByUserId = actorUserId;
        await SaveAssignmentAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<LaborEmployeeAssignmentDto?> FindAssignmentAsync(Guid assignmentId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        EnsureCurrentTenant(tenantId);
        var entity = await AssignmentQuery().SingleOrDefaultAsync(x => x.Id == assignmentId, cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<IReadOnlyList<LaborEmployeeAssignmentDto>> ListAssignmentsAsync(Guid tenantId, Guid? contractId = null, CancellationToken cancellationToken = default)
    {
        EnsureCurrentTenant(tenantId);
        var rows = await AssignmentQuery().Where(x => contractId == null || x.ContractId == contractId)
            .OrderBy(x => x.Employee!.Name).ThenBy(x => x.EffectiveStart).ToArrayAsync(cancellationToken);
        return rows.Select(ToDto).ToArray();
    }

    public Task<bool> HasDateConflictAsync(Guid tenantId, LaborEmployeeAssignmentRequest request, Guid? existingAssignmentId, CancellationToken cancellationToken = default)
    {
        EnsureCurrentTenant(tenantId);
        var requestedEnd = request.EffectiveEnd ?? DateOnly.MaxValue;
        return AssignmentBaseQuery().AnyAsync(x => x.Id != existingAssignmentId && x.Status == LaborAssignmentStatus.Active &&
            x.EmployeeId == request.EmployeeId && x.ContractId == request.ContractId &&
            request.EffectiveStart <= (x.EffectiveEnd ?? DateOnly.MaxValue) && requestedEnd >= x.EffectiveStart, cancellationToken);
    }

    public async Task<IReadOnlyList<LaborEmployeeOptionDto>> ListEmployeesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        EnsureCurrentTenant(tenantId);
        return await db.Employees.AsNoTracking()
            .Where(x => x.TenantId == tenantContext.TenantId && x.Status == Gccs.Domain.People.EmploymentStatus.Active)
            .OrderBy(x => x.Name)
            .Select(x => new LaborEmployeeOptionDto(x.Id, x.TenantId, x.EmployeeNumber, x.Name, x.Email))
            .ToArrayAsync(cancellationToken);
    }

    private IQueryable<LaborCategoryEntity> CategoryQuery(bool tracking = false) =>
        (tracking ? db.LaborCategories : db.LaborCategories.AsNoTracking()).Where(x => x.TenantId == tenantContext.TenantId);

    private IQueryable<LaborEmployeeAssignmentEntity> AssignmentBaseQuery(bool tracking = false) =>
        (tracking ? db.LaborEmployeeAssignments : db.LaborEmployeeAssignments.AsNoTracking()).Where(x => x.TenantId == tenantContext.TenantId);

    private IQueryable<LaborEmployeeAssignmentEntity> AssignmentQuery(bool tracking = false) =>
        AssignmentBaseQuery(tracking).Include(x => x.Employee).Include(x => x.Category)
            .Include(x => x.History).Include(x => x.EvidenceLinks);

    private async Task<(EmployeeEntity Employee, LaborCategoryEntity Category, LaborEvidenceLinkRequest[] EvidenceLinks)> ResolveReferencesAsync(LaborEmployeeAssignmentRequest request, CancellationToken token)
    {
        await RequireContractAsync(request.ContractId, token);
        var employee = await db.Employees.SingleOrDefaultAsync(x => x.TenantId == tenantContext.TenantId && x.Id == request.EmployeeId, token)
            ?? throw new LaborClassificationValidationException("The employee was not found for the current tenant.");
        var category = await CategoryQuery(true).SingleOrDefaultAsync(x => x.Id == request.CategoryId && x.ContractId == request.ContractId, token)
            ?? throw new LaborClassificationValidationException("The labor category was not found on the current-tenant contract.");
        var evidenceLinks = LaborClassificationService.NormalizeEvidenceLinks(request).ToArray();
        var evidenceIds = evidenceLinks.Select(x => x.EvidenceItemId).ToArray();
        if (evidenceIds.Length > 0)
        {
            var validCount = await db.EvidenceItems.CountAsync(x => x.TenantId == tenantContext.TenantId && evidenceIds.Contains(x.Id), token);
            if (validCount != evidenceIds.Length) throw new LaborClassificationValidationException("Every evidence link must belong to the current tenant.");
        }
        return (employee, category, evidenceLinks);
    }

    private async Task RequireContractAsync(Guid contractId, CancellationToken token)
    {
        if (!await db.Contracts.AnyAsync(x => x.TenantId == tenantContext.TenantId && x.Id == contractId, token))
            throw new LaborClassificationValidationException("The contract was not found for the current tenant.");
    }

    private async Task SaveAssignmentAsync(CancellationToken token)
    {
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException) { throw new LaborClassificationConflictException(); }
        catch (DbUpdateException exception) when (exception.InnerException?.Message.Contains("labor_assignment_no_overlap", StringComparison.OrdinalIgnoreCase) == true)
        { throw new LaborClassificationValidationException("Assignment effective dates conflict with an existing assignment."); }
    }

    private static void ResetReview(LaborEmployeeAssignmentEntity entity)
    {
        entity.ReviewStatus = LaborClassificationReviewStatus.PendingReview;
        entity.ReviewNotes = null;
        entity.ReviewedByUserId = null;
        entity.ReviewedAt = null;
    }

    private void EnsureCurrentTenant(Guid tenantId)
    {
        if (tenantId != tenantContext.TenantId) throw new InvalidOperationException("Labor classification tenant context mismatch.");
    }

    private static LaborCategoryDto ToDto(LaborCategoryEntity x) => new(x.Id, x.TenantId, x.ContractId, x.Title,
        x.WageDeterminationClassification, x.HourlyWage, x.FringeRate, x.FringeDescription, x.EffectiveStart, x.EffectiveEnd,
        x.SourceReference, x.IsActive, x.CreatedAt, x.UpdatedAt);

    private static LaborEmployeeAssignmentDto ToDto(LaborEmployeeAssignmentEntity x) => new(x.Id, x.TenantId, x.EmployeeId,
        x.Employee?.Name ?? string.Empty, x.Employee?.Email ?? string.Empty, x.ContractId, x.LaborCategoryId,
        x.Category?.Title ?? string.Empty, x.WorkLocation, x.EffectiveStart, x.EffectiveEnd, x.Status,
        x.SourceReference, x.EvidenceLinks.Select(link => link.EvidenceItemId).ToArray(),
        x.History.OrderBy(item => item.ChangedAt).Select(item => new LaborClassificationHistoryDto(item.Id,
            item.AssignmentId, item.PriorCategoryId, item.PriorCategoryTitle, item.NewCategoryId,
            item.NewCategoryTitle, item.ActorUserId, item.ChangedAt, item.Reason)).ToArray(),
        x.ReviewStatus, x.ReviewNotes, x.ReviewedByUserId, x.ReviewedAt, x.CreatedAt, x.UpdatedAt)
        { EvidenceLinks = x.EvidenceLinks.Select(link => new LaborEvidenceLinkRequest(link.EvidenceItemId, link.EvidenceType)).ToArray() };
}
