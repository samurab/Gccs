using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Contracts;
using Gccs.Domain.Audit;
using Gccs.Domain.Companies;
using Gccs.Domain.Contracts;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Contracts;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ContractRecordTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public ContractRecordTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_8_1_1_Create_draft_and_active_contract_records()
    {
        var tenantId = Guid.Parse("81818181-8181-8181-8181-8181818181a1");
        await using var factory = CreateFactory("tc-8-1-1", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();

        using var draftRequest = CreateRequest(
            HttpMethod.Post,
            "/api/contracts",
            CreateRequestBody("W15QKN-26-C-0001", ContractStatus.Draft),
            tenantId,
            Guid.NewGuid(),
            Permission.ManageContracts);
        var draftResponse = await client.SendAsync(draftRequest);
        var draft = await draftResponse.Content.ReadFromJsonAsync<ContractDto>(JsonOptions);
        using var activeRequest = CreateRequest(
            HttpMethod.Post,
            "/api/contracts",
            CreateRequestBody("W15QKN-26-C-0002", ContractStatus.Active),
            tenantId,
            Guid.NewGuid(),
            Permission.ManageContracts);
        var activeResponse = await client.SendAsync(activeRequest);
        var active = await activeResponse.Content.ReadFromJsonAsync<ContractDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, draftResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, activeResponse.StatusCode);
        Assert.NotNull(draft);
        Assert.NotNull(active);
        Assert.Equal(ContractStatus.Draft, draft.Status);
        Assert.Equal(ContractStatus.Active, active.Status);
        Assert.Equal(DataHandlingPosture.FciOnly, active.DataHandlingPosture);
    }

    [Fact]
    public async Task TC_8_1_2_Contract_list_is_tenant_scoped()
    {
        var tenantAId = Guid.Parse("81818181-8181-8181-8181-8181818181a2");
        var tenantBId = Guid.Parse("81818181-8181-8181-8181-8181818181b2");
        await using var factory = CreateFactory("tc-8-1-2", dbContext =>
        {
            SeedTenant(dbContext, tenantAId, "Tenant A");
            SeedTenant(dbContext, tenantBId, "Tenant B");
            dbContext.Contracts.Add(CreateContractEntity(tenantAId, "A-ONLY", "Tenant A contract"));
            dbContext.Contracts.Add(CreateContractEntity(tenantBId, "B-ONLY", "Tenant B contract"));
        });
        using var client = factory.CreateClient();

        using var tenantARequest = CreateRequest(HttpMethod.Get, "/api/contracts", tenantAId, Guid.NewGuid(), Permission.ViewContracts);
        var tenantAResponse = await client.SendAsync(tenantARequest);
        var tenantAContracts = await tenantAResponse.Content.ReadFromJsonAsync<ContractDto[]>(JsonOptions);
        using var tenantBRequest = CreateRequest(HttpMethod.Get, "/api/contracts", tenantBId, Guid.NewGuid(), Permission.ViewContracts);
        var tenantBResponse = await client.SendAsync(tenantBRequest);
        var tenantBContracts = await tenantBResponse.Content.ReadFromJsonAsync<ContractDto[]>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, tenantAResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, tenantBResponse.StatusCode);
        Assert.NotNull(tenantAContracts);
        Assert.NotNull(tenantBContracts);
        Assert.Equal(["A-ONLY"], tenantAContracts.Select(contract => contract.ContractNumber).ToArray());
        Assert.Equal(["B-ONLY"], tenantBContracts.Select(contract => contract.ContractNumber).ToArray());
    }

    [Fact]
    public async Task TC_8_1_3_Contract_detail_shows_key_fields()
    {
        var tenantId = Guid.Parse("81818181-8181-8181-8181-8181818181a3");
        await using var factory = CreateFactory("tc-8-1-3", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        using var createRequest = CreateRequest(
            HttpMethod.Post,
            "/api/contracts",
            CreateRequestBody("FA8750-26-F-0003", ContractStatus.Active),
            tenantId,
            Guid.NewGuid(),
            Permission.ManageContracts);
        var createResponse = await client.SendAsync(createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ContractDto>(JsonOptions);
        Assert.NotNull(created);

        using var detailRequest = CreateRequest(HttpMethod.Get, $"/api/contracts/{created.Id}", tenantId, Guid.NewGuid(), Permission.ViewContracts);
        var detailResponse = await client.SendAsync(detailRequest);
        var detail = await detailResponse.Content.ReadFromJsonAsync<ContractDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        Assert.NotNull(detail);
        Assert.Equal("FA8750-26-F-0003", detail.ContractNumber);
        Assert.Equal("Department of Defense", detail.AgencyOrPrimeName);
        Assert.Equal(ContractorRelationship.Subcontractor, detail.Relationship);
        Assert.Equal(ContractKind.FixedPrice, detail.Kind);
        Assert.Equal(new DateOnly(2026, 7, 1), detail.PeriodOfPerformanceStart);
        Assert.Equal(new DateOnly(2027, 6, 30), detail.PeriodOfPerformanceEnd);
        Assert.Equal(DataHandlingPosture.FciOnly, detail.DataHandlingPosture);
    }

    [Fact]
    public async Task TC_8_1_4_Contract_create_and_update_are_audit_logged()
    {
        var tenantId = Guid.Parse("81818181-8181-8181-8181-8181818181a4");
        var actorUserId = Guid.Parse("81818181-8181-8181-8181-8181818181b4");
        await using var factory = CreateFactory("tc-8-1-4", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();

        using var createRequest = CreateRequest(
            HttpMethod.Post,
            "/api/contracts",
            CreateRequestBody("N00178-26-C-0004", ContractStatus.Draft),
            tenantId,
            actorUserId,
            Permission.ManageContracts);
        var createResponse = await client.SendAsync(createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ContractDto>(JsonOptions);
        Assert.NotNull(created);
        using var updateRequest = CreateRequest(
            HttpMethod.Put,
            $"/api/contracts/{created.Id}",
            CreateRequestBody("N00178-26-C-0004", ContractStatus.Active) with { Title = "Updated support services contract" },
            tenantId,
            actorUserId,
            Permission.ManageContracts);
        var updateResponse = await client.SendAsync(updateRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var auditEvents = await dbContext.AuditLogEntries
            .Where(audit => audit.TenantId == tenantId && audit.EntityType == "Contract")
            .OrderBy(audit => audit.OccurredAt)
            .ToArrayAsync();

        Assert.Equal([AuditAction.Created, AuditAction.Updated], auditEvents.Select(audit => audit.Action).ToArray());
        Assert.All(auditEvents, audit => Assert.Equal(actorUserId, audit.ActorUserId));
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<ContractService>();
                services.AddScoped<IContractRepository, EfContractRepository>();
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

    private static UpsertContractRequest CreateRequestBody(string contractNumber, ContractStatus status) =>
        new(
            contractNumber,
            "Base operations support services",
            "Department of Defense",
            ContractorRelationship.Subcontractor,
            ContractKind.FixedPrice,
            status,
            new DateOnly(2026, 6, 15),
            new DateOnly(2026, 7, 1),
            new DateOnly(2027, 6, 30),
            "Arlington, VA",
            "No-CUI contract intake record for compliance tracking.",
            DataHandlingPosture.FciOnly);

    private static ContractEntity CreateContractEntity(Guid tenantId, string contractNumber, string title) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ContractNumber = contractNumber,
            Title = title,
            AgencyOrPrimeName = "Sample Prime",
            Relationship = ContractorRelationship.Prime,
            Kind = ContractKind.PurchaseOrder,
            Status = ContractStatus.Active,
            AwardedAt = new DateOnly(2026, 6, 15),
            PeriodOfPerformanceStart = new DateOnly(2026, 7, 1),
            PeriodOfPerformanceEnd = new DateOnly(2027, 6, 30),
            PlaceOfPerformance = "Remote",
            Description = "Seeded tenant-scoped contract.",
            DataHandlingPosture = DataHandlingPosture.NoFciOrCui,
            CreatedAt = DateTimeOffset.UtcNow
        };

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

    private static void SeedTenant(GccsDbContext dbContext, Guid tenantId, string name = "Contract Tenant")
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
}
