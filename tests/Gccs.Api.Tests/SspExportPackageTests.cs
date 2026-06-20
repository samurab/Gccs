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

public sealed class SspExportPackageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;
    public SspExportPackageTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task TC_29_3_1_Authorized_user_exports_current_tenant_ssp_package()
    {
        using var client = CreateClient();
        var ids = Ids();
        await CreateApprovedSectionAndNarrativeAsync(client, ids);

        var package = await ExportAsync(client, ids);

        Assert.Equal(ids.TenantId, package.TenantId);
        Assert.Equal("ssp-1", package.PackageVersion);
        Assert.Equal("Boundary A", package.SystemBoundary);
        Assert.Equal("security reviewer", package.Reviewer);
        Assert.NotEmpty(package.Sections);
        Assert.NotEmpty(package.HumanReadableReport);
        Assert.Contains("Boundary A", package.HumanReadableReport);
    }

    [Fact]
    public async Task TC_29_3_2_Export_includes_version_review_metadata_section_statuses_sources_and_poam_references()
    {
        using var client = CreateClient();
        var ids = Ids();
        await CreateApprovedSectionAndNarrativeAsync(client, ids);

        var package = await ExportAsync(client, ids);

        Assert.NotEqual(default, package.GeneratedAt);
        Assert.Equal(SspExportFormat.Both, package.Format);
        Assert.Contains("poam-1", package.PoamReferences);
        Assert.All(package.Sections, section =>
        {
            Assert.False(string.IsNullOrWhiteSpace(section.Owner));
            Assert.NotEmpty(section.SourceReferences);
            Assert.NotEqual(default, section.Status);
        });
        Assert.Single(package.History);
        Assert.Equal("Generated", package.History.Single().Action);
    }

    [Fact]
    public async Task TC_29_3_3_Export_excludes_prohibited_unknown_unapproved_and_cross_tenant_evidence()
    {
        using var client = CreateClient();
        var ids = Ids();
        await CreateApprovedSectionAndNarrativeAsync(client, ids);

        var package = await ExportAsync(client, ids, includeBadRecords: true);

        Assert.Single(package.IncludedEvidence);
        Assert.DoesNotContain(package.IncludedEvidence, record =>
            record.TenantId != ids.TenantId ||
            record.Status != SspExportRecordStatus.Approved ||
            record.Classification is SspExportRecordClassification.Unknown or SspExportRecordClassification.Prohibited);
    }

    [Fact]
    public async Task TC_29_3_4_Export_language_contains_no_certification_or_assessor_determination()
    {
        using var client = CreateClient();
        var ids = Ids();
        await CreateApprovedSectionAndNarrativeAsync(client, ids);

        var package = await ExportAsync(client, ids);

        Assert.Contains("not a certification", package.AuthorizationLanguage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("assessment determination", package.AuthorizationLanguage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("certified compliant", package.AuthorizationLanguage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(package.AuthorizationLanguage, package.HumanReadableReport);
    }

    [Fact]
    public async Task TC_29_3_5_Export_is_audit_logged_and_history_is_listed()
    {
        var audit = new CapturingAuditWriter();
        using var client = CreateClient(audit);
        var ids = Ids();
        await CreateApprovedSectionAndNarrativeAsync(client, ids);

        var package = await ExportAsync(client, ids);
        var listResponse = await client.SendAsync(Request(HttpMethod.Get, "/api/compliance/ssp/export-packages", ids));
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var packages = Assert.IsType<SspExportPackageDto[]>(await listResponse.Content.ReadFromJsonAsync<SspExportPackageDto[]>(JsonOptions));

        Assert.Contains(packages, item => item.Id == package.Id);
        Assert.Contains(audit.Events, item => item.EntityType == "SspExportPackage" && item.Action == AuditAction.Exported);
    }

    [Fact]
    public async Task TC_29_3_6_External_share_requires_explicit_approval()
    {
        using var client = CreateClient();
        var ids = Ids();
        await CreateApprovedSectionAndNarrativeAsync(client, ids);

        var request = ExportRequest(ids, externalShareRequested: true, approvedForExternalSharing: false);
        var response = await client.SendAsync(Request(HttpMethod.Post, "/api/compliance/ssp/export-packages", request, ids));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task CreateApprovedSectionAndNarrativeAsync(HttpClient client, TestIds ids)
    {
        var sectionResponse = await client.SendAsync(Request(HttpMethod.Post, "/api/compliance/ssp/sections", new CreateSspSectionRequest(
            SspSectionType.AuthorizationBoundary,
            "Authorization boundary",
            "security owner",
            [new SspLinkedRecordDto("systemBoundary", "boundary-1", "Defines reviewed system boundary.")],
            [new SspSourceReferenceDto("NIST SP 800-171 Rev. 2", "https://csrc.nist.gov/publications/detail/sp/800-171/rev-2/final", new DateOnly(2026, 6, 19))]), ids));
        Assert.Equal(HttpStatusCode.Created, sectionResponse.StatusCode);
        var section = Assert.IsType<SspSectionDto>(await sectionResponse.Content.ReadFromJsonAsync<SspSectionDto>(JsonOptions));

        var approveSection = await client.SendAsync(Request(HttpMethod.Post, $"/api/compliance/ssp/sections/{section.Id}/status", new SspSectionStatusRequest(SspSectionStatus.Approved, "reviewer", new DateOnly(2026, 6, 19), "security reviewer"), ids));
        Assert.Equal(HttpStatusCode.OK, approveSection.StatusCode);

        var narrativeResponse = await client.SendAsync(Request(HttpMethod.Post, $"/api/compliance/ssp/sections/{section.Id}/narratives", new GenerateSspNarrativeDraftRequest([new SspNarrativeSourceRecordDto("evidence", "evidence-1", ids.TenantId, "Boundary evidence reviewed.", "https://internal.example.com/evidence/evidence-1", true, false)], false), ids));
        Assert.Equal(HttpStatusCode.Created, narrativeResponse.StatusCode);
        var narrative = Assert.IsType<SspNarrativeDto>(await narrativeResponse.Content.ReadFromJsonAsync<SspNarrativeDto>(JsonOptions));

        var editResponse = await client.SendAsync(Request(HttpMethod.Put, $"/api/compliance/ssp/sections/{section.Id}/narratives/{narrative.Id}", new EditSspNarrativeDraftRequest("Approved boundary narrative."), ids));
        Assert.Equal(HttpStatusCode.OK, editResponse.StatusCode);

        var approveNarrative = await client.SendAsync(Request(HttpMethod.Post, $"/api/compliance/ssp/sections/{section.Id}/narratives/{narrative.Id}/approve", new ApproveSspNarrativeRequest("security reviewer", new DateOnly(2026, 6, 19)), ids));
        Assert.Equal(HttpStatusCode.OK, approveNarrative.StatusCode);
    }

    private static async Task<SspExportPackageDto> ExportAsync(HttpClient client, TestIds ids, bool includeBadRecords = false)
    {
        var response = await client.SendAsync(Request(HttpMethod.Post, "/api/compliance/ssp/export-packages", ExportRequest(ids, includeBadRecords: includeBadRecords), ids));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<SspExportPackageDto>(await response.Content.ReadFromJsonAsync<SspExportPackageDto>(JsonOptions));
    }

    private static CreateSspExportPackageRequest ExportRequest(TestIds ids, bool includeBadRecords = false, bool externalShareRequested = false, bool approvedForExternalSharing = true)
    {
        var records = new List<SspExportRecordDto>
        {
            new("evidence", "evidence-1", ids.TenantId, "Boundary evidence", SspExportRecordStatus.Approved, SspExportRecordClassification.Fci)
        };
        if (includeBadRecords)
        {
            records.AddRange([
                new("evidence", "draft", ids.TenantId, "Draft evidence", SspExportRecordStatus.Draft, SspExportRecordClassification.Fci),
                new("evidence", "unknown", ids.TenantId, "Unknown evidence", SspExportRecordStatus.Approved, SspExportRecordClassification.Unknown),
                new("evidence", "prohibited", ids.TenantId, "Prohibited evidence", SspExportRecordStatus.Approved, SspExportRecordClassification.Prohibited),
                new("evidence", "cross", Guid.NewGuid(), "Cross tenant evidence", SspExportRecordStatus.Approved, SspExportRecordClassification.Fci)
            ]);
        }

        return new CreateSspExportPackageRequest("ssp-1", "Boundary A", "security reviewer", SspExportFormat.Both, externalShareRequested, approvedForExternalSharing, ["poam-1"], records.ToArray());
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
