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

public sealed class ContractSpecificSubcontractorObligationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public ContractSpecificSubcontractorObligationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_24_3_1_and_TC_24_3_2_Linked_supplier_obligations_show_contract_owner_due_status_and_required_evidence()
    {
        var ids = StoryIds.ForCase("tc-24-3-1");
        await using var factory = CreateFactory("tc-24-3-1", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var created = await CreateSupplierObligationAsync(client, ids.TenantId, ids.SubcontractorId, CreateRequest(ids.AcceptedFlowDownId, ids.ObligationId));
        var bySubcontractor = await ListSubcontractorSupplierObligationsAsync(client, ids.TenantId, ids.SubcontractorId, null);
        var byContract = await ListContractSupplierObligationsAsync(client, ids.TenantId, ids.ContractId);

        Assert.Equal(ids.SubcontractorId, created.SubcontractorId);
        Assert.Equal(ids.ContractId, created.ContractId);
        Assert.Equal(ids.AcceptedFlowDownId, created.FlowDownClauseId);
        Assert.Equal(ids.ObligationId, created.ObligationId);
        Assert.Equal("Contracts", created.OwnerFunction);
        Assert.Equal(new DateOnly(2026, 8, 15), created.DueDate);
        Assert.Equal(SubcontractorEvidenceRequestStatus.Draft, created.Status);
        Assert.Contains(EvidenceType.SignedFlowDown, created.RequiredEvidenceTypes);
        Assert.Contains(bySubcontractor, obligation => obligation.Id == created.Id);
        Assert.Contains(byContract, obligation => obligation.Id == created.Id && obligation.SubcontractorName == "Story 24.3 Supplier");
    }

    [Fact]
    public async Task TC_24_3_3_Bulk_creation_uses_accepted_flow_downs_only()
    {
        var ids = StoryIds.ForCase("tc-24-3-3");
        await using var factory = CreateFactory("tc-24-3-3", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();
        var request = new BulkCreateSupplierObligationsRequest(
            ids.ContractId,
            new DateOnly(2026, 9, 1),
            "Supplier Management",
            [EvidenceType.SignedFlowDown, EvidenceType.SubcontractorCertification],
            SubcontractorEvidenceRequestStatus.Draft);

        var created = await BulkCreateSupplierObligationsAsync(client, ids.TenantId, ids.SubcontractorId, request);
        var all = await ListSubcontractorSupplierObligationsAsync(client, ids.TenantId, ids.SubcontractorId, ids.ContractId);

        Assert.Equal(2, created.Length);
        Assert.Contains(created, obligation => obligation.FlowDownClauseId == ids.AcceptedFlowDownId);
        Assert.Contains(created, obligation => obligation.FlowDownClauseId == ids.AcknowledgedFlowDownId);
        Assert.DoesNotContain(created, obligation => obligation.FlowDownClauseId == ids.SentFlowDownId);
        Assert.Equal(created.Select(item => item.Id).Order(), all.Select(item => item.Id).Order());
    }

    [Fact]
    public async Task TC_24_3_4_Supplier_obligations_are_tenant_scoped()
    {
        var ids = StoryIds.ForCase("tc-24-3-4");
        await using var factory = CreateFactory("tc-24-3-4", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();
        await CreateSupplierObligationAsync(client, ids.TenantId, ids.SubcontractorId, CreateRequest(ids.AcceptedFlowDownId, ids.ObligationId));
        await CreateSupplierObligationAsync(client, ids.OtherTenantId, ids.OtherSubcontractorId, CreateRequest(ids.OtherTenantFlowDownId, ids.ObligationId));

        using var denied = CreateHttpRequest<object?>(
            HttpMethod.Get,
            $"/api/subcontractors/{ids.SubcontractorId}/supplier-obligations",
            null,
            ids.OtherTenantId,
            Permission.ViewSubcontractors);
        var deniedResponse = await client.SendAsync(denied);
        var tenantItems = await ListContractSupplierObligationsAsync(client, ids.TenantId, ids.ContractId);

        Assert.Equal(HttpStatusCode.NotFound, deniedResponse.StatusCode);
        Assert.Single(tenantItems);
        Assert.DoesNotContain(tenantItems, obligation => obligation.SubcontractorId == ids.OtherSubcontractorId);
    }

    [Fact]
    public async Task TC_24_3_5_Creation_and_status_changes_are_audit_logged()
    {
        var ids = StoryIds.ForCase("tc-24-3-5");
        await using var factory = CreateFactory("tc-24-3-5", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();
        var created = await CreateSupplierObligationAsync(client, ids.TenantId, ids.SubcontractorId, CreateRequest(ids.AcceptedFlowDownId, ids.ObligationId));

        using var update = CreateHttpRequest(
            HttpMethod.Put,
            $"/api/subcontractors/{ids.SubcontractorId}/supplier-obligations/{created.Id}",
            CreateRequest(ids.AcceptedFlowDownId, ids.ObligationId) with { Status = SubcontractorEvidenceRequestStatus.Submitted },
            ids.TenantId,
            Permission.ManageSubcontractors);
        var updateResponse = await client.SendAsync(update);
        var updated = await updateResponse.Content.ReadFromJsonAsync<SupplierObligationDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.NotNull(updated);
        Assert.Equal(SubcontractorEvidenceRequestStatus.Submitted, updated.Status);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var audits = await dbContext.AuditLogEntries
            .Where(audit => audit.TenantId == ids.TenantId && audit.EntityType == "SupplierObligation" && audit.EntityId == created.Id.ToString())
            .ToArrayAsync();

        Assert.Contains(audits, audit => audit.Action == AuditAction.Created && audit.MetadataJson.Contains("SignedFlowDown", StringComparison.Ordinal));
        Assert.Contains(audits, audit => audit.Action == AuditAction.Updated && audit.MetadataJson.Contains("previousStatus", StringComparison.Ordinal));
    }

    private static async Task<SupplierObligationDto> CreateSupplierObligationAsync(
        HttpClient client,
        Guid tenantId,
        Guid subcontractorId,
        UpsertSupplierObligationRequest body)
    {
        using var request = CreateHttpRequest(HttpMethod.Post, $"/api/subcontractors/{subcontractorId}/supplier-obligations", body, tenantId, Permission.ManageSubcontractors);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<SupplierObligationDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected supplier obligation response.");
    }

    private static async Task<SupplierObligationDto[]> BulkCreateSupplierObligationsAsync(
        HttpClient client,
        Guid tenantId,
        Guid subcontractorId,
        BulkCreateSupplierObligationsRequest body)
    {
        using var request = CreateHttpRequest(HttpMethod.Post, $"/api/subcontractors/{subcontractorId}/supplier-obligations/bulk-from-flow-downs", body, tenantId, Permission.ManageSubcontractors);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<SupplierObligationDto[]>(JsonOptions) ?? [];
    }

    private static async Task<SupplierObligationDto[]> ListSubcontractorSupplierObligationsAsync(
        HttpClient client,
        Guid tenantId,
        Guid subcontractorId,
        Guid? contractId)
    {
        var query = contractId is null ? string.Empty : $"?contractId={contractId}";
        using var request = CreateHttpRequest<object?>(HttpMethod.Get, $"/api/subcontractors/{subcontractorId}/supplier-obligations{query}", null, tenantId, Permission.ViewSubcontractors);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<SupplierObligationDto[]>(JsonOptions) ?? [];
    }

    private static async Task<SupplierObligationDto[]> ListContractSupplierObligationsAsync(HttpClient client, Guid tenantId, Guid contractId)
    {
        using var request = CreateHttpRequest<object?>(HttpMethod.Get, $"/api/contracts/{contractId}/supplier-obligations", null, tenantId, Permission.ViewSubcontractors);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<SupplierObligationDto[]>(JsonOptions) ?? [];
    }

    private static UpsertSupplierObligationRequest CreateRequest(Guid flowDownId, string obligationId) =>
        new(
            flowDownId,
            "Provide signed flow-down and supplier certification",
            [EvidenceType.SignedFlowDown, EvidenceType.SubcontractorCertification],
            new DateOnly(2026, 8, 15),
            SubcontractorEvidenceRequestStatus.Draft,
            "Contracts",
            obligationId);

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

    private static HttpRequestMessage CreateHttpRequest<TContent>(
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
        dbContext.Tenants.AddRange(CreateTenant(ids.TenantId), CreateTenant(ids.OtherTenantId));
        dbContext.Contracts.AddRange(CreateContract(ids.TenantId, ids.ContractId), CreateContract(ids.OtherTenantId, ids.OtherContractId));
        dbContext.Subcontractors.AddRange(
            CreateSubcontractor(ids.TenantId, ids.SubcontractorId, "Story 24.3 Supplier"),
            CreateSubcontractor(ids.OtherTenantId, ids.OtherSubcontractorId, "Other Tenant Supplier"));
        dbContext.Set<ContractSubcontractorEntity>().AddRange(
            new ContractSubcontractorEntity { ContractId = ids.ContractId, SubcontractorId = ids.SubcontractorId },
            new ContractSubcontractorEntity { ContractId = ids.OtherContractId, SubcontractorId = ids.OtherSubcontractorId });
        dbContext.Clauses.Add(CreateClause(ids.ClauseLibraryId));
        dbContext.Obligations.Add(CreateObligation(ids.ObligationId));
        dbContext.Set<ContractClauseEntity>().AddRange(
            CreateContractClause(ids.ContractClauseId, ids.ContractId, ids.ClauseLibraryId),
            CreateContractClause(ids.OtherContractClauseId, ids.OtherContractId, ids.ClauseLibraryId));
        dbContext.Set<ContractClauseObligationEntity>().AddRange(
            new ContractClauseObligationEntity { ContractClauseId = ids.ContractClauseId, ObligationId = ids.ObligationId },
            new ContractClauseObligationEntity { ContractClauseId = ids.OtherContractClauseId, ObligationId = ids.ObligationId });
        dbContext.FlowDownClauses.AddRange(
            CreateFlowDown(ids.AcceptedFlowDownId, ids.SubcontractorId, ids.ContractId, ids.ContractClauseId, ids.ObligationId, FlowDownStatus.Signed, "52.204-21"),
            CreateFlowDown(ids.AcknowledgedFlowDownId, ids.SubcontractorId, ids.ContractId, ids.ContractClauseId, ids.ObligationId, FlowDownStatus.Acknowledged, "52.204-25"),
            CreateFlowDown(ids.SentFlowDownId, ids.SubcontractorId, ids.ContractId, ids.ContractClauseId, ids.ObligationId, FlowDownStatus.Sent, "52.204-27"),
            CreateFlowDown(ids.OtherTenantFlowDownId, ids.OtherSubcontractorId, ids.OtherContractId, ids.OtherContractClauseId, ids.ObligationId, FlowDownStatus.Signed, "52.222-41"));
    }

    private static TenantEntity CreateTenant(Guid tenantId) =>
        new()
        {
            Id = tenantId,
            Name = $"Story 24.3 Tenant {tenantId:N}",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static SubcontractorEntity CreateSubcontractor(Guid tenantId, Guid subcontractorId, string name) =>
        new()
        {
            Id = subcontractorId,
            TenantId = tenantId,
            Name = name,
            Status = SubcontractorStatus.Active,
            RoleDescription = "Supplier support",
            SmallBusinessStatus = "Small",
            CmmcStatus = "Level 1 complete",
            NdaStatus = "Executed",
            WorkshareDescription = "Flow-down workshare",
            HasFciAccess = true,
            HasCuiAccess = false,
            HasExportControlledAccess = false,
            OwnerFunction = "Supplier Management",
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static ContractEntity CreateContract(Guid tenantId, Guid contractId) =>
        new()
        {
            Id = contractId,
            TenantId = tenantId,
            ContractNumber = $"SUBOB-{contractId.ToString("N")[..6]}",
            Title = "Supplier obligation contract",
            AgencyOrPrimeName = "Prime Integrator",
            Relationship = ContractorRelationship.Prime,
            Kind = ContractKind.FixedPrice,
            Status = ContractStatus.Active,
            AwardedAt = new DateOnly(2026, 6, 15),
            PeriodOfPerformanceStart = new DateOnly(2026, 7, 1),
            PeriodOfPerformanceEnd = new DateOnly(2027, 6, 30),
            PlaceOfPerformance = "Arlington, VA",
            Description = "No-CUI contract-specific subcontractor obligation test.",
            DataHandlingPosture = DataHandlingPosture.FciOnly,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static ClauseEntity CreateClause(string clauseLibraryId) =>
        new()
        {
            Id = clauseLibraryId,
            Source = "FAR",
            Number = "52.204-21",
            Title = "Basic Safeguarding of Covered Contractor Information Systems",
            PlainEnglishSummary = "Safeguarding seed clause.",
            ApplicabilityLogic = "When FCI is handled.",
            UsuallyRequiresFlowDown = true,
            SourceName = "Acquisition source",
            SourceUrl = "https://example.test/source",
            SourceLastReviewedAt = new DateOnly(2026, 6, 15),
            LastReviewedAt = new DateOnly(2026, 6, 15),
            Confidence = "high",
            SourceConfidence = "high",
            ReviewState = ReviewState.Approved
        };

    private static ObligationEntity CreateObligation(string obligationId) =>
        new()
        {
            Id = obligationId,
            Source = "FAR",
            Title = "Collect supplier flow-down evidence",
            PlainEnglishSummary = "Track supplier proof for accepted flow-downs.",
            TriggerCondition = "Subcontractor has accepted flow-down obligations.",
            RequiredAction = "Collect evidence by the due date.",
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
        };

    private static ContractClauseEntity CreateContractClause(Guid contractClauseId, Guid contractId, string clauseLibraryId) =>
        new()
        {
            Id = contractClauseId,
            ContractId = contractId,
            ClauseLibraryId = clauseLibraryId,
            ClauseNumber = "52.204-21",
            Title = "Basic Safeguarding of Covered Contractor Information Systems",
            Source = ClauseSource.Far,
            SourceUrl = "https://example.test/source",
            AttachmentReason = "Contract requires subcontractor flow-down.",
            RequiresFlowDown = true,
            LastReviewedAt = new DateOnly(2026, 6, 15),
            Confidence = "high",
            ReviewState = ReviewState.Approved,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static FlowDownClauseEntity CreateFlowDown(
        Guid flowDownId,
        Guid subcontractorId,
        Guid contractId,
        Guid contractClauseId,
        string obligationId,
        FlowDownStatus status,
        string clauseNumber) =>
        new()
        {
            Id = flowDownId,
            SubcontractorId = subcontractorId,
            ContractId = contractId,
            ContractClauseId = contractClauseId,
            ObligationId = obligationId,
            ClauseNumber = clauseNumber,
            Title = $"Supplier flow-down {clauseNumber}",
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private sealed record StoryIds(
        Guid TenantId,
        Guid OtherTenantId,
        Guid ContractId,
        Guid OtherContractId,
        Guid SubcontractorId,
        Guid OtherSubcontractorId,
        Guid ContractClauseId,
        Guid OtherContractClauseId,
        string ClauseLibraryId,
        string ObligationId,
        Guid AcceptedFlowDownId,
        Guid AcknowledgedFlowDownId,
        Guid SentFlowDownId,
        Guid OtherTenantFlowDownId)
    {
        public static StoryIds ForCase(string caseName)
        {
            var suffix = Math.Abs(caseName.GetHashCode(StringComparison.Ordinal)) % 10000;
            return new StoryIds(
                Guid.Parse($"24324324-3243-2432-4324-32432431{suffix:D4}"),
                Guid.Parse($"24324324-3243-2432-4324-32432432{suffix:D4}"),
                Guid.Parse($"24324324-3243-2432-4324-32432433{suffix:D4}"),
                Guid.Parse($"24324324-3243-2432-4324-32432434{suffix:D4}"),
                Guid.Parse($"24324324-3243-2432-4324-32432435{suffix:D4}"),
                Guid.Parse($"24324324-3243-2432-4324-32432436{suffix:D4}"),
                Guid.Parse($"24324324-3243-2432-4324-32432437{suffix:D4}"),
                Guid.Parse($"24324324-3243-2432-4324-32432438{suffix:D4}"),
                $"clause-24-3-{suffix:D4}",
                $"obligation-24-3-{suffix:D4}",
                Guid.Parse($"24324324-3243-2432-4324-32432439{suffix:D4}"),
                Guid.Parse($"24324324-3243-2432-4324-32432440{suffix:D4}"),
                Guid.Parse($"24324324-3243-2432-4324-32432441{suffix:D4}"),
                Guid.Parse($"24324324-3243-2432-4324-32432442{suffix:D4}"));
        }
    }
}
