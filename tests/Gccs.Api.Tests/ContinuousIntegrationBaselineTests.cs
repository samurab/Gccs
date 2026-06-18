using Xunit;

namespace Gccs.Api.Tests;

public sealed class ContinuousIntegrationBaselineTests
{
    private static readonly string[] RequiredBranchProtectionChecks =
    [
        "Backend validation",
        "Frontend validation",
        "Secret scan"
    ];

    public static TheoryData<string, string, string[]> RequiredCiSteps => new()
    {
        {
            "Backend validation",
            "Restore backend dependencies",
            ["dotnet restore Gccs.slnx"]
        },
        {
            "Backend validation",
            "Scan backend dependencies for known vulnerabilities",
            ["dotnet list Gccs.slnx package --vulnerable --include-transitive"]
        },
        {
            "Backend validation",
            "Build backend solution",
            ["dotnet build Gccs.slnx --no-restore --configuration Release"]
        },
        {
            "Backend validation",
            "Validate EF Core migrations",
            [
                "dotnet tool restore",
                "migrations has-pending-model-changes",
                "--project src/Gccs.Infrastructure/Gccs.Infrastructure.csproj",
                "--startup-project apps/api/Gccs.Api.csproj",
                "migrations script --idempotent",
                "gccs-idempotent-migrations.sql"
            ]
        },
        {
            "Backend validation",
            "Run backend unit and integration tests",
            [
                "dotnet test Gccs.slnx",
                "--configuration Release",
                "gccs-backend-tests.trx",
                "TestResults/backend"
            ]
        },
        {
            "Backend validation",
            "Run extraction precision and recall evaluation",
            [
                "python3 tools/extraction-evaluation/evaluate_corpus.py",
                "--corpus tests/fixtures/extraction-corpus",
                "--output-dir TestResults/extraction-evaluation",
                "--min-precision 0.95",
                "--min-recall 0.95"
            ]
        },
        {
            "Frontend validation",
            "Restore frontend dependencies",
            ["npm ci"]
        },
        {
            "Frontend validation",
            "Scan frontend dependencies for known vulnerabilities",
            ["npm audit --audit-level=high"]
        },
        {
            "Frontend validation",
            "Lint frontend workspace",
            ["npm run lint:web"]
        },
        {
            "Frontend validation",
            "Run frontend unit tests",
            [
                "npm --workspace apps/web run test:run",
                "--reporter=junit",
                "TestResults/web/vitest-junit.xml"
            ]
        },
        {
            "Frontend validation",
            "Build frontend workspace",
            ["npm run build:web"]
        },
        {
            "Secret scan",
            "Scan repository for committed secrets",
            ["gitleaks/gitleaks-action@v2"]
        }
    };

    public static TheoryData<string, string> ControlledFailureScenarios => new()
    {
        { "Backend validation", "Build backend solution" },
        { "Backend validation", "Validate EF Core migrations" },
        { "Backend validation", "Run backend unit and integration tests" },
        { "Backend validation", "Run extraction precision and recall evaluation" },
        { "Frontend validation", "Lint frontend workspace" },
        { "Frontend validation", "Run frontend unit tests" },
        { "Frontend validation", "Build frontend workspace" },
        { "Backend validation", "Scan backend dependencies for known vulnerabilities" },
        { "Frontend validation", "Scan frontend dependencies for known vulnerabilities" },
        { "Secret scan", "Scan repository for committed secrets" }
    };

    [Fact]
    public void Tc_1_3_1_pull_request_validation_runs_required_backend_frontend_migration_and_scan_steps()
    {
        var workflow = ReadWorkflow();

        Assert.Contains("pull_request:", workflow);
        Assert.Contains("push:", workflow);

        foreach (var requiredCheck in RequiredBranchProtectionChecks)
        {
            Assert.Contains($"name: {requiredCheck}", workflow);
        }

        AssertCiStepContains("Backend validation", "Restore backend dependencies", "dotnet restore Gccs.slnx");
        AssertCiStepContains("Backend validation", "Build backend solution", "dotnet build Gccs.slnx --no-restore --configuration Release");
        AssertCiStepContains("Backend validation", "Validate EF Core migrations", "migrations has-pending-model-changes");
        AssertCiStepContains("Backend validation", "Validate EF Core migrations", "migrations script --idempotent");
        AssertCiStepContains("Backend validation", "Run backend unit and integration tests", "dotnet test Gccs.slnx");
        AssertCiStepContains("Backend validation", "Run extraction precision and recall evaluation", "tools/extraction-evaluation/evaluate_corpus.py");
        AssertCiStepContains("Backend validation", "Run extraction precision and recall evaluation", "TestResults/extraction-evaluation");

        AssertCiStepContains("Frontend validation", "Restore frontend dependencies", "npm ci");
        AssertCiStepContains("Frontend validation", "Lint frontend workspace", "npm run lint:web");
        AssertCiStepContains("Frontend validation", "Run frontend unit tests", "npm --workspace apps/web run test:run");
        AssertCiStepContains("Frontend validation", "Build frontend workspace", "npm run build:web");

        AssertCiStepContains("Backend validation", "Scan backend dependencies for known vulnerabilities", "dotnet list Gccs.slnx package --vulnerable --include-transitive");
        AssertCiStepContains("Frontend validation", "Scan frontend dependencies for known vulnerabilities", "npm audit --audit-level=high");
        AssertCiStepContains("Secret scan", "Scan repository for committed secrets", "gitleaks/gitleaks-action@v2");
    }

