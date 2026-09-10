using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Compliance;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Domain.Evidence;
using Gccs.Domain.Identity;
using Gccs.Infrastructure.Compliance;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class SspExportPackageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private static readonly Guid EligibleEvidenceId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid EligiblePoamId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private readonly WebApplicationFactory<Program> factory;
    public SspExportPackageTests(WebApplicationFactory<Program> factory) => this.factory = factory;

    [Fact]
    public async Task TC_29_3_1_Authorized_user_exports_current_tenant_package_with_human_and_machine_content()
    {
        using var client = CreateClient(); var ids = Ids(); await CreateApprovedSectionAndNarrativeAsync(client, ids);
        var package = await ExportAsync(client, ids);
        Assert.Equal(ids.TenantId, package.TenantId);
        Assert.Equal("Tenant Alpha", package.TenantName);
        Assert.Equal("ssp-1", package.PackageVersion);
        Assert.Equal("Boundary A", package.SystemBoundary);
        Assert.Equal("security reviewer", package.Reviewer);
        Assert.Equal(SspExportPackageStatus.InternalReview, package.Status);
        Assert.Contains("Tenant Alpha", package.HumanReadableReport);
        Assert.Contains("Approved boundary narrative.", package.HumanReadableReport);
        Assert.Equal(package.Id, package.MachineReadableMetadata.GetProperty("id").GetGuid());
        Assert.True(package.MachineReadableMetadata.GetProperty("draftOnly").GetBoolean());
    }

    [Fact]
    public async Task TC_29_3_2_Export_snapshots_review_sources_evidence_poam_and_history()
    {
        using var client = CreateClient(); var ids = Ids(); await CreateApprovedSectionAndNarrativeAsync(client, ids);
        var package = await ExportAsync(client, ids);
        Assert.NotEqual(default, package.GeneratedAt);
        Assert.Equal(SspExportFormat.Both, package.Format);
        Assert.Equal(EligibleEvidenceId, Assert.Single(package.IncludedEvidence).Id);
        Assert.Equal(EligiblePoamId, Assert.Single(package.PoamReferences).Id);
        var section = Assert.Single(package.Sections);
        Assert.Equal(SspSectionStatus.Approved, section.Status);
        Assert.Equal("security reviewer", section.Reviewer);
        Assert.Equal("po@example.com", section.NarrativeReviewer);
        Assert.NotEmpty(section.SourceReferences);
        Assert.NotEmpty(section.NarrativeSources);
        Assert.Equal("Generated", Assert.Single(package.History).Action);
        Assert.Contains("Package history", package.HumanReadableReport);
        Assert.Equal("Generated", package.MachineReadableMetadata.GetProperty("history")[0].GetProperty("action").GetString());
        var duplicateVersion = await client.SendAsync(Request(HttpMethod.Post, "/api/compliance/ssp/export-packages", ValidExportRequest(), ids));
        Assert.Equal(HttpStatusCode.BadRequest, duplicateVersion.StatusCode);
    }

    [Fact]
    public async Task TC_29_3_3_Ineligible_or_cross_tenant_source_selection_is_rejected_without_package_or_audit()
    {
        var audit = new CapturingAuditWriter(); var repository = new InMemorySspExportPackageRepository();
        using var client = CreateClient(audit, repository); var ids = Ids(); await CreateApprovedSectionAndNarrativeAsync(client, ids);
        var request = ValidExportRequest() with { EvidenceItemIds = [EligibleEvidenceId, Guid.Parse("10000000-0000-0000-0000-000000000099")] };
        var response = await client.SendAsync(Request(HttpMethod.Post, "/api/compliance/ssp/export-packages", request, ids));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await repository.ListAsync(ids.TenantId));
        Assert.DoesNotContain(audit.Events, item => item.EntityType == "SspExportPackage");
    }

    [Fact]
    public async Task TC_29_3_4_Language_is_draft_only_and_contains_no_positive_assurance_claim()
    {
        using var client = CreateClient(); var ids = Ids(); await CreateApprovedSectionAndNarrativeAsync(client, ids);
        var package = await ExportAsync(client, ids);
        Assert.Contains("human review only", package.Disclaimer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not provide certification", package.Disclaimer, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("certified compliant", package.HumanReadableReport, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("government approved", package.HumanReadableReport, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(package.Disclaimer, package.HumanReadableReport);
        var governmentClaim = await client.SendAsync(Request(HttpMethod.Post, "/api/compliance/ssp/export-packages", ValidExportRequest("ssp-unsafe") with { SystemBoundary = "Government approved system" }, ids));
        var certificationClaim = await client.SendAsync(Request(HttpMethod.Post, "/api/compliance/ssp/export-packages", ValidExportRequest("ssp-unsafe-2") with { SystemBoundary = "This system is certified." }, ids));
        Assert.Equal(HttpStatusCode.BadRequest, governmentClaim.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, certificationClaim.StatusCode);
    }

    [Fact]
    public async Task TC_29_3_5_Export_is_audit_logged_and_history_is_tenant_scoped()
    {
        var audit = new CapturingAuditWriter(); using var client = CreateClient(audit); var ids = Ids();
        await CreateApprovedSectionAndNarrativeAsync(client, ids); var package = await ExportAsync(client, ids);
        var list = await client.SendAsync(Request(HttpMethod.Get, "/api/compliance/ssp/export-packages", ids));
        var otherTenant = await client.SendAsync(Request(HttpMethod.Get, "/api/compliance/ssp/export-packages", Ids()));
        var crossTenantGet = await client.SendAsync(Request(HttpMethod.Get, $"/api/compliance/ssp/export-packages/{package.Id}", Ids()));
        Assert.Contains(Assert.IsType<SspExportPackageDto[]>(await list.Content.ReadFromJsonAsync<SspExportPackageDto[]>(JsonOptions)), item => item.Id == package.Id);
        Assert.Empty(Assert.IsType<SspExportPackageDto[]>(await otherTenant.Content.ReadFromJsonAsync<SspExportPackageDto[]>(JsonOptions)));
        Assert.Equal(HttpStatusCode.NotFound, crossTenantGet.StatusCode);
        Assert.Contains(audit.Events, item => item.EntityType == "SspExportPackage" && item.Action == AuditAction.Exported && item.Metadata?["draftOnly"] == "true");
    }

    [Fact]
    public async Task TC_29_3_6_External_share_is_blocked_until_separately_approved()
    {
        using var client = CreateClient(); var ids = Ids(); await CreateApprovedSectionAndNarrativeAsync(client, ids);
        var selfApproved = await client.SendAsync(Request(HttpMethod.Post, "/api/compliance/ssp/export-packages", ValidExportRequest() with { ExternalShareRequested = true }, ids));
        Assert.Equal(HttpStatusCode.BadRequest, selfApproved.StatusCode);
        var package = await ExportAsync(client, ids, "ssp-2");
        var blocked = await client.SendAsync(Request(HttpMethod.Post, $"/api/compliance/ssp/export-packages/{package.Id}/share", new SspExternalShareRequest("advisor@example.invalid", "Independent review"), ids));
        Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
        var approval = await client.SendAsync(Request(HttpMethod.Post, $"/api/compliance/ssp/export-packages/{package.Id}/external-share-approval", new SspExternalShareApprovalRequest("Leadership approved advisor review."), ids));
        Assert.Equal(HttpStatusCode.OK, approval.StatusCode);
        var repeatedApproval = await client.SendAsync(Request(HttpMethod.Post, $"/api/compliance/ssp/export-packages/{package.Id}/external-share-approval", new SspExternalShareApprovalRequest("Duplicate approval"), ids));
        Assert.Equal(HttpStatusCode.BadRequest, repeatedApproval.StatusCode);
        var shared = await client.SendAsync(Request(HttpMethod.Post, $"/api/compliance/ssp/export-packages/{package.Id}/share", new SspExternalShareRequest("advisor@example.invalid", "Independent review"), ids));
        var sharedPackage = Assert.IsType<SspExportPackageDto>(await shared.Content.ReadFromJsonAsync<SspExportPackageDto>(JsonOptions));
        Assert.Equal(SspExportPackageStatus.Shared, sharedPackage.Status);
        Assert.Equal(new[] { "Generated", "ExternalShareApproved", "Shared" }, sharedPackage.History.Select(item => item.Action));
        var repeatedShare = await client.SendAsync(Request(HttpMethod.Post, $"/api/compliance/ssp/export-packages/{package.Id}/share", new SspExternalShareRequest("other@example.invalid", "Second review"), ids));
        Assert.Equal(HttpStatusCode.BadRequest, repeatedShare.StatusCode);
    }

    [Fact]
    public async Task Export_generation_requires_ExportReports_and_approval_requires_ManageTenant()
    {
        using var client = CreateClient(); var ids = Ids(); await CreateApprovedSectionAndNarrativeAsync(client, ids);
        var deniedExport = await client.SendAsync(Request(HttpMethod.Post, "/api/compliance/ssp/export-packages", ValidExportRequest(), ids, Permission.ManageTenant, Permission.ManageCmmc));
        Assert.Equal(HttpStatusCode.Forbidden, deniedExport.StatusCode);
        var package = await ExportAsync(client, ids);
        var deniedApproval = await client.SendAsync(Request(HttpMethod.Post, $"/api/compliance/ssp/export-packages/{package.Id}/external-share-approval", new SspExternalShareApprovalRequest("Approval"), ids, Permission.ExportReports));
        Assert.Equal(HttpStatusCode.Forbidden, deniedApproval.StatusCode);
    }

    private static async Task CreateApprovedSectionAndNarrativeAsync(HttpClient client, TestIds ids)
    {
        var sectionResponse = await client.SendAsync(Request(HttpMethod.Post, "/api/compliance/ssp/sections", new CreateSspSectionRequest(SspSectionType.AuthorizationBoundary, "Authorization boundary", "security owner", [new SspLinkedRecordDto(SspLinkedRecordType.SystemBoundary, "boundary-1", "Defines reviewed system boundary.")], [new SspSourceReferenceDto("NIST SP 800-171 Rev. 2", "https://csrc.nist.gov/publications/detail/sp/800-171/rev-2/final", new DateOnly(2026, 6, 19))]), ids));
        var section = Assert.IsType<SspSectionDto>(await sectionResponse.Content.ReadFromJsonAsync<SspSectionDto>(JsonOptions));
        var review = await client.SendAsync(Request(HttpMethod.Post, $"/api/compliance/ssp/sections/{section.Id}/status", new SspSectionStatusRequest(SspSectionStatus.InReview, "ignored", section.Version), ids));
        var reviewed = Assert.IsType<SspSectionDto>(await review.Content.ReadFromJsonAsync<SspSectionDto>(JsonOptions));
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(Request(HttpMethod.Post, $"/api/compliance/ssp/sections/{section.Id}/status", new SspSectionStatusRequest(SspSectionStatus.Approved, "ignored", reviewed.Version, new DateOnly(2026, 6, 19), "security reviewer"), ids))).StatusCode);
        var generated = await client.SendAsync(Request(HttpMethod.Post, $"/api/compliance/ssp/sections/{section.Id}/narratives", new GenerateSspNarrativeDraftRequest([new SspNarrativeSourceLinkRequest(SspNarrativeSourceType.Evidence, EligibleEvidenceId.ToString())]), ids));
        var narrative = Assert.IsType<SspNarrativeDto>(await generated.Content.ReadFromJsonAsync<SspNarrativeDto>(JsonOptions));
        var edited = await client.SendAsync(Request(HttpMethod.Put, $"/api/compliance/ssp/sections/{section.Id}/narratives/{narrative.Id}", new EditSspNarrativeDraftRequest("Approved boundary narrative.", null, new ContentClassificationRequest(ContentClassification.Unclassified), narrative.Version), ids));
        narrative = Assert.IsType<SspNarrativeDto>(await edited.Content.ReadFromJsonAsync<SspNarrativeDto>(JsonOptions));
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(Request(HttpMethod.Post, $"/api/compliance/ssp/sections/{section.Id}/narratives/{narrative.Id}/approve", new ApproveSspNarrativeRequest(new DateOnly(2026, 6, 19), narrative.Version), ids))).StatusCode);
    }

    private static async Task<SspExportPackageDto> ExportAsync(HttpClient client, TestIds ids, string version = "ssp-1")
    {
        var response = await client.SendAsync(Request(HttpMethod.Post, "/api/compliance/ssp/export-packages", ValidExportRequest(version), ids));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<SspExportPackageDto>(await response.Content.ReadFromJsonAsync<SspExportPackageDto>(JsonOptions));
    }

    private static CreateSspExportPackageRequest ValidExportRequest(string version = "ssp-1") => new(version, "Boundary A", "security reviewer", SspExportFormat.Both, false, [EligibleEvidenceId], [EligiblePoamId]);

    private HttpClient CreateClient(IAuditEventWriter? audit = null, InMemorySspExportPackageRepository? packageRepository = null)
    {
        audit ??= new CapturingAuditWriter(); packageRepository ??= new InMemorySspExportPackageRepository();
        return factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false"); builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                var ssp = new InMemorySspSectionRepository();
                services.RemoveAll<ISspSectionRepository>(); services.RemoveAll<ISspNarrativeRepository>(); services.RemoveAll<ISspNarrativeSourceResolver>();
                services.RemoveAll<ISspExportSourceRepository>(); services.RemoveAll<ISspExportPackageRepository>(); services.RemoveAll<IAuditEventWriter>();
                services.RemoveAll<ICurrentDataHandlingNoticeGuard>(); services.RemoveAll<IContentContainmentRepository>();
                services.AddSingleton<ISspSectionRepository>(ssp); services.AddSingleton<ISspNarrativeRepository>(ssp);
                services.AddSingleton<ISspNarrativeSourceResolver, ExportNarrativeSourceResolver>(); services.AddSingleton<ISspExportSourceRepository, ExportSourceRepository>();
                services.AddSingleton<ISspExportPackageRepository>(packageRepository); services.AddSingleton(audit);
                services.AddSingleton<ICurrentDataHandlingNoticeGuard, AcknowledgedNoticeGuard>(); services.AddSingleton<IContentContainmentRepository, AllowContentContainment>();
            });
        }).CreateClient();
    }

    private static HttpRequestMessage Request<T>(HttpMethod method, string uri, T body, TestIds ids, params Permission[] permissions)
    { var request = Request(method, uri, ids, permissions); request.Content = JsonContent.Create(body, options: JsonOptions); return request; }
    private static HttpRequestMessage Request(HttpMethod method, string uri, TestIds ids, params Permission[] permissions)
    {
        if (permissions.Length == 0) permissions = [Permission.ManageTenant, Permission.ManageCmmc, Permission.ExportReports];
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true"); request.Headers.Add("X-Gccs-Dev-Tenant", ids.TenantId.ToString()); request.Headers.Add("X-Gccs-Dev-User", ids.ActorUserId.ToString());
        request.Headers.Add("X-Gccs-Dev-Email", "po@example.com"); request.Headers.Add("X-Gccs-Dev-Permissions", string.Join(',', permissions)); return request;
    }
    private static TestIds Ids() => new(Guid.NewGuid(), Guid.NewGuid());
    private sealed record TestIds(Guid TenantId, Guid ActorUserId);

    private sealed class ExportNarrativeSourceResolver : ISspNarrativeSourceResolver
    {
        public Task<IReadOnlyList<ResolvedSspNarrativeSource>> ResolveAsync(Guid tenantId, IReadOnlyList<SspNarrativeSourceLinkRequest> sources, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ResolvedSspNarrativeSource>>(sources.Select(source => new ResolvedSspNarrativeSource(source.SourceType, source.RecordId, "Boundary evidence", "Boundary evidence reviewed.", $"/evidence?item={EligibleEvidenceId}", "export-source-v1", ContentClassification.Unclassified)).ToArray());
    }
    private sealed class ExportSourceRepository : ISspExportSourceRepository
    {
        public Task<SspExportSourceSnapshot> ResolveAsync(Guid tenantId, Guid[] evidenceItemIds, Guid[] poamItemIds, CancellationToken cancellationToken = default)
        {
            SspExportEvidenceReferenceDto[] evidence = evidenceItemIds.Contains(EligibleEvidenceId) ? [new(EligibleEvidenceId, "Boundary evidence", EvidenceStatus.Approved, ContentClassification.Fci, "Security", DateTimeOffset.Parse("2026-06-19T12:00:00Z"), Guid.Parse("30000000-0000-0000-0000-000000000001"), null, null)] : [];
            SspExportPoamReferenceDto[] poam = poamItemIds.Contains(EligiblePoamId) ? [new(EligiblePoamId, Guid.Parse("40000000-0000-0000-0000-000000000001"), "AC.L2-3.1.1", "Access gap", "Complete remediation.", PoamStatus.InProgress, "Security", new DateOnly(2026, 12, 1))] : [];
            return Task.FromResult(new SspExportSourceSnapshot("Tenant Alpha", evidence, poam));
        }
    }
    private sealed class AcknowledgedNoticeGuard : ICurrentDataHandlingNoticeGuard { public Task EnsureAsync(string workflow, Guid actorUserId, CancellationToken cancellationToken = default) => Task.CompletedTask; }
    private sealed class AllowContentContainment : IContentContainmentRepository { public Task<bool> IsBlockedAsync(Guid tenantId, string entityType, string entityId, CancellationToken cancellationToken = default) => Task.FromResult(false); }
    private sealed class CapturingAuditWriter : IAuditEventWriter
    {
        public List<CapturedAudit> Events { get; } = [];
        public Task WriteAsync(Guid tenantId, Guid actorUserId, AuditAction action, string entityType, string entityId, string summary, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        { Events.Add(new CapturedAudit(tenantId, action, entityType, summary, metadata)); return Task.CompletedTask; }
    }
    private sealed record CapturedAudit(Guid TenantId, AuditAction Action, string EntityType, string Summary, IReadOnlyDictionary<string, string>? Metadata);
}
