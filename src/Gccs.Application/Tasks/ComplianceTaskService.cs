using Gccs.Application.Audit;
using Gccs.Application.Notifications;
using Gccs.Domain.Audit;
using Gccs.Domain.Compliance;

namespace Gccs.Application.Tasks;

public sealed class ComplianceTaskService(
    IComplianceTaskRepository repository,
    IAuditEventWriter auditEventWriter,
    IEnumerable<IAssignmentNotificationRepository> assignmentNotificationRepositories)
{
    private IAssignmentNotificationRepository? AssignmentNotifications => assignmentNotificationRepositories.FirstOrDefault();

    public Task<IReadOnlyList<ComplianceTaskDto>> ListCurrentTenantAsync(CancellationToken cancellationToken = default) =>
        repository.ListCurrentTenantAsync(cancellationToken);

    public Task<ComplianceTaskDto?> FindCurrentTenantAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        repository.FindCurrentTenantAsync(taskId, cancellationToken);

    public async Task<ComplianceTaskDto> CreateAsync(
        CreateComplianceTaskRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        Validate(normalized.Title, normalized.OwnerFunction, normalized.LinkedEntityType, normalized.LinkedEntityId);
        await EnsureValidAssigneeAsync(normalized.AssignedToUserId, cancellationToken);
        var status = ParseStatus(normalized.Status);
        var created = await repository.CreateAsync(normalized, status, actorUserId, cancellationToken) ??
            throw new ComplianceTaskValidationException("Task could not be created for the current tenant.");
        await WriteAuditAsync(created, actorUserId, AuditAction.Created, "Task was created.", null, created.Status, cancellationToken);
        await EmitAssignmentNotificationAsync(created, actorUserId, cancellationToken);
        return created;
    }

    public async Task<ComplianceTaskDto?> UpdateAsync(
        Guid taskId,
        UpdateComplianceTaskRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var before = await repository.FindCurrentTenantAsync(taskId, cancellationToken);
        if (before is null)
        {
            return null;
        }

        var normalized = Normalize(request);
        ValidatePatch(normalized);
        await EnsureValidAssigneeAsync(normalized.AssignedToUserId, cancellationToken);
        ComplianceTaskStatus? parsedStatus = normalized.Status is null ? null : ParseStatus(normalized.Status);
        var updated = await repository.UpdateAsync(taskId, normalized, parsedStatus, actorUserId, cancellationToken);

        if (updated is null)
        {
            return null;
        }

        var summary = before?.Status != updated.Status
            ? $"Task status changed to {updated.Status}."
            : "Task was updated.";
        await WriteAuditAsync(updated, actorUserId, AuditAction.Updated, summary, before?.Status, updated.Status, cancellationToken);
        if (updated.AssignedToUserId.HasValue && before?.AssignedToUserId != updated.AssignedToUserId)
        {
            await EmitAssignmentNotificationAsync(updated, actorUserId, cancellationToken);
        }

        return updated;
    }

    private async Task EmitAssignmentNotificationAsync(
        ComplianceTaskDto task,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (task.AssignedToUserId is not { } assignedUserId || AssignmentNotifications is null)
        {
            return;
        }

        await AssignmentNotifications.EmitTaskAssignmentAsync(
            task.TenantId,
            task.Id,
            assignedUserId,
            task.Title,
            actorUserId,
            queueEmail: true,
            linkUrl: $"/tasks/{task.Id}",
            cancellationToken: cancellationToken);
    }

    private async Task EnsureValidAssigneeAsync(Guid? assignedUserId, CancellationToken cancellationToken)
    {
        if (!assignedUserId.HasValue)
        {
            return;
        }

        if (!await repository.IsActiveCurrentTenantMemberAsync(assignedUserId.Value, cancellationToken))
        {
            throw new ComplianceTaskValidationException("Task assignee must be an active member of the current tenant.");
        }
    }

    private async Task WriteAuditAsync(
        ComplianceTaskDto task,
        Guid actorUserId,
        AuditAction action,
        string summary,
        string? previousStatus,
        string status,
        CancellationToken cancellationToken)
    {
        var metadata = new Dictionary<string, string>
        {
            ["status"] = status,
            ["linkedEntityType"] = task.LinkedEntityType,
            ["linkedEntityId"] = task.LinkedEntityId ?? string.Empty,
            ["priority"] = task.Priority.ToString()
        };

        if (previousStatus is not null)
        {
            metadata["previousStatus"] = previousStatus;
        }

        await auditEventWriter.WriteAsync(
            task.TenantId,
            actorUserId,
            action,
            "ComplianceTask",
            task.Id.ToString(),
            summary,
            metadata,
            cancellationToken);
    }

    private static CreateComplianceTaskRequest Normalize(CreateComplianceTaskRequest request) =>
        request with
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "open" : request.Status.Trim(),
            OwnerFunction = request.OwnerFunction.Trim(),
            LinkedEntityType = request.LinkedEntityType.Trim(),
            LinkedEntityId = string.IsNullOrWhiteSpace(request.LinkedEntityId) ? null : request.LinkedEntityId.Trim()
        };

    private static UpdateComplianceTaskRequest Normalize(UpdateComplianceTaskRequest request) =>
        request with
        {
            Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim(),
            Description = request.Description?.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? null : request.Status.Trim(),
            OwnerFunction = string.IsNullOrWhiteSpace(request.OwnerFunction) ? null : request.OwnerFunction.Trim(),
            LinkedEntityType = string.IsNullOrWhiteSpace(request.LinkedEntityType) ? null : request.LinkedEntityType.Trim(),
            LinkedEntityId = string.IsNullOrWhiteSpace(request.LinkedEntityId) ? null : request.LinkedEntityId.Trim()
        };

    private static void Validate(string title, string ownerFunction, string linkedEntityType, string? linkedEntityId)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ComplianceTaskValidationException("Task title is required.");
        }

        if (string.IsNullOrWhiteSpace(ownerFunction))
        {
            throw new ComplianceTaskValidationException("Task owner is required.");
        }

        if (!AllowedLinkedEntityTypes.Contains(linkedEntityType, StringComparer.OrdinalIgnoreCase))
        {
            throw new ComplianceTaskValidationException("Linked entity type is not supported.");
        }

        if (!string.Equals(linkedEntityType, "general", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(linkedEntityId))
        {
            throw new ComplianceTaskValidationException("Linked entity id is required for linked tasks.");
        }

        if (linkedEntityType.Equals("contract", StringComparison.OrdinalIgnoreCase) ||
            linkedEntityType.Equals("evidence", StringComparison.OrdinalIgnoreCase))
        {
            if (!Guid.TryParse(linkedEntityId, out _))
            {
                throw new ComplianceTaskValidationException($"Linked entity id must be a valid GUID for {linkedEntityType.ToLowerInvariant()} tasks.");
            }
        }
    }

    private static void ValidatePatch(UpdateComplianceTaskRequest request)
    {
        if (request.LinkedEntityType is not null)
        {
            Validate(request.Title ?? "patch", request.OwnerFunction ?? "patch", request.LinkedEntityType, request.LinkedEntityId);
        }
    }

    private static ComplianceTaskStatus ParseStatus(string status)
    {
        return ComplianceTaskStatusCodec.TryParse(status, out var parsed)
            ? parsed
            : throw new ComplianceTaskValidationException("Task status is not supported.");
    }

    private static readonly string[] AllowedLinkedEntityTypes =
    [
        "general",
        "obligation",
        "contract",
        "control",
        "evidence",
        "subcontractor",
        "certification",
        "extraction_regression_review"
    ];
}

