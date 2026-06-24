using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class TenantCreationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public TenantCreationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Platform_admin_can_create_no_cui_tenant_and_creation_is_audit_logged()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(databaseName);
        using var client = factory.CreateClient();
        var actorUserId = Guid.NewGuid();

        using var request = CreateRequest(
            HttpMethod.Post,
            "/api/tenants",
            new CreateTenantRequest("Acme Federal Services"),
            Guid.NewGuid(),
            actorUserId,
            Permission.ManageTenant);

        var response = await client.SendAsync(request);
        var tenant = await response.Content.ReadFromJsonAsync<TenantDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(tenant);
        Assert.Equal("Acme Federal Services", tenant.DisplayName);
        Assert.Equal(TenantStatus.Active, tenant.Status);
        Assert.Equal(TenantDataPosture.NoCui, tenant.DataPosture);
        Assert.NotEqual(Guid.Empty, tenant.Id);
        Assert.True(tenant.CreatedAt <= DateTimeOffset.UtcNow);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var persistedTenant = await dbContext.Tenants.SingleAsync(candidate => candidate.Id == tenant.Id);
        var auditEvent = await dbContext.AuditLogEntries.SingleAsync(candidate => candidate.EntityId == tenant.Id.ToString());

        Assert.Equal(tenant.Id, persistedTenant.Id);
        Assert.Equal(actorUserId, persistedTenant.CreatedByUserId);
        Assert.Equal(tenant.Id, auditEvent.TenantId);
        Assert.Equal(actorUserId, auditEvent.ActorUserId);
        Assert.Equal(AuditAction.Created, auditEvent.Action);
        Assert.Equal("Tenant", auditEvent.EntityType);
        Assert.Contains("created", auditEvent.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Tenant_reads_are_scoped_to_authenticated_tenant()
    {
        var tenantOneId = Guid.NewGuid();
        var tenantTwoId = Guid.NewGuid();
        var databaseName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(databaseName, dbContext =>
        {
            dbContext.Tenants.AddRange(
                CreateTenantEntity(tenantOneId, "Tenant One"),
                CreateTenantEntity(tenantTwoId, "Tenant Two"));
            dbContext.SaveChanges();
        });

        using var client = factory.CreateClient();
        using var ownTenantRequest = CreateRequest(
            HttpMethod.Get,
            $"/api/tenants/{tenantOneId}",
            tenantOneId,
            Guid.NewGuid(),
            Permission.ManageTenant);
        using var otherTenantRequest = CreateRequest(
            HttpMethod.Get,
            $"/api/tenants/{tenantTwoId}",
            tenantOneId,
            Guid.NewGuid(),
            Permission.ManageTenant);

        var ownTenantResponse = await client.SendAsync(ownTenantRequest);
        var otherTenantResponse = await client.SendAsync(otherTenantRequest);
        var ownTenant = await ownTenantResponse.Content.ReadFromJsonAsync<TenantDto>(JsonOptions);
        var otherTenantBody = await otherTenantResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, ownTenantResponse.StatusCode);
        Assert.NotNull(ownTenant);
        Assert.Equal(tenantOneId, ownTenant.Id);
        Assert.Equal(HttpStatusCode.Forbidden, otherTenantResponse.StatusCode);
        Assert.Contains("tenant_scope_mismatch", otherTenantBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(tenantTwoId.ToString(), otherTenantBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Tenant_status_change_is_scoped_and_audit_logged()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var databaseName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(databaseName, dbContext =>
        {
            dbContext.Tenants.Add(CreateTenantEntity(tenantId, "Acme Federal Services"));
            dbContext.SaveChanges();
        });

        using var client = factory.CreateClient();
        using var request = CreateRequest(
            HttpMethod.Patch,
            $"/api/tenants/{tenantId}/status",
            new UpdateTenantStatusRequest(TenantStatus.Suspended),
            tenantId,
            actorUserId,
            Permission.ManageTenant);

        var response = await client.SendAsync(request);
        var tenant = await response.Content.ReadFromJsonAsync<TenantDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(tenant);
        Assert.Equal(TenantStatus.Suspended, tenant.Status);
        Assert.NotNull(tenant.UpdatedAt);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var auditEvent = await dbContext.AuditLogEntries.SingleAsync(candidate =>
            candidate.EntityId == tenantId.ToString() &&
            candidate.Action == AuditAction.Updated);

        Assert.Equal(tenantId, auditEvent.TenantId);
        Assert.Equal(actorUserId, auditEvent.ActorUserId);
        Assert.Contains("status", auditEvent.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Tenant_creation_requires_manage_tenant_permission()
    {
        await using var factory = CreateFactory(Guid.NewGuid().ToString());
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            HttpMethod.Post,
            "/api/tenants",
            new CreateTenantRequest("Acme Federal Services"),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Permission.AuditorReadOnly);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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
                services.AddScoped<TenantService>();
                services.AddScoped<ITenantRepository, EfTenantRepository>();
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
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", permission.ToString());

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
        request.Headers.Add("X-Gccs-Dev-Permissions", permission.ToString());

        return request;
    }

    private static TenantEntity CreateTenantEntity(Guid tenantId, string name) =>
        new()
        {
            Id = tenantId,
            Name = name,
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        };
}
