using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Gccs.Application.Audit;
using Gccs.Application.Identity;
using Gccs.Application.Tenancy;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Identity;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class SecurityBoundaryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SecurityBoundaryTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
        });
    }

    [Fact]
    public async Task Health_is_public()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains(
            response.StatusCode,
            new[] { HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable });
    }

    [Fact]
    public async Task Api_routes_require_authentication()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/compliance/overview");
        request.Headers.Add("X-Correlation-ID", "tc-3-1-unauthenticated");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("tc-3-1-unauthenticated", response.Headers.GetValues("X-Correlation-ID").Single());
        Assert.Contains("authentication_required", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tc-3-1-unauthenticated", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Development_auth_allows_authenticated_api_access_and_resolves_current_context()
    {
        var tenantId = Guid.Parse("33333333-3333-3333-3333-333333333331");
        var userId = Guid.Parse("44444444-4444-4444-4444-444444444441");
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/me/access");
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        request.Headers.Add("X-Gccs-Dev-Email", "story-3-1@example.com");
        request.Headers.Add("X-Correlation-ID", "tc-3-1-context");

        var response = await client.SendAsync(request);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("tc-3-1-context", response.Headers.GetValues("X-Correlation-ID").Single());
        Assert.Equal(tenantId, payload.RootElement.GetProperty("tenantId").GetGuid());
        Assert.Equal(userId, payload.RootElement.GetProperty("userId").GetGuid());
        Assert.Equal("story-3-1@example.com", payload.RootElement.GetProperty("userEmail").GetString());
    }

    [Fact]
    public async Task Authenticated_permissioned_api_request_preserves_tenant_scoped_no_cui_response_shape()
    {
        var tenantId = Guid.Parse("33333333-3333-3333-3333-333333333332");
        var userId = Guid.Parse("44444444-4444-4444-4444-444444444442");
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/compliance/overview");
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", "ViewObligations");
        request.Headers.Add("X-Correlation-ID", "tc-3-1-protected-overview");

        var response = await client.SendAsync(request);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("tc-3-1-protected-overview", response.Headers.GetValues("X-Correlation-ID").Single());
        Assert.Equal(tenantId, payload.RootElement.GetProperty("tenantId").GetGuid());
        Assert.Equal(0, payload.RootElement.GetProperty("controlsTotal").GetInt32());
        Assert.Equal(0, payload.RootElement.GetProperty("evidenceItems").GetInt32());
        Assert.Empty(payload.RootElement.GetProperty("recentAuditEvents").EnumerateArray());
    }

    [Fact]
    public async Task Authenticated_api_request_without_tenant_returns_standard_missing_tenant_error()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/me/access");
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", "none");
        request.Headers.Add("X-Correlation-ID", "tc-3-1-missing-tenant");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("tc-3-1-missing-tenant", response.Headers.GetValues("X-Correlation-ID").Single());
        Assert.Contains("Tenant context required", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("missing_tenant_context", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tc-3-1-missing-tenant", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Permission_policy_rejects_missing_permission()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/obligations");
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Permissions", "ManageTenant");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Enforced_membership_authorization_rejects_user_without_active_membership()
    {
        await using var factory = CreateMembershipFactory("security-membership-missing");
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/compliance/overview");
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", Guid.Parse("33333333-3333-3333-3333-333333333333").ToString());
        request.Headers.Add("X-Gccs-Dev-User", Guid.Parse("44444444-4444-4444-4444-444444444443").ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", Permission.ViewObligations.ToString());

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("tenant_membership_required", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Enforced_membership_authorization_uses_database_role_instead_of_token_permissions()
    {
        var tenantId = Guid.Parse("33333333-3333-3333-3333-333333333334");
        var userId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        await using var factory = CreateMembershipFactory("security-membership-role", dbContext =>
        {
            SeedTenantMember(dbContext, tenantId, userId, RoleCatalog.Auditor);
        });
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/me/access");
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        request.Headers.Add("X-Gccs-Dev-Role", RoleCatalog.Owner);
        request.Headers.Add("X-Gccs-Dev-Permissions", Permission.ManageTenant.ToString());

        var response = await client.SendAsync(request);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var roles = payload.RootElement.GetProperty("roles").EnumerateArray().Select(role => role.GetString()).ToArray();
        var permissions = payload.RootElement.GetProperty("permissions").EnumerateArray().Select(permission => permission.GetString()).ToArray();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(RoleCatalog.Auditor, roles);
        Assert.DoesNotContain(RoleCatalog.Owner, roles);
        Assert.Contains(Permission.AuditorReadOnly.ToString(), permissions);
        Assert.DoesNotContain(Permission.ManageTenant.ToString(), permissions);
    }

    [Fact]
    public async Task Route_tenant_scope_rejects_mismatched_tenant_id_before_endpoint_logic()
    {
        var tenantId = Guid.Parse("33333333-3333-3333-3333-333333333335");
        var routeTenantId = Guid.Parse("33333333-3333-3333-3333-333333333336");
        await using var factory = CreatePersistenceFactory("security-route-tenant-mismatch");
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/tenants/{routeTenantId}/data-handling-mode/history");
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", Guid.Parse("44444444-4444-4444-4444-444444444445").ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", Permission.ManageTenant.ToString());

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("tenant_scope_mismatch", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Compliance_relevant_audit_events_include_request_correlation_id()
    {
        const string correlationId = "tc-3-1-audit-correlation";
        await using var factory = CreatePersistenceFactory("tc-3-1-audit-correlation");
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/tenants");
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-User", "55555555-5555-5555-5555-555555555551");
        request.Headers.Add("X-Correlation-ID", correlationId);
        request.Content = JsonContent.Create(new CreateTenantRequest("Story 3.1 Correlation Tenant"));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var auditEntry = await dbContext.AuditLogEntries.SingleAsync();

        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-ID").Single());
        Assert.Contains(correlationId, auditEntry.MetadataJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Failed_api_responses_and_logs_include_request_correlation_id()
    {
        const string correlationId = "tc-3-1-failed-log-correlation";
        var loggerProvider = new CapturingLoggerProvider();
        await using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.AddProvider(loggerProvider));
        });
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/compliance/overview");
        request.Headers.Add("X-Correlation-ID", correlationId);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        var failureLog = Assert.Single(loggerProvider.Entries, entry =>
            entry.Category == "Gccs.Api.Security.ApiFailureLogging" &&
            entry.Properties.TryGetValue("CorrelationId", out var loggedCorrelationId) &&
            loggedCorrelationId == correlationId);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-ID").Single());
        Assert.Contains(correlationId, body, StringComparison.Ordinal);
        Assert.Equal(LogLevel.Warning, failureLog.Level);
        Assert.Equal("anonymous", failureLog.Properties["UserId"]);
        Assert.Equal("none", failureLog.Properties["TenantId"]);
    }

    private WebApplicationFactory<Program> CreatePersistenceFactory(string databaseName) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<TenantService>();
                services.AddScoped<ITenantRepository, EfTenantRepository>();
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();
            });
        });

    private WebApplicationFactory<Program> CreateMembershipFactory(
        string databaseName,
        Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.UseSetting("Security:MembershipAuthorization:Enforce", "true");
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<ITenantMembershipRepository, EfTenantMembershipRepository>();

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
                seed?.Invoke(dbContext);
                dbContext.SaveChanges();
            });
        });

    private static void SeedTenantMember(GccsDbContext dbContext, Guid tenantId, Guid userId, string roleName)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = $"Security Tenant {tenantId:N}",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.Parse("2026-06-23T12:00:00Z")
        });
        dbContext.Users.Add(new UserEntity
        {
            Id = userId,
            TenantId = tenantId,
            Email = $"user-{userId:N}@example.com",
            DisplayName = "Security Test User",
            Status = UserStatus.Active,
            MfaEnabled = true,
            CreatedAt = DateTimeOffset.Parse("2026-06-23T12:00:00Z")
        });
        dbContext.TenantMemberships.Add(new TenantMembershipEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Status = MembershipStatus.Active,
            RoleName = roleName,
            CreatedAt = DateTimeOffset.Parse("2026-06-23T12:00:00Z")
        });
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly List<CapturedLogEntry> _entries = [];
        private readonly object _lock = new();

        public IReadOnlyList<CapturedLogEntry> Entries
        {
            get
            {
                lock (_lock)
                {
                    return _entries.ToArray();
                }
            }
        }

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, this);

        public void Dispose()
        {
        }

        private void Add(CapturedLogEntry entry)
        {
            lock (_lock)
            {
                _entries.Add(entry);
            }
        }

        private sealed class CapturingLogger(string categoryName, CapturingLoggerProvider provider) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull =>
                null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                var properties = state is IEnumerable<KeyValuePair<string, object?>> structuredState
                    ? structuredState.ToDictionary(
                        pair => pair.Key,
                        pair => pair.Value?.ToString() ?? string.Empty,
                        StringComparer.Ordinal)
                    : new Dictionary<string, string>(StringComparer.Ordinal);

                provider.Add(new CapturedLogEntry(
                    categoryName,
                    logLevel,
                    formatter(state, exception),
                    properties));
            }
        }
    }

    private sealed record CapturedLogEntry(
        string Category,
        LogLevel Level,
        string Message,
        IReadOnlyDictionary<string, string> Properties);
}
