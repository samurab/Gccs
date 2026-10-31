using Gccs.Application.Audit;
using Gccs.Domain.Audit;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;

namespace Gccs.Application.Identity;

public sealed class TenantWorkspaceSelectionService(
    ITenantWorkspaceSelectionRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public Task<TenantWorkspaceListDto> ListAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        repository.ListAsync(userId, cancellationToken);

    public async Task<TenantWorkspaceSelectionDto> SelectAsync(
        Guid userId,
        SelectTenantWorkspaceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.TenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant ID is required.", nameof(request));
        }

        var result = await repository.StageSelectionAsync(userId, request.TenantId, cancellationToken);
        if (result is null)
        {
            throw new TenantWorkspaceSelectionDeniedException(
                "The requested tenant is unavailable or the authenticated user does not have an active membership.");
        }

        // The EF repository stages the preference and access timestamp. The audit writer's
        // SaveChanges call commits all three records together in the scoped unit of work.
        await auditEventWriter.WriteChangeAsync(
            result.Workspace.TenantId,
            userId,
            AuditAction.PermissionChanged,
            "TenantWorkspaceSelection",
            userId.ToString(),
            "The authenticated user selected a tenant workspace.",
            result.PreviousTenantId?.ToString(),
            result.Workspace.TenantId.ToString(),
            new Dictionary<string, string>
            {
                ["membershipId"] = result.Workspace.MembershipId.ToString(),
                ["roleName"] = result.Workspace.RoleName
            },
            cancellationToken);

        return new TenantWorkspaceSelectionDto(
            result.Workspace.TenantId,
            result.Workspace.DisplayName,
            result.Workspace.RoleName,
            result.Workspace.DataHandlingMode);
    }
}

public interface ITenantWorkspaceSelectionRepository
{
    Task<TenantWorkspaceListDto> ListAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<TenantWorkspaceSelectionPersistenceResult?> StageSelectionAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

public sealed record TenantWorkspaceListDto(
    Guid? PreferredTenantId,
    IReadOnlyList<TenantWorkspaceDto> Tenants);

public sealed record TenantWorkspaceDto(
    Guid MembershipId,
    Guid TenantId,
    string DisplayName,
    TenantStatus TenantStatus,
    TenantDataPosture DataHandlingMode,
    MembershipStatus MembershipStatus,
    string RoleName,
    DateTimeOffset? LastAccessedAt,
    bool IsSelectable,
    string? UnavailableReason);

public sealed record SelectTenantWorkspaceRequest(Guid TenantId);

public sealed record TenantWorkspaceSelectionDto(
    Guid TenantId,
    string DisplayName,
    string RoleName,
    TenantDataPosture DataHandlingMode);

public sealed record TenantWorkspaceSelectionPersistenceResult(
    Guid? PreviousTenantId,
    TenantWorkspaceDto Workspace);

public sealed class TenantWorkspaceSelectionDeniedException(string message) : InvalidOperationException(message);
