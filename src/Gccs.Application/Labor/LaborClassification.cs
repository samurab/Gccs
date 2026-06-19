using Gccs.Application.Audit;
using Gccs.Domain.Audit;

namespace Gccs.Application.Labor;

public sealed class LaborClassificationService(
    ILaborClassificationRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public async Task<LaborCategoryDto> CreateCategoryAsync(
        LaborCategoryRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        ValidateCategory(normalized);
        var category = await repository.CreateCategoryAsync(normalized, tenantId, actorUserId, cancellationToken);
        await WriteCategoryAuditAsync(category, actorUserId, AuditAction.Created, "Labor category was created.", cancellationToken);
        return category;
    }

    public async Task<LaborCategoryDto?> UpdateCategoryAsync(
        Guid categoryId,
        LaborCategoryRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        ValidateCategory(normalized);
        var category = await repository.UpdateCategoryAsync(categoryId, normalized, actorUserId, cancellationToken);
        if (category is not null)
        {
            await WriteCategoryAuditAsync(category, actorUserId, AuditAction.Updated, "Labor category was updated.", cancellationToken);
        }

        return category;
    }

    public async Task<LaborCategoryDto?> DeactivateCategoryAsync(
        Guid categoryId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var category = await repository.SetCategoryActiveAsync(categoryId, isActive: false, actorUserId, cancellationToken);
        if (category is not null)
        {
            await WriteCategoryAuditAsync(category, actorUserId, AuditAction.Updated, "Labor category was deactivated.", cancellationToken);
        }

        return category;
    }

    public async Task<LaborEmployeeAssignmentDto> CreateAssignmentAsync(
        LaborEmployeeAssignmentRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        await ValidateAssignmentAsync(normalized, tenantId, null, cancellationToken);
        var assignment = await repository.CreateAssignmentAsync(normalized, tenantId, actorUserId, cancellationToken);
        await WriteAssignmentAuditAsync(assignment, actorUserId, AuditAction.Created, "Labor employee assignment was created.", cancellationToken);
        return assignment;
    }

    public async Task<LaborEmployeeAssignmentDto?> UpdateAssignmentAsync(
        Guid assignmentId,
        LaborEmployeeAssignmentRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = await repository.FindAssignmentAsync(assignmentId, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var normalized = Normalize(request);
        await ValidateAssignmentAsync(normalized, existing.TenantId, assignmentId, cancellationToken);
        var assignment = await repository.UpdateAssignmentAsync(assignmentId, normalized, actorUserId, cancellationToken);
        if (assignment is not null)
        {
            await WriteAssignmentAuditAsync(assignment, actorUserId, AuditAction.Updated, "Labor employee assignment was updated.", cancellationToken);
        }

        return assignment;
    }

    public async Task<LaborEmployeeAssignmentDto?> DeactivateAssignmentAsync(
        Guid assignmentId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var assignment = await repository.SetAssignmentStatusAsync(assignmentId, LaborAssignmentStatus.Inactive, actorUserId, cancellationToken);
        if (assignment is not null)
        {
            await WriteAssignmentAuditAsync(assignment, actorUserId, AuditAction.Updated, "Labor employee assignment was deactivated.", cancellationToken);
        }

        return assignment;
    }

    public async Task<LaborEmployeeAssignmentDto?> ReclassifyAsync(
        Guid assignmentId,
        Guid newCategoryId,
        string reason,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new LaborClassificationValidationException("Reclassification reason is required.");
        }

        var assignment = await repository.ReclassifyAsync(assignmentId, newCategoryId, reason.Trim(), actorUserId, cancellationToken);
        if (assignment is not null)
        {
            await WriteAssignmentAuditAsync(assignment, actorUserId, AuditAction.Updated, "Labor employee assignment was reclassified.", cancellationToken);
        }

        return assignment;
    }

    public async Task<LaborEmployeeAssignmentViewDto?> ViewAssignmentAsync(
        Guid assignmentId,
        bool canViewSensitiveEmployeeData,
        CancellationToken cancellationToken = default)
    {
        var assignment = await repository.FindAssignmentAsync(assignmentId, cancellationToken);
        if (assignment is null)
        {
            return null;
        }

        return new LaborEmployeeAssignmentViewDto(
            assignment.Id,
            assignment.TenantId,
            assignment.ContractId,
            assignment.EmployeeId,
            canViewSensitiveEmployeeData ? assignment.EmployeeName : null,
            canViewSensitiveEmployeeData ? assignment.EmployeeEmail : null,
            assignment.CategoryId,
            assignment.LaborCategoryTitle,
            assignment.WorkLocation,
            assignment.EffectiveStart,
            assignment.EffectiveEnd,
            assignment.Status,
            assignment.SourceReference,
            assignment.History);
    }

    private async Task ValidateAssignmentAsync(
        LaborEmployeeAssignmentRequest request,
        Guid tenantId,
        Guid? existingAssignmentId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SourceReference))
        {
            throw new LaborClassificationValidationException("Source reference is required.");
        }

        if (request.EmployeeId == Guid.Empty || request.ContractId == Guid.Empty || request.CategoryId == Guid.Empty)
        {
            throw new LaborClassificationValidationException("Employee, contract, and labor category are required.");
        }

        if (request.EffectiveEnd.HasValue && request.EffectiveEnd < request.EffectiveStart)
        {
            throw new LaborClassificationValidationException("Assignment end date cannot be before start date.");
        }

        var category = await repository.FindCategoryAsync(request.CategoryId, cancellationToken);
        if (category is null || category.TenantId != tenantId)
        {
            throw new LaborClassificationValidationException("Labor category was not found for the current tenant.");
        }

        if (!category.IsActive)
        {
            throw new LaborClassificationValidationException("Inactive labor categories cannot be assigned.");
        }

        if (await repository.HasDateConflictAsync(tenantId, request, existingAssignmentId, cancellationToken))
        {
            throw new LaborClassificationValidationException("Assignment effective dates conflict with an existing assignment.");
        }
    }

    private static LaborCategoryRequest Normalize(LaborCategoryRequest request) =>
        request with
        {
            Title = request.Title.Trim(),
            WageDeterminationClassification = request.WageDeterminationClassification.Trim(),
            FringeDescription = request.FringeDescription.Trim(),
            SourceReference = string.IsNullOrWhiteSpace(request.SourceReference) ? null : request.SourceReference.Trim()
        };

    private static LaborEmployeeAssignmentRequest Normalize(LaborEmployeeAssignmentRequest request) =>
        request with
        {
            EmployeeName = request.EmployeeName.Trim(),
            EmployeeEmail = request.EmployeeEmail.Trim(),
            WorkLocation = request.WorkLocation.Trim(),
            SourceReference = string.IsNullOrWhiteSpace(request.SourceReference) ? null : request.SourceReference.Trim()
        };

    private static void ValidateCategory(LaborCategoryRequest request)
    {
        if (request.ContractId == Guid.Empty)
        {
            throw new LaborClassificationValidationException("Contract is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.WageDeterminationClassification))
        {
            throw new LaborClassificationValidationException("Labor category title and wage determination classification are required.");
        }

        if (request.HourlyWage < 0 || request.FringeRate < 0)
        {
            throw new LaborClassificationValidationException("Wage and fringe rates cannot be negative.");
        }

        if (request.EffectiveEnd.HasValue && request.EffectiveEnd < request.EffectiveStart)
        {
            throw new LaborClassificationValidationException("Labor category end date cannot be before start date.");
        }

        if (string.IsNullOrWhiteSpace(request.SourceReference))
        {
            throw new LaborClassificationValidationException("Source reference is required.");
        }
    }

    private async Task WriteCategoryAuditAsync(
        LaborCategoryDto category,
        Guid actorUserId,
        AuditAction action,
        string summary,
        CancellationToken cancellationToken)
    {
        await auditEventWriter.WriteAsync(
            category.TenantId,
            actorUserId,
            action,
            "LaborCategory",
            category.Id.ToString(),
            summary,
            new Dictionary<string, string>
            {
                ["contractId"] = category.ContractId.ToString(),
                ["title"] = category.Title,
                ["isActive"] = category.IsActive.ToString()
            },
            cancellationToken);
    }

    private async Task WriteAssignmentAuditAsync(
        LaborEmployeeAssignmentDto assignment,
        Guid actorUserId,
        AuditAction action,
        string summary,
        CancellationToken cancellationToken)
    {
        await auditEventWriter.WriteAsync(
            assignment.TenantId,
            actorUserId,
            action,
            "LaborEmployeeAssignment",
            assignment.Id.ToString(),
            summary,
            new Dictionary<string, string>
            {
                ["contractId"] = assignment.ContractId.ToString(),
                ["employeeId"] = assignment.EmployeeId.ToString(),
                ["categoryId"] = assignment.CategoryId.ToString(),
                ["status"] = assignment.Status.ToString(),
                ["historyCount"] = assignment.History.Count.ToString()
            },
            cancellationToken);
    }
}

public interface ILaborClassificationRepository
{
    Task<LaborCategoryDto> CreateCategoryAsync(LaborCategoryRequest request, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<LaborCategoryDto?> UpdateCategoryAsync(Guid categoryId, LaborCategoryRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<LaborCategoryDto?> SetCategoryActiveAsync(Guid categoryId, bool isActive, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<LaborCategoryDto?> FindCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LaborCategoryDto>> ListCategoriesAsync(Guid tenantId, Guid? contractId = null, CancellationToken cancellationToken = default);
    Task<LaborEmployeeAssignmentDto> CreateAssignmentAsync(LaborEmployeeAssignmentRequest request, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<LaborEmployeeAssignmentDto?> UpdateAssignmentAsync(Guid assignmentId, LaborEmployeeAssignmentRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<LaborEmployeeAssignmentDto?> SetAssignmentStatusAsync(Guid assignmentId, LaborAssignmentStatus status, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<LaborEmployeeAssignmentDto?> ReclassifyAsync(Guid assignmentId, Guid newCategoryId, string reason, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<LaborEmployeeAssignmentDto?> FindAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LaborEmployeeAssignmentDto>> ListAssignmentsAsync(Guid tenantId, Guid? contractId = null, CancellationToken cancellationToken = default);
    Task<bool> HasDateConflictAsync(Guid tenantId, LaborEmployeeAssignmentRequest request, Guid? existingAssignmentId, CancellationToken cancellationToken = default);
}

public sealed record LaborCategoryRequest(
    Guid ContractId,
    string Title,
    string WageDeterminationClassification,
    decimal HourlyWage,
    decimal FringeRate,
    string FringeDescription,
    DateOnly EffectiveStart,
    DateOnly? EffectiveEnd,
    string? SourceReference);

public sealed record LaborCategoryDto(
    Guid Id,
    Guid TenantId,
    Guid ContractId,
    string Title,
    string WageDeterminationClassification,
    decimal HourlyWage,
    decimal FringeRate,
    string FringeDescription,
    DateOnly EffectiveStart,
    DateOnly? EffectiveEnd,
    string SourceReference,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record LaborEmployeeAssignmentRequest(
    Guid EmployeeId,
    string EmployeeName,
    string EmployeeEmail,
    Guid ContractId,
    Guid CategoryId,
    string WorkLocation,
    DateOnly EffectiveStart,
    DateOnly? EffectiveEnd,
    string? SourceReference);

public sealed record LaborEmployeeAssignmentDto(
    Guid Id,
    Guid TenantId,
    Guid EmployeeId,
    string EmployeeName,
    string EmployeeEmail,
    Guid ContractId,
    Guid CategoryId,
    string LaborCategoryTitle,
    string WorkLocation,
    DateOnly EffectiveStart,
    DateOnly? EffectiveEnd,
    LaborAssignmentStatus Status,
    string SourceReference,
    IReadOnlyList<LaborClassificationHistoryDto> History,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record LaborEmployeeAssignmentViewDto(
    Guid Id,
    Guid TenantId,
    Guid ContractId,
    Guid EmployeeId,
    string? EmployeeName,
    string? EmployeeEmail,
    Guid CategoryId,
    string LaborCategoryTitle,
    string WorkLocation,
    DateOnly EffectiveStart,
    DateOnly? EffectiveEnd,
    LaborAssignmentStatus Status,
    string SourceReference,
    IReadOnlyList<LaborClassificationHistoryDto> History);

public sealed record LaborClassificationHistoryDto(
    Guid Id,
    Guid AssignmentId,
    Guid? PriorCategoryId,
    string? PriorCategoryTitle,
    Guid NewCategoryId,
    string NewCategoryTitle,
    Guid ActorUserId,
    DateTimeOffset ChangedAt,
    string Reason);

public enum LaborAssignmentStatus
{
    Active,
    Inactive
}

public sealed class LaborClassificationValidationException(string message) : InvalidOperationException(message);
