using Gccs.Application.Audit;
using Gccs.Domain.Audit;

namespace Gccs.Application.Tenancy;

public sealed class CuiReadyApprovalChecklistService(
    ICuiReadyApprovalChecklistRepository repository,
    IAuditEventWriter auditEventWriter) : ICuiReadyApprovalChecklistGate
{
    public async Task<CuiReadyApprovalChecklistDto> CreateAsync(Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var checklist = await repository.CreateAsync(tenantId, actorUserId, DefaultItems(), cancellationToken);
        await WriteAuditAsync(checklist, actorUserId, AuditAction.Created, "created", cancellationToken);
        return checklist;
    }

    public Task<IReadOnlyList<CuiReadyApprovalChecklistDto>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        repository.ListAsync(tenantId, cancellationToken);

    public async Task<CuiReadyApprovalChecklistDto?> UpdateItemAsync(
        Guid tenantId,
        Guid checklistId,
        string itemKey,
        UpdateCuiReadyChecklistItemRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateCompletedItem(request);
        var checklist = await repository.UpdateItemAsync(tenantId, checklistId, itemKey, request, actorUserId, cancellationToken);
        if (checklist is not null)
        {
            await WriteAuditAsync(checklist, actorUserId, AuditAction.Updated, "updated", cancellationToken);
        }

        return checklist;
    }

    public async Task<CuiReadyApprovalChecklistDto?> SubmitForReviewAsync(
        Guid tenantId,
        Guid checklistId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var checklist = await repository.SetStateAsync(tenantId, checklistId, CuiReadyChecklistState.InReview, actorUserId, null, cancellationToken);
        if (checklist is not null)
        {
            await WriteAuditAsync(checklist, actorUserId, AuditAction.Updated, "submitted", cancellationToken);
        }

        return checklist;
    }

    public async Task<CuiReadyApprovalChecklistDto?> ApproveAsync(
        Guid tenantId,
        Guid checklistId,
        ReviewCuiReadyChecklistRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var current = await repository.FindAsync(tenantId, checklistId, cancellationToken);
        if (current is null)
        {
            return null;
        }

        var incomplete = current.Items.Where(item => item.IsRequired && item.Status is not CuiReadyChecklistItemStatus.Complete).ToArray();
        if (incomplete.Length > 0)
        {
            throw new CuiReadyApprovalChecklistValidationException("Checklist cannot be approved while required items are incomplete.");
        }

        var checklist = await repository.SetStateAsync(tenantId, checklistId, CuiReadyChecklistState.Approved, actorUserId, request.Reason, cancellationToken);
        if (checklist is not null)
        {
            await WriteAuditAsync(checklist, actorUserId, AuditAction.Approved, "approved", cancellationToken);
        }

        return checklist;
    }

    public async Task<CuiReadyApprovalChecklistDto?> RejectAsync(
        Guid tenantId,
        Guid checklistId,
        ReviewCuiReadyChecklistRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new CuiReadyApprovalChecklistValidationException("Checklist rejection reason is required.");
        }

        var checklist = await repository.SetStateAsync(tenantId, checklistId, CuiReadyChecklistState.Rejected, actorUserId, request.Reason.Trim(), cancellationToken);
        if (checklist is not null)
        {
            await WriteAuditAsync(checklist, actorUserId, AuditAction.Rejected, "rejected", cancellationToken);
        }

        return checklist;
    }

    public async Task<CuiReadyApprovalChecklistDto?> SupersedeAsync(
        Guid tenantId,
        Guid checklistId,
        ReviewCuiReadyChecklistRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var checklist = await repository.SetStateAsync(tenantId, checklistId, CuiReadyChecklistState.Superseded, actorUserId, request.Reason, cancellationToken);
        if (checklist is not null)
        {
            await WriteAuditAsync(checklist, actorUserId, AuditAction.Archived, "superseded", cancellationToken);
        }

        return checklist;
    }

    public async Task EnsureApprovedChecklistAsync(Guid tenantId, string approvalRecordReference, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(approvalRecordReference, out var checklistId))
        {
            throw new CuiReadyApprovalChecklistValidationException("CuiReady mode requires an approved checklist ID.");
        }

        var checklist = await repository.FindAsync(tenantId, checklistId, cancellationToken);
        if (checklist is null || checklist.State is not CuiReadyChecklistState.Approved)
        {
            throw new CuiReadyApprovalChecklistValidationException("CuiReady mode requires an approved checklist linked to the current tenant.");
        }
    }

    private static void ValidateCompletedItem(UpdateCuiReadyChecklistItemRequest request)
    {
        if (request.Status is not CuiReadyChecklistItemStatus.Complete)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(request.Owner))
        {
            throw new CuiReadyApprovalChecklistValidationException("Completed checklist items require an owner.");
        }

        if (request.ReviewerUserId is null)
        {
            throw new CuiReadyApprovalChecklistValidationException("Completed checklist items require a reviewer.");
        }

        if (request.ReviewedAt is null)
        {
            throw new CuiReadyApprovalChecklistValidationException("Completed checklist items require a review date.");
        }

        if (string.IsNullOrWhiteSpace(request.Notes) && string.IsNullOrWhiteSpace(request.EvidenceLink))
        {
            throw new CuiReadyApprovalChecklistValidationException("Completed checklist items require a supporting note or evidence link.");
        }
    }

    private Task WriteAuditAsync(
        CuiReadyApprovalChecklistDto checklist,
        Guid actorUserId,
        AuditAction action,
        string lifecycleAction,
        CancellationToken cancellationToken) =>
        auditEventWriter.WriteAsync(
            checklist.TenantId,
            actorUserId,
            action,
            "CuiReadyApprovalChecklist",
            checklist.Id.ToString(),
            $"CUI-ready approval checklist {lifecycleAction}.",
            new Dictionary<string, string>
            {
                ["tenantId"] = checklist.TenantId.ToString(),
                ["checklistId"] = checklist.Id.ToString(),
                ["state"] = checklist.State.ToString(),
                ["lifecycleAction"] = lifecycleAction
            },
            cancellationToken);

    private static IReadOnlyList<CreateCuiReadyChecklistItem> DefaultItems() =>
    [
        new("customer-agreement", "Customer agreement", "Customer agreement accepts CUI-ready service terms.", true),
        new("data-handling-notice", "Data handling notice", "Customer-facing CUI data handling notice is approved.", true),
        new("shared-responsibility-matrix", "Shared responsibility matrix", "Shared responsibility matrix is reviewed and attached.", true),
        new("security-review", "Security review", "Tenant isolation, upload, audit, and access controls are reviewed.", true),
        new("support-escalation", "Support escalation", "Support escalation path for CUI incidents is documented.", true),
        new("backup-restore", "Backup and restore", "Backup and restore procedure is tested or approved.", true),
        new("admin-access", "Admin access", "Least-privilege admin access and access review are complete.", true),
        new("retention", "Retention", "Retention, deletion, export, and litigation hold expectations are documented.", true),
        new("incident-response", "Incident response", "Incident response path and customer notification workflow are approved.", true)
    ];
}

