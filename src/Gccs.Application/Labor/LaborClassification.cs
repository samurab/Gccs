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
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        ValidateCategory(normalized);
        var existing = await repository.FindCategoryAsync(categoryId, tenantId, cancellationToken);
        if (existing is null || existing.ContractId != normalized.ContractId)
        {
            return null;
        }
        if (await repository.WouldInvalidateAssignmentsAsync(categoryId, normalized, tenantId, cancellationToken))
        {
            throw new LaborClassificationValidationException("The category contract or effective dates would invalidate an active employee assignment.");
        }
        var category = await repository.UpdateCategoryAsync(categoryId, normalized, tenantId, actorUserId, cancellationToken);
        if (category is not null)
        {
            await WriteCategoryAuditAsync(category, actorUserId, AuditAction.Updated, "Labor category was updated.", cancellationToken);
        }

        return category;
    }

    public async Task<LaborCategoryDto?> DeactivateCategoryAsync(
        Guid categoryId,
        Guid contractId,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = await repository.FindCategoryAsync(categoryId, tenantId, cancellationToken);
        if (existing is null || existing.ContractId != contractId)
        {
            return null;
        }

        if (await repository.HasActiveAssignmentsAsync(categoryId, tenantId, cancellationToken))
        {
            throw new LaborClassificationValidationException("A labor category with active employee assignments cannot be deactivated.");
        }

        var category = await repository.SetCategoryActiveAsync(categoryId, isActive: false, tenantId, actorUserId, cancellationToken);
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
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = await repository.FindAssignmentAsync(assignmentId, tenantId, cancellationToken);
        if (existing is null)
        {
            return null;
        }
        if (existing.ContractId != request.ContractId)
        {
            return null;
        }

        var normalized = Normalize(request);
        await ValidateAssignmentAsync(normalized, existing.TenantId, assignmentId, cancellationToken);
        var assignment = await repository.UpdateAssignmentAsync(assignmentId, normalized, tenantId, actorUserId, cancellationToken);
        if (assignment is not null)
        {
            await WriteAssignmentAuditAsync(assignment, actorUserId, AuditAction.Updated, "Labor employee assignment was updated.", cancellationToken);
        }

        return assignment;
    }

    public async Task<LaborEmployeeAssignmentDto?> DeactivateAssignmentAsync(
        Guid assignmentId,
        Guid contractId,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = await repository.FindAssignmentAsync(assignmentId, tenantId, cancellationToken);
        if (existing is null || existing.ContractId != contractId)
        {
            return null;
        }

        var assignment = await repository.SetAssignmentStatusAsync(assignmentId, LaborAssignmentStatus.Inactive, tenantId, actorUserId, cancellationToken);
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
        Guid contractId,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new LaborClassificationValidationException("Reclassification reason is required.");
        }

        var existing = await repository.FindAssignmentAsync(assignmentId, tenantId, cancellationToken);
        if (existing is null || existing.ContractId != contractId)
        {
            return null;
        }

        await ValidateAssignmentAsync(
            new LaborEmployeeAssignmentRequest(
                existing.EmployeeId,
                existing.ContractId,
                newCategoryId,
                existing.WorkLocation,
                existing.EffectiveStart,
                existing.EffectiveEnd,
                existing.SourceReference,
                existing.EvidenceItemIds),
            tenantId,
            assignmentId,
            cancellationToken);

        var assignment = await repository.ReclassifyAsync(assignmentId, newCategoryId, reason.Trim(), tenantId, actorUserId, cancellationToken);
        if (assignment is not null)
        {
            await WriteAssignmentAuditAsync(assignment, actorUserId, AuditAction.Updated, "Labor employee assignment was reclassified.", cancellationToken);
        }

        return assignment;
    }

    public async Task<LaborEmployeeAssignmentDto?> ReviewAssignmentAsync(
        Guid assignmentId,
        LaborClassificationReviewRequest request,
        Guid contractId,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (request.Status is not (LaborClassificationReviewStatus.Reviewed or LaborClassificationReviewStatus.Rejected) ||
            string.IsNullOrWhiteSpace(request.Notes))
        {
            throw new LaborClassificationValidationException("A reviewed or rejected classification requires review notes.");
        }

        var existing = await repository.FindAssignmentAsync(assignmentId, tenantId, cancellationToken);
        if (existing is null || existing.ContractId != contractId)
        {
            return null;
        }

        var assignment = await repository.ReviewAssignmentAsync(
            assignmentId, request.Status, request.Notes.Trim(), tenantId, actorUserId, cancellationToken);
        if (assignment is not null)
        {
            await WriteAssignmentAuditAsync(assignment, actorUserId, AuditAction.Updated, "Labor employee classification review was recorded.", cancellationToken);
        }

        return assignment;
    }

    public async Task<LaborEmployeeAssignmentViewDto?> ViewAssignmentAsync(
        Guid assignmentId,
        Guid tenantId,
        bool canViewSensitiveEmployeeData,
        CancellationToken cancellationToken = default)
    {
        var assignment = await repository.FindAssignmentAsync(assignmentId, tenantId, cancellationToken);
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
            assignment.EvidenceItemIds,
            assignment.History,
            assignment.ReviewStatus,
            assignment.ReviewNotes,
            assignment.ReviewedByUserId,
            assignment.ReviewedAt);
    }

    public Task<IReadOnlyList<LaborCategoryDto>> ListCategoriesAsync(
        Guid tenantId, Guid? contractId = null, CancellationToken cancellationToken = default) =>
        repository.ListCategoriesAsync(tenantId, contractId, cancellationToken);

    public async Task<IReadOnlyList<LaborEmployeeAssignmentViewDto>> ListAssignmentsAsync(
        Guid tenantId, Guid? contractId, bool canViewSensitiveEmployeeData, CancellationToken cancellationToken = default) =>
        (await repository.ListAssignmentsAsync(tenantId, contractId, cancellationToken))
            .Select(assignment => new LaborEmployeeAssignmentViewDto(
                assignment.Id, assignment.TenantId, assignment.ContractId, assignment.EmployeeId,
                canViewSensitiveEmployeeData ? assignment.EmployeeName : null,
                canViewSensitiveEmployeeData ? assignment.EmployeeEmail : null,
                assignment.CategoryId, assignment.LaborCategoryTitle, assignment.WorkLocation,
                assignment.EffectiveStart, assignment.EffectiveEnd, assignment.Status,
                assignment.SourceReference, assignment.EvidenceItemIds, assignment.History,
                assignment.ReviewStatus, assignment.ReviewNotes, assignment.ReviewedByUserId, assignment.ReviewedAt))
            .ToArray();

    public Task<IReadOnlyList<LaborEmployeeOptionDto>> ListEmployeesAsync(
        Guid tenantId, CancellationToken cancellationToken = default) =>
        repository.ListEmployeesAsync(tenantId, cancellationToken);

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

        if (string.IsNullOrWhiteSpace(request.WorkLocation))
        {
            throw new LaborClassificationValidationException("Work location is required.");
        }

        if (request.WorkLocation.Length > 240 || request.SourceReference?.Length > 500)
        {
            throw new LaborClassificationValidationException("Labor assignment text exceeds the supported length.");
        }

        if (request.EffectiveEnd.HasValue && request.EffectiveEnd < request.EffectiveStart)
        {
            throw new LaborClassificationValidationException("Assignment end date cannot be before start date.");
        }

        var category = await repository.FindCategoryAsync(request.CategoryId, tenantId, cancellationToken);
        if (category is null)
        {
            throw new LaborClassificationValidationException("Labor category was not found for the current tenant.");
        }

        if (!category.IsActive)
        {
            throw new LaborClassificationValidationException("Inactive labor categories cannot be assigned.");
        }

        if (category.ContractId != request.ContractId)
        {
            throw new LaborClassificationValidationException("The labor category does not belong to the assignment contract.");
        }

        var assignmentEnd = request.EffectiveEnd ?? DateOnly.MaxValue;
        var categoryEnd = category.EffectiveEnd ?? DateOnly.MaxValue;
        if (request.EffectiveStart < category.EffectiveStart || assignmentEnd > categoryEnd)
        {
            throw new LaborClassificationValidationException("Assignment effective dates must fall within the labor category effective dates.");
        }

        if (await repository.HasDateConflictAsync(tenantId, request, existingAssignmentId, cancellationToken))
        {
            throw new LaborClassificationValidationException("Assignment effective dates conflict with an existing assignment.");
        }
    }

    private static LaborCategoryRequest Normalize(LaborCategoryRequest request) =>
        request with
        {
            Title = request.Title?.Trim() ?? string.Empty,
            WageDeterminationClassification = request.WageDeterminationClassification?.Trim() ?? string.Empty,
            FringeDescription = request.FringeDescription?.Trim() ?? string.Empty,
            SourceReference = string.IsNullOrWhiteSpace(request.SourceReference) ? null : request.SourceReference.Trim()
        };

    private static LaborEmployeeAssignmentRequest Normalize(LaborEmployeeAssignmentRequest request) =>
        request with
        {
            WorkLocation = request.WorkLocation?.Trim() ?? string.Empty,
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

        if (request.Title.Length > 240 || request.WageDeterminationClassification.Length > 240 ||
            request.FringeDescription.Length > 1_000 || request.SourceReference?.Length > 500)
        {
            throw new LaborClassificationValidationException("Labor category text exceeds the supported length.");
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
                ["reviewStatus"] = assignment.ReviewStatus.ToString(),
                ["historyCount"] = assignment.History.Count.ToString()
            },
            cancellationToken);
    }
}

