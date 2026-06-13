using Xunit;

namespace Gccs.Api.Tests;

public sealed class ContinuousIntegrationBaselineTests
{
    [Fact]
    public void Tc_1_3_1_pull_request_validation_runs_required_backend_frontend_migration_and_scan_steps()
    {
        var root = FindRepositoryRoot();
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "ci.yml"));

        Assert.Contains("pull_request:", workflow);
        Assert.Contains("Restore backend dependencies", workflow);
        Assert.Contains("dotnet restore Gccs.slnx", workflow);
        Assert.Contains("Scan backend dependencies for known vulnerabilities", workflow);
        Assert.Contains("dotnet list Gccs.slnx package --vulnerable --include-transitive", workflow);
        Assert.Contains("Build backend solution", workflow);
        Assert.Contains("dotnet build Gccs.slnx --no-restore --configuration Release", workflow);
        Assert.Contains("Validate EF Core migrations", workflow);
        Assert.Contains("migrations has-pending-model-changes", workflow);
        Assert.Contains("migrations script --idempotent", workflow);
        Assert.Contains("Run backend unit and integration tests", workflow);
        Assert.Contains("dotnet test Gccs.slnx", workflow);

        Assert.Contains("Restore frontend dependencies", workflow);
        Assert.Contains("npm ci", workflow);
        Assert.Contains("Scan frontend dependencies for known vulnerabilities", workflow);
        Assert.Contains("npm audit --audit-level=high", workflow);
        Assert.Contains("Lint frontend workspace", workflow);
        Assert.Contains("npm run lint:web", workflow);
        Assert.Contains("Run frontend unit tests", workflow);
        Assert.Contains("npm --workspace apps/web run test:run", workflow);
        Assert.Contains("Build frontend workspace", workflow);
        Assert.Contains("npm run build:web", workflow);

        Assert.Contains("Secret scan", workflow);
        Assert.Contains("gitleaks/gitleaks-action", workflow);
    }

    [Fact]
    public void Tc_1_3_2_failing_validation_steps_are_blocking_required_job_steps()
    {
        var root = FindRepositoryRoot();
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "ci.yml"));

        foreach (var requiredStep in new[]
        {
            "Restore backend dependencies",
            "Scan backend dependencies for known vulnerabilities",
            "Build backend solution",
            "Validate EF Core migrations",
            "Run backend unit and integration tests",
            "Restore frontend dependencies",
            "Scan frontend dependencies for known vulnerabilities",
            "Lint frontend workspace",
            "Run frontend unit tests",
            "Build frontend workspace",
            "Scan repository for committed secrets"
        })
        {
            var stepIndex = workflow.IndexOf($"name: {requiredStep}", StringComparison.Ordinal);
            Assert.True(stepIndex >= 0, $"Expected CI step '{requiredStep}' to exist.");

            var nextStepIndex = workflow.IndexOf("\n      - name:", stepIndex + 1, StringComparison.Ordinal);
            var stepBlock = nextStepIndex >= 0
                ? workflow[stepIndex..nextStepIndex]
                : workflow[stepIndex..];

            Assert.DoesNotContain("continue-on-error: true", stepBlock);
        }
    }

    [Fact]
    public void Tc_1_3_3_ci_logs_and_artifacts_identify_the_failing_project_and_step()
    {
        var root = FindRepositoryRoot();
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "ci.yml"));

        Assert.Contains("name: Backend validation", workflow);
        Assert.Contains("name: Frontend validation", workflow);
        Assert.Contains("name: Secret scan", workflow);
        Assert.Contains("Build backend solution", workflow);
        Assert.Contains("Build frontend workspace", workflow);
        Assert.Contains("Lint frontend workspace", workflow);
        Assert.Contains("Validate EF Core migrations", workflow);
        Assert.Contains("Upload backend test results", workflow);
        Assert.Contains("backend-test-results", workflow);
        Assert.Contains("Upload frontend test results", workflow);
        Assert.Contains("frontend-test-results", workflow);
        Assert.Contains("gccs-backend-tests.trx", workflow);
        Assert.Contains("vitest-junit.xml", workflow);
    }

    [Fact]
    public void Tc_1_3_4_security_scan_failures_are_visible_to_reviewers()
    {
        var root = FindRepositoryRoot();
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "ci.yml"));
        var readme = File.ReadAllText(Path.Combine(root, "README.md"));
        var projectIndex = File.ReadAllText(Path.Combine(root, "PROJECT_INDEX.md"));

        Assert.Contains("permissions:", workflow);
        Assert.Contains("security-events: write", workflow);
        Assert.Contains("dotnet list Gccs.slnx package --vulnerable --include-transitive", workflow);
        Assert.Contains("npm audit --audit-level=high", workflow);
        Assert.Contains("gitleaks/gitleaks-action", workflow);

        foreach (var documentation in new[] { readme, projectIndex })
        {
            Assert.Contains("dependency vulnerability scans", documentation);
            Assert.Contains("secret scanning", documentation);
            Assert.Contains("branch protection", documentation);
        }
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
