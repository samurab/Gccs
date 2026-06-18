using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Evidence;
using Gccs.Application.Security;
using Gccs.Domain.Audit;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Common;
using Gccs.Domain.Companies;
using Gccs.Domain.Compliance;
using Gccs.Domain.Contracts;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Domain.Vendors;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Evidence;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class EvidenceRequestCreationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public EvidenceRequestCreationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_26_1_1_and_TC_26_1_2_Creates_requests_for_supported_record_types_with_context_fields()
    {
        var ids = StoryIds.ForCase("tc-26-1-1");
        await using var factory = CreateFactory("tc-26-1-1", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var obligation = await CreateEvidenceRequestAsync(client, ids, Request(EvidenceRequestRelatedRecordType.Obligation, ids.ObligationId, ids.AssigneeUserId));
        var control = await CreateEvidenceRequestAsync(client, ids, Request(EvidenceRequestRelatedRecordType.Control, ids.ControlId, ids.AssigneeUserId));
        var contract = await CreateEvidenceRequestAsync(client, ids, Request(EvidenceRequestRelatedRecordType.Contract, ids.ContractId.ToString(), ids.AssigneeUserId));
        var subcontractor = await CreateEvidenceRequestAsync(client, ids, Request(EvidenceRequestRelatedRecordType.Subcontractor, ids.SubcontractorId.ToString(), null, ids.SubcontractorId));

        Assert.Equal(EvidenceRequestStatus.Open, obligation.Status);
        Assert.Equal(ids.RequesterUserId, obligation.RequesterUserId);
        Assert.Equal(ids.AssigneeUserId, obligation.AssigneeUserId);
        Assert.Equal(new DateOnly(2026, 9, 1), obligation.DueDate);
        Assert.Equal("Upload policy evidence.", obligation.Instructions);
        Assert.Equal(EvidenceRequestRelatedRecordType.Control, control.RelatedRecordType);
        Assert.Equal(EvidenceRequestRelatedRecordType.Contract, contract.RelatedRecordType);
        Assert.Equal(ids.SubcontractorId, subcontractor.AssigneeSubcontractorId);
    }

    [Fact]
    public async Task TC_26_1_3_Assignee_receives_notification()
    {
        var ids = StoryIds.ForCase("tc-26-1-3");
        await using var factory = CreateFactory("tc-26-1-3", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var created = await CreateEvidenceRequestAsync(client, ids, Request(EvidenceRequestRelatedRecordType.Obligation, ids.ObligationId, ids.AssigneeUserId));

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.True(await dbContext.NotificationDeliveries.AnyAsync(notification =>
            notification.TenantId == ids.TenantId &&
            notification.UserId == ids.AssigneeUserId &&
            notification.SourceTaskId == created.Id &&
            notification.SourceType == "EvidenceRequest"));
    }

    [Fact]
    public async Task TC_26_1_4_Assignment_and_related_records_are_tenant_scoped_and_permissions_are_enforced()
    {
        var ids = StoryIds.ForCase("tc-26-1-4");
        await using var factory = CreateFactory("tc-26-1-4", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        using var outsideAssignee = CreateHttpRequest(
            HttpMethod.Post,
            "/api/evidence-requests",
            Request(EvidenceRequestRelatedRecordType.Obligation, ids.ObligationId, ids.OtherTenantUserId),
            ids.TenantId,
            ids.RequesterUserId,
            Permission.ManageEvidence);
        var outsideAssigneeResponse = await client.SendAsync(outsideAssignee);

        using var noPermission = CreateHttpRequest(
            HttpMethod.Post,
            "/api/evidence-requests",
            Request(EvidenceRequestRelatedRecordType.Obligation, ids.ObligationId, ids.AssigneeUserId),
            ids.TenantId,
            ids.RequesterUserId,
            Permission.ViewEvidence);
        var noPermissionResponse = await client.SendAsync(noPermission);

        Assert.Equal(HttpStatusCode.BadRequest, outsideAssigneeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, noPermissionResponse.StatusCode);
    }

    [Fact]
    public async Task TC_26_1_5_Request_creation_is_audit_logged()
    {
        var ids = StoryIds.ForCase("tc-26-1-5");
        await using var factory = CreateFactory("tc-26-1-5", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var created = await CreateEvidenceRequestAsync(client, ids, Request(EvidenceRequestRelatedRecordType.Obligation, ids.ObligationId, ids.AssigneeUserId));

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var audits = await dbContext.AuditLogEntries
            .Where(audit => audit.TenantId == ids.TenantId && audit.EntityType == "EvidenceRequest" && audit.EntityId == created.Id.ToString())
            .ToArrayAsync();
        Assert.Contains(audits, audit => audit.Action == AuditAction.Created && audit.MetadataJson.Contains(ids.ObligationId, StringComparison.Ordinal));
    }

    private static async Task<EvidenceRequestDto> CreateEvidenceRequestAsync(HttpClient client, StoryIds ids, CreateEvidenceRequestRequest body)
    {
        using var request = CreateHttpRequest(HttpMethod.Post, "/api/evidence-requests", body, ids.TenantId, ids.RequesterUserId, Permission.ManageEvidence);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<EvidenceRequestDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected evidence request response.");
    }

    private static CreateEvidenceRequestRequest Request(
        EvidenceRequestRelatedRecordType relatedType,
        string relatedId,
        Guid? assigneeUserId,
        Guid? assigneeSubcontractorId = null) =>
        new(
            relatedType,
            relatedId,
            assigneeUserId,
            assigneeSubcontractorId,
            new DateOnly(2026, 9, 1),
            "Upload policy evidence.");

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<EvidenceRequestService>();
                services.AddScoped<IEvidenceRequestRepository, EfEvidenceRequestRepository>();
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

    private static HttpRequestMessage CreateHttpRequest<TContent>(
        HttpMethod method,
        string requestUri,
        TContent content,
        Guid tenantId,
        Guid userId,
        params Permission[] permissions)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", string.Join(",", permissions.Select(permission => permission.ToString())));
        if (content is not null)
        {
            request.Content = JsonContent.Create(content, options: JsonOptions);
        }

        return request;
    }

    private static void SeedScenario(GccsDbContext dbContext, StoryIds ids)
    {
        dbContext.Tenants.AddRange(CreateTenant(ids.TenantId), CreateTenant(ids.OtherTenantId));
        dbContext.Users.AddRange(
            CreateUser(ids.TenantId, ids.RequesterUserId, "requester@example.test"),
            CreateUser(ids.TenantId, ids.AssigneeUserId, "assignee@example.test"),
            CreateUser(ids.OtherTenantId, ids.OtherTenantUserId, "other@example.test"));
        dbContext.Obligations.Add(new ObligationEntity
        {
            Id = ids.ObligationId,
            Source = "FAR",
            Title = "Provide evidence",
            PlainEnglishSummary = "Provide evidence.",
            TriggerCondition = "Request is issued.",
            RequiredAction = "Upload evidence.",
            OwnerFunction = "Compliance",
            RiskLevel = RiskLevel.Medium,
            SourceName = "FAR",
            SourceUrl = "https://example.test",
            SourceLastReviewedAt = new DateOnly(2026, 6, 17),
            LastReviewedAt = new DateOnly(2026, 6, 17),
            Confidence = "high",
            SourceConfidence = "high",
            ReviewState = ReviewState.Approved
        });
        dbContext.Controls.Add(new ControlEntity
        {
            Id = ids.ControlId,
            Framework = ControlFramework.NistSp800171Revision2,
            CmmcLevel = CmmcLevel.Level2,
            Family = "AC",
            Title = "Access Control",
            Requirement = "Limit access.",
            AssessmentObjective = "Verify evidence.",
            SourceName = "NIST",
            SourceUrl = "https://example.test/nist",
            SourceLastReviewedAt = new DateOnly(2026, 6, 17)
        });
        dbContext.Contracts.Add(new ContractEntity
        {
            Id = ids.ContractId,
            TenantId = ids.TenantId,
            ContractNumber = "EVREQ-001",
            Title = "Evidence Request Contract",
            AgencyOrPrimeName = "Prime",
            Relationship = ContractorRelationship.Prime,
            Kind = ContractKind.FixedPrice,
            Status = ContractStatus.Active,
            PeriodOfPerformanceStart = new DateOnly(2026, 7, 1),
            PeriodOfPerformanceEnd = new DateOnly(2027, 6, 30),
            PlaceOfPerformance = "Arlington, VA",
            Description = "Evidence request test contract.",
            DataHandlingPosture = DataHandlingPosture.FciOnly,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Subcontractors.Add(new SubcontractorEntity
        {
            Id = ids.SubcontractorId,
            TenantId = ids.TenantId,
            Name = "Evidence Request Sub",
            Status = SubcontractorStatus.Active,
            RoleDescription = "Supplier",
            SmallBusinessStatus = "Small",
            CmmcStatus = "Ready",
            NdaStatus = "Executed",
            WorkshareDescription = "Support",
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private static TenantEntity CreateTenant(Guid tenantId) =>
        new()
        {
            Id = tenantId,
            Name = $"Evidence Request Tenant {tenantId:N}",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static UserEntity CreateUser(Guid tenantId, Guid userId, string email) =>
        new()
        {
            Id = userId,
            TenantId = tenantId,
            Email = email,
            DisplayName = email,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private sealed record StoryIds(
        Guid TenantId,
        Guid OtherTenantId,
        Guid RequesterUserId,
        Guid AssigneeUserId,
        Guid OtherTenantUserId,
        Guid ContractId,
        Guid SubcontractorId,
        string ObligationId,
        string ControlId)
    {
        public static StoryIds ForCase(string caseName)
        {
            var suffix = Math.Abs(caseName.GetHashCode(StringComparison.Ordinal)) % 10000;
            return new StoryIds(
                Guid.Parse($"26126126-1261-2612-6126-12612631{suffix:D4}"),
                Guid.Parse($"26126126-1261-2612-6126-12612632{suffix:D4}"),
                Guid.Parse($"26126126-1261-2612-6126-12612633{suffix:D4}"),
                Guid.Parse($"26126126-1261-2612-6126-12612634{suffix:D4}"),
                Guid.Parse($"26126126-1261-2612-6126-12612635{suffix:D4}"),
                Guid.Parse($"26126126-1261-2612-6126-12612636{suffix:D4}"),
                Guid.Parse($"26126126-1261-2612-6126-12612637{suffix:D4}"),
                $"obligation-26-1-{suffix:D4}",
                $"AC.L2-3.1.{suffix % 100:D2}");
        }
    }
}
