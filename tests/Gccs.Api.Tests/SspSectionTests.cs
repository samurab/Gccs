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

public sealed class SspSectionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;
    public SspSectionTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task TC_29_1_1_Authorized_user_can_create_and_update_tenant_scoped_ssp_section()
    {
        using var client = CreateClient();
        var ids = Ids();
        var created = await CreateSectionAsync(client, ids);

        Assert.Equal(ids.TenantId, created.TenantId);
        Assert.Equal(SspSectionType.AuthorizationBoundary, created.SectionType);
        Assert.Equal(SspSectionStatus.Draft, created.Status);
        Assert.Contains(created.LinkedRecords, record => record.RecordType == "systemBoundary");
        Assert.Contains(created.SourceReferences, source => source.Source == "NIST SP 800-171 Rev. 2");

        var update = new UpdateSspSectionRequest(
            SspSectionType.Environment,
            "Cloud environment",
            "security lead",
            [new SspLinkedRecordDto("asset", "asset-1", "Hosts assessed workload.")],
            [Source()]);
        var response = await client.SendAsync(Request(HttpMethod.Put, $"/api/compliance/ssp/sections/{created.Id}", update, ids));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = Assert.IsType<SspSectionDto>(await response.Content.ReadFromJsonAsync<SspSectionDto>(JsonOptions));
        Assert.Equal(SspSectionType.Environment, updated.SectionType);
        Assert.Equal("security lead", updated.Owner);
        Assert.Equal(SspSectionStatus.Draft, updated.Status);
    }

    [Fact]
    public async Task TC_29_1_2_Approval_requires_reviewer_review_date_and_source_support()
    {
        using var client = CreateClient();
        var ids = Ids();
        var created = await CreateSectionAsync(client, ids);

        var missingMetadata = await client.SendAsync(Request(HttpMethod.Post, $"/api/compliance/ssp/sections/{created.Id}/status", new SspSectionStatusRequest(SspSectionStatus.Approved, "owner"), ids));
        Assert.Equal(HttpStatusCode.BadRequest, missingMetadata.StatusCode);

        var approved = await ChangeStatusAsync(client, ids, created.Id, new SspSectionStatusRequest(SspSectionStatus.Approved, "owner", new DateOnly(2026, 6, 19), "security reviewer"));
        Assert.Equal(SspSectionStatus.Approved, approved.Status);
        Assert.Equal("security reviewer", approved.Reviewer);
        Assert.Equal(new DateOnly(2026, 6, 19), approved.ReviewDate);
    }

    [Fact]
    public async Task TC_29_1_3_Status_changes_preserve_history()
    {
        using var client = CreateClient();
        var ids = Ids();
        var created = await CreateSectionAsync(client, ids);

        var inReview = await ChangeStatusAsync(client, ids, created.Id, new SspSectionStatusRequest(SspSectionStatus.InReview, "owner", null, null, "Ready for SME review."));
        var approved = await ChangeStatusAsync(client, ids, created.Id, new SspSectionStatusRequest(SspSectionStatus.Approved, "reviewer", new DateOnly(2026, 6, 19), "security reviewer"));

        Assert.Equal(SspSectionStatus.InReview, inReview.Status);
        Assert.Equal(SspSectionStatus.Approved, approved.Status);
        Assert.True(approved.History.Length >= 3);
        Assert.Contains(approved.History, item => item.Status == SspSectionStatus.Draft);
        Assert.Contains(approved.History, item => item.Status == SspSectionStatus.InReview);
        Assert.Contains(approved.History, item => item.Status == SspSectionStatus.Approved);
    }

    [Fact]
    public async Task TC_29_1_4_Cross_tenant_sections_are_not_visible()
    {
        using var client = CreateClient();
        var ids = Ids();
        var otherIds = Ids();
        var created = await CreateSectionAsync(client, ids);

        var otherTenantGet = await client.SendAsync(Request(HttpMethod.Get, $"/api/compliance/ssp/sections/{created.Id}", otherIds));
        Assert.Equal(HttpStatusCode.NotFound, otherTenantGet.StatusCode);

        var listResponse = await client.SendAsync(Request(HttpMethod.Get, "/api/compliance/ssp/sections", otherIds));
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var otherTenantSections = Assert.IsType<SspSectionDto[]>(await listResponse.Content.ReadFromJsonAsync<SspSectionDto[]>(JsonOptions));
        Assert.Empty(otherTenantSections);
    }

    [Fact]
    public async Task TC_29_1_5_Create_update_approval_and_archive_are_audit_logged()
    {
        var audit = new CapturingAuditWriter();
        using var client = CreateClient(audit);
        var ids = Ids();
        var created = await CreateSectionAsync(client, ids);

        await client.SendAsync(Request(HttpMethod.Put, $"/api/compliance/ssp/sections/{created.Id}", new UpdateSspSectionRequest(created.SectionType, "Updated boundary", created.Owner, created.LinkedRecords, created.SourceReferences), ids));
        await ChangeStatusAsync(client, ids, created.Id, new SspSectionStatusRequest(SspSectionStatus.Approved, "reviewer", new DateOnly(2026, 6, 19), "security reviewer"));
        await ChangeStatusAsync(client, ids, created.Id, new SspSectionStatusRequest(SspSectionStatus.Archived, "owner", null, null, "Replaced by a new section."));

        Assert.Contains(audit.Events, item => item.Action == AuditAction.Created && item.EntityType == "SspSection");
        Assert.Contains(audit.Events, item => item.Action == AuditAction.Updated && item.EntityType == "SspSection");
        Assert.Contains(audit.Events, item => item.Action == AuditAction.Approved && item.EntityType == "SspSection");
        Assert.Contains(audit.Events, item => item.Action == AuditAction.Archived && item.EntityType == "SspSection");
    }

    private static async Task<SspSectionDto> CreateSectionAsync(HttpClient client, TestIds ids)
    {
        var request = new CreateSspSectionRequest(
            SspSectionType.AuthorizationBoundary,
            "Authorization boundary",
            "security owner",
            [
                new SspLinkedRecordDto("companyProfile", "company-1", "Identifies legal entity."),
                new SspLinkedRecordDto("systemBoundary", "boundary-1", "Defines assessed environment."),
                new SspLinkedRecordDto("cmmcControl", "AC.L2-3.1.1", "Provides control context."),
                new SspLinkedRecordDto("responsibilityMatrix", "matrix-1", "Identifies inherited responsibility."),
                new SspLinkedRecordDto("poamItem", "poam-1", "Tracks open remediation."),
                new SspLinkedRecordDto("evidence", "evidence-1", "Supports boundary assertion.")
            ],
            [Source()]);

        var response = await client.SendAsync(Request(HttpMethod.Post, "/api/compliance/ssp/sections", request, ids));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<SspSectionDto>(await response.Content.ReadFromJsonAsync<SspSectionDto>(JsonOptions));
    }

    private static async Task<SspSectionDto> ChangeStatusAsync(HttpClient client, TestIds ids, Guid sectionId, SspSectionStatusRequest request)
    {
        var response = await client.SendAsync(Request(HttpMethod.Post, $"/api/compliance/ssp/sections/{sectionId}/status", request, ids));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Assert.IsType<SspSectionDto>(await response.Content.ReadFromJsonAsync<SspSectionDto>(JsonOptions));
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
                services.AddSingleton<ISspExportPackageRepository>(repository);
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

    private static SspSourceReferenceDto Source() =>
        new("NIST SP 800-171 Rev. 2", "https://csrc.nist.gov/publications/detail/sp/800-171/rev-2/final", new DateOnly(2026, 6, 19));

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
