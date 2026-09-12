using Gccs.Application.Portals;
using Gccs.Domain.Common;
using Gccs.Domain.Reports;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Portals;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ExternalPortalScopeValidatorTests
{
    [Fact]
    public async Task Validator_accepts_only_current_tenant_approved_no_cui_packages_and_contracts()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var contractId = Guid.NewGuid();
        var packageId = Guid.NewGuid();
        var cuiPackageId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<GccsDbContext>()
            .UseInMemoryDatabase($"external-portal-scope-{Guid.NewGuid()}").Options;
        await using var db = new GccsDbContext(options);
        db.Tenants.AddRange(Tenant(tenantId), Tenant(otherTenantId));
        db.Contracts.Add(new ContractEntity
        {
            Id = contractId, TenantId = tenantId, ContractNumber = "C-001", Title = "Allowed contract",
            PeriodOfPerformanceStart = new DateOnly(2026, 1, 1), PeriodOfPerformanceEnd = new DateOnly(2026, 12, 31),
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.Reports.AddRange(
            Report(packageId, tenantId, ContentClassification.Unclassified),
            Report(cuiPackageId, tenantId, ContentClassification.Cui));
        await db.SaveChangesAsync();
        var validator = new EfExternalPortalScopeValidator(db);

        await validator.ValidateAsync(tenantId, [packageId], [contractId]);
        await Assert.ThrowsAsync<ExternalPortalAccessException>(() =>
            validator.ValidateAsync(otherTenantId, [packageId], [contractId]));
        await Assert.ThrowsAsync<ExternalPortalAccessException>(() =>
            validator.ValidateAsync(tenantId, [cuiPackageId], [contractId]));
    }

    private static TenantEntity Tenant(Guid id) => new()
    {
        Id = id, Name = id.ToString(), Status = TenantStatus.Active,
        DataPosture = TenantDataPosture.NoCui, CreatedAt = DateTimeOffset.UtcNow
    };

    private static ReportEntity Report(Guid id, Guid tenantId, ContentClassification classification) => new()
    {
        Id = id, TenantId = tenantId, Type = ReportType.ComplianceStatus, Title = "Review package",
        Status = ReportStatus.Complete, GeneratedAt = DateTimeOffset.UtcNow, GeneratedByUserId = Guid.NewGuid(),
        Classification = classification, SnapshotJson = "{}", ExportHtml = string.Empty, CreatedAt = DateTimeOffset.UtcNow
    };
}
