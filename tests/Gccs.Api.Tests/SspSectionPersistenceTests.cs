using Gccs.Application.Common;
using Gccs.Application.Audit;
using Gccs.Application.Compliance;
using Gccs.Application.Security;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Compliance;
using Gccs.Infrastructure.Common;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class SspSectionPersistenceTests
{
    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task PostgreSQL_migration_persists_tenant_scoped_sections_and_rejects_stale_versions()
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION") ??
            throw new InvalidOperationException("Set GCCS_TEST_POSTGRES_CONNECTION to run this test.");
        var options = new DbContextOptionsBuilder<GccsDbContext>().UseGccsPostgres(connectionString).Options;
        var tenant = Guid.NewGuid(); var actor = Guid.NewGuid(); Guid sectionId;
        await using (var setup = new GccsDbContext(options))
        {
            await PostgresTestDatabase.MigrateAsync(setup);
            setup.Tenants.Add(Tenant(tenant)); await setup.SaveChangesAsync();
            sectionId = (await new EfSspSectionRepository(setup).CreateAsync(tenant,
                new CreateSspSectionRequest(SspSectionType.AuthorizationBoundary, "Boundary", "Security", [], [Source()]), actor, "tester@example.com")).Id;
        }
        try
        {
            await using var first = new GccsDbContext(options); await using var second = new GccsDbContext(options);
            var firstRepository = new EfSspSectionRepository(first); var secondRepository = new EfSspSectionRepository(second);
            var firstRead = (await firstRepository.GetAsync(tenant, sectionId))!;
            var secondRead = (await secondRepository.GetAsync(tenant, sectionId))!;
            var winner = await firstRepository.UpdateAsync(tenant, sectionId,
                new UpdateSspSectionRequest(firstRead.SectionType, "Winning boundary", firstRead.Owner, [], firstRead.SourceReferences, firstRead.Version), actor);
            await Assert.ThrowsAsync<ContentRevisionConflictException>(() => secondRepository.UpdateAsync(tenant, sectionId,
                new UpdateSspSectionRequest(secondRead.SectionType, "Stale boundary", secondRead.Owner, [], secondRead.SourceReferences, secondRead.Version), actor));
            Assert.Equal(2, winner!.Version);
            Assert.Null(await firstRepository.GetAsync(Guid.NewGuid(), sectionId));
        }
        finally
        {
            await using var cleanup = new GccsDbContext(options);
            var section = await cleanup.SspSections.SingleOrDefaultAsync(x => x.Id == sectionId);
            if (section is not null) cleanup.SspSections.Remove(section);
            var tenantEntity = await cleanup.Tenants.SingleOrDefaultAsync(x => x.Id == tenant);
            if (tenantEntity is not null) cleanup.Tenants.Remove(tenantEntity);
            await cleanup.SaveChangesAsync();
        }
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task PostgreSQL_audit_failure_rolls_back_section_and_initial_history()
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION") ??
            throw new InvalidOperationException("Set GCCS_TEST_POSTGRES_CONNECTION to run this test.");
        var tenant = Guid.NewGuid(); var actor = Guid.NewGuid();
        var services = new ServiceCollection();
        services.AddDbContext<GccsDbContext>(options => options.UseGccsPostgres(connectionString));
        services.AddScoped<ISspSectionRepository, EfSspSectionRepository>();
        services.AddScoped<ISspSectionLinkValidator, EfSspSectionLinkValidator>();
        services.AddScoped<IApplicationTransaction, EfApplicationTransaction>();
        services.AddScoped<IAuditEventWriter, ThrowingAuditWriter>();
        services.AddSingleton<ICurrentTenantContext>(new FixedTenantContext(tenant, actor));
        services.AddSingleton<InMemorySspSectionRepository>();
        services.AddSingleton<ISspNarrativeRepository>(provider => provider.GetRequiredService<InMemorySspSectionRepository>());
        services.AddSingleton<ISspExportPackageRepository>(provider => provider.GetRequiredService<InMemorySspSectionRepository>());
        services.AddScoped<SspSectionService>();
        await using var provider = services.BuildServiceProvider();
        await using (var setupScope = provider.CreateAsyncScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<GccsDbContext>();
            await PostgresTestDatabase.MigrateAsync(db); db.Tenants.Add(Tenant(tenant)); await db.SaveChangesAsync();
        }
        try
        {
            await using var scope = provider.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<SspSectionService>();
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(
                new CreateSspSectionRequest(SspSectionType.SystemDescription, "System", "Security", [], [Source()]), actor));
            var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            db.ChangeTracker.Clear();
            Assert.False(await db.SspSections.AnyAsync(x => x.TenantId == tenant));
            Assert.False(await db.SspSectionHistory.AnyAsync(x => x.TenantId == tenant));
        }
        finally
        {
            await using var cleanup = new GccsDbContext(new DbContextOptionsBuilder<GccsDbContext>().UseGccsPostgres(connectionString).Options);
            var tenantEntity = await cleanup.Tenants.SingleOrDefaultAsync(x => x.Id == tenant);
            if (tenantEntity is not null) { cleanup.Tenants.Remove(tenantEntity); await cleanup.SaveChangesAsync(); }
        }
    }

    [Fact]
    public async Task EF_repository_persists_history_and_filters_every_query_by_tenant()
    {
        var root = new InMemoryDatabaseRoot();
        var options = new DbContextOptionsBuilder<GccsDbContext>().UseInMemoryDatabase($"ssp-{Guid.NewGuid():N}", root).Options;
        var tenantA = Guid.NewGuid(); var tenantB = Guid.NewGuid(); var actor = Guid.NewGuid(); var companyId = Guid.NewGuid();
        await using (var setup = new GccsDbContext(options))
        {
            setup.Tenants.AddRange(Tenant(tenantA), Tenant(tenantB));
            setup.CompanyProfiles.Add(new CompanyProfileEntity { Id = companyId, TenantId = tenantA, LegalEntityName = "Tenant A", CreatedAt = DateTimeOffset.UtcNow, CreatedByUserId = actor });
            await setup.SaveChangesAsync();
        }

        Guid sectionId;
        await using (var first = new GccsDbContext(options))
        {
            var links = new EfSspSectionLinkValidator(first);
            var request = new CreateSspSectionRequest(SspSectionType.SystemDescription, "System description", "Security",
                [new SspLinkedRecordDto(SspLinkedRecordType.CompanyProfile, companyId.ToString(), "Defines the organization.")], [Source()]);
            await links.ValidateAsync(tenantA, request.LinkedRecords);
            var section = await new EfSspSectionRepository(first).CreateAsync(tenantA, request, actor, "tester@example.com");
            sectionId = section.Id;
        }

        await using (var reloaded = new GccsDbContext(options))
        {
            var repository = new EfSspSectionRepository(reloaded);
            var section = Assert.IsType<SspSectionDto>(await repository.GetAsync(tenantA, sectionId));
            Assert.Equal(1, section.Version);
            Assert.Equal("tester@example.com", Assert.Single(section.History).ActorName);
            Assert.Null(await repository.GetAsync(tenantB, sectionId));
            Assert.Empty(await repository.ListAsync(tenantB));
        }
    }

    [Fact]
    public async Task Link_validator_rejects_cross_tenant_and_NoCui_CUI_evidence()
    {
        var options = new DbContextOptionsBuilder<GccsDbContext>().UseInMemoryDatabase($"ssp-links-{Guid.NewGuid():N}").Options;
        var tenantA = Guid.NewGuid(); var tenantB = Guid.NewGuid(); var actor = Guid.NewGuid();
        var otherCompany = Guid.NewGuid(); var cuiEvidence = Guid.NewGuid();
        await using var db = new GccsDbContext(options);
        db.Tenants.AddRange(Tenant(tenantA), Tenant(tenantB));
        db.CompanyProfiles.Add(new CompanyProfileEntity { Id = otherCompany, TenantId = tenantB, LegalEntityName = "Tenant B", CreatedAt = DateTimeOffset.UtcNow, CreatedByUserId = actor });
        db.EvidenceItems.Add(new EvidenceItemEntity
        {
            Id = cuiEvidence, TenantId = tenantA, Name = "CUI evidence", Classification = ContentClassification.Cui,
            Status = Gccs.Domain.Evidence.EvidenceStatus.Draft, CreatedAt = DateTimeOffset.UtcNow, CreatedByUserId = actor
        });
        await db.SaveChangesAsync();
        var validator = new EfSspSectionLinkValidator(db);

        await Assert.ThrowsAsync<SspSectionValidationException>(() => validator.ValidateAsync(tenantA,
            [new SspLinkedRecordDto(SspLinkedRecordType.CompanyProfile, otherCompany.ToString(), "Cross-tenant reference.")]));
        await Assert.ThrowsAsync<SspSectionValidationException>(() => validator.ValidateAsync(tenantA,
            [new SspLinkedRecordDto(SspLinkedRecordType.Evidence, cuiEvidence.ToString(), "Unsupported CUI source.")]));
        Assert.Empty(db.SspSections);
        Assert.Empty(db.AuditLogEntries);
    }

    [Fact]
    public async Task EF_repository_rejects_stale_update_without_losing_the_winning_version()
    {
        var root = new InMemoryDatabaseRoot();
        var options = new DbContextOptionsBuilder<GccsDbContext>().UseInMemoryDatabase($"ssp-concurrency-{Guid.NewGuid():N}", root).Options;
        var tenant = Guid.NewGuid(); var actor = Guid.NewGuid(); Guid sectionId;
        await using (var setup = new GccsDbContext(options))
        {
            setup.Tenants.Add(Tenant(tenant)); await setup.SaveChangesAsync();
            sectionId = (await new EfSspSectionRepository(setup).CreateAsync(tenant,
                new CreateSspSectionRequest(SspSectionType.Environment, "Environment", "Security", [], [Source()]), actor, "tester@example.com")).Id;
        }
        await using var first = new GccsDbContext(options); await using var second = new GccsDbContext(options);
        var winner = new EfSspSectionRepository(first); var stale = new EfSspSectionRepository(second);
        var firstRead = Assert.IsType<SspSectionDto>(await winner.GetAsync(tenant, sectionId));
        var staleRead = Assert.IsType<SspSectionDto>(await stale.GetAsync(tenant, sectionId));
        var updated = await winner.UpdateAsync(tenant, sectionId,
            new UpdateSspSectionRequest(firstRead.SectionType, "Winning update", firstRead.Owner, [], firstRead.SourceReferences, firstRead.Version), actor);

        await Assert.ThrowsAsync<ContentRevisionConflictException>(() => stale.UpdateAsync(tenant, sectionId,
            new UpdateSspSectionRequest(staleRead.SectionType, "Stale update", staleRead.Owner, [], staleRead.SourceReferences, staleRead.Version), actor));
        Assert.Equal("Winning update", (await winner.GetAsync(tenant, sectionId))!.Title);
        Assert.Equal(2, updated!.Version);
    }

    private static TenantEntity Tenant(Guid id) => new()
    {
        Id = id, Name = $"Tenant {id:N}", Status = TenantStatus.Active, DataPosture = TenantDataPosture.NoCui,
        CreatedAt = DateTimeOffset.UtcNow, CreatedByUserId = Guid.NewGuid()
    };

    private static SspSourceReferenceDto Source() => new("NIST SP 800-171", "https://csrc.nist.gov/", new DateOnly(2026, 9, 1));

    private sealed record FixedTenantContext(Guid TenantId, Guid UserId) : ICurrentTenantContext
    {
        public string UserEmail => "security.owner@example.invalid";
    }

    private sealed class ThrowingAuditWriter : IAuditEventWriter
    {
        public Task WriteAsync(Guid tenantId, Guid actorUserId, Gccs.Domain.Audit.AuditAction action, string entityType, string entityId, string summary, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Synthetic audit persistence failure.");
    }
}
