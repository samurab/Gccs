using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Calendar;
using Gccs.Application.Notifications;
using Gccs.Application.Reports;
using Gccs.Domain.Companies;
using Gccs.Domain.Contracts;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Calendar;
using Gccs.Infrastructure.Notifications;
using Gccs.Application.Audit;
using Gccs.Domain.Audit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class EsrsApplicabilityCalendarTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly WebApplicationFactory<Program> _factory;
    public EsrsApplicabilityCalendarTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task TC_31_1_1_And_5_Create_persists_task_review_metadata_and_audit()
    {
        var ids = StoryIds.Create();
        await using var factory = CreateFactory(nameof(TC_31_1_1_And_5_Create_persists_task_review_metadata_and_audit), ids);
        using var client = factory.CreateClient();
        var response = await client.SendAsync(Request(HttpMethod.Post, $"/api/contracts/{ids.ContractId}/esrs-applicabilities", CreateRequest(ids.ContractId), ids.TenantId, Permission.ManageContracts));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<EsrsApplicabilityDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(ids.TenantId, created.TenantId);
        Assert.Equal(ids.UserId, created.ReviewedByUserId);
        Assert.Equal(EsrsReportType.Isr, created.ReportType);
        Assert.Equal("FAR 52.219-9", created.SourceClause);

        var changedDueDate = new DateOnly(2026, 5, 1);
        var updateResponse = await client.SendAsync(Request(HttpMethod.Put,
            $"/api/contracts/{ids.ContractId}/esrs-applicabilities/{created.Id}",
            CreateRequest(ids.ContractId) with { DueDate = changedDueDate, Rationale = "Updated after a documented contract-file review." },
            ids.TenantId, Permission.ManageContracts));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var stored = await db.EsrsApplicabilities.SingleAsync(x => x.Id == created.Id);
        var task = await db.ComplianceTasks.SingleAsync(x => x.Id == created.TaskId);
        Assert.Equal(changedDueDate, stored.DueDate);
        Assert.Equal(stored.DueDate, task.DueAt);
        Assert.Equal(ids.ContractId, task.ContractId);
        Assert.Equal(ids.UserId, task.AssignedToUserId);
        Assert.Equal(2, await db.AuditLogEntries.CountAsync(x => x.TenantId == ids.TenantId && x.EntityType == "EsrsApplicability" && x.EntityId == created.Id.ToString()));
    }

    [Fact]
    public async Task TC_31_1_2_And_4_Active_report_uses_shared_calendar_overdue_and_reminder_pipeline()
    {
        var ids = StoryIds.Create();
        await using var factory = CreateFactory(nameof(TC_31_1_2_And_4_Active_report_uses_shared_calendar_overdue_and_reminder_pipeline), ids);
        using var client = factory.CreateClient();
        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2);
        var body = CreateRequest(ids.ContractId) with { PeriodStart = dueDate.AddMonths(-6), PeriodEnd = dueDate.AddDays(-15), DueDate = dueDate };
        var created = await CreateAsync(client, ids, body);

        var calendarResponse = await client.SendAsync(Request<object>(HttpMethod.Get, $"/api/calendar/events?from={dueDate.AddDays(-1):yyyy-MM-dd}&to={dueDate.AddDays(1):yyyy-MM-dd}", null, ids.TenantId, Permission.ViewTasks));
        Assert.Equal(HttpStatusCode.OK, calendarResponse.StatusCode);
        var events = await calendarResponse.Content.ReadFromJsonAsync<CalendarEventDto[]>(JsonOptions);
        var calendarItem = Assert.Single(events!);
        Assert.Equal($"task:{created.TaskId}", calendarItem.Id);
        Assert.Equal("Reports", calendarItem.Module);
        Assert.True(calendarItem.IsOverdue);

        var reminderResponse = await client.SendAsync(Request(HttpMethod.Post, "/api/notifications/due-date-reminders", new RunDueDateReminderRequest(14, null), ids.TenantId, Permission.ManageTasks));
        Assert.Equal(HttpStatusCode.OK, reminderResponse.StatusCode);
        var reminders = await reminderResponse.Content.ReadFromJsonAsync<DueDateReminderRunResult>(JsonOptions);
        Assert.Contains(reminders!.Items, x => x.TaskId == created.TaskId && x.Category == "overdue");

        var completedResponse = await client.SendAsync(Request(HttpMethod.Patch, $"/api/contracts/{ids.ContractId}/esrs-applicabilities/{created.Id}/status", new UpdateEsrsStatusRequest(EsrsReportTaskStatus.Completed), ids.TenantId, Permission.ManageContracts));
        Assert.Equal(HttpStatusCode.OK, completedResponse.StatusCode);
        var completed = await completedResponse.Content.ReadFromJsonAsync<EsrsApplicabilityDto>(JsonOptions);
        Assert.False(completed!.IsOverdue);
    }

    [Fact]
    public async Task TC_31_1_3_Missing_source_blocks_all_business_and_audit_writes()
    {
        var ids = StoryIds.Create();
        await using var factory = CreateFactory(nameof(TC_31_1_3_Missing_source_blocks_all_business_and_audit_writes), ids);
        using var client = factory.CreateClient();
        var body = CreateRequest(ids.ContractId) with { SourceClause = " ", Rationale = null };
        var response = await client.SendAsync(Request(HttpMethod.Post, $"/api/contracts/{ids.ContractId}/esrs-applicabilities", body, ids.TenantId, Permission.ManageContracts));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Empty(await db.EsrsApplicabilities.ToArrayAsync());
        Assert.Empty(await db.ComplianceTasks.ToArrayAsync());
        Assert.DoesNotContain(await db.AuditLogEntries.ToArrayAsync(), x => x.EntityType == "EsrsApplicability");
    }

    [Fact]
    public async Task Tenant_scope_and_server_RBAC_fail_closed_without_cross_tenant_mutation()
    {
        var ids = StoryIds.Create();
        await using var factory = CreateFactory(nameof(Tenant_scope_and_server_RBAC_fail_closed_without_cross_tenant_mutation), ids);
        using var client = factory.CreateClient();
        var forbidden = await client.SendAsync(Request(HttpMethod.Post, $"/api/contracts/{ids.ContractId}/esrs-applicabilities", CreateRequest(ids.ContractId), ids.TenantId, Permission.ViewContracts));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        var created = await CreateAsync(client, ids, CreateRequest(ids.ContractId));
        var hidden = await client.SendAsync(Request<object>(HttpMethod.Get, $"/api/contracts/{ids.ContractId}/esrs-applicabilities", null, ids.OtherTenantId, Permission.ViewContracts));
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        var crossTenantMutation = await client.SendAsync(Request(HttpMethod.Patch,
            $"/api/contracts/{ids.ContractId}/esrs-applicabilities/{created.Id}/status",
            new UpdateEsrsStatusRequest(EsrsReportTaskStatus.Completed), ids.OtherTenantId, Permission.ManageContracts));
        Assert.Equal(HttpStatusCode.NotFound, crossTenantMutation.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var applicability = await db.EsrsApplicabilities.SingleAsync();
        Assert.Equal(Gccs.Domain.Compliance.ComplianceTaskStatus.Open, (await db.ComplianceTasks.SingleAsync(x => x.Id == applicability.TaskId)).Status);
    }

    [Fact]
    public async Task Default_ISR_and_SSR_schedules_are_suggestions_with_review_guidance()
    {
        var ids = StoryIds.Create();
        await using var factory = CreateFactory(nameof(Default_ISR_and_SSR_schedules_are_suggestions_with_review_guidance), ids);
        using var client = factory.CreateClient();
        var response = await client.SendAsync(Request<object>(HttpMethod.Get, "/api/esrs/schedule-templates?fiscalYear=2027", null, ids.TenantId, Permission.ViewContracts));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var schedules = await response.Content.ReadFromJsonAsync<EsrsScheduleTemplateDto[]>(JsonOptions);
        Assert.Equal(3, schedules!.Length);
        Assert.Contains(schedules, x => x.ReportType == EsrsReportType.Isr && x.DueDate == new DateOnly(2027, 4, 30));
        Assert.Contains(schedules, x => x.ReportType == EsrsReportType.Ssr && x.Guidance.Contains("confirm", StringComparison.OrdinalIgnoreCase));
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task PostgreSQL_audit_failure_rolls_back_applicability_and_calendar_task()
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")
            ?? throw new InvalidOperationException("GCCS_TEST_POSTGRES_CONNECTION is required.");
        var ids = new StoryIds(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<GccsDbContext>();
                services.RemoveAll<DbContextOptions<GccsDbContext>>();
                services.RemoveAll<IAuditEventWriter>();
                services.AddDbContext<GccsDbContext>(options => options.UseGccsPostgres(connectionString));
                services.AddScoped<IAuditEventWriter, FailingAuditWriter>();
                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                PostgresTestDatabase.Migrate(db);
                db.Tenants.Add(Tenant(ids.TenantId));
                db.Users.Add(new UserEntity
                {
                    Id = ids.UserId, TenantId = ids.TenantId, PreferredTenantId = ids.TenantId,
                    Email = $"esrs-{ids.UserId:N}@example.test", DisplayName = "eSRS reviewer",
                    Status = UserStatus.Active, CreatedAt = DateTimeOffset.UtcNow
                });
                db.TenantMemberships.Add(new TenantMembershipEntity
                {
                    Id = Guid.NewGuid(), TenantId = ids.TenantId, UserId = ids.UserId,
                    Status = MembershipStatus.Active, RoleName = "ContractsManager", CreatedAt = DateTimeOffset.UtcNow
                });
                db.Contracts.Add(new ContractEntity
                {
                    Id = ids.ContractId, TenantId = ids.TenantId, ContractNumber = $"FA-{ids.ContractId:N}", Title = "Atomic eSRS contract",
                    AgencyOrPrimeName = "Department of Defense", Relationship = ContractorRelationship.Prime,
                    Kind = ContractKind.FixedPrice, Status = ContractStatus.Active,
                    DataHandlingPosture = DataHandlingPosture.FciOnly, CreatedAt = DateTimeOffset.UtcNow, CreatedByUserId = ids.UserId
                });
                db.SaveChanges();
            });
        });

        try
        {
            using var client = factory.CreateClient();
            var response = await client.SendAsync(Request(HttpMethod.Post, $"/api/contracts/{ids.ContractId}/esrs-applicabilities",
                CreateRequest(ids.ContractId), ids.TenantId, Permission.ManageContracts, ids.UserId));
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Contains("audit_write_failed", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            Assert.False(await db.EsrsApplicabilities.AnyAsync(x => x.TenantId == ids.TenantId));
            Assert.False(await db.ComplianceTasks.AnyAsync(x => x.TenantId == ids.TenantId));
            Assert.False(await db.AuditLogEntries.AnyAsync(x => x.TenantId == ids.TenantId));
        }
        finally
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            await db.AuditLogEntries.Where(x => x.TenantId == ids.TenantId).ExecuteDeleteAsync();
            await db.EsrsApplicabilities.Where(x => x.TenantId == ids.TenantId).ExecuteDeleteAsync();
            await db.ComplianceTasks.Where(x => x.TenantId == ids.TenantId).ExecuteDeleteAsync();
            await db.Contracts.Where(x => x.TenantId == ids.TenantId).ExecuteDeleteAsync();
            await db.TenantMemberships.Where(x => x.TenantId == ids.TenantId).ExecuteDeleteAsync();
            await db.Users.Where(x => x.TenantId == ids.TenantId).ExecuteDeleteAsync();
            await db.Tenants.Where(x => x.Id == ids.TenantId).ExecuteDeleteAsync();
        }
    }

    private static async Task<EsrsApplicabilityDto> CreateAsync(HttpClient client, StoryIds ids, EsrsApplicabilityRequest body)
    {
        var response = await client.SendAsync(Request(HttpMethod.Post, $"/api/contracts/{ids.ContractId}/esrs-applicabilities", body, ids.TenantId, Permission.ManageContracts));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<EsrsApplicabilityDto>(JsonOptions))!;
    }

    private WebApplicationFactory<Program> CreateFactory(string name, StoryIds ids) => _factory.WithWebHostBuilder(builder =>
    {
        builder.UseSetting("LocalDependencies:Enabled", "false");
        builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
        builder.ConfigureServices(services =>
        {
            services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase($"esrs-{name}"));
            services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();
            services.AddScoped<IDueDateReminderRepository, EfDueDateReminderRepository>();
            services.AddScoped<ICalendarRepository, EfCalendarRepository>();
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
            db.Tenants.AddRange(Tenant(ids.TenantId), Tenant(ids.OtherTenantId));
            db.Contracts.Add(new ContractEntity
            {
                Id = ids.ContractId, TenantId = ids.TenantId, ContractNumber = "FA-31-1", Title = "eSRS test contract",
                AgencyOrPrimeName = "Department of Defense", Relationship = ContractorRelationship.Prime,
                Kind = ContractKind.FixedPrice, Status = ContractStatus.Active,
                PeriodOfPerformanceStart = new(2026, 1, 1), PeriodOfPerformanceEnd = new(2028, 12, 31),
                PlaceOfPerformance = "Virginia", Description = "No-CUI metadata-only test contract.",
                DataHandlingPosture = DataHandlingPosture.FciOnly, CreatedAt = DateTimeOffset.UtcNow, CreatedByUserId = ids.UserId
            });
            db.SaveChanges();
        });
    });

    private static TenantEntity Tenant(Guid id) => new() { Id = id, Name = $"Tenant {id:N}", Status = TenantStatus.Active, DataPosture = TenantDataPosture.NoCui, CreatedAt = DateTimeOffset.UtcNow };
    private static EsrsApplicabilityRequest CreateRequest(Guid contractId) => new(contractId, "Prime contract", "Department of Defense", "Individual", "Prime",
        EsrsReportType.Isr, new(2026, 1, 1), new(2026, 3, 31), new(2026, 4, 30), "FAR 52.219-9", "Contract file review supports applicability.", "Contracts");

    private static HttpRequestMessage Request<T>(HttpMethod method, string uri, T? body, Guid tenantId, Permission permission, Guid? userId = null)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", (userId ?? StoryIds.User).ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", permission.ToString());
        if (body is not null) request.Content = JsonContent.Create(body, options: JsonOptions);
        return request;
    }

    private sealed record StoryIds(Guid TenantId, Guid OtherTenantId, Guid ContractId, Guid UserId)
    {
        public static readonly Guid User = Guid.Parse("31131131-1131-3113-1131-3113113113cc");
        public static StoryIds Create() => new(Guid.Parse("31131131-1131-3113-1131-3113113113aa"), Guid.Parse("31131131-1131-3113-1131-3113113113dd"), Guid.Parse("31131131-1131-3113-1131-3113113113bb"), User);
    }

    private sealed class FailingAuditWriter : IAuditEventWriter
    {
        public Task WriteAsync(Guid tenantId, Guid actorUserId, AuditAction action, string entityType, string entityId,
            string summary, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default) =>
            throw new AuditWriteException("Synthetic eSRS audit persistence failure.");
    }
}
