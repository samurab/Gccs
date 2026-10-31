using Gccs.Application.Audit;
using Gccs.Domain.Audit;

namespace Gccs.Application.Compliance;

public sealed class ComplianceChecklistService(
    IComplianceChecklistRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public Task<IReadOnlyList<ComplianceChecklistTemplateDto>> ListTemplatesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(ComplianceChecklistTemplateCatalog.All);

    public Task<IReadOnlyList<ComplianceChecklistInstanceDto>> ListCurrentTenantAsync(CancellationToken cancellationToken = default) =>
        repository.ListCurrentTenantAsync(cancellationToken);

    public async Task<ComplianceChecklistInstanceDto?> CreateCurrentTenantAsync(
        CreateComplianceChecklistInstanceRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var template = FindTemplate(request.TemplateKey);
        var created = await repository.CreateCurrentTenantAsync(template, actorUserId, cancellationToken);
        if (created is not null)
        {
            await WriteAuditAsync(created, actorUserId, AuditAction.Created, "created", cancellationToken);
        }

        return created;
    }

    public async Task<ComplianceChecklistInstanceDto?> UpdateItemCurrentTenantAsync(
        Guid checklistId,
        Guid itemId,
        UpdateComplianceChecklistItemRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateUpdate(request);
        var updated = await repository.UpdateItemCurrentTenantAsync(checklistId, itemId, request, actorUserId, cancellationToken);
        if (updated is not null)
        {
            await WriteAuditAsync(updated, actorUserId, AuditAction.Updated, "item-updated", cancellationToken);
        }

        return updated;
    }

    private static ComplianceChecklistTemplateDto FindTemplate(string templateKey)
    {
        var template = ComplianceChecklistTemplateCatalog.All.SingleOrDefault(item =>
            string.Equals(item.Key, templateKey.Trim(), StringComparison.OrdinalIgnoreCase));
        return template ?? throw new ComplianceChecklistValidationException("Checklist template was not found.");
    }

    private static void ValidateUpdate(UpdateComplianceChecklistItemRequest request)
    {
        if (!ComplianceChecklistStatusValues.All.Contains(request.Status, StringComparer.OrdinalIgnoreCase))
        {
            throw new ComplianceChecklistValidationException("Checklist item status is invalid.");
        }

        if (!ComplianceChecklistReviewStatusValues.All.Contains(request.ReviewStatus, StringComparer.OrdinalIgnoreCase))
        {
            throw new ComplianceChecklistValidationException("Checklist item review status is invalid.");
        }

        if (request.Status.Equals(ComplianceChecklistStatusValues.Complete, StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(request.Notes) &&
            request.EvidenceItemId is null)
        {
            throw new ComplianceChecklistValidationException("Completed checklist items require notes or linked evidence.");
        }
    }

    private Task WriteAuditAsync(
        ComplianceChecklistInstanceDto checklist,
        Guid actorUserId,
        AuditAction action,
        string lifecycleAction,
        CancellationToken cancellationToken) =>
        auditEventWriter.WriteAsync(
            checklist.TenantId,
            actorUserId,
            action,
            "ComplianceChecklist",
            checklist.Id.ToString(),
            $"Compliance checklist {lifecycleAction}.",
            new Dictionary<string, string>
            {
                ["checklistId"] = checklist.Id.ToString(),
                ["templateKey"] = checklist.TemplateKey,
                ["checklistType"] = checklist.ChecklistType,
                ["reviewStatus"] = checklist.ReviewStatus,
                ["lifecycleAction"] = lifecycleAction
            },
            cancellationToken);
}

public interface IComplianceChecklistRepository
{
    Task<IReadOnlyList<ComplianceChecklistInstanceDto>> ListCurrentTenantAsync(CancellationToken cancellationToken = default);

    Task<ComplianceChecklistInstanceDto?> CreateCurrentTenantAsync(
        ComplianceChecklistTemplateDto template,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<ComplianceChecklistInstanceDto?> UpdateItemCurrentTenantAsync(
        Guid checklistId,
        Guid itemId,
        UpdateComplianceChecklistItemRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed record CreateComplianceChecklistInstanceRequest(string TemplateKey);

public sealed record UpdateComplianceChecklistItemRequest(
    string Status,
    Guid? OwnerUserId,
    string ReviewStatus,
    Guid? ReviewedByUserId,
    DateTimeOffset? ReviewedAt,
    string? Notes,
    string? ControlId,
    Guid? EvidenceItemId,
    Guid? PoamItemId);

public sealed record ComplianceChecklistTemplateDto(
    string Key,
    string Name,
    string ChecklistType,
    string Description,
    IReadOnlyList<ComplianceChecklistTemplateItemDto> Items);

public sealed record ComplianceChecklistTemplateItemDto(
    string Key,
    string Title,
    string Description,
    string? ControlId,
    string? EvidenceType,
    string? PoamHint);

public sealed record ComplianceChecklistInstanceDto(
    Guid Id,
    Guid TenantId,
    string TemplateKey,
    string Name,
    string ChecklistType,
    string ReviewStatus,
    DateTimeOffset CreatedAt,
    Guid CreatedByUserId,
    IReadOnlyList<ComplianceChecklistItemDto> Items);

public sealed record ComplianceChecklistItemDto(
    Guid Id,
    Guid ChecklistId,
    string TemplateItemKey,
    string Title,
    string Description,
    string Status,
    Guid? OwnerUserId,
    string ReviewStatus,
    Guid? ReviewedByUserId,
    DateTimeOffset? ReviewedAt,
    string? Notes,
    string? ControlId,
    Guid? EvidenceItemId,
    Guid? PoamItemId,
    DateTimeOffset? CompletedAt,
    Guid? CompletedByUserId);

public static class ComplianceChecklistStatusValues
{
    public const string NotStarted = "NotStarted";
    public const string InProgress = "InProgress";
    public const string Blocked = "Blocked";
    public const string Complete = "Complete";

    public static readonly string[] All = [NotStarted, InProgress, Blocked, Complete];
}

public static class ComplianceChecklistReviewStatusValues
{
    public const string NotReviewed = "NotReviewed";
    public const string PendingReview = "PendingReview";
    public const string Accepted = "Accepted";
    public const string Rejected = "Rejected";

    public static readonly string[] All = [NotReviewed, PendingReview, Accepted, Rejected];
}

public sealed class ComplianceChecklistValidationException(string message) : InvalidOperationException(message);

public static class ComplianceChecklistTemplateCatalog
{
    public static readonly IReadOnlyList<ComplianceChecklistTemplateDto> All =
    [
        new(
            "cmmc-readiness",
            "CMMC Readiness Checklist",
            "CmmcReadiness",
            "Reusable checklist for CMMC readiness workflow tracking. This does not certify CMMC compliance.",
            [
                new("access-control-evidence", "Access control evidence mapped", "Link current access control evidence to applicable controls.", "AC.L1-3.1.1", "SystemConfiguration", "Open POA&M if evidence is missing."),
                new("mfa-review", "MFA review recorded", "Record MFA implementation notes, owner, and reviewer metadata.", "IA.L1-3.5.2", "Screenshot", "Create remediation item for gaps."),
                new("poam-review", "Open POA&Ms reviewed", "Review open POA&M items and assign remediation owners.", null, null, "Link open remediation items.")
            ]),
        new(
            "no-cui-upload",
            "No-CUI Upload Checklist",
            "NoCuiUpload",
            "Reusable checklist for No-CUI evidence and document upload guardrails.",
            [
                new("attestation-confirmed", "No-CUI attestation confirmed", "Confirm upload workflows require explicit No-CUI attestation.", null, "Policy", null),
                new("file-validation", "File validation reviewed", "Confirm allowed type and size limits are enforced.", null, "SystemConfiguration", null),
                new("rejection-audit", "Rejected uploads audit logged", "Confirm rejected upload attempts create audit records.", null, "AccessReview", null)
            ]),
        new(
            "supplier-risk",
            "Supplier Risk Checklist",
            "SupplierRisk",
            "Reusable checklist for supplier and subcontractor risk review.",
            [
                new("supplier-profile", "Supplier profile reviewed", "Confirm supplier profile, role, access posture, and status are current.", null, "VendorAttestation", "Create POA&M for unresolved access gaps."),
                new("flow-downs", "Flow-down obligations checked", "Confirm required flow-down clauses are assigned and tracked.", null, "SignedFlowDown", null),
                new("supplier-evidence", "Supplier evidence reviewed", "Review supplier evidence requests and overdue submissions.", null, "SubcontractorCertification", null)
            ]),
        new(
            "security-policy-review",
            "Security Policy Review Checklist",
            "SecurityPolicyReview",
            "Reusable checklist for source-backed security policy review.",
            [
                new("policy-source", "Policy source traceability verified", "Confirm policy source, version, and review metadata are present.", null, "Policy", null),
                new("policy-approval", "Policy approval recorded", "Confirm policy approval metadata and owner assignment are present.", null, "Policy", null),
                new("policy-evidence", "Policy evidence linked", "Link approved policy evidence to relevant controls or obligations.", null, "Policy", "Open POA&M for missing evidence.")
            ])
    ];
}