public sealed class ComplianceTaskValidationException(string message) : InvalidOperationException(message);

public sealed class ComplianceTaskSearchService(
    IComplianceTaskSearchRepository repository,
    IComplianceTaskCursorCodec cursorCodec,
    Gccs.Application.Security.ICurrentTenantContext tenantContext)
{
    private const int MaximumOffset = 100_000;

    public async Task<ComplianceTaskPageDto> SearchCurrentTenantAsync(
        ComplianceTaskSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Page < 1) throw new ComplianceTaskSearchValidationException("Page must be at least 1.");
        if (query.PageSize is < 1 or > 100) throw new ComplianceTaskSearchValidationException("Page size must be between 1 and 100.");
        var offset = (long)(query.Page - 1) * query.PageSize;
        if (offset > MaximumOffset)
            throw new ComplianceTaskSearchValidationException($"Requested task page exceeds the maximum supported offset of {MaximumOffset} records. Use cursor pagination instead.");
        if (!string.IsNullOrWhiteSpace(query.Cursor) && query.PageWasSpecified)
            throw new ComplianceTaskSearchValidationException("Page and cursor cannot be used together.");
        if (query.DueFrom.HasValue && query.DueTo.HasValue && query.DueFrom > query.DueTo)
            throw new ComplianceTaskSearchValidationException("Due-from date cannot be after due-to date.");

        ComplianceTaskStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!ComplianceTaskStatusCodec.TryParse(query.Status, out var parsed))
                throw new ComplianceTaskSearchValidationException("Task status is not supported.");
            status = parsed;
        }

        var fingerprint = BuildFilterFingerprint(status, query);
        ComplianceTaskCursor? cursor = null;
        if (!string.IsNullOrWhiteSpace(query.Cursor) &&
            !cursorCodec.TryUnprotect(query.Cursor, tenantContext.TenantId, fingerprint, out cursor))
        {
            throw new ComplianceTaskSearchValidationException("Task search cursor is invalid or does not match the current tenant and filters.");
        }

        var page = await repository.SearchCurrentTenantAsync(status, query, cursor, cancellationToken);
        if (!page.HasMore || page.Items.Count == 0)
        {
            return page;
        }

        var last = page.Items[^1];
        return page with
        {
            NextCursor = cursorCodec.Protect(
                tenantContext.TenantId,
                fingerprint,
                new ComplianceTaskCursor(last.DueAt, last.CreatedAt, last.Id))
        };
    }

    private static string BuildFilterFingerprint(ComplianceTaskStatus? status, ComplianceTaskSearchQuery query) =>
        string.Join('|',
            status.HasValue ? ComplianceTaskStatusCodec.Format(status.Value) : string.Empty,
            query.OwnerUserId?.ToString("D") ?? string.Empty,
            query.DueFrom?.ToString("yyyy-MM-dd") ?? string.Empty,
            query.DueTo?.ToString("yyyy-MM-dd") ?? string.Empty,
            query.PageSize.ToString(System.Globalization.CultureInfo.InvariantCulture));
}

public sealed class ComplianceTaskSearchValidationException(string message) : InvalidOperationException(message);
