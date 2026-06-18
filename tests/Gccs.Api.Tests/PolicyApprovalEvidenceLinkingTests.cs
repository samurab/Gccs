using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Compliance;
using Gccs.Application.Security;
using Gccs.Domain.Audit;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Common;
using Gccs.Domain.Companies;
using Gccs.Domain.Compliance;
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

public sealed class PolicyApprovalEvidenceLinkingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public PolicyApprovalEvidenceLinkingTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_25_3_1_and_TC_25_3_2_Authorized_user_can_approve_reject_or_revise_policy_with_metadata()
    {
        var ids = StoryIds.ForCase("tc-25-3-1");
        await using var factory = CreateFactory("tc-25-3-1", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();
        var approvedPolicy = await GeneratePolicyAsync(client, ids.TenantId);
        var rejectedPolicy = await GeneratePolicyAsync(client, ids.TenantId);

        var approved = await ReviewPolicyAsync(client, ids.TenantId, approvedPolicy.Id, new PolicyApprovalRequest(
            PolicyApprovalDecision.Approve,
            new DateOnly(2027, 6, 17),
            [ids.ObligationId],
            [ids.ControlId],
            "Approved for evidence reuse."));
        var rejected = await ReviewPolicyAsync(client, ids.TenantId, rejectedPolicy.Id, new PolicyApprovalRequest(
            PolicyApprovalDecision.Reject,
            null,
            [],
            [],
            "Needs legal review."));
        var revisionRequested = await ReviewPolicyAsync(client, ids.TenantId, approvedPolicy.Id, new PolicyApprovalRequest(
            PolicyApprovalDecision.Revise,
            null,
            [],
            [],
            "Refresh scope statement."));

        Assert.Equal(GeneratedPolicyStatus.Approved, approved.Status);
        Assert.NotNull(approved.ApprovedByUserId);
        Assert.NotNull(approved.ApprovedAt);
        Assert.Equal(new DateOnly(2027, 6, 17), approved.ReviewDueAt);
        Assert.Equal(GeneratedPolicyStatus.Rejected, rejected.Status);
        Assert.Equal(GeneratedPolicyStatus.RevisionRequested, revisionRequested.Status);
    }

    [Fact]
    public async Task TC_25_3_3_Approved_policy_links_to_obligations_and_controls_as_evidence()
    {
        var ids = StoryIds.ForCase("tc-25-3-3");
        await using var factory = CreateFactory("tc-25-3-3", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();
        var policy = await GeneratePolicyAsync(client, ids.TenantId);

        var approved = await ReviewPolicyAsync(client, ids.TenantId, policy.Id, new PolicyApprovalRequest(
            PolicyApprovalDecision.Approve,
            new DateOnly(2027, 6, 17),
            [ids.ObligationId],
            [ids.ControlId],
            "Approved."));

        Assert.NotNull(approved.EvidenceItemId);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var evidence = await dbContext.EvidenceItems
            .Include(item => item.Obligations)
            .Include(item => item.Controls)
            .SingleAsync(item => item.Id == approved.EvidenceItemId);

        Assert.Equal(EvidenceType.Policy, evidence.Type);
        Assert.Equal(EvidenceStatus.Approved, evidence.Status);
        Assert.Contains(evidence.Obligations, link => link.ObligationId == ids.ObligationId);
        Assert.Contains(evidence.Controls, link => link.ControlId == ids.ControlId);
    }

    [Fact]
    public async Task TC_25_3_4_Revisions_preserve_prior_approved_versions()
    {
        var ids = StoryIds.ForCase("tc-25-3-4");
        await using var factory = CreateFactory("tc-25-3-4", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();
        var policy = await GeneratePolicyAsync(client, ids.TenantId);
        await ReviewPolicyAsync(client, ids.TenantId, policy.Id, new PolicyApprovalRequest(PolicyApprovalDecision.Approve, new DateOnly(2027, 6, 17), [ids.ObligationId], [ids.ControlId]));
        await ReviewPolicyAsync(client, ids.TenantId, policy.Id, new PolicyApprovalRequest(PolicyApprovalDecision.Revise, null, [], [], "Update."));

        var revisions = await ListRevisionsAsync(client, ids.TenantId, policy.Id);

        Assert.Contains(revisions, revision => revision.Status == GeneratedPolicyStatus.Approved);
        Assert.Contains(revisions, revision => revision.Body.Contains("Acme Federal Services LLC", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TC_25_3_5_Policy_approval_actions_are_tenant_scoped_and_audit_logged()
    {
        var ids = StoryIds.ForCase("tc-25-3-5");
        await using var factory = CreateFactory("tc-25-3-5", dbContext =>
        {
            SeedScenario(dbContext, ids);
            SeedTenant(dbContext, ids.OtherTenantId);
        });
        using var client = factory.CreateClient();
        var policy = await GeneratePolicyAsync(client, ids.TenantId);

        using var denied = CreateRequest(HttpMethod.Put, $"/api/generated-policies/{policy.Id}/review", new PolicyApprovalRequest(PolicyApprovalDecision.Approve, null, [], []), ids.OtherTenantId, Permission.ApproveEvidence);
        var deniedResponse = await client.SendAsync(denied);
        var approved = await ReviewPolicyAsync(client, ids.TenantId, policy.Id, new PolicyApprovalRequest(PolicyApprovalDecision.Approve, new DateOnly(2027, 6, 17), [ids.ObligationId], [ids.ControlId]));

        Assert.Equal(HttpStatusCode.NotFound, deniedResponse.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var audits = await dbContext.AuditLogEntries
            .Where(audit => audit.TenantId == ids.TenantId && audit.EntityType == "GeneratedPolicy" && audit.EntityId == approved.Id.ToString())
            .ToArrayAsync();
        Assert.Contains(audits, audit => audit.Action == AuditAction.Approved && audit.MetadataJson.Contains("evidenceItemId", StringComparison.Ordinal));
    }

    private async Task<GeneratedPolicyDto> GeneratePolicyAsync(HttpClient client, Guid tenantId)
    {
        var template = await CreateTemplateAsync(client, tenantId);
        using var request = CreateRequest<object?>(HttpMethod.Post, $"/api/policy-templates/{template.Id}/generate", new { }, tenantId, Permission.ManageEvidence);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<GeneratedPolicyDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected generated policy.");
    }

    private static async Task<GeneratedPolicyDto> ReviewPolicyAsync(HttpClient client, Guid tenantId, Guid policyId, PolicyApprovalRequest body)
    {
        using var request = CreateRequest(HttpMethod.Put, $"/api/generated-policies/{policyId}/review", body, tenantId, Permission.ApproveEvidence);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<GeneratedPolicyDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected reviewed policy.");
    }

    private static async Task<PolicyRevisionDto[]> ListRevisionsAsync(HttpClient client, Guid tenantId, Guid policyId)
    {
        using var request = CreateRequest<object?>(HttpMethod.Get, $"/api/generated-policies/{policyId}/revisions", null, tenantId, Permission.ViewEvidence);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<PolicyRevisionDto[]>(JsonOptions) ?? [];
    }

    private static async Task<PolicyTemplateDto> CreateTemplateAsync(HttpClient client, Guid tenantId)
    {
        using var request = CreateRequest(HttpMethod.Post, "/api/policy-templates", Template(), tenantId, Permission.ManageObligations);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<PolicyTemplateDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected policy template.");
    }

    private static UpsertPolicyTemplateRequest Template() =>
        new(
            "Approved Access Control Policy",
            "Cybersecurity",
            "Policy for {{company_name}}.",
            ["company_name"],
            [new PolicyTemplateSourceReferenceDto("FAR 52.204-21", "https://www.acquisition.gov/far/52.204-21", new DateOnly(2026, 6, 17))],
            "v1.0",
            PolicyTemplateStatus.Approved,
            "Compliance Content",
            new DateOnly(2026, 6, 17),
            Guid.Parse("25325325-3253-2532-5325-325325325299"),
            false);

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<PolicyTemplateService>();
                services.AddScoped<IPolicyTemplateRepository, EfPolicyTemplateRepository>();
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

    private static HttpRequestMessage CreateRequest<TContent>(HttpMethod method, string requestUri, TContent content, Guid tenantId, params Permission[] permissions)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", Guid.NewGuid().ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", string.Join(",", permissions.Select(permission => permission.ToString())));
        if (content is not null)
        {
            request.Content = JsonContent.Create(content, options: JsonOptions);
        }

        return request;
    }

    private static void SeedScenario(GccsDbContext dbContext, StoryIds ids)
    {
        SeedTenant(dbContext, ids.TenantId);
        dbContext.CompanyProfiles.Add(new CompanyProfileEntity
        {
            Id = Guid.NewGuid(),
            TenantId = ids.TenantId,
            LegalEntityName = "Acme Federal Services LLC",
            ContractorRole = ContractorRole.Prime,
            ProductsAndServices = "IT services",
            EmployeeRange = CompanyRange.Small,
            RevenueRange = CompanyRange.Small,
            ItEnvironmentDescription = "Commercial cloud",
            DataHandlingPosture = DataHandlingPosture.FciOnly,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Obligations.Add(new ObligationEntity
        {
            Id = ids.ObligationId,
            Source = "FAR",
            Title = "Maintain policy evidence",
            PlainEnglishSummary = "Maintain approved policies.",
            TriggerCondition = "Contract requires policy evidence.",
            RequiredAction = "Approve and link policy.",
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
            Requirement = "Limit system access.",
            AssessmentObjective = "Verify access control policy.",
            SourceName = "NIST",
            SourceUrl = "https://example.test/nist",
            SourceLastReviewedAt = new DateOnly(2026, 6, 17)
        });
    }

    private static void SeedTenant(GccsDbContext dbContext, Guid tenantId)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = $"Policy Approval Tenant {tenantId:N}",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private sealed record StoryIds(Guid TenantId, Guid OtherTenantId, string ObligationId, string ControlId)
    {
        public static StoryIds ForCase(string caseName)
        {
            var suffix = Math.Abs(caseName.GetHashCode(StringComparison.Ordinal)) % 10000;
            return new StoryIds(
                Guid.Parse($"25325325-3253-2532-5325-32532531{suffix:D4}"),
                Guid.Parse($"25325325-3253-2532-5325-32532532{suffix:D4}"),
                $"obligation-25-3-{suffix:D4}",
                $"AC.L2-3.1.{suffix % 100:D2}");
        }
    }
}
