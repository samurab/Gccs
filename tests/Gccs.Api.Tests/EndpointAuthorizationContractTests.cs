using Gccs.Api.Security;
using Gccs.Domain.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class EndpointAuthorizationContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EndpointAuthorizationContractTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
        });
    }

    [Fact]
    public void Report_endpoint_inventory_matches_the_executable_authorization_contract()
    {
        using var client = _factory.CreateClient();
        var actual = _factory.Services
            .GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .SelectMany(endpoint =>
            {
                var methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [];
                var permission = endpoint.Metadata.GetMetadata<RequiredPermissionMetadata>()?.Permission;
                return methods.Select(method => new EndpointContract(
                    method,
                    endpoint.RoutePattern.RawText ?? string.Empty,
                    endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName ?? string.Empty,
                    permission));
            })
            .Where(endpoint =>
                endpoint.Route.StartsWith("/api/reports", StringComparison.Ordinal) ||
                endpoint.Route.StartsWith("/api/report-exports", StringComparison.Ordinal))
            .OrderBy(endpoint => endpoint.Method)
            .ThenBy(endpoint => endpoint.Route)
            .ToArray();

        EndpointContract[] expected =
        [
            new("GET", "/api/reports/approved-evidence-packages", "ListApprovedEvidencePackages", Permission.ViewReports),
            new("GET", "/api/reports/evidence-packages/{reportId:guid}", "GetEvidencePackage", Permission.ViewReports),
            new("GET", "/api/reports/exports/{reportType}", "ExportSimpleReportCsv", Permission.ViewReports),
            new("GET", "/api/reports/recent", "ListRecentReports", Permission.ViewReports),
            new("GET", "/api/reports/{reportId:guid}", "GetReportArtifact", Permission.ViewReports),
            new("GET", "/api/report-exports/{exportId:guid}", "GetReportPdfExport", Permission.ExportReports),
            new("GET", "/api/report-exports/{exportId:guid}/content", "DownloadReportPdfExport", Permission.ExportReports),
            new("POST", "/api/reports/cmmc-readiness", "GenerateCmmcReadinessReport", Permission.ManageReports),
            new("POST", "/api/reports/compliance-status", "GenerateComplianceStatusReport", Permission.ManageReports),
            new("POST", "/api/reports/evidence-packages", "GenerateEvidencePackage", Permission.ManageReports),
            new("POST", "/api/reports/subcontractor-compliance", "GenerateSubcontractorComplianceReport", Permission.ManageReports),
            new("POST", "/api/reports/{reportId:guid}/archive", "ArchiveReport", Permission.ArchiveReports),
            new("POST", "/api/reports/{reportId:guid}/exports/pdf", "RequestReportPdfExport", Permission.ExportReports),
            new("POST", "/api/reports/{reportId:guid}/restore", "RestoreReport", Permission.ArchiveReports)
        ];

        Assert.Equal(
            expected.OrderBy(endpoint => endpoint.Method).ThenBy(endpoint => endpoint.Route),
            actual);
    }

    private sealed record EndpointContract(
        string Method,
        string Route,
        string Name,
        Permission? Permission);
}
