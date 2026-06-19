using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Compliance;
using Gccs.Domain.Audit;
using Gccs.Domain.Identity;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Compliance;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class FedRampControlMappingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public FedRampControlMappingTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task TC_37_1_1_Create_control_mapping_persists_required_baseline_fields()
    {
        using var client = CreateClient();
        var ids = Ids();
        var created = await CreateAsync(client, ids);

        Assert.Equal("AC-2", created.ControlId);
        Assert.Equal("Access Control", created.Family);
        Assert.Equal("Moderate", created.Baseline);
        Assert.Equal("security-owner", created.Owner);
        Assert.Equal(FedRampImplementationStatus.Implemented, created.ImplementationStatus);
        Assert.NotEmpty(created.EvidenceLinks);
        Assert.Equal("NIST SP 800-53 Rev. 5", created.SourceReference);
    }

    [Fact]
    public async Task TC_37_1_2_Link_mappings_to_security_and_operations_evidence()
    {
        using var client = CreateClient();
        var ids = Ids();
        var created = await CreateAsync(client, ids);
        var security = await LinkEvidenceAsync(client, ids, created.Id, FedRampEvidenceType.Identity, "identity audit");
        var operations = await LinkEvidenceAsync(client, ids, created.Id, FedRampEvidenceType.OperationsEvidence, "release readiness");

        Assert.Contains(security.EvidenceLinks, link => link.EvidenceType == FedRampEvidenceType.Identity);
        Assert.Contains(operations.EvidenceLinks, link => link.EvidenceType == FedRampEvidenceType.OperationsEvidence);
    }

    [Fact]
    public async Task TC_37_1_3_Approval_requires_owner_reviewer_review_date_source_and_evidence_or_gap()
    {
        using var client = CreateClient();
        var ids = Ids();
        var invalidCreate = await client.SendAsync(Request(HttpMethod.Post, "/api/enterprise/fedramp/control-mappings", ValidCreate() with { Owner = "" }, ids));
        var created = await CreateAsync(client, ids);
        var invalidApproval = await client.SendAsync(Request(HttpMethod.Post, $"/api/enterprise/fedramp/control-mappings/{created.Id}/state", new FedRampControlReviewRequest(FedRampReviewState.Approved, "", DateOnly.FromDateTime(DateTime.UtcNow), "reviewed"), ids));
        var approved = await ChangeStateAsync(client, ids, created.Id, FedRampReviewState.Approved);

        Assert.Equal(HttpStatusCode.BadRequest, invalidCreate.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidApproval.StatusCode);
        Assert.Equal(FedRampReviewState.Approved, approved.ReviewState);
    }

    [Fact]
    public async Task TC_37_1_4_Open_gaps_are_reportable_by_family_severity_owner_and_target_date()
    {
        using var client = CreateClient();
        var ids = Ids();
        var created = await CreateAsync(client, ids);
        await client.SendAsync(Request(HttpMethod.Post, $"/api/enterprise/fedramp/control-mappings/{created.Id}/gaps", new FedRampGapRequest("MFA evidence missing.", FedRampGapSeverity.High, "security-owner", new DateOnly(2026, 9, 30)), ids));

        var response = await client.SendAsync(Request(HttpMethod.Get, "/api/enterprise/fedramp/control-mappings?family=Access%20Control&severity=High&owner=security-owner&targetDate=2026-09-30", ids));
        var report = await response.Content.ReadFromJsonAsync<FedRampControlMappingDto[]>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(report!);
        Assert.Contains(report![0].Gaps, gap => gap.Severity == FedRampGapSeverity.High && gap.Owner == "security-owner");
    }

    [Fact]
    public async Task TC_37_1_5_Lifecycle_changes_are_audit_logged()
    {
        var auditWriter = new CapturingAuditWriter();
        using var client = CreateClient(auditWriter);
        var ids = Ids();
        var created = await CreateAsync(client, ids);
        foreach (var state in Enum.GetValues<FedRampReviewState>().Where(state => state != FedRampReviewState.Draft))
        {
            await ChangeStateAsync(client, ids, created.Id, state);
        }

        Assert.Contains(auditWriter.Events, audit => audit.EntityType == "FedRampControlMapping" && audit.Action == AuditAction.Created);
        Assert.Contains(auditWriter.Events, audit => audit.Summary.Contains("Archived", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<FedRampControlMappingDto> CreateAsync(HttpClient client, TestIds ids)
    {
        var response = await client.SendAsync(Request(HttpMethod.Post, "/api/enterprise/fedramp/control-mappings", ValidCreate(), ids));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<FedRampControlMappingDto>(await response.Content.ReadFromJsonAsync<FedRampControlMappingDto>(JsonOptions));
    }

    private static async Task<FedRampControlMappingDto> LinkEvidenceAsync(HttpClient client, TestIds ids, Guid mappingId, FedRampEvidenceType type, string label)
    {
        var response = await client.SendAsync(Request(HttpMethod.Post, $"/api/enterprise/fedramp/control-mappings/{mappingId}/evidence", new FedRampEvidenceLinkRequest(label, $"https://evidence.example.com/{type}", type), ids));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Assert.IsType<FedRampControlMappingDto>(await response.Content.ReadFromJsonAsync<FedRampControlMappingDto>(JsonOptions));
    }

    private static async Task<FedRampControlMappingDto> ChangeStateAsync(HttpClient client, TestIds ids, Guid mappingId, FedRampReviewState state)
    {
        var response = await client.SendAsync(Request(HttpMethod.Post, $"/api/enterprise/fedramp/control-mappings/{mappingId}/state", new FedRampControlReviewRequest(state, "fedramp-reviewer", DateOnly.FromDateTime(DateTime.UtcNow), $"moved to {state}"), ids));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Assert.IsType<FedRampControlMappingDto>(await response.Content.ReadFromJsonAsync<FedRampControlMappingDto>(JsonOptions));
    }

    private static CreateFedRampControlMappingRequest ValidCreate() =>
        new("AC-2", "Access Control", "Moderate", "security-owner", FedRampImplementationStatus.Implemented, "Mapped to GCCS identity lifecycle and audit controls.", "Azure Government", [new FedRampEvidenceLinkDto("identity evidence", "https://evidence.example.com/identity", FedRampEvidenceType.Identity)], null, "NIST SP 800-53 Rev. 5");

    private HttpClient CreateClient(IAuditEventWriter? auditWriter = null)
    {
        auditWriter ??= new CapturingAuditWriter();
        return
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IFedRampControlMappingRepository, InMemoryFedRampControlMappingRepository>();
                services.AddScoped<FedRampControlMappingService>();
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
        request.Headers.Add("X-Gccs-Dev-Email", "security@example.com");
        request.Headers.Add("X-Gccs-Dev-Permissions", Permission.ManageTenant.ToString());
        return request;
    }

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
