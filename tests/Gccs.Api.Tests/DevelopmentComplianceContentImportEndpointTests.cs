using System.Net;
using System.Net.Http.Json;
using Gccs.Application.Compliance;
using Gccs.Domain.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class DevelopmentComplianceContentImportEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DevelopmentComplianceContentImportEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IComplianceContentImporter>();
                services.AddSingleton<IComplianceContentImporter, CapturingComplianceContentImporter>();
            });
        });
    }

    [Fact]
    public async Task Development_import_endpoint_runs_compliance_content_importer()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/dev/compliance-content/import");
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Permissions", Permission.ManageObligations.ToString());

        var response = await client.SendAsync(request);
        var report = await response.Content.ReadFromJsonAsync<ComplianceContentImportReport>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(report);
        Assert.True(report.Succeeded);
        Assert.Contains("packages/compliance-content", CapturingComplianceContentImporter.LastPackageRoot);
    }

    private sealed class CapturingComplianceContentImporter : IComplianceContentImporter
    {
        public static string LastPackageRoot { get; private set; } = string.Empty;

        public Task<ComplianceContentImportReport> ImportDirectoryAsync(
            string packageRoot,
            CancellationToken cancellationToken = default)
        {
            LastPackageRoot = packageRoot.Replace('\\', '/');
            return Task.FromResult(new ComplianceContentImportReport(
                true,
                1,
                0,
                1,
                0,
                1,
                0,
                1,
                [],
                ["Imported test content."]));
        }

        public Task<ComplianceContentImportReport> ImportFileAsync(
            string filePath,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The development import endpoint imports a package directory.");
    }
}
