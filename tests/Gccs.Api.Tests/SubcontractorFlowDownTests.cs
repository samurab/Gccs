using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
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
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Subcontractors;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class SubcontractorFlowDownTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public SubcontractorFlowDownTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_14_2_1_Assign_required_flow_down_clauses_from_contract_obligations()
    {
        var ids = StoryIds.ForCase("tc-14-2-1");
        await using var factory = CreateFactory("tc-14-2-1", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var created = await CreateFlowDownAsync(client, ids.TenantId, ids.SubcontractorId, CreateRequest(ids));
        var bySubcontractor = await ListFlowDownsAsync(client, ids.TenantId, ids.SubcontractorId, null);

        Assert.Equal(ids.ContractId, created.ContractId);
        Assert.Equal(ids.ContractClauseId, created.ContractClauseId);
        Assert.Equal(ids.ObligationId, created.ObligationId);
        Assert.Equal("52.244-6", created.ClauseNumber);
        Assert.Equal(FlowDownStatus.Required, created.Status);
        Assert.Contains(bySubcontractor, flowDown => flowDown.Id == created.Id && flowDown.ContractId == ids.ContractId);
    }

    [Fact]
    public async Task TC_14_2_2_Status_visibility_by_subcontractor_and_contract()
    {
        var ids = StoryIds.ForCase("tc-14-2-2");
        await using var factory = CreateFactory("tc-14-2-2", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();
        var flowDown = await CreateFlowDownAsync(client, ids.TenantId, ids.SubcontractorId, CreateRequest(ids));
        var statuses = new[]
        {
            FlowDownStatus.Required,
            FlowDownStatus.Sent,
            FlowDownStatus.Acknowledged,
            FlowDownStatus.Signed,
            FlowDownStatus.Waived,
            FlowDownStatus.NotApplicable
        };

        foreach (var status in statuses)
        {
            flowDown = await UpdateFlowDownAsync(
                client,
                ids.TenantId,
                ids.SubcontractorId,
                flowDown.Id,
                CreateRequest(ids) with
                {
                    Status = status,
                    SentAt = status is FlowDownStatus.Sent ? new DateOnly(2026, 7, 1) : null,
                    AcknowledgedAt = status is FlowDownStatus.Acknowledged ? new DateOnly(2026, 7, 2) : null,
                    SignedAt = status is FlowDownStatus.Signed ? new DateOnly(2026, 7, 3) : null,
                    WaivedAt = status is FlowDownStatus.Waived ? new DateOnly(2026, 7, 4) : null
                });

            var bySubcontractor = await ListFlowDownsAsync(client, ids.TenantId, ids.SubcontractorId, null);
            var byContract = await ListFlowDownsAsync(client, ids.TenantId, ids.SubcontractorId, ids.ContractId);

            Assert.Contains(bySubcontractor, candidate => candidate.Id == flowDown.Id && candidate.Status == status);
            Assert.Contains(byContract, candidate => candidate.Id == flowDown.Id && candidate.Status == status);
        }
    }

    [Fact]
    public async Task TC_14_2_3_Link_approved_signed_evidence_to_flow_down_record()
    {
        var ids = StoryIds.ForCase("tc-14-2-3");
        await using var factory = CreateFactory("tc-14-2-3", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();
        var flowDown = await CreateFlowDownAsync(client, ids.TenantId, ids.SubcontractorId, CreateRequest(ids));

        var updated = await UpdateFlowDownAsync(
            client,
            ids.TenantId,
            ids.SubcontractorId,
            flowDown.Id,
            CreateRequest(ids) with
            {
                Status = FlowDownStatus.Signed,
                SignedAt = new DateOnly(2026, 7, 10),
                SignedEvidenceItemId = ids.SignedEvidenceItemId
            });

        Assert.Equal(FlowDownStatus.Signed, updated.Status);
        Assert.Equal(ids.SignedEvidenceItemId, updated.SignedEvidenceItemId);
        var displayed = await ListFlowDownsAsync(client, ids.TenantId, ids.SubcontractorId, ids.ContractId);
        Assert.Contains(displayed, candidate => candidate.Id == flowDown.Id && candidate.SignedEvidenceItemId == ids.SignedEvidenceItemId);
    }

    [Fact]
    public async Task TC_14_2_4_Flow_down_assignment_and_status_changes_are_audit_logged()
    {
        var ids = StoryIds.ForCase("tc-14-2-4");
        await using var factory = CreateFactory("tc-14-2-4", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var flowDown = await CreateFlowDownAsync(client, ids.TenantId, ids.SubcontractorId, CreateRequest(ids));
        await UpdateFlowDownAsync(
            client,
            ids.TenantId,
            ids.SubcontractorId,
            flowDown.Id,
            CreateRequest(ids) with { Status = FlowDownStatus.Sent, SentAt = new DateOnly(2026, 7, 1) });

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var audits = await dbContext.AuditLogEntries
            .Where(audit => audit.TenantId == ids.TenantId && audit.EntityType == "SubcontractorFlowDown" && audit.EntityId == flowDown.Id.ToString())
            .ToArrayAsync();

        Assert.Contains(audits, audit => audit.Action == AuditAction.Created && audit.MetadataJson.Contains("52.244-6", StringComparison.Ordinal));
        Assert.Contains(audits, audit => audit.Action == AuditAction.Updated && audit.MetadataJson.Contains("previousStatus", StringComparison.Ordinal));

        using var deniedRequest = CreateRequest<object?>(
            HttpMethod.Get,
            $"/api/subcontractors/{ids.SubcontractorId}/flow-downs",
            null,
            ids.OtherTenantId,
            Permission.ViewSubcontractors);
        var deniedResponse = await client.SendAsync(deniedRequest);
        Assert.Equal(HttpStatusCode.NotFound, deniedResponse.StatusCode);
    }

    private static async Task<SubcontractorFlowDownDto> CreateFlowDownAsync(
        HttpClient client,
        Guid tenantId,
        Guid subcontractorId,
        UpsertSubcontractorFlowDownRequest body)
    {
        using var request = CreateRequest(HttpMethod.Post, $"/api/subcontractors/{subcontractorId}/flow-downs", body, tenantId, Permission.ManageSubcontractors);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<SubcontractorFlowDownDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected flow-down response.");
    }

    private static async Task<SubcontractorFlowDownDto> UpdateFlowDownAsync(
        HttpClient client,
        Guid tenantId,
        Guid subcontractorId,
        Guid flowDownId,
        UpsertSubcontractorFlowDownRequest body)
    {
        using var request = CreateRequest(HttpMethod.Put, $"/api/subcontractors/{subcontractorId}/flow-downs/{flowDownId}", body, tenantId, Permission.ManageSubcontractors);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<SubcontractorFlowDownDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected flow-down response.");
    }

    private static async Task<SubcontractorFlowDownDto[]> ListFlowDownsAsync(
        HttpClient client,
        Guid tenantId,
        Guid subcontractorId,
        Guid? contractId)
    {
        var uri = contractId is null
            ? $"/api/subcontractors/{subcontractorId}/flow-downs"
            : $"/api/subcontractors/{subcontractorId}/flow-downs?contractId={contractId}";
        using var request = CreateRequest<object?>(HttpMethod.Get, uri, null, tenantId, Permission.ViewSubcontractors);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<SubcontractorFlowDownDto[]>(JsonOptions) ?? [];
    }

    private static UpsertSubcontractorFlowDownRequest CreateRequest(StoryIds ids) =>
        new(
            ids.ContractId,
            ids.ContractClauseId,
            ids.ObligationId,
            "52.244-6",
            "Subcontracts and Commercial Products and Commercial Services",
            FlowDownStatus.Required,
            null,
            null,
            null,
            null,
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
        dbContext.Contracts.Add(CreateContract(ids.TenantId, ids.ContractId));
        dbContext.Subcontractors.Add(new SubcontractorEntity
        {
            Id = ids.SubcontractorId,
            TenantId = ids.TenantId,
            Name = "Flow Down Supplier LLC",
            Status = SubcontractorStatus.Active,
            RoleDescription = "Component manufacturing",
            SmallBusinessStatus = "Small",
            CmmcStatus = "Level 1 complete",
            NdaStatus = "Executed",
            WorkshareDescription = "Assembly support",
            HasFciAccess = true,
            HasCuiAccess = false,
            HasExportControlledAccess = false,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Set<ContractSubcontractorEntity>().Add(new ContractSubcontractorEntity
        {
            ContractId = ids.ContractId,
            SubcontractorId = ids.SubcontractorId
        });
        dbContext.Clauses.Add(new ClauseEntity
        {
            Id = ids.ClauseLibraryId,
            Source = "FAR",
            Number = "52.244-6",
            Title = "Subcontracts and Commercial Products and Commercial Services",
            PlainEnglishSummary = "Flow-down seed clause.",
            ApplicabilityLogic = "When required by the contract.",
            UsuallyRequiresFlowDown = true,
            SourceName = "Acquisition source",
            SourceUrl = "https://example.test/source",
            SourceLastReviewedAt = new DateOnly(2026, 6, 15),
            LastReviewedAt = new DateOnly(2026, 6, 15),
            Confidence = "high",
            SourceConfidence = "high",
            ReviewState = ReviewState.Approved
        });
        dbContext.Obligations.Add(new ObligationEntity
        {
            Id = ids.ObligationId,
            Source = "FAR",
            Title = "Flow down subcontract clauses",
            PlainEnglishSummary = "Assign flow-down requirement.",
            TriggerCondition = "Subcontractor performs covered work.",
            RequiredAction = "Assign and track signed flow-down.",
            OwnerFunction = "Contracts",
            RiskLevel = RiskLevel.Medium,
            RequiresFlowDown = true,
            FlowDownRequirement = "Required for subcontractor.",
            SourceName = "Acquisition source",
            SourceUrl = "https://example.test/source",
            SourceLastReviewedAt = new DateOnly(2026, 6, 15),
            LastReviewedAt = new DateOnly(2026, 6, 15),
            Confidence = "high",
            SourceConfidence = "high",
            ReviewState = ReviewState.Approved
        });
        dbContext.Set<ContractClauseEntity>().Add(new ContractClauseEntity
        {
            Id = ids.ContractClauseId,
            ContractId = ids.ContractId,
            ClauseLibraryId = ids.ClauseLibraryId,
            ClauseNumber = "52.244-6",
            Title = "Subcontracts and Commercial Products and Commercial Services",
            Source = ClauseSource.Far,
            SourceUrl = "https://example.test/source",
            AttachmentReason = "Contract requires subcontractor flow-down.",
            RequiresFlowDown = true,
            LastReviewedAt = new DateOnly(2026, 6, 15),
            Confidence = "high",
            ReviewState = ReviewState.Approved,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Set<ContractClauseObligationEntity>().Add(new ContractClauseObligationEntity
        {
            ContractClauseId = ids.ContractClauseId,
            ObligationId = ids.ObligationId
        });
        dbContext.EvidenceItems.Add(new EvidenceItemEntity
        {
            Id = ids.SignedEvidenceItemId,
            TenantId = ids.TenantId,
            Name = "Signed subcontract flow-down",
            Description = "Approved signed flow-down evidence.",
            Type = EvidenceType.SignedFlowDown,
            OwnerFunction = "Contracts",
            Status = EvidenceStatus.Approved,
            ApprovedAt = DateTimeOffset.UtcNow,
            ApprovedByUserId = Guid.Parse("14214214-2142-1421-4214-214214214299"),
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private static TenantEntity CreateTenant(Guid tenantId) =>
        new()
        {
            Id = tenantId,
            Name = "Flow Down Tenant",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static ContractEntity CreateContract(Guid tenantId, Guid contractId) =>
        new()
        {
            Id = contractId,
            TenantId = tenantId,
            ContractNumber = "FLOW-2026-001",
            Title = "Flow-down support",
            AgencyOrPrimeName = "Prime Integrator",
            Relationship = ContractorRelationship.Prime,
            Kind = ContractKind.FixedPrice,
            Status = ContractStatus.Active,
            AwardedAt = new DateOnly(2026, 6, 15),
            PeriodOfPerformanceStart = new DateOnly(2026, 7, 1),
            PeriodOfPerformanceEnd = new DateOnly(2027, 6, 30),
            PlaceOfPerformance = "Arlington, VA",
            Description = "No-CUI flow-down tracking test contract.",
            DataHandlingPosture = DataHandlingPosture.FciOnly,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private sealed record StoryIds(
        Guid TenantId,
        Guid OtherTenantId,
        Guid ContractId,
        Guid SubcontractorId,
        Guid ContractClauseId,
        string ClauseLibraryId,
        string ObligationId,
        Guid SignedEvidenceItemId)
    {
        public static StoryIds ForCase(string caseName)
        {
            var suffix = Math.Abs(caseName.GetHashCode(StringComparison.Ordinal)) % 10000;
            return new StoryIds(
                Guid.Parse($"14214214-2142-1421-4214-21421421{suffix:D4}"),
                Guid.Parse($"14214214-2142-1421-4214-21421422{suffix:D4}"),
                Guid.Parse($"14214214-2142-1421-4214-21421423{suffix:D4}"),
                Guid.Parse($"14214214-2142-1421-4214-21421424{suffix:D4}"),
                Guid.Parse($"14214214-2142-1421-4214-21421425{suffix:D4}"),
                $"clause-14-2-{suffix:D4}",
                $"obligation-14-2-{suffix:D4}",
                Guid.Parse($"14214214-2142-1421-4214-21421426{suffix:D4}"));
        }
    }
}
