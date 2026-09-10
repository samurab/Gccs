using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Compliance;
using Gccs.Application.Security;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Domain.Evidence;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Common;
using Gccs.Infrastructure.Compliance;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class SspExportPackagePersistenceTests
{
    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task PostgreSQL_package_and_audit_are_atomic_and_package_history_is_durable()
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION") ??
            throw new InvalidOperationException("Set GCCS_TEST_POSTGRES_CONNECTION to run this test.");
        var tenantId = Guid.NewGuid(); var actorId = Guid.NewGuid();
        var services = BuildServices(connectionString, tenantId, actorId, throwAudit: false);
        await using var provider = services.BuildServiceProvider();
        await SeedTenantAndSectionAsync(provider, tenantId, actorId);
        try
        {
            Guid packageId;
            await using (var scope = provider.CreateAsyncScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<SspExportPackageService>();
                var package = await service.GenerateAsync(new CreateSspExportPackageRequest(
                    "ssp-persistent-1", "Persistent boundary", "reviewer@example.invalid", SspExportFormat.Both, false, [], []), actorId);
                packageId = package.Id;
            }

            await using (var verify = new GccsDbContext(new DbContextOptionsBuilder<GccsDbContext>().UseGccsPostgres(connectionString).Options))
            {
                Assert.True(await verify.SspExportPackages.AnyAsync(item => item.Id == packageId && item.TenantId == tenantId));
                Assert.True(await verify.SspExportPackageHistory.AnyAsync(item => item.PackageId == packageId && item.TenantId == tenantId && item.Action == "Generated"));
                Assert.True(await verify.AuditLogEntries.AnyAsync(item => item.EntityId == packageId.ToString() && item.TenantId == tenantId && item.Action == AuditAction.Exported));
            }

            var failingServices = BuildServices(connectionString, tenantId, actorId, throwAudit: true);
            await using var failingProvider = failingServices.BuildServiceProvider();
            await using (var scope = failingProvider.CreateAsyncScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<SspExportPackageService>();
                await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateAsync(new CreateSspExportPackageRequest(
                    "ssp-rollback-1", "Rollback boundary", "reviewer@example.invalid", SspExportFormat.Both, false, [], []), actorId));
            }
            await using var rollbackVerify = new GccsDbContext(new DbContextOptionsBuilder<GccsDbContext>().UseGccsPostgres(connectionString).Options);
            Assert.False(await rollbackVerify.SspExportPackages.AnyAsync(item => item.TenantId == tenantId && item.PackageVersion == "ssp-rollback-1"));
            Assert.False(await rollbackVerify.SspExportPackageHistory.AnyAsync(item => item.TenantId == tenantId && item.Notes == "Internal review package generated." && item.Package!.PackageVersion == "ssp-rollback-1"));

            await using (var scope = failingProvider.CreateAsyncScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<SspExportPackageService>();
                await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdatePolicyAsync(
                    new UpdateSspExportPolicyRequest(false, 0, "Synthetic rollback"), actorId));
            }
            Assert.False(await rollbackVerify.SspExportPolicies.AnyAsync(item => item.TenantId == tenantId));
        }
        finally { await CleanupAsync(connectionString, tenantId); }
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task PostgreSQL_source_resolution_returns_only_approved_current_tenant_no_cui_evidence()
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION") ??
            throw new InvalidOperationException("Set GCCS_TEST_POSTGRES_CONNECTION to run this test.");
        var options = new DbContextOptionsBuilder<GccsDbContext>().UseGccsPostgres(connectionString).Options;
        var tenantId = Guid.NewGuid(); var otherTenantId = Guid.NewGuid(); var actorId = Guid.NewGuid();
        await using var db = new GccsDbContext(options);
        await PostgresTestDatabase.MigrateAsync(db);
        db.Tenants.AddRange(Tenant(tenantId, "Tenant Alpha"), Tenant(otherTenantId, "Tenant Other"));
        var eligible = Evidence(tenantId, actorId, EvidenceStatus.Approved, ContentClassification.Fci);
        var unapproved = Evidence(tenantId, actorId, EvidenceStatus.InReview, ContentClassification.Fci);
        var prohibited = Evidence(tenantId, actorId, EvidenceStatus.Approved, ContentClassification.Prohibited);
        var unknown = Evidence(tenantId, actorId, EvidenceStatus.Approved, ContentClassification.Unknown);
        var cui = Evidence(tenantId, actorId, EvidenceStatus.Approved, ContentClassification.Cui);
        var crossTenant = Evidence(otherTenantId, actorId, EvidenceStatus.Approved, ContentClassification.Fci);
        db.EvidenceItems.AddRange(eligible, unapproved, prohibited, unknown, cui, crossTenant);
        await db.SaveChangesAsync();
        try
        {
            var snapshot = await new EfSspExportSourceRepository(db, TimeProvider.System).ResolveAsync(
                tenantId, [eligible.Id, unapproved.Id, prohibited.Id, unknown.Id, cui.Id, crossTenant.Id], []);
            Assert.Equal("Tenant Alpha", snapshot.TenantName);
            Assert.Equal(eligible.Id, Assert.Single(snapshot.Evidence).Id);
        }
        finally
        {
            db.EvidenceItems.RemoveRange(db.EvidenceItems.Where(item => item.TenantId == tenantId || item.TenantId == otherTenantId));
            db.Tenants.RemoveRange(db.Tenants.Where(item => item.Id == tenantId || item.Id == otherTenantId));
            await db.SaveChangesAsync();
        }
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task PostgreSQL_duplicate_versions_and_lifecycle_concurrency_have_one_winner()
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION") ??
            throw new InvalidOperationException("Set GCCS_TEST_POSTGRES_CONNECTION to run this test.");
        var tenantId = Guid.NewGuid(); var generatorId = Guid.NewGuid();
        var services = BuildServices(connectionString, tenantId, generatorId, throwAudit: false);
        await using var provider = services.BuildServiceProvider();
        await SeedTenantAndSectionAsync(provider, tenantId, generatorId);
        try
        {
            var generated = await Task.WhenAll(
                TryGenerateAsync(provider, "ssp-concurrent-1", generatorId),
                TryGenerateAsync(provider, "ssp-concurrent-1", generatorId));
            var packageId = Assert.Single(generated, id => id.HasValue)!.Value;
            Assert.Single(generated, id => !id.HasValue);
            await using (var generatedVerify = new GccsDbContext(new DbContextOptionsBuilder<GccsDbContext>().UseGccsPostgres(connectionString).Options))
                Assert.Equal(1, await generatedVerify.SspExportPackages.Where(item => item.Id == packageId).Select(item => item.Version).SingleAsync());

            var approved = await Task.WhenAll(
                TryApproveAsync(provider, packageId, Guid.NewGuid()),
                TryApproveAsync(provider, packageId, Guid.NewGuid()));
            Assert.Single(approved, value => value.Succeeded);
            Assert.Single(approved, value => !value.Succeeded);

            var recorded = await Task.WhenAll(
                TryRecordShareAsync(provider, packageId, "advisor-one@example.invalid", generatorId),
                TryRecordShareAsync(provider, packageId, "advisor-two@example.invalid", generatorId));
            Assert.Single(recorded, value => value.Succeeded);
            Assert.Single(recorded, value => !value.Succeeded);

            await using var verify = new GccsDbContext(new DbContextOptionsBuilder<GccsDbContext>().UseGccsPostgres(connectionString).Options);
            Assert.Single(await verify.SspExportPackages.Where(item => item.TenantId == tenantId && item.PackageVersion == "ssp-concurrent-1").ToArrayAsync());
            Assert.Single(await verify.SspExportPackageHistory.Where(item => item.TenantId == tenantId && item.PackageId == packageId && item.Action == "ExternalShareApproved").ToArrayAsync());
            Assert.Single(await verify.SspExportPackageHistory.Where(item => item.TenantId == tenantId && item.PackageId == packageId && item.Action == "Shared").ToArrayAsync());

            var policyUpdates = await Task.WhenAll(
                TryUpdatePolicyAsync(provider, requireIndependentApproval: false, generatorId),
                TryUpdatePolicyAsync(provider, requireIndependentApproval: true, generatorId));
            Assert.Single(policyUpdates, value => value.Succeeded);
            Assert.Single(policyUpdates, value => !value.Succeeded);
            verify.ChangeTracker.Clear();
            var storedPolicy = await verify.SspExportPolicies.SingleAsync(item => item.TenantId == tenantId);
            Assert.Equal(1, storedPolicy.Version);
        }
        finally { await CleanupAsync(connectionString, tenantId); }
    }

    private static ServiceCollection BuildServices(string connectionString, Guid tenantId, Guid actorId, bool throwAudit)
    {
        var services = new ServiceCollection();
        services.AddDbContext<GccsDbContext>(options => options.UseGccsPostgres(connectionString));
        services.AddScoped<ISspSectionRepository, EfSspSectionRepository>();
        services.AddScoped<ISspNarrativeRepository, EfSspNarrativeRepository>();
        services.AddScoped<ISspExportSourceRepository, EfSspExportSourceRepository>();
        services.AddScoped<ISspExportPackageRepository, EfSspExportPackageRepository>();
        services.AddScoped<ISspExportPolicyRepository, EfSspExportPolicyRepository>();
        services.AddScoped<IApplicationTransaction, EfApplicationTransaction>();
        services.AddScoped<SspExportPackageService>();
        services.AddSingleton<SspExportLanguagePolicy>();
        services.AddSingleton<ICurrentTenantContext>(new FixedTenantContext(tenantId, actorId));
        services.AddSingleton<IAuditRequestMetadata>(new StaticAuditRequestMetadata("127.0.0.1", "test", "ssp-export-test"));
        services.AddSingleton(TimeProvider.System);
        if (throwAudit) services.AddScoped<IAuditEventWriter, ThrowingAuditWriter>();
        else services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();
        return services;
    }

    private static async Task<Guid?> TryGenerateAsync(ServiceProvider provider, string version, Guid actorId)
    {
        await using var scope = provider.CreateAsyncScope();
        try
        {
            var package = await scope.ServiceProvider.GetRequiredService<SspExportPackageService>().GenerateAsync(
                new CreateSspExportPackageRequest(version, "Concurrent boundary", "reviewer@example.invalid", SspExportFormat.Both, false, [], []),
                actorId);
            return package.Id;
        }
        catch (SspExportPackageValidationException) { return null; }
    }

    private static async Task<AttemptResult> TryApproveAsync(ServiceProvider provider, Guid packageId, Guid actorId)
    {
        await using var scope = provider.CreateAsyncScope();
        try
        {
            var approved = await scope.ServiceProvider.GetRequiredService<SspExportPackageService>().ApproveExternalShareAsync(
                packageId, new SspExternalShareApprovalRequest("Concurrent approval"), actorId);
            return new AttemptResult(approved is not null, approved is null ? "Package was not found." : null);
        }
        catch (SspExportPackageValidationException exception) { return new AttemptResult(false, exception.Message); }
    }

    private static async Task<AttemptResult> TryRecordShareAsync(ServiceProvider provider, Guid packageId, string recipient, Guid actorId)
    {
        await using var scope = provider.CreateAsyncScope();
        try
        {
            var recorded = await scope.ServiceProvider.GetRequiredService<SspExportPackageService>().RecordExternalShareAsync(
                packageId, new SspExternalShareRequest(recipient, "Concurrent external-share record"), actorId);
            return new AttemptResult(recorded is not null, recorded is null ? "Package was not found." : null);
        }
        catch (SspExportPackageValidationException exception) { return new AttemptResult(false, exception.Message); }
    }

    private static async Task<AttemptResult> TryUpdatePolicyAsync(ServiceProvider provider, bool requireIndependentApproval, Guid actorId)
    {
        await using var scope = provider.CreateAsyncScope();
        try
        {
            await scope.ServiceProvider.GetRequiredService<SspExportPackageService>().UpdatePolicyAsync(
                new UpdateSspExportPolicyRequest(requireIndependentApproval, 0, "Concurrent policy update"), actorId);
            return new AttemptResult(true, null);
        }
        catch (SspExportPackageValidationException exception) { return new AttemptResult(false, exception.Message); }
    }

    private sealed record AttemptResult(bool Succeeded, string? Error);

    private static async Task SeedTenantAndSectionAsync(ServiceProvider provider, Guid tenantId, Guid actorId)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        await PostgresTestDatabase.MigrateAsync(db);
        db.Tenants.Add(Tenant(tenantId, "Tenant Alpha")); await db.SaveChangesAsync();
        await new EfSspSectionRepository(db).CreateAsync(tenantId,
            new CreateSspSectionRequest(SspSectionType.SystemDescription, "System", "Security", [], [new SspSourceReferenceDto("NIST", "https://csrc.nist.gov/", DateOnly.FromDateTime(DateTime.UtcNow))]),
            actorId, "owner@example.invalid");
    }

    private static TenantEntity Tenant(Guid id, string name) => new()
    {
        Id = id, Name = name, Status = TenantStatus.Active, DataPosture = TenantDataPosture.NoCui,
        CreatedAt = DateTimeOffset.UtcNow, CreatedByUserId = Guid.NewGuid()
    };

    private static EvidenceItemEntity Evidence(Guid tenantId, Guid actorId, EvidenceStatus status, ContentClassification classification) => new()
    {
        Id = Guid.NewGuid(), TenantId = tenantId, Name = $"Evidence {Guid.NewGuid():N}", Description = "Test evidence", Type = EvidenceType.SystemConfiguration,
        OwnerFunction = "Security", Status = status, Classification = classification, ApprovedAt = status == EvidenceStatus.Approved ? DateTimeOffset.UtcNow : null,
        ApprovedByUserId = status == EvidenceStatus.Approved ? actorId : null, CreatedAt = DateTimeOffset.UtcNow, CreatedByUserId = actorId
    };

    private static async Task CleanupAsync(string connectionString, Guid tenantId)
    {
        await using var db = new GccsDbContext(new DbContextOptionsBuilder<GccsDbContext>().UseGccsPostgres(connectionString).Options);
        var history = await db.SspExportPackageHistory.Where(item => item.TenantId == tenantId).ToArrayAsync();
        if (history.Length > 0) { db.SspExportPackageHistory.RemoveRange(history); await db.SaveChangesAsync(); }
        var packages = await db.SspExportPackages.Where(item => item.TenantId == tenantId).ToArrayAsync();
        if (packages.Length > 0) { db.SspExportPackages.RemoveRange(packages); await db.SaveChangesAsync(); }
        var policies = await db.SspExportPolicies.Where(item => item.TenantId == tenantId).ToArrayAsync();
        if (policies.Length > 0) { db.SspExportPolicies.RemoveRange(policies); await db.SaveChangesAsync(); }
        var sections = await db.SspSections.Where(item => item.TenantId == tenantId).ToArrayAsync();
        if (sections.Length > 0) { db.SspSections.RemoveRange(sections); await db.SaveChangesAsync(); }
        var tenant = await db.Tenants.SingleOrDefaultAsync(item => item.Id == tenantId);
        if (tenant is not null) { db.Tenants.Remove(tenant); await db.SaveChangesAsync(); }
    }

    private sealed record FixedTenantContext(Guid TenantId, Guid UserId) : ICurrentTenantContext { public string UserEmail => "reviewer@example.invalid"; }
    private sealed record StaticAuditRequestMetadata(string IpAddress, string UserAgent, string CorrelationId) : IAuditRequestMetadata;
    private sealed class ThrowingAuditWriter : IAuditEventWriter
    {
        public Task WriteAsync(Guid tenantId, Guid actorUserId, AuditAction action, string entityType, string entityId, string summary, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Synthetic audit failure.");
    }
}
