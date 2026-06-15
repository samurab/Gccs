using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Security;
using Gccs.Application.Tasks;
using Gccs.Domain.Audit;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Common;
using Gccs.Domain.Companies;
using Gccs.Domain.Compliance;
using Gccs.Domain.Evidence;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class RenewalGenerationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public RenewalGenerationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_11_3_1_Generates_renewal_tasks_for_profile_evidence_insurance_policy_and_cmmc_dates()
    {
        var tenantId = Guid.Parse("11311311-3113-1131-1311-3113113113a1");
        await using var factory = CreateFactory("tc-11-3-1", dbContext => SeedRenewalScenario(dbContext, tenantId));
        using var client = factory.CreateClient();

        var result = await GenerateAsync(client, tenantId, new GenerateRenewalTasksRequest(30));

        Assert.Equal(6, result.CreatedCount);
        Assert.Contains(result.Items, item => item.SourceType == "sam_registration" && item.Created);
        Assert.Contains(result.Items, item => item.SourceType == "certification" && item.Created);
        Assert.Contains(result.Items, item => item.SourceType == "evidence_expiration" && item.Created);
        Assert.Contains(result.Items, item => item.SourceType == "insurance" && item.Created);
        Assert.Contains(result.Items, item => item.SourceType == "policy_review" && item.TaskType == ComplianceTaskType.PolicyReview && item.Created);
        Assert.Contains(result.Items, item => item.SourceType == "cmmc_affirmation" && item.Created);
    }

    [Fact]
    public async Task TC_11_3_2_Running_generation_twice_skips_duplicate_source_and_due_date_tasks()
    {
        var tenantId = Guid.Parse("11311311-3113-1131-1311-3113113113a2");
        await using var factory = CreateFactory("tc-11-3-2", dbContext => SeedRenewalScenario(dbContext, tenantId));
        using var client = factory.CreateClient();

        var first = await GenerateAsync(client, tenantId, new GenerateRenewalTasksRequest(30));
        var second = await GenerateAsync(client, tenantId, new GenerateRenewalTasksRequest(30));

        Assert.Equal(6, first.CreatedCount);
        Assert.Equal(0, second.CreatedCount);
        Assert.Equal(6, second.SkippedDuplicateCount);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(6, await dbContext.ComplianceTasks.CountAsync(task => task.TenantId == tenantId));
    }

    [Fact]
    public async Task TC_11_3_3_Default_and_configured_lead_times_produce_expected_reminder_due_dates()
    {
        var defaultTenantId = Guid.Parse("11311311-3113-1131-1311-3113113113a3");
        var configuredTenantId = Guid.Parse("11311311-3113-1131-1311-3113113113b3");
        await using var factory = CreateFactory("tc-11-3-3", dbContext =>
        {
            SeedTenant(dbContext, defaultTenantId);
            SeedCompanyProfile(dbContext, defaultTenantId, Guid.Parse("11311311-3113-1131-1311-3113113113c3"), new DateOnly(2026, 8, 30));
            SeedTenant(dbContext, configuredTenantId);
            SeedCompanyProfile(dbContext, configuredTenantId, Guid.Parse("11311311-3113-1131-1311-3113113113d3"), new DateOnly(2026, 9, 30));
        });
        using var client = factory.CreateClient();

        var defaultResult = await GenerateAsync(client, defaultTenantId, new GenerateRenewalTasksRequest(null));
        var configuredResult = await GenerateAsync(client, configuredTenantId, new GenerateRenewalTasksRequest(14));

        Assert.Equal(30, defaultResult.LeadTimeDays);
        Assert.Contains(defaultResult.Items, item => item.SourceDueAt == new DateOnly(2026, 8, 30) && item.ReminderDueAt == new DateOnly(2026, 7, 31));
        Assert.Equal(14, configuredResult.LeadTimeDays);
        Assert.Contains(configuredResult.Items, item => item.SourceDueAt == new DateOnly(2026, 9, 30) && item.ReminderDueAt == new DateOnly(2026, 9, 16));
    }

    [Fact]
    public async Task TC_11_3_4_Generated_renewal_tasks_link_back_to_originating_source_records()
    {
        var tenantId = Guid.Parse("11311311-3113-1131-1311-3113113113a4");
        var profileId = Guid.Parse("11311311-3113-1131-1311-3113113113c4");
        var certificationId = Guid.Parse("11311311-3113-1131-1311-3113113113d4");
        var evidenceId = Guid.Parse("11311311-3113-1131-1311-3113113113e4");
        var affirmationId = Guid.Parse("11311311-3113-1131-1311-3113113113f4");
        await using var factory = CreateFactory("tc-11-3-4", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            SeedCompanyProfile(dbContext, tenantId, profileId, new DateOnly(2026, 8, 30), certificationId);
            SeedEvidence(dbContext, tenantId, evidenceId, "Access review evidence", EvidenceType.AccessReview, new DateOnly(2026, 9, 15), []);
            SeedAnnualAffirmation(dbContext, tenantId, affirmationId, new DateOnly(2026, 10, 15));
        });
        using var client = factory.CreateClient();

        await GenerateAsync(client, tenantId, new GenerateRenewalTasksRequest(30));

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Contains(await dbContext.ComplianceTasks.Where(task => task.TenantId == tenantId).ToArrayAsync(), task =>
            task.ControlId == $"company-profile:{profileId}:sam");
        Assert.Contains(await dbContext.ComplianceTasks.Where(task => task.TenantId == tenantId).ToArrayAsync(), task =>
            task.ControlId == $"certification:{certificationId}");
        Assert.Contains(await dbContext.ComplianceTasks.Where(task => task.TenantId == tenantId).ToArrayAsync(), task =>
            task.EvidenceItemId == evidenceId);
        Assert.Contains(await dbContext.ComplianceTasks.Where(task => task.TenantId == tenantId).ToArrayAsync(), task =>
            task.ControlId == $"cmmc-affirmation:{affirmationId}");
        Assert.Contains(await dbContext.AuditLogEntries.Where(audit => audit.TenantId == tenantId).ToArrayAsync(), audit =>
            audit.Action == AuditAction.Created && audit.EntityType == "ComplianceTaskRenewalGeneration");
    }

    private async Task<RenewalTaskGenerationResult> GenerateAsync(
        HttpClient client,
        Guid tenantId,
        GenerateRenewalTasksRequest body)
    {
        using var request = CreateRequest(HttpMethod.Post, "/api/tasks/renewals/generate", body, tenantId, Guid.NewGuid(), Permission.ManageTasks);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<RenewalTaskGenerationResult>(JsonOptions) ??
            throw new InvalidOperationException("Expected renewal generation response.");
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<RenewalGenerationService>();
                services.AddScoped<IRenewalTaskRepository, EfRenewalTaskRepository>();
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
        Guid userId,
        Permission permission)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", permission.ToString());
        request.Content = JsonContent.Create(content, options: JsonOptions);
        return request;
    }

    private static void SeedRenewalScenario(GccsDbContext dbContext, Guid tenantId)
    {
        SeedTenant(dbContext, tenantId);
        SeedCompanyProfile(dbContext, tenantId, Guid.NewGuid(), new DateOnly(2026, 8, 30), Guid.NewGuid());
        SeedEvidence(dbContext, tenantId, Guid.NewGuid(), "Access review evidence", EvidenceType.AccessReview, new DateOnly(2026, 9, 15), []);
        SeedEvidence(dbContext, tenantId, Guid.NewGuid(), "General liability insurance", EvidenceType.VendorAttestation, new DateOnly(2026, 10, 1), ["insurance"]);
        SeedEvidence(dbContext, tenantId, Guid.NewGuid(), "Access control policy", EvidenceType.Policy, new DateOnly(2026, 11, 1), ["policy"]);
        SeedAnnualAffirmation(dbContext, tenantId, Guid.NewGuid(), new DateOnly(2026, 12, 1));
    }

    private static void SeedTenant(GccsDbContext dbContext, Guid tenantId)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = "Renewal Tenant",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private static void SeedCompanyProfile(
        GccsDbContext dbContext,
        Guid tenantId,
        Guid profileId,
        DateOnly samExpiresAt,
        Guid? certificationId = null)
    {
        var profile = new CompanyProfileEntity
        {
            Id = profileId,
            TenantId = tenantId,
            LegalEntityName = "Renewal Test LLC",
            Uei = "RENEWAL12345",
            CageCode = "9Z9Z9",
            SamRegistrationExpiresAt = samExpiresAt,
            ContractorRole = ContractorRole.Subcontractor,
            ProductsAndServices = "Cybersecurity services",
            EmployeeRange = CompanyRange.Small,
            RevenueRange = CompanyRange.Small,
            ItEnvironmentDescription = "No-CUI test tenant.",
            DataHandlingPosture = DataHandlingPosture.FciOnly,
            CreatedAt = DateTimeOffset.UtcNow
        };
        profile.Certifications.Add(new CompanyCertificationEntity
        {
            Id = certificationId ?? Guid.NewGuid(),
            CompanyProfileId = profile.Id,
            Type = CertificationType.Wosb,
            Status = CertificationStatus.Active,
            Issuer = "SBA",
            EffectiveAt = new DateOnly(2026, 1, 1),
            ExpiresAt = samExpiresAt.AddDays(15),
            ReferenceNumber = "WOSB-RENEW"
        });
        dbContext.CompanyProfiles.Add(profile);
    }

    private static void SeedEvidence(
        GccsDbContext dbContext,
        Guid tenantId,
        Guid evidenceId,
        string name,
        EvidenceType type,
        DateOnly expiresAt,
        string[] tags)
    {
        dbContext.EvidenceItems.Add(new EvidenceItemEntity
        {
            Id = evidenceId,
            TenantId = tenantId,
            Name = name,
            Description = "Renewal generation source evidence.",
            Type = type,
            Status = EvidenceStatus.Approved,
            ExpiresAt = expiresAt,
            TagsJson = JsonSerializer.Serialize(tags, JsonOptions),
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private static void SeedAnnualAffirmation(GccsDbContext dbContext, Guid tenantId, Guid affirmationId, DateOnly dueAt)
    {
        dbContext.AnnualAffirmations.Add(new AnnualAffirmationEntity
        {
            Id = affirmationId,
            TenantId = tenantId,
            Level = CmmcLevel.Level1,
            DueAt = dueAt,
            Status = AffirmationStatus.DueSoon
        });
    }
}
