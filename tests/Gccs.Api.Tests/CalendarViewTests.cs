using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Calendar;
using Gccs.Domain.Companies;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Domain.Contracts;
using Gccs.Domain.Identity;
using Gccs.Domain.Reports;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Calendar;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class CalendarViewTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public CalendarViewTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_11_2_1_Calendar_aggregates_tasks_renewals_reports_deadlines_deliverables_and_policy_reviews()
    {
        var tenantId = Guid.Parse("11211211-2112-1121-1211-2112112112a1");
        await using var factory = CreateFactory("tc-11-2-1", dbContext => SeedCalendarScenario(dbContext, tenantId));
        using var client = factory.CreateClient();

        var events = await ListAsync(client, tenantId);

        Assert.Contains(events, item => item.Category == "task" && item.Title == "Obligation follow-up");
        Assert.Contains(events, item => item.Category == "task" && item.Module == "Renewals");
        Assert.Contains(events, item => item.Category == "task" && item.Module == "Policy reviews");
        Assert.Contains(events, item => item.Category == "deliverable");
        Assert.Contains(events, item => item.Category == "reporting_deadline");
        Assert.Contains(events, item => item.Category == "report");
    }

    [Fact]
    public async Task TC_11_2_2_Calendar_filters_by_owner_status_risk_contract_and_module()
    {
        var tenantId = Guid.Parse("11211211-2112-1121-1211-2112112112a2");
        var contractId = Guid.Parse("11211211-2112-1121-1211-2112112112c2");
        await using var factory = CreateFactory("tc-11-2-2", dbContext => SeedCalendarScenario(dbContext, tenantId, contractId));
        using var client = factory.CreateClient();

        var events = await ListAsync(client, tenantId, $"owner=contracts&status=open&risk=High&contractId={contractId}&module=Obligations");

        var item = Assert.Single(events);
        Assert.Equal("Obligation follow-up", item.Title);
        Assert.Equal(contractId, item.ContractId);
    }

    [Fact]
    public async Task TC_11_2_3_Calendar_flags_overdue_items()
    {
        var tenantId = Guid.Parse("11211211-2112-1121-1211-2112112112a3");
        await using var factory = CreateFactory("tc-11-2-3", dbContext => SeedCalendarScenario(dbContext, tenantId));
        using var client = factory.CreateClient();

        var events = await ListAsync(client, tenantId);

        Assert.Contains(events, item => item.Title == "Overdue policy review" && item.IsOverdue);
    }

    [Fact]
    public async Task TC_11_2_4_Calendar_excludes_other_tenant_items()
    {
        var tenantAId = Guid.Parse("11211211-2112-1121-1211-2112112112a4");
        var tenantBId = Guid.Parse("11211211-2112-1121-1211-2112112112b4");
        await using var factory = CreateFactory("tc-11-2-4", dbContext =>
        {
            SeedCalendarScenario(dbContext, tenantAId);
            SeedCalendarScenario(dbContext, tenantBId);
        });
        using var client = factory.CreateClient();

        var events = await ListAsync(client, tenantAId);

        Assert.DoesNotContain(events, item => item.Id.Contains(tenantBId.ToString()[..8], StringComparison.OrdinalIgnoreCase));
        Assert.All(events, item => Assert.NotEqual("Other tenant task", item.Title));
    }

    private async Task<CalendarEventDto[]> ListAsync(HttpClient client, Guid tenantId, string query = "")
    {
        var separator = string.IsNullOrWhiteSpace(query) ? "" : $"&{query}";
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/calendar/events?from=2026-06-01&to=2026-08-31{separator}");
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", Guid.NewGuid().ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", Permission.ViewTasks.ToString());
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<CalendarEventDto[]>(JsonOptions) ?? [];
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<ICalendarRepository, EfCalendarRepository>();

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
                seed?.Invoke(dbContext);
                dbContext.SaveChanges();
            });
        });

    private static void SeedCalendarScenario(GccsDbContext dbContext, Guid tenantId, Guid? contractId = null)
    {
        SeedTenant(dbContext, tenantId);
        var actualContractId = contractId ?? Guid.NewGuid();
        dbContext.Contracts.Add(new ContractEntity
        {
            Id = actualContractId,
            TenantId = tenantId,
            ContractNumber = $"CAL-{tenantId.ToString("N")[..8]}",
            Title = "Calendar contract",
            AgencyOrPrimeName = "Sample Agency",
            Relationship = ContractorRelationship.Prime,
            Kind = ContractKind.FixedPrice,
            Status = ContractStatus.Active,
            AwardedAt = new DateOnly(2026, 6, 15),
            PeriodOfPerformanceStart = new DateOnly(2026, 7, 1),
            PeriodOfPerformanceEnd = new DateOnly(2027, 6, 30),
            PlaceOfPerformance = "Remote",
            Description = "Calendar test contract.",
            DataHandlingPosture = DataHandlingPosture.FciOnly,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.ComplianceTasks.AddRange(
            CreateTask(tenantId, "Obligation follow-up", ComplianceTaskType.ObligationAction, RiskLevel.High, "contracts", new DateOnly(2026, 7, 10), actualContractId, "far-52-204-21"),
            CreateTask(tenantId, "SAM renewal", ComplianceTaskType.Renewal, RiskLevel.Medium, "contracts", new DateOnly(2026, 8, 1), actualContractId),
            CreateTask(tenantId, "Overdue policy review", ComplianceTaskType.PolicyReview, RiskLevel.High, "security", new DateOnly(2026, 6, 1), actualContractId));
        dbContext.Set<ContractDeliverableEntity>().Add(new ContractDeliverableEntity
        {
            Id = Guid.NewGuid(),
            ContractId = actualContractId,
            Name = "Monthly report deliverable",
            Description = "Deliverable due date.",
            DueAt = new DateOnly(2026, 7, 15),
            OwnerFunction = "contracts",
            Status = DeliverableStatus.InProgress
        });
        dbContext.Set<ContractReportingDeadlineEntity>().Add(new ContractReportingDeadlineEntity
        {
            Id = Guid.NewGuid(),
            ContractId = actualContractId,
            Name = "Quarterly reporting deadline",
            Description = "Report deadline.",
            DueAt = new DateOnly(2026, 7, 31),
            Recurrence = RecurrencePattern.None,
            OwnerFunction = "reports"
        });
        dbContext.Reports.Add(new ReportEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Type = ReportType.ComplianceStatus,
            Title = "Compliance status report",
            Status = ReportStatus.Complete,
            GeneratedAt = new DateTimeOffset(2026, 7, 20, 12, 0, 0, TimeSpan.Zero),
            GeneratedByUserId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private static ComplianceTaskEntity CreateTask(
        Guid tenantId,
        string title,
        ComplianceTaskType type,
        RiskLevel risk,
        string owner,
        DateOnly dueAt,
        Guid contractId,
        string? obligationId = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = title,
            Description = "Calendar task.",
            Type = type,
            Status = ComplianceTaskStatus.Open,
            RiskLevel = risk,
            OwnerFunction = owner,
            DueAt = dueAt,
            ContractId = contractId,
            ObligationId = obligationId,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static void SeedTenant(GccsDbContext dbContext, Guid tenantId)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = "Calendar Tenant",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}
