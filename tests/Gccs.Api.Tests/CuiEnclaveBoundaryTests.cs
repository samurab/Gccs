using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Compliance;
using Gccs.Domain.Audit;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Compliance;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class CuiEnclaveBoundaryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;
    public CuiEnclaveBoundaryTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task TC_38_1_1_Create_enclave_boundary_metadata_complete()
    {
        using var client = CreateClient();
        var ids = Ids();

        var enclave = await CreateEnclaveAsync(client, ids);

        Assert.Equal(ids.TenantId, enclave.TenantId);
        Assert.Equal("GovCloud", enclave.Environment);
        Assert.Contains("CUI enclave", enclave.BoundaryDescription);
        Assert.Equal(TenantDataPosture.CuiReady, enclave.DataHandlingMode);
        Assert.Contains("evidence-upload", enclave.ApprovedWorkflows);
        Assert.Equal("s3://gccs-cui/enclave-a", enclave.StorageLocation);
        Assert.Equal("aks-gov-cui-pool", enclave.ComputeBoundary);
        Assert.Contains("private endpoint", enclave.NetworkRestrictions);
        Assert.Equal("sentinel-cui-workspace", enclave.LoggingDestination);
        Assert.Contains("immutable", enclave.BackupPolicy);
        Assert.Contains("just-in-time", enclave.SupportAccessModel);
    }

    [Fact]
    public async Task TC_38_1_2_Activation_requires_cui_ready_checklist_incident_and_matrix()
    {
        using var client = CreateClient();
        var ids = Ids();
        var enclave = await CreateEnclaveAsync(client, ids);

        foreach (var readiness in new[]
        {
            Ready() with { TenantDataHandlingMode = TenantDataPosture.NoCui },
            Ready() with { ChecklistApproved = false },
            Ready() with { IncidentReadinessApproved = false },
            Ready() with { SharedResponsibilityMatrixAcknowledged = false }
        })
        {
            var blocked = await client.SendAsync(Request(HttpMethod.Post, $"/api/enterprise/cui/enclaves/{enclave.Id}/status", new CuiEnclaveStatusRequest(CuiEnclaveStatus.Active, "security-owner", readiness), ids));
            Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
        }

        var activated = await SetStatusAsync(client, ids, enclave.Id, CuiEnclaveStatus.Active);
        Assert.Equal(CuiEnclaveStatus.Active, activated.Status);
    }

    [Fact]
    public async Task TC_38_1_3_Approved_workflows_enforced_for_real_cui()
    {
        using var client = CreateClient();
        var ids = Ids();
        var enclave = await CreateEnclaveAsync(client, ids);
        await SetStatusAsync(client, ids, enclave.Id, CuiEnclaveStatus.Active);

        var approved = await ProcessingCheckAsync(client, ids, enclave.Id, "evidence-upload", true);
        var blocked = await ProcessingCheckAsync(client, ids, enclave.Id, "unapproved-export", true);

        Assert.True(approved.Allowed);
        Assert.False(blocked.Allowed);
    }

    [Fact]
    public async Task TC_38_1_4_Suspended_retired_and_revoked_enclaves_block_new_cui_processing()
    {
        using var client = CreateClient();
        var ids = Ids();
        var enclave = await CreateEnclaveAsync(client, ids);

        foreach (var status in new[] { CuiEnclaveStatus.Suspended, CuiEnclaveStatus.Retired, CuiEnclaveStatus.Revoked })
        {
            await SetStatusAsync(client, ids, enclave.Id, status);
            var decision = await ProcessingCheckAsync(client, ids, enclave.Id, "evidence-upload", true);
            Assert.False(decision.Allowed);
            Assert.Equal(status, decision.EnclaveStatus);
        }
    }

    [Fact]
    public async Task TC_38_1_5_Enclave_lifecycle_actions_are_audit_logged()
    {
        var audit = new CapturingAuditWriter();
        using var client = CreateClient(audit);
        var ids = Ids();
        var enclave = await CreateEnclaveAsync(client, ids);

        foreach (var status in new[]
        {
            CuiEnclaveStatus.UnderReview,
            CuiEnclaveStatus.Approved,
            CuiEnclaveStatus.Active,
            CuiEnclaveStatus.Suspended,
            CuiEnclaveStatus.Retired,
            CuiEnclaveStatus.Revoked
        })
        {
            await SetStatusAsync(client, ids, enclave.Id, status);
        }

        Assert.Contains(audit.Events, item => item.EntityType == "CuiEnclaveBoundary" && item.Action == AuditAction.Created);
        Assert.Contains(audit.Events, item => item.Summary.Contains("UnderReview", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(audit.Events, item => item.Summary.Contains("Active", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(audit.Events, item => item.Action == AuditAction.Archived && item.Summary.Contains("Revoked", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<CuiEnclaveBoundaryDto> CreateEnclaveAsync(HttpClient client, TestIds ids)
    {
        var response = await client.SendAsync(Request(HttpMethod.Post, "/api/enterprise/cui/enclaves", new CreateCuiEnclaveBoundaryRequest(
            "GovCloud",
            "CUI enclave boundary for approved tenant workflows.",
            TenantDataPosture.CuiReady,
            ["evidence-upload", "contract-intake"],
            "s3://gccs-cui/enclave-a",
            "aks-gov-cui-pool",
            "private endpoint only with no public ingress",
            "sentinel-cui-workspace",
            "immutable daily backup with restore test evidence",
            "just-in-time support only with customer approval",
            "security-owner"), ids));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<CuiEnclaveBoundaryDto>(await response.Content.ReadFromJsonAsync<CuiEnclaveBoundaryDto>(JsonOptions));
    }

    private static async Task<CuiEnclaveBoundaryDto> SetStatusAsync(HttpClient client, TestIds ids, Guid enclaveId, CuiEnclaveStatus status)
    {
        var response = await client.SendAsync(Request(HttpMethod.Post, $"/api/enterprise/cui/enclaves/{enclaveId}/status", new CuiEnclaveStatusRequest(status, "security-owner", Ready(), "reviewed"), ids));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Assert.IsType<CuiEnclaveBoundaryDto>(await response.Content.ReadFromJsonAsync<CuiEnclaveBoundaryDto>(JsonOptions));
    }

    private static async Task<CuiProcessingDecisionDto> ProcessingCheckAsync(HttpClient client, TestIds ids, Guid enclaveId, string workflow, bool containsRealCui)
    {
        var response = await client.SendAsync(Request(HttpMethod.Post, $"/api/enterprise/cui/enclaves/{enclaveId}/processing-check", new CuiProcessingRequest(workflow, containsRealCui), ids));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Assert.IsType<CuiProcessingDecisionDto>(await response.Content.ReadFromJsonAsync<CuiProcessingDecisionDto>(JsonOptions));
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
                services.AddSingleton<ICuiEnclaveBoundaryRepository, InMemoryCuiEnclaveBoundaryRepository>();
                services.AddScoped<CuiEnclaveBoundaryService>();
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

    private static CuiEnclaveReadinessRequest Ready() => new(TenantDataPosture.CuiReady, true, true, true);
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
