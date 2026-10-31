using System.Text.Json;
using Gccs.Application.Identity;
using Gccs.Application.Security;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Identity;

public sealed class EfSamlIdentityProviderConfigurationRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : ISamlIdentityProviderConfigurationRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<SamlIdentityProviderConfigurationDto>> ListCurrentTenantAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.SamlIdentityProviderConfigurations
            .AsNoTracking()
            .Where(configuration => configuration.TenantId == tenantContext.TenantId)
            .OrderByDescending(configuration => configuration.CreatedAt)
            .Select(configuration => ToDto(configuration))
            .ToListAsync(cancellationToken);

    public async Task<SamlIdentityProviderConfigurationDto?> GetCurrentTenantAsync(
        Guid configurationId,
        CancellationToken cancellationToken = default)
    {
        var configuration = await dbContext.SamlIdentityProviderConfigurations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == configurationId && candidate.TenantId == tenantContext.TenantId,
                cancellationToken);

        return configuration is null ? null : ToDto(configuration);
    }

    public Task<bool> CurrentTenantEntityIdExistsAsync(
        string entityId,
        Guid? excludingConfigurationId,
        CancellationToken cancellationToken = default) =>
        dbContext.SamlIdentityProviderConfigurations.AnyAsync(
            configuration =>
                configuration.TenantId == tenantContext.TenantId &&
                configuration.EntityId == entityId &&
                (!excludingConfigurationId.HasValue || configuration.Id != excludingConfigurationId.Value),
            cancellationToken);

    public async Task<SamlIdentityProviderConfigurationDto> AddToCurrentTenantAsync(
        SamlIdentityProviderConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var entity = new SamlIdentityProviderConfigurationEntity
        {
            Id = configuration.Id,
            TenantId = tenantContext.TenantId,
            EntityId = configuration.EntityId,
            SsoUrl = configuration.SsoUrl,
            CertificatePem = configuration.CertificatePem,
            CertificateFingerprint = configuration.CertificateFingerprint,
            CertificateExpiresAt = configuration.CertificateExpiresAt,
            SigningRequirement = configuration.SigningRequirement,
            NameIdFormat = configuration.NameIdFormat,
            AttributeMappingsJson = JsonSerializer.Serialize(configuration.AttributeMappings, JsonOptions),
            Status = configuration.Status,
            MetadataUrl = configuration.MetadataUrl,
            CallbackUrl = configuration.CallbackUrl,
            LastTestedAt = configuration.LastTestedAt,
            LastTestResult = configuration.LastTestResult,
            LastTestDiagnosticSummary = configuration.LastTestDiagnosticSummary,
            CreatedAt = configuration.CreatedAt,
            CreatedByUserId = configuration.CreatedByUserId,
            UpdatedAt = configuration.UpdatedAt,
            UpdatedByUserId = configuration.UpdatedByUserId
        };

        dbContext.SamlIdentityProviderConfigurations.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public Task<SamlIdentityProviderConfigurationDto?> UpdateStatusInCurrentTenantScopeAsync(
        Guid configurationId,
        SamlConfigurationStatus status,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        UpdateCurrentTenantAsync(
            configurationId,
            actorUserId,
            configuration => configuration.Status = status,
            cancellationToken);

    public Task<SamlIdentityProviderConfigurationDto?> UpdateTestResultInCurrentTenantScopeAsync(
        Guid configurationId,
        SamlTestResult result,
        string diagnosticSummary,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        UpdateCurrentTenantAsync(
            configurationId,
            actorUserId,
            configuration =>
            {
                configuration.LastTestedAt = DateTimeOffset.UtcNow;
                configuration.LastTestResult = result;
                configuration.LastTestDiagnosticSummary = diagnosticSummary;
            },
            cancellationToken);

    public Task<SamlIdentityProviderConfigurationDto?> RotateCertificateInCurrentTenantScopeAsync(
        Guid configurationId,
        string certificatePem,
        string? certificateFingerprint,
        DateTimeOffset certificateExpiresAt,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        UpdateCurrentTenantAsync(
            configurationId,
            actorUserId,
            configuration =>
            {
                configuration.CertificatePem = certificatePem;
                configuration.CertificateFingerprint = certificateFingerprint;
                configuration.CertificateExpiresAt = certificateExpiresAt;
                configuration.Status = SamlConfigurationStatus.Rotated;
            },
            cancellationToken);

    private async Task<SamlIdentityProviderConfigurationDto?> UpdateCurrentTenantAsync(
        Guid configurationId,
        Guid actorUserId,
        Action<SamlIdentityProviderConfigurationEntity> update,
        CancellationToken cancellationToken)
    {
        var configuration = await dbContext.SamlIdentityProviderConfigurations
            .SingleOrDefaultAsync(
                candidate => candidate.Id == configurationId && candidate.TenantId == tenantContext.TenantId,
                cancellationToken);

        if (configuration is null)
        {
            return null;
        }

        update(configuration);
        configuration.UpdatedAt = DateTimeOffset.UtcNow;
        configuration.UpdatedByUserId = actorUserId;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(configuration);
    }

    private static SamlIdentityProviderConfigurationDto ToDto(SamlIdentityProviderConfigurationEntity configuration) =>
        new(
            configuration.Id,
            configuration.TenantId,
            configuration.EntityId,
            configuration.SsoUrl,
            configuration.CertificateFingerprint,
            configuration.CertificateExpiresAt,
            configuration.SigningRequirement,
            configuration.NameIdFormat,
            DeserializeMappings(configuration.AttributeMappingsJson),
            configuration.Status,
            configuration.MetadataUrl,
            configuration.CallbackUrl,
            configuration.LastTestedAt,
            configuration.LastTestResult,
            configuration.LastTestDiagnosticSummary,
            configuration.CreatedAt,
            configuration.UpdatedAt);

    private static IReadOnlyDictionary<string, string> DeserializeMappings(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, string>();
        }

        return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions) ?? new Dictionary<string, string>();
    }
}
