using Xunit;

namespace Gccs.Api.Tests;

public sealed class StagingEnvironmentTests
{
    [Fact]
    public void TC_17_3_1_Staging_deployment_workflow_covers_core_services_and_migrations()
    {
        var root = FindRepositoryRoot();
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "staging.yml"));
        var terraform = File.ReadAllText(Path.Combine(root, "infra", "terraform", "environments", "staging", "main.tf"));

        Assert.Contains("name: Staging deployment", workflow);
        Assert.Contains("environment:", workflow);
        Assert.Contains("name: staging", workflow);
        Assert.Contains("Build staging artifacts", workflow);
        Assert.Contains("dotnet publish apps/api/Gccs.Api.csproj", workflow);
        Assert.Contains("npm run build:web", workflow);
        Assert.Contains("Generate idempotent migration script", workflow);
        Assert.Contains("migrations script --idempotent", workflow);
        Assert.Contains("Deploy staging API, web, data, storage, cache, queue, and secrets", workflow);

        foreach (var serviceSignal in new[] { "api", "web", "database", "object_storage", "cache", "queue", "secrets" })
        {
            Assert.Contains(serviceSignal, terraform);
            Assert.Contains(serviceSignal, workflow);
        }
    }

    [Fact]
    public void TC_17_3_2_Staging_contract_blocks_production_customer_data_and_secrets()
    {
        var root = FindRepositoryRoot();
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "staging.yml"));
        var terraform = File.ReadAllText(Path.Combine(root, "infra", "terraform", "environments", "staging", "main.tf"));
        var runbook = File.ReadAllText(Path.Combine(root, "docs", "staging-environment.md"));

        Assert.Contains("STAGING_CUSTOMER_DATA_MODE: synthetic-only", workflow);
        Assert.Contains("Validate staging No-CUI and no production data guardrails", workflow);
        Assert.Contains("No-CUI / compliance management only", workflow);
        Assert.Contains("synthetic-only", terraform);
        Assert.Contains("must not contain production customer data", terraform);
        Assert.Contains("Staging contains no production customer data.", runbook);
        Assert.Contains("must not reuse production secrets", runbook);
        Assert.Contains("sanitized fixtures or synthetic seed data", runbook);
    }

    [Fact]
    public void TC_17_3_3_Staging_health_checks_cover_api_database_cache_storage_and_jobs()
    {
        var root = FindRepositoryRoot();
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "staging.yml"));
        var healthService = File.ReadAllText(Path.Combine(root, "apps", "api", "LocalDevelopment", "LocalDependencyHealthService.cs"));
        var runbook = File.ReadAllText(Path.Combine(root, "docs", "staging-environment.md"));

        Assert.Contains("/health", workflow);
        Assert.Contains("\"service\":\"gccs-api\"", workflow);
        Assert.Contains("\"name\":\"postgresql\"", workflow);
        Assert.Contains("\"name\":\"redis\"", workflow);
        Assert.Contains("\"name\":\"object-storage\"", workflow);
        Assert.Contains("\"name\":\"background-jobs\"", workflow);
        Assert.Contains("CheckBackgroundJobsAsync", healthService);
        Assert.Contains("Background job queue coordination is reachable through Redis.", healthService);

        foreach (var requiredSignal in new[] { "API status", "Database dependency", "Cache dependency", "Storage dependency", "Background job dependency" })
        {
            Assert.Contains(requiredSignal, runbook);
        }
    }

    [Fact]
    public void TC_17_3_4_Staging_smoke_tests_are_visible_in_ci_cd()
    {
        var root = FindRepositoryRoot();
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "staging.yml"));
        var runbook = File.ReadAllText(Path.Combine(root, "docs", "staging-environment.md"));

        Assert.Contains("Run staging smoke tests", workflow);
        Assert.Contains("curl --fail --show-error --silent \"$STAGING_API_BASE_URL/health\"", workflow);
        Assert.Contains("tee \"$RUNNER_TEMP/staging-health.json\"", workflow);
        Assert.Contains("Upload staging smoke test results", workflow);
        Assert.Contains("if: always()", workflow);
        Assert.Contains("staging-smoke-test-results", workflow);
        Assert.Contains("staging-health.json", workflow);
        Assert.Contains("success or failure is visible in CI/CD", runbook);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Gccs.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate repository root.");
    }
}
