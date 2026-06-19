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

public sealed class TrustArtifactLibraryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public TrustArtifactLibraryTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task TC_37_2_1_Create_artifact_stores_required_metadata()
    {
        using var client = CreateClient();
        var ids = Ids();
        var artifact = await CreateAsync(client, ids);

        Assert.Equal("security-owner", artifact.Owner);
        Assert.Equal("v1.0", artifact.Version);
        Assert.Equal(TrustArtifactStatus.Draft, artifact.Status);
        Assert.Equal(TrustArtifactAudience.RegulatedCustomer, artifact.Audience);
        Assert.Equal(new DateOnly(2026, 6, 19), artifact.EffectiveDate);
        Assert.Equal(new DateOnly(2027, 6, 19), artifact.ExpirationDate);
        Assert.Equal("trust/security-overview.pdf", artifact.SourceFile);
    }

    [Fact]
    public async Task TC_37_2_2_Publication_requires_review_and_approval_metadata()
    {
        using var client = CreateClient();
        var ids = Ids();
        var artifact = await CreateAsync(client, ids);

        var invalid = await client.SendAsync(Request(HttpMethod.Post, $"/api/enterprise/trust-artifacts/{artifact.Id}/status", new TrustArtifactStatusRequest(TrustArtifactStatus.Published, "publisher"), ids));
        var valid = await SetStatusAsync(client, ids, artifact.Id, TrustArtifactStatus.Published);

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(TrustArtifactStatus.Published, valid.Status);
        Assert.Equal("approver", valid.ApprovedBy);
    }

    [Fact]
    public async Task TC_37_2_3_Draft_expired_and_superseded_artifacts_cannot_be_shared()
    {
        using var client = CreateClient();
        var ids = Ids();
        var draft = await CreateAsync(client, ids);
        var expired = await SetStatusAsync(client, ids, (await CreateAsync(client, ids, TrustArtifactType.DataRetentionPolicy)).Id, TrustArtifactStatus.Expired);
        var superseded = await SetStatusAsync(client, ids, (await CreateAsync(client, ids, TrustArtifactType.SupportSla)).Id, TrustArtifactStatus.Superseded);

        Assert.False((await ShareAsync(client, ids, draft.Id)).Allowed);
        Assert.False((await ShareAsync(client, ids, expired.Id)).Allowed);
        Assert.False((await ShareAsync(client, ids, superseded.Id)).Allowed);
    }

    [Fact]
    public async Task TC_37_2_4_Sharing_restrictions_enforce_audience_tenant_environment_and_nda()
    {
        using var client = CreateClient();
        var ids = Ids();
        var artifact = await SetStatusAsync(client, ids, (await CreateAsync(client, ids)).Id, TrustArtifactStatus.Published);

        var noNda = await ShareAsync(client, ids, artifact.Id, nda: false);
        var wrongAudience = await ShareAsync(client, ids, artifact.Id, audience: TrustArtifactAudience.Prospect);
        var wrongTier = await ShareAsync(client, ids, artifact.Id, tier: "commercial");
        var wrongEnvironment = await ShareAsync(client, ids, artifact.Id, environment: "commercial");
        var allowed = await ShareAsync(client, ids, artifact.Id);

        Assert.Equal("nda_required", noNda.ReasonCode);
        Assert.Equal("recipient_not_permitted", wrongAudience.ReasonCode);
        Assert.Equal("recipient_not_permitted", wrongTier.ReasonCode);
        Assert.Equal("recipient_not_permitted", wrongEnvironment.ReasonCode);
        Assert.True(allowed.Allowed);
    }

    [Fact]
    public async Task TC_37_2_5_Lifecycle_and_sharing_actions_are_audit_logged()
    {
        var audit = new CapturingAuditWriter();
        using var client = CreateClient(audit);
        var ids = Ids();
        var artifact = await CreateAsync(client, ids);
        foreach (var status in new[] { TrustArtifactStatus.InReview, TrustArtifactStatus.Approved, TrustArtifactStatus.Published, TrustArtifactStatus.Expired, TrustArtifactStatus.Superseded, TrustArtifactStatus.Archived })
        {
            artifact = await SetStatusAsync(client, ids, artifact.Id, status);
        }
        await ShareAsync(client, ids, artifact.Id);

        Assert.Contains(audit.Events, item => item.Action == AuditAction.Created);
        Assert.Contains(audit.Events, item => item.Summary.Contains("Archived", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(audit.Events, item => item.Action == AuditAction.Rejected);
    }

    private static async Task<TrustArtifactDto> CreateAsync(HttpClient client, TestIds ids, TrustArtifactType type = TrustArtifactType.SecurityOverview)
    {
        var response = await client.SendAsync(Request(HttpMethod.Post, "/api/enterprise/trust-artifacts", new CreateTrustArtifactRequest(type, "security-owner", "v1.0", TrustArtifactAudience.RegulatedCustomer, new DateOnly(2026, 6, 19), new DateOnly(2027, 6, 19), "trust/security-overview.pdf", true, "enterprise", "govcloud"), ids));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<TrustArtifactDto>(await response.Content.ReadFromJsonAsync<TrustArtifactDto>(JsonOptions));
    }

    private static async Task<TrustArtifactDto> SetStatusAsync(HttpClient client, TestIds ids, Guid artifactId, TrustArtifactStatus status)
    {
        var response = await client.SendAsync(Request(HttpMethod.Post, $"/api/enterprise/trust-artifacts/{artifactId}/status", new TrustArtifactStatusRequest(status, "actor", new DateOnly(2026, 6, 20), "reviewer", "approver"), ids));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Assert.IsType<TrustArtifactDto>(await response.Content.ReadFromJsonAsync<TrustArtifactDto>(JsonOptions));
    }

    private static async Task<TrustArtifactShareResult> ShareAsync(HttpClient client, TestIds ids, Guid artifactId, bool nda = true, TrustArtifactAudience audience = TrustArtifactAudience.RegulatedCustomer, string tier = "enterprise", string environment = "govcloud")
    {
        var response = await client.SendAsync(Request(HttpMethod.Post, $"/api/enterprise/trust-artifacts/{artifactId}/share", new TrustArtifactShareRequest("buyer@example.com", audience, tier, environment, nda), ids));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Assert.IsType<TrustArtifactShareResult>(await response.Content.ReadFromJsonAsync<TrustArtifactShareResult>(JsonOptions));
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
                services.AddSingleton<ITrustArtifactLibraryRepository, InMemoryTrustArtifactLibraryRepository>();
                services.AddScoped<TrustArtifactLibraryService>();
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
        request.Headers.Add("X-Gccs-Dev-Email", "cs@example.com");
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
            Events.Add(new CapturedAudit(action, summary));
            return Task.CompletedTask;
        }
    }

    private sealed record CapturedAudit(AuditAction Action, string Summary);
}
