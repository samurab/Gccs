using Gccs.Api.LocalDevelopment;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Domain.Evidence;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
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
        var bootstrapper = CreateBootstrapper(
            provider,
            "Production",
            developmentAuthEnabled: true,
            marketingDemoEnabled: true);

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

    [Fact]
    public async Task Marketing_demo_seed_is_excluded_when_flag_is_disabled()
    {
        await using var provider = CreateProvider("marketing-demo-flag-off", "Development", developmentAuthEnabled: true);
        var bootstrapper = CreateBootstrapper(provider, "Development", developmentAuthEnabled: true, marketingDemoEnabled: false);

        await bootstrapper.StartAsync(CancellationToken.None);

        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.False(await dbContext.Tenants.AnyAsync(tenant => tenant.Id == NorthstarTenantId));
        Assert.Equal(2, await dbContext.Tenants.CountAsync());
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Marketing_demo_seed_requires_development_auth_and_seed_data(
        bool developmentAuthEnabled,
        bool seedDataEnabled)
    {
        await using var provider = CreateProvider(
            $"marketing-demo-gates-{developmentAuthEnabled}-{seedDataEnabled}",
            "Development",
            developmentAuthEnabled);
        var bootstrapper = CreateBootstrapper(
            provider,
            "Development",
            developmentAuthEnabled,
            marketingDemoEnabled: true,
            seedDataEnabled);

        await bootstrapper.StartAsync(CancellationToken.None);

        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.False(await dbContext.Tenants.AnyAsync(tenant => tenant.Id == NorthstarTenantId));
    }

    [Fact]
    public async Task Marketing_demo_seed_creates_exact_northstar_tenant_users_and_roles_without_internal_branding()
    {
        await using var provider = CreateProvider("marketing-demo-northstar", "Development", developmentAuthEnabled: true);
        var bootstrapper = CreateBootstrapper(provider, "Development", developmentAuthEnabled: true, marketingDemoEnabled: true);

        await bootstrapper.StartAsync(CancellationToken.None);

        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var tenant = await dbContext.Tenants.SingleAsync(candidate => candidate.Id == NorthstarTenantId);
        var users = await dbContext.Users
            .Where(user => user.TenantId == NorthstarTenantId)
            .OrderBy(user => user.DisplayName)
            .ToArrayAsync();
        var memberships = await dbContext.TenantMemberships
            .Where(membership => membership.TenantId == NorthstarTenantId)
            .OrderBy(membership => membership.RoleName)
            .ToArrayAsync();
        var modeHistory = await dbContext.TenantDataHandlingModeHistory.SingleAsync(history => history.TenantId == NorthstarTenantId);
        var audit = await dbContext.AuditLogEntries.SingleAsync(entry => entry.TenantId == NorthstarTenantId);

        Assert.Equal(3, await dbContext.Tenants.CountAsync());
        Assert.Equal("Northstar Precision Systems", tenant.Name);
        Assert.Equal(TenantDataPosture.NoCui, tenant.DataPosture);
        Assert.Equal(4, users.Length);
        Assert.Equal(
            ["Alex Morgan", "Daniel Brooks", "Elena Ortiz", "Priya Shah"],
            users.Select(user => user.DisplayName).ToArray());
        Assert.All(users, user => Assert.EndsWith(".northstar@example.com", user.Email, StringComparison.Ordinal));
        Assert.Equal(
            [RoleCatalog.Admin, RoleCatalog.Auditor, RoleCatalog.ComplianceManager, RoleCatalog.Contributor],
            memberships.Select(membership => membership.RoleName).ToArray());
        Assert.All(memberships, membership => Assert.Equal(MembershipStatus.Active, membership.Status));

        var customerVisibleSeedText = string.Join(
            " ",
            users.SelectMany(user => new[] { user.Email, user.DisplayName })
                .Concat([modeHistory.Reason, modeHistory.ApprovalRecordReference, audit.UserAgent, audit.Summary, audit.MetadataJson]));
        Assert.DoesNotContain("gccs", customerVisibleSeedText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FeDril", customerVisibleSeedText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Marketing_demo_seed_has_high_risk_overdue_poam_and_non_sensitive_evidence_metadata()
    {
        await using var provider = CreateProvider("marketing-demo-readiness", "Development", developmentAuthEnabled: true);
        var bootstrapper = CreateBootstrapper(provider, "Development", developmentAuthEnabled: true, marketingDemoEnabled: true);

        await bootstrapper.StartAsync(CancellationToken.None);

        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var poam = await dbContext.PoamItems.SingleAsync(item => item.TenantId == NorthstarTenantId);
        var evidence = await dbContext.EvidenceItems.SingleAsync(item => item.TenantId == NorthstarTenantId);

        Assert.Equal(RiskLevel.High, poam.RiskLevel);
        Assert.Equal(PoamStatus.Open, poam.Status);
        Assert.True(poam.TargetCompletionAt < DateOnly.FromDateTime(DateTime.UtcNow));
        Assert.Contains("fictional", poam.PlannedRemediation, StringComparison.OrdinalIgnoreCase);

        Assert.Equal("Northstar quarterly access review summary", evidence.Name);
        Assert.Equal("northstar-quarterly-access-review-summary.pdf", evidence.OriginalFileName);
        Assert.Equal(ContentClassification.Unclassified, evidence.Classification);
        Assert.Null(evidence.StorageUri);
        Assert.Equal("metadata-only", evidence.UploadValidationStatus);
        Assert.Equal("not-applicable-metadata-only", evidence.MalwareScanStatus);
        Assert.Contains("non-sensitive", evidence.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No file content is stored", evidence.Description, StringComparison.Ordinal);
        Assert.DoesNotContain("gccs", evidence.Description, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gccs", evidence.ClassificationReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Marketing_demo_seed_is_idempotent()
    {
        await using var provider = CreateProvider("marketing-demo-idempotent", "Development", developmentAuthEnabled: true);
        var bootstrapper = CreateBootstrapper(provider, "Development", developmentAuthEnabled: true, marketingDemoEnabled: true);

        await bootstrapper.StartAsync(CancellationToken.None);
        await bootstrapper.StartAsync(CancellationToken.None);

        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(1, await dbContext.Tenants.CountAsync(tenant => tenant.Id == NorthstarTenantId));
        Assert.Equal(4, await dbContext.Users.CountAsync(user => user.TenantId == NorthstarTenantId));
        Assert.Equal(4, await dbContext.TenantMemberships.CountAsync(membership => membership.TenantId == NorthstarTenantId));
        Assert.Equal(1, await dbContext.EvidenceItems.CountAsync(evidence => evidence.TenantId == NorthstarTenantId));
        Assert.Equal(1, await dbContext.Assessments.CountAsync(assessment => assessment.TenantId == NorthstarTenantId));
        Assert.Equal(1, await dbContext.PoamItems.CountAsync(poam => poam.TenantId == NorthstarTenantId));
        Assert.Equal(1, await dbContext.AuditLogEntries.CountAsync(audit => audit.TenantId == NorthstarTenantId));
        Assert.Equal(1, await dbContext.TenantDataHandlingModeHistory.CountAsync(history => history.TenantId == NorthstarTenantId));
    }

    [Fact]
    public async Task Marketing_demo_seed_fails_closed_when_tenant_identifier_belongs_to_another_record()
    {
        await using var provider = CreateProvider("marketing-demo-tenant-id-collision", "Development", developmentAuthEnabled: true);
        var originalCreatedAt = new DateTimeOffset(2025, 1, 10, 9, 30, 0, TimeSpan.Zero);
        var originalCreatedBy = Guid.Parse("99999999-9999-9999-9999-999999999901");
        using (var setupScope = provider.CreateScope())
        {
            var setupContext = setupScope.ServiceProvider.GetRequiredService<GccsDbContext>();
            setupContext.Tenants.Add(new TenantEntity
            {
                Id = NorthstarTenantId,
                Name = "Existing tenant using reserved identifier",
                Status = TenantStatus.Trialing,
                DataPosture = TenantDataPosture.DemoSandbox,
                TrialEndsAt = new DateOnly(2026, 12, 31),
                CreatedAt = originalCreatedAt,
                CreatedByUserId = originalCreatedBy
            });
            await setupContext.SaveChangesAsync();
        }

        var bootstrapper = CreateBootstrapper(provider, "Development", developmentAuthEnabled: true, marketingDemoEnabled: true);
        await bootstrapper.StartAsync(CancellationToken.None);

        using var assertionScope = provider.CreateScope();
        var dbContext = assertionScope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var tenant = await dbContext.Tenants.SingleAsync(candidate => candidate.Id == NorthstarTenantId);
        Assert.Equal("Existing tenant using reserved identifier", tenant.Name);
        Assert.Equal(TenantStatus.Trialing, tenant.Status);
        Assert.Equal(TenantDataPosture.DemoSandbox, tenant.DataPosture);
        Assert.Equal(new DateOnly(2026, 12, 31), tenant.TrialEndsAt);
        Assert.Equal(originalCreatedAt, tenant.CreatedAt);
        Assert.Equal(originalCreatedBy, tenant.CreatedByUserId);
        await AssertNoNorthstarSeedDependentsAsync(dbContext);
        Assert.Empty(await dbContext.Controls.ToArrayAsync());
    }

    [Fact]
    public async Task Marketing_demo_seed_fails_closed_when_deterministic_user_identifier_belongs_to_another_tenant()
    {
        await using var provider = CreateProvider("marketing-demo-user-id-collision", "Development", developmentAuthEnabled: true);
        var otherTenantId = Guid.Parse("99999999-9999-9999-9999-999999999902");
        var originalCreatedAt = new DateTimeOffset(2025, 2, 11, 10, 45, 0, TimeSpan.Zero);
        using (var setupScope = provider.CreateScope())
        {
            var setupContext = setupScope.ServiceProvider.GetRequiredService<GccsDbContext>();
            setupContext.Tenants.Add(new TenantEntity
            {
                Id = otherTenantId,
                Name = "Existing unrelated tenant",
                Status = TenantStatus.Active,
                DataPosture = TenantDataPosture.NoCui,
                CreatedAt = originalCreatedAt,
                CreatedByUserId = NorthstarAdminUserId
            });
            setupContext.Users.Add(new UserEntity
            {
                Id = NorthstarAdminUserId,
                TenantId = otherTenantId,
                Email = "existing.user.unrelated@example.com",
                DisplayName = "Existing User",
                Status = UserStatus.Disabled,
                MfaEnabled = false,
                CreatedAt = originalCreatedAt,
                CreatedByUserId = NorthstarAdminUserId
            });
            await setupContext.SaveChangesAsync();
        }

        var bootstrapper = CreateBootstrapper(provider, "Development", developmentAuthEnabled: true, marketingDemoEnabled: true);
        await bootstrapper.StartAsync(CancellationToken.None);

        using var assertionScope = provider.CreateScope();
        var dbContext = assertionScope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var existingUser = await dbContext.Users.SingleAsync(user => user.Id == NorthstarAdminUserId);
        Assert.Equal(otherTenantId, existingUser.TenantId);
        Assert.Equal("existing.user.unrelated@example.com", existingUser.Email);
        Assert.Equal("Existing User", existingUser.DisplayName);
        Assert.Equal(UserStatus.Disabled, existingUser.Status);
        Assert.False(existingUser.MfaEnabled);
        Assert.Equal(originalCreatedAt, existingUser.CreatedAt);
        Assert.False(await dbContext.Tenants.AnyAsync(tenant => tenant.Id == NorthstarTenantId));
        Assert.False(await dbContext.TenantMemberships.AnyAsync(membership => membership.UserId == NorthstarAdminUserId));
        await AssertNoNorthstarSeedDependentsAsync(dbContext);
        Assert.Empty(await dbContext.Controls.ToArrayAsync());
    }

    private static async Task AssertNoNorthstarSeedDependentsAsync(GccsDbContext dbContext)
    {
        Assert.False(await dbContext.Users.AnyAsync(user => user.TenantId == NorthstarTenantId));
        Assert.False(await dbContext.Roles.AnyAsync(role => role.TenantId == NorthstarTenantId));
        Assert.False(await dbContext.TenantMemberships.AnyAsync(membership => membership.TenantId == NorthstarTenantId));
        Assert.False(await dbContext.TenantDataHandlingModeHistory.AnyAsync(history => history.TenantId == NorthstarTenantId));
        Assert.False(await dbContext.EvidenceItems.AnyAsync(evidence => evidence.TenantId == NorthstarTenantId));
        Assert.False(await dbContext.Assessments.AnyAsync(assessment => assessment.TenantId == NorthstarTenantId));
        Assert.False(await dbContext.PoamItems.AnyAsync(poam => poam.TenantId == NorthstarTenantId));
        Assert.False(await dbContext.AuditLogEntries.AnyAsync(audit => audit.TenantId == NorthstarTenantId));
        Assert.False(await dbContext.CuiReadyApprovalChecklists.AnyAsync(checklist => checklist.TenantId == NorthstarTenantId));
    }

    private static readonly Guid TenantAlphaId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TenantBetaId = Guid.Parse("11111111-1111-1111-1111-111111111112");
    private static readonly Guid NorthstarTenantId = Guid.Parse("11111111-1111-1111-1111-111111111113");
    private static readonly Guid NorthstarAdminUserId = Guid.Parse("22222222-2222-2222-2222-222222222242");

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
        bool developmentAuthEnabled,
        bool marketingDemoEnabled = false,
        bool seedDataEnabled = true)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:DevelopmentAuth:Enabled"] = developmentAuthEnabled.ToString(),
                ["Security:DevelopmentAuth:DefaultTenantId"] = TenantAlphaId.ToString(),
                ["Security:DevelopmentAuth:DefaultUserId"] = "22222222-2222-2222-2222-222222222222",
                ["LocalDevelopment:SeedData:Enabled"] = seedDataEnabled.ToString(),
                ["MarketingDemo:Enabled"] = marketingDemoEnabled.ToString()
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
