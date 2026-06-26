using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Gccs.Application.Audit;
using Gccs.Application.Compliance;
using Gccs.Domain.Audit;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Compliance;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ComplianceChecklistTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid ActorUserId = Guid.Parse("96969696-9696-9696-9696-969696969696");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly WebApplicationFactory<Program> _factory;

    public ComplianceChecklistTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Tenant_can_create_checklist_instance_from_template()
    {
        var tenantId = Guid.Parse("96969696-9696-9696-9696-9696969696a1");
        await using var factory = CreateFactory("checklist-create", dbContext => dbContext.Tenants.Add(CreateTenant(tenantId)));
        using var client = factory.CreateClient();
        using var request = CreateJsonRequest(
            HttpMethod.Post,
            "/api/compliance/checklists",
            tenantId,
            new CreateComplianceChecklistInstanceRequest("cmmc-readiness"),
            Permission.ManageTasks);

        var response = await client.SendAsync(request);
        var checklist = await response.Content.ReadFromJsonAsync<ComplianceChecklistInstanceDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(checklist);
        Assert.Equal(tenantId, checklist.TenantId);
        Assert.Equal("cmmc-readiness", checklist.TemplateKey);
        Assert.Equal("CmmcReadiness", checklist.ChecklistType);
        Assert.NotEmpty(checklist.Items);
        Assert.All(checklist.Items, item => Assert.Equal(ComplianceChecklistStatusValues.NotStarted, item.Status));
    }

    [Fact]
    public async Task Tenant_can_update_own_checklist_item()
    {
        var tenantId = Guid.Parse("96969696-9696-9696-9696-9696969696a2");
        await using var factory = CreateFactory("checklist-update", dbContext => dbContext.Tenants.Add(CreateTenant(tenantId)));
        using var client = factory.CreateClient();
        var checklist = await CreateChecklistAsync(client, tenantId);
        var item = checklist.Items[0];
        using var request = CreateJsonRequest(
            HttpMethod.Put,
            $"/api/compliance/checklists/{checklist.Id}/items/{item.Id}",
            tenantId,
            new UpdateComplianceChecklistItemRequest(
                ComplianceChecklistStatusValues.Complete,
                ActorUserId,
                ComplianceChecklistReviewStatusValues.PendingReview,
                null,
                null,
                "Evidence owner confirmed this item is complete.",
                item.ControlId,
                null,
                null),
            Permission.ManageTasks);

        var response = await client.SendAsync(request);
        var updated = await response.Content.ReadFromJsonAsync<ComplianceChecklistInstanceDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(updated);
        var updatedItem = Assert.Single(updated.Items, candidate => candidate.Id == item.Id);
        Assert.Equal(ComplianceChecklistStatusValues.Complete, updatedItem.Status);
        Assert.Equal(ComplianceChecklistReviewStatusValues.PendingReview, updatedItem.ReviewStatus);
        Assert.Equal(ActorUserId, updatedItem.OwnerUserId);
        Assert.Equal(ActorUserId, updatedItem.CompletedByUserId);
        Assert.NotNull(updatedItem.CompletedAt);
    }

    [Fact]
    public async Task Tenant_cannot_access_another_tenants_checklist()
    {
        var tenantAId = Guid.Parse("96969696-9696-9696-9696-9696969696a3");
        var tenantBId = Guid.Parse("96969696-9696-9696-9696-9696969696b3");
        await using var factory = CreateFactory("checklist-cross-tenant", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantAId));
            dbContext.Tenants.Add(CreateTenant(tenantBId));
        });
        using var client = factory.CreateClient();
        var checklist = await CreateChecklistAsync(client, tenantAId);
        var item = checklist.Items[0];

        using var listRequest = CreateRequest(HttpMethod.Get, "/api/compliance/checklists", tenantBId, Permission.ViewTasks);
        var listResponse = await client.SendAsync(listRequest);
        var list = await listResponse.Content.ReadFromJsonAsync<ComplianceChecklistInstanceDto[]>(JsonOptions);

        using var updateRequest = CreateJsonRequest(
            HttpMethod.Put,
            $"/api/compliance/checklists/{checklist.Id}/items/{item.Id}",
            tenantBId,
            new UpdateComplianceChecklistItemRequest(
                ComplianceChecklistStatusValues.Complete,
                null,
                ComplianceChecklistReviewStatusValues.PendingReview,
                null,
                null,
                "Cross-tenant update should not resolve.",
                null,
                null,
                null),
            Permission.ManageTasks);
        var updateResponse = await client.SendAsync(updateRequest);

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.NotNull(list);
        Assert.Empty(list);
        Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);
    }

    [Fact]
    public async Task Unauthorized_user_cannot_update_checklist()
    {
        var tenantId = Guid.Parse("96969696-9696-9696-9696-9696969696a4");
        await using var factory = CreateFactory("checklist-unauthorized-update", dbContext => dbContext.Tenants.Add(CreateTenant(tenantId)));
        using var client = factory.CreateClient();
        var checklist = await CreateChecklistAsync(client, tenantId);
        var item = checklist.Items[0];
        using var request = CreateJsonRequest(
            HttpMethod.Put,
            $"/api/compliance/checklists/{checklist.Id}/items/{item.Id}",
            tenantId,
            new UpdateComplianceChecklistItemRequest(
                ComplianceChecklistStatusValues.InProgress,
                null,
                ComplianceChecklistReviewStatusValues.NotReviewed,
                null,
                null,
                null,
                null,
                null,
                null),
            Permission.ViewTasks);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Checklist_item_update_creates_audit_log()
    {
        var tenantId = Guid.Parse("96969696-9696-9696-9696-9696969696a5");
        await using var factory = CreateFactory("checklist-audit", dbContext => dbContext.Tenants.Add(CreateTenant(tenantId)));
        using var client = factory.CreateClient();
        var checklist = await CreateChecklistAsync(client, tenantId);
        var item = checklist.Items[0];
        using var request = CreateJsonRequest(
            HttpMethod.Put,
            $"/api/compliance/checklists/{checklist.Id}/items/{item.Id}",
            tenantId,
            new UpdateComplianceChecklistItemRequest(
                ComplianceChecklistStatusValues.Complete,
                null,
                ComplianceChecklistReviewStatusValues.PendingReview,
                null,
                null,
                "Completed with review notes.",
                null,
                null,
                null),
            Permission.ManageTasks);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Contains(await dbContext.AuditLogEntries.Where(audit => audit.TenantId == tenantId).ToArrayAsync(), audit =>
            audit.Action == AuditAction.Updated &&
            audit.EntityType == "ComplianceChecklist" &&
            audit.EntityId == checklist.Id.ToString() &&
            audit.MetadataJson.Contains("item-updated", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Empty_checklist_list_returns_empty_array()
    {
        var tenantId = Guid.Parse("96969696-9696-9696-9696-9696969696a6");
        await using var factory = CreateFactory("checklist-empty-list", dbContext => dbContext.Tenants.Add(CreateTenant(tenantId)));
        using var client = factory.CreateClient();
        using var request = CreateRequest(HttpMethod.Get, "/api/compliance/checklists", tenantId, Permission.ViewTasks);

        var response = await client.SendAsync(request);
        var list = await response.Content.ReadFromJsonAsync<ComplianceChecklistInstanceDto[]>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(list);
        Assert.Empty(list);
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<ComplianceChecklistService>();
                services.AddScoped<IComplianceChecklistRepository, EfComplianceChecklistRepository>();
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

    private static async Task<ComplianceChecklistInstanceDto> CreateChecklistAsync(HttpClient client, Guid tenantId)
    {
        using var request = CreateJsonRequest(
            HttpMethod.Post,
            "/api/compliance/checklists",
            tenantId,
            new CreateComplianceChecklistInstanceRequest("cmmc-readiness"),
            Permission.ManageTasks);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ComplianceChecklistInstanceDto>(JsonOptions))!;
    }

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        string requestUri,
        Guid tenantId,
        params Permission[] permissions)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", ActorUserId.ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", string.Join(",", permissions.Select(permission => permission.ToString())));
        return request;
    }

    private static HttpRequestMessage CreateJsonRequest<TBody>(
        HttpMethod method,
        string requestUri,
        Guid tenantId,
        TBody body,
        params Permission[] permissions)
    {
        var request = CreateRequest(method, requestUri, tenantId, permissions);
        request.Content = JsonContent.Create(body);
        return request;
    }

    private static TenantEntity CreateTenant(Guid tenantId) =>
        new()
        {
            Id = tenantId,
            Name = $"Checklist Tenant {tenantId:N}"[..32],
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.Parse("2026-06-20T12:00:00Z")
        };
}
