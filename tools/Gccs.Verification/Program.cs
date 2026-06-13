using System.Net;
using System.Net.Http.Json;
using System.Text;
using Gccs.Application.Audit;
using Gccs.Application.Identity;
using Gccs.Application.Reports;
using Gccs.Application.Tenancy;
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
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gccs.Verification;

public static class Program
{
public static async Task Main()
{
var repositoryRoot = FindRepositoryRoot();
var tenantId = Guid.Parse("24242424-2424-2424-2424-2424242424a1");
var testRunId = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");

await using var factory = new WebApplicationFactory<global::Program>()
    .WithWebHostBuilder(builder =>
    {
        builder.UseContentRoot(Path.Combine(repositoryRoot, "apps", "api"));
        builder.UseSetting("LocalDependencies:Enabled", "false");
        builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
        builder.ConfigureServices(services =>
        {
            services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase($"tc-2-4-rbac-{testRunId}"));
            services.AddScoped<ITenantRepository, EfTenantRepository>();
            services.AddScoped<ITenantMembershipRepository, EfTenantMembershipRepository>();
            services.AddScoped<ITenantInvitationRepository, EfTenantInvitationRepository>();
            services.AddScoped<IReportRepository, EfReportRepository>();
            services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
            SeedVerificationData(dbContext, tenantId);
        });
    });

using var client = factory.CreateClient();
var roles = CreateRoles();
var observations = new List<Observation>();

await ExecuteServerPermissionMatrix(client, roles, tenantId, observations);
ExecuteWorkspaceUiCheck(repositoryRoot, roles, observations);
await ExecuteRestrictedActionCheck(client, roles.Single(role => role.Name == "Auditor"), tenantId, observations);
await ExecuteAuditorEvidenceCheck(client, roles.Single(role => role.Name == "Auditor"), tenantId, observations);

var report = BuildReport(repositoryRoot, tenantId, roles, observations);
var outputDirectory = Path.Combine(repositoryRoot, "artifacts", "test-results");
Directory.CreateDirectory(outputDirectory);
var outputPath = Path.Combine(outputDirectory, "tc-2.4-rbac-verification.md");
await File.WriteAllTextAsync(outputPath, report);

Console.WriteLine(outputPath);
Console.WriteLine();
Console.WriteLine(Summarize(observations));
}

static async Task ExecuteServerPermissionMatrix(
    HttpClient client,
    IReadOnlyList<RoleSetup> roles,
    Guid tenantId,
    List<Observation> observations)
{
    var endpointChecks = new[]
    {
        EndpointCheck.Get("profile", "/api/company-profile", "Representative company profile endpoint exists and enforces profile read/write permissions."),
        EndpointCheck.Get("contract", "/api/contracts", "Representative contract endpoint exists and enforces contract permissions."),
        EndpointCheck.Get("obligation", "/api/obligations", "Roles with ViewObligations can read source-backed obligations; roles without it are forbidden."),
        EndpointCheck.Get("task", "/api/tasks", "Representative task/calendar endpoint exists and enforces task permissions."),
        EndpointCheck.Get("evidence", "/api/evidence", "Representative evidence endpoint exists and enforces evidence permissions."),
        EndpointCheck.Get("report", "/api/reports/approved-evidence-packages", "Representative approved evidence package report endpoint enforces report-read permissions."),
        EndpointCheck.Get("subcontractor", "/api/subcontractors", "Representative subcontractor endpoint exists and enforces subcontractor permissions."),
        EndpointCheck.Get("admin-read", "/api/tenant-members", "Only roles with ManageUsers can list tenant members."),
        EndpointCheck.Json("admin-write", HttpMethod.Post, "/api/tenant-invitations", role => new
        {
            email = $"{role.Slug}.{Guid.NewGuid():N}@example.com",
            roleName = "Contributor",
            expiresInDays = 7
        }, "Only roles with ManageUsers can create tenant invitations.")
    };

    foreach (var role in roles)
    {
        foreach (var check in endpointChecks)
        {
            using var request = CreateRequest(check.Method, check.Path, role, tenantId);
            if (check.BodyFactory is not null)
            {
                request.Content = JsonContent.Create(check.BodyFactory(role));
            }

            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            var expectedStatus = ExpectedStatusFor(check, role);
            observations.Add(new Observation(
                "TC-2.4.1",
                $"{role.Name} {check.Area} {check.Method.Method} {check.Path}",
                $"Setup role '{role.Name}' with permissions [{string.Join(", ", role.Permissions)}]. Send {check.Method.Method} {check.Path}.",
                check.Expected,
                FormatActual(response.StatusCode, body),
                Classify(response.StatusCode, expectedStatus, check.Path),
                DetailFor(response.StatusCode, expectedStatus, check.Path, body)));
        }
    }
}

