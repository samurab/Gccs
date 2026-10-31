using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Compliance;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Domain.Identity;
using Gccs.Infrastructure.Compliance;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
    public async Task TC_29_2_1_Generates_draft_from_server_resolved_source()
    {
        var fixture = CreateFixture();
        using var client = fixture.Client;
        var section = await CreateSectionAsync(client, fixture.Ids);
        var narrative = await GenerateNarrativeAsync(client, fixture.Ids, section.Id, "approved-evidence");

        Assert.Equal(SspNarrativeStatus.Draft, narrative.Status);
        Assert.True(narrative.DraftOnly);
        Assert.False(narrative.AiAssisted);
        Assert.Equal(ContentClassification.Unclassified, narrative.Classification.Classification);
        var source = Assert.Single(narrative.SourceRecords);
        Assert.Equal("Authoritative evidence", source.Label);
        Assert.Equal("fingerprint-1", source.Fingerprint);
        Assert.DoesNotContain(fixture.Ids.TenantId.ToString(), narrative.GeneratedText);

        var listed = await SendAsync<SspNarrativeDto[]>(client, Request(HttpMethod.Get,
            $"/api/compliance/ssp/sections/{section.Id}/narratives", fixture.Ids, Permission.ViewCmmc));
        Assert.Equal(narrative.Id, Assert.Single(listed).Id);
    }

    [Fact]
    public async Task TC_29_2_2_Unapproved_missing_and_cross_tenant_sources_are_rejected_without_audit()
    {
        var fixture = CreateFixture();
        using var client = fixture.Client;
        var section = await CreateSectionAsync(client, fixture.Ids);

        foreach (var sourceId in new[] { "unapproved", "missing", "other-tenant" })
        {
            using var response = await client.SendAsync(Request(HttpMethod.Post,
                $"/api/compliance/ssp/sections/{section.Id}/narratives",
                new GenerateSspNarrativeDraftRequest([Link(sourceId)]), fixture.Ids, Permission.ManageCmmc));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        Assert.DoesNotContain(fixture.Audit.Events, item => item.EntityType == "SspNarrative");
    }

    [Fact]
    public async Task TC_29_2_3_Approval_revalidates_sources_and_blocks_placeholders_and_stale_versions()
    {
        var fixture = CreateFixture();
        using var client = fixture.Client;
        var section = await CreateSectionAsync(client, fixture.Ids);
        var placeholder = await GenerateNarrativeAsync(client, fixture.Ids, section.Id, "approved-evidence");
        placeholder = await EditNarrativeAsync(client, fixture.Ids, section.Id, placeholder, "Implemented boundary uses {{missing asset}}.");
        using var placeholderApproval = await client.SendAsync(ApproveRequest(section.Id, placeholder, fixture.Ids));
        Assert.Equal(HttpStatusCode.BadRequest, placeholderApproval.StatusCode);

        var changed = await GenerateNarrativeAsync(client, fixture.Ids, section.Id, "approved-evidence");
        fixture.Sources.SetFingerprint("approved-evidence", "fingerprint-2");
        using var changedApproval = await client.SendAsync(ApproveRequest(section.Id, changed, fixture.Ids));
        Assert.Equal(HttpStatusCode.BadRequest, changedApproval.StatusCode);

        fixture.Sources.SetFingerprint("approved-evidence", "fingerprint-1");
        var revoked = await GenerateNarrativeAsync(client, fixture.Ids, section.Id, "approved-evidence");
        fixture.Sources.SetApproved("approved-evidence", false);
        using var revokedApproval = await client.SendAsync(ApproveRequest(section.Id, revoked, fixture.Ids));
        Assert.Equal(HttpStatusCode.BadRequest, revokedApproval.StatusCode);

        fixture.Sources.SetApproved("approved-evidence", true);
        var stale = await GenerateNarrativeAsync(client, fixture.Ids, section.Id, "approved-evidence");
        stale = await EditNarrativeAsync(client, fixture.Ids, section.Id, stale, "Current implementation narrative.");
        using var winningEdit = await client.SendAsync(Request(HttpMethod.Put,
            $"/api/compliance/ssp/sections/{section.Id}/narratives/{stale.Id}",
            Edit("Winning edit.", stale.Version), fixture.Ids, Permission.ManageCmmc));
        Assert.Equal(HttpStatusCode.OK, winningEdit.StatusCode);
        using var staleEdit = await client.SendAsync(Request(HttpMethod.Put,
            $"/api/compliance/ssp/sections/{section.Id}/narratives/{stale.Id}",
            Edit("Stale edit.", stale.Version), fixture.Ids, Permission.ManageCmmc));
        Assert.Equal(HttpStatusCode.Conflict, staleEdit.StatusCode);
    }

    [Fact]
    public async Task Approval_revalidates_sources_inside_the_approval_transaction()
    {
        var fixture = CreateFixture();
        using var client = fixture.Client;
        var section = await CreateSectionAsync(client, fixture.Ids);
        var narrative = await GenerateNarrativeAsync(client, fixture.Ids, section.Id, "approved-evidence");
        fixture.Transaction.BeforeExecute = () => fixture.Sources.SetFingerprint("approved-evidence", "changed-at-transaction-boundary");

        using var response = await client.SendAsync(ApproveRequest(section.Id, narrative, fixture.Ids));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain(fixture.Audit.Events, item =>
            item.EntityType == "SspNarrative" && item.Action == AuditAction.Approved);
    }

    [Fact]
    public async Task TC_29_2_4_Comparison_includes_approved_proposed_reviewer_and_sources()
    {
        var fixture = CreateFixture();
        using var client = fixture.Client;
        var section = await CreateSectionAsync(client, fixture.Ids);
        var first = await GenerateNarrativeAsync(client, fixture.Ids, section.Id, "approved-evidence");
        first = await EditNarrativeAsync(client, fixture.Ids, section.Id, first, "Approved boundary narrative.", "First review note.");
        first = await ApproveNarrativeAsync(client, fixture.Ids, section.Id, first);

        var proposed = await GenerateNarrativeAsync(client, fixture.Ids, section.Id, "approved-evidence");
        proposed = await EditNarrativeAsync(client, fixture.Ids, section.Id, proposed, "Proposed boundary narrative.", "Compare this change.");
        var comparison = await SendAsync<SspNarrativeComparisonDto>(client, Request(HttpMethod.Get,
            $"/api/compliance/ssp/sections/{section.Id}/narratives/{proposed.Id}/comparison", fixture.Ids, Permission.ViewCmmc));

        Assert.Equal(first.Id, comparison.ApprovedNarrativeId);
        Assert.Equal("Approved boundary narrative.", comparison.CurrentApprovedText);
        Assert.Equal("Proposed boundary narrative.", comparison.ProposedText);
        Assert.Equal(fixture.Ids.ActorUserId, comparison.CurrentApprovedReviewerUserId);
        Assert.Equal("po@example.com", comparison.CurrentApprovedReviewer);
        Assert.Equal("Compare this change.", comparison.ProposedReviewerNotes);
        Assert.Single(comparison.CurrentApprovedSources);
        Assert.Single(comparison.ProposedSources);
    }

    [Fact]
    public async Task TC_29_2_5_Lifecycle_is_audited_without_narrative_text()
    {
        var fixture = CreateFixture();
        using var client = fixture.Client;
        var section = await CreateSectionAsync(client, fixture.Ids);
        var narrative = await GenerateNarrativeAsync(client, fixture.Ids, section.Id, "approved-evidence");
        narrative = await EditNarrativeAsync(client, fixture.Ids, section.Id, narrative, "Sensitive-looking test marker must not enter audit metadata.");
        await ApproveNarrativeAsync(client, fixture.Ids, section.Id, narrative);

        var events = fixture.Audit.Events.Where(item => item.EntityType == "SspNarrative").ToArray();
        Assert.Equal([AuditAction.Created, AuditAction.Updated, AuditAction.Approved], events.Select(item => item.Action));
        Assert.All(events, item => Assert.DoesNotContain("Sensitive-looking", JsonSerializer.Serialize(item.Metadata)));
        Assert.All(events, item => Assert.Equal(section.Id.ToString(), item.Metadata!["sectionId"]));
    }

    [Fact]
    public async Task Compliance_manager_can_mutate_view_only_cannot_and_cross_tenant_is_hidden()
    {
        Assert.Contains(Permission.ManageCmmc, RoleCatalog.GetPermissions(RoleCatalog.ComplianceManager));
        var fixture = CreateFixture();
        using var client = fixture.Client;
        var section = await CreateSectionAsync(client, fixture.Ids);
        var narrative = await GenerateNarrativeAsync(client, fixture.Ids, section.Id, "approved-evidence");

        using var denied = await client.SendAsync(Request(HttpMethod.Post,
            $"/api/compliance/ssp/sections/{section.Id}/narratives",
            new GenerateSspNarrativeDraftRequest([Link("approved-evidence")]), fixture.Ids, Permission.ViewCmmc));
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        var other = fixture.Ids with { TenantId = Guid.NewGuid() };
        using var hidden = await client.SendAsync(Request(HttpMethod.Get,
            $"/api/compliance/ssp/sections/{section.Id}/narratives/{narrative.Id}", other, Permission.ViewCmmc));
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
    }

    [Fact]
    public async Task Invalid_payload_returns_validation_problem_instead_of_500()
    {
        var fixture = CreateFixture();
        using var client = fixture.Client;
        var section = await CreateSectionAsync(client, fixture.Ids);
        using var missingSources = await client.SendAsync(Request(HttpMethod.Post,
            $"/api/compliance/ssp/sections/{section.Id}/narratives",
            new GenerateSspNarrativeDraftRequest([]), fixture.Ids, Permission.ManageCmmc));
        Assert.Equal(HttpStatusCode.BadRequest, missingSources.StatusCode);

        var narrative = await GenerateNarrativeAsync(client, fixture.Ids, section.Id, "approved-evidence");
        using var emptyEdit = await client.SendAsync(Request(HttpMethod.Put,
            $"/api/compliance/ssp/sections/{section.Id}/narratives/{narrative.Id}",
            Edit(" ", narrative.Version), fixture.Ids, Permission.ManageCmmc));
        Assert.Equal(HttpStatusCode.BadRequest, emptyEdit.StatusCode);
    }

    [Fact]
    public async Task NoCui_tenant_cannot_generate_or_edit_a_CUI_narrative()
    {
        var fixture = CreateFixture();
        using var client = fixture.Client;
        var section = await CreateSectionAsync(client, fixture.Ids);
        using var generate = await client.SendAsync(Request(HttpMethod.Post,
            $"/api/compliance/ssp/sections/{section.Id}/narratives",
            new GenerateSspNarrativeDraftRequest([Link("cui-evidence")]), fixture.Ids, Permission.ManageCmmc));
        Assert.Equal(HttpStatusCode.Forbidden, generate.StatusCode);

        var narrative = await GenerateNarrativeAsync(client, fixture.Ids, section.Id, "approved-evidence");
        using var edit = await client.SendAsync(Request(HttpMethod.Put,
            $"/api/compliance/ssp/sections/{section.Id}/narratives/{narrative.Id}",
            new EditSspNarrativeDraftRequest("Possible CUI text.", null, new ContentClassificationRequest(ContentClassification.Cui), narrative.Version),
            fixture.Ids, Permission.ManageCmmc));
        Assert.Equal(HttpStatusCode.Forbidden, edit.StatusCode);
        Assert.DoesNotContain(fixture.Audit.Events, item => item.EntityType == "SspNarrative" && item.Action == AuditAction.Updated);
    }

    [Fact]
    public async Task AI_assisted_generation_fails_closed_when_no_approved_provider_is_configured()
    {
        var fixture = CreateFixture();
        using var client = fixture.Client;
        var section = await CreateSectionAsync(client, fixture.Ids);
        using var response = await client.SendAsync(Request(HttpMethod.Post,
            $"/api/compliance/ssp/sections/{section.Id}/narratives",
            new GenerateSspNarrativeDraftRequest([Link("approved-evidence")], SspNarrativeGenerationMode.AiAssisted),
            fixture.Ids, Permission.ManageCmmc));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain(fixture.Audit.Events, item => item.EntityType == "SspNarrative");
    }

    [Fact]
    public async Task Configured_AI_adapter_output_is_always_persisted_as_draft_only()
    {
        var fixture = CreateFixture(enableAi: true);
        using var client = fixture.Client;
        var section = await CreateSectionAsync(client, fixture.Ids);
        using var response = await client.SendAsync(Request(HttpMethod.Post,
            $"/api/compliance/ssp/sections/{section.Id}/narratives",
            new GenerateSspNarrativeDraftRequest([Link("approved-evidence")], SspNarrativeGenerationMode.AiAssisted),
            fixture.Ids, Permission.ManageCmmc));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var narrative = Assert.IsType<SspNarrativeDto>(await response.Content.ReadFromJsonAsync<SspNarrativeDto>(JsonOptions));
        Assert.True(narrative.AiAssisted);
        Assert.True(narrative.DraftOnly);
        Assert.Equal(SspNarrativeStatus.Draft, narrative.Status);
    }

    private Fixture CreateFixture(bool enableAi = false)
    {
        var ids = new TestIds(Guid.NewGuid(), Guid.NewGuid());
        var audit = new CapturingAuditWriter();
        var sources = new ControlledSourceResolver(ids.TenantId);
        var transaction = new BoundaryMutationTransaction();
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                var repository = new InMemorySspSectionRepository();
                services.RemoveAll<ISspSectionRepository>();
                services.RemoveAll<ISspNarrativeRepository>();
                services.RemoveAll<ISspNarrativeSourceResolver>();
                services.RemoveAll<ISspNarrativeAiGenerator>();
                services.RemoveAll<IAuditEventWriter>();
                services.RemoveAll<ICurrentDataHandlingNoticeGuard>();
                services.RemoveAll<IContentContainmentRepository>();
                services.RemoveAll<IApplicationTransaction>();
                services.AddSingleton<ISspSectionRepository>(repository);
                services.AddSingleton<ISspNarrativeRepository>(repository);
                services.AddSingleton<ISspNarrativeSourceResolver>(sources);
                if (enableAi) services.AddSingleton<ISspNarrativeAiGenerator, StubAiGenerator>();
                else services.AddSingleton<ISspNarrativeAiGenerator, UnavailableSspNarrativeAiGenerator>();
                services.AddSingleton<IAuditEventWriter>(audit);
                services.AddSingleton<ICurrentDataHandlingNoticeGuard, AcknowledgedNoticeGuard>();
                services.AddSingleton<IContentContainmentRepository, AllowContentContainment>();
                services.AddSingleton<IApplicationTransaction>(transaction);
            });
        });
        return new Fixture(factory.CreateClient(), ids, audit, sources, transaction);
    }

    private static async Task<SspSectionDto> CreateSectionAsync(HttpClient client, TestIds ids)
    {
        using var response = await client.SendAsync(Request(HttpMethod.Post, "/api/compliance/ssp/sections", new CreateSspSectionRequest(
            SspSectionType.ControlImplementationNarratives,
            "Control implementation narratives",
            "security owner",
            [new SspLinkedRecordDto(SspLinkedRecordType.CmmcControl, "AC.L2-3.1.1", "Narrative source.")],
            [new SspSourceReferenceDto("NIST SP 800-171 Rev. 2", "https://csrc.nist.gov/publications/detail/sp/800-171/rev-2/final", new DateOnly(2026, 6, 19))]), ids, Permission.ManageCmmc));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<SspSectionDto>(await response.Content.ReadFromJsonAsync<SspSectionDto>(JsonOptions));
    }

    private static async Task<SspNarrativeDto> GenerateNarrativeAsync(HttpClient client, TestIds ids, Guid sectionId, string sourceId)
    {
        using var response = await client.SendAsync(Request(HttpMethod.Post, $"/api/compliance/ssp/sections/{sectionId}/narratives",
            new GenerateSspNarrativeDraftRequest([Link(sourceId)]), ids, Permission.ManageCmmc));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<SspNarrativeDto>(await response.Content.ReadFromJsonAsync<SspNarrativeDto>(JsonOptions));
    }

    private static async Task<SspNarrativeDto> EditNarrativeAsync(HttpClient client, TestIds ids, Guid sectionId, SspNarrativeDto narrative, string text, string? notes = "edited")
    {
        using var response = await client.SendAsync(Request(HttpMethod.Put, $"/api/compliance/ssp/sections/{sectionId}/narratives/{narrative.Id}",
            Edit(text, narrative.Version, notes), ids, Permission.ManageCmmc));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Assert.IsType<SspNarrativeDto>(await response.Content.ReadFromJsonAsync<SspNarrativeDto>(JsonOptions));
    }

    private static async Task<SspNarrativeDto> ApproveNarrativeAsync(HttpClient client, TestIds ids, Guid sectionId, SspNarrativeDto narrative)
    {
        using var response = await client.SendAsync(ApproveRequest(sectionId, narrative, ids));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Assert.IsType<SspNarrativeDto>(await response.Content.ReadFromJsonAsync<SspNarrativeDto>(JsonOptions));
    }

    private static HttpRequestMessage ApproveRequest(Guid sectionId, SspNarrativeDto narrative, TestIds ids) =>
        Request(HttpMethod.Post, $"/api/compliance/ssp/sections/{sectionId}/narratives/{narrative.Id}/approve",
            new ApproveSspNarrativeRequest(new DateOnly(2026, 9, 9), narrative.Version), ids, Permission.ManageCmmc);

    private static EditSspNarrativeDraftRequest Edit(string text, long version, string? notes = "edited") =>
        new(text, notes, new ContentClassificationRequest(ContentClassification.Unclassified), version);

    private static SspNarrativeSourceLinkRequest Link(string id) => new(SspNarrativeSourceType.Evidence, id);

    private static async Task<T> SendAsync<T>(HttpClient client, HttpRequestMessage request)
    {
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Assert.IsType<T>(await response.Content.ReadFromJsonAsync<T>(JsonOptions));
    }

    private static HttpRequestMessage Request<T>(HttpMethod method, string uri, T body, TestIds ids, Permission permission)
    {
        var request = Request(method, uri, ids, permission);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        return request;
    }

    private static HttpRequestMessage Request(HttpMethod method, string uri, TestIds ids, Permission permission)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", ids.TenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", ids.ActorUserId.ToString());
        request.Headers.Add("X-Gccs-Dev-Email", "po@example.com");
        request.Headers.Add("X-Gccs-Dev-Permissions", permission.ToString());
        return request;
    }

    private sealed record Fixture(
        HttpClient Client,
        TestIds Ids,
        CapturingAuditWriter Audit,
        ControlledSourceResolver Sources,
        BoundaryMutationTransaction Transaction);
    private sealed record TestIds(Guid TenantId, Guid ActorUserId);

    private sealed class AcknowledgedNoticeGuard : ICurrentDataHandlingNoticeGuard
    {
        public Task EnsureAsync(string workflow, Guid actorUserId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class AllowContentContainment : IContentContainmentRepository
    {
        public Task<bool> IsBlockedAsync(Guid tenantId, string entityType, string entityId, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }

    private sealed class StubAiGenerator : ISspNarrativeAiGenerator
    {
        public Task<string> GenerateAsync(IReadOnlyList<ResolvedSspNarrativeSource> sources, CancellationToken cancellationToken = default) =>
            Task.FromResult("AI-assisted draft synthesized from the approved source summary.");
    }

    private sealed class BoundaryMutationTransaction : IApplicationTransaction
    {
        public Action? BeforeExecute { get; set; }

        public Task<T> ExecuteAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            var beforeExecute = BeforeExecute;
            BeforeExecute = null;
            beforeExecute?.Invoke();
            return operation(cancellationToken);
        }
    }

    private sealed class ControlledSourceResolver(Guid tenantId) : ISspNarrativeSourceResolver
    {
        private readonly Dictionary<string, SourceState> _states = new(StringComparer.Ordinal)
        {
            ["approved-evidence"] = new(tenantId, true, "fingerprint-1", ContentClassification.Unclassified),
            ["cui-evidence"] = new(tenantId, true, "cui-v1", ContentClassification.Cui),
            ["unapproved"] = new(tenantId, false, "unapproved", ContentClassification.Unclassified),
            ["other-tenant"] = new(Guid.NewGuid(), true, "other", ContentClassification.Unclassified)
        };

        public void SetFingerprint(string id, string fingerprint) => _states[id] = _states[id] with { Fingerprint = fingerprint };
        public void SetApproved(string id, bool approved) => _states[id] = _states[id] with { Approved = approved };

        public Task<IReadOnlyList<ResolvedSspNarrativeSource>> ResolveAsync(Guid currentTenantId, IReadOnlyList<SspNarrativeSourceLinkRequest> sources, CancellationToken cancellationToken = default)
        {
            var result = new List<ResolvedSspNarrativeSource>();
            foreach (var source in sources)
            {
                if (!_states.TryGetValue(source.RecordId, out var state) || state.TenantId != currentTenantId || !state.Approved)
                    throw new SspNarrativeValidationException($"The requested {source.SourceType} source was not found in the current tenant scope or is not approved and current.");
                result.Add(new ResolvedSspNarrativeSource(source.SourceType, source.RecordId, "Authoritative evidence",
                    "MFA is enforced for administrative users.", "/evidence?item=approved-evidence", state.Fingerprint, state.Classification));
            }
            return Task.FromResult<IReadOnlyList<ResolvedSspNarrativeSource>>(result);
        }

        private sealed record SourceState(Guid TenantId, bool Approved, string Fingerprint, ContentClassification Classification);
    }

    private sealed class CapturingAuditWriter : IAuditEventWriter
    {
        public List<CapturedAudit> Events { get; } = [];
        public Task WriteAsync(Guid tenantId, Guid actorUserId, AuditAction action, string entityType, string entityId, string summary, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        {
            Events.Add(new CapturedAudit(action, entityType, summary, metadata));
            return Task.CompletedTask;
        }
    }

    private sealed record CapturedAudit(AuditAction Action, string EntityType, string Summary, IReadOnlyDictionary<string, string>? Metadata);
}
