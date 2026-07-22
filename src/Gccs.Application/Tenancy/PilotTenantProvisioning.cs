using Gccs.Application.Identity;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;

namespace Gccs.Application.Tenancy;

public sealed class PilotTenantProvisioningService(IPilotTenantProvisioningRepository repository)
{
    public async Task<PilotTenantProvisioningResultDto> ProvisionAsync(
        PilotTenantProvisioningRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        var roleName = NormalizePilotOwnerRole(request.OwnerRoleName);

        return await repository.ProvisionAsync(
            request with
            {
                DisplayName = request.DisplayName.Trim(),
                OwnerEmail = request.OwnerEmail.Trim().ToLowerInvariant(),
                OwnerDisplayName = request.OwnerDisplayName.Trim(),
                OwnerRoleName = roleName,
                SetupReason = NormalizeReason(request.SetupReason) ??
                    "Pilot tenant provisioned with No-CUI compliance management posture."
            },
            actorUserId,
            cancellationToken);
    }

    private static void ValidateRequest(PilotTenantProvisioningRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new ArgumentException("Pilot tenant display name is required.", nameof(request));
        }

        if (request.DisplayName.Trim().Length > 240)
        {
            throw new ArgumentException("Pilot tenant display name must be 240 characters or fewer.", nameof(request));
        }

        if (request.OwnerUserId == Guid.Empty)
        {
            throw new ArgumentException("Pilot owner user ID is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.OwnerEmail) ||
            request.OwnerEmail.Trim().Length > 320 ||
            !request.OwnerEmail.Contains('@', StringComparison.Ordinal))
        {
            throw new ArgumentException("A valid pilot owner email is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.OwnerDisplayName) || request.OwnerDisplayName.Trim().Length > 200)
        {
            throw new ArgumentException("Pilot owner display name is required and must be 200 characters or fewer.", nameof(request));
        }

        NormalizePilotOwnerRole(request.OwnerRoleName);
    }

    private static string NormalizePilotOwnerRole(string roleName)
    {
        if (!RoleCatalog.TryNormalizeRoleName(roleName, out var canonicalRoleName) ||
            canonicalRoleName is not (RoleCatalog.Owner or RoleCatalog.Admin))
        {
            throw new ArgumentException("Pilot owner role must be Owner or Admin.", nameof(roleName));
        }

        return canonicalRoleName;
    }

    private static string? NormalizeReason(string? reason) =>
        string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
}

public interface IPilotTenantProvisioningRepository
{
    Task<PilotTenantProvisioningResultDto> ProvisionAsync(
        PilotTenantProvisioningRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed record PilotTenantProvisioningRequest(
    string DisplayName,
    Guid OwnerUserId,
    string OwnerEmail,
    string OwnerDisplayName,
    string OwnerRoleName = RoleCatalog.Owner,
    DateOnly? TrialEndsAt = null,
    string? SetupReason = null);

public sealed record PilotTenantProvisioningResultDto(
    TenantDto Tenant,
    TenantMemberDto Owner,
    TenantDataPosture DataHandlingMode,
    string SetupReason);