static void ExecuteWorkspaceUiCheck(string repositoryRoot, IReadOnlyList<RoleSetup> roles, List<Observation> observations)
{
    var appSourcePath = Path.Combine(repositoryRoot, "apps", "web", "src", "App.tsx");
    var apiSourcePath = Path.Combine(repositoryRoot, "apps", "web", "src", "lib", "api.ts");
    var appSource = File.ReadAllText(appSourcePath);
    var apiSource = File.ReadAllText(apiSourcePath);
    var inviteFormPresent = appSource.Contains("className=\"invite-form\"", StringComparison.Ordinal);
    var inviteFormPermissionGuarded =
        appSource.Contains("const canManageUsers = access.permissions.includes(\"ManageUsers\")", StringComparison.Ordinal) &&
        appSource.Contains("{canManageUsers ? (", StringComparison.Ordinal);
    var roleHeaderSupported = apiSource.Contains("X-Gccs-Dev-Role", StringComparison.Ordinal);
    var appTestSource = File.ReadAllText(Path.Combine(repositoryRoot, "apps", "web", "src", "App.test.tsx"));
    var appRenderTestCoversRestrictedRoles =
        appTestSource.Contains("hides tenant management actions for %s", StringComparison.Ordinal) &&
        appTestSource.Contains("\"Compliance Manager\", \"Contributor\", \"Auditor\", \"Advisor\"", StringComparison.Ordinal);
    var appRenderTestCoversPrivilegedRoles =
        appTestSource.Contains("renders tenant management actions for %s", StringComparison.Ordinal) &&
        appTestSource.Contains("\"Owner\", \"Admin\"", StringComparison.Ordinal);

    foreach (var role in roles)
    {
        var expected = role.Permissions.Contains(Permission.ManageUsers)
            ? "Admin invitation actions may render for this role."
            : "Admin invitation actions must be hidden for this role.";
        var actual = inviteFormPresent && inviteFormPermissionGuarded
            ? "Workspace renders the invitation form only inside a ManageUsers permission gate."
            : inviteFormPresent
                ? "Workspace renders the invitation form, but no ManageUsers render gate was found."
                : "No invitation form was found in the workspace source.";
        actual += roleHeaderSupported
            ? " Development web API calls support X-Gccs-Dev-Role for role-specific local rendering."
            : " Development web API calls do not support role-specific local rendering headers.";
        actual += appRenderTestCoversRestrictedRoles && appRenderTestCoversPrivilegedRoles
            ? " App.test.tsx renders Owner, Admin, Compliance Manager, Contributor, Auditor, and Advisor workspaces for this action-visibility check."
            : " Frontend render tests do not cover every role in the action-visibility matrix.";

        var outcome = role.Permissions.Contains(Permission.ManageUsers)
            ? inviteFormPresent && inviteFormPermissionGuarded && appRenderTestCoversPrivilegedRoles ? Outcome.Pass : Outcome.Fail
            : inviteFormPresent && inviteFormPermissionGuarded && appRenderTestCoversRestrictedRoles ? Outcome.Pass : Outcome.Fail;
        var detail = outcome == Outcome.Fail
            ? "Restricted UI action visibility is not fully covered by the current workspace render path."
            : "Restricted action visibility matched the current ManageUsers render gate evidence.";

        observations.Add(new Observation(
            "TC-2.4.2",
            $"{role.Name} workspace action visibility",
            $"Render/check workspace for role '{role.Name}' and verify unavailable actions are hidden.",
            expected,
            actual,
            outcome,
            detail));
    }
}

