using Gccs.Domain.Identity;

namespace Gccs.Application.Tenancy;

public sealed class DevelopmentTestingContextService(IDevelopmentTenantCatalogRepository repository)
{
    public async Task<DevelopmentTestingContextDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await repository.ListAsync(cancellationToken);
        var personas = await repository.ListPersonasAsync(cancellationToken);
        return new DevelopmentTestingContextDto(tenants, personas, RoleCatalog.Roles);
    }
}

public interface IDevelopmentTenantCatalogRepository
{
    Task<IReadOnlyList<DevelopmentTenantOptionDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DevelopmentPersonaOptionDto>> ListPersonasAsync(CancellationToken cancellationToken = default);
}

public sealed record DevelopmentTestingContextDto(
    IReadOnlyList<DevelopmentTenantOptionDto> Tenants,
    IReadOnlyList<DevelopmentPersonaOptionDto> Personas,
    IReadOnlyList<string> Roles);

public sealed record DevelopmentTenantOptionDto(
    Guid TenantId,
    string DisplayName,
    string TenantStatus,
    string DataHandlingMode,
    bool IsSelectable,
    string? UnavailableReason);

public sealed record DevelopmentPersonaOptionDto(
    Guid TenantId,
    Guid UserId,
    string Email,
    string DisplayName,
    string RoleName);
