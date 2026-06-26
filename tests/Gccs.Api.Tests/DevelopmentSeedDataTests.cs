using Gccs.Api.LocalDevelopment;
using Gccs.Domain.Identity;
using Gccs.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class DevelopmentSeedDataTests
{
    [Fact]
    public async Task Seed_data_loads_in_development_environment()
    {
        await using var provider = CreateProvider("development-seed-loads", "Development", developmentAuthEnabled: true);
        var bootstrapper = CreateBootstrapper(provider, "Development", developmentAuthEnabled: true);

        await bootstrapper.StartAsync(CancellationToken.None);

        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(2, await dbContext.Tenants.CountAsync(tenant => tenant.Name == "Tenant Alpha" || tenant.Name == "Tenant Beta"));
        Assert.True(await dbContext.Controls.AnyAsync(control => control.Id == "AC.L1-3.1.1"));
        Assert.True(await dbContext.EvidenceItems.AnyAsync(evidence => evidence.TenantId == TenantAlphaId));
        Assert.True(await dbContext.PoamItems.AnyAsync(poam => poam.TenantId == TenantBetaId));
        Assert.True(await dbContext.AuditLogEntries.AnyAsync(audit => audit.TenantId == TenantAlphaId));
        Assert.True(await dbContext.CuiReadyApprovalChecklists.AnyAsync(checklist => checklist.TenantId == TenantBetaId));
    }

    [Fact]
    public async Task Seed_data_does_not_load_in_production_environment()
    {
        await using var provider = CreateProvider("development-seed-production-skip", "Production", developmentAuthEnabled: true);
        var bootstrapper = CreateBootstrapper(provider, "Production", developmentAuthEnabled: true);

        await bootstrapper.StartAsync(CancellationToken.None);

        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Empty(await dbContext.Tenants.ToArrayAsync());
        Assert.Empty(await dbContext.Users.ToArrayAsync());
    }

    [Fact]
    public async Task Tenant_alpha_and_beta_records_remain_separated()
    {
        await using var provider = CreateProvider("development-seed-isolation", "Development", developmentAuthEnabled: true);
        var bootstrapper = CreateBootstrapper(provider, "Development", developmentAuthEnabled: true);

        await bootstrapper.StartAsync(CancellationToken.None);

        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var alphaEvidence = await dbContext.EvidenceItems.SingleAsync(evidence => evidence.TenantId == TenantAlphaId);
        var betaEvidence = await dbContext.EvidenceItems.SingleAsync(evidence => evidence.TenantId == TenantBetaId);
        var alphaPoam = await dbContext.PoamItems.SingleAsync(poam => poam.TenantId == TenantAlphaId);
        var betaPoam = await dbContext.PoamItems.SingleAsync(poam => poam.TenantId == TenantBetaId);

        Assert.NotEqual(alphaEvidence.Id, betaEvidence.Id);
        Assert.NotEqual(alphaPoam.Id, betaPoam.Id);
        Assert.Equal(TenantAlphaId, alphaPoam.TenantId);
        Assert.Equal(TenantBetaId, betaPoam.TenantId);
        Assert.DoesNotContain(await dbContext.EvidenceItems.Where(evidence => evidence.TenantId == TenantAlphaId).ToArrayAsync(), evidence => evidence.Name.Contains("Tenant Beta", StringComparison.Ordinal));
        Assert.DoesNotContain(await dbContext.PoamItems.Where(poam => poam.TenantId == TenantBetaId).ToArrayAsync(), poam => poam.Weakness.Contains("Tenant Alpha", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Development_users_have_expected_roles()
    {
        await using var provider = CreateProvider("development-seed-roles", "Development", developmentAuthEnabled: true);
        var bootstrapper = CreateBootstrapper(provider, "Development", developmentAuthEnabled: true);

        await bootstrapper.StartAsync(CancellationToken.None);

        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var alphaMemberships = await dbContext.TenantMemberships
            .Where(membership => membership.TenantId == TenantAlphaId)
            .OrderBy(membership => membership.RoleName)
            .Select(membership => membership.RoleName)
            .ToArrayAsync();
        var betaMemberships = await dbContext.TenantMemberships
            .Where(membership => membership.TenantId == TenantBetaId)
            .OrderBy(membership => membership.RoleName)
            .Select(membership => membership.RoleName)
            .ToArrayAsync();

        Assert.Equal(
            [RoleCatalog.Admin, RoleCatalog.Auditor, RoleCatalog.ComplianceManager, RoleCatalog.Contributor],
            alphaMemberships);
        Assert.Equal(alphaMemberships, betaMemberships);
        Assert.Equal(4, await dbContext.Users.CountAsync(user => user.TenantId == TenantAlphaId));
        Assert.Equal(4, await dbContext.Users.CountAsync(user => user.TenantId == TenantBetaId));
    }

    private static readonly Guid TenantAlphaId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TenantBetaId = Guid.Parse("11111111-1111-1111-1111-111111111112");

    private static ServiceProvider CreateProvider(string databaseName, string environmentName, bool developmentAuthEnabled)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment(environmentName));
        services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
        return services.BuildServiceProvider();
    }

    private static DevelopmentTenantBootstrapper CreateBootstrapper(
        IServiceProvider provider,
        string environmentName,
        bool developmentAuthEnabled)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:DevelopmentAuth:Enabled"] = developmentAuthEnabled.ToString(),
                ["Security:DevelopmentAuth:DefaultTenantId"] = TenantAlphaId.ToString(),
                ["Security:DevelopmentAuth:DefaultUserId"] = "22222222-2222-2222-2222-222222222222",
                ["LocalDevelopment:SeedData:Enabled"] = "true"
            })
            .Build();

        return new DevelopmentTenantBootstrapper(
            provider,
            configuration,
            new TestWebHostEnvironment(environmentName),
            NullLogger<DevelopmentTenantBootstrapper>.Instance);
    }

    private sealed class TestWebHostEnvironment(string environmentName) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Gccs.Api.Tests";
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