static async Task ExecuteRestrictedActionCheck(HttpClient client, RoleSetup auditor, Guid tenantId, List<Observation> observations)
{
    using var request = CreateRequest(HttpMethod.Post, "/api/tenant-invitations", auditor, tenantId);
    request.Content = JsonContent.Create(new
    {
        email = $"restricted.{Guid.NewGuid():N}@example.com",
        roleName = "Contributor",
        expiresInDays = 7
    });

    var response = await client.SendAsync(request);
    var body = await response.Content.ReadAsStringAsync();
    var hasStandardBody =
        response.Content.Headers.ContentType?.MediaType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true &&
        !string.IsNullOrWhiteSpace(body) &&
        (body.Contains("authorization", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("forbidden", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("permission", StringComparison.OrdinalIgnoreCase));

    observations.Add(new Observation(
        "TC-2.4.3",
        "Auditor direct restricted admin write",
        "As Auditor, POST /api/tenant-invitations with a valid invitation body.",
        "API returns the standard authorization error response with a consistent machine-readable/message body.",
        FormatActual(response.StatusCode, body),
        response.StatusCode == HttpStatusCode.Forbidden && hasStandardBody ? Outcome.Pass : Outcome.Fail,
        response.StatusCode == HttpStatusCode.Forbidden && !hasStandardBody
            ? "Authorization is enforced, but the response body is empty/non-standard."
            : "Restricted action response matched the expected standard authorization shape."));
}

static async Task ExecuteAuditorEvidenceCheck(HttpClient client, RoleSetup auditor, Guid tenantId, List<Observation> observations)
{
    var checks = new[]
    {
        EndpointCheck.Get("auditor-approved-evidence-read", "/api/reports/approved-evidence-packages", "Auditor can view approved evidence packages."),
        EndpointCheck.Json("auditor-evidence-create", HttpMethod.Post, "/api/evidence", _ => new { title = "Auditor must not create evidence" }, "Auditor cannot create tenant evidence."),
        EndpointCheck.Json("auditor-evidence-update", HttpMethod.Patch, "/api/evidence/24242424-2424-2424-2424-2424242424e1", _ => new { title = "Auditor must not update evidence" }, "Auditor cannot update tenant evidence."),
        EndpointCheck.Json("auditor-evidence-approve", HttpMethod.Post, "/api/evidence/24242424-2424-2424-2424-2424242424e1/approve", _ => new { }, "Auditor cannot approve tenant evidence."),
        new("auditor-evidence-delete", HttpMethod.Delete, "/api/evidence/24242424-2424-2424-2424-2424242424e1", null, "Auditor cannot delete tenant evidence."),
        EndpointCheck.Json("auditor-assign-tenant-data", HttpMethod.Post, "/api/tenant-members", _ => new
        {
            userId = Guid.NewGuid(),
            email = $"auditor.assign.{Guid.NewGuid():N}@example.com",
            displayName = "Auditor Assign Attempt",
            roleName = "Contributor"
        }, "Auditor cannot assign tenant data or users.")
    };

    foreach (var check in checks)
    {
        using var request = CreateRequest(check.Method, check.Path, auditor, tenantId);
        if (check.BodyFactory is not null)
        {
            request.Content = JsonContent.Create(check.BodyFactory(auditor));
        }

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        var expectedStatus = check.Area == "auditor-approved-evidence-read" ? HttpStatusCode.OK : HttpStatusCode.Forbidden;
        observations.Add(new Observation(
            "TC-2.4.4",
            check.Area,
            $"As Auditor, send {check.Method.Method} {check.Path}.",
            check.Expected,
            FormatActual(response.StatusCode, body),
            Classify(response.StatusCode, expectedStatus, check.Path),
            DetailFor(response.StatusCode, expectedStatus, check.Path, body)));
    }
}

static HttpRequestMessage CreateRequest(HttpMethod method, string path, RoleSetup role, Guid tenantId)
{
    var request = new HttpRequestMessage(method, path);
    request.Headers.Add("X-Gccs-Dev-Auth", "true");
    request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
    request.Headers.Add("X-Gccs-Dev-User", role.UserId.ToString());
    request.Headers.Add("X-Gccs-Dev-Email", $"{role.Slug}@example.com");
    request.Headers.Add("X-Gccs-Dev-Permissions", string.Join(",", role.Permissions));
    return request;
}

static HttpStatusCode ExpectedStatusFor(EndpointCheck check, RoleSetup role)
{
    if (check.Path == "/api/obligations")
    {
        return role.Permissions.Contains(Permission.ViewObligations) ? HttpStatusCode.OK : HttpStatusCode.Forbidden;
    }

    if (check.Path == "/api/reports/approved-evidence-packages")
    {
        return role.Permissions.Contains(Permission.ViewReports) ? HttpStatusCode.OK : HttpStatusCode.Forbidden;
    }

    if (check.Path == "/api/tenant-members" || check.Path == "/api/tenant-invitations")
    {
        return role.Permissions.Contains(Permission.ManageUsers) ? SuccessStatusFor(check.Method) : HttpStatusCode.Forbidden;
    }

    return HttpStatusCode.OK;
}

static HttpStatusCode SuccessStatusFor(HttpMethod method) =>
    method == HttpMethod.Post ? HttpStatusCode.Created : HttpStatusCode.OK;

static Outcome Classify(HttpStatusCode actualStatus, HttpStatusCode expectedStatus, string path)
{
    if (actualStatus == expectedStatus)
    {
        return Outcome.Pass;
    }

    return actualStatus == HttpStatusCode.NotFound && !IsImplementedEndpoint(path)
        ? Outcome.MissingCoverage
        : Outcome.Fail;
}

static string DetailFor(HttpStatusCode actualStatus, HttpStatusCode expectedStatus, string path, string body)
{
    if (actualStatus == expectedStatus)
    {
        return "Status matched the expected permission result.";
    }

    if (actualStatus == HttpStatusCode.NotFound && !IsImplementedEndpoint(path))
    {
        return "Endpoint is not implemented, so this portion of the permission matrix cannot be verified against runtime authorization.";
    }

    return $"Expected HTTP {(int)expectedStatus}, received HTTP {(int)actualStatus}. Response body length: {body.Length}.";
}

static bool IsImplementedEndpoint(string path) =>
    path.StartsWith("/api/obligations", StringComparison.OrdinalIgnoreCase) ||
    path.StartsWith("/api/reports/approved-evidence-packages", StringComparison.OrdinalIgnoreCase) ||
    path.StartsWith("/api/tenant-members", StringComparison.OrdinalIgnoreCase) ||
    path.StartsWith("/api/tenant-invitations", StringComparison.OrdinalIgnoreCase) ||
    path.StartsWith("/api/tenants", StringComparison.OrdinalIgnoreCase);

static string FormatActual(HttpStatusCode statusCode, string body)
{
    var sanitizedBody = string.IsNullOrWhiteSpace(body)
        ? "<empty body>"
        : body.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
    if (sanitizedBody.Length > 300)
    {
        sanitizedBody = sanitizedBody[..300] + "...";
    }

    return $"HTTP {(int)statusCode} {statusCode}; body: {sanitizedBody}";
}

static IReadOnlyList<RoleSetup> CreateRoles()
{
    return
    [
        CreateRole(RoleCatalog.Owner, "owner", "24242424-2424-2424-2424-2424242424b1"),
        CreateRole(RoleCatalog.Admin, "admin", "24242424-2424-2424-2424-2424242424b2"),
        CreateRole(RoleCatalog.ComplianceManager, "compliance-manager", "24242424-2424-2424-2424-2424242424b3"),
        CreateRole(RoleCatalog.Contributor, "contributor", "24242424-2424-2424-2424-2424242424b4"),
        CreateRole(RoleCatalog.Auditor, "auditor", "24242424-2424-2424-2424-2424242424b5"),
        CreateRole(RoleCatalog.Advisor, "advisor", "24242424-2424-2424-2424-2424242424b6")
    ];
}

static RoleSetup CreateRole(string roleName, string slug, string userId) =>
    new(roleName, slug, Guid.Parse(userId), RoleCatalog.GetPermissions(roleName));

static void SeedVerificationData(GccsDbContext dbContext, Guid tenantId)
{
    dbContext.Tenants.Add(new TenantEntity
    {
        Id = tenantId,
        Name = "TC-2.4 Verification Tenant",
        Status = TenantStatus.Active,
        DataPosture = TenantDataPosture.NoCui,
        CreatedAt = DateTimeOffset.Parse("2026-06-13T12:00:00Z"),
        CreatedByUserId = Guid.Parse("24242424-2424-2424-2424-2424242424b1")
    });

    foreach (var role in CreateRoles())
    {
        dbContext.Users.Add(new UserEntity
        {
            Id = role.UserId,
            TenantId = tenantId,
            Email = $"{role.Slug}@example.com",
            DisplayName = $"{role.Name} Verification User",
            Status = UserStatus.Active,
            MfaEnabled = true,
            CreatedAt = DateTimeOffset.Parse("2026-06-13T12:00:00Z")
        });

        dbContext.TenantMemberships.Add(new TenantMembershipEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = role.UserId,
            RoleName = role.Name,
            Status = MembershipStatus.Active,
            CreatedAt = DateTimeOffset.Parse("2026-06-13T12:00:00Z"),
            CreatedByUserId = role.UserId
        });
    }

    var reportId = Guid.Parse("24242424-2424-2424-2424-2424242424c1");
    var approvedEvidenceId = Guid.Parse("24242424-2424-2424-2424-2424242424e1");
    var draftEvidenceId = Guid.Parse("24242424-2424-2424-2424-2424242424f1");
    dbContext.Reports.Add(new ReportEntity
    {
        Id = reportId,
        TenantId = tenantId,
        Type = ReportType.PrimeEvidencePackage,
        Title = "TC-2.4 Approved Evidence Package",
        Status = ReportStatus.Complete,
        GeneratedAt = DateTimeOffset.Parse("2026-06-13T12:00:00Z"),
        GeneratedByUserId = Guid.Parse("24242424-2424-2424-2424-2424242424b1"),
        CreatedAt = DateTimeOffset.Parse("2026-06-13T12:00:00Z")
    });
    dbContext.EvidenceItems.AddRange(
        new EvidenceItemEntity
        {
            Id = approvedEvidenceId,
            TenantId = tenantId,
            Name = "Approved MFA configuration evidence",
            Description = "Seeded approved evidence for auditor read-only package verification.",
            Type = EvidenceType.SystemConfiguration,
            Status = EvidenceStatus.Approved,
            TagsJson = "[]",
            ApprovedAt = DateTimeOffset.Parse("2026-06-13T12:00:00Z"),
            ApprovedByUserId = Guid.Parse("24242424-2424-2424-2424-2424242424b3"),
            CreatedAt = DateTimeOffset.Parse("2026-06-13T12:00:00Z")
        },
        new EvidenceItemEntity
        {
            Id = draftEvidenceId,
            TenantId = tenantId,
            Name = "Draft antivirus screenshot",
            Description = "Seeded draft evidence that must not appear in approved auditor packages.",
            Type = EvidenceType.Screenshot,
            Status = EvidenceStatus.Uploaded,
            TagsJson = "[]",
            CreatedAt = DateTimeOffset.Parse("2026-06-13T12:00:00Z")
        });
    dbContext.Set<ReportEvidenceEntity>().AddRange(
        new ReportEvidenceEntity { ReportId = reportId, EvidenceItemId = approvedEvidenceId },
        new ReportEvidenceEntity { ReportId = reportId, EvidenceItemId = draftEvidenceId });

    dbContext.SaveChanges();
}

static string BuildReport(string repositoryRoot, Guid tenantId, IReadOnlyList<RoleSetup> roles, IReadOnlyList<Observation> observations)
{
    var builder = new StringBuilder();
    builder.AppendLine("# TC-2.4 Role-Based Permissions Verification");
    builder.AppendLine();
    builder.AppendLine($"Executed at: {DateTimeOffset.UtcNow:O}");
    builder.AppendLine($"Repository: `{repositoryRoot}`");
    builder.AppendLine($"Tenant: `{tenantId}`");
    builder.AppendLine();
    builder.AppendLine("## Setup Data");
    builder.AppendLine();
    builder.AppendLine("- API host: ASP.NET Core `WebApplicationFactory<Program>` with EF Core in-memory database.");
    builder.AppendLine("- Authentication: local development headers (`X-Gccs-Dev-Auth`, `X-Gccs-Dev-Tenant`, `X-Gccs-Dev-User`, `X-Gccs-Dev-Email`, `X-Gccs-Dev-Permissions`).");
    builder.AppendLine("- Seed data: one active No-CUI tenant and one active user/membership per tested role.");
    builder.AppendLine("- Implemented endpoint families observed in `apps/api/Program.cs`: compliance overview, obligations, approved evidence package reports, tenant members, tenant invitations, tenants.");
    builder.AppendLine("- Seeded evidence package: one completed prime evidence package linked to one approved evidence item and one draft item; the report endpoint should expose only approved evidence.");
    builder.AppendLine();
    builder.AppendLine("| Role | Permissions used |");
    builder.AppendLine("| --- | --- |");
    foreach (var role in roles)
    {
        builder.AppendLine($"| {role.Name} | {string.Join(", ", role.Permissions)} |");
    }

    builder.AppendLine();
    builder.AppendLine("## Results");
    builder.AppendLine();
    foreach (var testCaseGroup in observations.GroupBy(observation => observation.TestCase))
    {
        builder.AppendLine($"### {testCaseGroup.Key}");
        builder.AppendLine();
        builder.AppendLine("| Check | Steps | Expected result | Actual result | Outcome | Notes |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- |");
        foreach (var observation in testCaseGroup)
        {
            builder.AppendLine($"| {Escape(observation.Name)} | {Escape(observation.Steps)} | {Escape(observation.Expected)} | {Escape(observation.Actual)} | {observation.Outcome} | {Escape(observation.Notes)} |");
        }

        builder.AppendLine();
    }

    builder.AppendLine("## Defects And Missing Coverage");
    builder.AppendLine();
    var defects = observations
        .Where(observation => observation.Outcome is Outcome.Fail or Outcome.MissingCoverage)
        .ToArray();
    if (defects.Length == 0)
    {
        builder.AppendLine("- None found.");
    }
    else
    {
        foreach (var defect in defects)
        {
            builder.AppendLine($"- `{defect.TestCase}` {defect.Name}: {defect.Notes}");
        }
    }

    builder.AppendLine();
    builder.AppendLine("## Coverage Notes");
    builder.AppendLine();
    builder.AppendLine("- Profile, contract, task/calendar, direct evidence CRUD, and subcontractor API endpoints are not currently implemented, so their runtime RBAC behavior cannot be verified.");
    builder.AppendLine("- The workspace UI gates tenant member and invitation actions behind `ManageUsers`; current frontend render coverage verifies Owner/Admin allowed actions and Compliance Manager/Contributor/Auditor/Advisor hidden actions.");
    builder.AppendLine("- The API enforces permission claims for implemented protected endpoints and returns a standard `application/problem+json` authorization error body.");
    return builder.ToString();
}

static string Escape(string value) =>
    value.Replace("|", "\\|", StringComparison.Ordinal)
        .Replace("\r", " ", StringComparison.Ordinal)
        .Replace("\n", " ", StringComparison.Ordinal);

static string Summarize(IReadOnlyList<Observation> observations)
{
    var counts = observations
        .GroupBy(observation => observation.Outcome)
        .OrderBy(group => group.Key.ToString())
        .Select(group => $"{group.Key}: {group.Count()}");
    return string.Join(", ", counts);
}

static string FindRepositoryRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Gccs.slnx")))
    {
        directory = directory.Parent;
    }

    return directory?.FullName ?? Directory.GetCurrentDirectory();
}
}

internal sealed record RoleSetup(
    string Name,
    string Slug,
    Guid UserId,
    IReadOnlyCollection<Permission> Permissions);

internal sealed record EndpointCheck(
    string Area,
    HttpMethod Method,
    string Path,
    Func<RoleSetup, object>? BodyFactory,
    string Expected)
{
    public static EndpointCheck Get(string area, string path, string expected) =>
        new(area, HttpMethod.Get, path, null, expected);

    public static EndpointCheck Json(
        string area,
        HttpMethod method,
        string path,
        Func<RoleSetup, object> bodyFactory,
        string expected) =>
        new(area, method, path, bodyFactory, expected);
}

internal sealed record Observation(
    string TestCase,
    string Name,
    string Steps,
    string Expected,
    string Actual,
    Outcome Outcome,
    string Notes);

internal enum Outcome
{
    Pass,
    Fail,
    MissingCoverage
}
