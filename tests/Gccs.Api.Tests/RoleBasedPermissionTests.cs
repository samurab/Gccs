using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Identity;
using Gccs.Application.Reports;
using Gccs.Application.Tenancy;
using Gccs.Application.Storage;
using Gccs.Api.Security;
using Gccs.Domain.Audit;
using Gccs.Domain.Evidence;
using Gccs.Domain.Identity;
using Gccs.Domain.Reports;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Identity;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Reports;
using Gccs.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class RoleBasedPermissionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public RoleBasedPermissionTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void TC_2_4_1_Role_catalog_maps_permissions_across_mvp_workflow_areas()
    {
        Assert.Equal(
            [RoleCatalog.Owner, RoleCatalog.Admin, RoleCatalog.ComplianceManager, RoleCatalog.Contributor, RoleCatalog.Auditor, RoleCatalog.Advisor],
            RoleCatalog.Roles);

        AssertRoleHas(RoleCatalog.Owner, Permission.ManageTenant, Permission.ManageUsers, Permission.ManageReports, Permission.ArchiveReports, Permission.ExportReports, Permission.ViewAuditLog);
        AssertRoleHas(RoleCatalog.Admin, Permission.ManageUsers, Permission.ManageCompanyProfile, Permission.ManageContracts, Permission.ManageEvidence, Permission.ArchiveReports);
        AssertRoleHas(RoleCatalog.ComplianceManager, Permission.ManageObligations, Permission.ManageTasks, Permission.ApproveEvidence, Permission.ManageSubcontractors);
        AssertRoleHas(RoleCatalog.Contributor, Permission.ViewCompanyProfile, Permission.ManageTasks, Permission.ManageEvidence);
        AssertRoleHas(RoleCatalog.Auditor, Permission.AuditorReadOnly, Permission.ViewEvidence, Permission.ViewReports);
        AssertRoleHas(RoleCatalog.Advisor, Permission.ManageContracts, Permission.ManageObligations, Permission.ManageReports, Permission.ViewAuditLog);

        Assert.All(RoleCatalog.Roles, roleName =>
            AssertRoleHas(
                roleName,
                Permission.ViewCompanyProfile,
                Permission.ViewContracts,
                Permission.ViewObligations,
                Permission.ViewTasks,
                Permission.ViewEvidence,
                Permission.ViewCmmc,
                Permission.ViewSubcontractors,
                Permission.ViewReports));
        AssertRoleHas(
            RoleCatalog.Admin,
            Permission.ManageCompanyProfile,
            Permission.ManageContracts,
            Permission.ManageObligations,
            Permission.ManageTasks,
            Permission.ManageEvidence,
            Permission.ApproveEvidence,
            Permission.ManageCmmc,
            Permission.ManageSubcontractors,
            Permission.ManageReports);
        AssertRoleDoesNotHave(RoleCatalog.Admin, Permission.ManageTenant, Permission.ExportReports);
        AssertRoleDoesNotHave(RoleCatalog.Contributor, Permission.ManageUsers, Permission.ApproveEvidence, Permission.ManageReports, Permission.ArchiveReports);
        AssertRoleDoesNotHave(
            RoleCatalog.Auditor,
            Permission.ManageUsers,
            Permission.ManageCompanyProfile,
            Permission.ManageContracts,
            Permission.ManageObligations,
            Permission.ManageTasks,
            Permission.ManageEvidence,
            Permission.ApproveEvidence,
            Permission.ManageCmmc,
            Permission.ManageSubcontractors,
            Permission.ManageReports,
            Permission.ArchiveReports,
            Permission.ExportReports,
            Permission.ManageTenant);
        AssertRoleDoesNotHave(RoleCatalog.Advisor, Permission.ManageUsers, Permission.ManageTenant, Permission.ArchiveReports);
        AssertRoleDoesNotHave(RoleCatalog.ComplianceManager, Permission.ArchiveReports);
    }

    [Fact]
    public void Every_role_that_can_manage_reports_can_also_view_reports()
    {
        Assert.All(RoleCatalog.Roles, roleName =>
        {
            var permissions = RoleCatalog.GetPermissions(roleName);
            if (permissions.Contains(Permission.ManageReports))
            {
                Assert.Contains(Permission.ViewReports, permissions);
            }
        });
    }

    [Fact]
    public void Report_archive_permission_is_limited_to_owner_and_admin()
    {
        var rolesWithArchivePermission = RoleCatalog.Roles
            .Where(roleName => RoleCatalog.GetPermissions(roleName).Contains(Permission.ArchiveReports))
            .ToArray();

        Assert.Equal([RoleCatalog.Owner, RoleCatalog.Admin], rolesWithArchivePermission);
    }

    [Fact]
    public void Report_export_permission_is_limited_to_owner()
    {
        var rolesWithExportPermission = RoleCatalog.Roles
            .Where(roleName => RoleCatalog.GetPermissions(roleName).Contains(Permission.ExportReports))
            .ToArray();

        Assert.Equal([RoleCatalog.Owner], rolesWithExportPermission);
    }

    [Fact]
    public async Task TC_2_4_1_Server_side_permission_checks_use_role_derived_permissions()
    {
        var tenantId = Guid.Parse("24242424-2424-2424-2424-2424242424a1");
        await using var factory = CreateFactory("tc-2-4-1-server", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-2.4.1 Tenant"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();

        using var contributorObligationsRequest = CreateRequest(HttpMethod.Get, "/api/obligations", tenantId, Guid.NewGuid(), RoleCatalog.Contributor);
        using var auditorInvitationsRequest = CreateRequest(HttpMethod.Get, "/api/tenant-invitations", tenantId, Guid.NewGuid(), RoleCatalog.Auditor);
        using var adminInvitationsRequest = CreateRequest(HttpMethod.Get, "/api/tenant-invitations", tenantId, Guid.NewGuid(), RoleCatalog.Admin);
        using var adminTenantCreateRequest = CreateRequest(
            HttpMethod.Post,
            "/api/tenants",
            new CreateTenantRequest("Blocked Admin Tenant"),
            tenantId,
            Guid.NewGuid(),
            RoleCatalog.Admin);

        var contributorObligationsResponse = await client.SendAsync(contributorObligationsRequest);
        var auditorInvitationsResponse = await client.SendAsync(auditorInvitationsRequest);
        var adminInvitationsResponse = await client.SendAsync(adminInvitationsRequest);
        var adminTenantCreateResponse = await client.SendAsync(adminTenantCreateRequest);

        Assert.Equal(HttpStatusCode.OK, contributorObligationsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, auditorInvitationsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, adminInvitationsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, adminTenantCreateResponse.StatusCode);
    }

    [Fact]
    public async Task TC_2_4_1_Roles_match_permission_matrix_for_representative_implemented_endpoints()
    {
        var tenantId = Guid.Parse("24242424-2424-2424-2424-2424242424b1");
        await using var factory = CreateFactory("tc-2-4-1-matrix", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-2.4.1 Matrix Tenant"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();

        var checks = new[]
        {
            EndpointPermissionCheck.Get("tenant-context", $"/api/tenants/{tenantId}", Permission.ViewCompanyProfile, HttpStatusCode.OK),
            EndpointPermissionCheck.Get("obligation", "/api/obligations", Permission.ViewObligations, HttpStatusCode.OK),
            EndpointPermissionCheck.Get("report", "/api/reports/approved-evidence-packages", Permission.ViewReports, HttpStatusCode.OK),
            EndpointPermissionCheck.Get("admin-read", "/api/tenant-members", Permission.ManageUsers, HttpStatusCode.OK),
            EndpointPermissionCheck.Json(
                "admin-write",
                HttpMethod.Post,
                "/api/tenant-invitations",
                Permission.ManageUsers,
                HttpStatusCode.Created,
                roleName => new CreateTenantInvitationRequest(
                    $"{Slugify(roleName)}.{Guid.NewGuid():N}@example.com",
                    RoleCatalog.Contributor,
                    7)),
            EndpointPermissionCheck.Json(
                "tenant-admin",
                HttpMethod.Post,
                "/api/tenants",
                Permission.ManageTenant,
                HttpStatusCode.Created,
                roleName => new CreateTenantRequest($"TC-2.4.1 {roleName} Tenant {Guid.NewGuid():N}"))
        };

        foreach (var roleName in RoleCatalog.Roles)
        {
            var rolePermissions = RoleCatalog.GetPermissions(roleName);

            foreach (var check in checks)
            {
                using var request = CreateRequest(
                    check.Method,
                    check.Path,
                    check.BodyFactory?.Invoke(roleName),
                    tenantId,
                    Guid.NewGuid(),
                    roleName);

                var response = await client.SendAsync(request);
                var expectedStatus = rolePermissions.Contains(check.RequiredPermission)
                    ? check.AllowedStatus
                    : HttpStatusCode.Forbidden;
                var responseBody = await response.Content.ReadAsStringAsync();

                Assert.True(
                    response.StatusCode == expectedStatus,
                    $"{roleName} {check.Area} {check.Method} {check.Path} expected {expectedStatus} but returned {response.StatusCode}. Body: {responseBody}");

                if (expectedStatus == HttpStatusCode.Forbidden)
                {
                    AssertStandardAuthorizationError(response, responseBody);
                }
            }
        }
    }

    [Fact]
    public async Task TC_2_4_3_Permission_failures_return_standard_problem_details()
    {
        var tenantId = Guid.Parse("24242424-2424-2424-2424-2424242424a3");
        await using var factory = CreateFactory("tc-2-4-3", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-2.4.3 Tenant"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        using var request = CreateRequest(HttpMethod.Get, "/api/tenant-invitations", tenantId, Guid.NewGuid(), RoleCatalog.Auditor);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Permission denied", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("permission_denied", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_2_4_4_Auditor_can_view_tenant_scoped_approved_evidence_packages_but_cannot_modify_data()
    {
        var tenantAId = Guid.Parse("24242424-2424-2424-2424-2424242424a4");
        var tenantBId = Guid.Parse("24242424-2424-2424-2424-2424242424b4");
        var tenantAReportId = Guid.Parse("24242424-2424-2424-2424-2424242424c4");
        var tenantBReportId = Guid.Parse("24242424-2424-2424-2424-2424242424d4");
        var approvedEvidenceId = Guid.Parse("24242424-2424-2424-2424-2424242424e4");
        var draftEvidenceId = Guid.Parse("24242424-2424-2424-2424-2424242424f4");
        var tenantBEvidenceId = Guid.Parse("24242424-2424-2424-2424-2424242424b5");
        await using var factory = CreateFactory("tc-2-4-4", dbContext =>
        {
            dbContext.Tenants.AddRange(
                CreateTenant(tenantAId, "TC-2.4.4 Tenant A"),
                CreateTenant(tenantBId, "TC-2.4.4 Tenant B"));
            dbContext.Reports.AddRange(
                CreateEvidencePackage(tenantAReportId, tenantAId, "Tenant A Approved Package"),
                CreateEvidencePackage(tenantBReportId, tenantBId, "Tenant B Approved Package"));
            dbContext.EvidenceItems.AddRange(
                CreateEvidence(approvedEvidenceId, tenantAId, "Approved access review", EvidenceStatus.Approved),
                CreateEvidence(draftEvidenceId, tenantAId, "Draft screenshot", EvidenceStatus.Uploaded),
                CreateEvidence(tenantBEvidenceId, tenantBId, "Other tenant evidence", EvidenceStatus.Approved));
            dbContext.Set<ReportEvidenceEntity>().AddRange(
                new ReportEvidenceEntity { ReportId = tenantAReportId, EvidenceItemId = approvedEvidenceId },
                new ReportEvidenceEntity { ReportId = tenantAReportId, EvidenceItemId = draftEvidenceId },
                new ReportEvidenceEntity { ReportId = tenantBReportId, EvidenceItemId = tenantBEvidenceId });
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        var auditorUserId = Guid.Parse("24242424-2424-2424-2424-2424242424aa");

        using var packagesRequest = CreateRequest(
            HttpMethod.Get,
            "/api/reports/approved-evidence-packages",
            tenantAId,
            auditorUserId,
            RoleCatalog.Auditor);
        using var blockedInviteRequest = CreateRequest(
            HttpMethod.Post,
            "/api/tenant-invitations",
            new CreateTenantInvitationRequest("blocked@example.com", RoleCatalog.Contributor),
            tenantAId,
            auditorUserId,
            RoleCatalog.Auditor);
        using var blockedTenantUpdateRequest = CreateRequest(
            HttpMethod.Patch,
            $"/api/tenants/{tenantAId}/status",
            new UpdateTenantStatusRequest(TenantStatus.Suspended),
            tenantAId,
            auditorUserId,
            RoleCatalog.Auditor);
        using var blockedEvidenceApprovalRequest = CreateRequest(
            HttpMethod.Post,
            "/api/tenant-invitations/24242424-2424-2424-2424-2424242424d1/expire",
            tenantAId,
            auditorUserId,
            RoleCatalog.Auditor);
        using var blockedDeleteLikeRequest = CreateRequest(
            HttpMethod.Post,
            "/api/tenant-invitations/24242424-2424-2424-2424-2424242424d1/revoke",
            new RevokeTenantInvitationRequest("Unauthorized role test."),
            tenantAId,
            auditorUserId,
            RoleCatalog.Auditor);
        using var blockedAssignRequest = CreateRequest(
            HttpMethod.Post,
            "/api/tenant-members",
            new AssignTenantMemberRequest(Guid.NewGuid(), "blocked.assign@example.com", "Blocked Assign", RoleCatalog.Contributor),
            tenantAId,
            auditorUserId,
            RoleCatalog.Auditor);
        using var blockedComplianceReportRequest = CreateRequest(
            HttpMethod.Post,
            "/api/reports/compliance-status",
            tenantAId,
            auditorUserId,
            RoleCatalog.Auditor);
        using var blockedCmmcReportRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/reports/cmmc-readiness?assessmentId={Guid.NewGuid()}",
            tenantAId,
            auditorUserId,
            RoleCatalog.Auditor);
        using var blockedSubcontractorReportRequest = CreateRequest(
            HttpMethod.Post,
            "/api/reports/subcontractor-compliance",
            tenantAId,
            auditorUserId,
            RoleCatalog.Auditor);
        using var blockedEvidencePackageRequest = CreateRequest(
            HttpMethod.Post,
            "/api/reports/evidence-packages",
            tenantAId,
            auditorUserId,
            RoleCatalog.Auditor);

        var packagesResponse = await client.SendAsync(packagesRequest);
        var blockedInviteResponse = await client.SendAsync(blockedInviteRequest);
        var blockedTenantUpdateResponse = await client.SendAsync(blockedTenantUpdateRequest);
        var blockedEvidenceApprovalResponse = await client.SendAsync(blockedEvidenceApprovalRequest);
        var blockedDeleteLikeResponse = await client.SendAsync(blockedDeleteLikeRequest);
        var blockedAssignResponse = await client.SendAsync(blockedAssignRequest);
        var blockedComplianceReportResponse = await client.SendAsync(blockedComplianceReportRequest);
        var blockedCmmcReportResponse = await client.SendAsync(blockedCmmcReportRequest);
        var blockedSubcontractorReportResponse = await client.SendAsync(blockedSubcontractorReportRequest);
        var blockedEvidencePackageResponse = await client.SendAsync(blockedEvidencePackageRequest);
        var packages = await packagesResponse.Content.ReadFromJsonAsync<ApprovedEvidencePackageDto[]>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, packagesResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, blockedInviteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, blockedTenantUpdateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, blockedEvidenceApprovalResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, blockedDeleteLikeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, blockedAssignResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, blockedComplianceReportResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, blockedCmmcReportResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, blockedSubcontractorReportResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, blockedEvidencePackageResponse.StatusCode);
        await AssertStandardAuthorizationErrorAsync(blockedInviteResponse);
        await AssertStandardAuthorizationErrorAsync(blockedTenantUpdateResponse);
        await AssertStandardAuthorizationErrorAsync(blockedEvidenceApprovalResponse);
        await AssertStandardAuthorizationErrorAsync(blockedDeleteLikeResponse);
        await AssertStandardAuthorizationErrorAsync(blockedAssignResponse);
        await AssertStandardAuthorizationErrorAsync(blockedComplianceReportResponse);
        await AssertStandardAuthorizationErrorAsync(blockedCmmcReportResponse);
        await AssertStandardAuthorizationErrorAsync(blockedSubcontractorReportResponse);
        await AssertStandardAuthorizationErrorAsync(blockedEvidencePackageResponse);
        Assert.DoesNotContain(Permission.ManageEvidence, RoleCatalog.GetPermissions(RoleCatalog.Auditor));
        Assert.DoesNotContain(Permission.ApproveEvidence, RoleCatalog.GetPermissions(RoleCatalog.Auditor));
        Assert.DoesNotContain(Permission.ManageReports, RoleCatalog.GetPermissions(RoleCatalog.Auditor));
        Assert.DoesNotContain(Permission.ManageUsers, RoleCatalog.GetPermissions(RoleCatalog.Auditor));

        var package = Assert.Single(packages ?? []);
        Assert.Equal(tenantAReportId, package.ReportId);
        Assert.Equal(tenantAId, package.TenantId);
        Assert.Equal("Tenant A Approved Package", package.Title);
        var evidence = Assert.Single(package.EvidenceItems);
        Assert.Equal(approvedEvidenceId, evidence.EvidenceItemId);
        Assert.Equal(EvidenceStatus.Approved, evidence.Status);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.False(await dbContext.TenantInvitations.AnyAsync(candidate => candidate.TenantId == tenantAId));
        Assert.False(await dbContext.TenantMemberships.AnyAsync(candidate => candidate.TenantId == tenantAId));
        Assert.Single(await dbContext.Reports.Where(candidate => candidate.TenantId == tenantAId).ToArrayAsync());
        var auditEvents = await dbContext.AuditLogEntries
            .Where(candidate => candidate.TenantId == tenantAId)
            .ToListAsync();
        Assert.All(auditEvents, audit =>
        {
            Assert.Equal("Authorization", audit.EntityType);
            Assert.Equal(AuditAction.Rejected, audit.Action);
            Assert.Equal(auditorUserId, audit.ActorUserId);
        });
        Assert.DoesNotContain(auditEvents, audit => audit.EntityType is "TenantInvitation" or "TenantMembership");
    }

    [Theory]
    [InlineData(RoleCatalog.Auditor)]
    [InlineData(RoleCatalog.Contributor)]
    public async Task Read_only_report_roles_cannot_invoke_any_report_generation_endpoint(string roleName)
    {
        var tenantId = Guid.NewGuid();
        await using var factory = CreateFactory($"report-mutations-{roleName}", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, $"{roleName} report mutation tenant"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        var userId = Guid.NewGuid();
        var paths = new[]
        {
            "/api/reports/compliance-status",
            $"/api/reports/cmmc-readiness?assessmentId={Guid.NewGuid()}",
            "/api/reports/subcontractor-compliance",
            "/api/reports/evidence-packages"
        };

        foreach (var path in paths)
        {
            using var request = CreateRequest(HttpMethod.Post, path, tenantId, userId, roleName);
            using var response = await client.SendAsync(request);
            await AssertStandardAuthorizationErrorAsync(response);
        }

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Empty(await dbContext.Reports.Where(report => report.TenantId == tenantId).ToArrayAsync());
    }

    [Fact]
    public async Task UAT_02_Auditor_can_open_report_detail_but_cannot_archive_it()
    {
        var tenantId = Guid.NewGuid();
        var reportId = Guid.NewGuid();
        await using var factory = CreateFactory("uat-02-auditor-report-detail", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "UAT-02 Auditor tenant"));
            dbContext.Reports.Add(CreateComplianceStatusReport(reportId, tenantId));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        var auditorId = Guid.NewGuid();

        using var listRequest = CreateRequest(HttpMethod.Get, "/api/reports/recent", tenantId, auditorId, RoleCatalog.Auditor);
        using var listResponse = await client.SendAsync(listRequest);
        var reports = await listResponse.Content.ReadFromJsonAsync<ReportHistoryItemDto[]>(JsonOptions);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Equal(reportId, Assert.Single(reports ?? []).Id);

        using var detailRequest = CreateRequest(HttpMethod.Get, $"/api/reports/{reportId}", tenantId, auditorId, RoleCatalog.Auditor);
        using var detailResponse = await client.SendAsync(detailRequest);
        var detail = await detailResponse.Content.ReadFromJsonAsync<ReportArtifactDetailDto>(JsonOptions);
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        Assert.Equal(reportId, detail?.Id);
        Assert.Equal(1, detail?.Snapshot.GetProperty("totalObligations").GetInt32());

        using var archiveRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/reports/{reportId}/archive",
            new ReportLifecycleRequest("Auditor must not archive reports."),
            tenantId,
            auditorId,
            RoleCatalog.Auditor);
        using var archiveResponse = await client.SendAsync(archiveRequest);
        await AssertStandardAuthorizationErrorAsync(archiveResponse);

        using var exportRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/reports/{reportId}/exports/pdf",
            tenantId,
            auditorId,
            RoleCatalog.Auditor);
        using var exportResponse = await client.SendAsync(exportRequest);
        await AssertStandardAuthorizationErrorAsync(exportResponse);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(ReportStatus.Complete, (await dbContext.Reports.SingleAsync()).Status);
        Assert.DoesNotContain(
            await dbContext.AuditLogEntries.ToArrayAsync(),
            audit => audit.EntityType == "Report" && audit.EntityId == reportId.ToString());
    }

    [Fact]
    public async Task Owner_can_request_one_idempotent_tenant_scoped_pdf_export_while_other_roles_are_denied()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var reportId = Guid.NewGuid();
        await using var factory = CreateFactory("owner-report-pdf-export", dbContext =>
        {
            dbContext.Tenants.AddRange(
                CreateTenant(tenantId, "PDF export tenant"),
                CreateTenant(otherTenantId, "Other PDF export tenant"));
            dbContext.Reports.Add(CreateComplianceStatusReport(reportId, tenantId));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        var ownerId = Guid.NewGuid();

        using var firstRequest = CreateRequest(HttpMethod.Post, $"/api/reports/{reportId}/exports/pdf", tenantId, ownerId, RoleCatalog.Owner);
        using var firstResponse = await client.SendAsync(firstRequest);
        var first = await firstResponse.Content.ReadFromJsonAsync<ReportExportDto>(JsonOptions);
        Assert.Equal(HttpStatusCode.Accepted, firstResponse.StatusCode);
        Assert.Equal(ReportExportStatus.Queued, first?.Status);

        using var repeatedRequest = CreateRequest(HttpMethod.Post, $"/api/reports/{reportId}/exports/pdf", tenantId, ownerId, RoleCatalog.Owner);
        using var repeatedResponse = await client.SendAsync(repeatedRequest);
        var repeated = await repeatedResponse.Content.ReadFromJsonAsync<ReportExportDto>(JsonOptions);
        Assert.Equal(HttpStatusCode.Accepted, repeatedResponse.StatusCode);
        Assert.Equal(first?.Id, repeated?.Id);

        foreach (var role in new[] { RoleCatalog.Admin, RoleCatalog.ComplianceManager, RoleCatalog.Contributor, RoleCatalog.Auditor, RoleCatalog.Advisor })
        {
            using var deniedRequest = CreateRequest(HttpMethod.Post, $"/api/reports/{reportId}/exports/pdf", tenantId, Guid.NewGuid(), role);
            using var deniedResponse = await client.SendAsync(deniedRequest);
            await AssertStandardAuthorizationErrorAsync(deniedResponse);
        }

        using var crossTenantRequest = CreateRequest(HttpMethod.Post, $"/api/reports/{reportId}/exports/pdf", otherTenantId, Guid.NewGuid(), RoleCatalog.Owner);
        using var crossTenantResponse = await client.SendAsync(crossTenantRequest);
        Assert.Equal(HttpStatusCode.NotFound, crossTenantResponse.StatusCode);

        await using (var processingScope = factory.Services.CreateAsyncScope())
        {
            var repository = processingScope.ServiceProvider.GetRequiredService<IReportExportRepository>();
            var claimed = await repository.TryClaimNextAsync(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5), 3);
            Assert.NotNull(claimed);
            processingScope.ServiceProvider.GetRequiredService<HttpTenantContext>().InitializeBackground(
                claimed!.TenantId,
                claimed.RequestedByUserId,
                claimed.RequestedByEmail);
            var processed = await processingScope.ServiceProvider.GetRequiredService<ReportExportService>().ProcessAsync(claimed);
            Assert.Equal(ReportExportStatus.Ready, processed?.Status);
        }

        using var downloadRequest = CreateRequest(
            HttpMethod.Get,
            $"/api/report-exports/{first!.Id}/content",
            tenantId,
            ownerId,
            RoleCatalog.Owner);
        using var downloadResponse = await client.SendAsync(downloadRequest);
        var pdf = await downloadResponse.Content.ReadAsByteArrayAsync();
        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
        Assert.Equal("application/pdf", downloadResponse.Content.Headers.ContentType?.MediaType);
        Assert.StartsWith("%PDF-", System.Text.Encoding.ASCII.GetString(pdf, 0, Math.Min(pdf.Length, 5)));

        using var auditorDownloadRequest = CreateRequest(
            HttpMethod.Get,
            $"/api/report-exports/{first.Id}/content",
            tenantId,
            Guid.NewGuid(),
            RoleCatalog.Auditor);
        using var auditorDownloadResponse = await client.SendAsync(auditorDownloadRequest);
        await AssertStandardAuthorizationErrorAsync(auditorDownloadResponse);

        using var crossTenantDownloadRequest = CreateRequest(
            HttpMethod.Get,
            $"/api/report-exports/{first.Id}/content",
            otherTenantId,
            Guid.NewGuid(),
            RoleCatalog.Owner);
        using var crossTenantDownloadResponse = await client.SendAsync(crossTenantDownloadRequest);
        Assert.Equal(HttpStatusCode.NotFound, crossTenantDownloadResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var export = Assert.Single(await dbContext.ReportExports.ToArrayAsync());
        Assert.Equal(tenantId, export.TenantId);
        Assert.Equal(reportId, export.ReportId);
        Assert.Single(await dbContext.AuditLogEntries
            .Where(audit => audit.EntityType == "ReportExport" && audit.Action == AuditAction.Created)
            .ToArrayAsync());
        Assert.Single(await dbContext.AuditLogEntries
            .Where(audit => audit.EntityType == "ReportExport" && audit.Action == AuditAction.Exported)
            .ToArrayAsync());
        Assert.Single(await dbContext.AuditLogEntries
            .Where(audit => audit.EntityType == "ReportExport" && audit.Action == AuditAction.Downloaded)
            .ToArrayAsync());
    }

    [Fact]
    public async Task Admin_can_archive_and_restore_report_with_tenant_scoped_idempotent_audit_history()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var reportId = Guid.NewGuid();
        await using var factory = CreateFactory("report-archive-lifecycle", dbContext =>
        {
            dbContext.Tenants.AddRange(
                CreateTenant(tenantId, "Archive tenant"),
                CreateTenant(otherTenantId, "Other archive tenant"));
            dbContext.Reports.Add(CreateComplianceStatusReport(reportId, tenantId));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        var adminId = Guid.NewGuid();

        using var invalidRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/reports/{reportId}/archive",
            new ReportLifecycleRequest(" "),
            tenantId,
            adminId,
            RoleCatalog.Admin);
        using var invalidResponse = await client.SendAsync(invalidRequest);
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);

        using var crossTenantRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/reports/{reportId}/archive",
            new ReportLifecycleRequest("Must not cross tenant boundaries."),
            otherTenantId,
            adminId,
            RoleCatalog.Admin);
        using var crossTenantResponse = await client.SendAsync(crossTenantRequest);
        Assert.Equal(HttpStatusCode.NotFound, crossTenantResponse.StatusCode);

        using var archiveRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/reports/{reportId}/archive",
            new ReportLifecycleRequest("Superseded by a corrected immutable snapshot."),
            tenantId,
            adminId,
            RoleCatalog.Admin);
        using var archiveResponse = await client.SendAsync(archiveRequest);
        var archived = await archiveResponse.Content.ReadFromJsonAsync<ReportArtifactDetailDto>(JsonOptions);
        Assert.Equal(HttpStatusCode.OK, archiveResponse.StatusCode);
        Assert.Equal(ReportStatus.Archived, archived?.Status);
        Assert.Equal("Superseded by a corrected immutable snapshot.", archived?.ArchiveReason);
        Assert.Equal(adminId, archived?.ArchivedByUserId);
        Assert.NotNull(archived?.ArchivedAt);

        using var repeatedArchiveRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/reports/{reportId}/archive",
            new ReportLifecycleRequest("Repeated request must be idempotent."),
            tenantId,
            adminId,
            RoleCatalog.Admin);
        using var repeatedArchiveResponse = await client.SendAsync(repeatedArchiveRequest);
        Assert.Equal(HttpStatusCode.OK, repeatedArchiveResponse.StatusCode);

        using var restoreRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/reports/{reportId}/restore",
            new ReportLifecycleRequest("Original snapshot remains the authoritative artifact."),
            tenantId,
            adminId,
            RoleCatalog.Admin);
        using var restoreResponse = await client.SendAsync(restoreRequest);
        var restored = await restoreResponse.Content.ReadFromJsonAsync<ReportArtifactDetailDto>(JsonOptions);
        Assert.Equal(HttpStatusCode.OK, restoreResponse.StatusCode);
        Assert.Equal(ReportStatus.Complete, restored?.Status);
        Assert.Null(restored?.ArchivedAt);
        Assert.Null(restored?.ArchivedByUserId);
        Assert.Null(restored?.ArchiveReason);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var audits = await dbContext.AuditLogEntries
            .Where(audit => audit.EntityType == "Report" && audit.EntityId == reportId.ToString())
            .OrderBy(audit => audit.OccurredAt)
            .ToArrayAsync();
        Assert.Equal(2, audits.Length);
        Assert.Equal(AuditAction.Archived, audits[0].Action);
        Assert.Contains("Superseded by a corrected immutable snapshot.", audits[0].MetadataJson);
        Assert.Equal(AuditAction.Updated, audits[1].Action);
        Assert.Contains("Original snapshot remains the authoritative artifact.", audits[1].MetadataJson);
    }

    [Fact]
    public async Task Concurrent_archive_requests_produce_one_state_transition_and_one_audit_event()
    {
        var tenantId = Guid.NewGuid();
        var reportId = Guid.NewGuid();
        await using var factory = CreateFactory("report-archive-concurrency", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "Concurrent archive tenant"));
            dbContext.Reports.Add(CreateComplianceStatusReport(reportId, tenantId));
            dbContext.SaveChanges();
        });
        using var firstClient = factory.CreateClient();
        using var secondClient = factory.CreateClient();
        var adminId = Guid.NewGuid();
        using var firstRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/reports/{reportId}/archive",
            new ReportLifecycleRequest("First concurrent archive request."),
            tenantId,
            adminId,
            RoleCatalog.Admin);
        using var secondRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/reports/{reportId}/archive",
            new ReportLifecycleRequest("Second concurrent archive request."),
            tenantId,
            adminId,
            RoleCatalog.Admin);

        var responses = await Task.WhenAll(
            firstClient.SendAsync(firstRequest),
            secondClient.SendAsync(secondRequest));
        using var firstResponse = responses[0];
        using var secondResponse = responses[1];

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(ReportStatus.Archived, (await dbContext.Reports.SingleAsync()).Status);
        Assert.Single(await dbContext.AuditLogEntries
            .Where(audit =>
                audit.EntityType == "Report" &&
                audit.EntityId == reportId.ToString() &&
                audit.Action == AuditAction.Archived)
            .ToArrayAsync());
    }

    [Fact]
    public async Task Current_access_endpoint_returns_role_derived_permissions_for_ui_gating()
    {
        await using var factory = CreateFactory("tc-2-4-access");
        using var client = factory.CreateClient();
        using var request = CreateRequest(HttpMethod.Get, "/api/me/access", Guid.NewGuid(), Guid.NewGuid(), RoleCatalog.Auditor);

        var response = await client.SendAsync(request);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var permissions = payload.RootElement.GetProperty("permissions").EnumerateArray().Select(item => item.GetString()).ToArray();
        var roles = payload.RootElement.GetProperty("roles").EnumerateArray().Select(item => item.GetString()).ToArray();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(RoleCatalog.Auditor, roles);
        Assert.Contains(Permission.ViewReports.ToString(), permissions);
        Assert.Contains(Permission.ViewCmmc.ToString(), permissions);
        Assert.Contains(Permission.AuditorReadOnly.ToString(), permissions);
        Assert.DoesNotContain(Permission.ManageCmmc.ToString(), permissions);
        Assert.DoesNotContain(Permission.ManageReports.ToString(), permissions);
        Assert.DoesNotContain(Permission.ManageUsers.ToString(), permissions);
        Assert.True(payload.RootElement.GetProperty("rolePermissionMatrix").TryGetProperty(RoleCatalog.Owner, out _));
    }

    private static void AssertRoleHas(string roleName, params Permission[] permissions)
    {
        var rolePermissions = RoleCatalog.GetPermissions(roleName);
        Assert.All(permissions, permission => Assert.Contains(permission, rolePermissions));
    }

    private static void AssertRoleDoesNotHave(string roleName, params Permission[] permissions)
    {
        var rolePermissions = RoleCatalog.GetPermissions(roleName);
        Assert.All(permissions, permission => Assert.DoesNotContain(permission, rolePermissions));
    }

    private static async Task AssertStandardAuthorizationErrorAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();

        AssertStandardAuthorizationError(response, body);
    }

    private static void AssertStandardAuthorizationError(HttpResponseMessage response, string body)
    {
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Permission denied", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("permission_denied", body, StringComparison.OrdinalIgnoreCase);
    }

    private WebApplicationFactory<Program> CreateFactory(
        string databaseName,
        Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<TenantService>();
                services.AddScoped<ITenantRepository, EfTenantRepository>();
                services.AddScoped<TenantMembershipService>();
                services.AddScoped<ITenantMembershipRepository, EfTenantMembershipRepository>();
                services.AddScoped<TenantInvitationService>();
                services.AddScoped<ITenantInvitationRepository, EfTenantInvitationRepository>();
                services.AddScoped<IReportRepository, EfReportRepository>();
                services.AddScoped<IReportExportRepository, EfReportExportRepository>();
                services.AddSingleton<IObjectStorageService, TestObjectStorageService>();
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
                seed?.Invoke(dbContext);
            });
        });

    private static HttpRequestMessage CreateRequest<TContent>(
        HttpMethod method,
        string requestUri,
        TContent? content,
        Guid tenantId,
        Guid userId,
        string roleName)
    {
        var request = CreateRequest(method, requestUri, tenantId, userId, roleName);
        if (content is not null)
        {
            request.Content = JsonContent.Create(content, options: JsonOptions);
        }

        return request;
    }

    private static string Slugify(string roleName) =>
        roleName.Replace(" ", ".", StringComparison.Ordinal).ToLowerInvariant();

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        string requestUri,
        Guid tenantId,
        Guid userId,
        string roleName)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        request.Headers.Add("X-Gccs-Dev-Role", roleName);

        return request;
    }

    private static TenantEntity CreateTenant(Guid tenantId, string name) =>
        new()
        {
            Id = tenantId,
            Name = name,
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.Parse("2026-06-13T12:00:00Z")
        };

    private static ReportEntity CreateEvidencePackage(Guid reportId, Guid tenantId, string title) =>
        new()
        {
            Id = reportId,
            TenantId = tenantId,
            Type = ReportType.PrimeEvidencePackage,
            Title = title,
            Status = ReportStatus.Complete,
            GeneratedAt = DateTimeOffset.Parse("2026-06-13T12:00:00Z"),
            GeneratedByUserId = Guid.Parse("24242424-2424-2424-2424-242424242499"),
            CreatedAt = DateTimeOffset.Parse("2026-06-13T12:00:00Z")
        };

    private static ReportEntity CreateComplianceStatusReport(Guid reportId, Guid tenantId) =>
        new()
        {
            Id = reportId,
            TenantId = tenantId,
            Type = ReportType.ComplianceStatus,
            Title = "Compliance status report",
            Status = ReportStatus.Complete,
            GeneratedAt = DateTimeOffset.Parse("2026-07-28T12:00:00Z"),
            GeneratedByUserId = Guid.Parse("24242424-2424-2424-2424-242424242499"),
            SnapshotJson = """{"totalObligations":1}""",
            CreatedAt = DateTimeOffset.Parse("2026-07-28T12:00:00Z")
        };

    private static EvidenceItemEntity CreateEvidence(
        Guid evidenceId,
        Guid tenantId,
        string name,
        EvidenceStatus status) =>
        new()
        {
            Id = evidenceId,
            TenantId = tenantId,
            Name = name,
            Description = "Seeded for RBAC read-only tests.",
            Type = EvidenceType.AccessReview,
            Status = status,
            TagsJson = "[]",
            ApprovedAt = status == EvidenceStatus.Approved ? DateTimeOffset.Parse("2026-06-13T12:00:00Z") : null,
            ApprovedByUserId = status == EvidenceStatus.Approved ? Guid.Parse("24242424-2424-2424-2424-242424242498") : null,
            CreatedAt = DateTimeOffset.Parse("2026-06-13T12:00:00Z")
        };

    private sealed record EndpointPermissionCheck(
        string Area,
        HttpMethod Method,
        string Path,
        Permission RequiredPermission,
        HttpStatusCode AllowedStatus,
        Func<string, object>? BodyFactory)
    {
        public static EndpointPermissionCheck Get(
            string area,
            string path,
            Permission requiredPermission,
            HttpStatusCode allowedStatus) =>
            new(area, HttpMethod.Get, path, requiredPermission, allowedStatus, null);

        public static EndpointPermissionCheck Json(
            string area,
            HttpMethod method,
            string path,
            Permission requiredPermission,
            HttpStatusCode allowedStatus,
            Func<string, object> bodyFactory) =>
            new(area, method, path, requiredPermission, allowedStatus, bodyFactory);
    }
}
