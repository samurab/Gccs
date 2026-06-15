using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Compliance;
using Gccs.Application.Security;
using Gccs.Domain.Audit;
using Gccs.Domain.Companies;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Domain.Contracts;
using Gccs.Domain.Evidence;
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

public sealed class ObligationDetailTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public ObligationDetailTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_10_2_1_Detail_shows_source_backed_obligation_content()
    {
        var tenantId = Guid.Parse("10210210-2102-1021-0210-2102102102a1");
        var scenario = DetailScenario.Create(tenantId);
        await using var factory = CreateFactory("tc-10-2-1", dbContext => SeedScenario(dbContext, scenario));
        using var client = factory.CreateClient();

        var detail = await GetDetailAsync(client, scenario);

        Assert.Equal("Apply FCI safeguards", detail.Title);
        Assert.Equal("Contract involves FCI.", detail.TriggerCondition);
        Assert.Equal("Apply basic safeguarding controls.", detail.RequiredAction);
        Assert.Equal("IT/security", detail.OwnerFunction);
        Assert.Contains("Access control policy", detail.EvidenceExamples);
        Assert.True(detail.FlowDownRequired);
        Assert.Equal("Flow down to subcontractors handling FCI.", detail.FlowDownRequirement);
        Assert.Equal("https://www.acquisition.gov/far/52.204-21", detail.SourceUrl);
        Assert.Equal("high", detail.Confidence);
        Assert.Equal(new DateOnly(2026, 6, 3), detail.LastReviewedAt);
        Assert.True(detail.RequiresExpertReview);
    }

    [Fact]
    public async Task TC_10_2_2_Detail_shows_linked_tasks_and_evidence()
    {
        var tenantId = Guid.Parse("10210210-2102-1021-0210-2102102102a2");
        var scenario = DetailScenario.Create(tenantId);
        await using var factory = CreateFactory("tc-10-2-2", dbContext => SeedScenario(dbContext, scenario));
        using var client = factory.CreateClient();

        var detail = await GetDetailAsync(client, scenario);

        var task = Assert.Single(detail.LinkedTasks);
        Assert.Equal("Collect MFA configuration", task.Title);
        Assert.Equal("Open", task.Status);
        var evidence = Assert.Single(detail.LinkedEvidence);
        Assert.Equal("Access control policy", evidence.Name);
        Assert.Equal(EvidenceStatus.Approved, evidence.Status);
        Assert.Equal(EvidenceType.Policy, evidence.Type);
    }

    [Fact]
    public async Task TC_10_2_3_Status_change_persists_and_returns_updated_detail()
    {
        var tenantId = Guid.Parse("10210210-2102-1021-0210-2102102102a3");
        var scenario = DetailScenario.Create(tenantId);
        await using var factory = CreateFactory("tc-10-2-3", dbContext => SeedScenario(dbContext, scenario));
        using var client = factory.CreateClient();

        var detail = await PatchStatusAsync(client, scenario, ComplianceTaskStatus.InProgress);

        Assert.Equal("InProgress", detail.Status);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var task = await dbContext.ComplianceTasks.SingleAsync(task => task.Id == scenario.TaskId);
        Assert.Equal(ComplianceTaskStatus.InProgress, task.Status);
    }

    [Fact]
    public async Task TC_10_2_4_Status_change_is_audit_logged_and_cross_tenant_detail_is_denied()
    {
        var tenantAId = Guid.Parse("10210210-2102-1021-0210-2102102102a4");
        var tenantBId = Guid.Parse("10210210-2102-1021-0210-2102102102b4");
        var scenario = DetailScenario.Create(tenantAId);
        await using var factory = CreateFactory("tc-10-2-4", dbContext =>
        {
            SeedScenario(dbContext, scenario);
            SeedTenant(dbContext, tenantBId, "Other Tenant");
        });
        using var client = factory.CreateClient();

        await PatchStatusAsync(client, scenario, ComplianceTaskStatus.Blocked);
        using var deniedRequest = CreateRequest(
            HttpMethod.Get,
            $"/api/contract-obligations/{scenario.ContractClauseId}/{scenario.ObligationId}",
            tenantBId,
            Guid.NewGuid(),
            Permission.ViewObligations);
        var deniedResponse = await client.SendAsync(deniedRequest);

        Assert.Equal(HttpStatusCode.NotFound, deniedResponse.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var auditEvent = await dbContext.AuditLogEntries.SingleAsync(audit =>
            audit.TenantId == tenantAId &&
            audit.EntityType == "ContractObligation" &&
            audit.EntityId == $"{scenario.ContractClauseId:N}:{scenario.ObligationId}");
        Assert.Equal(AuditAction.Updated, auditEvent.Action);
        Assert.Contains("Blocked", auditEvent.Summary);
        Assert.Contains("previousStatus", auditEvent.MetadataJson);
    }

    [Fact]
    public async Task TC_10_3_1_Assign_obligation_to_tenant_member_updates_detail_and_dashboard()
    {
        var tenantId = Guid.Parse("10310310-3103-1031-0310-3103103103a1");
        var scenario = DetailScenario.Create(tenantId);
        await using var factory = CreateFactory("tc-10-3-1", dbContext => SeedScenario(dbContext, scenario));
        using var client = factory.CreateClient();

        var detail = await PatchOwnerAsync(client, scenario, new AssignContractObligationOwnerRequest(scenario.AssigneeUserId, null));
        var dashboardItems = await ListDashboardAsync(client, scenario);

        Assert.Equal(scenario.AssigneeUserId, detail.AssignedUserId);
        Assert.Equal("Assigned Owner", detail.AssignedUserDisplayName);
        Assert.Equal("Assigned Owner", detail.OwnerFunction);
        Assert.Equal("Assigned Owner", Assert.Single(dashboardItems).OwnerFunction);
    }

    [Fact]
    public async Task TC_10_3_2_Assign_obligation_to_role_updates_detail_and_dashboard()
    {
        var tenantId = Guid.Parse("10310310-3103-1031-0310-3103103103a2");
        var scenario = DetailScenario.Create(tenantId);
        await using var factory = CreateFactory("tc-10-3-2", dbContext => SeedScenario(dbContext, scenario));
        using var client = factory.CreateClient();

        var detail = await PatchOwnerAsync(client, scenario, new AssignContractObligationOwnerRequest(null, "ComplianceManager"));
        var dashboardItems = await ListDashboardAsync(client, scenario);

        Assert.Null(detail.AssignedUserId);
        Assert.Equal("ComplianceManager", detail.AssignedRoleName);
        Assert.Equal("ComplianceManager", detail.OwnerFunction);
        Assert.Equal("ComplianceManager", Assert.Single(dashboardItems).OwnerFunction);
    }

    [Fact]
    public async Task TC_10_3_3_Unauthorized_role_cannot_assign_obligation_owner()
    {
        var tenantId = Guid.Parse("10310310-3103-1031-0310-3103103103a3");
        var scenario = DetailScenario.Create(tenantId);
        await using var factory = CreateFactory("tc-10-3-3", dbContext => SeedScenario(dbContext, scenario));
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            HttpMethod.Patch,
            $"/api/contract-obligations/{scenario.ContractClauseId}/{scenario.ObligationId}/owner",
            new AssignContractObligationOwnerRequest(scenario.AssigneeUserId, null),
            scenario.TenantId,
            Guid.NewGuid(),
            Permission.ViewObligations);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task TC_10_3_4_Assignment_changes_are_audit_logged_with_notification_metadata()
    {
        var tenantId = Guid.Parse("10310310-3103-1031-0310-3103103103a4");
        var scenario = DetailScenario.Create(tenantId);
        await using var factory = CreateFactory("tc-10-3-4", dbContext => SeedScenario(dbContext, scenario));
        using var client = factory.CreateClient();

        await PatchOwnerAsync(client, scenario, new AssignContractObligationOwnerRequest(scenario.AssigneeUserId, null, Notify: true));

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var auditEvent = await dbContext.AuditLogEntries.SingleAsync(audit =>
            audit.TenantId == tenantId &&
            audit.EntityType == "ContractObligation" &&
            audit.Summary.Contains("owner changed", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(AuditAction.Updated, auditEvent.Action);
        Assert.Contains("assignmentType", auditEvent.MetadataJson);
        Assert.Contains("notificationEmitted", auditEvent.MetadataJson);
        Assert.Contains("True", auditEvent.MetadataJson);
    }

    private async Task<ContractObligationDetailDto> GetDetailAsync(HttpClient client, DetailScenario scenario)
    {
        using var request = CreateRequest(
            HttpMethod.Get,
            $"/api/contract-obligations/{scenario.ContractClauseId}/{scenario.ObligationId}",
            scenario.TenantId,
            Guid.NewGuid(),
            Permission.ViewObligations);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<ContractObligationDetailDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected obligation detail response.");
    }

    private async Task<ContractObligationDetailDto> PatchStatusAsync(
        HttpClient client,
        DetailScenario scenario,
        ComplianceTaskStatus status)
    {
        using var request = CreateRequest(
            HttpMethod.Patch,
            $"/api/contract-obligations/{scenario.ContractClauseId}/{scenario.ObligationId}/status",
            new UpdateContractObligationStatusRequest(status),
            scenario.TenantId,
            Guid.NewGuid(),
            Permission.ManageObligations);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<ContractObligationDetailDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected obligation detail response.");
    }

    private async Task<ContractObligationDetailDto> PatchOwnerAsync(
        HttpClient client,
        DetailScenario scenario,
        AssignContractObligationOwnerRequest assignment)
    {
        using var request = CreateRequest(
            HttpMethod.Patch,
            $"/api/contract-obligations/{scenario.ContractClauseId}/{scenario.ObligationId}/owner",
            assignment,
            scenario.TenantId,
            Guid.NewGuid(),
            Permission.ManageObligations);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<ContractObligationDetailDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected obligation detail response.");
    }

    private async Task<ObligationDashboardItemDto[]> ListDashboardAsync(HttpClient client, DetailScenario scenario)
    {
        using var request = CreateRequest(HttpMethod.Get, "/api/contract-obligations", scenario.TenantId, Guid.NewGuid(), Permission.ViewObligations);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<ObligationDashboardItemDto[]>(JsonOptions) ?? [];
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<ObligationDetailService>();
                services.AddScoped<IObligationDetailRepository, EfObligationDetailRepository>();
                services.AddScoped<IObligationDashboardRepository, EfObligationDashboardRepository>();
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

    private static void SeedScenario(GccsDbContext dbContext, DetailScenario scenario)
    {
        SeedTenant(dbContext, scenario.TenantId, "Detail Tenant");
        dbContext.Users.Add(new UserEntity
        {
            Id = scenario.AssigneeUserId,
            TenantId = scenario.TenantId,
            Email = "assigned.owner@example.com",
            DisplayName = "Assigned Owner",
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.TenantMemberships.Add(new TenantMembershipEntity
        {
            Id = Guid.NewGuid(),
            TenantId = scenario.TenantId,
            UserId = scenario.AssigneeUserId,
            RoleName = "Contributor",
            Status = MembershipStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Contracts.Add(new ContractEntity
        {
            Id = scenario.ContractId,
            TenantId = scenario.TenantId,
            ContractNumber = "W15QKN-26-C-0001",
            Title = "Base operations support services",
            AgencyOrPrimeName = "Department of Defense",
            Relationship = ContractorRelationship.Prime,
            Kind = ContractKind.FixedPrice,
            Status = ContractStatus.Active,
            AwardedAt = new DateOnly(2026, 6, 15),
            PeriodOfPerformanceStart = new DateOnly(2026, 7, 1),
            PeriodOfPerformanceEnd = new DateOnly(2027, 6, 30),
            PlaceOfPerformance = "Remote",
            Description = "Seeded obligation detail contract.",
            DataHandlingPosture = DataHandlingPosture.FciOnly,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Set<ContractClauseEntity>().Add(new ContractClauseEntity
        {
            Id = scenario.ContractClauseId,
            ContractId = scenario.ContractId,
            ClauseLibraryId = "far-52-204-21",
            ClauseNumber = "52.204-21",
            Title = "Basic Safeguarding",
            Source = ClauseSource.Far,
            SourceUrl = "https://www.acquisition.gov/far/52.204-21",
            AttachmentReason = "Mapped clause applies.",
            RequiresFlowDown = true,
            LastReviewedAt = new DateOnly(2026, 6, 3),
            Confidence = "high",
            RequiresExpertReview = true,
            ReviewState = ReviewState.Published,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Obligations.Add(new ObligationEntity
        {
            Id = scenario.ObligationId,
            Source = "FAR 52.204-21",
            Title = "Apply FCI safeguards",
            PlainEnglishSummary = "Apply basic safeguarding controls to systems that handle FCI.",
            TriggerCondition = "Contract involves FCI.",
            RequiredAction = "Apply basic safeguarding controls.",
            OwnerFunction = "IT/security",
            RiskLevel = RiskLevel.High,
            RequiresFlowDown = true,
            FlowDownRequirement = "Flow down to subcontractors handling FCI.",
            ApplicabilityJson = "{}",
            EvidenceExamplesJson = """["Access control policy","MFA configuration"]""",
            SourceName = "FAR 52.204-21",
            SourceUrl = "https://www.acquisition.gov/far/52.204-21",
            SourceLastReviewedAt = new DateOnly(2026, 6, 3),
            SourceConfidence = "high",
            SourceRequiresExpertReview = true,
            LastReviewedAt = new DateOnly(2026, 6, 3),
            Confidence = "high",
            RequiresExpertReview = true,
            ReviewState = ReviewState.Published
        });
        dbContext.Set<ContractClauseObligationEntity>().Add(new ContractClauseObligationEntity
        {
            ContractClauseId = scenario.ContractClauseId,
            ObligationId = scenario.ObligationId
        });
        dbContext.ComplianceTasks.Add(new ComplianceTaskEntity
        {
            Id = scenario.TaskId,
            TenantId = scenario.TenantId,
            Title = "Collect MFA configuration",
            Description = "Collect evidence for the mapped obligation.",
            Type = ComplianceTaskType.ObligationAction,
            Status = ComplianceTaskStatus.Open,
            RiskLevel = RiskLevel.High,
            OwnerFunction = "IT/security",
            DueAt = new DateOnly(2026, 7, 15),
            ContractId = scenario.ContractId,
            ObligationId = scenario.ObligationId,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.EvidenceItems.Add(new EvidenceItemEntity
        {
            Id = scenario.EvidenceItemId,
            TenantId = scenario.TenantId,
            Name = "Access control policy",
            Description = "Approved policy evidence.",
            Type = EvidenceType.Policy,
            Status = EvidenceStatus.Approved,
            OriginalFileName = "access-control-policy.pdf",
            ExpiresAt = new DateOnly(2027, 6, 30),
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Set<EvidenceObligationEntity>().Add(new EvidenceObligationEntity
        {
            EvidenceItemId = scenario.EvidenceItemId,
            ObligationId = scenario.ObligationId
        });
    }

    private static HttpRequestMessage CreateRequest<TContent>(
        HttpMethod method,
        string requestUri,
        TContent content,
        Guid tenantId,
        Guid userId,
        Permission permission)
    {
        var request = CreateRequest(method, requestUri, tenantId, userId, permission);
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

    private static void SeedTenant(GccsDbContext dbContext, Guid tenantId, string name)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = name,
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private sealed record DetailScenario(
        Guid TenantId,
        Guid ContractId,
        Guid ContractClauseId,
        string ObligationId,
        Guid TaskId,
        Guid EvidenceItemId,
        Guid AssigneeUserId)
    {
        public static DetailScenario Create(Guid tenantId) =>
            new(
                tenantId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                "obligation-fci-safeguards",
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid());
    }
}
