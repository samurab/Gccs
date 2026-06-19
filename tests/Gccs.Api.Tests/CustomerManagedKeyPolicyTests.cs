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

public sealed class CustomerManagedKeyPolicyTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;
    public CustomerManagedKeyPolicyTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task TC_38_2_1_Register_policy_for_approved_environment()
    {
        using var client = CreateClient();
        var ids = Ids();

        var policy = await RegisterAsync(client, ids);
        policy = await ValidateAsync(client, ids, policy.Id, Valid());

        Assert.Equal(ids.TenantId, policy.TenantId);
        Assert.Equal("AzureKeyVault", policy.Provider);
        Assert.Equal("cmk-gov-001", policy.KeyId);
        Assert.Equal("GovCloud", policy.Environment);
        Assert.Equal(CustomerManagedKeyPolicyStatus.Validated, policy.Status);
        Assert.Equal(90, policy.RotationCadenceDays);
        Assert.Equal("security-owner", policy.Owner);
        Assert.Equal("ciso", policy.Approver);
        Assert.NotNull(policy.LastValidation);
    }

    [Fact]
    public async Task TC_38_2_2_Invalid_key_policy_activation_is_blocked()
    {
        using var client = CreateClient();
        var ids = Ids();
        var policy = await RegisterAsync(client, ids);

        foreach (var validation in new[]
        {
            Valid() with { KeyAvailable = false },
            Valid() with { PermissionsGranted = false },
            Valid() with { RegionMatches = false },
            Valid() with { EncryptionCompatible = false },
            Valid() with { BackupImplicationsAccepted = false }
        })
        {
            var blocked = await client.SendAsync(Request(HttpMethod.Post, $"/api/enterprise/cui/customer-managed-key-policies/{policy.Id}/activate", validation, ids));
            Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
        }

        var activated = await ActivateAsync(client, ids, policy.Id);
        Assert.Equal(CustomerManagedKeyPolicyStatus.Active, activated.Status);
    }

    [Fact]
    public async Task TC_38_2_3_Key_lifecycle_history_preserves_reviewer_metadata()
    {
        using var client = CreateClient();
        var ids = Ids();
        var policy = await RegisterAsync(client, ids);
        policy = await ActivateAsync(client, ids, policy.Id);

        foreach (var status in new[] { CustomerManagedKeyPolicyStatus.Rotated, CustomerManagedKeyPolicyStatus.Suspended, CustomerManagedKeyPolicyStatus.Revoked, CustomerManagedKeyPolicyStatus.Validated })
        {
            policy = await SetStatusAsync(client, ids, policy.Id, status);
        }

        Assert.Contains(policy.History, item => item.Status == CustomerManagedKeyPolicyStatus.Rotated && item.Reviewer == "key-reviewer");
        Assert.Contains(policy.History, item => item.Status == CustomerManagedKeyPolicyStatus.Suspended && item.Reviewer == "key-reviewer");
        Assert.Contains(policy.History, item => item.Status == CustomerManagedKeyPolicyStatus.Revoked && item.Reviewer == "key-reviewer");
        Assert.Contains(policy.History, item => item.Status == CustomerManagedKeyPolicyStatus.Validated && item.Reviewer == "key-reviewer");
    }

    [Fact]
    public async Task TC_38_2_4_Unavailable_revoked_or_suspended_keys_block_dependent_workflows()
    {
        using var client = CreateClient();
        var ids = Ids();
        var policy = await RegisterAsync(client, ids);
        policy = await ActivateAsync(client, ids, policy.Id);
        var allowed = await WorkflowCheckAsync(client, ids, policy.Id);
        Assert.True(allowed.Allowed);

        foreach (var status in new[] { CustomerManagedKeyPolicyStatus.Suspended, CustomerManagedKeyPolicyStatus.Revoked, CustomerManagedKeyPolicyStatus.ValidationFailed })
        {
            await SetStatusAsync(client, ids, policy.Id, status);
            var blocked = await WorkflowCheckAsync(client, ids, policy.Id);
            Assert.False(blocked.Allowed);
            Assert.Equal(status, blocked.Status);
        }
    }

    [Fact]
    public async Task TC_38_2_5_Key_events_are_audit_logged()
    {
        var audit = new CapturingAuditWriter();
        using var client = CreateClient(audit);
        var ids = Ids();
        var policy = await RegisterAsync(client, ids);

        await ValidateAsync(client, ids, policy.Id, Valid());
        await ActivateAsync(client, ids, policy.Id);
        await SetStatusAsync(client, ids, policy.Id, CustomerManagedKeyPolicyStatus.Rotated);
        await SetStatusAsync(client, ids, policy.Id, CustomerManagedKeyPolicyStatus.Suspended);
        await SetStatusAsync(client, ids, policy.Id, CustomerManagedKeyPolicyStatus.Revoked);
        await ValidateAsync(client, ids, policy.Id, Valid() with { KeyAvailable = false });

        Assert.Contains(audit.Events, item => item.EntityType == "CustomerManagedKeyPolicy" && item.Action == AuditAction.Created);
        Assert.Contains(audit.Events, item => item.Summary.Contains("validation succeeded", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(audit.Events, item => item.Summary.Contains("activated", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(audit.Events, item => item.Summary.Contains("Rotated", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(audit.Events, item => item.Action == AuditAction.Archived && item.Summary.Contains("Revoked", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(audit.Events, item => item.Action == AuditAction.Rejected);
    }

    private static async Task<CustomerManagedKeyPolicyDto> RegisterAsync(HttpClient client, TestIds ids)
    {
        var response = await client.SendAsync(Request(HttpMethod.Post, "/api/enterprise/cui/customer-managed-key-policies", new RegisterCustomerManagedKeyPolicyRequest(
            "AzureKeyVault",
            "cmk-gov-001",
            "GovCloud",
            90,
            new DateOnly(2026, 1, 15),
            new DateOnly(2026, 7, 15),
            "security-owner",
            "ciso",
            "security-oncall@example.com"), ids));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<CustomerManagedKeyPolicyDto>(await response.Content.ReadFromJsonAsync<CustomerManagedKeyPolicyDto>(JsonOptions));
    }

    private static async Task<CustomerManagedKeyPolicyDto> ValidateAsync(HttpClient client, TestIds ids, Guid policyId, CustomerManagedKeyValidationRequest validation)
    {
        var response = await client.SendAsync(Request(HttpMethod.Post, $"/api/enterprise/cui/customer-managed-key-policies/{policyId}/validate", validation, ids));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Assert.IsType<CustomerManagedKeyPolicyDto>(await response.Content.ReadFromJsonAsync<CustomerManagedKeyPolicyDto>(JsonOptions));
    }

    private static async Task<CustomerManagedKeyPolicyDto> ActivateAsync(HttpClient client, TestIds ids, Guid policyId)
    {
        var response = await client.SendAsync(Request(HttpMethod.Post, $"/api/enterprise/cui/customer-managed-key-policies/{policyId}/activate", Valid(), ids));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Assert.IsType<CustomerManagedKeyPolicyDto>(await response.Content.ReadFromJsonAsync<CustomerManagedKeyPolicyDto>(JsonOptions));
    }

    private static async Task<CustomerManagedKeyPolicyDto> SetStatusAsync(HttpClient client, TestIds ids, Guid policyId, CustomerManagedKeyPolicyStatus status)
    {
        var response = await client.SendAsync(Request(HttpMethod.Post, $"/api/enterprise/cui/customer-managed-key-policies/{policyId}/status", new CustomerManagedKeyStatusRequest(status, "key-reviewer", "reviewed"), ids));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Assert.IsType<CustomerManagedKeyPolicyDto>(await response.Content.ReadFromJsonAsync<CustomerManagedKeyPolicyDto>(JsonOptions));
    }

    private static async Task<CustomerManagedKeyWorkflowDecisionDto> WorkflowCheckAsync(HttpClient client, TestIds ids, Guid policyId)
    {
        var response = await client.SendAsync(Request(HttpMethod.Post, $"/api/enterprise/cui/customer-managed-key-policies/{policyId}/workflow-check", new CustomerManagedKeyWorkflowRequest("evidence-encryption"), ids));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Assert.IsType<CustomerManagedKeyWorkflowDecisionDto>(await response.Content.ReadFromJsonAsync<CustomerManagedKeyWorkflowDecisionDto>(JsonOptions));
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
                services.AddSingleton<ICustomerManagedKeyPolicyRepository, InMemoryCustomerManagedKeyPolicyRepository>();
                services.AddScoped<CustomerManagedKeyPolicyService>();
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

    private static CustomerManagedKeyValidationRequest Valid() => new(true, true, true, true, true, "key-reviewer");
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
