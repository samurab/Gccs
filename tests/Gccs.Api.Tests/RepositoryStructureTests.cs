using Xunit;

namespace Gccs.Api.Tests;

public sealed class RepositoryStructureTests
{
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

    [Fact]
    public void Repository_keeps_story_1_1_project_boundaries_visible()
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
    public void Developer_docs_explain_no_cui_posture_and_backend_workflow_ownership()
    {
        var root = FindRepositoryRoot();
        var readme = File.ReadAllText(Path.Combine(root, "README.md"));
        var projectIndex = File.ReadAllText(Path.Combine(root, "PROJECT_INDEX.md"));

        Assert.Contains("No-CUI / compliance management only", readme);
        Assert.Contains("Compliance workflow logic must be reusable from backend services and tests.", readme);
        Assert.Contains("Ownership Boundaries", readme);
        Assert.Contains("compliance workflow logic remains outside UI-only code", projectIndex);
        Assert.Contains("Tenant scoping, RBAC, No-CUI policy enforcement, audit logging decisions", projectIndex);
    }

    [Fact]
    public void Web_client_does_not_embed_source_backed_obligation_library_content()
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
