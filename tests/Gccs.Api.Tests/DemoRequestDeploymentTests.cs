using System.Text.Json;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class DemoRequestDeploymentTests
{
    [Fact]
    public void Development_does_not_grant_platform_permissions_to_every_persona()
    {
        using var document = JsonDocument.Parse(ReadText("apps", "api", "appsettings.Development.json"));

        var permissions = document.RootElement
            .GetProperty("Security")
            .GetProperty("DevelopmentAuth")
            .GetProperty("DefaultPlatformPermissions")
            .GetString();

        Assert.True(string.IsNullOrEmpty(permissions));
    }

    [Theory]
    [InlineData("staging.yml", "STAGING_RESOURCE_GROUP")]
    [InlineData("production.yml", "PRODUCTION_RESOURCE_GROUP")]
    public void Deployment_fails_closed_and_configures_demo_delivery_before_api_deploy(string workflowName, string resourceGroupSetting)
    {
        var workflow = ReadText(".github", "workflows", workflowName);

        foreach (var signal in new[]
        {
            "DEMO_REQUESTS_ENDPOINT: ${{ vars.DEMO_REQUESTS_ENDPOINT }}",
            "DEMO_REQUESTS_SENDER_ADDRESS: ${{ vars.DEMO_REQUESTS_SENDER_ADDRESS }}",
            "DEMO_REQUESTS_RECIPIENT_ADDRESS: ${{ vars.DEMO_REQUESTS_RECIPIENT_ADDRESS }}",
            "test -n \"$DEMO_REQUESTS_ENDPOINT\"",
            "test -n \"$DEMO_REQUESTS_SENDER_ADDRESS\"",
            "test -n \"$DEMO_REQUESTS_RECIPIENT_ADDRESS\"",
            "DemoRequests__Enabled=true",
            "DemoRequests__UseManagedIdentity=true",
            "DemoRequests__Endpoint=\"$DEMO_REQUESTS_ENDPOINT\"",
            "DemoRequests__SenderAddress=\"$DEMO_REQUESTS_SENDER_ADDRESS\"",
            "DemoRequests__RecipientAddress=\"$DEMO_REQUESTS_RECIPIENT_ADDRESS\""
        })
        {
            Assert.Contains(signal, workflow);
        }

        Assert.Contains(resourceGroupSetting, workflow);
        Assert.True(
            workflow.IndexOf("Configure " + (workflowName == "staging.yml" ? "staging" : "production") + " demo-request delivery", StringComparison.Ordinal) <
            workflow.IndexOf("Deploy " + (workflowName == "staging.yml" ? "staging" : "production") + " API App Service", StringComparison.Ordinal),
            "Demo-request runtime configuration must be applied before the API starts.");
    }

    private static string ReadText(params string[] path)
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Gccs.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine([directory.FullName, .. path]));
    }
}
