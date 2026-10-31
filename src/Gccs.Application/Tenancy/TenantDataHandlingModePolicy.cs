using Gccs.Application.Audit;
using Gccs.Application.Security;
using Gccs.Domain.Audit;
using Gccs.Domain.Tenancy;

namespace Gccs.Application.Tenancy;

public sealed class TenantDataHandlingModePolicyService(
    IServiceProvider serviceProvider,
    ICurrentTenantContext tenantContext,
    IAuditEventWriter auditEventWriter,
    ICurrentDataHandlingNoticeGuard noticeGuard)
{
    public async Task EnsureAllowedAsync(
        TenantDataHandlingModePolicyRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var mode = await FindCurrentModeAsync(cancellationToken);
        var normalized = request.Normalize();
        if (normalized.ContainsSyntheticCui)
            normalized = normalized with { ApprovalChecksPassed =
                serviceProvider.GetService(typeof(ISyntheticContentApprovalRepository)) is ISyntheticContentApprovalRepository provenance &&
                await provenance.IsApprovedAsync(normalized.EntityType, normalized.EntityId, cancellationToken) };
        // Approval is a persisted tenant decision, never a caller assertion.
        if (mode == TenantDataPosture.CuiReady && (normalized.ContainsRealCui || normalized.RequiresCuiReadyApproval))
            normalized = normalized with { ApprovalChecksPassed = await HasCurrentApprovalAsync(cancellationToken) };
        var denialReason = GetDenialReason(mode, normalized);
        if (denialReason is null && normalized.EntityType is not null && normalized.EntityId is not null &&
            serviceProvider.GetService(typeof(IContentContainmentRepository)) is IContentContainmentRepository containment &&
            await containment.IsBlockedAsync(tenantContext.TenantId, normalized.EntityType, normalized.EntityId, cancellationToken))
        {
            denialReason = "An unresolved data handling escalation blocks use of this content.";
        }

        if (denialReason is null)
        {
            await noticeGuard.EnsureAsync(normalized.Workflow.ToString(), actorUserId, cancellationToken);
            return;
        }

        var tenantId = TryGetCurrentTenantId();
        if (tenantId is not null)
        {
            await auditEventWriter.WriteAsync(
                tenantId.Value,
                actorUserId,
                AuditAction.Rejected,
                "TenantDataHandlingModePolicy",
                normalized.EntityId ?? tenantId.Value.ToString(),
                "Tenant data handling mode blocked a restricted workflow.",
                new Dictionary<string, string>
                {
                    ["eventType"] = Phase1ACuiAuditEvents.BlockedEventType(normalized.Workflow),
                    ["workflow"] = normalized.Workflow.ToString(),
                    ["mode"] = mode.ToString(),
                    ["result"] = "rejected",
                    ["reason"] = denialReason,
                    ["entityType"] = normalized.EntityType ?? string.Empty,
                    ["entityId"] = normalized.EntityId ?? string.Empty,
                    ["containsRealCui"] = normalized.ContainsRealCui.ToString(),
                    ["containsSyntheticCui"] = normalized.ContainsSyntheticCui.ToString(),
                    ["classificationConfirmed"] = normalized.ClassificationConfirmed.ToString(),
                    ["approvalChecksPassed"] = normalized.ApprovalChecksPassed.ToString()
                },
                cancellationToken);
        }

        throw new TenantDataHandlingModeRestrictedException(
            mode,
            normalized.Workflow,
            denialReason,
            normalized.EntityType,
            normalized.EntityId);
    }

    private Guid? TryGetCurrentTenantId()
    {
        try
        {
            return tenantContext.TenantId;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private async Task<bool> HasCurrentApprovalAsync(CancellationToken cancellationToken)
    {
        if (serviceProvider.GetService(typeof(ITenantRepository)) is not ITenantRepository tenants ||
            serviceProvider.GetService(typeof(ICuiReadyApprovalChecklistGate)) is not ICuiReadyApprovalChecklistGate gate)
            return false;
        var history = await tenants.ListDataHandlingModeHistoryInCurrentTenantScopeAsync(tenantContext.TenantId, cancellationToken);
        var current = history.OrderByDescending(item => item.ChangedAt).ThenByDescending(item => item.Id).FirstOrDefault();
        if (current?.NewMode != TenantDataPosture.CuiReady || string.IsNullOrWhiteSpace(current.ApprovalRecordReference))
            return false;
        try
        {
            await gate.EnsureApprovedChecklistAsync(tenantContext.TenantId, current.ApprovalRecordReference, cancellationToken);
            return true;
        }
        catch (CuiReadyApprovalChecklistValidationException) { return false; }
    }

    private async Task<TenantDataPosture> FindCurrentModeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var repository = serviceProvider.GetService(typeof(ITenantRepository)) as ITenantRepository;
            return await (repository?.FindCurrentTenantDataHandlingModeAsync(cancellationToken) ??
                Task.FromResult<TenantDataPosture?>(TenantDataPosture.NoCui)) ??
                TenantDataPosture.NoCui;
        }
        catch (InvalidOperationException)
        {
            return TenantDataPosture.NoCui;
        }
    }

    private static string? GetDenialReason(
        TenantDataPosture mode,
        TenantDataHandlingModePolicyRequest request)
    {
        if (request.ContainsRealCui)
        {
            return mode switch
            {
                TenantDataPosture.NoCui =>
                    "NoCui tenants cannot create, upload, process, report on, export, or delete records classified as real CUI.",
                TenantDataPosture.DemoSandbox =>
                    "DemoSandbox tenants can use seeded synthetic CUI examples but cannot use real customer CUI.",
                TenantDataPosture.CuiReady when !request.ClassificationConfirmed =>
                    "CuiReady tenants require confirmed classification before CUI workflows can continue.",
                TenantDataPosture.CuiReady when !request.ApprovalChecksPassed =>
                    "CuiReady tenants require completed approval checks before CUI workflows can continue.",
                _ => null
            };
        }

        if (request.ContainsSyntheticCui)
        {
            return mode switch
            {
                TenantDataPosture.NoCui =>
                    "NoCui tenants cannot create, upload, process, report on, export, or delete synthetic CUI demo records.",
                TenantDataPosture.CuiReady =>
                    "Synthetic CUI demo records are allowed only in DemoSandbox tenants.",
                TenantDataPosture.DemoSandbox when !request.ApprovalChecksPassed =>
                    "DemoSandbox tenants require approved imported demo seed metadata before synthetic CUI workflows can continue.",
                _ => null
            };
        }

        if (request.RequiresCuiReadyApproval &&
            mode is TenantDataPosture.CuiReady &&
            (!request.ClassificationConfirmed || !request.ApprovalChecksPassed))
        {
            return !request.ClassificationConfirmed
                ? "CuiReady tenants require confirmed classification before CUI workflows can continue."
                : "CuiReady tenants require completed approval checks before CUI workflows can continue.";
        }

        return null;
    }
}

