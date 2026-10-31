using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Calendar;
using Gccs.Application.Security;
using Gccs.Application.Subcontractors;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Domain.Companies;
using Gccs.Domain.Compliance;
using Gccs.Domain.Contracts;
using Gccs.Domain.Evidence;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Domain.Vendors;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Calendar;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Subcontractors;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class SubcontractorEvidenceRequestTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public SubcontractorEvidenceRequestTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_14_3_1_Create_evidence_request_with_requested_item_due_status_recipient_and_linked_obligation()
    {
        var ids = StoryIds.ForCase("tc-14-3-1");
        await using var factory = CreateFactory("tc-14-3-1", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var created = await CreateEvidenceRequestAsync(client, ids.TenantId, ids.SubcontractorId, CreateRequest(ids));
        var list = await ListEvidenceRequestsAsync(client, ids.TenantId, ids.SubcontractorId);

        Assert.Equal("Signed subcontractor flow-down package", created.RequestedItem);
        Assert.Equal(SubcontractorEvidenceRequestStatus.Sent, created.Status);
        Assert.Equal("supplier@example.com", created.RecipientEmail);
        Assert.Equal(ids.ObligationId, created.ObligationId);
        Assert.Equal(ids.FlowDownId, created.RelatedFlowDownClauseId);
        Assert.False(created.IsOverdue);
        Assert.Contains(list, request => request.Id == created.Id && request.RequestedEvidenceTypes.Contains(EvidenceType.SignedFlowDown));
    }

    [Fact]
    public async Task TC_14_3_2_Subcontractor_evidence_request_due_date_appears_on_calendar()
    {
        var ids = StoryIds.ForCase("tc-14-3-2");
        await using var factory = CreateFactory("tc-14-3-2", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();
        var created = await CreateEvidenceRequestAsync(client, ids.TenantId, ids.SubcontractorId, CreateRequest(ids));

        var events = await ListCalendarEventsAsync(client, ids.TenantId, created.DueDate.AddDays(-1), created.DueDate.AddDays(1));

        Assert.Contains(events, calendarEvent =>
            calendarEvent.Id == $"subcontractor-evidence-request:{created.Id}" &&
            calendarEvent.Category == "subcontractor_evidence_request" &&
            calendarEvent.RelatedEntityId == created.Id.ToString());
    }

    [Fact]
    public async Task TC_14_3_3_Link_received_evidence_satisfies_request_and_updates_completion()
    {
        var ids = StoryIds.ForCase("tc-14-3-3");
        await using var factory = CreateFactory("tc-14-3-3", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();
        var created = await CreateEvidenceRequestAsync(client, ids.TenantId, ids.SubcontractorId, CreateRequest(ids));

        var updated = await UpdateEvidenceRequestAsync(
            client,
            ids.TenantId,
            ids.SubcontractorId,
            created.Id,
            CreateRequest(ids) with
            {
                Status = SubcontractorEvidenceRequestStatus.Satisfied,
                ReceivedEvidenceItemId = ids.ReceivedEvidenceItemId
            });

        Assert.Equal(SubcontractorEvidenceRequestStatus.Satisfied, updated.Status);
        Assert.Equal(ids.ReceivedEvidenceItemId, updated.ReceivedEvidenceItemId);
        Assert.NotNull(updated.CompletedAt);
        Assert.False(updated.IsOverdue);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.True(await dbContext.Set<SubcontractorEvidenceEntity>().AnyAsync(link =>
            link.SubcontractorId == ids.SubcontractorId && link.EvidenceItemId == ids.ReceivedEvidenceItemId));
    }

    [Fact]
    public async Task TC_14_3_4_Overdue_evidence_request_is_flagged_in_list_calendar_and_warning_state()
    {
        var ids = StoryIds.ForCase("tc-14-3-4");
        var overdueDueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2);
        await using var factory = CreateFactory("tc-14-3-4", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var created = await CreateEvidenceRequestAsync(
            client,
            ids.TenantId,
            ids.SubcontractorId,
            CreateRequest(ids) with { DueDate = overdueDueDate });
        var list = await ListEvidenceRequestsAsync(client, ids.TenantId, ids.SubcontractorId);
        var events = await ListCalendarEventsAsync(client, ids.TenantId, overdueDueDate.AddDays(-1), overdueDueDate.AddDays(1));

        Assert.Contains(list, request => request.Id == created.Id && request.IsOverdue);
        Assert.Contains(events, calendarEvent =>
            calendarEvent.Id == $"subcontractor-evidence-request:{created.Id}" &&
            calendarEvent.Status == "overdue" &&
            calendarEvent.IsOverdue &&
            calendarEvent.RiskLevel == RiskLevel.High);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var audits = await dbContext.AuditLogEntries
            .Where(audit => audit.TenantId == ids.TenantId && audit.EntityType == "SubcontractorEvidenceRequest" && audit.EntityId == created.Id.ToString())
            .ToArrayAsync();
        Assert.Contains(audits, audit => audit.Action == AuditAction.Created && audit.MetadataJson.Contains("isOverdue", StringComparison.Ordinal));
    }

    private static async Task<SubcontractorEvidenceRequestDto> CreateEvidenceRequestAsync(
        HttpClient client,
        Guid tenantId,
        Guid subcontractorId,
        UpsertSubcontractorEvidenceRequestRequest body)
    {
        using var request = CreateRequest(HttpMethod.Post, $"/api/subcontractors/{subcontractorId}/evidence-requests", body, tenantId, Permission.ManageSubcontractors);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<SubcontractorEvidenceRequestDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected subcontractor evidence request response.");
    }

    private static async Task<SubcontractorEvidenceRequestDto> UpdateEvidenceRequestAsync(
        HttpClient client,
        Guid tenantId,
        Guid subcontractorId,
        Guid evidenceRequestId,
        UpsertSubcontractorEvidenceRequestRequest body)
    {
        using var request = CreateRequest(HttpMethod.Put, $"/api/subcontractors/{subcontractorId}/evidence-requests/{evidenceRequestId}", body, tenantId, Permission.ManageSubcontractors);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<SubcontractorEvidenceRequestDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected subcontractor evidence request response.");
    }

    private static async Task<SubcontractorEvidenceRequestDto[]> ListEvidenceRequestsAsync(HttpClient client, Guid tenantId, Guid subcontractorId)
    {
        using var request = CreateRequest<object?>(HttpMethod.Get, $"/api/subcontractors/{subcontractorId}/evidence-requests", null, tenantId, Permission.ViewSubcontractors);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<SubcontractorEvidenceRequestDto[]>(JsonOptions) ?? [];
    }

    private static async Task<CalendarEventDto[]> ListCalendarEventsAsync(HttpClient client, Guid tenantId, DateOnly from, DateOnly to)
    {
        using var request = CreateRequest<object?>(HttpMethod.Get, $"/api/calendar/events?from={from:O}&to={to:O}&module=Subcontractors", null, tenantId, Permission.ViewTasks);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<CalendarEventDto[]>(JsonOptions) ?? [];
    }

    private static UpsertSubcontractorEvidenceRequestRequest CreateRequest(StoryIds ids) =>
        new(
            "Signed subcontractor flow-down package",
            [EvidenceType.SignedFlowDown, EvidenceType.SubcontractorCertification],
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(14),
            SubcontractorEvidenceRequestStatus.Sent,
            "Supplier Contact",
            "supplier@example.com",
            ids.ObligationId,
            ids.FlowDownId,
            null);

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<SubcontractorService>();
                services.AddScoped<ISubcontractorRepository, EfSubcontractorRepository>();
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

    private static void SeedScenario(GccsDbContext dbContext, StoryIds ids)
    {
        dbContext.Tenants.AddRange(
            CreateTenant(ids.TenantId),
            CreateTenant(ids.OtherTenantId));
        dbContext.Subcontractors.Add(new SubcontractorEntity
        {
            Id = ids.SubcontractorId,
            TenantId = ids.TenantId,
            Name = "Evidence Supplier LLC",
            Status = SubcontractorStatus.Active,
            RoleDescription = "Specialty support",
            SmallBusinessStatus = "Small",
            CmmcStatus = "Level 1 complete",
            NdaStatus = "Executed",
            WorkshareDescription = "Evidence request support",
            HasFciAccess = true,
            HasCuiAccess = false,
            HasExportControlledAccess = false,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Obligations.Add(new ObligationEntity
        {
            Id = ids.ObligationId,
            Source = "FAR",
            Title = "Collect subcontractor evidence",
            PlainEnglishSummary = "Request and receive subcontractor evidence.",
            TriggerCondition = "Subcontractor has a required flow-down.",
            RequiredAction = "Request evidence and track receipt.",
            OwnerFunction = "Contracts",
            RiskLevel = RiskLevel.Medium,
            RequiresFlowDown = true,
            FlowDownRequirement = "Request signed flow-down evidence.",
            SourceName = "Acquisition source",
            SourceUrl = "https://example.test/source",
            SourceLastReviewedAt = new DateOnly(2026, 6, 15),
            LastReviewedAt = new DateOnly(2026, 6, 15),
            Confidence = "high",
            SourceConfidence = "high",
            ReviewState = ReviewState.Approved
        });
        dbContext.FlowDownClauses.Add(new FlowDownClauseEntity
        {
            Id = ids.FlowDownId,
            SubcontractorId = ids.SubcontractorId,
            ObligationId = ids.ObligationId,
            ClauseNumber = "52.244-6",
            Title = "Subcontractor flow-down",
            Status = FlowDownStatus.Sent,
            SentAt = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1),
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.EvidenceItems.Add(new EvidenceItemEntity
        {
            Id = ids.ReceivedEvidenceItemId,
            TenantId = ids.TenantId,
            Name = "Received signed flow-down evidence",
            Description = "Received subcontractor evidence.",
            Type = EvidenceType.SignedFlowDown,
            OwnerFunction = "Contracts",
            Status = EvidenceStatus.Uploaded,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private static TenantEntity CreateTenant(Guid tenantId) =>
        new()
        {
            Id = tenantId,
            Name = "Evidence Request Tenant",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private sealed record StoryIds(
        Guid TenantId,
        Guid OtherTenantId,
        Guid SubcontractorId,
        Guid FlowDownId,
        string ObligationId,
        Guid ReceivedEvidenceItemId)
    {
        public static StoryIds ForCase(string caseName)
        {
            var suffix = Math.Abs(caseName.GetHashCode(StringComparison.Ordinal)) % 10000;
            return new StoryIds(
                Guid.Parse($"14314314-3143-1431-4314-31431431{suffix:D4}"),
                Guid.Parse($"14314314-3143-1431-4314-31431432{suffix:D4}"),
                Guid.Parse($"14314314-3143-1431-4314-31431433{suffix:D4}"),
                Guid.Parse($"14314314-3143-1431-4314-31431434{suffix:D4}"),
                $"obligation-14-3-{suffix:D4}",
                Guid.Parse($"14314314-3143-1431-4314-31431435{suffix:D4}"));
        }
    }
}
