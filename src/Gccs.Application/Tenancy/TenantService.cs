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

        var now = DateTimeOffset.UtcNow;
        var tenant = new Tenant(
            Guid.NewGuid(),
            request.DisplayName.Trim(),
            request.Status,
            TenantDataPosture.NoCui,
            request.TrialEndsAt,
            new EntityAudit(now, actorUserId, null, null));

        await tenantRepository.AddAsync(tenant, cancellationToken);
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
                ["dataPosture"] = tenant.DataPosture.ToString()
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

    private static TenantDto ToDto(Tenant tenant) =>
        new(
            tenant.Id,
            tenant.Name,
            tenant.Status,
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
}
