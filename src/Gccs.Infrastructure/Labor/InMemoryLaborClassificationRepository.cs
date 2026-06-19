using Gccs.Application.Labor;

namespace Gccs.Infrastructure.Labor;

public sealed class InMemoryLaborClassificationRepository : ILaborClassificationRepository
{
    private readonly List<LaborCategoryDto> _categories = [];
    private readonly List<LaborEmployeeAssignmentDto> _assignments = [];

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
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = _categories.SingleOrDefault(category => category.Id == categoryId);
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
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = _categories.SingleOrDefault(category => category.Id == categoryId);
        if (existing is null)
        {
            return Task.FromResult<LaborCategoryDto?>(null);
        }

        var updated = existing with { IsActive = isActive, UpdatedAt = DateTimeOffset.UtcNow };
        ReplaceCategory(existing, updated);
        return Task.FromResult<LaborCategoryDto?>(updated);
    }

    public Task<LaborCategoryDto?> FindCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_categories.SingleOrDefault(category => category.Id == categoryId));

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

    public Task<LaborEmployeeAssignmentDto> CreateAssignmentAsync(
        LaborEmployeeAssignmentRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var category = _categories.Single(candidate => candidate.Id == request.CategoryId);
        var assignment = new LaborEmployeeAssignmentDto(
            Guid.NewGuid(),
            tenantId,
            request.EmployeeId,
            request.EmployeeName,
            request.EmployeeEmail,
            request.ContractId,
            request.CategoryId,
            category.Title,
            request.WorkLocation,
            request.EffectiveStart,
            request.EffectiveEnd,
            LaborAssignmentStatus.Active,
            request.SourceReference ?? string.Empty,
            [],
            DateTimeOffset.UtcNow,
            null);
        _assignments.Add(assignment);
        return Task.FromResult(assignment);
    }

    public Task<LaborEmployeeAssignmentDto?> UpdateAssignmentAsync(
        Guid assignmentId,
        LaborEmployeeAssignmentRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = _assignments.SingleOrDefault(assignment => assignment.Id == assignmentId);
        if (existing is null)
        {
            return Task.FromResult<LaborEmployeeAssignmentDto?>(null);
        }

        var category = _categories.Single(candidate => candidate.Id == request.CategoryId);
        var updated = existing with
        {
            EmployeeId = request.EmployeeId,
            EmployeeName = request.EmployeeName,
            EmployeeEmail = request.EmployeeEmail,
            ContractId = request.ContractId,
            CategoryId = request.CategoryId,
            LaborCategoryTitle = category.Title,
            WorkLocation = request.WorkLocation,
            EffectiveStart = request.EffectiveStart,
            EffectiveEnd = request.EffectiveEnd,
            SourceReference = request.SourceReference ?? string.Empty,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        ReplaceAssignment(existing, updated);
        return Task.FromResult<LaborEmployeeAssignmentDto?>(updated);
    }

    public Task<LaborEmployeeAssignmentDto?> SetAssignmentStatusAsync(
        Guid assignmentId,
        LaborAssignmentStatus status,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = _assignments.SingleOrDefault(assignment => assignment.Id == assignmentId);
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
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = _assignments.SingleOrDefault(assignment => assignment.Id == assignmentId);
        var newCategory = _categories.SingleOrDefault(category => category.Id == newCategoryId);
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
            UpdatedAt = DateTimeOffset.UtcNow
        };
        ReplaceAssignment(existing, updated);
        return Task.FromResult<LaborEmployeeAssignmentDto?>(updated);
    }

    public Task<LaborEmployeeAssignmentDto?> FindAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_assignments.SingleOrDefault(assignment => assignment.Id == assignmentId));

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
