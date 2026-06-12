using Xunit;

namespace Gccs.Api.Tests;

public sealed class RepositoryStructureTests
{
    private sealed record OwnershipBoundary(
        string Path,
        string ReadmeOwnerSignal,
        string ReadmeExclusionSignal,
        string ProjectIndexSignal);

    private static readonly string[] RequiredDirectories =
    [
        "apps/api",
        "apps/web",
        "src/Gccs.Domain",
        "src/Gccs.Application",
        "src/Gccs.Infrastructure",
        "packages/compliance-content",
        "docs",
        "infra"
    ];

    private static readonly string[] RequiredSolutionProjects =
    [
        "apps/api/Gccs.Api.csproj",
        "src/Gccs.Domain/Gccs.Domain.csproj",
        "src/Gccs.Application/Gccs.Application.csproj",
        "src/Gccs.Infrastructure/Gccs.Infrastructure.csproj",
        "tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj"
    ];

    private static readonly OwnershipBoundary[] RequiredOwnershipBoundaries =
    [
        new(
            "apps/api",
            "HTTP endpoints, authentication, tenant context, RBAC policies",
            "UI-only compliance logic",
            "HTTP boundary, local/prod auth configuration, tenant context, RBAC policies"),
        new(
            "apps/web",
            "React screens, route shell, UI states, client API calls",
            "Compliance applicability decisions, tenant authorization, RBAC enforcement",
            "Authenticated React workspace, UI composition, client API calls"),
        new(
            "src/Gccs.Domain",
            "Framework-independent entities, value objects, enums, and domain rules",
            "ASP.NET, EF Core, React, HTTP, database, or cloud SDK dependencies",
            "Framework-independent compliance model"),
        new(
            "src/Gccs.Application",
            "Use cases, DTOs, ports/interfaces, workflow orchestration",
            "Direct database, object storage, external API, or UI rendering code",
            "Use cases, DTOs, repository/storage ports"),
        new(
            "src/Gccs.Infrastructure",
            "EF Core persistence, migrations, repository adapters",
            "Customer-facing workflow decisions that belong in application/domain code",
            "EF Core schema, migrations, repository adapters"),
        new(
            "packages/compliance-content",
            "Source-backed obligation seed content with URLs",
            "Tenant-specific data or unreviewed customer-facing legal determinations",
            "Governed source-backed obligation package"),
        new(
            "docs",
            "Product, architecture, API, database, governance, and delivery documentation",
            "Runtime workflow behavior",
            "Product strategy, architecture, API contract"),
        new(
            "infra",
            "Local Docker services, generated SQL, and future IaC",
            "Application business logic or compliance content",
            "Local service composition, generated schema")
    ];

    [Fact]
    public void Tc_1_1_1_repository_keeps_required_project_boundaries_visible()
    {
        var root = FindRepositoryRoot();

        foreach (var directory in RequiredDirectories)
        {
            Assert.True(
                Directory.Exists(Path.Combine(root, directory)),
                $"Expected Story 1.1 directory '{directory}' to exist.");
        }

        var solution = File.ReadAllText(Path.Combine(root, "Gccs.slnx"));
        foreach (var projectPath in RequiredSolutionProjects)
        {
            Assert.Contains(projectPath, solution);
        }
    }

    [Fact]
    public void Tc_1_1_1_readme_and_docs_describe_ownership_boundaries()
    {
        var root = FindRepositoryRoot();
        var readme = File.ReadAllText(Path.Combine(root, "README.md"));
        var projectIndex = File.ReadAllText(Path.Combine(root, "PROJECT_INDEX.md"));
        var architecture = File.ReadAllText(Path.Combine(root, "docs", "architecture.md"));

        Assert.Contains("Ownership Boundaries", readme);
        Assert.Contains("## Ownership Boundaries", projectIndex);
        Assert.Contains("## Application Boundaries", architecture);

        foreach (var boundary in RequiredOwnershipBoundaries)
        {
            Assert.Contains($"`{boundary.Path}`", readme);
            Assert.Contains(boundary.ReadmeOwnerSignal, readme);
            Assert.Contains(boundary.ReadmeExclusionSignal, readme);

            Assert.Contains($"`{boundary.Path}`", projectIndex);
            Assert.Contains(boundary.ProjectIndexSignal, projectIndex);
        }
    }

    [Fact]
    public void Tc_1_1_2_developer_docs_match_clean_checkout_restore_build_and_test_commands()
    {
        var root = FindRepositoryRoot();
        var readme = File.ReadAllText(Path.Combine(root, "README.md"));
        var projectIndex = File.ReadAllText(Path.Combine(root, "PROJECT_INDEX.md"));
        var rootPackageJson = File.ReadAllText(Path.Combine(root, "package.json"));
        var webPackageJson = File.ReadAllText(Path.Combine(root, "apps", "web", "package.json"));

        foreach (var command in new[]
        {
            "dotnet restore Gccs.slnx",
            "dotnet build Gccs.slnx",
            "npm install",
            "npm run test:api",
            "npm run test:web",
            "npm run build:web",
            "npm test"
        })
        {
            Assert.Contains(command, readme);
            Assert.Contains(command, projectIndex);
        }

        Assert.Contains("\"restore:api\": \"dotnet restore Gccs.slnx\"", rootPackageJson);
        Assert.Contains("\"build:api\": \"dotnet build Gccs.slnx\"", rootPackageJson);
        Assert.Contains("\"test:api\": \"dotnet test Gccs.slnx\"", rootPackageJson);
        Assert.Contains("\"build:web\": \"npm --workspace apps/web run build\"", rootPackageJson);
        Assert.Contains("\"test:web\": \"npm --workspace apps/web run test:run\"", rootPackageJson);
        Assert.Contains("\"test\": \"npm run test:api && npm run test:web\"", rootPackageJson);
        Assert.Contains("\"build\": \"tsc -b && vite build\"", webPackageJson);
    }

