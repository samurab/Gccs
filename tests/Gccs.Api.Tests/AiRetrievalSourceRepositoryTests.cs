using Gccs.Application.Ai;
using Gccs.Application.Reports;
using Gccs.Application.Security;
using Gccs.Domain.Companies;
using Gccs.Domain.Common;
using Gccs.Domain.Contracts;
using Gccs.Domain.Evidence;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Ai;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class AiRetrievalSourceRepositoryTests
{
    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task PostgreSql_query_returns_only_safe_approved_current_tenant_metadata()
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")!;
        var ids = TestIds.Create();
        var options = new DbContextOptionsBuilder<GccsDbContext>().UseGccsPostgres(connectionString).Options;
        await using var db = new GccsDbContext(options);
        await PostgresTestDatabase.MigrateAsync(db);
        db.Tenants.AddRange(Tenant(ids.TenantId, "AI retrieval PostgreSQL tenant"), Tenant(ids.OtherTenantId, "AI retrieval other tenant"));
        db.EvidenceItems.AddRange(
            Evidence(ids.ApprovedEvidenceId, ids.TenantId, ContentClassification.Fci),
            Evidence(ids.UnsafeEvidenceId, ids.TenantId, ContentClassification.SyntheticCui),
            Evidence(Guid.NewGuid(), ids.OtherTenantId, ContentClassification.Unclassified));
        await db.SaveChangesAsync();

        try
        {
            var repository = new EfAiRetrievalSourceRepository(db, new FixedTenantContext(ids.TenantId, ids.ActorUserId));
            var batch = await repository.SearchSourcesAsync(new AiRetrievalSourceQuery(
                ids.TenantId,
                "evidence",
                "evidence",
                AiRetrievalSourceAccess.EvidenceMetadata,
                10));

            var source = Assert.Single(batch.Sources);
            Assert.Equal($"evidence-metadata:{ids.ApprovedEvidenceId}", source.Id);
            Assert.Equal(ids.TenantId, source.TenantId);
        }
        finally
        {
            await db.EvidenceItems.Where(item => item.TenantId == ids.TenantId || item.TenantId == ids.OtherTenantId)
                .ExecuteDeleteAsync();
            await db.Tenants.Where(tenant => tenant.Id == ids.TenantId || tenant.Id == ids.OtherTenantId)
                .ExecuteDeleteAsync();
        }
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task PostgreSql_full_text_search_finds_an_older_relevant_source_beyond_the_family_limit_and_uses_gin_index()
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")!;
        var ids = TestIds.Create();
        var prefix = $"fts-{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<GccsDbContext>().UseGccsPostgres(connectionString).Options;
        await using var db = new GccsDbContext(options);
        await PostgresTestDatabase.MigrateAsync(db);
        db.Tenants.Add(Tenant(ids.TenantId, "AI retrieval ranking tenant"));
        var target = Obligation($"{prefix}-target", ReviewState.Published);
        target.Title = "Quasarfabric retention requirement";
        target.PlainEnglishSummary = "Quasarfabric records require documented retention.";
        target.LastReviewedAt = new DateOnly(2020, 1, 1);
        db.Obligations.Add(target);
        db.Obligations.AddRange(Enumerable.Range(0, 40).Select(index =>
        {
            var obligation = Obligation($"{prefix}-newer-{index:D2}", ReviewState.Published);
            obligation.Title = $"Unrelated current obligation {index}";
            obligation.PlainEnglishSummary = "A recently reviewed source about unrelated purchasing controls.";
            obligation.LastReviewedAt = new DateOnly(2026, 9, 1);
            return obligation;
        }));
        await db.SaveChangesAsync();

        try
        {
            var repository = new EfAiRetrievalSourceRepository(db, new FixedTenantContext(ids.TenantId, ids.ActorUserId));
            var batch = await repository.SearchSourcesAsync(new AiRetrievalSourceQuery(
                ids.TenantId,
                "What is the quasarfabric retention requirement?",
                "compliance",
                AiRetrievalSourceAccess.ComplianceLibrary,
                AiRetrievalAssistantService.MaximumCandidateCount));

            Assert.Contains(batch.Sources, source => source.Id == $"obligation:{prefix}-target");
            Assert.DoesNotContain(batch.Sources, source => source.Id.StartsWith($"obligation:{prefix}-newer", StringComparison.Ordinal));

            await db.Database.OpenConnectionAsync();
            await using var disableSequentialScan = db.Database.GetDbConnection().CreateCommand();
            disableSequentialScan.CommandText = "SET enable_seqscan = off";
            await disableSequentialScan.ExecuteNonQueryAsync();
            await using var explain = db.Database.GetDbConnection().CreateCommand();
            explain.CommandText = "EXPLAIN SELECT id FROM gccs.obligations WHERE search_vector @@ websearch_to_tsquery('english', 'quasarfabric')";
            await using var reader = await explain.ExecuteReaderAsync();
            var plan = new List<string>();
            while (await reader.ReadAsync())
                plan.Add(reader.GetString(0));
            Assert.Contains(plan, line => line.Contains("IX_obligations_search_vector", StringComparison.Ordinal));
        }
        finally
        {
            await db.Obligations.Where(item => item.Id.StartsWith(prefix)).ExecuteDeleteAsync();
            await db.Tenants.Where(tenant => tenant.Id == ids.TenantId).ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task Approved_source_families_are_resolved_from_authoritative_records()
    {
        var ids = TestIds.Create();
        await using var db = CreateDatabase(ids);
        var repository = new EfAiRetrievalSourceRepository(db, new FixedTenantContext(ids.TenantId, ids.ActorUserId));

        var batch = await repository.SearchSourcesAsync(new AiRetrievalSourceQuery(
            ids.TenantId,
            "FAR OR contract OR document OR evidence OR report",
            "contract",
            AiRetrievalSourceAccess.All,
            AiRetrievalAssistantService.MaximumCandidateCount));

        Assert.Equal(TenantDataPosture.NoCui, batch.DataPosture);
        Assert.Contains(batch.Sources, source => source.Id == "obligation:published-obligation");
        Assert.Contains(batch.Sources, source => source.Id == $"contract-document-excerpt:{ids.AcceptedCandidateId}");
        Assert.Contains(batch.Sources, source => source.Id == $"approved-report:{ids.ApprovedReportId}");
        Assert.Contains(batch.Sources, source => source.Id == $"evidence-metadata:{ids.ApprovedEvidenceId}");
        Assert.Equal(
            [AiRetrievalSourceKind.ComplianceLibrary, AiRetrievalSourceKind.TenantDocument,
                AiRetrievalSourceKind.ApprovedReport, AiRetrievalSourceKind.EvidenceMetadata],
            batch.Sources.Take(4).Select(source => source.SourceKind));
    }

    [Fact]
    public async Task Unsafe_unapproved_blocked_expired_and_cross_tenant_records_are_not_materialized()
    {
        var ids = TestIds.Create();
        await using var db = CreateDatabase(ids);
        var repository = new EfAiRetrievalSourceRepository(db, new FixedTenantContext(ids.TenantId, ids.ActorUserId));

        var batch = await repository.SearchSourcesAsync(new AiRetrievalSourceQuery(
            ids.TenantId,
            "all sources",
            "evidence",
            AiRetrievalSourceAccess.All,
            AiRetrievalAssistantService.MaximumCandidateCount));

        Assert.DoesNotContain(batch.Sources, source => source.Id.Contains("draft-obligation", StringComparison.Ordinal));
        Assert.DoesNotContain(batch.Sources, source => source.Id.Contains(ids.OtherTenantId.ToString(), StringComparison.Ordinal));
        Assert.DoesNotContain(batch.Sources, source => source.Id.Contains(ids.UnsafeEvidenceId.ToString(), StringComparison.Ordinal));
        Assert.DoesNotContain(batch.Sources, source => source.Id.Contains(ids.ExpiredEvidenceId.ToString(), StringComparison.Ordinal));
        Assert.DoesNotContain(batch.Sources, source => source.Id.Contains(ids.BlockedEvidenceId.ToString(), StringComparison.Ordinal));
        Assert.DoesNotContain(batch.Sources, source => source.Id.Contains(ids.DraftReportId.ToString(), StringComparison.Ordinal));
    }

    [Fact]
    public async Task Source_family_access_is_applied_before_database_queries_return_content()
    {
        var ids = TestIds.Create();
        await using var db = CreateDatabase(ids);
        var repository = new EfAiRetrievalSourceRepository(db, new FixedTenantContext(ids.TenantId, ids.ActorUserId));

        var batch = await repository.SearchSourcesAsync(new AiRetrievalSourceQuery(
            ids.TenantId,
            "evidence",
            "evidence",
            AiRetrievalSourceAccess.EvidenceMetadata,
            AiRetrievalAssistantService.MaximumCandidateCount));

        var source = Assert.Single(batch.Sources);
        Assert.Equal(AiRetrievalSourceKind.EvidenceMetadata, source.SourceKind);
        Assert.Equal(ids.TenantId, source.TenantId);
    }

    [Fact]
    public async Task Repository_rejects_a_tenant_scope_mismatch()
    {
        var ids = TestIds.Create();
        await using var db = CreateDatabase(ids);
        var repository = new EfAiRetrievalSourceRepository(db, new FixedTenantContext(ids.TenantId, ids.ActorUserId));

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.SearchSourcesAsync(new AiRetrievalSourceQuery(
            ids.OtherTenantId,
            "evidence",
            "evidence",
            AiRetrievalSourceAccess.All,
            10)));
    }

    private static GccsDbContext CreateDatabase(TestIds ids)
    {
        var options = new DbContextOptionsBuilder<GccsDbContext>()
            .UseInMemoryDatabase($"ai-retrieval-{Guid.NewGuid():N}")
            .Options;
        var db = new GccsDbContext(options);
        db.Database.EnsureCreated();
        db.Tenants.AddRange(
            Tenant(ids.TenantId, "Current tenant"),
            Tenant(ids.OtherTenantId, "Other tenant"));
        db.Obligations.AddRange(
            Obligation("published-obligation", ReviewState.Published),
            Obligation("draft-obligation", ReviewState.Approved));

        var currentContract = Contract(ids.ContractId, ids.TenantId, "CURRENT");
        var otherContract = Contract(ids.OtherContractId, ids.OtherTenantId, "OTHER");
        db.Contracts.AddRange(currentContract, otherContract);
        AddDocumentCandidate(db, ids, currentContract, ids.DocumentId, ids.JobId, ids.AcceptedCandidateId, ContentClassification.Fci, "accepted");
        AddDocumentCandidate(db, ids, otherContract, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ContentClassification.Unclassified, "accepted");

        db.SprReportPackages.AddRange(
            Report(ids.ApprovedReportId, ids.TenantId, ids.ContractId, EsrsReportPackageStatus.Approved),
            Report(ids.DraftReportId, ids.TenantId, ids.ContractId, EsrsReportPackageStatus.Draft),
            Report(Guid.NewGuid(), ids.OtherTenantId, ids.OtherContractId, EsrsReportPackageStatus.Approved));

        db.EvidenceItems.AddRange(
            Evidence(ids.ApprovedEvidenceId, ids.TenantId, ContentClassification.Unclassified),
            Evidence(ids.UnsafeEvidenceId, ids.TenantId, ContentClassification.SyntheticCui),
            Evidence(ids.ExpiredEvidenceId, ids.TenantId, ContentClassification.Fci, expiresAt: new DateOnly(2020, 1, 1)),
            Evidence(ids.BlockedEvidenceId, ids.TenantId, ContentClassification.Fci, isUseBlocked: true),
            Evidence(Guid.NewGuid(), ids.OtherTenantId, ContentClassification.Unclassified));
        db.SaveChanges();
        return db;
    }

    private static TenantEntity Tenant(Guid id, string name) => new()
    {
        Id = id,
        Name = name,
        Status = TenantStatus.Active,
        DataPosture = TenantDataPosture.NoCui,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static ObligationEntity Obligation(string id, ReviewState reviewState) => new()
    {
        Id = id,
        Source = "FAR 52.204-21",
        Title = "Basic safeguarding",
        PlainEnglishSummary = "FCI systems require basic safeguarding controls.",
        RequiredAction = "Apply the published safeguards.",
        SourceName = "Acquisition.gov",
        SourceUrl = "https://www.acquisition.gov/far/52.204-21",
        SourceLastReviewedAt = new DateOnly(2026, 9, 1),
        LastReviewedAt = new DateOnly(2026, 9, 1),
        ReviewState = reviewState
    };

    private static ContractEntity Contract(Guid id, Guid tenantId, string number) => new()
    {
        Id = id,
        TenantId = tenantId,
        ContractNumber = number,
        Title = $"{number} contract",
        AgencyOrPrimeName = "Synthetic prime",
        Relationship = ContractorRelationship.Prime,
        Kind = ContractKind.FixedPrice,
        Status = ContractStatus.Active,
        PeriodOfPerformanceStart = new DateOnly(2026, 1, 1),
        PeriodOfPerformanceEnd = new DateOnly(2027, 1, 1),
        PlaceOfPerformance = "Remote",
        DataHandlingPosture = DataHandlingPosture.FciOnly,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static void AddDocumentCandidate(
        GccsDbContext db,
        TestIds ids,
        ContractEntity contract,
        Guid documentId,
        Guid jobId,
        Guid candidateId,
        ContentClassification classification,
        string reviewStatus)
    {
        var document = new ContractDocumentEntity
        {
            Id = documentId,
            ContractId = contract.Id,
            Type = ContractDocumentType.Contract,
            FileName = $"{contract.ContractNumber}.txt",
            ContentType = "text/plain",
            SizeBytes = 100,
            ExtractedTextHash = new string('a', 64),
            ValidationStatus = "accepted",
            MalwareScanStatus = "clean",
            NoticeVersion = "no-cui-mvp-v1",
            UploadedAt = DateTimeOffset.UtcNow,
            UploadedByUserId = ids.ActorUserId,
            Classification = classification
        };
        var job = new ExtractionJobEntity
        {
            Id = jobId,
            TenantId = contract.TenantId,
            SourceDocumentId = documentId,
            RequestedByUserId = ids.ActorUserId,
            Status = ExtractionJobStatus.Completed,
            RequestedAt = DateTimeOffset.UtcNow,
            CompletedAt = DateTimeOffset.UtcNow,
            Classification = classification
        };
        var candidate = new ClauseCandidateEntity
        {
            Id = candidateId,
            TenantId = contract.TenantId,
            ExtractionJobId = jobId,
            SourceDocumentId = documentId,
            NormalizedCitation = "FAR 52.204-21",
            RawExtractedText = "The contractor shall apply basic safeguarding requirements.",
            Confidence = 1m,
            LocationMetadata = "page 1",
            MatchMethod = "exact",
            ReviewStatus = reviewStatus,
            ReviewedByUserId = ids.ActorUserId,
            ReviewedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Set<ContractDocumentEntity>().Add(document);
        db.Set<ExtractionJobEntity>().Add(job);
        db.Set<ClauseCandidateEntity>().Add(candidate);
    }

    private static SprReportPackageEntity Report(
        Guid id,
        Guid tenantId,
        Guid contractId,
        EsrsReportPackageStatus status) => new()
    {
        Id = id,
        TenantId = tenantId,
        ContractId = contractId,
        ReportType = EsrsReportType.Ssr,
        PeriodStart = new DateOnly(2026, 1, 1),
        PeriodEnd = new DateOnly(2026, 12, 31),
        Status = status,
        Version = 1,
        NotSubmittedDisclaimer = "Preparation only.",
        ReviewerUserId = status == EsrsReportPackageStatus.Approved ? Guid.NewGuid() : null,
        ApprovedAt = status == EsrsReportPackageStatus.Approved ? DateTimeOffset.UtcNow : null,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static EvidenceItemEntity Evidence(
        Guid id,
        Guid tenantId,
        ContentClassification classification,
        DateOnly? expiresAt = null,
        bool isUseBlocked = false) => new()
    {
        Id = id,
        TenantId = tenantId,
        Name = $"Evidence {id:N}",
        Description = "Approved non-CUI evidence metadata.",
        Type = EvidenceType.Policy,
        OwnerFunction = "Security",
        Status = EvidenceStatus.Approved,
        ApprovedByUserId = Guid.NewGuid(),
        ApprovedAt = DateTimeOffset.UtcNow,
        ExpiresAt = expiresAt,
        Classification = classification,
        IsUseBlocked = isUseBlocked,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private sealed class FixedTenantContext(Guid tenantId, Guid userId) : ICurrentTenantContext
    {
        public Guid TenantId { get; } = tenantId;
        public Guid UserId { get; } = userId;
        public string UserEmail => "assistant-user@example.test";
    }

    private sealed record TestIds(
        Guid TenantId,
        Guid OtherTenantId,
        Guid ActorUserId,
        Guid ContractId,
        Guid OtherContractId,
        Guid DocumentId,
        Guid JobId,
        Guid AcceptedCandidateId,
        Guid ApprovedReportId,
        Guid DraftReportId,
        Guid ApprovedEvidenceId,
        Guid UnsafeEvidenceId,
        Guid ExpiredEvidenceId,
        Guid BlockedEvidenceId)
    {
        public static TestIds Create() => new(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
    }
}
