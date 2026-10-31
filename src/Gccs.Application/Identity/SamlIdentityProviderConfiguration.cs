using System.Security.Cryptography;
using System.Text;
using Gccs.Application.Audit;
using Gccs.Application.Security;
using Gccs.Domain.Audit;
using Gccs.Domain.Identity;

namespace Gccs.Application.Identity;

public sealed class SamlIdentityProviderConfigurationService(
    ISamlIdentityProviderConfigurationRepository repository,
    ICurrentTenantContext tenantContext,
    IAuditEventWriter auditEventWriter)
{
    public Task<IReadOnlyList<SamlIdentityProviderConfigurationDto>> ListCurrentTenantAsync(
        CancellationToken cancellationToken = default) =>
        repository.ListCurrentTenantAsync(cancellationToken);

    public async Task<SamlIdentityProviderConfigurationDto> CreateAsync(
        UpsertSamlIdentityProviderConfigurationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateRequestShape(request);
        var entityId = NormalizeRequired(request.EntityId, nameof(request.EntityId), 512);
        if (await repository.CurrentTenantEntityIdExistsAsync(entityId, null, cancellationToken))
        {
            throw new SamlConfigurationValidationException("A SAML configuration with this entity ID already exists for the current tenant.");
        }

        var now = DateTimeOffset.UtcNow;
        var configuration = new SamlIdentityProviderConfiguration(
            Guid.NewGuid(),
            tenantContext.TenantId,
            entityId,
            request.SsoUrl.Trim(),
            NormalizeOptional(request.CertificatePem, 8000),
            ComputeFingerprint(request.CertificatePem),
            request.CertificateExpiresAt,
            request.SigningRequirement,
            request.NameIdFormat,
            NormalizeMappings(request.AttributeMappings),
            SamlConfigurationStatus.Draft,
            NormalizeOptional(request.MetadataUrl, 1000),
            NormalizeRequired(request.CallbackUrl, nameof(request.CallbackUrl), 1000),
            null,
            null,
            null,
            now,
            actorUserId,
            null,
            null);

        var created = await repository.AddToCurrentTenantAsync(configuration, cancellationToken);
        await WriteAuditAsync(created, actorUserId, AuditAction.Created, "SAML identity provider configuration was created.", cancellationToken);
        return created;
    }

    public async Task<SamlIdentityProviderConfigurationDto?> TestAsync(
        Guid configurationId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var configuration = await repository.GetCurrentTenantAsync(configurationId, cancellationToken);
        if (configuration is null)
        {
            return null;
        }

        var (result, diagnosticSummary) = ValidateForEnable(configuration, requireSuccessfulConnectionTest: false) switch
        {
            [] when configuration.SsoUrl.Contains("warning", StringComparison.OrdinalIgnoreCase) =>
                (SamlTestResult.Warning, "SAML provider metadata is reachable, but the connection returned non-blocking warnings."),
            [] when configuration.SsoUrl.Contains("fail", StringComparison.OrdinalIgnoreCase) ||
                    configuration.SsoUrl.Contains("unreachable", StringComparison.OrdinalIgnoreCase) =>
                (SamlTestResult.Failure, "SAML provider metadata could not be reached or validated."),
            [] => (SamlTestResult.Success, "SAML provider metadata validated successfully."),
            var errors => (SamlTestResult.Failure, string.Join(" ", errors))
        };

        var tested = await repository.UpdateTestResultInCurrentTenantScopeAsync(
            configurationId,
            result,
            RedactDiagnostic(diagnosticSummary),
            actorUserId,
            cancellationToken);

        if (tested is not null)
        {
            await WriteAuditAsync(tested, actorUserId, AuditAction.Updated, $"SAML identity provider test completed with result {result}.", cancellationToken);
        }

        return tested;
    }

    public Task<SamlIdentityProviderConfigurationDto?> EnableAsync(
        Guid configurationId,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(configurationId, SamlConfigurationStatus.Enabled, actorUserId, cancellationToken);

    public Task<SamlIdentityProviderConfigurationDto?> DisableAsync(
        Guid configurationId,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(configurationId, SamlConfigurationStatus.Disabled, actorUserId, cancellationToken);

    public Task<SamlIdentityProviderConfigurationDto?> ArchiveAsync(
        Guid configurationId,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(configurationId, SamlConfigurationStatus.Archived, actorUserId, cancellationToken);

    public async Task<SamlIdentityProviderConfigurationDto?> RotateCertificateAsync(
        Guid configurationId,
        RotateSamlCertificateRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CertificatePem))
        {
            throw new SamlConfigurationValidationException("Certificate is required.");
        }

        if (request.CertificateExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new SamlConfigurationValidationException("Certificate expiration must be in the future.");
        }

        var updated = await repository.RotateCertificateInCurrentTenantScopeAsync(
            configurationId,
            request.CertificatePem.Trim(),
            ComputeFingerprint(request.CertificatePem),
            request.CertificateExpiresAt,
            actorUserId,
            cancellationToken);

        if (updated is not null)
        {
            await WriteAuditAsync(updated, actorUserId, AuditAction.Updated, "SAML identity provider certificate was rotated.", cancellationToken);
        }

        return updated;
    }

    public async Task<bool> CanUseForSignInAsync(Guid configurationId, CancellationToken cancellationToken = default)
    {
        var configuration = await repository.GetCurrentTenantAsync(configurationId, cancellationToken);
        return configuration is { Status: SamlConfigurationStatus.Enabled };
    }

    private async Task<SamlIdentityProviderConfigurationDto?> ChangeStatusAsync(
        Guid configurationId,
        SamlConfigurationStatus status,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var configuration = await repository.GetCurrentTenantAsync(configurationId, cancellationToken);
        if (configuration is null)
        {
            return null;
        }

        if (configuration.Status is SamlConfigurationStatus.Archived && status is not SamlConfigurationStatus.Archived)
        {
            throw new SamlConfigurationValidationException("Archived SAML configurations cannot be re-enabled.");
        }

        if (status is SamlConfigurationStatus.Enabled)
        {
            var validationErrors = ValidateForEnable(configuration, requireSuccessfulConnectionTest: true);
            if (validationErrors.Count > 0)
            {
                throw new SamlConfigurationValidationException(string.Join(" ", validationErrors));
            }
        }

        var updated = await repository.UpdateStatusInCurrentTenantScopeAsync(configurationId, status, actorUserId, cancellationToken);
        if (updated is not null)
        {
            await WriteAuditAsync(updated, actorUserId, AuditAction.Updated, $"SAML identity provider configuration status changed to {status}.", cancellationToken);
        }

        return updated;
    }

    private IReadOnlyList<string> ValidateForEnable(
        SamlIdentityProviderConfigurationDto configuration,
        bool requireSuccessfulConnectionTest)
    {
        List<string> errors = [];
        if (string.IsNullOrWhiteSpace(configuration.EntityId))
        {
            errors.Add("Entity ID is required.");
        }

        if (string.IsNullOrWhiteSpace(configuration.MetadataUrl) && string.IsNullOrWhiteSpace(configuration.SsoUrl))
        {
            errors.Add("Provider metadata URL or SSO URL is required.");
        }

        if (!Uri.TryCreate(configuration.SsoUrl, UriKind.Absolute, out var ssoUrl) || ssoUrl.Scheme != Uri.UriSchemeHttps)
        {
            errors.Add("SSO URL must be an absolute HTTPS URL.");
        }

        if (!IsCurrentTenantCallback(configuration.CallbackUrl))
        {
            errors.Add("Callback URL must belong to the current tenant.");
        }

        if (string.IsNullOrWhiteSpace(configuration.CertificateFingerprint))
        {
            errors.Add("Certificate is required.");
        }

        if (configuration.CertificateExpiresAt <= DateTimeOffset.UtcNow)
        {
            errors.Add("Certificate is expired.");
        }

        if (requireSuccessfulConnectionTest &&
            configuration.LastTestResult is not SamlTestResult.Success and not SamlTestResult.Warning)
        {
            errors.Add("SAML configuration must pass a connection test before enablement.");
        }

        return errors;
    }

    private static void ValidateRequestShape(UpsertSamlIdentityProviderConfigurationRequest request)
    {
        NormalizeRequired(request.EntityId, nameof(request.EntityId), 512);
        NormalizeRequired(request.SsoUrl, nameof(request.SsoUrl), 1000);
        NormalizeRequired(request.CallbackUrl, nameof(request.CallbackUrl), 1000);

        if (request.CertificateExpiresAt == default)
        {
            throw new SamlConfigurationValidationException("Certificate expiration is required.");
        }

        if (!Uri.TryCreate(request.SsoUrl, UriKind.Absolute, out var ssoUrl) || ssoUrl.Scheme != Uri.UriSchemeHttps)
        {
            throw new SamlConfigurationValidationException("SSO URL must be an absolute HTTPS URL.");
        }
    }

    private bool IsCurrentTenantCallback(string callbackUrl)
    {
        if (!Uri.TryCreate(callbackUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        return uri.AbsolutePath.Contains(tenantContext.TenantId.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private Task WriteAuditAsync(
        SamlIdentityProviderConfigurationDto configuration,
        Guid actorUserId,
        AuditAction action,
        string summary,
        CancellationToken cancellationToken) =>
        auditEventWriter.WriteAsync(
            configuration.TenantId,
            actorUserId,
            action,
            "SamlIdentityProviderConfiguration",
            configuration.Id.ToString(),
            summary,
            new Dictionary<string, string>
            {
                ["entityId"] = configuration.EntityId,
                ["status"] = configuration.Status.ToString(),
                ["metadataUrl"] = configuration.MetadataUrl ?? string.Empty,
                ["lastTestResult"] = configuration.LastTestResult?.ToString() ?? string.Empty
            },
            cancellationToken);

    private static string NormalizeRequired(string value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength)
        {
            throw new SamlConfigurationValidationException($"{fieldName} is required and must be {maxLength} characters or fewer.");
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (value.Trim().Length > maxLength)
        {
            throw new SamlConfigurationValidationException($"Value must be {maxLength} characters or fewer.");
        }

        return value.Trim();
    }

    private static IReadOnlyDictionary<string, string> NormalizeMappings(IReadOnlyDictionary<string, string> mappings) =>
        mappings
            .Where(mapping => !string.IsNullOrWhiteSpace(mapping.Key) && !string.IsNullOrWhiteSpace(mapping.Value))
            .ToDictionary(
                mapping => mapping.Key.Trim(),
                mapping => mapping.Value.Trim(),
                StringComparer.OrdinalIgnoreCase);

    private static string? ComputeFingerprint(string? certificatePem)
    {
        if (string.IsNullOrWhiteSpace(certificatePem))
        {
            return null;
        }

        var bytes = Encoding.UTF8.GetBytes(certificatePem.Trim());
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static string RedactDiagnostic(string diagnostic) =>
        diagnostic
            .Replace("-----BEGIN CERTIFICATE-----", "[certificate-redacted]", StringComparison.OrdinalIgnoreCase)
            .Replace("-----END CERTIFICATE-----", "[certificate-redacted]", StringComparison.OrdinalIgnoreCase)
            .Replace("Bearer ", "[token-redacted] ", StringComparison.OrdinalIgnoreCase);
}

public interface ISamlIdentityProviderConfigurationRepository
{
    Task<IReadOnlyList<SamlIdentityProviderConfigurationDto>> ListCurrentTenantAsync(CancellationToken cancellationToken = default);

    Task<SamlIdentityProviderConfigurationDto?> GetCurrentTenantAsync(Guid configurationId, CancellationToken cancellationToken = default);

    Task<bool> CurrentTenantEntityIdExistsAsync(string entityId, Guid? excludingConfigurationId, CancellationToken cancellationToken = default);

    Task<SamlIdentityProviderConfigurationDto> AddToCurrentTenantAsync(
        SamlIdentityProviderConfiguration configuration,
        CancellationToken cancellationToken = default);

    Task<SamlIdentityProviderConfigurationDto?> UpdateStatusInCurrentTenantScopeAsync(
        Guid configurationId,
        SamlConfigurationStatus status,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<SamlIdentityProviderConfigurationDto?> UpdateTestResultInCurrentTenantScopeAsync(
        Guid configurationId,
        SamlTestResult result,
        string diagnosticSummary,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<SamlIdentityProviderConfigurationDto?> RotateCertificateInCurrentTenantScopeAsync(
        Guid configurationId,
        string certificatePem,
        string? certificateFingerprint,
        DateTimeOffset certificateExpiresAt,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed record SamlIdentityProviderConfiguration(
    Guid Id,
    Guid TenantId,
    string EntityId,
    string SsoUrl,
    string? CertificatePem,
    string? CertificateFingerprint,
    DateTimeOffset CertificateExpiresAt,
    SamlSigningRequirement SigningRequirement,
    SamlNameIdFormat NameIdFormat,
    IReadOnlyDictionary<string, string> AttributeMappings,
    SamlConfigurationStatus Status,
    string? MetadataUrl,
    string CallbackUrl,
    DateTimeOffset? LastTestedAt,
    SamlTestResult? LastTestResult,
    string? LastTestDiagnosticSummary,
    DateTimeOffset CreatedAt,
    Guid CreatedByUserId,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedByUserId);

public sealed record SamlIdentityProviderConfigurationDto(
    Guid Id,
    Guid TenantId,
    string EntityId,
    string SsoUrl,
    string? CertificateFingerprint,
    DateTimeOffset CertificateExpiresAt,
    SamlSigningRequirement SigningRequirement,
    SamlNameIdFormat NameIdFormat,
    IReadOnlyDictionary<string, string> AttributeMappings,
    SamlConfigurationStatus Status,
    string? MetadataUrl,
    string CallbackUrl,
    DateTimeOffset? LastTestedAt,
    SamlTestResult? LastTestResult,
    string? LastTestDiagnosticSummary,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record UpsertSamlIdentityProviderConfigurationRequest(
    string EntityId,
    string SsoUrl,
    string? CertificatePem,
    DateTimeOffset CertificateExpiresAt,
    SamlSigningRequirement SigningRequirement,
    SamlNameIdFormat NameIdFormat,
    IReadOnlyDictionary<string, string> AttributeMappings,
    string? MetadataUrl,
    string CallbackUrl);

public sealed record RotateSamlCertificateRequest(
    string CertificatePem,
    DateTimeOffset CertificateExpiresAt);

public enum SamlConfigurationStatus
{
    Draft,
    Enabled,
    Disabled,
    Rotated,
    Archived
}

public enum SamlSigningRequirement
{
    SignedAssertions,
    SignedResponses,
    SignedResponsesAndAssertions
}

public enum SamlNameIdFormat
{
    EmailAddress,
    Persistent,
    Transient,
    Unspecified
}

public enum SamlTestResult
{
    Success,
    Warning,
    Failure
}

public sealed class SamlConfigurationValidationException(string message) : InvalidOperationException(message);
