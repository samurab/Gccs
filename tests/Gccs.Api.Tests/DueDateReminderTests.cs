using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Notifications;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Notifications;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class DueDateReminderTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public DueDateReminderTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_16_2_1_Due_date_reminder_job_selects_tasks_within_configured_lead_time()
    {
        var ids = StoryIds.ForCase("tc-16-2-1");
        await using var factory = CreateFactory("tc-16-2-1", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var result = await RunAsync(client, ids.TenantId, new RunDueDateReminderRequest(14, null));

        Assert.Equal(2, result.UpcomingSelected);
        Assert.Contains(result.Items, item => item.TaskId == ids.UpcomingTaskId && item.Status == "Delivered");
        Assert.DoesNotContain(result.Items, item => item.TaskId == ids.FutureTaskId);
    }

    [Fact]
    public async Task TC_16_2_2_Due_date_reminder_job_is_idempotent_for_same_event()
    {
        var ids = StoryIds.ForCase("tc-16-2-2");
        await using var factory = CreateFactory("tc-16-2-2", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var first = await RunAsync(client, ids.TenantId, new RunDueDateReminderRequest(14, null));
        var second = await RunAsync(client, ids.TenantId, new RunDueDateReminderRequest(14, null));

        Assert.Equal(3, first.Created);
        Assert.Equal(0, second.Created);
        Assert.Equal(3, second.Skipped);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(3, await dbContext.NotificationDeliveries.CountAsync(delivery => delivery.TenantId == ids.TenantId));
    }

    [Fact]
    public async Task TC_16_2_3_Overdue_reminders_are_categorized_separately()
    {
        var ids = StoryIds.ForCase("tc-16-2-3");
        await using var factory = CreateFactory("tc-16-2-3", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var result = await RunAsync(client, ids.TenantId, new RunDueDateReminderRequest(14, null));

        Assert.Equal(1, result.OverdueSelected);
        Assert.Contains(result.Items, item => item.TaskId == ids.OverdueTaskId && item.Category == "overdue");
        Assert.Contains(result.Items, item => item.TaskId == ids.UpcomingTaskId && item.Category == "upcoming");
    }

    [Fact]
    public async Task TC_16_2_4_Delivery_failure_is_logged_without_crashing_unrelated_deliveries()
    {
        var ids = StoryIds.ForCase("tc-16-2-4");
        await using var factory = CreateFactory("tc-16-2-4", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var result = await RunAsync(client, ids.TenantId, new RunDueDateReminderRequest(14, ids.UpcomingTaskId));

        Assert.Equal(1, result.Failed);
        Assert.Equal(2, result.Created);
        Assert.Contains(result.Items, item => item.TaskId == ids.UpcomingTaskId && item.Status == "Failed");
        Assert.Contains(result.Items, item => item.TaskId == ids.OverdueTaskId && item.Status == "Delivered");
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Contains(await dbContext.NotificationDeliveries.Where(delivery => delivery.TenantId == ids.TenantId).ToArrayAsync(), delivery =>
            delivery.SourceTaskId == ids.UpcomingTaskId &&
            delivery.Status == "Failed" &&
            delivery.FailureMessage!.Contains("Simulated", StringComparison.Ordinal));
        Assert.Contains(await dbContext.AuditLogEntries.Where(audit => audit.TenantId == ids.TenantId).ToArrayAsync(), audit =>
            audit.EntityType == "DueDateReminderRun" && audit.MetadataJson.Contains("\"failed\":\"1\"", StringComparison.Ordinal));
    }

    private static async Task<DueDateReminderRunResult> RunAsync(
        HttpClient client,
        Guid tenantId,
        RunDueDateReminderRequest body)
    {
        using var request = CreateRequest(HttpMethod.Post, "/api/notifications/due-date-reminders", body, tenantId);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<DueDateReminderRunResult>(JsonOptions) ??
            throw new InvalidOperationException("Expected reminder run response.");
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<DueDateReminderService>();
                services.AddScoped<IDueDateReminderRepository, EfDueDateReminderRepository>();
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

    private static HttpRequestMessage CreateRequest<TContent>(HttpMethod method, string requestUri, TContent content, Guid tenantId)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", Guid.NewGuid().ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", Permission.ManageTasks.ToString());
        if (content is not null)
        {
            request.Content = JsonContent.Create(content, options: JsonOptions);
        }

        return request;
    }

    private static void SeedScenario(GccsDbContext dbContext, StoryIds ids)
    {
        dbContext.Tenants.Add(CreateTenant(ids.TenantId));
        dbContext.ComplianceTasks.AddRange(
            CreateTask(ids.TenantId, ids.UpcomingTaskId, "Upcoming task", DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7)),
            CreateTask(ids.TenantId, ids.SecondUpcomingTaskId, "Second upcoming task", DateOnly.FromDateTime(DateTime.UtcNow).AddDays(14)),
            CreateTask(ids.TenantId, ids.OverdueTaskId, "Overdue task", DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2)),
            CreateTask(ids.TenantId, ids.FutureTaskId, "Future task", DateOnly.FromDateTime(DateTime.UtcNow).AddDays(45)));
    }

    private static TenantEntity CreateTenant(Guid tenantId) =>
        new()
        {
            Id = tenantId,
            Name = "Reminder Tenant",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static ComplianceTaskEntity CreateTask(Guid tenantId, Guid taskId, string title, DateOnly dueAt) =>
        new()
        {
            Id = taskId,
            TenantId = tenantId,
            Title = title,
            Description = title,
            Type = ComplianceTaskType.CalendarReminder,
            Status = ComplianceTaskStatus.Open,
            RiskLevel = RiskLevel.Medium,
            OwnerFunction = "Compliance",
            DueAt = dueAt,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private sealed record StoryIds(
        Guid TenantId,
        Guid UpcomingTaskId,
        Guid SecondUpcomingTaskId,
        Guid OverdueTaskId,
        Guid FutureTaskId)
    {
        public static StoryIds ForCase(string caseName)
        {
            var suffix = Math.Abs(caseName.GetHashCode(StringComparison.Ordinal)) % 10000;
            return new StoryIds(
                Guid.Parse($"16216216-2162-1621-6216-21621621{suffix:D4}"),
                Guid.Parse($"16216216-2162-1621-6216-21621622{suffix:D4}"),
                Guid.Parse($"16216216-2162-1621-6216-21621623{suffix:D4}"),
                Guid.Parse($"16216216-2162-1621-6216-21621624{suffix:D4}"),
                Guid.Parse($"16216216-2162-1621-6216-21621625{suffix:D4}"));
        }
    }
}