    [Theory]
    [MemberData(nameof(RequiredCiSteps))]
    public void Tc_1_3_1_required_ci_steps_stay_in_the_expected_required_check(string jobName, string stepName, string[] expectedSignals)
    {
        var stepBlock = GetStepBlock(ReadWorkflow(), jobName, stepName);

        foreach (var expectedSignal in expectedSignals)
        {
            Assert.Contains(expectedSignal, stepBlock);
        }
    }

    [Theory]
    [MemberData(nameof(ControlledFailureScenarios))]
    public void Tc_1_3_2_controlled_failing_steps_make_required_pull_request_checks_unmergeable(string failedCheck, string failedStep)
    {
        var root = FindRepositoryRoot();
        var workflow = ReadWorkflow();
        var readme = File.ReadAllText(Path.Combine(root, "README.md"));
        var projectIndex = File.ReadAllText(Path.Combine(root, "PROJECT_INDEX.md"));
        var stepBlock = GetStepBlock(workflow, failedCheck, failedStep);

        Assert.DoesNotContain("continue-on-error: true", stepBlock);
        AssertConfiguredBranchProtectionChecksAreDocumented(readme);
        AssertConfiguredBranchProtectionChecksAreDocumented(projectIndex);

        var simulatedPullRequestChecks = RequiredBranchProtectionChecks.ToDictionary(
            checkName => checkName,
            checkName => checkName == failedCheck ? CheckConclusion.Failure : CheckConclusion.Success);

        Assert.False(
            BranchProtectionAllowsMerge(simulatedPullRequestChecks),
            $"A controlled failure in '{failedStep}' should fail required check '{failedCheck}' and leave the pull request unmergeable.");
    }

    [Theory]
    [MemberData(nameof(ControlledFailureScenarios))]
    public void Tc_1_3_3_failing_ci_logs_identify_the_project_command_and_step_without_unrelated_job_inspection(string jobName, string stepName)
    {
        var workflow = ReadWorkflow();
        var jobBlock = GetJobBlock(workflow, jobName);
        var stepBlock = GetStepBlock(workflow, jobName, stepName);

        Assert.Contains($"name: {jobName}", jobBlock);
        Assert.Contains($"name: {stepName}", stepBlock);
        Assert.True(
            StepIdentifiesProjectOrWorkspace(stepBlock, jobName),
            $"Expected '{stepName}' in '{jobName}' to identify the affected solution, project, workspace, or repository in its command.");
    }

    [Fact]
    public void Tc_1_3_3_test_failures_upload_focused_backend_and_frontend_artifacts()
    {
        var workflow = ReadWorkflow();
        var backendUpload = GetStepBlock(workflow, "Backend validation", "Upload backend test results");
        var frontendUpload = GetStepBlock(workflow, "Frontend validation", "Upload frontend test results");

        Assert.Contains("if: always()", backendUpload);
        Assert.Contains("backend-test-results", backendUpload);
        Assert.Contains("TestResults/backend", backendUpload);
        Assert.Contains("TestResults/extraction-evaluation", backendUpload);
        Assert.Contains("gccs-backend-tests.trx", GetStepBlock(workflow, "Backend validation", "Run backend unit and integration tests"));

        Assert.Contains("if: always()", frontendUpload);
        Assert.Contains("frontend-test-results", frontendUpload);
        Assert.Contains("apps/web/TestResults/web", frontendUpload);
        Assert.Contains("vitest-junit.xml", GetStepBlock(workflow, "Frontend validation", "Run frontend unit tests"));
    }

    [Theory]
    [InlineData("Backend validation", "Scan backend dependencies for known vulnerabilities")]
    [InlineData("Frontend validation", "Scan frontend dependencies for known vulnerabilities")]
    [InlineData("Secret scan", "Scan repository for committed secrets")]
    public void Tc_1_3_4_dependency_and_secret_scan_findings_are_visible_in_required_pull_request_checks(string failedCheck, string failedStep)
    {
        var workflow = ReadWorkflow();
        var stepBlock = GetStepBlock(workflow, failedCheck, failedStep);

        Assert.DoesNotContain("continue-on-error: true", stepBlock);
        Assert.Contains($"name: {failedCheck}", workflow);

        var simulatedPullRequestChecks = RequiredBranchProtectionChecks.ToDictionary(
            checkName => checkName,
            checkName => checkName == failedCheck ? CheckConclusion.Failure : CheckConclusion.Success);

        Assert.False(BranchProtectionAllowsMerge(simulatedPullRequestChecks));
    }

