using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Compliance;
using Gccs.Domain.Audit;
using Gccs.Domain.Identity;
using Gccs.Infrastructure.Compliance;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class SspNarrativeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;
    public SspNarrativeTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task TC_29_2_1_Generates_draft_only_narrative_from_approved_current_tenant_sources()
    {
        using var client = CreateClient();
        var ids = Ids();
        var section = await CreateSectionAsync(client, ids);
        var narrative = await GenerateNarrativeAsync(client, ids, section.Id, aiAssisted: true);

        Assert.Equal(SspNarrativeStatus.Draft, narrative.Status);
        Assert.True(narrative.DraftOnly);
        Assert.True(narrative.AiAssisted);
        Assert.Contains("Draft AI-assisted", narrative.GeneratedText);
        Assert.All(narrative.SourceRecords, source =>
        {
            Assert.Equal(ids.TenantId, source.TenantId);
            Assert.True(source.Approved);
        });
    }

    [Fact]
    public async Task TC_29_2_2_Generation_rejects_unapproved_or_cross_tenant_sources()
    {
        using var client = CreateClient();
        var ids = Ids();
        var section = await CreateSectionAsync(client, ids);

        var unapproved = await client.SendAsync(Request(HttpMethod.Post, $"/api/compliance/ssp/sections/{section.Id}/narratives", new GenerateSspNarrativeDraftRequest([Source(ids.TenantId) with { Approved = false }], false), ids));
        Assert.Equal(HttpStatusCode.BadRequest, unapproved.StatusCode);

        var crossTenant = await client.SendAsync(Request(HttpMethod.Post, $"/api/compliance/ssp/sections/{section.Id}/narratives", new GenerateSspNarrativeDraftRequest([Source(Guid.NewGuid())], false), ids));
        Assert.Equal(HttpStatusCode.BadRequest, crossTenant.StatusCode);
    }

    [Fact]
    public async Task TC_29_2_3_Approval_blocks_missing_sources_placeholders_and_outdated_references()
    {
        using var client = CreateClient();
        var ids = Ids();
        var section = await CreateSectionAsync(client, ids);
        var narrative = await GenerateNarrativeAsync(client, ids, section.Id);

        var edited = await EditNarrativeAsync(client, ids, section.Id, narrative.Id, "Implemented boundary uses {{missing asset}}.");
        Assert.True(edited.DraftOnly);

        var placeholderApproval = await client.SendAsync(Request(HttpMethod.Post, $"/api/compliance/ssp/sections/{section.Id}/narratives/{narrative.Id}/approve", new ApproveSspNarrativeRequest("reviewer", new DateOnly(2026, 6, 19)), ids));
        Assert.Equal(HttpStatusCode.BadRequest, placeholderApproval.StatusCode);

        var outdated = await GenerateNarrativeAsync(client, ids, section.Id, source: Source(ids.TenantId) with { Outdated = true });
        var outdatedApproval = await client.SendAsync(Request(HttpMethod.Post, $"/api/compliance/ssp/sections/{section.Id}/narratives/{outdated.Id}/approve", new ApproveSspNarrativeRequest("reviewer", new DateOnly(2026, 6, 19)), ids));
        Assert.Equal(HttpStatusCode.BadRequest, outdatedApproval.StatusCode);
    }

    [Fact]
    public async Task TC_29_2_4_Approved_narrative_comparison_shows_current_and_proposed_text()
    {
        using var client = CreateClient();
        var ids = Ids();
        var section = await CreateSectionAsync(client, ids);
        var first = await GenerateNarrativeAsync(client, ids, section.Id);
        first = await EditNarrativeAsync(client, ids, section.Id, first.Id, "Approved boundary narrative.");
        await ApproveNarrativeAsync(client, ids, section.Id, first.Id);

        var proposed = await GenerateNarrativeAsync(client, ids, section.Id, source: Source(ids.TenantId) with { Summary = "Updated implementation detail." });
        proposed = await EditNarrativeAsync(client, ids, section.Id, proposed.Id, "Proposed boundary narrative.");

        var response = await client.SendAsync(Request(HttpMethod.Get, $"/api/compliance/ssp/sections/{section.Id}/narratives/{proposed.Id}/comparison", ids));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var comparison = Assert.IsType<SspNarrativeComparisonDto>(await response.Content.ReadFromJsonAsync<SspNarrativeComparisonDto>(JsonOptions));

        Assert.Equal(first.Id, comparison.ApprovedNarrativeId);
        Assert.Equal(proposed.Id, comparison.DraftNarrativeId);
        Assert.Equal("Approved boundary narrative.", comparison.CurrentApprovedText);
        Assert.Equal("Proposed boundary narrative.", comparison.ProposedText);
    }

    [Fact]
    public async Task TC_29_2_5_Narrative_generation_edit_and_approval_are_audit_logged()
    {
        var audit = new CapturingAuditWriter();
        using var client = CreateClient(audit);
        var ids = Ids();
        var section = await CreateSectionAsync(client, ids);
        var narrative = await GenerateNarrativeAsync(client, ids, section.Id);
        await EditNarrativeAsync(client, ids, section.Id, narrative.Id, "Reviewed narrative text.");
        await ApproveNarrativeAsync(client, ids, section.Id, narrative.Id);

        Assert.Contains(audit.Events, item => item.EntityType == "SspNarrative" && item.Action == AuditAction.Created);
        Assert.Contains(audit.Events, item => item.EntityType == "SspNarrative" && item.Action == AuditAction.Updated);
        Assert.Contains(audit.Events, item => item.EntityType == "SspNarrative" && item.Action == AuditAction.Approved);
    }

    private static async Task<SspSectionDto> CreateSectionAsync(HttpClient client, TestIds ids)
    {
        var response = await client.SendAsync(Request(HttpMethod.Post, "/api/compliance/ssp/sections", new CreateSspSectionRequest(
            SspSectionType.ControlImplementationNarratives,
            "Control implementation narratives",
            "security owner",
            [new SspLinkedRecordDto("cmmcControl", "AC.L2-3.1.1", "Narrative source.")],
            [new SspSourceReferenceDto("NIST SP 800-171 Rev. 2", "https://csrc.nist.gov/publications/detail/sp/800-171/rev-2/final", new DateOnly(2026, 6, 19))]), ids));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<SspSectionDto>(await response.Content.ReadFromJsonAsync<SspSectionDto>(JsonOptions));
    }

    private static async Task<SspNarrativeDto> GenerateNarrativeAsync(HttpClient client, TestIds ids, Guid sectionId, bool aiAssisted = false, SspNarrativeSourceRecordDto? source = null)
    {
        var response = await client.SendAsync(Request(HttpMethod.Post, $"/api/compliance/ssp/sections/{sectionId}/narratives", new GenerateSspNarrativeDraftRequest([source ?? Source(ids.TenantId)], aiAssisted, "SME should review."), ids));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<SspNarrativeDto>(await response.Content.ReadFromJsonAsync<SspNarrativeDto>(JsonOptions));
    }

    private static async Task<SspNarrativeDto> EditNarrativeAsync(HttpClient client, TestIds ids, Guid sectionId, Guid narrativeId, string text)
    {
        var response = await client.SendAsync(Request(HttpMethod.Put, $"/api/compliance/ssp/sections/{sectionId}/narratives/{narrativeId}", new EditSspNarrativeDraftRequest(text, "edited"), ids));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Assert.IsType<SspNarrativeDto>(await response.Content.ReadFromJsonAsync<SspNarrativeDto>(JsonOptions));
    }

    private static async Task<SspNarrativeDto> ApproveNarrativeAsync(HttpClient client, TestIds ids, Guid sectionId, Guid narrativeId)
    {
        var response = await client.SendAsync(Request(HttpMethod.Post, $"/api/compliance/ssp/sections/{sectionId}/narratives/{narrativeId}/approve", new ApproveSspNarrativeRequest("security reviewer", new DateOnly(2026, 6, 19)), ids));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Assert.IsType<SspNarrativeDto>(await response.Content.ReadFromJsonAsync<SspNarrativeDto>(JsonOptions));
    }

    private HttpClient CreateClient(IAuditEventWriter? auditWriter = null)
    {
        auditWriter ??= new CapturingAuditWriter();
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                var repository = new InMemorySspSectionRepository();
                services.AddSingleton<ISspSectionRepository>(repository);
                services.AddSingleton<ISspNarrativeRepository>(repository);
                services.AddScoped<SspSectionService>();
                services.AddSingleton(auditWriter);
            });
        }).CreateClient();
    }

    private static HttpRequestMessage Request<T>(HttpMethod method, string uri, T body, TestIds ids)
    {
        var request = Request(method, uri, ids);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        return request;
    }

    private static HttpRequestMessage Request(HttpMethod method, string uri, TestIds ids)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", ids.TenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", ids.ActorUserId.ToString());
        request.Headers.Add("X-Gccs-Dev-Email", "po@example.com");
        request.Headers.Add("X-Gccs-Dev-Permissions", Permission.ManageTenant.ToString());
        return request;
    }

    private static SspNarrativeSourceRecordDto Source(Guid tenantId) =>
        new("evidence", "evidence-1", tenantId, "MFA is enforced for administrative users.", "https://internal.example.com/evidence/evidence-1", true, false);

    private static TestIds Ids() => new(Guid.NewGuid(), Guid.NewGuid());
    private sealed record TestIds(Guid TenantId, Guid ActorUserId);

    private sealed class CapturingAuditWriter : IAuditEventWriter
    {
        public List<CapturedAudit> Events { get; } = [];

        public Task WriteAsync(Guid tenantId, Guid actorUserId, AuditAction action, string entityType, string entityId, string summary, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        {
            Events.Add(new CapturedAudit(action, entityType, summary));
            return Task.CompletedTask;
        }
    }

    private sealed record CapturedAudit(AuditAction Action, string EntityType, string Summary);
}
