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

    public async Task<ComplianceTaskDto> CreateAsync(
        CreateComplianceTaskRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        Validate(normalized.Title, normalized.OwnerFunction, normalized.LinkedEntityType, normalized.LinkedEntityId);
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
        var before = (await repository.ListCurrentTenantAsync(cancellationToken)).FirstOrDefault(task => task.Id == taskId);
        var normalized = Normalize(request);
        ValidatePatch(normalized);
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
            cancellationToken);
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
        var normalized = status.Trim().Replace("-", "_", StringComparison.OrdinalIgnoreCase).ToLowerInvariant();
        return normalized switch
        {
            "open" => ComplianceTaskStatus.Open,
            "in_progress" => ComplianceTaskStatus.InProgress,
            "blocked" => ComplianceTaskStatus.Blocked,
            "waiting_for_review" => ComplianceTaskStatus.WaitingForReview,
            "completed" or "complete" or "done" => ComplianceTaskStatus.Done,
            "canceled" or "cancelled" => ComplianceTaskStatus.Canceled,
            _ => throw new ComplianceTaskValidationException("Task status is not supported.")
        };
    }

    private static readonly string[] AllowedLinkedEntityTypes =
    [
        "general",
        "obligation",
        "contract",
        "control",
        "evidence",
        "subcontractor",
        "certification"
    ];
}

public sealed class ComplianceTaskValidationException(string message) : InvalidOperationException(message);
