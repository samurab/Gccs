using Gccs.Application.Security;
using Gccs.Application.Tenancy;
using Gccs.Domain.Common;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
namespace Gccs.Infrastructure.Tenancy;

public sealed class UnavailableSyntheticContentApprovalRepository : ISyntheticContentApprovalRepository
{
    public Task<bool> IsApprovedAsync(string? entityType, string? entityId, CancellationToken cancellationToken) => Task.FromResult(false);
}

public sealed class EfSyntheticContentApprovalRepository(GccsDbContext db, ICurrentTenantContext context) : ISyntheticContentApprovalRepository
{
    public async Task<bool> IsApprovedAsync(string? type, string? entityId, CancellationToken ct)
    {
        if (!Guid.TryParse(entityId, out var id)) return false;
        return type switch
        {
            "EvidenceItem" => await db.EvidenceItems.AnyAsync(e => e.Id == id && e.TenantId == context.TenantId && e.Classification == ContentClassification.SyntheticCui && e.ClassificationSource == ContentClassificationSource.ImportedDemoSeed && e.ClassificationIsApprovedDemoContent, ct),
            "EvidenceFileVersion" => await db.EvidenceFileVersions.AnyAsync(e => e.Id == id && db.EvidenceItems.Any(p => p.Id == e.EvidenceItemId && p.TenantId == context.TenantId) && e.Classification == ContentClassification.SyntheticCui && e.ClassificationSource == ContentClassificationSource.ImportedDemoSeed && e.ClassificationIsApprovedDemoContent, ct),
            "ContractDocument" => await db.Set<ContractDocumentEntity>().AnyAsync(e => e.Id == id && db.Contracts.Any(c => c.Id == e.ContractId && c.TenantId == context.TenantId) && e.Classification == ContentClassification.SyntheticCui && e.ClassificationSource == ContentClassificationSource.ImportedDemoSeed && e.ClassificationIsApprovedDemoContent, ct),
            "ExtractionJob" => await db.Set<ExtractionJobEntity>().AnyAsync(e => e.Id == id && e.TenantId == context.TenantId && e.Classification == ContentClassification.SyntheticCui && e.ClassificationSource == ContentClassificationSource.ImportedDemoSeed && e.ClassificationIsApprovedDemoContent, ct),
            "Report" => await db.Reports.AnyAsync(e => e.Id == id && e.TenantId == context.TenantId && e.Classification == ContentClassification.SyntheticCui && e.ClassificationSource == ContentClassificationSource.ImportedDemoSeed && e.ClassificationIsApprovedDemoContent, ct),
            _ => false
        };
    }
}