public sealed record TenantDataHandlingModePolicyRequest(
    TenantDataHandlingWorkflow Workflow,
    bool ContainsRealCui,
    bool ContainsSyntheticCui = false,
    bool ClassificationConfirmed = true,
    bool ApprovalChecksPassed = false,
    bool RequiresCuiReadyApproval = false,
    string? EntityType = null,
    string? EntityId = null)
{
    public TenantDataHandlingModePolicyRequest Normalize() =>
        this with
        {
            EntityType = string.IsNullOrWhiteSpace(EntityType) ? null : EntityType.Trim(),
            EntityId = string.IsNullOrWhiteSpace(EntityId) ? null : EntityId.Trim()
        };
}

public enum TenantDataHandlingWorkflow
{
    ContractIntake,
    ContractDocumentUpload,
    EvidenceUpload,
    EvidenceSubmission,
    Note,
    GeneratedPolicy,
    SspNarrative,
    Report,
    ExtractionJob
}

public sealed class TenantDataHandlingModeRestrictedException(
    TenantDataPosture mode,
    TenantDataHandlingWorkflow workflow,
    string message,
    string? entityType = null,
    string? entityId = null) : InvalidOperationException(message)
{
    public TenantDataPosture Mode { get; } = mode;

    public TenantDataHandlingWorkflow Workflow { get; } = workflow;

    public string? EntityType { get; } = entityType;

    public string? EntityId { get; } = entityId;
}