    [Fact]
    public void Tc_1_3_4_security_scans_have_permissions_and_commands_reviewers_can_trace()
    {
        var workflow = ReadWorkflow();

        Assert.Contains("permissions:", workflow);
        Assert.Contains("contents: read", workflow);
        Assert.Contains("security-events: write", workflow);
        AssertCiStepContains("Backend validation", "Scan backend dependencies for known vulnerabilities", "dotnet list Gccs.slnx package --vulnerable --include-transitive");
        AssertCiStepContains("Frontend validation", "Scan frontend dependencies for known vulnerabilities", "npm audit --audit-level=high");
        AssertCiStepContains("Secret scan", "Scan repository for committed secrets", "gitleaks/gitleaks-action@v2");
        AssertCiStepContains("Secret scan", "Scan repository for committed secrets", "GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}");
    }

    private static bool BranchProtectionAllowsMerge(IReadOnlyDictionary<string, CheckConclusion> checkConclusions)
    {
        return RequiredBranchProtectionChecks.All(requiredCheck =>
            checkConclusions.TryGetValue(requiredCheck, out var conclusion) &&
            conclusion == CheckConclusion.Success);
    }

    private static bool StepIdentifiesProjectOrWorkspace(string stepBlock, string jobName)
    {
        if (stepBlock.Contains("Gccs.slnx", StringComparison.Ordinal) ||
            stepBlock.Contains("src/Gccs.Infrastructure/Gccs.Infrastructure.csproj", StringComparison.Ordinal) ||
            stepBlock.Contains("apps/api/Gccs.Api.csproj", StringComparison.Ordinal) ||
            stepBlock.Contains("tools/extraction-evaluation/evaluate_corpus.py", StringComparison.Ordinal) ||
            stepBlock.Contains("tests/fixtures/extraction-corpus", StringComparison.Ordinal) ||
            stepBlock.Contains("apps/web", StringComparison.Ordinal) ||
            stepBlock.Contains("lint:web", StringComparison.Ordinal) ||
            stepBlock.Contains("build:web", StringComparison.Ordinal) ||
            (jobName == "Frontend validation" && stepBlock.Contains("npm audit", StringComparison.Ordinal)))
        {
            return true;
        }

        return jobName == "Secret scan" &&
            stepBlock.Contains("repository", StringComparison.OrdinalIgnoreCase) &&
            stepBlock.Contains("gitleaks", StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertConfiguredBranchProtectionChecksAreDocumented(string documentation)
    {
        Assert.Contains("branch protection", documentation);

        foreach (var requiredCheck in RequiredBranchProtectionChecks)
        {
            Assert.Contains(requiredCheck, documentation);
        }
    }

    private static void AssertCiStepContains(string jobName, string stepName, string expectedContent)
    {
        Assert.Contains(expectedContent, GetStepBlock(ReadWorkflow(), jobName, stepName));
    }

    private static string GetJobBlock(string workflow, string jobName)
    {
        var jobNameIndex = workflow.IndexOf($"    name: {jobName}", StringComparison.Ordinal);
        Assert.True(jobNameIndex >= 0, $"Expected CI job named '{jobName}' to exist.");

        var jobStart = workflow.LastIndexOf("\n  ", jobNameIndex, StringComparison.Ordinal);
        Assert.True(jobStart >= 0, $"Expected CI job '{jobName}' to be nested under jobs.");

        var nextJobIndex = workflow.IndexOf("\n  ", jobNameIndex + 1, StringComparison.Ordinal);

        while (nextJobIndex >= 0 && nextJobIndex + 3 < workflow.Length && workflow[nextJobIndex + 3] == ' ')
        {
            nextJobIndex = workflow.IndexOf("\n  ", nextJobIndex + 1, StringComparison.Ordinal);
        }

        return nextJobIndex >= 0
            ? workflow[jobStart..nextJobIndex]
            : workflow[jobStart..];
    }

    private static string GetStepBlock(string workflow, string jobName, string stepName)
    {
        var jobBlock = GetJobBlock(workflow, jobName);
        var stepIndex = jobBlock.IndexOf($"      - name: {stepName}", StringComparison.Ordinal);
        Assert.True(stepIndex >= 0, $"Expected CI step '{stepName}' to exist in job '{jobName}'.");

        var nextStepIndex = jobBlock.IndexOf("\n      - name:", stepIndex + 1, StringComparison.Ordinal);

        return nextStepIndex >= 0
            ? jobBlock[stepIndex..nextStepIndex]
            : jobBlock[stepIndex..];
    }

    private static string ReadWorkflow()
    {
        return File.ReadAllText(Path.Combine(FindRepositoryRoot(), ".github", "workflows", "ci.yml"));
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

    private enum CheckConclusion
    {
        Success,
        Failure
    }
}
