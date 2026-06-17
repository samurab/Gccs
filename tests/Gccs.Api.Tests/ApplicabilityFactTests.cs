using Gccs.Application.Compliance;
using Gccs.Domain.Companies;
using Gccs.Domain.Common;
using Gccs.Domain.Contracts;
using Gccs.Domain.Tenancy;
using Gccs.Domain.Vendors;
using Gccs.Infrastructure.Compliance;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ApplicabilityFactTests
{
    [Fact]
    public async Task TC_21_1_1_TC_21_1_3_and_TC_21_1_4_Derives_tenant_scoped_facts_with_provenance()
    {
        await using var dbContext = CreateDbContext();
        var tenantAId = Guid.Parse("21121121-1211-2112-1121-1211211211a1");
        var tenantBId = Guid.Parse("21121121-1211-2112-1121-1211211211b1");
        var contractId = Guid.Parse("21121121-1211-2112-1121-1211211211c1");
        var clauseId = Guid.Parse("21121121-1211-2112-1121-1211211211d1");
        var subcontractorId = Guid.Parse("21121121-1211-2112-1121-1211211211e1");
        SeedTenant(dbContext, tenantAId);
        SeedTenant(dbContext, tenantBId);
        dbContext.CompanyProfiles.Add(CreateCompany(tenantAId));
        dbContext.CompanyProfiles.Add(CreateCompany(tenantBId));
        dbContext.Contracts.Add(CreateContract(tenantAId, contractId));
        dbContext.Set<ContractClauseEntity>().Add(CreateClause(contractId, clauseId));
        dbContext.Subcontractors.Add(CreateSubcontractor(tenantAId, subcontractorId));
        await dbContext.SaveChangesAsync();
        var service = new ApplicabilityFactService(new EfApplicabilityFactRepository(dbContext));

        var facts = await service.ListAsync(new ApplicabilityFactQuery(tenantAId, contractId, clauseId, subcontractorId));

        Assert.Contains(facts, fact => fact.Key == "company.naics" && fact.Value == "541511" && fact.SourceType == "CompanyNaicsCode");
        Assert.Contains(facts, fact => fact.Key == "company.certification" && fact.Value == "Wosb");
        Assert.Contains(facts, fact => fact.Key == "contract.agency" && fact.Value == "Defense Logistics Agency");
        Assert.Contains(facts, fact => fact.Key == "contract.data_type" && fact.Value == "FciOnly");
        Assert.Contains(facts, fact => fact.Key == "clause.citation" && fact.Value == "52.204-21");
        Assert.Contains(facts, fact => fact.Key == "subcontractor.has_cui_access" && fact.Value == "True");
        Assert.All(facts, fact => Assert.Equal(tenantAId, fact.TenantId));
        Assert.DoesNotContain(facts, fact => fact.SourceId == tenantBId.ToString());
        Assert.Contains(facts, fact => fact.LastUpdatedAt is not null);
    }

    [Fact]
    public async Task TC_21_1_2_Unknown_facts_are_explicit()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.Parse("21121121-1211-2112-1121-1211211211a2");
        SeedTenant(dbContext, tenantId);
        var service = new ApplicabilityFactService(new EfApplicabilityFactRepository(dbContext));

        var facts = await service.ListAsync(new ApplicabilityFactQuery(tenantId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

        Assert.Contains(facts, fact => fact.Key == "company.profile" && fact.IsUnknown && fact.Value == "unknown");
        Assert.Contains(facts, fact => fact.Key == "contract.record" && fact.IsUnknown && fact.Value == "unknown");
        Assert.Contains(facts, fact => fact.Key == "clause.citation" && fact.IsUnknown && fact.Value == "unknown");
        Assert.Contains(facts, fact => fact.Key == "subcontractor.role" && fact.IsUnknown && fact.Value == "unknown");
    }

    [Fact]
    public void TC_21_1_5_Fact_definitions_are_documented()
    {
        var content = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "docs", "applicability-facts.md"));

        Assert.Contains("company.naics", content, StringComparison.Ordinal);
        Assert.Contains("contract.data_type", content, StringComparison.Ordinal);
        Assert.Contains("subcontractor.has_cui_access", content, StringComparison.Ordinal);
    }

    private static GccsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GccsDbContext>()
            .UseInMemoryDatabase($"applicability-facts-{Guid.NewGuid():N}")
            .Options;
        return new GccsDbContext(options);
    }

    private static CompanyProfileEntity CreateCompany(Guid tenantId) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LegalEntityName = "Fact Co",
            ContractorRole = ContractorRole.Prime,
            EmployeeRange = CompanyRange.Small,
            RevenueRange = CompanyRange.Small,
            DataHandlingPosture = DataHandlingPosture.FciOnly,
            AgencyCustomersJson = """["DLA"]""",
            CreatedAt = DateTimeOffset.UtcNow,
            NaicsCodes =
            [
                new CompanyNaicsCodeEntity
                {
                    Id = Guid.NewGuid(),
                    Code = "541511",
                    Title = "Custom Computer Programming Services",
                    IsPrimary = true,
                    QualifiesAsSmall = true,
                    LastCheckedAt = new DateOnly(2026, 6, 17)
                }
            ],
            Certifications =
            [
                new CompanyCertificationEntity
                {
                    Id = Guid.NewGuid(),
                    Type = CertificationType.Wosb,
                    Status = CertificationStatus.Active,
                    Issuer = "SBA",
                    ExpiresAt = new DateOnly(2027, 6, 17)
                }
            ],
            Locations =
            [
                new CompanyLocationEntity
                {
                    Id = Guid.NewGuid(),
                    City = "Arlington",
                    StateOrProvince = "VA",
                    Country = "US"
                }
            ]
        };

    private static ContractEntity CreateContract(Guid tenantId, Guid contractId) =>
        new()
        {
            Id = contractId,
            TenantId = tenantId,
            ContractNumber = "FACT-1",
            Title = "Fact contract",
            AgencyOrPrimeName = "Defense Logistics Agency",
            Relationship = ContractorRelationship.Prime,
            Kind = ContractKind.FixedPrice,
            Status = ContractStatus.Active,
            PeriodOfPerformanceStart = new DateOnly(2026, 7, 1),
            PeriodOfPerformanceEnd = new DateOnly(2027, 6, 30),
            PlaceOfPerformance = "Virginia",
            DataHandlingPosture = DataHandlingPosture.FciOnly,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static ContractClauseEntity CreateClause(Guid contractId, Guid clauseId) =>
        new()
        {
            Id = clauseId,
            ContractId = contractId,
            ClauseLibraryId = "far-52-204-21",
            ClauseNumber = "52.204-21",
            Title = "Basic Safeguarding",
            FullText = "Protect FCI.",
            Source = ClauseSource.Far,
            SourceUrl = "https://www.acquisition.gov/far/52.204-21",
            AttachmentReason = "Fact derivation.",
            LastReviewedAt = new DateOnly(2026, 6, 17),
            Confidence = "high",
            ReviewState = ReviewState.Published,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static SubcontractorEntity CreateSubcontractor(Guid tenantId, Guid subcontractorId) =>
        new()
        {
            Id = subcontractorId,
            TenantId = tenantId,
            Name = "Fact Sub",
            Status = SubcontractorStatus.Active,
            RoleDescription = "Specialty IT subcontractor",
            HasFciAccess = true,
            HasCuiAccess = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

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

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Gccs.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
