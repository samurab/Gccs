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

public sealed class FedRampReadinessExportPackageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;
    public FedRampReadinessExportPackageTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task TC_37_3_1_Generate_package_from_approved_records()
    {
        using var client = CreateClient();
        var ids = Ids();
        var package = await GenerateAsync(client, ids);

        Assert.Equal("pkg-1", package.PackageVersion);
        Assert.Equal("Moderate readiness", package.Scope);
        Assert.Equal("GovCloud", package.Environment);
        Assert.Equal("fedramp-reviewer", package.Reviewer);
        Assert.NotEmpty(package.Gaps);
        Assert.NotEmpty(package.AcceptedRisks);
        Assert.Contains("ready", package.ReadinessSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Single(package.IncludedRecords);
    }

    [Fact]
    public async Task TC_37_3_2_Package_language_does_not_claim_authorization_without_governance_approval()
    {
        using var client = CreateClient();
        var ids = Ids();
        var package = await GenerateAsync(client, ids, governanceApproved: false);

        Assert.Contains("does not claim FedRAMP authorization", package.AuthorizationLanguage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_37_3_3_Excluded_records_are_not_exported()
    {
        using var client = CreateClient();
        var ids = Ids();
        var package = await GenerateAsync(client, ids, includeBadRecords: true);

        Assert.Single(package.IncludedRecords);
        Assert.DoesNotContain(package.IncludedRecords, record => record.Status is FedRampPackageRecordStatus.Draft or FedRampPackageRecordStatus.Expired or FedRampPackageRecordStatus.Superseded || record.Restricted || record.Prohibited || record.TenantId != ids.TenantId);
    }

    [Fact]
    public async Task TC_37_3_4_Package_lifecycle_transitions_store_metadata()
    {
        using var client = CreateClient();
        var ids = Ids();
        var package = await GenerateAsync(client, ids);
        foreach (var status in new[] { FedRampReadinessPackageStatus.InReview, FedRampReadinessPackageStatus.Approved, FedRampReadinessPackageStatus.Shared, FedRampReadinessPackageStatus.Superseded, FedRampReadinessPackageStatus.Archived })
        {
            package = await SetStatusAsync(client, ids, package.Id, status);
            Assert.Equal(status, package.Status);
            Assert.Equal("owner", package.LastActor);
        }
    }

    [Fact]
    public async Task TC_37_3_5_Package_actions_are_audit_logged()
    {
        var audit = new CapturingAuditWriter();
        using var client = CreateClient(audit);
        var ids = Ids();
        var package = await GenerateAsync(client, ids);
        await SetStatusAsync(client, ids, package.Id, FedRampReadinessPackageStatus.Approved);
        await client.SendAsync(Request(HttpMethod.Post, $"/api/enterprise/fedramp/readiness-packages/{package.Id}/share", new FedRampReadinessPackageShareRequest("advisor@example.com", "review"), ids));
        await SetStatusAsync(client, ids, package.Id, FedRampReadinessPackageStatus.Revoked);
        await SetStatusAsync(client, ids, package.Id, FedRampReadinessPackageStatus.Superseded);
        await SetStatusAsync(client, ids, package.Id, FedRampReadinessPackageStatus.Archived);

        Assert.Contains(audit.Events, item => item.Action == AuditAction.Created);
        Assert.Contains(audit.Events, item => item.Action == AuditAction.Exported);
        Assert.Contains(audit.Events, item => item.Summary.Contains("Archived", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<FedRampReadinessPackageDto> GenerateAsync(HttpClient client, TestIds ids, bool governanceApproved = false, bool includeBadRecords = false)
    {
        var records = new List<FedRampPackageRecordDto>
        {
            new("control", "AC-2", "Access control", FedRampPackageRecordStatus.Approved, false, false, ids.TenantId)
        };
        if (includeBadRecords)
        {
            records.AddRange([
                new("artifact", "draft", "Draft", FedRampPackageRecordStatus.Draft, false, false, ids.TenantId),
                new("artifact", "expired", "Expired", FedRampPackageRecordStatus.Expired, false, false, ids.TenantId),
                new("artifact", "superseded", "Superseded", FedRampPackageRecordStatus.Superseded, false, false, ids.TenantId),
                new("artifact", "restricted", "Restricted", FedRampPackageRecordStatus.Published, true, false, ids.TenantId),
                new("artifact", "prohibited", "Prohibited", FedRampPackageRecordStatus.Published, false, true, ids.TenantId),
                new("artifact", "cross", "Cross tenant", FedRampPackageRecordStatus.Published, false, false, Guid.NewGuid())
            ]);
        }

        var response = await client.SendAsync(Request(HttpMethod.Post, "/api/enterprise/fedramp/readiness-packages", new CreateFedRampReadinessPackageRequest("pkg-1", "Moderate readiness", "GovCloud", "fedramp-reviewer", governanceApproved, records.ToArray(), ["AC-2 gap"], ["accepted IA risk"], "Ready for advisor review."), ids));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<FedRampReadinessPackageDto>(await response.Content.ReadFromJsonAsync<FedRampReadinessPackageDto>(JsonOptions));
    }

    private static async Task<FedRampReadinessPackageDto> SetStatusAsync(HttpClient client, TestIds ids, Guid packageId, FedRampReadinessPackageStatus status)
    {
        var response = await client.SendAsync(Request(HttpMethod.Post, $"/api/enterprise/fedramp/readiness-packages/{packageId}/status", new FedRampReadinessPackageStatusRequest(status, "owner", "ok"), ids));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Assert.IsType<FedRampReadinessPackageDto>(await response.Content.ReadFromJsonAsync<FedRampReadinessPackageDto>(JsonOptions));
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
                services.AddSingleton<IFedRampReadinessExportPackageRepository, InMemoryFedRampReadinessExportPackageRepository>();
                services.AddScoped<FedRampReadinessExportPackageService>();
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
            Events.Add(new CapturedAudit(action, summary));
            return Task.CompletedTask;
        }
    }

    private sealed record CapturedAudit(AuditAction Action, string Summary);
}
