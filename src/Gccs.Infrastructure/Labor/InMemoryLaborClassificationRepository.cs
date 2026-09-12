using Gccs.Application.Labor;

namespace Gccs.Infrastructure.Labor;

public sealed class InMemoryLaborClassificationRepository : ILaborClassificationRepository
{
    private readonly List<LaborCategoryDto> _categories = [];
    private readonly List<LaborEmployeeAssignmentDto> _assignments = [];
    private readonly List<LaborEmployeeOptionDto> _employees = [];

    public void AddEmployee(LaborEmployeeOptionDto employee) => _employees.Add(employee);

    public Task<LaborCategoryDto> CreateCategoryAsync(
        LaborCategoryRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var category = new LaborCategoryDto(
            Guid.NewGuid(),
            tenantId,
            request.ContractId,
            request.Title,
            request.WageDeterminationClassification,
            request.HourlyWage,
            request.FringeRate,
            request.FringeDescription,
            request.EffectiveStart,
            request.EffectiveEnd,
            request.SourceReference ?? string.Empty,
            true,
            DateTimeOffset.UtcNow,
            null);
        _categories.Add(category);
        return Task.FromResult(category);
    }

    public Task<LaborCategoryDto?> UpdateCategoryAsync(
        Guid categoryId,
        LaborCategoryRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = _categories.SingleOrDefault(category => category.Id == categoryId && category.TenantId == tenantId);
        if (existing is null)
        {
            return Task.FromResult<LaborCategoryDto?>(null);
        }

        var updated = existing with
        {
            ContractId = request.ContractId,
            Title = request.Title,
            WageDeterminationClassification = request.WageDeterminationClassification,
            HourlyWage = request.HourlyWage,
            FringeRate = request.FringeRate,
            FringeDescription = request.FringeDescription,
            EffectiveStart = request.EffectiveStart,
            EffectiveEnd = request.EffectiveEnd,
            SourceReference = request.SourceReference ?? string.Empty,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        ReplaceCategory(existing, updated);
        return Task.FromResult<LaborCategoryDto?>(updated);
    }

    public Task<LaborCategoryDto?> SetCategoryActiveAsync(
        Guid categoryId,
        bool isActive,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = _categories.SingleOrDefault(category => category.Id == categoryId && category.TenantId == tenantId);
        if (existing is null)
        {
            return Task.FromResult<LaborCategoryDto?>(null);
        }

        var updated = existing with { IsActive = isActive, UpdatedAt = DateTimeOffset.UtcNow };
        ReplaceCategory(existing, updated);
        return Task.FromResult<LaborCategoryDto?>(updated);
    }

    public Task<LaborCategoryDto?> FindCategoryAsync(Guid categoryId, Guid tenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_categories.SingleOrDefault(category => category.Id == categoryId && category.TenantId == tenantId));

    public Task<IReadOnlyList<LaborCategoryDto>> ListCategoriesAsync(
        Guid tenantId,
        Guid? contractId = null,
        CancellationToken cancellationToken = default)
    {
        var categories = _categories
            .Where(category => category.TenantId == tenantId)
            .Where(category => contractId is null || category.ContractId == contractId)
            .OrderBy(category => category.Title)
            .ToArray();
        return Task.FromResult<IReadOnlyList<LaborCategoryDto>>(categories);
    }

    public Task<bool> HasActiveAssignmentsAsync(Guid categoryId, Guid tenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_assignments.Any(x => x.TenantId == tenantId && x.CategoryId == categoryId && x.Status == LaborAssignmentStatus.Active));

    public Task<bool> WouldInvalidateAssignmentsAsync(Guid categoryId, LaborCategoryRequest request, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var requestedEnd = request.EffectiveEnd ?? DateOnly.MaxValue;
        return Task.FromResult(_assignments.Any(x => x.TenantId == tenantId && x.CategoryId == categoryId &&
            x.Status == LaborAssignmentStatus.Active && (x.ContractId != request.ContractId ||
            x.EffectiveStart < request.EffectiveStart || (x.EffectiveEnd ?? DateOnly.MaxValue) > requestedEnd)));
    }

    public Task<LaborEmployeeAssignmentDto> CreateAssignmentAsync(
        LaborEmployeeAssignmentRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var category = _categories.Single(candidate => candidate.Id == request.CategoryId && candidate.TenantId == tenantId);
        var employee = _employees.SingleOrDefault(candidate => candidate.Id == request.EmployeeId && candidate.TenantId == tenantId)
            ?? throw new LaborClassificationValidationException("The employee was not found for the current tenant.");
        var assignment = new LaborEmployeeAssignmentDto(
            Guid.NewGuid(),
            tenantId,
            request.EmployeeId,
            employee.Name,
            employee.Email,
            request.ContractId,
            request.CategoryId,
            category.Title,
            request.WorkLocation,
            request.EffectiveStart,
            request.EffectiveEnd,
            LaborAssignmentStatus.Active,
            request.SourceReference ?? string.Empty,
            request.EvidenceItemIds?.Distinct().ToArray() ?? [],
            [],
            LaborClassificationReviewStatus.PendingReview,
            null,
            null,
            null,
            DateTimeOffset.UtcNow,
            null)
        { EvidenceLinks = LaborClassificationService.NormalizeEvidenceLinks(request) };
        _assignments.Add(assignment);
        return Task.FromResult(assignment);
    }

    public Task<LaborEmployeeAssignmentDto?> UpdateAssignmentAsync(
        Guid assignmentId,
        LaborEmployeeAssignmentRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = _assignments.SingleOrDefault(assignment => assignment.Id == assignmentId && assignment.TenantId == tenantId);
        if (existing is null)
        {
            return Task.FromResult<LaborEmployeeAssignmentDto?>(null);
        }

        var category = _categories.Single(candidate => candidate.Id == request.CategoryId && candidate.TenantId == tenantId);
        var employee = _employees.SingleOrDefault(candidate => candidate.Id == request.EmployeeId && candidate.TenantId == tenantId)
            ?? throw new LaborClassificationValidationException("The employee was not found for the current tenant.");
        var updated = existing with
        {
            EmployeeId = request.EmployeeId,
            EmployeeName = employee.Name,
            EmployeeEmail = employee.Email,
            ContractId = request.ContractId,
            CategoryId = request.CategoryId,
            LaborCategoryTitle = category.Title,
            WorkLocation = request.WorkLocation,
            EffectiveStart = request.EffectiveStart,
            EffectiveEnd = request.EffectiveEnd,
            SourceReference = request.SourceReference ?? string.Empty,
            EvidenceItemIds = LaborClassificationService.NormalizeEvidenceLinks(request).Select(x => x.EvidenceItemId).ToArray(),
            EvidenceLinks = LaborClassificationService.NormalizeEvidenceLinks(request),
            ReviewStatus = LaborClassificationReviewStatus.PendingReview,
            ReviewNotes = null,
            ReviewedByUserId = null,
            ReviewedAt = null,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        ReplaceAssignment(existing, updated);
        return Task.FromResult<LaborEmployeeAssignmentDto?>(updated);
    }

    public Task<LaborEmployeeAssignmentDto?> SetAssignmentStatusAsync(
        Guid assignmentId,
        LaborAssignmentStatus status,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = _assignments.SingleOrDefault(assignment => assignment.Id == assignmentId && assignment.TenantId == tenantId);
        if (existing is null)
        {
            return Task.FromResult<LaborEmployeeAssignmentDto?>(null);
        }

        var updated = existing with { Status = status, UpdatedAt = DateTimeOffset.UtcNow };
        ReplaceAssignment(existing, updated);
        return Task.FromResult<LaborEmployeeAssignmentDto?>(updated);
    }

    public Task<LaborEmployeeAssignmentDto?> ReclassifyAsync(
        Guid assignmentId,
        Guid newCategoryId,
        string reason,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = _assignments.SingleOrDefault(assignment => assignment.Id == assignmentId && assignment.TenantId == tenantId);
        var newCategory = _categories.SingleOrDefault(category => category.Id == newCategoryId && category.TenantId == tenantId);
        if (existing is null || newCategory is null || !newCategory.IsActive)
        {
            return Task.FromResult<LaborEmployeeAssignmentDto?>(null);
        }

        var history = existing.History.Concat([
            new LaborClassificationHistoryDto(
                Guid.NewGuid(),
                existing.Id,
                existing.CategoryId,
                existing.LaborCategoryTitle,
                newCategory.Id,
                newCategory.Title,
                actorUserId,
                DateTimeOffset.UtcNow,
                reason)
        ]).ToArray();
        var updated = existing with
        {
            CategoryId = newCategory.Id,
            LaborCategoryTitle = newCategory.Title,
            History = history,
            ReviewStatus = LaborClassificationReviewStatus.PendingReview,
            ReviewNotes = null,
            ReviewedByUserId = null,
            ReviewedAt = null,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        ReplaceAssignment(existing, updated);
        return Task.FromResult<LaborEmployeeAssignmentDto?>(updated);
    }

    public Task<LaborEmployeeAssignmentDto?> ReviewAssignmentAsync(
        Guid assignmentId,
        LaborClassificationReviewStatus status,
        string notes,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = _assignments.SingleOrDefault(x => x.Id == assignmentId && x.TenantId == tenantId);
        if (existing is null) return Task.FromResult<LaborEmployeeAssignmentDto?>(null);
        var now = DateTimeOffset.UtcNow;
        var updated = existing with
        {
            ReviewStatus = status,
            ReviewNotes = notes,
            ReviewedByUserId = actorUserId,
            ReviewedAt = now,
            UpdatedAt = now
        };
        ReplaceAssignment(existing, updated);
        return Task.FromResult<LaborEmployeeAssignmentDto?>(updated);
    }

    public Task<LaborEmployeeAssignmentDto?> FindAssignmentAsync(Guid assignmentId, Guid tenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_assignments.SingleOrDefault(assignment => assignment.Id == assignmentId && assignment.TenantId == tenantId));

    public Task<IReadOnlyList<LaborEmployeeAssignmentDto>> ListAssignmentsAsync(
        Guid tenantId,
        Guid? contractId = null,
        CancellationToken cancellationToken = default)
    {
        var assignments = _assignments
            .Where(assignment => assignment.TenantId == tenantId)
            .Where(assignment => contractId is null || assignment.ContractId == contractId)
            .OrderBy(assignment => assignment.EmployeeName)
            .ToArray();
        return Task.FromResult<IReadOnlyList<LaborEmployeeAssignmentDto>>(assignments);
    }

    public Task<bool> HasDateConflictAsync(
        Guid tenantId,
        LaborEmployeeAssignmentRequest request,
        Guid? existingAssignmentId,
        CancellationToken cancellationToken = default)
    {
        var requestedEnd = request.EffectiveEnd ?? DateOnly.MaxValue;
        var conflict = _assignments.Any(assignment =>
            assignment.TenantId == tenantId &&
            assignment.Id != existingAssignmentId &&
            assignment.Status == LaborAssignmentStatus.Active &&
            assignment.EmployeeId == request.EmployeeId &&
            assignment.ContractId == request.ContractId &&
            request.EffectiveStart <= (assignment.EffectiveEnd ?? DateOnly.MaxValue) &&
            requestedEnd >= assignment.EffectiveStart);
        return Task.FromResult(conflict);
    }

    public Task<IReadOnlyList<LaborEmployeeOptionDto>> ListEmployeesAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<LaborEmployeeOptionDto>>(_employees.Where(x => x.TenantId == tenantId).OrderBy(x => x.Name).ToArray());

    private void ReplaceCategory(LaborCategoryDto existing, LaborCategoryDto updated)
    {
        _categories.Remove(existing);
        _categories.Add(updated);
    }

    private void ReplaceAssignment(LaborEmployeeAssignmentDto existing, LaborEmployeeAssignmentDto updated)
    {
        _assignments.Remove(existing);
        _assignments.Add(updated);
    }
}
