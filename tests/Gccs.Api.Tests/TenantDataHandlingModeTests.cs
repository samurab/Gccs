using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Security;
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

public sealed class TenantDataHandlingModeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public TenantDataHandlingModeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_1A_1_1_1_Tenant_has_one_active_data_handling_mode_and_history()
    {
        var actorUserId = Guid.Parse("1a111111-1111-1111-1111-111111111111");
        await using var factory = CreateFactory("tc-1a-1-1-1");
        using var client = factory.CreateClient();
        using var createRequest = CreateRequest(
            HttpMethod.Post,
            "/api/tenants",
            new CreateTenantRequest("TC-1A.1.1.1 Tenant"),
            Guid.Parse("1a111111-1111-1111-1111-111111111110"),
            actorUserId,
            Permission.ManageTenant);

        var createResponse = await client.SendAsync(createRequest);
        var createdTenant = await createResponse.Content.ReadFromJsonAsync<TenantDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createdTenant);
        Assert.Equal(TenantDataPosture.NoCui, createdTenant.DataPosture);
        Assert.Equal(TenantDataPosture.NoCui, createdTenant.DataHandlingMode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var persistedTenant = await dbContext.Tenants.SingleAsync(candidate => candidate.Id == createdTenant.Id);
        var history = await dbContext.TenantDataHandlingModeHistory.SingleAsync(candidate => candidate.TenantId == createdTenant.Id);

        Assert.Equal(TenantDataPosture.NoCui, persistedTenant.DataPosture);
        Assert.Null(history.PreviousMode);
        Assert.Equal(TenantDataPosture.NoCui, history.NewMode);
        Assert.Equal(actorUserId, history.ActorUserId);
    }

    [Fact]
    public async Task TC_1A_1_1_2_New_tenants_default_to_no_cui_and_explicit_demo_sandbox_is_allowed()
    {
        await using var factory = CreateFactory("tc-1a-1-1-2");
        using var client = factory.CreateClient();

        using var defaultRequest = CreateRequest(
            HttpMethod.Post,
            "/api/tenants",
            new CreateTenantRequest("TC-1A.1.1.2 Default Tenant"),
            Guid.Parse("1a112222-2222-2222-2222-222222222220"),
            Guid.Parse("1a112222-2222-2222-2222-222222222221"),
            Permission.ManageTenant);
        using var demoRequest = CreateRequest(
            HttpMethod.Post,
            "/api/tenants",
            new CreateTenantRequest(
                "TC-1A.1.1.2 Demo Tenant",
                DataHandlingMode: TenantDataPosture.DemoSandbox,
                DataHandlingModeReason: "Seeded demo tenant for synthetic CUI workflows."),
            Guid.Parse("1a112222-2222-2222-2222-222222222222"),
            Guid.Parse("1a112222-2222-2222-2222-222222222223"),
            Permission.ManageTenant);

        var defaultResponse = await client.SendAsync(defaultRequest);
        var demoResponse = await client.SendAsync(demoRequest);
        var defaultTenant = await defaultResponse.Content.ReadFromJsonAsync<TenantDto>(JsonOptions);
        var demoTenant = await demoResponse.Content.ReadFromJsonAsync<TenantDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, defaultResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, demoResponse.StatusCode);
        Assert.NotNull(defaultTenant);
        Assert.NotNull(demoTenant);
        Assert.Equal(TenantDataPosture.NoCui, defaultTenant.DataHandlingMode);
        Assert.Equal(TenantDataPosture.DemoSandbox, demoTenant.DataHandlingMode);
    }

    [Fact]
    public async Task TC_1A_1_1_3_CuiReady_cannot_be_assigned_without_approval_reference()
    {
        var tenantId = Guid.Parse("1a113333-3333-3333-3333-333333333331");
        await using var factory = CreateFactory("tc-1a-1-1-3", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenantEntity(tenantId, "TC-1A.1.1.3 Tenant"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            HttpMethod.Patch,
            $"/api/tenants/{tenantId}/data-handling-mode",
            new UpdateTenantDataHandlingModeRequest(TenantDataPosture.CuiReady, "Attempt CUI enablement without checklist."),
            tenantId,
            Guid.Parse("1a113333-3333-3333-3333-333333333332"),
            Permission.ManageTenant);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("approved CUI-ready checklist reference", body, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var tenant = await dbContext.Tenants.SingleAsync(candidate => candidate.Id == tenantId);

        Assert.Equal(TenantDataPosture.NoCui, tenant.DataPosture);
        Assert.Empty(dbContext.TenantDataHandlingModeHistory.Where(candidate => candidate.TenantId == tenantId));
    }

    [Fact]
    public async Task TC_1A_1_1_4_Mode_changes_persist_history_and_audit_metadata()
    {
        var tenantId = Guid.Parse("1a114444-4444-4444-4444-444444444441");
        var actorUserId = Guid.Parse("1a114444-4444-4444-4444-444444444442");
        await using var factory = CreateFactory("tc-1a-1-1-4", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenantEntity(tenantId, "TC-1A.1.1.4 Tenant"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            HttpMethod.Patch,
            $"/api/tenants/{tenantId}/data-handling-mode",
            new UpdateTenantDataHandlingModeRequest(TenantDataPosture.DemoSandbox, "Switch to synthetic demo workflow."),
            tenantId,
            actorUserId,
            Permission.ManageTenant);

        var response = await client.SendAsync(request);
        var updatedTenant = await response.Content.ReadFromJsonAsync<TenantDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(updatedTenant);
        Assert.Equal(TenantDataPosture.DemoSandbox, updatedTenant.DataHandlingMode);

        using var historyRequest = CreateRequest(
            HttpMethod.Get,
            $"/api/tenants/{tenantId}/data-handling-mode/history",
            tenantId,
            actorUserId,
            Permission.ManageTenant);
        var historyResponse = await client.SendAsync(historyRequest);
        var history = await historyResponse.Content.ReadFromJsonAsync<TenantDataHandlingModeHistoryDto[]>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        Assert.NotNull(history);
        var historyItem = Assert.Single(history);
        Assert.Equal(TenantDataPosture.NoCui, historyItem.PreviousMode);
        Assert.Equal(TenantDataPosture.DemoSandbox, historyItem.NewMode);
        Assert.Equal(actorUserId, historyItem.ActorUserId);
        Assert.Equal("Switch to synthetic demo workflow.", historyItem.Reason);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var auditEvent = await dbContext.AuditLogEntries.SingleAsync(candidate =>
            candidate.EntityId == tenantId.ToString() &&
            candidate.Action == AuditAction.Updated &&
            candidate.Summary.Contains("data handling mode"));
        var metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(auditEvent.MetadataJson) ?? [];

        Assert.Equal(tenantId, auditEvent.TenantId);
        Assert.Equal(actorUserId, auditEvent.ActorUserId);
        Assert.Equal("NoCui", metadata["beforeDataHandlingMode"]);
        Assert.Equal("DemoSandbox", metadata["afterDataHandlingMode"]);
        Assert.Equal("Switch to synthetic demo workflow.", metadata["reason"]);
    }

    [Fact]
    public async Task TC_1A_1_1_5_Current_tenant_mode_is_available_to_workflow_services()
    {
        var tenantId = Guid.Parse("1a115555-5555-5555-5555-555555555551");
        await using var factory = CreateFactory("tc-1a-1-1-5", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenantEntity(tenantId, "TC-1A.1.1.5 Tenant", TenantDataPosture.DemoSandbox));
            dbContext.SaveChanges();
        });

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var service = new TenantService(
            new EfTenantRepository(dbContext, new FixedTenantContext(tenantId, Guid.Parse("1a115555-5555-5555-5555-555555555552"))),
            scope.ServiceProvider.GetRequiredService<IAuditEventWriter>());

        var dataHandlingMode = await service.FindCurrentTenantDataHandlingModeAsync();

        Assert.Equal(TenantDataPosture.DemoSandbox, dataHandlingMode);
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
        request.Headers.Add("X-Gccs-Dev-Permissions", permission.ToString());

        return request;
    }

    private static TenantEntity CreateTenantEntity(
        Guid tenantId,
        string name,
        TenantDataPosture dataHandlingMode = TenantDataPosture.NoCui) =>
        new()
        {
            Id = tenantId,
            Name = name,
            Status = TenantStatus.Active,
            DataPosture = dataHandlingMode,
            CreatedAt = DateTimeOffset.Parse("2026-06-18T12:00:00Z")
        };

    private sealed record FixedTenantContext(Guid TenantId, Guid UserId) : ICurrentTenantContext
    {
        public string UserEmail => "phase1a-mode-test@example.com";
    }
}