public interface ICuiReadyApprovalChecklistGate
{
    Task EnsureApprovedChecklistAsync(Guid tenantId, string approvalRecordReference, CancellationToken cancellationToken = default);
}

public interface ICuiReadyApprovalChecklistRepository
{
    Task<CuiReadyApprovalChecklistDto> CreateAsync(Guid tenantId, Guid actorUserId, IReadOnlyList<CreateCuiReadyChecklistItem> items, CancellationToken cancellationToken = default);
    Task<CuiReadyApprovalChecklistDto?> FindAsync(Guid tenantId, Guid checklistId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CuiReadyApprovalChecklistDto>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<CuiReadyApprovalChecklistDto?> UpdateItemAsync(Guid tenantId, Guid checklistId, string itemKey, UpdateCuiReadyChecklistItemRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<CuiReadyApprovalChecklistDto?> SetStateAsync(Guid tenantId, Guid checklistId, CuiReadyChecklistState state, Guid actorUserId, string? reason, CancellationToken cancellationToken = default);
}

public sealed class CuiReadyApprovalChecklistValidationException(string message) : InvalidOperationException(message);

public sealed record CreateCuiReadyChecklistItem(string ItemKey, string Section, string Description, bool IsRequired);

public sealed record CuiReadyApprovalChecklistDto(
    Guid Id,
    Guid TenantId,
    int Version,
    CuiReadyChecklistState State,
    string? RejectionReason,
    Guid? ReviewedByUserId,
    DateTimeOffset? ReviewedAt,
    DateTimeOffset CreatedAt,
    Guid? CreatedByUserId,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedByUserId,
    IReadOnlyList<CuiReadyApprovalChecklistItemDto> Items);

public sealed record CuiReadyApprovalChecklistItemDto(
    Guid Id,
    Guid ChecklistId,
    string ItemKey,
    string Section,
    string Description,
    bool IsRequired,
    CuiReadyChecklistItemStatus Status,
    string? Owner,
    string? EvidenceLink,
    Guid? ReviewerUserId,
    DateOnly? ReviewedAt,
    string? Notes);

public sealed record UpdateCuiReadyChecklistItemRequest(
    CuiReadyChecklistItemStatus Status,
    string? Owner,
    string? EvidenceLink,
    Guid? ReviewerUserId,
    DateOnly? ReviewedAt,
    string? Notes);

public sealed record ReviewCuiReadyChecklistRequest(string? Reason);

public enum CuiReadyChecklistState
{
    Draft,
    InReview,
    Approved,
    Rejected,
    Superseded
}

public enum CuiReadyChecklistItemStatus
{
    NotStarted,
    InProgress,
    Complete,
    NotApplicable
}
