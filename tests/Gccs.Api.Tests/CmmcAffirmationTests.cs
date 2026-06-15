using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Calendar;
using Gccs.Application.Cmmc;
using Gccs.Application.Security;
using Gccs.Application.Tasks;
using Gccs.Domain.Audit;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Compliance;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Calendar;
using Gccs.Infrastructure.Cmmc;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class CmmcAffirmationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public CmmcAffirmationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_13_4_1_Set_annual_affirmation_due_date_appears_on_calendar()
    {
        var tenantId = Guid.Parse("13413414-3413-1413-4134-1341341341a1");
        await using var factory = CreateFactory("tc-13-4-1", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        var created = await CreateAffirmationAsync(client, tenantId, CreateAffirmationRequest());

        var events = await ListCalendarAsync(client, tenantId);

        Assert.Contains(events, item =>
            item.Id == $"cmmc-affirmation:{created.Id}" &&
            item.Category == "cmmc_affirmation" &&
            item.Module == "CMMC" &&
            item.Date == new DateOnly(2026, 7, 15));
    }

    [Fact]
    public async Task TC_13_4_2_Upcoming_annual_affirmation_creates_reminder_task()
    {
        var tenantId = Guid.Parse("13413414-3413-1413-4134-1341341341a2");
        await using var factory = CreateFactory("tc-13-4-2", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        var created = await CreateAffirmationAsync(client, tenantId, CreateAffirmationRequest());

        using var generateRequest = CreateRequest(
            HttpMethod.Post,
            "/api/tasks/renewals/generate",
            new GenerateRenewalTasksRequest(30),
            tenantId,
            Permission.ManageTasks);
        var generateResponse = await client.SendAsync(generateRequest);
        var result = await generateResponse.Content.ReadFromJsonAsync<RenewalTaskGenerationResult>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, generateResponse.StatusCode);
        Assert.NotNull(result);
        Assert.Contains(result.Items, item =>
            item.SourceType == "cmmc_affirmation" &&
            item.SourceId == created.Id.ToString() &&
            item.Created &&
            item.LinkedEntityType == "cmmc-affirmation");
    }

    [Fact]
    public async Task TC_13_4_3_Link_evidence_to_affirmation_record_and_display_it()
    {
        var tenantId = Guid.Parse("13413414-3413-1413-4134-1341341341a3");
        var evidenceId = Guid.Parse("13413414-3413-1413-4134-1341341341e3");
        await using var factory = CreateFactory("tc-13-4-3", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();

        await CreateAffirmationAsync(client, tenantId, CreateAffirmationRequest() with { EvidenceItemIds = [evidenceId] });
        var affirmations = await ListAffirmationsAsync(client, tenantId);

        var affirmation = Assert.Single(affirmations);
        Assert.Equal([evidenceId], affirmation.EvidenceItemIds);
    }

    [Fact]
    public async Task TC_13_4_4_Update_dates_evidence_links_and_status_are_audit_logged()
    {
        var tenantId = Guid.Parse("13413414-3413-1413-4134-1341341341a4");
        var evidenceId = Guid.Parse("13413414-3413-1413-4134-1341341341e4");
        await using var factory = CreateFactory("tc-13-4-4", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        var created = await CreateAffirmationAsync(client, tenantId, CreateAffirmationRequest());

        using var updateRequest = CreateRequest(
            HttpMethod.Patch,
            $"/api/cmmc/affirmations/{created.Id}",
            CreateAffirmationRequest() with
            {
                DueAt = new DateOnly(2026, 8, 15),
                SubmittedAt = new DateOnly(2026, 8, 1),
                EvidenceItemIds = [evidenceId],
                Status = AffirmationStatus.Submitted
            },
            tenantId,
            Permission.ManageCmmc);
        var updateResponse = await client.SendAsync(updateRequest);
        var updated = await updateResponse.Content.ReadFromJsonAsync<CmmcAffirmationDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.NotNull(updated);
        Assert.Equal(new DateOnly(2026, 8, 15), updated.DueAt);
        Assert.Equal(new DateOnly(2026, 8, 1), updated.SubmittedAt);
        Assert.Equal([evidenceId], updated.EvidenceItemIds);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var audits = await dbContext.AuditLogEntries
            .Where(audit => audit.TenantId == tenantId && audit.EntityType == "CmmcAffirmation" && audit.EntityId == created.Id.ToString())
            .ToArrayAsync();
        Assert.Contains(audits, audit => audit.Action == AuditAction.Created);
        Assert.Contains(audits, audit => audit.Action == AuditAction.Updated && audit.MetadataJson.Contains("Submitted", StringComparison.Ordinal));
    }

    private static async Task<CmmcAffirmationDto> CreateAffirmationAsync(
        HttpClient client,
        Guid tenantId,
        UpsertCmmcAffirmationRequest body)
    {
        using var request = CreateRequest(HttpMethod.Post, "/api/cmmc/affirmations", body, tenantId, Permission.ManageCmmc);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<CmmcAffirmationDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected CMMC affirmation response.");
    }

    private static async Task<CmmcAffirmationDto[]> ListAffirmationsAsync(HttpClient client, Guid tenantId)
    {
        using var request = CreateRequest<object?>(HttpMethod.Get, "/api/cmmc/affirmations", null, tenantId, Permission.ViewCmmc);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<CmmcAffirmationDto[]>(JsonOptions) ?? [];
    }

    private static async Task<CalendarEventDto[]> ListCalendarAsync(HttpClient client, Guid tenantId)
    {
        using var request = CreateRequest<object?>(
            HttpMethod.Get,
            "/api/calendar/events?from=2026-06-01&to=2026-08-31",
            null,
            tenantId,
            Permission.ViewTasks);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<CalendarEventDto[]>(JsonOptions) ?? [];
    }

    private static UpsertCmmcAffirmationRequest CreateAffirmationRequest() =>
        new(
            CmmcLevel.Level1,
            new DateOnly(2026, 7, 15),
            null,
            null,
            null,
            [],
            AffirmationStatus.DueSoon);

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<CmmcAffirmationService>();
                services.AddScoped<RenewalGenerationService>();
                services.AddScoped<ICmmcAffirmationRepository, EfCmmcAffirmationRepository>();
                services.AddScoped<IRenewalTaskRepository, EfRenewalTaskRepository>();
                services.AddScoped<ICalendarRepository, EfCalendarRepository>();
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
        Permission permission)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", Guid.NewGuid().ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", permission.ToString());
        if (content is not null)
        {
            request.Content = JsonContent.Create(content, options: JsonOptions);
        }

        return request;
    }

    private static void SeedTenant(GccsDbContext dbContext, Guid tenantId)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = "CMMC Affirmation Tenant",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}
