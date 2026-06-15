using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Contracts;
using Gccs.Application.NoCui;
using Gccs.Domain.Audit;
using Gccs.Domain.Companies;
using Gccs.Domain.Common;
using Gccs.Domain.Contracts;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Contracts;
using Gccs.Infrastructure.NoCui;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ContractClauseAttachmentTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public ContractClauseAttachmentTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_9_2_1_Attach_published_clause_to_contract_with_reason_and_source_reference()
    {
        var tenantId = Guid.Parse("92929292-9292-9292-9292-9292929292a1");
        var contractId = Guid.Parse("92929292-9292-9292-9292-9292929292b1");
        await using var factory = CreateFactory("tc-9-2-1", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            dbContext.Contracts.Add(CreateContract(tenantId, contractId));
            dbContext.Clauses.Add(CreateClause("far-52-204-21", "52.204-21", "Basic Safeguarding"));
        });
        using var client = factory.CreateClient();

        using var attachRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/contracts/{contractId}/clauses",
            new AttachContractClauseRequest("far-52-204-21", "Required by award package.", "sow.pdf section 7"),
            tenantId,
            Guid.NewGuid(),
            Permission.ManageContracts);
        var attachResponse = await client.SendAsync(attachRequest);
        var attached = await attachResponse.Content.ReadFromJsonAsync<ContractClauseDto>(JsonOptions);
        using var listRequest = CreateRequest(HttpMethod.Get, $"/api/contracts/{contractId}/clauses", tenantId, Guid.NewGuid(), Permission.ViewContracts);
        var listResponse = await client.SendAsync(listRequest);
        var listed = await listResponse.Content.ReadFromJsonAsync<ContractClauseDto[]>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, attachResponse.StatusCode);
        Assert.NotNull(attached);
        Assert.Equal("52.204-21", attached.ClauseNumber);
        Assert.Equal("Required by award package.", attached.AttachmentReason);
        Assert.Equal("sow.pdf section 7", attached.SourceDocumentReference);
        Assert.Equal("https://www.acquisition.gov/far/52.204-21", attached.SourceUrl);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Equal([attached.Id], (listed ?? []).Select(clause => clause.Id).ToArray());
    }

    [Fact]
    public async Task TC_9_2_2_Duplicate_clause_attachment_is_prevented()
    {
        var tenantId = Guid.Parse("92929292-9292-9292-9292-9292929292a2");
        var contractId = Guid.Parse("92929292-9292-9292-9292-9292929292b2");
        await using var factory = CreateFactory("tc-9-2-2", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            dbContext.Contracts.Add(CreateContract(tenantId, contractId));
            dbContext.Clauses.Add(CreateClause("far-52-204-25", "52.204-25", "Telecom Restrictions"));
        });
        using var client = factory.CreateClient();

        using var firstRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/contracts/{contractId}/clauses",
            new AttachContractClauseRequest("far-52-204-25", "Prime flow-down requires it.", "flowdown.pdf"),
            tenantId,
            Guid.NewGuid(),
            Permission.ManageContracts);
        var firstResponse = await client.SendAsync(firstRequest);
        using var duplicateRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/contracts/{contractId}/clauses",
            new AttachContractClauseRequest("far-52-204-25", "Duplicate attempt.", "flowdown.pdf"),
            tenantId,
            Guid.NewGuid(),
            Permission.ManageContracts);
        var duplicateResponse = await client.SendAsync(duplicateRequest);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task TC_9_2_3_Removing_clause_requires_reason_and_then_succeeds()
    {
        var tenantId = Guid.Parse("92929292-9292-9292-9292-9292929292a3");
        var contractId = Guid.Parse("92929292-9292-9292-9292-9292929292b3");
        await using var factory = CreateFactory("tc-9-2-3", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            dbContext.Contracts.Add(CreateContract(tenantId, contractId));
            dbContext.Clauses.Add(CreateClause("far-52-204-27", "52.204-27", "ByteDance Covered Application"));
        });
        using var client = factory.CreateClient();
        var clause = await AttachClauseAsync(client, tenantId, contractId, "far-52-204-27");

        using var invalidRemoveRequest = CreateRequest(
            HttpMethod.Delete,
            $"/api/contracts/{contractId}/clauses/{clause.Id}",
            new RemoveContractClauseRequest(" "),
            tenantId,
            Guid.NewGuid(),
            Permission.ManageContracts);
        var invalidRemoveResponse = await client.SendAsync(invalidRemoveRequest);
        using var validRemoveRequest = CreateRequest(
            HttpMethod.Delete,
            $"/api/contracts/{contractId}/clauses/{clause.Id}",
            new RemoveContractClauseRequest("Clause was removed from the revised PO."),
            tenantId,
            Guid.NewGuid(),
            Permission.ManageContracts);
        var validRemoveResponse = await client.SendAsync(validRemoveRequest);
        using var listRequest = CreateRequest(HttpMethod.Get, $"/api/contracts/{contractId}/clauses", tenantId, Guid.NewGuid(), Permission.ViewContracts);
        var listResponse = await client.SendAsync(listRequest);
        var listed = await listResponse.Content.ReadFromJsonAsync<ContractClauseDto[]>(JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, invalidRemoveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, validRemoveResponse.StatusCode);
        Assert.Empty(listed ?? []);
    }

    [Fact]
    public async Task TC_9_2_4_Add_remove_are_audit_logged_and_cross_tenant_ids_are_denied()
    {
        var tenantAId = Guid.Parse("92929292-9292-9292-9292-9292929292a4");
        var tenantBId = Guid.Parse("92929292-9292-9292-9292-9292929292b4");
        var contractAId = Guid.Parse("92929292-9292-9292-9292-9292929292c4");
        var contractBId = Guid.Parse("92929292-9292-9292-9292-9292929292d4");
        var actorUserId = Guid.Parse("92929292-9292-9292-9292-9292929292e4");
        await using var factory = CreateFactory("tc-9-2-4", dbContext =>
        {
            SeedTenant(dbContext, tenantAId, "Tenant A");
            SeedTenant(dbContext, tenantBId, "Tenant B");
            dbContext.Contracts.Add(CreateContract(tenantAId, contractAId));
            dbContext.Contracts.Add(CreateContract(tenantBId, contractBId));
            dbContext.Clauses.Add(CreateClause("global-far", "52.204-21", "Basic Safeguarding"));
            dbContext.Clauses.Add(CreateClause("tenant-b-custom", "CUSTOM-B", "Tenant B Custom Clause", ReviewState.Published, tenantBId));
        });
        using var client = factory.CreateClient();

        var attached = await AttachClauseAsync(client, tenantAId, contractAId, "global-far", actorUserId);
        using var removeRequest = CreateRequest(
            HttpMethod.Delete,
            $"/api/contracts/{contractAId}/clauses/{attached.Id}",
            new RemoveContractClauseRequest("Removed after customer confirmed clause did not apply."),
            tenantAId,
            actorUserId,
            Permission.ManageContracts);
        var removeResponse = await client.SendAsync(removeRequest);
        using var crossTenantContractRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/contracts/{contractBId}/clauses",
            new AttachContractClauseRequest("global-far", "Wrong tenant contract.", null),
            tenantAId,
            actorUserId,
            Permission.ManageContracts);
        var crossTenantContractResponse = await client.SendAsync(crossTenantContractRequest);
        using var crossTenantClauseRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/contracts/{contractAId}/clauses",
            new AttachContractClauseRequest("tenant-b-custom", "Wrong tenant clause.", null),
            tenantAId,
            actorUserId,
            Permission.ManageContracts);
        var crossTenantClauseResponse = await client.SendAsync(crossTenantClauseRequest);

        Assert.Equal(HttpStatusCode.OK, removeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, crossTenantContractResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, crossTenantClauseResponse.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var auditEvents = await dbContext.AuditLogEntries
            .Where(audit => audit.TenantId == tenantAId && audit.EntityType == "ContractClause")
            .OrderBy(audit => audit.OccurredAt)
            .ToArrayAsync();

        Assert.Equal([AuditAction.Created, AuditAction.Deleted], auditEvents.Select(audit => audit.Action).ToArray());
        Assert.All(auditEvents, audit => Assert.Equal(actorUserId, audit.ActorUserId));
    }

    private async Task<ContractClauseDto> AttachClauseAsync(
        HttpClient client,
        Guid tenantId,
        Guid contractId,
        string clauseLibraryId,
        Guid? actorUserId = null)
    {
        using var request = CreateRequest(
            HttpMethod.Post,
            $"/api/contracts/{contractId}/clauses",
            new AttachContractClauseRequest(clauseLibraryId, "Clause applies to this contract.", "contract.pdf"),
            tenantId,
            actorUserId ?? Guid.NewGuid(),
            Permission.ManageContracts);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<ContractClauseDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected contract clause response.");
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
                services.AddScoped<INoCuiAcknowledgementRepository, EfNoCuiAcknowledgementRepository>();
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

    private static ContractEntity CreateContract(Guid tenantId, Guid contractId) =>
        new()
        {
            Id = contractId,
            TenantId = tenantId,
            ContractNumber = $"CON-{contractId.ToString("N")[..6]}",
            Title = "Clause attachment contract",
            AgencyOrPrimeName = "Sample Prime",
            Relationship = ContractorRelationship.Prime,
            Kind = ContractKind.FixedPrice,
            Status = ContractStatus.Active,
            AwardedAt = new DateOnly(2026, 6, 15),
            PeriodOfPerformanceStart = new DateOnly(2026, 7, 1),
            PeriodOfPerformanceEnd = new DateOnly(2027, 6, 30),
            PlaceOfPerformance = "Remote",
            Description = "Seeded contract for clause attachment.",
            DataHandlingPosture = DataHandlingPosture.FciOnly,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static ClauseEntity CreateClause(
        string id,
        string number,
        string title,
        ReviewState reviewState = ReviewState.Published,
        Guid? tenantId = null) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            Source = number.StartsWith("CUSTOM", StringComparison.OrdinalIgnoreCase) ? "Custom" : $"FAR {number}",
            Number = number,
            Title = title,
            PlainEnglishSummary = $"{title} summary.",
            ApplicabilityLogic = "Clause appears in a contract.",
            ClauseTextVersion = "current",
            RequiredActionIdsJson = "[]",
            UsuallyRequiresFlowDown = true,
            SourceName = number,
            SourceUrl = number.StartsWith("CUSTOM", StringComparison.OrdinalIgnoreCase)
                ? "https://example.com/custom-clause"
                : $"https://www.acquisition.gov/far/{number}",
            SourceLastReviewedAt = new DateOnly(2026, 6, 3),
            SourceConfidence = "high",
            LastReviewedAt = new DateOnly(2026, 6, 3),
            Confidence = "high",
            ReviewState = reviewState
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

    private static void SeedTenant(GccsDbContext dbContext, Guid tenantId, string name = "Clause Attachment Tenant")
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
