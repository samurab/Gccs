using System.Net;
using System.Net.Http.Json;
using Gccs.Application.Tenancy;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class DevelopmentTestingContextTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DevelopmentTestingContextTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Catalog_returns_all_tenants_and_supported_roles_in_development()
    {
        var activeTenantId = Guid.NewGuid();
        var archivedTenantId = Guid.NewGuid();
        using var factory = CreateFactory(
            nameof(Catalog_returns_all_tenants_and_supported_roles_in_development),
            developmentTestingEnabled: true,
            dbContext =>
            {
                dbContext.Tenants.AddRange(
                    CreateTenant(archivedTenantId, "Zulu archived", TenantStatus.Archived),
                    CreateTenant(activeTenantId, "Alpha active", TenantStatus.Active));
            });
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/development/testing-context");
        request.Headers.Add("X-Gccs-Dev-Auth", "true");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        var result = await response.Content.ReadFromJsonAsync<DevelopmentTestingContextDto>();
        Assert.NotNull(result);
        Assert.Equal([activeTenantId, archivedTenantId], result.Tenants.Select(tenant => tenant.TenantId));
        Assert.True(result.Tenants[0].IsSelectable);
        Assert.False(result.Tenants[1].IsSelectable);
        Assert.Equal("The tenant is not operational.", result.Tenants[1].UnavailableReason);
        Assert.Equal(
            ["Owner", "Admin", "Compliance Manager", "Contributor", "Auditor", "Advisor"],
            result.Roles);
    }

    [Fact]
    public async Task Catalog_route_is_not_registered_when_development_testing_is_disabled()
    {
        using var factory = CreateFactory(
            nameof(Catalog_route_is_not_registered_when_development_testing_is_disabled),
            developmentTestingEnabled: false,
            _ => { });
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/development/testing-context");
        request.Headers.Add("X-Gccs-Dev-Auth", "true");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void Catalog_route_is_not_registered_outside_development_even_when_flags_are_enabled()
    {
        using var factory = CreateFactory(
            nameof(Catalog_route_is_not_registered_outside_development_even_when_flags_are_enabled),
            developmentTestingEnabled: true,
            _ => { },
            environment: "Production");
        _ = factory.Services;
        Assert.DoesNotContain(
            factory.Services.GetRequiredService<EndpointDataSource>().Endpoints,
            endpoint => endpoint is RouteEndpoint routeEndpoint &&
                        routeEndpoint.RoutePattern.RawText == "/api/development/testing-context");
        Assert.DoesNotContain(
            factory.Services.GetServices<DevelopmentTestingContextService>(),
            _ => true);
    }

    [Fact]
    public void Common_infrastructure_registration_excludes_development_testing_services()
    {
        var services = new ServiceCollection();

        services.AddGccsInfrastructure();

        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ServiceType == typeof(DevelopmentTestingContextService));
        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ServiceType == typeof(IDevelopmentTenantCatalogRepository));
    }

    private WebApplicationFactory<Program> CreateFactory(
        string databaseName,
        bool developmentTestingEnabled,
        Action<GccsDbContext> seed,
        string environment = "Development") =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.UseSetting("Security:DevelopmentAuth:Enabled", "true");
            builder.UseSetting("Security:DevelopmentTesting:Enabled", developmentTestingEnabled.ToString());
            if (environment != "Development")
            {
                builder.UseSetting("Authentication:Authority", "https://login.microsoftonline.com/test-tenant/v2.0");
                builder.UseSetting("Authentication:Audience", "api://gccs-tests");
                builder.UseSetting("Cors:AllowedOrigins:0", "https://gccs-tests.example");
                builder.UseSetting("AllowedHosts", "gccs-tests.example");
            }
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                if (environment == "Development")
                {
                    services.AddScoped<DevelopmentTestingContextService>();
                    services.AddScoped<IDevelopmentTenantCatalogRepository, EfDevelopmentTenantCatalogRepository>();
                }

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
                seed(dbContext);
                dbContext.SaveChanges();
            });
        });

    private static TenantEntity CreateTenant(Guid tenantId, string name, TenantStatus status) =>
        new()
        {
            Id = tenantId,
            Name = name,
            Status = status,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.Parse("2026-07-25T12:00:00Z")
        };
}
