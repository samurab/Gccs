using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Security;
using Gccs.Application.Subcontractors;
using Gccs.Domain.Audit;
using Gccs.Domain.Companies;
using Gccs.Domain.Contracts;
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

public sealed class SubcontractorProfileTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public SubcontractorProfileTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_14_1_1_Create_and_update_subcontractor_profile_fields()
    {
        var tenantId = Guid.Parse("14114111-4114-1114-1411-4114114111a1");
        var contractId = Guid.Parse("14114111-4114-1114-1411-4114114111c1");
        await using var factory = CreateFactory("tc-14-1-1", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            SeedContract(dbContext, tenantId, contractId);
        });
        using var client = factory.CreateClient();

        var created = await CreateSubcontractorAsync(client, tenantId, CreateRequestBody([contractId]));
        var updated = await UpdateSubcontractorAsync(client, tenantId, created.Id, CreateRequestBody([contractId]) with
        {
            Name = "Updated Supplier LLC",
            Status = SubcontractorStatus.Active,
            CmmcStatus = "Level 2 self-assessment",
            WorksharePercentage = 42.5m
        });

        Assert.Equal("Updated Supplier LLC", updated.Name);
        Assert.Equal(SubcontractorStatus.Active, updated.Status);
        Assert.Equal("Level 2 self-assessment", updated.CmmcStatus);
        Assert.Equal(42.5m, updated.WorksharePercentage);
        Assert.Equal("Jane Contracts", updated.ContactName);
    }

    [Fact]
    public async Task TC_14_1_2_Subcontractor_contract_links_display_in_list_and_detail()
    {
        var tenantId = Guid.Parse("14114111-4114-1114-1411-4114114111a2");
        var contractId = Guid.Parse("14114111-4114-1114-1411-4114114111c2");
        await using var factory = CreateFactory("tc-14-1-2", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            SeedContract(dbContext, tenantId, contractId);
        });
        using var client = factory.CreateClient();

        var created = await CreateSubcontractorAsync(client, tenantId, CreateRequestBody([contractId]));
        var list = await ListSubcontractorsAsync(client, tenantId);
        var detail = await GetSubcontractorAsync(client, tenantId, created.Id);

        Assert.Contains(list, item => item.Id == created.Id && item.ContractIds.SequenceEqual([contractId]));
        Assert.Equal([contractId], detail.ContractIds);
    }

    [Fact]
    public async Task TC_14_1_3_Cui_and_export_control_flags_are_visible_without_implying_storage()
    {
        var tenantId = Guid.Parse("14114111-4114-1114-1411-4114114111a3");
        await using var factory = CreateFactory("tc-14-1-3", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();

        var created = await CreateSubcontractorAsync(client, tenantId, CreateRequestBody([]) with
        {
            HasCuiAccess = true,
            HasExportControlledAccess = true
        });

        Assert.True(created.HasCuiAccess);
        Assert.True(created.HasExportControlledAccess);
        Assert.Equal([], created.ContractIds);
    }

    [Fact]
    public async Task TC_14_1_4_Cross_tenant_access_is_denied_and_changes_are_audit_logged()
    {
        var tenantAId = Guid.Parse("14114111-4114-1114-1411-4114114111a4");
        var tenantBId = Guid.Parse("14114111-4114-1114-1411-4114114111b4");
        await using var factory = CreateFactory("tc-14-1-4", dbContext =>
        {
            SeedTenant(dbContext, tenantAId);
            SeedTenant(dbContext, tenantBId);
        });
        using var client = factory.CreateClient();
        var created = await CreateSubcontractorAsync(client, tenantAId, CreateRequestBody([]));

        using var deniedRequest = CreateRequest<object?>(HttpMethod.Get, $"/api/subcontractors/{created.Id}", null, tenantBId, Permission.ViewSubcontractors);
        var deniedResponse = await client.SendAsync(deniedRequest);
        await UpdateSubcontractorAsync(client, tenantAId, created.Id, CreateRequestBody([]) with { Status = SubcontractorStatus.Active });

        Assert.Equal(HttpStatusCode.NotFound, deniedResponse.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var audits = await dbContext.AuditLogEntries
            .Where(audit => audit.TenantId == tenantAId && audit.EntityType == "Subcontractor" && audit.EntityId == created.Id.ToString())
            .ToArrayAsync();
        Assert.Contains(audits, audit => audit.Action == AuditAction.Created);
        Assert.Contains(audits, audit => audit.Action == AuditAction.Updated);
    }

    private static async Task<SubcontractorDto> CreateSubcontractorAsync(
        HttpClient client,
        Guid tenantId,
        UpsertSubcontractorRequest body)
    {
        using var request = CreateRequest(HttpMethod.Post, "/api/subcontractors", body, tenantId, Permission.ManageSubcontractors);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<SubcontractorDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected subcontractor response.");
    }

    private static async Task<SubcontractorDto> UpdateSubcontractorAsync(
        HttpClient client,
        Guid tenantId,
        Guid subcontractorId,
        UpsertSubcontractorRequest body)
    {
        using var request = CreateRequest(HttpMethod.Put, $"/api/subcontractors/{subcontractorId}", body, tenantId, Permission.ManageSubcontractors);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<SubcontractorDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected subcontractor response.");
    }

    private static async Task<SubcontractorDto[]> ListSubcontractorsAsync(HttpClient client, Guid tenantId)
    {
        using var request = CreateRequest<object?>(HttpMethod.Get, "/api/subcontractors", null, tenantId, Permission.ViewSubcontractors);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<SubcontractorDto[]>(JsonOptions) ?? [];
    }

    private static async Task<SubcontractorDto> GetSubcontractorAsync(HttpClient client, Guid tenantId, Guid subcontractorId)
    {
        using var request = CreateRequest<object?>(HttpMethod.Get, $"/api/subcontractors/{subcontractorId}", null, tenantId, Permission.ViewSubcontractors);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<SubcontractorDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected subcontractor detail response.");
    }

    private static UpsertSubcontractorRequest CreateRequestBody(IReadOnlyList<Guid> contractIds) =>
        new(
            "Mission Supplier LLC",
            "SUBUEI123456",
            "7SUB1",
            SubcontractorStatus.Prospective,
            "CUI helpdesk support",
            "Small",
            "Level 1 complete",
            new DateOnly(2027, 1, 31),
            "Executed",
            "Tier 2 support workshare",
            35.5m,
            true,
            true,
            true,
            "Level 2",
            "Jane Contracts",
            "jane@example.com",
            "555-0100",
            "Contracts Manager",
            contractIds);

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

    private static void SeedTenant(GccsDbContext dbContext, Guid tenantId)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = "Subcontractor Tenant",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private static void SeedContract(GccsDbContext dbContext, Guid tenantId, Guid contractId)
    {
        dbContext.Contracts.Add(new ContractEntity
        {
            Id = contractId,
            TenantId = tenantId,
            ContractNumber = "SUB-2026-001",
            Title = "Subcontractor support",
            AgencyOrPrimeName = "Prime Integrator",
            Relationship = ContractorRelationship.Subcontractor,
            Kind = ContractKind.FixedPrice,
            Status = ContractStatus.Active,
            AwardedAt = new DateOnly(2026, 6, 15),
            PeriodOfPerformanceStart = new DateOnly(2026, 7, 1),
            PeriodOfPerformanceEnd = new DateOnly(2027, 6, 30),
            PlaceOfPerformance = "Arlington, VA",
            Description = "No-CUI subcontractor test contract.",
            DataHandlingPosture = DataHandlingPosture.FciOnly,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}
