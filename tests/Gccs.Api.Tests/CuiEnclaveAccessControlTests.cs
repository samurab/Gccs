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

public sealed class CuiEnclaveAccessControlTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;
    public CuiEnclaveAccessControlTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task TC_38_3_1_Enclave_operation_permissions_are_enforced()
    {
        using var client = CreateClient();
        var ids = Ids();
        var enclaveId = Guid.NewGuid();

        await AssertOperationAllowedAsync(client, ids, "/api/enterprise/cui/access/view", new CuiEnclaveOperationRequest(enclaveId, CuiEnclaveOperation.View), Permission.ViewEnclave);
        await AssertOperationAllowedAsync(client, ids, "/api/enterprise/cui/access/upload", new CuiEnclaveOperationRequest(enclaveId, CuiEnclaveOperation.Upload), Permission.UploadEnclave);
        await AssertOperationAllowedAsync(client, ids, "/api/enterprise/cui/access/download", new CuiEnclaveOperationRequest(enclaveId, CuiEnclaveOperation.Download), Permission.DownloadEnclave);
        await AssertOperationAllowedAsync(client, ids, "/api/enterprise/cui/exports", ValidExport(enclaveId), Permission.ExportEnclave);
        await AssertOperationAllowedAsync(client, ids, "/api/enterprise/cui/access/approve", new CuiEnclaveOperationRequest(enclaveId, CuiEnclaveOperation.Approve), Permission.ApproveEnclave);
        await AssertOperationAllowedAsync(client, ids, "/api/enterprise/cui/support-access", ValidSupport(enclaveId), Permission.SupportEnclave);
        await AssertOperationAllowedAsync(client, ids, "/api/enterprise/cui/emergency-access", ValidEmergency(enclaveId), Permission.EmergencyEnclave);

        var disallowed = await client.SendAsync(Request(HttpMethod.Post, "/api/enterprise/cui/access/view", new CuiEnclaveOperationRequest(enclaveId, CuiEnclaveOperation.View), ids, Permission.ViewEvidence));
        Assert.Equal(HttpStatusCode.Forbidden, disallowed.StatusCode);
    }

    [Fact]
    public async Task TC_38_3_2_Just_in_time_support_access_expires_and_keeps_session_log()
    {
        using var client = CreateClient();
        var ids = Ids();
        var support = await SupportAsync(client, ids, Guid.NewGuid());

        Assert.Equal("containment support", support.Reason);
        Assert.Equal("single evidence folder", support.Scope);
        Assert.Equal("customer-admin", support.Approver);
        Assert.Contains("session started", support.SessionLog);
        Assert.True(support.ExpiresAt > support.GrantedAt);
        Assert.False(support.Expired);

        var expiredResponse = await client.SendAsync(Request(HttpMethod.Post, $"/api/enterprise/cui/support-access/{support.Id}/expire", ids, Permission.SupportEnclave));
        Assert.Equal(HttpStatusCode.OK, expiredResponse.StatusCode);
        var expired = Assert.IsType<CuiEnclaveSupportAccessDto>(await expiredResponse.Content.ReadFromJsonAsync<CuiEnclaveSupportAccessDto>(JsonOptions));
        Assert.True(expired.Expired);
    }

    [Fact]
    public async Task TC_38_3_3_Enclave_export_policy_is_enforced()
    {
        using var client = CreateClient();
        var ids = Ids();
        var enclaveId = Guid.NewGuid();

        var created = await client.SendAsync(Request(HttpMethod.Post, "/api/enterprise/cui/exports", ValidExport(enclaveId), ids, Permission.ExportEnclave));
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var export = Assert.IsType<CuiEnclaveExportDto>(await created.Content.ReadFromJsonAsync<CuiEnclaveExportDto>(JsonOptions));
        Assert.Equal("EvidencePackage", export.PackageType);
        Assert.True(export.Watermarked);
        Assert.True(export.Encrypted);

        foreach (var invalid in new[]
        {
            ValidExport(enclaveId) with { PackageType = "RawDump" },
            ValidExport(enclaveId) with { RecipientAllowed = false },
            ValidExport(enclaveId) with { Watermarked = false },
            ValidExport(enclaveId) with { Encrypted = false },
            ValidExport(enclaveId) with { ApprovalGranted = false }
        })
        {
            var blocked = await client.SendAsync(Request(HttpMethod.Post, "/api/enterprise/cui/exports", invalid, ids, Permission.ExportEnclave));
            Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
        }
    }

    [Fact]
    public async Task TC_38_3_4_Emergency_access_requires_incident_time_limit_and_post_review()
    {
        using var client = CreateClient();
        var ids = Ids();
        var enclaveId = Guid.NewGuid();

        foreach (var invalid in new[]
        {
            ValidEmergency(enclaveId) with { ElevatedApproval = false },
            ValidEmergency(enclaveId) with { IncidentId = string.Empty },
            ValidEmergency(enclaveId) with { DurationMinutes = 0 },
            ValidEmergency(enclaveId) with { DurationMinutes = 241 },
            ValidEmergency(enclaveId) with { PostAccessReviewRequired = false }
        })
        {
            var blocked = await client.SendAsync(Request(HttpMethod.Post, "/api/enterprise/cui/emergency-access", invalid, ids, Permission.EmergencyEnclave));
            Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
        }

        var approved = await EmergencyAsync(client, ids, enclaveId);
        Assert.Equal("INC-38-3", approved.IncidentId);
        Assert.True(approved.ExpiresAt > approved.GrantedAt);
        Assert.True(approved.PostAccessReviewRequired);

        var reviewedResponse = await client.SendAsync(Request(HttpMethod.Post, $"/api/enterprise/cui/emergency-access/{approved.Id}/post-access-review", new CuiEnclavePostAccessReviewRequest("security-reviewer"), ids, Permission.EmergencyEnclave));
        Assert.Equal(HttpStatusCode.OK, reviewedResponse.StatusCode);
        var reviewed = Assert.IsType<CuiEnclaveEmergencyAccessDto>(await reviewedResponse.Content.ReadFromJsonAsync<CuiEnclaveEmergencyAccessDto>(JsonOptions));
        Assert.Equal("security-reviewer", reviewed.PostAccessReviewer);
    }

    [Fact]
    public async Task TC_38_3_5_Access_export_support_emergency_and_review_actions_are_audit_logged()
    {
        var audit = new CapturingAuditWriter();
        using var client = CreateClient(audit);
        var ids = Ids();
        var enclaveId = Guid.NewGuid();

        await client.SendAsync(Request(HttpMethod.Post, "/api/enterprise/cui/access/view", new CuiEnclaveOperationRequest(enclaveId, CuiEnclaveOperation.View), ids, Permission.ViewEnclave));
        await client.SendAsync(Request(HttpMethod.Post, "/api/enterprise/cui/exports", ValidExport(enclaveId), ids, Permission.ExportEnclave));
        var support = await SupportAsync(client, ids, enclaveId);
        await client.SendAsync(Request(HttpMethod.Post, $"/api/enterprise/cui/support-access/{support.Id}/expire", ids, Permission.SupportEnclave));
        var emergency = await EmergencyAsync(client, ids, enclaveId);
        await client.SendAsync(Request(HttpMethod.Post, $"/api/enterprise/cui/emergency-access/{emergency.Id}/post-access-review", new CuiEnclavePostAccessReviewRequest("security-reviewer"), ids, Permission.EmergencyEnclave));

        Assert.Contains(audit.Events, item => item.EntityType == "CuiEnclaveAccess" && item.Action == AuditAction.Approved);
        Assert.Contains(audit.Events, item => item.EntityType == "CuiEnclaveExport" && item.Action == AuditAction.Exported);
        Assert.Contains(audit.Events, item => item.EntityType == "CuiEnclaveSupportAccess" && item.Action == AuditAction.Approved);
        Assert.Contains(audit.Events, item => item.EntityType == "CuiEnclaveSupportAccess" && item.Action == AuditAction.Expired);
        Assert.Contains(audit.Events, item => item.EntityType == "CuiEnclaveEmergencyAccess" && item.Summary.Contains("approved", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(audit.Events, item => item.EntityType == "CuiEnclaveEmergencyAccess" && item.Summary.Contains("post-access review", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task AssertOperationAllowedAsync<T>(HttpClient client, TestIds ids, string uri, T body, Permission permission)
    {
        var response = await client.SendAsync(Request(HttpMethod.Post, uri, body, ids, permission));
        Assert.True(response.IsSuccessStatusCode, $"{uri} returned {response.StatusCode}");
    }

    private static async Task<CuiEnclaveSupportAccessDto> SupportAsync(HttpClient client, TestIds ids, Guid enclaveId)
    {
        var response = await client.SendAsync(Request(HttpMethod.Post, "/api/enterprise/cui/support-access", ValidSupport(enclaveId), ids, Permission.SupportEnclave));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Assert.IsType<CuiEnclaveSupportAccessDto>(await response.Content.ReadFromJsonAsync<CuiEnclaveSupportAccessDto>(JsonOptions));
    }

    private static async Task<CuiEnclaveEmergencyAccessDto> EmergencyAsync(HttpClient client, TestIds ids, Guid enclaveId)
    {
        var response = await client.SendAsync(Request(HttpMethod.Post, "/api/enterprise/cui/emergency-access", ValidEmergency(enclaveId), ids, Permission.EmergencyEnclave));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Assert.IsType<CuiEnclaveEmergencyAccessDto>(await response.Content.ReadFromJsonAsync<CuiEnclaveEmergencyAccessDto>(JsonOptions));
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
                services.AddSingleton<ICuiEnclaveAccessControlRepository, InMemoryCuiEnclaveAccessControlRepository>();
                services.AddScoped<CuiEnclaveAccessControlService>();
                services.AddSingleton(auditWriter);
            });
        }).CreateClient();
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
        request.Headers.Add("X-Gccs-Dev-Email", "security@example.com");
        request.Headers.Add("X-Gccs-Dev-Permissions", permission.ToString());
        return request;
    }

    private static CuiEnclaveSupportAccessRequest ValidSupport(Guid enclaveId) =>
        new(enclaveId, "containment support", "single evidence folder", "customer-admin", 60, "session started; command log enabled");

    private static CuiEnclaveExportRequest ValidExport(Guid enclaveId) =>
        new(enclaveId, "EvidencePackage", "advisor@example.com", true, true, true, true);

    private static CuiEnclaveEmergencyAccessRequest ValidEmergency(Guid enclaveId) =>
        new(enclaveId, true, "INC-38-3", 120, true, "security-director");

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