public interface ILaborClassificationRepository
{
    Task<LaborCategoryDto> CreateCategoryAsync(LaborCategoryRequest request, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<LaborCategoryDto?> UpdateCategoryAsync(Guid categoryId, LaborCategoryRequest request, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<LaborCategoryDto?> SetCategoryActiveAsync(Guid categoryId, bool isActive, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<LaborCategoryDto?> FindCategoryAsync(Guid categoryId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LaborCategoryDto>> ListCategoriesAsync(Guid tenantId, Guid? contractId = null, CancellationToken cancellationToken = default);
    Task<bool> HasActiveAssignmentsAsync(Guid categoryId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> WouldInvalidateAssignmentsAsync(Guid categoryId, LaborCategoryRequest request, Guid tenantId, CancellationToken cancellationToken = default);
    Task<LaborEmployeeAssignmentDto> CreateAssignmentAsync(LaborEmployeeAssignmentRequest request, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<LaborEmployeeAssignmentDto?> UpdateAssignmentAsync(Guid assignmentId, LaborEmployeeAssignmentRequest request, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<LaborEmployeeAssignmentDto?> SetAssignmentStatusAsync(Guid assignmentId, LaborAssignmentStatus status, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<LaborEmployeeAssignmentDto?> ReclassifyAsync(Guid assignmentId, Guid newCategoryId, string reason, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<LaborEmployeeAssignmentDto?> ReviewAssignmentAsync(Guid assignmentId, LaborClassificationReviewStatus status, string notes, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<LaborEmployeeAssignmentDto?> FindAssignmentAsync(Guid assignmentId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LaborEmployeeAssignmentDto>> ListAssignmentsAsync(Guid tenantId, Guid? contractId = null, CancellationToken cancellationToken = default);
    Task<bool> HasDateConflictAsync(Guid tenantId, LaborEmployeeAssignmentRequest request, Guid? existingAssignmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LaborEmployeeOptionDto>> ListEmployeesAsync(Guid tenantId, CancellationToken cancellationToken = default);
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
    Guid ContractId,
    Guid CategoryId,
    string WorkLocation,
    DateOnly EffectiveStart,
    DateOnly? EffectiveEnd,
    string? SourceReference,
    IReadOnlyList<Guid>? EvidenceItemIds = null);

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
    IReadOnlyList<Guid> EvidenceItemIds,
    IReadOnlyList<LaborClassificationHistoryDto> History,
    LaborClassificationReviewStatus ReviewStatus,
    string? ReviewNotes,
    Guid? ReviewedByUserId,
    DateTimeOffset? ReviewedAt,
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
    IReadOnlyList<Guid> EvidenceItemIds,
    IReadOnlyList<LaborClassificationHistoryDto> History,
    LaborClassificationReviewStatus ReviewStatus,
    string? ReviewNotes,
    Guid? ReviewedByUserId,
    DateTimeOffset? ReviewedAt);

public sealed record LaborEmployeeOptionDto(Guid Id, Guid TenantId, string EmployeeNumber, string Name, string Email);

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

public enum LaborClassificationReviewStatus
{
    PendingReview,
    Reviewed,
    Rejected
}

public sealed record LaborReclassificationRequest(Guid NewCategoryId, string Reason);
public sealed record LaborClassificationReviewRequest(LaborClassificationReviewStatus Status, string Notes);

public sealed class LaborClassificationValidationException(string message) : InvalidOperationException(message);

public sealed class LaborClassificationConflictException : InvalidOperationException
{
    public LaborClassificationConflictException() : base("The labor assignment changed concurrently. Reload and retry.") { }
}
