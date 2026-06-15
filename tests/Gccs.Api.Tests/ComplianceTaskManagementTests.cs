using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Security;
using Gccs.Application.Tasks;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ComplianceTaskManagementTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public ComplianceTaskManagementTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_11_1_1_Create_tasks_linked_to_supported_compliance_entities()
    {
        var tenantId = Guid.Parse("11111111-1111-1111-1111-1111111111a1");
        await using var factory = CreateFactory("tc-11-1-1", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        var links = new[]
        {
            ("obligation", "far-52-204-21"),
            ("contract", Guid.NewGuid().ToString()),
            ("control", "AC.L1-3.1.1"),
            ("evidence", Guid.NewGuid().ToString()),
            ("subcontractor", Guid.NewGuid().ToString()),
            ("certification", Guid.NewGuid().ToString())
        };

        foreach (var (type, id) in links)
        {
            var task = await CreateTaskAsync(client, tenantId, type, id);
            Assert.Equal(type, task.LinkedEntityType);
            Assert.Equal(id, task.LinkedEntityId);
            Assert.Equal("open", task.Status);
        }
    }

    [Fact]
    public async Task TC_11_1_2_Task_status_moves_through_expected_states_and_reopens()
    {
        var tenantId = Guid.Parse("11111111-1111-1111-1111-1111111111a2");
        await using var factory = CreateFactory("tc-11-1-2", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        var task = await CreateTaskAsync(client, tenantId, "obligation", "far-52-204-21");
        var statuses = new[] { "in_progress", "blocked", "completed", "canceled", "open" };

        foreach (var status in statuses)
        {
            task = await PatchTaskAsync(client, tenantId, task.Id, new UpdateComplianceTaskRequest(null, null, status, null, null, null, null, null, null));
            Assert.Equal(status, task.Status);
        }
    }

    [Fact]
    public async Task TC_11_1_3_Task_updates_are_tenant_scoped()
    {
        var tenantAId = Guid.Parse("11111111-1111-1111-1111-1111111111a3");
        var tenantBId = Guid.Parse("11111111-1111-1111-1111-1111111111b3");
        await using var factory = CreateFactory("tc-11-1-3", dbContext =>
        {
            SeedTenant(dbContext, tenantAId);
            SeedTenant(dbContext, tenantBId);
        });
        using var client = factory.CreateClient();
        var task = await CreateTaskAsync(client, tenantAId, "obligation", "far-52-204-21");
        using var request = CreateRequest(
            HttpMethod.Patch,
            $"/api/tasks/{task.Id}",
            new UpdateComplianceTaskRequest(null, null, "blocked", null, null, null, null, null, null),
            tenantBId,
            Guid.NewGuid(),
            Permission.ManageTasks);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TC_11_1_4_Task_status_changes_are_audit_logged()
    {
        var tenantId = Guid.Parse("11111111-1111-1111-1111-1111111111a4");
        await using var factory = CreateFactory("tc-11-1-4", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        var task = await CreateTaskAsync(client, tenantId, "obligation", "far-52-204-21");

        await PatchTaskAsync(client, tenantId, task.Id, new UpdateComplianceTaskRequest(null, null, "in_progress", null, null, null, null, null, null));

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var auditEvent = await dbContext.AuditLogEntries.SingleAsync(audit =>
            audit.TenantId == tenantId &&
            audit.EntityType == "ComplianceTask" &&
            audit.EntityId == task.Id.ToString() &&
            audit.Action == AuditAction.Updated);
        Assert.Contains("status changed", auditEvent.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("previousStatus", auditEvent.MetadataJson);
        Assert.Contains("in_progress", auditEvent.MetadataJson);
    }

    private async Task<ComplianceTaskDto> CreateTaskAsync(HttpClient client, Guid tenantId, string linkType, string linkId)
    {
        using var request = CreateRequest(
            HttpMethod.Post,
            "/api/tasks",
            new CreateComplianceTaskRequest(
                $"Task for {linkType}",
                "Track compliance work.",
                "open",
                RiskLevel.High,
                null,
                "contracts",
                new DateOnly(2026, 7, 1),
                linkType,
                linkId),
            tenantId,
            Guid.NewGuid(),
            Permission.ManageTasks);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<ComplianceTaskDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected task response.");
    }

    private async Task<ComplianceTaskDto> PatchTaskAsync(HttpClient client, Guid tenantId, Guid taskId, UpdateComplianceTaskRequest patch)
    {
        using var request = CreateRequest(HttpMethod.Patch, $"/api/tasks/{taskId}", patch, tenantId, Guid.NewGuid(), Permission.ManageTasks);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<ComplianceTaskDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected task response.");
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<ComplianceTaskService>();
                services.AddScoped<IComplianceTaskRepository, EfComplianceTaskRepository>();
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
                seed?.Invoke(dbContext);
                dbContext.SaveChanges();
            });
        });

    private static HttpRequestMessage CreateRequest<TContent>(
        HttpMethod method,
        string requestUri,
        TContent content,
        Guid tenantId,
        Guid userId,
        Permission permission)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", permission.ToString());
        request.Content = JsonContent.Create(content, options: JsonOptions);
        return request;
    }

    private static void SeedTenant(GccsDbContext dbContext, Guid tenantId)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = "Task Tenant",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}
