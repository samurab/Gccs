using Gccs.Application.Audit;
using Gccs.Application.Companies;
using Gccs.Application.Security;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Domain.Companies;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Companies;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class CompanySizeEvaluationTests
{
    private static readonly Guid TenantId = Guid.Parse("23223223-2232-2322-3223-2322322323a1");
    private static readonly Guid OtherTenantId = Guid.Parse("23223223-2232-2322-3223-2322322323b1");
    private static readonly Guid ActorUserId = Guid.Parse("23223223-2232-2322-3223-2322322323c1");

    [Fact]
    public async Task TC_23_2_1_TC_23_2_2_and_TC_23_2_3_Evaluates_with_approved_records_and_source_context()
    {
        await using var dbContext = CreateDbContext();
        SeedTenant(dbContext, TenantId);
        SeedSizeStandard(dbContext, "541511", ReviewState.Draft, 1m);
        SeedSizeStandard(dbContext, "541511", ReviewState.Approved, 34_000_000m);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, TenantId, new CapturingAuditEventWriter());

        var missing = await service.EvaluateCurrentTenantAsync(new CompanySizeEvaluationRequest("541511", null, null));
        var evaluated = await service.EvaluateCurrentTenantAsync(new CompanySizeEvaluationRequest("541511", 20_000_000m, null));

        Assert.Equal("InsufficientInformation", missing.Result);
        Assert.Equal("LikelySmall", evaluated.Result);
        Assert.Equal("541511", evaluated.NaicsCode);
        Assert.Equal("Receipts", evaluated.Metric);
        Assert.Equal(34_000_000m, evaluated.Threshold);
        Assert.Equal(20_000_000m, evaluated.EnteredValue);
        Assert.Equal("https://www.sba.gov/document/support-table-size-standards", evaluated.SourceUrl);
        Assert.True(evaluated.RunAt > DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task TC_23_2_4_and_TC_23_2_5_Saves_results_to_current_profile_and_audits()
    {
        await using var dbContext = CreateDbContext();
        SeedTenant(dbContext, TenantId);
        SeedTenant(dbContext, OtherTenantId);
        SeedCompany(dbContext, TenantId, "541511");
        SeedCompany(dbContext, OtherTenantId, "541511");
        SeedSizeStandard(dbContext, "541511", ReviewState.Approved, 34_000_000m);
        await dbContext.SaveChangesAsync();
        var auditWriter = new CapturingAuditEventWriter();
        var service = CreateService(dbContext, TenantId, auditWriter);
        var result = await service.EvaluateCurrentTenantAsync(new CompanySizeEvaluationRequest("541511", 40_000_000m, null));

        var saved = await service.SaveCurrentTenantAsync(result, ActorUserId);

        Assert.NotNull(saved);
        Assert.Equal("OtherThanSmall", saved.Result);
        var tenantProfile = await dbContext.CompanyProfiles.Include(profile => profile.NaicsCodes).SingleAsync(profile => profile.TenantId == TenantId);
        var otherProfile = await dbContext.CompanyProfiles.Include(profile => profile.NaicsCodes).SingleAsync(profile => profile.TenantId == OtherTenantId);
        Assert.False(tenantProfile.NaicsCodes.Single().QualifiesAsSmall);
        Assert.Null(otherProfile.NaicsCodes.Single().QualifiesAsSmall);
        var audit = Assert.Single(auditWriter.Events);
        Assert.Equal("CompanySizeEvaluation", audit.EntityType);
        Assert.Equal("OtherThanSmall", audit.Metadata["result"]);
    }

    private static CompanySizeEvaluationService CreateService(
        GccsDbContext dbContext,
        Guid tenantId,
        IAuditEventWriter auditWriter) =>
        new(new EfCompanySizeEvaluationRepository(dbContext, new FixedTenantContext(tenantId)), auditWriter);

    private static void SeedTenant(GccsDbContext dbContext, Guid tenantId)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = $"Tenant {tenantId:N}",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private static void SeedCompany(GccsDbContext dbContext, Guid tenantId, string naicsCode)
    {
        var profileId = Guid.NewGuid();
        dbContext.CompanyProfiles.Add(new CompanyProfileEntity
        {
            Id = profileId,
            TenantId = tenantId,
            LegalEntityName = $"Company {tenantId:N}",
            AgencyCustomersJson = "[]",
            KeySystemsJson = "[]",
            ContractorRole = ContractorRole.Unknown,
            EmployeeRange = CompanyRange.Unknown,
            RevenueRange = CompanyRange.Unknown,
            DataHandlingPosture = DataHandlingPosture.Unknown,
            CreatedAt = DateTimeOffset.UtcNow,
            NaicsCodes =
            [
                new CompanyNaicsCodeEntity
                {
                    Id = Guid.NewGuid(),
                    CompanyProfileId = profileId,
                    Code = naicsCode,
                    Title = "Custom Computer Programming Services",
                    IsPrimary = true
                }
            ]
        });
    }

    private static void SeedSizeStandard(GccsDbContext dbContext, string naicsCode, ReviewState status, decimal threshold)
    {
        dbContext.SbaSizeStandards.Add(new SbaSizeStandardEntity
        {
            Id = Guid.NewGuid(),
            NaicsCode = naicsCode,
            Metric = "Receipts",
            Threshold = threshold,
            Unit = "USD",
            SourceUrl = "https://www.sba.gov/document/support-table-size-standards",
            EffectiveAt = new DateOnly(2026, 1, 1),
            LastReviewedAt = new DateOnly(2026, 6, 17),
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = ActorUserId
        });
    }

    private static GccsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GccsDbContext>()
            .UseInMemoryDatabase($"company-size-evaluation-{Guid.NewGuid():N}")
            .Options;
        return new GccsDbContext(options);
    }

    private sealed class FixedTenantContext(Guid tenantId) : ICurrentTenantContext
    {
        public Guid TenantId { get; } = tenantId;
        public Guid UserId => ActorUserId;
        public string UserEmail => "actor@example.test";
    }

    private sealed class CapturingAuditEventWriter : IAuditEventWriter
    {
        public List<CapturedAuditEvent> Events { get; } = [];

        public Task WriteAsync(
            Guid tenantId,
            Guid actorUserId,
            AuditAction action,
            string entityType,
            string entityId,
            string summary,
            IReadOnlyDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            Events.Add(new CapturedAuditEvent(tenantId, actorUserId, action, entityType, entityId, summary, metadata ?? new Dictionary<string, string>()));
            return Task.CompletedTask;
        }
    }

    private sealed record CapturedAuditEvent(
        Guid TenantId,
        Guid ActorUserId,
        AuditAction Action,
        string EntityType,
        string EntityId,
        string Summary,
        IReadOnlyDictionary<string, string> Metadata);
}
