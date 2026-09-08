using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Security;
using Gccs.Domain.Audit;

namespace Gccs.Application.Tenancy;

public sealed class CuiReadyApprovalChecklistService(
    ICuiReadyApprovalChecklistRepository repository,
    IAuditEventWriter auditEventWriter,
    ICuiReadinessEvidenceRepository? evidence = null,
    IApplicationTransaction? transaction = null,
    ICurrentTenantContext? context = null) : ICuiReadyApprovalChecklistGate
{
    private Task<T> SerializedAsync<T>(Guid tenantId, Func<CancellationToken, Task<T>> action, CancellationToken ct) =>
        transaction is null ? action(ct) : transaction.ExecuteAsync(async token =>
        {
            if (evidence is not null) await evidence.LockTenantAsync(tenantId, token);
            return await action(token);
        }, ct);

    public Task<CuiReadyApprovalChecklistDto?> UpdateItemAsync(Guid tenantId, Guid checklistId, string itemKey,
        UpdateCuiReadyChecklistItemRequest request, Guid actorUserId, CancellationToken cancellationToken = default) =>
        SerializedAsync(tenantId, ct => UpdateItemCoreAsync(tenantId, checklistId, itemKey, request, actorUserId, ct), cancellationToken);

    public Task<CuiReadyApprovalChecklistDto?> SubmitForReviewAsync(Guid tenantId, Guid checklistId, Guid actorUserId, CancellationToken cancellationToken = default) =>
        SerializedAsync(tenantId, ct => SubmitForReviewCoreAsync(tenantId, checklistId, actorUserId, ct), cancellationToken);

    public Task<CuiReadyApprovalChecklistDto?> RejectAsync(Guid tenantId, Guid checklistId, ReviewCuiReadyChecklistRequest request,
        Guid actorUserId, CancellationToken cancellationToken = default) =>
        SerializedAsync(tenantId, ct => RejectCoreAsync(tenantId, checklistId, request, actorUserId, ct), cancellationToken);

    public Task<CuiReadyApprovalChecklistDto?> SupersedeAsync(Guid tenantId, Guid checklistId, ReviewCuiReadyChecklistRequest request,
        Guid actorUserId, CancellationToken cancellationToken = default) =>
        SerializedAsync(tenantId, ct => SupersedeCoreAsync(tenantId, checklistId, request, actorUserId, ct), cancellationToken);

    public Task<CuiReadyApprovalChecklistDto> CreateAsync(Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default) =>
        SerializedAsync(tenantId, ct => CreateCoreAsync(tenantId, actorUserId, ct), cancellationToken);

    private async Task<CuiReadyApprovalChecklistDto> CreateCoreAsync(Guid tenantId, Guid actorUserId, CancellationToken cancellationToken)
    {
        var checklist = await repository.CreateAsync(tenantId, actorUserId, DefaultItems(), cancellationToken);
        await WriteAuditAsync(checklist, actorUserId, AuditAction.Created, "created", cancellationToken);
        return checklist;
    }

    public Task<IReadOnlyList<CuiReadyApprovalChecklistDto>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        repository.ListAsync(tenantId, cancellationToken);

    private async Task<CuiReadyApprovalChecklistDto?> UpdateItemCoreAsync(
        Guid tenantId,
        Guid checklistId,
        string itemKey,
        UpdateCuiReadyChecklistItemRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        request = request.Status == CuiReadyChecklistItemStatus.Complete
            ? request with
            {
                ReviewerUserId = actorUserId,
                ReviewedAt = DateOnly.FromDateTime(DateTime.UtcNow)
            }
            : request with { ReviewerUserId = null, ReviewedAt = null };
        ValidateCompletedItem(request);
        var checklist = await repository.UpdateItemAsync(tenantId, checklistId, itemKey, request, actorUserId, cancellationToken);
        if (checklist is not null)
        {
            await WriteAuditAsync(checklist, actorUserId, AuditAction.Updated, "updated", cancellationToken);
        }

        return checklist;
    }

    private async Task<CuiReadyApprovalChecklistDto?> SubmitForReviewCoreAsync(
        Guid tenantId,
        Guid checklistId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var current = await repository.FindAsync(tenantId, checklistId, cancellationToken);
        if (current is null) return null;
        if (current.State != CuiReadyChecklistState.Draft)
            throw new CuiReadyApprovalChecklistValidationException("Only a draft checklist can be submitted.");
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
        try
        {
            var result = await SerializedAsync(tenantId,
                ct => ApproveCoreAsync(tenantId, checklistId, request, actorUserId, ct), cancellationToken);
            if (result is null) await FailureAsync(tenantId, actorUserId, "approval", "checklist_unavailable", cancellationToken);
            return result;
        }
        catch (CuiReadyApprovalChecklistValidationException exception)
        {
            await FailureAsync(tenantId, actorUserId, "approval", exception.Message, CancellationToken.None);
            throw;
        }
    }

    private async Task<CuiReadyApprovalChecklistDto?> ApproveCoreAsync(Guid tenantId, Guid checklistId,
        ReviewCuiReadyChecklistRequest request, Guid actorUserId, CancellationToken cancellationToken)
    {
        var current = await repository.FindAsync(tenantId, checklistId, cancellationToken);
        if (current is null)
        {
            return null;
        }

        if (current.State != CuiReadyChecklistState.InReview)
            throw new CuiReadyApprovalChecklistValidationException("Only an in-review checklist can receive final approval.");
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 1000)
            throw new CuiReadyApprovalChecklistValidationException("Final approval requires review notes of at most 1000 characters.");
        await ValidateEvidenceAsync(tenantId, current, cancellationToken);

        var checklist = await repository.SetStateAsync(tenantId, checklistId, CuiReadyChecklistState.Approved, actorUserId, request.Reason, cancellationToken);
        if (checklist is not null)
        {
            await WriteAuditAsync(checklist, actorUserId, AuditAction.Approved, "approved", cancellationToken);
        }

        return checklist;
    }

    private async Task<CuiReadyApprovalChecklistDto?> RejectCoreAsync(
        Guid tenantId,
        Guid checklistId,
        ReviewCuiReadyChecklistRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 1000)
        {
            throw new CuiReadyApprovalChecklistValidationException("Checklist rejection reason is required and must not exceed 1000 characters.");
        }

        var current = await repository.FindAsync(tenantId, checklistId, cancellationToken);
        if (current is null) return null;
        if (current.State is CuiReadyChecklistState.Approved or CuiReadyChecklistState.Rejected or CuiReadyChecklistState.Superseded)
            throw new CuiReadyApprovalChecklistValidationException("Only a draft or in-review checklist can be rejected.");

        var checklist = await repository.SetStateAsync(tenantId, checklistId, CuiReadyChecklistState.Rejected, actorUserId, request.Reason.Trim(), cancellationToken);
        if (checklist is not null)
        {
            await WriteAuditAsync(checklist, actorUserId, AuditAction.Rejected, "rejected", cancellationToken);
        }

        return checklist;
    }

    private async Task<CuiReadyApprovalChecklistDto?> SupersedeCoreAsync(
        Guid tenantId,
        Guid checklistId,
        ReviewCuiReadyChecklistRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 1000)
            throw new CuiReadyApprovalChecklistValidationException("Checklist supersession reason is required and must not exceed 1000 characters.");
        var current = await repository.FindAsync(tenantId, checklistId, cancellationToken);
        if (current is null) return null;
        if (current.State != CuiReadyChecklistState.Approved)
            throw new CuiReadyApprovalChecklistValidationException("Only an approved checklist can be superseded.");
        var checklist = await repository.SetStateAsync(tenantId, checklistId, CuiReadyChecklistState.Superseded, actorUserId, request.Reason.Trim(), cancellationToken);
        if (checklist is not null)
        {
            await WriteAuditAsync(checklist, actorUserId, AuditAction.Archived, "superseded", cancellationToken);
        }

        return checklist;
    }

    public async Task EnsureApprovedChecklistAsync(Guid tenantId, string approvalRecordReference, CancellationToken cancellationToken = default)
    {
        try
        {
            await SerializedAsync(tenantId, async ct =>
            {
                await EnsureApprovedChecklistCoreAsync(tenantId, approvalRecordReference, ct);
                return true;
            }, cancellationToken);
        }
        catch (CuiReadyApprovalChecklistValidationException exception)
        {
            await FailureAsync(tenantId, context?.UserId ?? Guid.Empty, "use", exception.Message, CancellationToken.None);
            throw;
        }
    }

    private async Task EnsureApprovedChecklistCoreAsync(Guid tenantId, string approvalRecordReference, CancellationToken cancellationToken)
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

        if (checklist.ReviewedByUserId is null || checklist.ReviewedByUserId == Guid.Empty || string.IsNullOrWhiteSpace(checklist.ReviewNotes) || checklist.ReviewedAt is null ||
            checklist.ReviewedAt.Value < DateTimeOffset.UtcNow.AddYears(-1) || checklist.ReviewedAt.Value > DateTimeOffset.UtcNow)
        {
            throw new CuiReadyApprovalChecklistValidationException("CuiReady mode requires a non-expired checklist approval reviewed within the last year.");
        }
        await ValidateEvidenceAsync(tenantId, checklist, cancellationToken);
    }

    private async Task ValidateEvidenceAsync(Guid tenantId, CuiReadyApprovalChecklistDto checklist, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (DefaultItems().Any(required => !checklist.Items.Any(i => i.ItemKey == required.ItemKey && i.IsRequired)) ||
            checklist.Items.Any(i => i.IsRequired && (i.Status != CuiReadyChecklistItemStatus.Complete ||
                i.ReviewerUserId is null || i.ReviewerUserId == Guid.Empty || i.ReviewedAt is null ||
                i.ReviewedAt > today || i.ReviewedAt <= today.AddYears(-1))))
            throw new CuiReadyApprovalChecklistValidationException("All required items need a current completed review.");
        if (evidence is null)
            throw new CuiReadyApprovalChecklistValidationException("Supporting evidence verification is unavailable; approval is blocked.");
        string? error;
        try { error = await evidence.ValidateLinksAsync(tenantId, checklist, ct); }
        catch (Exception ex) when (ex is DataHandlingNoticeValidationException or SharedResponsibilityMatrixValidationException or IOException or System.Text.Json.JsonException)
        { throw new CuiReadyApprovalChecklistValidationException("Current published readiness documents are unavailable."); }
        if (error is not null) throw new CuiReadyApprovalChecklistValidationException(error);
    }

    private Task FailureAsync(Guid tenantId, Guid actor, string operation, string reason, CancellationToken ct) =>
        auditEventWriter.WriteAsync(context?.TenantId ?? tenantId, context?.UserId ?? actor, AuditAction.Rejected,
            "CuiReadyApprovalChecklist", (context?.TenantId ?? tenantId).ToString(), "CUI-ready gate evaluation failed.",
            new Dictionary<string, string> { ["eventType"] = Phase1ACuiAuditEvents.FailedCuiApproval,
                ["operation"] = operation, ["reason"] = reason, ["result"] = "failed" }, ct);

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
                ["eventType"] = action switch
                {
                    AuditAction.Approved => Phase1ACuiAuditEvents.ChecklistApproval,
                    AuditAction.Rejected => Phase1ACuiAuditEvents.ChecklistRejection,
                    _ => $"checklist-{lifecycleAction}"
                },
                ["result"] = action == AuditAction.Rejected ? "rejected" : "succeeded",
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
        new("antitrust-procurement-integrity", "Antitrust and procurement integrity", "Bid, pricing, source-selection, and competitor-contact controls are reviewed with supporting evidence.", true),
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
    string? ReviewNotes,
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
    string? Notes,
    Guid? SupportingRecordId = null,
    string? SupportingVersion = null);

public sealed record UpdateCuiReadyChecklistItemRequest(
    CuiReadyChecklistItemStatus Status,
    string? Owner,
    string? EvidenceLink,
    Guid? ReviewerUserId,
    DateOnly? ReviewedAt,
    string? Notes,
    Guid? SupportingRecordId = null,
    string? SupportingVersion = null);

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
