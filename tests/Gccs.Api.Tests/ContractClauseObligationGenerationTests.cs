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
using Gccs.Domain.Compliance;
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

public sealed class ContractClauseObligationGenerationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public ContractClauseObligationGenerationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_9_3_1_Attaching_clause_with_mapped_templates_generates_contract_obligations()
    {
        var tenantId = Guid.Parse("93939393-9393-9393-9393-9393939393a1");
        var contractId = Guid.Parse("93939393-9393-9393-9393-9393939393b1");
        await using var factory = CreateFactory("tc-9-3-1", dbContext => SeedScenario(dbContext, tenantId, contractId));
        using var client = factory.CreateClient();

        var attached = await AttachClauseAsync(client, tenantId, contractId);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var mapping = await dbContext.Set<ContractClauseObligationEntity>().SingleAsync(mapping => mapping.ContractClauseId == attached.Id);

        Assert.Equal("far-52-204-21", attached.ClauseLibraryId);
        Assert.Equal("obligation-fci-safeguards", mapping.ObligationId);
    }

    [Fact]
    public async Task TC_9_3_2_Generated_obligations_preserve_links_and_source_metadata()
    {
        var tenantId = Guid.Parse("93939393-9393-9393-9393-9393939393a2");
        var contractId = Guid.Parse("93939393-9393-9393-9393-9393939393b2");
        await using var factory = CreateFactory("tc-9-3-2", dbContext => SeedScenario(dbContext, tenantId, contractId));
        using var client = factory.CreateClient();

        var attached = await AttachClauseAsync(client, tenantId, contractId);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var mapping = await dbContext.Set<ContractClauseObligationEntity>()
            .Include(mapping => mapping.Obligation)
            .SingleAsync(mapping => mapping.ContractClauseId == attached.Id);

        Assert.Equal(attached.Id, mapping.ContractClauseId);
        Assert.NotNull(mapping.Obligation);
        Assert.Equal("https://www.acquisition.gov/far/52.204-21", mapping.Obligation.SourceUrl);
        Assert.Equal("IT/security", mapping.Obligation.OwnerFunction);
        Assert.Equal("Apply basic safeguarding controls.", mapping.Obligation.RequiredAction);
        Assert.Contains("Access control policy", mapping.Obligation.EvidenceExamplesJson);
        Assert.Equal(RiskLevel.High, mapping.Obligation.RiskLevel);
        Assert.Equal("high", mapping.Obligation.Confidence);
        Assert.Equal(ReviewState.Published, mapping.Obligation.ReviewState);
        Assert.Equal(new DateOnly(2026, 6, 3), mapping.Obligation.LastReviewedAt);
    }

    [Fact]
    public async Task TC_9_3_3_Default_tasks_are_created_and_linked_to_generated_obligations()
    {
        var tenantId = Guid.Parse("93939393-9393-9393-9393-9393939393a3");
        var contractId = Guid.Parse("93939393-9393-9393-9393-9393939393b3");
        await using var factory = CreateFactory("tc-9-3-3", dbContext => SeedScenario(dbContext, tenantId, contractId));
        using var client = factory.CreateClient();

        await AttachClauseAsync(client, tenantId, contractId);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var task = await dbContext.ComplianceTasks.SingleAsync(task =>
            task.TenantId == tenantId &&
            task.ContractId == contractId &&
            task.ObligationId == "obligation-fci-safeguards");

        Assert.Equal(ComplianceTaskType.ObligationAction, task.Type);
        Assert.Equal(ComplianceTaskStatus.Open, task.Status);
        Assert.Equal("IT/security", task.OwnerFunction);
        Assert.Equal(RiskLevel.High, task.RiskLevel);
    }

    [Fact]
    public async Task TC_9_3_4_Regeneration_is_idempotent_for_obligations_and_tasks()
    {
        var tenantId = Guid.Parse("93939393-9393-9393-9393-9393939393a4");
        var contractId = Guid.Parse("93939393-9393-9393-9393-9393939393b4");
        await using var factory = CreateFactory("tc-9-3-4", dbContext => SeedScenario(dbContext, tenantId, contractId));
        using var client = factory.CreateClient();
        var attached = await AttachClauseAsync(client, tenantId, contractId);

        using var regenerateRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/contracts/{contractId}/clauses/{attached.Id}/obligations/generate",
            tenantId,
            Guid.NewGuid(),
            Permission.ManageContracts);
        var regenerateResponse = await client.SendAsync(regenerateRequest);
        var generated = await regenerateResponse.Content.ReadFromJsonAsync<GeneratedContractObligationsDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, regenerateResponse.StatusCode);
        Assert.NotNull(generated);
        Assert.Equal(["obligation-fci-safeguards"], generated.ObligationIds);
        Assert.Equal(0, generated.TasksCreated);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();

        Assert.Equal(1, await dbContext.Set<ContractClauseObligationEntity>().CountAsync(mapping => mapping.ContractClauseId == attached.Id));
        Assert.Equal(1, await dbContext.ComplianceTasks.CountAsync(task => task.ContractId == contractId && task.ObligationId == "obligation-fci-safeguards"));
    }

    private async Task<ContractClauseDto> AttachClauseAsync(HttpClient client, Guid tenantId, Guid contractId)
    {
        using var request = CreateRequest(
            HttpMethod.Post,
            $"/api/contracts/{contractId}/clauses",
            new AttachContractClauseRequest("far-52-204-21", "Mapped clause applies.", "contract.pdf"),
            tenantId,
            Guid.NewGuid(),
            Permission.ManageContracts);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<ContractClauseDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected attached clause response.");
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

    private static void SeedScenario(GccsDbContext dbContext, Guid tenantId, Guid contractId)
    {
        SeedTenant(dbContext, tenantId);
        dbContext.Contracts.Add(CreateContract(tenantId, contractId));
        dbContext.Clauses.Add(CreateClause());
        dbContext.Obligations.Add(CreateObligation());
    }

    private static ContractEntity CreateContract(Guid tenantId, Guid contractId) =>
        new()
        {
            Id = contractId,
            TenantId = tenantId,
            ContractNumber = $"CON-{contractId.ToString("N")[..6]}",
            Title = "Generated obligation contract",
            AgencyOrPrimeName = "Sample Prime",
            Relationship = ContractorRelationship.Prime,
            Kind = ContractKind.FixedPrice,
            Status = ContractStatus.Active,
            AwardedAt = new DateOnly(2026, 6, 15),
            PeriodOfPerformanceStart = new DateOnly(2026, 7, 1),
            PeriodOfPerformanceEnd = new DateOnly(2027, 6, 30),
            PlaceOfPerformance = "Remote",
            Description = "Seeded contract for generated obligations.",
            DataHandlingPosture = DataHandlingPosture.FciOnly,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static ClauseEntity CreateClause() =>
        new()
        {
            Id = "far-52-204-21",
            Source = "FAR 52.204-21",
            Number = "52.204-21",
            Title = "Basic Safeguarding",
            PlainEnglishSummary = "Protect FCI.",
            ApplicabilityLogic = "Contract involves FCI.",
            ClauseTextVersion = "current",
            RequiredActionIdsJson = """["obligation-fci-safeguards"]""",
            UsuallyRequiresFlowDown = true,
            SourceName = "FAR 52.204-21",
            SourceUrl = "https://www.acquisition.gov/far/52.204-21",
            SourceLastReviewedAt = new DateOnly(2026, 6, 3),
            SourceConfidence = "high",
            LastReviewedAt = new DateOnly(2026, 6, 3),
            Confidence = "high",
            ReviewState = ReviewState.Published
        };

    private static ObligationEntity CreateObligation() =>
        new()
        {
            Id = "obligation-fci-safeguards",
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
            EvidenceExamplesJson = """["Access control policy"]""",
            SourceName = "FAR 52.204-21",
            SourceUrl = "https://www.acquisition.gov/far/52.204-21",
            SourceLastReviewedAt = new DateOnly(2026, 6, 3),
            SourceConfidence = "high",
            LastReviewedAt = new DateOnly(2026, 6, 3),
            Confidence = "high",
            ReviewState = ReviewState.Published
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

    private static void SeedTenant(GccsDbContext dbContext, Guid tenantId)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = "Generated Obligation Tenant",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}
