using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Compliance;
using Gccs.Application.Security;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Domain.Evidence;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Common;
using Gccs.Infrastructure.Compliance;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class SspNarrativePersistenceTests
{
    [Fact]
    public async Task EF_repository_persists_narrative_sources_and_filters_every_read_by_tenant()
    {
        var root = new InMemoryDatabaseRoot();
        var options = new DbContextOptionsBuilder<GccsDbContext>().UseInMemoryDatabase($"ssp-narrative-{Guid.NewGuid():N}", root).Options;
        var tenantA = Guid.NewGuid(); var tenantB = Guid.NewGuid(); var actor = Guid.NewGuid(); Guid sectionId; Guid narrativeId;
        await using (var setup = new GccsDbContext(options))
        {
            setup.Tenants.AddRange(Tenant(tenantA), Tenant(tenantB));
            await setup.SaveChangesAsync();
            sectionId = (await new EfSspSectionRepository(setup).CreateAsync(tenantA,
                new CreateSspSectionRequest(SspSectionType.SystemDescription, "System", "Security", [], [Source()]), actor, "owner@example.invalid")).Id;
            narrativeId = (await new EfSspNarrativeRepository(setup).CreateDraftAsync(tenantA, sectionId,
                "Source-backed draft.", false, null, Classification(), [Resolved("fingerprint-1")], actor)).Id;
        }

        await using var reloaded = new GccsDbContext(options);
        var repository = new EfSspNarrativeRepository(reloaded);
        var narrative = Assert.IsType<SspNarrativeDto>(await repository.GetNarrativeAsync(tenantA, sectionId, narrativeId));
        Assert.True(narrative.DraftOnly);
        Assert.Equal("fingerprint-1", Assert.Single(narrative.SourceRecords).Fingerprint);
        Assert.Null(await repository.GetNarrativeAsync(tenantB, sectionId, narrativeId));
        Assert.Empty(await repository.ListNarrativesAsync(tenantB, sectionId));
    }

    [Fact]
    public async Task EF_source_resolver_enforces_tenant_approval_freshness_and_governed_content_state()
    {
        var options = new DbContextOptionsBuilder<GccsDbContext>().UseInMemoryDatabase($"ssp-source-{Guid.NewGuid():N}").Options;
        var tenantA = Guid.NewGuid(); var tenantB = Guid.NewGuid(); var actor = Guid.NewGuid();
        var approved = Evidence(tenantA, actor, EvidenceStatus.Approved);
        var unapproved = Evidence(tenantA, actor, EvidenceStatus.InReview);
        var otherTenant = Evidence(tenantB, actor, EvidenceStatus.Approved);
        var expired = Evidence(tenantA, actor, EvidenceStatus.Approved); expired.ExpiresAt = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        var approvedPolicy = Policy(tenantA, actor, "Approved", "[]");
        var placeholderPolicy = Policy(tenantA, actor, "Approved", "[\"system_name\"]");
        await using var db = new GccsDbContext(options);
        db.Tenants.AddRange(Tenant(tenantA), Tenant(tenantB));
        db.EvidenceItems.AddRange(approved, unapproved, otherTenant, expired);
        db.GeneratedPolicies.AddRange(approvedPolicy, placeholderPolicy);
        db.Clauses.Add(new ClauseEntity
        {
            Id = "published-clause", Number = "52.204-21", Title = "Basic Safeguarding", PlainEnglishSummary = "Apply safeguarding controls.",
            SourceUrl = "https://www.acquisition.gov/far/52.204-21", Source = "FAR", SourceName = "FAR", SourceLastReviewedAt = DateOnly.FromDateTime(DateTime.UtcNow),
            LastReviewedAt = DateOnly.FromDateTime(DateTime.UtcNow), ReviewState = ReviewState.Published, ClauseTextVersion = "current", Confidence = "high", SourceConfidence = "high"
        });
        db.Clauses.Add(new ClauseEntity
        {
            Id = "draft-clause", Number = "draft", Title = "Draft", PlainEnglishSummary = "Unreviewed.", SourceUrl = "https://example.invalid/draft",
            Source = "test", SourceName = "test", SourceLastReviewedAt = DateOnly.FromDateTime(DateTime.UtcNow), LastReviewedAt = DateOnly.FromDateTime(DateTime.UtcNow),
            ReviewState = ReviewState.Draft, ClauseTextVersion = "draft"
        });
        db.Obligations.Add(new ObligationEntity
        {
            Id = "published-obligation", Source = "FAR", Title = "Protect systems", PlainEnglishSummary = "Apply required safeguards.",
            RequiredAction = "Maintain safeguards.", SourceName = "FAR", SourceUrl = "https://www.acquisition.gov/far/52.204-21",
            SourceLastReviewedAt = DateOnly.FromDateTime(DateTime.UtcNow), LastReviewedAt = DateOnly.FromDateTime(DateTime.UtcNow),
            ReviewState = ReviewState.Published
        });
        db.Obligations.Add(new ObligationEntity
        {
            Id = "stale-obligation", Source = "FAR", Title = "Stale obligation", PlainEnglishSummary = "Outdated summary.",
            RequiredAction = "Review again.", SourceName = "FAR", SourceUrl = "https://example.invalid/stale",
            SourceLastReviewedAt = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-2), LastReviewedAt = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-2),
            NextReviewDueAt = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1), ReviewState = ReviewState.Published
        });
        await db.SaveChangesAsync();
        var resolver = new EfSspNarrativeSourceResolver(db);

        Assert.Single(await resolver.ResolveAsync(tenantA, [new(SspNarrativeSourceType.Evidence, approved.Id.ToString())]));
        Assert.Single(await resolver.ResolveAsync(tenantA, [new(SspNarrativeSourceType.GeneratedPolicy, approvedPolicy.Id.ToString())]));
        Assert.Single(await resolver.ResolveAsync(tenantA, [new(SspNarrativeSourceType.Clause, "published-clause")]));
        Assert.Single(await resolver.ResolveAsync(tenantA, [new(SspNarrativeSourceType.Obligation, "published-obligation")]));
        foreach (var link in new[]
        {
            new SspNarrativeSourceLinkRequest(SspNarrativeSourceType.Evidence, unapproved.Id.ToString()),
            new SspNarrativeSourceLinkRequest(SspNarrativeSourceType.Evidence, otherTenant.Id.ToString()),
            new SspNarrativeSourceLinkRequest(SspNarrativeSourceType.Evidence, expired.Id.ToString()),
            new SspNarrativeSourceLinkRequest(SspNarrativeSourceType.GeneratedPolicy, placeholderPolicy.Id.ToString()),
            new SspNarrativeSourceLinkRequest(SspNarrativeSourceType.Clause, "draft-clause"),
            new SspNarrativeSourceLinkRequest(SspNarrativeSourceType.Obligation, "stale-obligation")
        })
            await Assert.ThrowsAsync<SspNarrativeValidationException>(() => resolver.ResolveAsync(tenantA, [link]));
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task PostgreSQL_approval_source_lock_blocks_concurrent_source_mutation()
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION") ??
            throw new InvalidOperationException("Set GCCS_TEST_POSTGRES_CONNECTION to run this test.");
        var options = new DbContextOptionsBuilder<GccsDbContext>().UseGccsPostgres(connectionString).Options;
        var tenant = Guid.NewGuid(); var actor = Guid.NewGuid(); var evidence = Evidence(tenant, actor, EvidenceStatus.Approved);
        await using (var setup = new GccsDbContext(options))
        {
            await PostgresTestDatabase.MigrateAsync(setup);
            setup.Tenants.Add(Tenant(tenant)); setup.EvidenceItems.Add(evidence); await setup.SaveChangesAsync();
        }
        try
        {
            await using var locker = new GccsDbContext(options);
            await using var transaction = await locker.Database.BeginTransactionAsync();
            var resolver = new EfSspNarrativeSourceResolver(locker);
            Assert.Single(await resolver.ResolveAsync(tenant, [new(SspNarrativeSourceType.Evidence, evidence.Id.ToString())]));

            await using var updater = new GccsDbContext(options);
            var mutable = await updater.EvidenceItems.SingleAsync(item => item.Id == evidence.Id && item.TenantId == tenant);
            mutable.Status = EvidenceStatus.InReview;
            using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => updater.SaveChangesAsync(timeout.Token));

            await transaction.RollbackAsync();
            await using var afterRelease = new GccsDbContext(options);
            var released = await afterRelease.EvidenceItems.SingleAsync(item => item.Id == evidence.Id && item.TenantId == tenant);
            released.Status = EvidenceStatus.InReview;
            await afterRelease.SaveChangesAsync();
        }
        finally { await CleanupAsync(options, tenant); }
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task PostgreSQL_migration_persists_narratives_and_rejects_stale_edits()
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION") ??
            throw new InvalidOperationException("Set GCCS_TEST_POSTGRES_CONNECTION to run this test.");
        var options = new DbContextOptionsBuilder<GccsDbContext>().UseGccsPostgres(connectionString).Options;
        var tenant = Guid.NewGuid(); var actor = Guid.NewGuid(); Guid sectionId; Guid narrativeId;
        await using (var setup = new GccsDbContext(options))
        {
            await PostgresTestDatabase.MigrateAsync(setup);
            setup.Tenants.Add(Tenant(tenant)); await setup.SaveChangesAsync();
            sectionId = (await new EfSspSectionRepository(setup).CreateAsync(tenant,
                new CreateSspSectionRequest(SspSectionType.Environment, "Environment", "Security", [], [Source()]), actor, "owner@example.invalid")).Id;
            narrativeId = (await new EfSspNarrativeRepository(setup).CreateDraftAsync(tenant, sectionId,
                "Persistent draft.", false, null, Classification(), [Resolved("pg-v1")], actor)).Id;
        }
        try
        {
            await using var first = new GccsDbContext(options); await using var second = new GccsDbContext(options);
            var winner = new EfSspNarrativeRepository(first); var stale = new EfSspNarrativeRepository(second);
            var firstRead = (await winner.GetNarrativeAsync(tenant, sectionId, narrativeId))!;
            var staleRead = (await stale.GetNarrativeAsync(tenant, sectionId, narrativeId))!;
            var updated = await winner.UpdateDraftAsync(tenant, sectionId, narrativeId,
                Edit("Winning text.", firstRead.Version), Classification(), actor);
            await Assert.ThrowsAsync<ContentRevisionConflictException>(() => stale.UpdateDraftAsync(tenant, sectionId, narrativeId,
                Edit("Stale text.", staleRead.Version), Classification(), actor));
            Assert.Equal("Winning text.", updated!.EditedText);
            Assert.Equal("Winning text.", (await winner.GetNarrativeAsync(tenant, sectionId, narrativeId))!.EditedText);

            var proposed = await winner.CreateDraftAsync(tenant, sectionId,
                "Replacement draft.", false, null, Classification(), [Resolved("pg-v2")], actor);
            var firstApproved = await winner.ApproveAsync(tenant, sectionId, narrativeId,
                new ApproveSspNarrativeRequest(DateOnly.FromDateTime(DateTime.UtcNow), updated.Version), actor, "reviewer@example.invalid");
            Assert.Equal(SspNarrativeStatus.Approved, firstApproved!.Status);
            var replacementApproved = await winner.ApproveAsync(tenant, sectionId, proposed.Id,
                new ApproveSspNarrativeRequest(DateOnly.FromDateTime(DateTime.UtcNow), proposed.Version), actor, "reviewer@example.invalid");
            Assert.Equal(SspNarrativeStatus.Approved, replacementApproved!.Status);

            var persisted = await winner.ListNarrativesAsync(tenant, sectionId);
            Assert.Equal(SspNarrativeStatus.Superseded, persisted.Single(item => item.Id == narrativeId).Status);
            Assert.Equal(proposed.Id, Assert.Single(persisted, item => item.Status == SspNarrativeStatus.Approved).Id);
        }
        finally { await CleanupAsync(options, tenant); }
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task PostgreSQL_generated_policy_audit_failure_rolls_back_classified_generation()
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION") ??
            throw new InvalidOperationException("Set GCCS_TEST_POSTGRES_CONNECTION to run this test.");
        var tenant = Guid.NewGuid(); var actor = Guid.NewGuid(); var templateId = Guid.NewGuid();
        var services = new ServiceCollection();
        services.AddDbContext<GccsDbContext>(options => options.UseGccsPostgres(connectionString));
        services.AddScoped<IPolicyTemplateRepository, EfPolicyTemplateRepository>();
        services.AddScoped<IApplicationTransaction, EfApplicationTransaction>();
        services.AddScoped<TenantDataHandlingModePolicyService>();
        services.AddScoped<ContentClassificationPolicy>();
        services.AddScoped<PolicyTemplateService>();
        services.AddSingleton<ICurrentTenantContext>(new FixedTenantContext(tenant, actor));
        services.AddSingleton<ICurrentDataHandlingNoticeGuard, AcknowledgedNoticeGuard>();
        services.AddSingleton<IAuditEventWriter, ThrowingAuditWriter>();
        await using var provider = services.BuildServiceProvider();
        await using (var setupScope = provider.CreateAsyncScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<GccsDbContext>();
            await PostgresTestDatabase.MigrateAsync(db); db.Tenants.Add(Tenant(tenant));
            db.PolicyTemplates.Add(new PolicyTemplateEntity
            {
                Id = templateId, TenantId = tenant, Title = "Approved policy", Category = "Security", Body = "Approved body.",
                PlaceholdersJson = "[]", SourceReferencesJson = "[]", Version = "1.0", Status = PolicyTemplateStatus.Approved.ToString(),
                OwnerFunction = "Security", CreatedAt = DateTimeOffset.UtcNow, CreatedByUserId = actor
            });
            await db.SaveChangesAsync();
        }
        try
        {
            await using var scope = provider.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<PolicyTemplateService>();
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateDraftPolicyAsync(templateId,
                new GenerateDraftPolicyRequest(new ContentClassificationRequest(ContentClassification.Unclassified, ContentClassificationSource.UserSelected)), actor));
            var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>(); db.ChangeTracker.Clear();
            Assert.False(await db.GeneratedPolicies.AnyAsync(policy => policy.TenantId == tenant));
        }
        finally { await CleanupAsync(new DbContextOptionsBuilder<GccsDbContext>().UseGccsPostgres(connectionString).Options, tenant); }
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task PostgreSQL_audit_failure_rolls_back_narrative_generation()
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION") ??
            throw new InvalidOperationException("Set GCCS_TEST_POSTGRES_CONNECTION to run this test.");
        var tenant = Guid.NewGuid(); var actor = Guid.NewGuid(); var evidence = Evidence(tenant, actor, EvidenceStatus.Approved); Guid sectionId;
        var services = new ServiceCollection();
        services.AddDbContext<GccsDbContext>(options => options.UseGccsPostgres(connectionString));
        services.AddScoped<ISspSectionRepository, EfSspSectionRepository>();
        services.AddScoped<ISspNarrativeRepository, EfSspNarrativeRepository>();
        services.AddScoped<ISspNarrativeSourceResolver, EfSspNarrativeSourceResolver>();
        services.AddSingleton<ISspNarrativeAiGenerator, UnavailableSspNarrativeAiGenerator>();
        services.AddScoped<IApplicationTransaction, EfApplicationTransaction>();
        services.AddScoped<TenantDataHandlingModePolicyService>();
        services.AddScoped<ContentClassificationPolicy>();
        services.AddScoped<SspNarrativeService>();
        services.AddSingleton<ICurrentTenantContext>(new FixedTenantContext(tenant, actor));
        services.AddSingleton<ICurrentDataHandlingNoticeGuard, AcknowledgedNoticeGuard>();
        services.AddSingleton<IAuditEventWriter, ThrowingAuditWriter>();
        await using var provider = services.BuildServiceProvider();
        await using (var setupScope = provider.CreateAsyncScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<GccsDbContext>();
            await PostgresTestDatabase.MigrateAsync(db); db.Tenants.Add(Tenant(tenant)); db.EvidenceItems.Add(evidence); await db.SaveChangesAsync();
            sectionId = (await new EfSspSectionRepository(db).CreateAsync(tenant,
                new CreateSspSectionRequest(SspSectionType.EvidenceReferences, "Evidence", "Security", [], [Source()]), actor, "owner@example.invalid")).Id;
        }
        try
        {
            await using var scope = provider.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<SspNarrativeService>();
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateAsync(sectionId,
                new GenerateSspNarrativeDraftRequest([new(SspNarrativeSourceType.Evidence, evidence.Id.ToString())])));
            var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>(); db.ChangeTracker.Clear();
            Assert.False(await db.SspNarratives.AnyAsync(x => x.TenantId == tenant));
            Assert.False(await db.SspNarrativeSources.AnyAsync(x => x.TenantId == tenant));
        }
        finally
        {
            var options = new DbContextOptionsBuilder<GccsDbContext>().UseGccsPostgres(connectionString).Options;
            await CleanupAsync(options, tenant);
        }
    }

    private static TenantEntity Tenant(Guid id) => new()
    {
        Id = id, Name = $"Tenant {id:N}", Status = TenantStatus.Active, DataPosture = TenantDataPosture.NoCui,
        CreatedAt = DateTimeOffset.UtcNow, CreatedByUserId = Guid.NewGuid()
    };

    private static EvidenceItemEntity Evidence(Guid tenant, Guid actor, EvidenceStatus status) => new()
    {
        Id = Guid.NewGuid(), TenantId = tenant, Name = "Approved evidence", Description = "MFA is enforced.", Type = EvidenceType.SystemConfiguration,
        OwnerFunction = "Security", Status = status, ApprovedAt = status == EvidenceStatus.Approved ? DateTimeOffset.UtcNow : null,
        ApprovedByUserId = status == EvidenceStatus.Approved ? actor : null, Classification = ContentClassification.Unclassified,
        CreatedAt = DateTimeOffset.UtcNow, CreatedByUserId = actor
    };

    private static GeneratedPolicyEntity Policy(Guid tenant, Guid actor, string status, string missingPlaceholders) => new()
    {
        Id = Guid.NewGuid(), TenantId = tenant, SourceTemplateId = Guid.NewGuid(), SourceTemplateVersion = "1.0",
        GeneratedAt = DateTimeOffset.UtcNow, Title = "Approved security policy", Body = "Policy body.", Status = status,
        ApprovedAt = status == "Approved" ? DateTimeOffset.UtcNow : null,
        ApprovedByUserId = status == "Approved" ? actor : null,
        MissingPlaceholdersJson = missingPlaceholders, Classification = ContentClassification.Unclassified,
        ClassificationSource = ContentClassificationSource.UserSelected, ClassificationRevision = 1,
        CreatedAt = DateTimeOffset.UtcNow, CreatedByUserId = actor
    };

    private static SspSourceReferenceDto Source() => new("NIST SP 800-171", "https://csrc.nist.gov/", DateOnly.FromDateTime(DateTime.UtcNow));
    private static ContentClassificationDto Classification() => new(ContentClassification.Unclassified, ContentClassificationSource.SystemSuggested, null, null, null, "test", false);
    private static ResolvedSspNarrativeSource Resolved(string fingerprint) => new(SspNarrativeSourceType.Evidence, Guid.NewGuid().ToString(), "Evidence", "MFA is enforced.", "/evidence", fingerprint, ContentClassification.Unclassified);
    private static EditSspNarrativeDraftRequest Edit(string text, long version) => new(text, null, new ContentClassificationRequest(ContentClassification.Unclassified), version);

    private static async Task CleanupAsync(DbContextOptions<GccsDbContext> options, Guid tenant)
    {
        await using var cleanup = new GccsDbContext(options);
        var sections = await cleanup.SspSections.Where(x => x.TenantId == tenant).ToArrayAsync();
        if (sections.Length > 0) { cleanup.SspSections.RemoveRange(sections); await cleanup.SaveChangesAsync(); }
        var evidence = await cleanup.EvidenceItems.Where(x => x.TenantId == tenant).ToArrayAsync();
        if (evidence.Length > 0) { cleanup.EvidenceItems.RemoveRange(evidence); await cleanup.SaveChangesAsync(); }
        var policies = await cleanup.PolicyTemplates.Where(x => x.TenantId == tenant).ToArrayAsync();
        if (policies.Length > 0) { cleanup.PolicyTemplates.RemoveRange(policies); await cleanup.SaveChangesAsync(); }
        var entity = await cleanup.Tenants.SingleOrDefaultAsync(x => x.Id == tenant);
        if (entity is not null) { cleanup.Tenants.Remove(entity); await cleanup.SaveChangesAsync(); }
    }

    private sealed record FixedTenantContext(Guid TenantId, Guid UserId) : ICurrentTenantContext
    {
        public string UserEmail => "reviewer@example.invalid";
    }

    private sealed class AcknowledgedNoticeGuard : ICurrentDataHandlingNoticeGuard
    {
        public Task EnsureAsync(string workflow, Guid actorUserId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class ThrowingAuditWriter : IAuditEventWriter
    {
        public Task WriteAsync(Guid tenantId, Guid actorUserId, AuditAction action, string entityType, string entityId, string summary, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Synthetic audit persistence failure.");
    }
}
