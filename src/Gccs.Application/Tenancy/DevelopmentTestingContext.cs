using Gccs.Domain.Identity;

namespace Gccs.Application.Tenancy;

public sealed class DevelopmentTestingContextService(IDevelopmentTenantCatalogRepository repository)
{
    public async Task<DevelopmentTestingContextDto> GetAsync(CancellationToken cancellationToken = default) =>
        new(
            await repository.ListAsync(cancellationToken),
            RoleCatalog.Roles);
}

public interface IDevelopmentTenantCatalogRepository
{
    Task<IReadOnlyList<DevelopmentTenantOptionDto>> ListAsync(CancellationToken cancellationToken = default);
}

public sealed record DevelopmentTestingContextDto(
    IReadOnlyList<DevelopmentTenantOptionDto> Tenants,
    IReadOnlyList<string> Roles);

public sealed record DevelopmentTenantOptionDto(
    Guid TenantId,
    string DisplayName,
    string TenantStatus,
    string DataHandlingMode,
    bool IsSelectable,
    string? UnavailableReason);
