using Xunit;

namespace Gccs.Api.Tests;

public sealed class TaskApiOperationalDependencyTests
{
    [Fact]
    public void Deployment_and_observation_workflows_enforce_cursor_key_and_measured_retirement()
    {
        var root = FindRepositoryRoot();
        var production = File.ReadAllText(Path.Combine(root, ".github", "workflows", "production.yml"));
        var staging = File.ReadAllText(Path.Combine(root, ".github", "workflows", "staging.yml"));
        var observation = File.ReadAllText(Path.Combine(root, ".github", "workflows", "task-api-compatibility-observation.yml"));
        var predeployIndex = File.ReadAllText(Path.Combine(root, "infra", "database", "predeploy-compliance-task-search-index.sql"));

        foreach (var workflow in new[] { production, staging })
        {
            Assert.Contains("TASK_SEARCH_CURSOR_SIGNING_KEY", workflow, StringComparison.Ordinal);
            Assert.Contains("TaskSearch__CursorSigningKey", workflow, StringComparison.Ordinal);
            Assert.Contains("base64 --decode", workflow, StringComparison.Ordinal);
        }

        Assert.Contains("APPLICATIONINSIGHTS_CONNECTION_STRING", production, StringComparison.Ordinal);
        Assert.Contains("ago(30d)", observation, StringComparison.Ordinal);
        Assert.Contains("collectorCoverageBuckets", observation, StringComparison.Ordinal);
        Assert.Contains("requiredCoverageBuckets:55", observation, StringComparison.Ordinal);
        Assert.Contains("test \"$collector_coverage_buckets\" -ge 55", observation, StringComparison.Ordinal);
        Assert.Contains("test \"$legacy_usage_count\" -eq 0", observation, StringComparison.Ordinal);
        Assert.Contains("containsCui:false", observation, StringComparison.Ordinal);
        Assert.Contains("to_regclass('gccs.compliance_tasks')", predeployIndex, StringComparison.Ordinal);
        Assert.Contains("\\if :compliance_tasks_exists", predeployIndex, StringComparison.Ordinal);
        Assert.Contains("\\else", predeployIndex, StringComparison.Ordinal);
        Assert.Contains("\\endif", predeployIndex, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Gccs.slnx")))
        {
            current = current.Parent;
        }

        return current?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
    }
}
