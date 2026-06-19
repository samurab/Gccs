using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Identity;
using Gccs.Domain.Audit;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Identity;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class SamlIdentityProviderConfigurationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public SamlIdentityProviderConfigurationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_35_1_1_Admin_creates_and_tests_saml_configuration()
    {
        var tenantId = Guid.Parse("35135135-1351-3513-5135-1351351351a1");
        var actorUserId = Guid.Parse("35135135-1351-3513-5135-1351351351b1");
        await using var factory = CreateFactory("tc-35-1-1", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-35.1.1 Tenant"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();

        var created = await CreateConfigurationAsync(client, tenantId, actorUserId);
        using var testRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/enterprise/saml-configurations/{created.Id}/test",
            tenantId,
            actorUserId,
            Permission.ManageUsers);

        var testResponse = await client.SendAsync(testRequest);
        var tested = await testResponse.Content.ReadFromJsonAsync<SamlIdentityProviderConfigurationDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, testResponse.StatusCode);
        Assert.NotNull(tested);
        Assert.Equal(SamlTestResult.Success, tested.LastTestResult);
        Assert.NotNull(tested.LastTestedAt);
        Assert.Contains("validated", tested.LastTestDiagnosticSummary, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(tested.CertificateFingerprint);
        Assert.DoesNotContain("BEGIN CERTIFICATE", JsonSerializer.Serialize(tested, JsonOptions), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_35_1_2_Invalid_enablement_is_rejected()
    {
        var tenantId = Guid.Parse("35135135-1351-3513-5135-1351351351a2");
        var actorUserId = Guid.Parse("35135135-1351-3513-5135-1351351351b2");
        var expiredId = Guid.Parse("35135135-1351-3513-5135-1351351351c2");
        var invalidCallbackId = Guid.Parse("35135135-1351-3513-5135-1351351351d2");
        var untestedId = Guid.Parse("35135135-1351-3513-5135-1351351351e2");
        await using var factory = CreateFactory("tc-35-1-2", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-35.1.2 Tenant"));
            dbContext.SamlIdentityProviderConfigurations.AddRange(
                CreateEntity(expiredId, tenantId, "https://idp.example.test/expired", CertificateExpiresAt: DateTimeOffset.UtcNow.AddDays(-1), LastTestResult: SamlTestResult.Success),
                CreateEntity(invalidCallbackId, tenantId, "https://idp.example.test/bad-callback", CallbackUrl: "https://app.gccs.local/api/saml/other-tenant/acs", LastTestResult: SamlTestResult.Success),
                CreateEntity(untestedId, tenantId, "https://idp.example.test/untested"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();

        var expiredResponse = await client.SendAsync(CreateRequest(HttpMethod.Post, $"/api/enterprise/saml-configurations/{expiredId}/enable", tenantId, actorUserId, Permission.ManageUsers));
        var callbackResponse = await client.SendAsync(CreateRequest(HttpMethod.Post, $"/api/enterprise/saml-configurations/{invalidCallbackId}/enable", tenantId, actorUserId, Permission.ManageUsers));
        var untestedResponse = await client.SendAsync(CreateRequest(HttpMethod.Post, $"/api/enterprise/saml-configurations/{untestedId}/enable", tenantId, actorUserId, Permission.ManageUsers));

        Assert.Equal(HttpStatusCode.BadRequest, expiredResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, callbackResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, untestedResponse.StatusCode);
        Assert.Contains("expired", await expiredResponse.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("current tenant", await callbackResponse.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("connection test", await untestedResponse.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_35_1_3_Failed_test_diagnostics_do_not_expose_secrets()
    {
        var tenantId = Guid.Parse("35135135-1351-3513-5135-1351351351a3");
        var actorUserId = Guid.Parse("35135135-1351-3513-5135-1351351351b3");
        await using var factory = CreateFactory("tc-35-1-3", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-35.1.3 Tenant"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        var created = await CreateConfigurationAsync(
            client,
            tenantId,
            actorUserId,
            entityId: "https://idp.example.test/failure",
            ssoUrl: "https://unreachable-idp.example.test/saml/sso");

        var response = await client.SendAsync(CreateRequest(HttpMethod.Post, $"/api/enterprise/saml-configurations/{created.Id}/test", tenantId, actorUserId, Permission.ManageUsers));
        var tested = await response.Content.ReadFromJsonAsync<SamlIdentityProviderConfigurationDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(tested);
        Assert.Equal(SamlTestResult.Failure, tested.LastTestResult);
        Assert.Contains("could not be reached", tested.LastTestDiagnosticSummary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("BEGIN CERTIFICATE", tested.LastTestDiagnosticSummary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Bearer", tested.LastTestDiagnosticSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_35_1_4_Disabled_and_archived_configurations_are_unusable_for_sign_in()
    {
        var tenantId = Guid.Parse("35135135-1351-3513-5135-1351351351a4");
        var actorUserId = Guid.Parse("35135135-1351-3513-5135-1351351351b4");
        await using var factory = CreateFactory("tc-35-1-4", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-35.1.4 Tenant"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        var created = await CreateConfigurationAsync(client, tenantId, actorUserId);
        await client.SendAsync(CreateRequest(HttpMethod.Post, $"/api/enterprise/saml-configurations/{created.Id}/test", tenantId, actorUserId, Permission.ManageUsers));

        var enableResponse = await client.SendAsync(CreateRequest(HttpMethod.Post, $"/api/enterprise/saml-configurations/{created.Id}/enable", tenantId, actorUserId, Permission.ManageUsers));
        var disableResponse = await client.SendAsync(CreateRequest(HttpMethod.Post, $"/api/enterprise/saml-configurations/{created.Id}/disable", tenantId, actorUserId, Permission.ManageUsers));
        var archiveResponse = await client.SendAsync(CreateRequest(HttpMethod.Post, $"/api/enterprise/saml-configurations/{created.Id}/archive", tenantId, actorUserId, Permission.ManageUsers));
        var reenableArchivedResponse = await client.SendAsync(CreateRequest(HttpMethod.Post, $"/api/enterprise/saml-configurations/{created.Id}/enable", tenantId, actorUserId, Permission.ManageUsers));
        var enabled = await enableResponse.Content.ReadFromJsonAsync<SamlIdentityProviderConfigurationDto>(JsonOptions);
        var disabled = await disableResponse.Content.ReadFromJsonAsync<SamlIdentityProviderConfigurationDto>(JsonOptions);
        var archived = await archiveResponse.Content.ReadFromJsonAsync<SamlIdentityProviderConfigurationDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, enableResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, disableResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, archiveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, reenableArchivedResponse.StatusCode);
        Assert.Equal(SamlConfigurationStatus.Enabled, enabled?.Status);
        Assert.Equal(SamlConfigurationStatus.Disabled, disabled?.Status);
        Assert.Equal(SamlConfigurationStatus.Archived, archived?.Status);
    }

    [Fact]
    public async Task TC_35_1_5_Lifecycle_actions_are_audit_logged()
    {
        var tenantId = Guid.Parse("35135135-1351-3513-5135-1351351351a5");
        var actorUserId = Guid.Parse("35135135-1351-3513-5135-1351351351b5");
        await using var factory = CreateFactory("tc-35-1-5", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-35.1.5 Tenant"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();

        var created = await CreateConfigurationAsync(client, tenantId, actorUserId);
        await client.SendAsync(CreateRequest(HttpMethod.Post, $"/api/enterprise/saml-configurations/{created.Id}/test", tenantId, actorUserId, Permission.ManageUsers));
        await client.SendAsync(CreateRequest(HttpMethod.Post, $"/api/enterprise/saml-configurations/{created.Id}/enable", tenantId, actorUserId, Permission.ManageUsers));
        await client.SendAsync(CreateRequest(HttpMethod.Post, $"/api/enterprise/saml-configurations/{created.Id}/disable", tenantId, actorUserId, Permission.ManageUsers));
        await client.SendAsync(CreateRequest(HttpMethod.Post, $"/api/enterprise/saml-configurations/{created.Id}/rotate-certificate", new RotateSamlCertificateRequest(TestCertificate("rotated"), DateTimeOffset.UtcNow.AddYears(3)), tenantId, actorUserId, Permission.ManageUsers));
        await client.SendAsync(CreateRequest(HttpMethod.Post, $"/api/enterprise/saml-configurations/{created.Id}/archive", tenantId, actorUserId, Permission.ManageUsers));

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var auditEvents = await dbContext.AuditLogEntries
            .Where(candidate => candidate.TenantId == tenantId && candidate.EntityType == "SamlIdentityProviderConfiguration")
            .OrderBy(candidate => candidate.OccurredAt)
            .ToListAsync();

        Assert.Equal(6, auditEvents.Count);
        Assert.Contains(auditEvents, audit => audit.Action == AuditAction.Created);
        Assert.Contains(auditEvents, audit => audit.Summary.Contains("test completed", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(auditEvents, audit => audit.Summary.Contains("Enabled", StringComparison.Ordinal));
        Assert.Contains(auditEvents, audit => audit.Summary.Contains("Disabled", StringComparison.Ordinal));
        Assert.Contains(auditEvents, audit => audit.Summary.Contains("rotated", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(auditEvents, audit => audit.Summary.Contains("Archived", StringComparison.Ordinal));
        Assert.All(auditEvents, audit => Assert.DoesNotContain("BEGIN CERTIFICATE", audit.MetadataJson, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<SamlIdentityProviderConfigurationDto> CreateConfigurationAsync(
        HttpClient client,
        Guid tenantId,
        Guid actorUserId,
        string entityId = "https://idp.example.test/metadata",
        string ssoUrl = "https://idp.example.test/saml/sso")
    {
        using var request = CreateRequest(
            HttpMethod.Post,
            "/api/enterprise/saml-configurations",
            CreateRequestBody(tenantId, entityId, ssoUrl),
            tenantId,
            actorUserId,
            Permission.ManageUsers);

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var configuration = await response.Content.ReadFromJsonAsync<SamlIdentityProviderConfigurationDto>(JsonOptions);
        return Assert.IsType<SamlIdentityProviderConfigurationDto>(configuration);
    }

    private WebApplicationFactory<Program> CreateFactory(
        string databaseName,
        Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<SamlIdentityProviderConfigurationService>();
                services.AddScoped<ISamlIdentityProviderConfigurationRepository, EfSamlIdentityProviderConfigurationRepository>();
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
                seed?.Invoke(dbContext);
            });
        });

    private static HttpRequestMessage CreateRequest<TContent>(
        HttpMethod method,
        string requestUri,
        TContent? content,
        Guid tenantId,
        Guid userId,
        Permission permission)
    {
        var request = CreateRequest(method, requestUri, tenantId, userId, permission);
        if (content is not null)
        {
            request.Content = JsonContent.Create(content, options: JsonOptions);
        }

        return request;
    }

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        string requestUri,
        Guid tenantId,
        Guid userId,
        Permission permission)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        request.Headers.Add("X-Gccs-Dev-Email", "admin@example.com");
        request.Headers.Add("X-Gccs-Dev-Permissions", permission.ToString());
        return request;
    }

    private static UpsertSamlIdentityProviderConfigurationRequest CreateRequestBody(
        Guid tenantId,
        string entityId = "https://idp.example.test/metadata",
        string ssoUrl = "https://idp.example.test/saml/sso") =>
        new(
            entityId,
            ssoUrl,
            TestCertificate("created"),
            DateTimeOffset.UtcNow.AddYears(2),
            SamlSigningRequirement.SignedResponsesAndAssertions,
            SamlNameIdFormat.EmailAddress,
            new Dictionary<string, string>
            {
                ["email"] = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress",
                ["displayName"] = "displayName"
            },
            "https://idp.example.test/metadata.xml",
            $"https://app.gccs.local/api/saml/{tenantId}/acs");

    private static TenantEntity CreateTenant(Guid tenantId, string name) =>
        new()
        {
            Id = tenantId,
            Name = name,
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.Parse("2026-06-19T12:00:00Z")
        };

    private static SamlIdentityProviderConfigurationEntity CreateEntity(
        Guid id,
        Guid tenantId,
        string entityId,
        string SsoUrl = "https://idp.example.test/saml/sso",
        string? CallbackUrl = null,
        DateTimeOffset? CertificateExpiresAt = null,
        SamlTestResult? LastTestResult = null) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            EntityId = entityId,
            SsoUrl = SsoUrl,
            CertificatePem = TestCertificate("seeded"),
            CertificateFingerprint = "seeded-fingerprint",
            CertificateExpiresAt = CertificateExpiresAt ?? DateTimeOffset.UtcNow.AddYears(1),
            SigningRequirement = SamlSigningRequirement.SignedAssertions,
            NameIdFormat = SamlNameIdFormat.EmailAddress,
            AttributeMappingsJson = "{\"email\":\"email\"}",
            Status = SamlConfigurationStatus.Draft,
            MetadataUrl = "https://idp.example.test/metadata.xml",
            CallbackUrl = CallbackUrl ?? $"https://app.gccs.local/api/saml/{tenantId}/acs",
            LastTestedAt = LastTestResult.HasValue ? DateTimeOffset.UtcNow.AddMinutes(-5) : null,
            LastTestResult = LastTestResult,
            LastTestDiagnosticSummary = LastTestResult.HasValue ? "Seeded test result." : null,
            CreatedAt = DateTimeOffset.Parse("2026-06-19T12:00:00Z"),
            CreatedByUserId = Guid.Parse("35135135-1351-3513-5135-135135135199")
        };

    private static string TestCertificate(string marker) =>
        $"-----BEGIN CERTIFICATE-----\\nsynthetic-public-certificate-{marker}\\n-----END CERTIFICATE-----";
}
