using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Identity;
using Gccs.Application.Portals;
using Gccs.Domain.Common;
using Gccs.Domain.Evidence;
using Gccs.Domain.Reports;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Identity;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Portals;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ApprovedPackagePortalReviewEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly WebApplicationFactory<Program> _factory;

    public ApprovedPackagePortalReviewEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Dashboard_excludes_draft_internal_unknown_prohibited_and_cross_tenant_packages()
    {
        var ids = TestIds.Create();
        await using var factory = CreateFactory(nameof(Dashboard_excludes_draft_internal_unknown_prohibited_and_cross_tenant_packages), ids);
        using var client = factory.CreateClient();

        var response = await client.SendAsync(PortalRequest(HttpMethod.Get,
            $"/api/external-portal/invitations/{ids.InvitationId}/packages", ids));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var packages = Assert.IsType<PortalPackageDashboardItemDto[]>(
            await response.Content.ReadFromJsonAsync<PortalPackageDashboardItemDto[]>(JsonOptions));
        var package = Assert.Single(packages);
        Assert.Equal(ids.ApprovedPackageId, package.PackageId);
        Assert.Equal(ids.EvidenceId, Assert.Single(package.EvidenceItemIds));
        Assert.Equal("Approved evidence", Assert.Single(package.EvidenceReferences).Name);
        Assert.NotEqual(default, package.ReviewDueAt);
        Assert.NotEqual(default, package.ExternalReviewApprovedAt);
        Assert.True(package.DownloadAvailable);
    }

    [Fact]
    public async Task Reviewer_message_is_durable_append_only_and_does_not_modify_source_package()
    {
        var ids = TestIds.Create();
        await using var factory = CreateFactory(nameof(Reviewer_message_is_durable_append_only_and_does_not_modify_source_package), ids);
        using var client = factory.CreateClient();
        string originalSnapshot;
        using (var scope = factory.Services.CreateScope())
            originalSnapshot = await scope.ServiceProvider.GetRequiredService<GccsDbContext>().Reports
                .Where(item => item.Id == ids.ApprovedPackageId).Select(item => item.SnapshotJson).SingleAsync();

        var response = await client.SendAsync(PortalRequest(HttpMethod.Post,
            $"/api/external-portal/invitations/{ids.InvitationId}/packages/{ids.SharedPackageId}/messages", ids,
            new PortalPackageReviewMessageRequest(PortalCommentKind.Question, "Which source supports this status?")));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var responseJson = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(ids.TenantId.ToString(), responseJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(ids.ReviewerUserId.ToString(), responseJson, StringComparison.OrdinalIgnoreCase);
        using var verificationScope = factory.Services.CreateScope();
        var db = verificationScope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var message = Assert.Single(await db.PortalPackageReviewMessages.AsNoTracking().ToArrayAsync());
        Assert.Equal(ids.SharedPackageId, message.SharedPackageId);
        Assert.Equal(PortalCommentKind.Question, message.Kind);
        Assert.Equal(originalSnapshot, await db.Reports.Where(item => item.Id == ids.ApprovedPackageId)
            .Select(item => item.SnapshotJson).SingleAsync());
        Assert.Contains(await db.PortalPackageActivities.AsNoTracking().ToArrayAsync(),
            item => item.ActivityType == PortalPackageActivityType.Comment);
        Assert.Contains(await db.AuditLogEntries.AsNoTracking().ToArrayAsync(),
            item => item.EntityType == "PortalPackageReviewMessage");
        var trackedMessage = await db.PortalPackageReviewMessages.SingleAsync(item => item.Id == message.Id);
        trackedMessage.Body = "Attempted replacement";
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Download_requires_permission_and_includes_server_watermark_and_metadata_headers()
    {
        var ids = TestIds.Create();
        await using var factory = CreateFactory(nameof(Download_requires_permission_and_includes_server_watermark_and_metadata_headers), ids);
        using var client = factory.CreateClient();

        var response = await client.SendAsync(PortalRequest(HttpMethod.Get,
            $"/api/external-portal/invitations/{ids.InvitationId}/packages/{ids.SharedPackageId}/download", ids));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ids.ApprovedPackageId.ToString(), response.Headers.GetValues("X-FeDril-Package-Id").Single());
        Assert.Equal("true", response.Headers.GetValues("X-FeDril-Watermark-Applied").Single());
        Assert.Contains("data-portal-watermark", await response.Content.ReadAsStringAsync());

        var denied = await client.SendAsync(PortalRequest(HttpMethod.Get,
            $"/api/external-portal/invitations/{ids.NoDownloadInvitationId}/packages/{ids.NoDownloadSharedPackageId}/download", ids));
        Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
    }

    [Fact]
    public async Task Strong_authentication_is_enforced_server_side()
    {
        var ids = TestIds.Create();
        await using var factory = CreateFactory(nameof(Strong_authentication_is_enforced_server_side), ids);
        using var client = factory.CreateClient();
        var request = PortalRequest(HttpMethod.Get,
            $"/api/external-portal/invitations/{ids.InvitationId}/packages", ids);
        request.Headers.Remove("X-Gccs-Dev-Amr");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<GccsDbContext>()
            .PortalPackageActivities.AsNoTracking().ToArrayAsync());
    }

    [Fact]
    public async Task Cross_tenant_shared_package_is_not_disclosed_or_mutated()
    {
        var ids = TestIds.Create();
        await using var factory = CreateFactory(nameof(Cross_tenant_shared_package_is_not_disclosed_or_mutated), ids);
        using var client = factory.CreateClient();

        var response = await client.SendAsync(PortalRequest(HttpMethod.Post,
            $"/api/external-portal/invitations/{ids.InvitationId}/packages/{ids.CrossTenantSharedPackageId}/messages", ids,
            new PortalPackageReviewMessageRequest(PortalCommentKind.Comment, "Cross-tenant probe")));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Empty(await db.PortalPackageReviewMessages.AsNoTracking().ToArrayAsync());
        Assert.Empty(await db.PortalPackageActivities.AsNoTracking().ToArrayAsync());
    }

    [Fact]
    public async Task Evidence_that_becomes_ineligible_after_sharing_removes_package_from_dashboard()
    {
        var ids = TestIds.Create();
        await using var factory = CreateFactory(nameof(Evidence_that_becomes_ineligible_after_sharing_removes_package_from_dashboard), ids);
        using var client = factory.CreateClient();
        var uri = $"/api/external-portal/invitations/{ids.InvitationId}/packages";
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(PortalRequest(HttpMethod.Get, uri, ids))).StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            var evidence = await db.EvidenceItems.SingleAsync(item => item.Id == ids.EvidenceId);
            evidence.Status = EvidenceStatus.Rejected;
            await db.SaveChangesAsync();
        }

        var response = await client.SendAsync(PortalRequest(HttpMethod.Get, uri, ids));
        var packages = await response.Content.ReadFromJsonAsync<PortalPackageDashboardItemDto[]>(JsonOptions);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(Assert.IsType<PortalPackageDashboardItemDto[]>(packages));
    }

    [Fact]
    public async Task Legacy_share_without_a_source_fingerprint_is_hidden_until_reapproved()
    {
        var ids = TestIds.Create();
        await using var factory = CreateFactory(nameof(Legacy_share_without_a_source_fingerprint_is_hidden_until_reapproved), ids);
        using var client = factory.CreateClient();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            var share = await db.SharedPortalPackages.SingleAsync(item => item.Id == ids.SharedPackageId);
            share.ApprovedSourceFingerprint = string.Empty;
            await db.SaveChangesAsync();
        }

        var response = await client.SendAsync(PortalRequest(HttpMethod.Get,
            $"/api/external-portal/invitations/{ids.InvitationId}/packages", ids));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(Assert.IsType<PortalPackageDashboardItemDto[]>(
            await response.Content.ReadFromJsonAsync<PortalPackageDashboardItemDto[]>(JsonOptions)));
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, TestIds ids) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.UseSetting("Security:MembershipAuthorization:Enforce", "true");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<GccsDbContext>();
                services.RemoveAll<DbContextOptions<GccsDbContext>>();
                services.RemoveAll<IExternalPortalAccessRepository>();
                services.RemoveAll<IPortalPackageLifecycleRepository>();
                services.RemoveAll<IPortalPackageRepository>();
                services.RemoveAll<IAuditEventWriter>();
                services.RemoveAll<ITenantMembershipRepository>();
                services.RemoveAll<PortalReviewDownloadPolicy>();
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<IExternalPortalAccessRepository, EfExternalPortalAccessRepository>();
                services.AddScoped<IPortalPackageLifecycleRepository, EfPortalPackageLifecycleRepository>();
                services.AddScoped<IPortalPackageRepository, EfPortalPackageRepository>();
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();
                services.AddScoped<ITenantMembershipRepository, EfTenantMembershipRepository>();
                services.AddSingleton(new PortalReviewDownloadPolicy(true));

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                db.Database.EnsureDeleted(); db.Database.EnsureCreated();
                Seed(db, ids);
            });
        });

    private static void Seed(GccsDbContext db, TestIds ids)
    {
        var now = DateTimeOffset.UtcNow;
        db.Tenants.AddRange(Tenant(ids.TenantId), Tenant(ids.OtherTenantId));
        db.EvidenceItems.Add(new EvidenceItemEntity
        {
            Id = ids.EvidenceId, TenantId = ids.TenantId, Name = "Approved evidence", Description = "Safe reference",
            Type = EvidenceType.Policy, Status = EvidenceStatus.Approved, Classification = ContentClassification.Fci,
            ApprovedAt = now, ApprovedByUserId = ids.AdminUserId, CreatedAt = now
        });
        db.Reports.AddRange(
            Report(ids.ApprovedPackageId, ids.TenantId, ReportStatus.Complete, ContentClassification.Fci, "{}", now, ids.EvidenceId),
            Report(ids.DraftPackageId, ids.TenantId, ReportStatus.Generating, ContentClassification.Fci, "{}", now),
            Report(ids.InternalPackageId, ids.TenantId, ReportStatus.Complete, ContentClassification.Fci, "{\"reviewerNotes\":\"internal\"}", now),
            Report(ids.CuiPackageId, ids.TenantId, ReportStatus.Complete, ContentClassification.Cui, "{}", now),
            Report(ids.SyntheticCuiPackageId, ids.TenantId, ReportStatus.Complete, ContentClassification.SyntheticCui, "{}", now),
            Report(ids.UnknownPackageId, ids.TenantId, ReportStatus.Complete, ContentClassification.Unknown, "{}", now),
            Report(ids.ProhibitedPackageId, ids.TenantId, ReportStatus.Complete, ContentClassification.Prohibited, "{}", now),
            Report(ids.CrossTenantPackageId, ids.OtherTenantId, ReportStatus.Complete, ContentClassification.Fci, "{}", now));
        db.ExternalPortalInvitations.AddRange(
            Invitation(ids.InvitationId, ids.TenantId, true, ids, now),
            Invitation(ids.NoDownloadInvitationId, ids.TenantId, false, ids, now),
            new ExternalPortalInvitationEntity
            {
                Id = ids.CrossTenantInvitationId, TenantId = ids.OtherTenantId,
                Email = "other-reviewer@example.test", Role = ExternalPortalRole.AuditorReviewer,
                ExpiresAt = now.AddDays(30), CanDownload = true, StrongAuthenticationRequired = true,
                Status = ExternalPortalInvitationStatus.Pending, Version = 1, CreatedAt = now,
                PackageScopes =
                [
                    new ExternalPortalInvitationPackageScopeEntity
                    {
                        TenantId = ids.OtherTenantId, InvitationId = ids.CrossTenantInvitationId,
                        PackageId = ids.CrossTenantPackageId
                    }
                ]
            });
        db.SharedPortalPackages.AddRange(
            Share(ids.SharedPackageId, ids.TenantId, ids.InvitationId, ids.ApprovedPackageId, now, ids.EvidenceId),
            Share(Guid.NewGuid(), ids.TenantId, ids.InvitationId, ids.DraftPackageId, now),
            Share(Guid.NewGuid(), ids.TenantId, ids.InvitationId, ids.InternalPackageId, now),
            Share(Guid.NewGuid(), ids.TenantId, ids.InvitationId, ids.CuiPackageId, now),
            Share(Guid.NewGuid(), ids.TenantId, ids.InvitationId, ids.SyntheticCuiPackageId, now),
            Share(Guid.NewGuid(), ids.TenantId, ids.InvitationId, ids.UnknownPackageId, now),
            Share(Guid.NewGuid(), ids.TenantId, ids.InvitationId, ids.ProhibitedPackageId, now),
            Share(ids.NoDownloadSharedPackageId, ids.TenantId, ids.NoDownloadInvitationId, ids.ApprovedPackageId, now, ids.EvidenceId),
            Share(ids.CrossTenantSharedPackageId, ids.OtherTenantId, ids.CrossTenantInvitationId, ids.CrossTenantPackageId, now));
        db.SaveChanges();
    }

    private static TenantEntity Tenant(Guid id) => new()
    {
        Id = id, Name = id.ToString(), Status = TenantStatus.Active,
        DataPosture = TenantDataPosture.NoCui, CreatedAt = DateTimeOffset.UtcNow
    };

    private static ReportEntity Report(Guid id, Guid tenantId, ReportStatus status,
        ContentClassification classification, string snapshot, DateTimeOffset generatedAt, Guid? evidenceId = null)
    {
        var report = new ReportEntity
        {
            Id = id, TenantId = tenantId, Type = ReportType.CmmcReadiness, Title = $"Package {id:N}",
            Status = status, GeneratedAt = generatedAt, GeneratedByUserId = Guid.NewGuid(),
            Classification = classification, SnapshotJson = snapshot, ExportHtml = "<main>Approved package</main>",
            CreatedAt = DateTimeOffset.UtcNow
        };
        if (evidenceId is not null) report.EvidenceItems.Add(new ReportEvidenceEntity { ReportId = id, EvidenceItemId = evidenceId.Value });
        return report;
    }

    private static ExternalPortalInvitationEntity Invitation(
        Guid id, Guid tenantId, bool canDownload, TestIds ids, DateTimeOffset now) => new()
    {
        Id = id, TenantId = tenantId, Email = "reviewer@example.test", Role = ExternalPortalRole.AuditorReviewer,
        ExpiresAt = now.AddDays(30), CanDownload = canDownload, StrongAuthenticationRequired = true,
        Status = ExternalPortalInvitationStatus.Pending, Version = 1, CreatedAt = now,
            PackageScopes = new[] { ids.ApprovedPackageId, ids.DraftPackageId, ids.InternalPackageId, ids.CuiPackageId, ids.SyntheticCuiPackageId, ids.UnknownPackageId, ids.ProhibitedPackageId }
            .Select(packageId => new ExternalPortalInvitationPackageScopeEntity { TenantId = tenantId, InvitationId = id, PackageId = packageId }).ToArray()
    };

    private static SharedPortalPackageEntity Share(
        Guid id, Guid tenantId, Guid invitationId, Guid packageId, DateTimeOffset now, Guid? evidenceId = null) => new()
    {
        Id = id, TenantId = tenantId, InvitationId = invitationId, PackageId = packageId,
        Version = 1, State = SharedPortalPackageState.Active, ExpiresAt = now.AddDays(30),
        ReviewDueAt = now.AddDays(21), ExternalReviewApprovedAt = now,
        ExternalReviewApprovedByUserId = null,
        ExternalReviewApprovalReason = "Endpoint test approval",
        ApprovedSourceVersion = 1, ApprovedSourceFingerprint = ReportFingerprint(packageId, now, evidenceId),
        ReminderAt = now.AddDays(23), CreatedAt = now
    };

    private static string ReportFingerprint(Guid packageId, DateTimeOffset generatedAt, Guid? evidenceId)
    {
        var evidenceIds = evidenceId is null ? Array.Empty<Guid>() : [evidenceId.Value];
        return PortalPackageFingerprint.Create(new PortalPackageDto(
            packageId, Guid.Empty, null, $"Package {packageId:N}", 1, PortalPackageStatus.Approved,
            ContentClassification.Fci, false, evidenceIds, generatedAt)
        {
            SourceKind = $"Report:{ReportType.CmmcReadiness}",
            EvidenceReferences = evidenceId is null ? [] :
                [new PortalEvidenceReferenceDto(evidenceId.Value, "Approved evidence", EvidenceType.Policy.ToString(),
                    ContentClassification.Fci, generatedAt, null)],
            SourceIntegrityFingerprint = Hash($"Package {packageId:N}", ReportStatus.Complete.ToString(),
                ContentClassification.Fci.ToString(), "{}", "<main>Approved package</main>")
        });
    }

    private static string Hash(params string?[] values) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(string.Join('\u001f', values))))
            .ToLowerInvariant();

    private static HttpRequestMessage PortalRequest<T>(HttpMethod method, string uri, TestIds ids, T body)
    {
        var request = PortalRequest(method, uri, ids);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        return request;
    }

    private static HttpRequestMessage PortalRequest(HttpMethod method, string uri, TestIds ids)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-User", ids.ReviewerUserId.ToString());
        request.Headers.Add("X-Gccs-Dev-Email", "reviewer@example.test");
        request.Headers.Add("X-Gccs-Dev-Amr", "mfa");
        return request;
    }

    private sealed record TestIds(
        Guid TenantId, Guid OtherTenantId, Guid AdminUserId, Guid ReviewerUserId,
        Guid InvitationId, Guid NoDownloadInvitationId, Guid CrossTenantInvitationId,
        Guid SharedPackageId, Guid NoDownloadSharedPackageId, Guid CrossTenantSharedPackageId,
        Guid ApprovedPackageId, Guid DraftPackageId, Guid InternalPackageId, Guid UnknownPackageId,
        Guid ProhibitedPackageId, Guid CuiPackageId, Guid SyntheticCuiPackageId,
        Guid CrossTenantPackageId, Guid EvidenceId)
    {
        public static TestIds Create() => new(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid());
    }
}
