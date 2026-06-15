using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Notifications;
using Gccs.Application.Tasks;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Notifications;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class AssignmentNotificationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public AssignmentNotificationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_16_3_1_Assigned_task_user_receives_notification()
    {
        var ids = StoryIds.ForCase("tc-16-3-1");
        await using var factory = CreateFactory("tc-16-3-1", dbContext => SeedTenant(dbContext, ids));
        using var client = factory.CreateClient();

        var task = await CreateAssignedTaskAsync(client, ids);
        var notifications = await ListNotificationsAsync(client, ids.TenantId, ids.AssignedUserId);

        var notification = Assert.Single(notifications);
        Assert.Equal(task.Id, notification.SourceTaskId);
        Assert.Equal(ids.AssignedUserId, notification.UserId);
        Assert.Equal("assignment", notification.Category);
        Assert.Contains(task.Title, notification.Placeholder, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TC_16_3_2_Assignment_notification_links_to_source_record()
    {
        var ids = StoryIds.ForCase("tc-16-3-2");
        await using var factory = CreateFactory("tc-16-3-2", dbContext => SeedTenant(dbContext, ids));
        using var client = factory.CreateClient();

        var task = await CreateAssignedTaskAsync(client, ids);
        var notification = Assert.Single(await ListNotificationsAsync(client, ids.TenantId, ids.AssignedUserId));
        using var openRequest = CreateRequest<object?>(HttpMethod.Get, $"/api{notification.LinkUrl}", null, ids.TenantId, ids.AssignedUserId, Permission.ViewTasks);
        var openResponse = await client.SendAsync(openRequest);
        var openedTask = await openResponse.Content.ReadFromJsonAsync<ComplianceTaskDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, openResponse.StatusCode);
        Assert.Equal($"/tasks/{task.Id}", notification.LinkUrl);
        Assert.Equal(task.Id, openedTask?.Id);
    }

    [Fact]
    public async Task TC_16_3_3_Mark_assignment_notification_as_read_persists_state()
    {
        var ids = StoryIds.ForCase("tc-16-3-3");
        await using var factory = CreateFactory("tc-16-3-3", dbContext => SeedTenant(dbContext, ids));
        using var client = factory.CreateClient();

        await CreateAssignedTaskAsync(client, ids);
        var notification = Assert.Single(await ListNotificationsAsync(client, ids.TenantId, ids.AssignedUserId));
        using var markReadRequest = CreateRequest<object?>(HttpMethod.Post, $"/api/notifications/{notification.Id}/read", null, ids.TenantId, ids.AssignedUserId, Permission.ViewTasks);
        var markReadResponse = await client.SendAsync(markReadRequest);
        var updated = await markReadResponse.Content.ReadFromJsonAsync<NotificationCenterItemDto>(JsonOptions);
        var after = Assert.Single(await ListNotificationsAsync(client, ids.TenantId, ids.AssignedUserId));

        Assert.Equal(HttpStatusCode.OK, markReadResponse.StatusCode);
        Assert.NotNull(updated?.ReadAt);
        Assert.NotNull(after.ReadAt);
    }

    [Fact]
    public async Task TC_16_3_4_Unauthorized_user_cannot_open_assignment_notification_link()
    {
        var ids = StoryIds.ForCase("tc-16-3-4");
        await using var factory = CreateFactory("tc-16-3-4", dbContext => SeedTenant(dbContext, ids));
        using var client = factory.CreateClient();

        await CreateAssignedTaskAsync(client, ids);
        var notification = Assert.Single(await ListNotificationsAsync(client, ids.TenantId, ids.AssignedUserId));
        using var deniedRequest = CreateRequest<object?>(HttpMethod.Get, $"/api{notification.LinkUrl}", null, ids.TenantId, ids.AssignedUserId, Permission.ViewEvidence);
        var deniedResponse = await client.SendAsync(deniedRequest);

        Assert.Equal(HttpStatusCode.Forbidden, deniedResponse.StatusCode);
    }

    private static async Task<ComplianceTaskDto> CreateAssignedTaskAsync(HttpClient client, StoryIds ids)
    {
        var body = new CreateComplianceTaskRequest(
            "Assigned notification task",
            "Assignment notification test.",
            "open",
            RiskLevel.Medium,
            ids.AssignedUserId,
            "Compliance",
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7),
            "general",
            null);
        using var request = CreateRequest(HttpMethod.Post, "/api/tasks", body, ids.TenantId, ids.ActorUserId, Permission.ManageTasks);
        var response = await client.SendAsync(request);

        Assert.True(
            response.StatusCode == HttpStatusCode.Created,
            $"Expected Created, got {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return await response.Content.ReadFromJsonAsync<ComplianceTaskDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected task response.");
    }

    private static async Task<NotificationCenterItemDto[]> ListNotificationsAsync(HttpClient client, Guid tenantId, Guid userId)
    {
        using var request = CreateRequest<object?>(HttpMethod.Get, "/api/notifications", null, tenantId, userId, Permission.ViewTasks);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<NotificationCenterItemDto[]>(JsonOptions) ?? [];
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
                services.AddScoped<IAssignmentNotificationRepository, EfAssignmentNotificationRepository>();
                services.AddScoped<AssignmentNotificationService>();
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
        if (content is not null)
        {
            request.Content = JsonContent.Create(content, options: JsonOptions);
        }

        return request;
    }

    private static void SeedTenant(GccsDbContext dbContext, StoryIds ids)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = ids.TenantId,
            Name = "Assignment Notification Tenant",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private sealed record StoryIds(Guid TenantId, Guid ActorUserId, Guid AssignedUserId)
    {
        public static StoryIds ForCase(string caseName)
        {
            var suffix = Math.Abs(caseName.GetHashCode(StringComparison.Ordinal)) % 10000;
            return new StoryIds(
                Guid.Parse($"16316316-3163-1631-6316-31631631{suffix:D4}"),
                Guid.Parse($"16316316-3163-1631-6316-31631632{suffix:D4}"),
                Guid.Parse($"16316316-3163-1631-6316-31631633{suffix:D4}"));
        }
    }
}
