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
        using var getRequest = CreateRequest(HttpMethod.Get, $"/api/tasks/{task.Id}", tenantBId, Guid.NewGuid(), Permission.ViewTasks);
        var getResponse = await client.SendAsync(getRequest);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
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

    [Fact]
    public async Task Task_search_filters_pages_and_excludes_other_tenants()
    {
        var tenantA = Guid.NewGuid(); var tenantB = Guid.NewGuid(); var owner = Guid.NewGuid();
        await using var factory = CreateFactory("task-search", dbContext =>
        {
            SeedTenant(dbContext, tenantA); SeedTenant(dbContext, tenantB);
            SeedTask(dbContext, tenantA, owner, ComplianceTaskStatus.InProgress, new DateOnly(2026, 7, 1), "First");
            SeedTask(dbContext, tenantA, owner, ComplianceTaskStatus.InProgress, new DateOnly(2026, 7, 2), "Second");
            SeedTask(dbContext, tenantA, owner, ComplianceTaskStatus.Open, new DateOnly(2026, 7, 1), "Wrong status");
            SeedTask(dbContext, tenantB, owner, ComplianceTaskStatus.InProgress, new DateOnly(2026, 7, 1), "Other tenant");
        });
        using var client = factory.CreateClient();
        var query = $"status=InProgress&ownerUserId={owner}&dueFrom=2026-07-01&dueTo=2026-07-02&pageSize=1";

        var first = await SearchAsync(client, tenantA, $"/api/tasks/search?{query}&page=1");
        var second = await SearchAsync(client, tenantA, $"/api/tasks/search?{query}&page=2");
        var defaults = await SearchAsync(client, tenantA, "/api/tasks/search");

        Assert.Equal(2, first.TotalCount);
        Assert.Equal(1, first.PageSize);
        Assert.Equal("First", Assert.Single(first.Items).Title);
        Assert.Equal("in_progress", first.Items[0].Status);
        Assert.Equal("Second", Assert.Single(second.Items).Title);
        Assert.Equal(25, defaults.PageSize);
        Assert.Equal(3, defaults.TotalCount);
    }

    [Theory]
    [InlineData("status=unsupported")]
    [InlineData("dueFrom=2026-07-02&dueTo=2026-07-01")]
    [InlineData("page=0")]
    [InlineData("pageSize=101")]
    [InlineData("page=2147483647&pageSize=100")]
    public async Task Task_search_rejects_invalid_filters_without_returning_data(string query)
    {
        var tenantId = Guid.NewGuid();
        await using var factory = CreateFactory("invalid-task-search-" + Guid.NewGuid(), dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        using var request = CreateRequest(HttpMethod.Get, $"/api/tasks/search?{query}", tenantId, Guid.NewGuid(), Permission.ViewTasks);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Task_search_requires_view_tasks_permission()
    {
        var tenantId = Guid.NewGuid();
        await using var factory = CreateFactory("denied-task-search", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        using var request = CreateRequest(HttpMethod.Get, "/api/tasks/search", tenantId, Guid.NewGuid(), Permission.ViewObligations);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Task_search_cursor_is_stable_tenant_and_filter_bound_and_rejects_mixed_pagination()
    {
        var tenantA = Guid.NewGuid(); var tenantB = Guid.NewGuid(); var owner = Guid.NewGuid();
        await using var factory = CreateFactory("task-cursor", dbContext =>
        {
            SeedTenant(dbContext, tenantA); SeedTenant(dbContext, tenantB);
            SeedTask(dbContext, tenantA, owner, ComplianceTaskStatus.InProgress, new DateOnly(2026, 7, 1), "First");
            SeedTask(dbContext, tenantA, owner, ComplianceTaskStatus.InProgress, new DateOnly(2026, 7, 2), "Second");
            SeedTask(dbContext, tenantA, owner, ComplianceTaskStatus.InProgress, new DateOnly(2026, 7, 3), "Third");
            SeedTask(dbContext, tenantA, owner, ComplianceTaskStatus.InProgress, null, "No due date");
        });
        using var client = factory.CreateClient();
        var filters = $"status=in_progress&ownerUserId={owner}&pageSize=1";

        var first = await SearchAsync(client, tenantA, $"/api/tasks/search?{filters}");
        Assert.True(first.HasMore);
        Assert.False(string.IsNullOrWhiteSpace(first.NextCursor));
        Assert.Equal("First", Assert.Single(first.Items).Title);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            SeedTask(dbContext, tenantA, owner, ComplianceTaskStatus.InProgress, new DateOnly(2026, 6, 30), "Inserted before cursor");
            await dbContext.SaveChangesAsync();
        }

        var encodedCursor = Uri.EscapeDataString(first.NextCursor!);
        var second = await SearchAsync(client, tenantA, $"/api/tasks/search?{filters}&cursor={encodedCursor}");
        Assert.Equal("Second", Assert.Single(second.Items).Title);
        var third = await SearchAsync(
            client,
            tenantA,
            $"/api/tasks/search?{filters}&cursor={Uri.EscapeDataString(second.NextCursor!)}");
        Assert.Equal("Third", Assert.Single(third.Items).Title);
        var final = await SearchAsync(
            client,
            tenantA,
            $"/api/tasks/search?{filters}&cursor={Uri.EscapeDataString(third.NextCursor!)}");
        Assert.Equal("No due date", Assert.Single(final.Items).Title);
        Assert.False(final.HasMore);
        Assert.Null(final.NextCursor);

        foreach (var (tenant, suffix) in new[]
        {
            (tenantA, $"{filters}&page=2&cursor={encodedCursor}"),
            (tenantA, $"status=open&ownerUserId={owner}&pageSize=1&cursor={encodedCursor}"),
            (tenantB, $"{filters}&cursor={encodedCursor}"),
            (tenantA, $"{filters}&cursor={Uri.EscapeDataString(first.NextCursor![..^1] + "x")}")
        })
        {
            using var request = CreateRequest(HttpMethod.Get, $"/api/tasks/search?{suffix}", tenant, Guid.NewGuid(), Permission.ViewTasks);
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

    [Fact]
    public async Task Legacy_task_list_returns_deprecation_and_successor_headers()
    {
        var tenantId = Guid.NewGuid();
        await using var factory = CreateFactory("legacy-task-list", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        using var request = CreateRequest(HttpMethod.Get, "/api/tasks", tenantId, Guid.NewGuid(), Permission.ViewTasks);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("true", Assert.Single(response.Headers.GetValues("Deprecation")));
        Assert.Contains("/api/tasks/search", Assert.Single(response.Headers.GetValues("Link")), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("contract")]
    [InlineData("evidence")]
    public async Task Task_create_rejects_malformed_guid_links_without_persisting_state(string linkedEntityType)
    {
        var tenantId = Guid.NewGuid();
        await using var factory = CreateFactory("invalid-task-link-" + Guid.NewGuid(), dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            HttpMethod.Post,
            "/api/tasks",
            new CreateComplianceTaskRequest(
                "Invalid linked task", "Must not persist.", "open", RiskLevel.High, null,
                "contracts", new DateOnly(2026, 7, 1), linkedEntityType, "not-a-guid"),
            tenantId,
            Guid.NewGuid(),
            Permission.ManageTasks);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.False(await dbContext.ComplianceTasks.AnyAsync());
        Assert.False(await dbContext.AuditLogEntries.AnyAsync(entry => entry.EntityType == "ComplianceTask"));
    }

    [Fact]
    public async Task Task_create_rejects_cross_tenant_and_inactive_assignees_without_side_effects()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var inactiveUserId = Guid.NewGuid();
        var otherTenantUserId = Guid.NewGuid();
        await using var factory = CreateFactory("invalid-task-assignees-" + Guid.NewGuid(), dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            SeedTenant(dbContext, otherTenantId);
            dbContext.Users.AddRange(
                new UserEntity
                {
                    Id = inactiveUserId, TenantId = tenantId, Email = "inactive@example.test",
                    DisplayName = "Inactive Member", Status = UserStatus.Disabled
                },
                new UserEntity
                {
                    Id = otherTenantUserId, TenantId = otherTenantId, Email = "other@example.test",
                    DisplayName = "Other Member", Status = UserStatus.Active
                });
            dbContext.TenantMemberships.AddRange(
                new TenantMembershipEntity
                {
                    Id = Guid.NewGuid(), TenantId = tenantId, UserId = inactiveUserId,
                    RoleName = RoleCatalog.Contributor, Status = MembershipStatus.Active
                },
                new TenantMembershipEntity
                {
                    Id = Guid.NewGuid(), TenantId = otherTenantId, UserId = otherTenantUserId,
                    RoleName = RoleCatalog.Contributor, Status = MembershipStatus.Active
                });
        });
        using var client = factory.CreateClient();

        foreach (var assignedToUserId in new[] { inactiveUserId, otherTenantUserId })
        {
            using var request = CreateRequest(
                HttpMethod.Post,
                "/api/tasks",
                new CreateComplianceTaskRequest(
                    "Invalid assignment", "Must not persist.", "open", RiskLevel.High, assignedToUserId,
                    "contracts", new DateOnly(2026, 7, 1), "obligation", "far-52-204-21"),
                tenantId,
                Guid.NewGuid(),
                Permission.ManageTasks);
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.False(await dbContext.ComplianceTasks.AnyAsync());
        Assert.False(await dbContext.AuditLogEntries.AnyAsync(entry => entry.EntityType == "ComplianceTask"));
        Assert.False(await dbContext.NotificationDeliveries.AnyAsync());
    }

    private static async Task<ComplianceTaskPageDto> SearchAsync(HttpClient client, Guid tenantId, string uri)
    {
        using var request = CreateRequest(HttpMethod.Get, uri, tenantId, Guid.NewGuid(), Permission.ViewTasks);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<ComplianceTaskPageDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected task search response.");
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
                services.AddScoped<ComplianceTaskSearchService>();
                services.AddScoped<IComplianceTaskRepository, EfComplianceTaskRepository>();
                services.AddScoped<IComplianceTaskSearchRepository, EfComplianceTaskRepository>();
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

    private static void SeedTask(
        GccsDbContext dbContext,
        Guid tenantId,
        Guid owner,
        ComplianceTaskStatus status,
        DateOnly? dueAt,
        string title)
    {
        dbContext.ComplianceTasks.Add(new ComplianceTaskEntity
        {
            Id = Guid.NewGuid(), TenantId = tenantId, Title = title, Description = "Search fixture",
            Type = ComplianceTaskType.ObligationAction, Status = status, RiskLevel = RiskLevel.Medium,
            AssignedToUserId = owner, OwnerFunction = "Compliance", DueAt = dueAt,
            CreatedAt = DateTimeOffset.UtcNow
        });
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
