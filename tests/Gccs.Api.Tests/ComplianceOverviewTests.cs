using System.Net;
using System.Net.Http.Json;
using Gccs.Application.Compliance;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ComplianceOverviewTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ComplianceOverviewTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
        });
    }

    [Fact]
    public async Task Overview_returns_source_backed_mvp_modules_and_priority_obligations()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/compliance/overview");
        request.Headers.Add("X-Gccs-Dev-Auth", "true");

        var response = await client.SendAsync(request);
        var overview = await response.Content.ReadFromJsonAsync<ComplianceOverviewDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(overview);
        Assert.Equal("No-CUI / compliance management only", overview.MvpDataPosture);
        Assert.Contains(overview.Modules, module => module.Key == "obligations" && module.Status == "seeded");
        Assert.Contains(overview.PriorityObligations, obligation =>
            obligation.Id == "cmmc-32-cfr-170" &&
            obligation.SourceUrl == "https://www.ecfr.gov/current/title-32/subtitle-A/chapter-I/subchapter-G/part-170");
    }
}