    [Fact]
    public void Tc_1_1_3_implemented_compliance_workflows_are_backend_enforced_not_ui_only()
    {
        var root = FindRepositoryRoot();
        var program = File.ReadAllText(Path.Combine(root, "apps", "api", "Program.cs"));
        var apiSecurity = File.ReadAllText(Path.Combine(root, "apps", "api", "Security", "ApiSecurityExtensions.cs"));
        var overviewService = File.ReadAllText(Path.Combine(root, "src", "Gccs.Application", "Compliance", "ComplianceOverviewService.cs"));
        var obligationRepository = File.ReadAllText(Path.Combine(root, "src", "Gccs.Infrastructure", "Compliance", "InMemoryObligationRepository.cs"));
        var sourceReference = File.ReadAllText(Path.Combine(root, "src", "Gccs.Domain", "Compliance", "SourceReference.cs"));
        var securityTests = File.ReadAllText(Path.Combine(root, "tests", "Gccs.Api.Tests", "SecurityBoundaryTests.cs"));
        var projectIndex = File.ReadAllText(Path.Combine(root, "PROJECT_INDEX.md"));

        Assert.Contains(".RequireAuthorization()", program);
        Assert.Contains("ITenantContext tenantContext", program);
        Assert.Contains(".RequirePermission(Permission.AuditorReadOnly)", program);
        Assert.Contains("RequireClaim(PermissionClaimType, permission.ToString())", apiSecurity);
        Assert.Contains("TenantIdClaimType", apiSecurity);

        Assert.Contains("No-CUI / compliance management only", overviewService);
        Assert.Contains("obligation.SourceReference.Url", overviewService);
        Assert.Contains("https://www.acquisition.gov/far/52.204-21", obligationRepository);
        Assert.Contains("https://www.ecfr.gov/current/title-32/subtitle-A/chapter-I/subchapter-G/part-170", obligationRepository);
        Assert.Contains("LastReviewedAt", sourceReference);

        Assert.Contains("Api_routes_require_authentication", securityTests);
        Assert.Contains("Permission_policy_rejects_missing_permission", securityTests);
        Assert.Contains("Tenant scoping, RBAC, No-CUI policy enforcement, audit logging decisions", projectIndex);
        Assert.Contains("compliance workflow logic remains outside UI-only code", projectIndex);
    }

    [Fact]
    public void Tc_1_1_3_web_client_does_not_embed_source_backed_obligation_library_content()
    {
        var root = FindRepositoryRoot();
        var webSourceFiles = Directory.EnumerateFiles(Path.Combine(root, "apps/web/src"), "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".ts", StringComparison.Ordinal) || path.EndsWith(".tsx", StringComparison.Ordinal))
            .Where(path => !Path.GetFileName(path).Contains(".test.", StringComparison.Ordinal))
            .ToArray();

        var combinedWebSource = string.Join(Environment.NewLine, webSourceFiles.Select(File.ReadAllText));

        Assert.DoesNotContain("https://www.acquisition.gov", combinedWebSource);
        Assert.DoesNotContain("https://www.ecfr.gov", combinedWebSource);
        Assert.DoesNotContain("FAR 52.", combinedWebSource);
        Assert.DoesNotContain("DFARS 252.", combinedWebSource);
    }

    [Fact]
    public void Tc_1_1_4_developer_docs_explicitly_position_mvp_as_no_cui_compliance_management_only()
    {
        var root = FindRepositoryRoot();
        var docs = string.Join(
            Environment.NewLine,
            File.ReadAllText(Path.Combine(root, "README.md")),
            File.ReadAllText(Path.Combine(root, "PROJECT_INDEX.md")),
            File.ReadAllText(Path.Combine(root, "docs", "architecture.md")),
            File.ReadAllText(Path.Combine(root, "docs", "product-strategy.md")),
            File.ReadAllText(Path.Combine(root, "docs", "mvp-execution-plan.md")));

        Assert.Contains("No-CUI / compliance management only", docs);
        Assert.Contains("Do not add customer CUI storage", docs);
        Assert.Contains("prevented from uploading CUI", docs);
        Assert.Contains("classified data", docs);
        Assert.Contains("ITAR/export-controlled", docs);
        Assert.Contains("separate architecture, shared responsibility matrix, customer terms, support model, and assessment posture", docs);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Gccs.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing Gccs.slnx.");
    }
}
