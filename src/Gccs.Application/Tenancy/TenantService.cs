using Gccs.Application.Audit;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Domain.Tenancy;

namespace Gccs.Application.Tenancy;

public sealed class TenantService(ITenantRepository tenantRepository, IAuditEventWriter auditEventWriter)
{
    private static readonly TenantStatus[] CreatableStatuses =
    [
        TenantStatus.Active,
        TenantStatus.Trialing,
        TenantStatus.Suspended
    ];

    private static readonly TenantDataPosture[] SupportedDataHandlingModes =
    [
        TenantDataPosture.DemoSandbox,
        TenantDataPosture.NoCui,
        TenantDataPosture.CuiReady
    ];

    public async Task<TenantDto> CreateAsync(
        CreateTenantRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateDisplayName(request.DisplayName);

        if (!CreatableStatuses.Contains(request.Status))
        {
            throw new ArgumentException("Tenant status must be active, trialing, or suspended for creation.", nameof(request));
        }

        var dataHandlingMode = request.DataHandlingMode ?? TenantDataPosture.NoCui;
        ValidateDataHandlingMode(dataHandlingMode, request.DataHandlingModeReason, request.ApprovalRecordReference, isCreate: true);

        var now = DateTimeOffset.UtcNow;
        var tenant = new Tenant(
            Guid.NewGuid(),
            request.DisplayName.Trim(),
            request.Status,
            dataHandlingMode,
            request.TrialEndsAt,
            new EntityAudit(now, actorUserId, null, null));

        await tenantRepository.AddAsync(tenant, cancellationToken);
        await tenantRepository.AddDataHandlingModeHistoryAsync(
            tenant.Id,
            previousMode: null,
            tenant.DataPosture,
            actorUserId,
            NormalizeReason(request.DataHandlingModeReason) ?? $"Tenant created with {tenant.DataPosture} data handling mode.",
            request.ApprovalRecordReference,
            cancellationToken);

        await auditEventWriter.WriteAsync(
            tenant.Id,
            actorUserId,
            AuditAction.Created,
            "Tenant",
            tenant.Id.ToString(),
            $"Tenant '{tenant.Name}' was created.",
            new Dictionary<string, string>
            {
                ["status"] = tenant.Status.ToString(),
                ["dataPosture"] = tenant.DataPosture.ToString(),
                ["dataHandlingMode"] = tenant.DataPosture.ToString()
            },
            cancellationToken);

        return ToDto(tenant);
    }

    public async Task<TenantDto?> FindInCurrentTenantScopeAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await tenantRepository.FindInCurrentTenantScopeAsync(tenantId, cancellationToken);
        return tenant is null ? null : ToDto(tenant);
    }

    public async Task<TenantDto?> UpdateStatusAsync(
        Guid tenantId,
        UpdateTenantStatusRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (request.Status is TenantStatus.Trialing)
        {
            throw new ArgumentException("Tenant status changes may set active, suspended, or archived.", nameof(request));
        }

        var existingTenant = await tenantRepository.FindInCurrentTenantScopeAsync(tenantId, cancellationToken);
        if (existingTenant is null)
        {
            return null;
        }

        var tenant = await tenantRepository.UpdateStatusInCurrentTenantScopeAsync(tenantId, request.Status, cancellationToken);
        if (tenant is null)
        {
            return null;
        }

        await auditEventWriter.WriteAsync(
            tenant.Id,
            actorUserId,
            AuditAction.Updated,
            "Tenant",
            tenant.Id.ToString(),
            $"Tenant '{tenant.Name}' status changed to {tenant.Status}.",
            new Dictionary<string, string>
            {
                ["beforeStatus"] = existingTenant.Status.ToString(),
                ["afterStatus"] = tenant.Status.ToString()
            },
            cancellationToken);

        return ToDto(tenant);
    }

    public async Task<TenantDto?> UpdateDataHandlingModeAsync(
        Guid tenantId,
        UpdateTenantDataHandlingModeRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateDataHandlingMode(request.DataHandlingMode, request.Reason, request.ApprovalRecordReference, isCreate: false);

        var existingTenant = await tenantRepository.FindInCurrentTenantScopeAsync(tenantId, cancellationToken);
        if (existingTenant is null)
        {
            return null;
        }

        var reason = NormalizeReason(request.Reason);
        if (reason is null)
        {
            throw new ArgumentException("Data handling mode change reason is required.", nameof(request));
        }

        var tenant = await tenantRepository.UpdateDataHandlingModeInCurrentTenantScopeAsync(
            tenantId,
            request.DataHandlingMode,
            actorUserId,
            cancellationToken);
        if (tenant is null)
        {
            return null;
        }

        await tenantRepository.AddDataHandlingModeHistoryAsync(
            tenant.Id,
            existingTenant.DataPosture,
            tenant.DataPosture,
            actorUserId,
            reason,
            request.ApprovalRecordReference,
            cancellationToken);

        await auditEventWriter.WriteAsync(
            tenant.Id,
            actorUserId,
            AuditAction.Updated,
            "Tenant",
            tenant.Id.ToString(),
            $"Tenant '{tenant.Name}' data handling mode changed to {tenant.DataPosture}.",
            new Dictionary<string, string>
            {
                ["beforeDataHandlingMode"] = existingTenant.DataPosture.ToString(),
                ["afterDataHandlingMode"] = tenant.DataPosture.ToString(),
                ["reason"] = reason,
                ["approvalRecordReference"] = request.ApprovalRecordReference?.Trim() ?? string.Empty
            },
            cancellationToken);

        return ToDto(tenant);
    }

    public Task<IReadOnlyList<TenantDataHandlingModeHistoryDto>> ListDataHandlingModeHistoryAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        tenantRepository.ListDataHandlingModeHistoryInCurrentTenantScopeAsync(tenantId, cancellationToken);

    public Task<TenantDataPosture?> FindCurrentTenantDataHandlingModeAsync(CancellationToken cancellationToken = default) =>
        tenantRepository.FindCurrentTenantDataHandlingModeAsync(cancellationToken);

    private static TenantDto ToDto(Tenant tenant) =>
        new(
            tenant.Id,
            tenant.Name,
            tenant.Status,
            tenant.DataPosture,
            tenant.DataPosture,
            tenant.TrialEndsAt,
            tenant.Audit.CreatedAt,
            tenant.Audit.UpdatedAt);

    private static void ValidateDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Tenant display name is required.", nameof(displayName));
        }

        if (displayName.Trim().Length > 240)
        {
            throw new ArgumentException("Tenant display name must be 240 characters or fewer.", nameof(displayName));
        }
    }

    private static void ValidateDataHandlingMode(
        TenantDataPosture dataHandlingMode,
        string? reason,
        string? approvalRecordReference,
        bool isCreate)
    {
        if (!SupportedDataHandlingModes.Contains(dataHandlingMode))
        {
            throw new ArgumentException("Tenant data handling mode must be DemoSandbox, NoCui, or CuiReady.", nameof(dataHandlingMode));
        }

        if (!isCreate && NormalizeReason(reason) is null)
        {
            throw new ArgumentException("Data handling mode change reason is required.", nameof(reason));
        }

        if (dataHandlingMode is TenantDataPosture.CuiReady && string.IsNullOrWhiteSpace(approvalRecordReference))
        {
            throw new ArgumentException("CuiReady mode requires an approved CUI-ready checklist reference.", nameof(approvalRecordReference));
        }
    }

    private static string? NormalizeReason(string? reason) =>
        string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
}
