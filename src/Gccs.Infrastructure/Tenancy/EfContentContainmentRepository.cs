using Gccs.Application.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Tenancy;

public sealed class EfContentContainmentRepository(GccsDbContext db) : IContentContainmentRepository
{
    public async Task<bool> IsBlockedAsync(Guid tenantId, string entityType, string entityId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(entityId, out var id)) return false;
        return entityType switch
        {
            "EvidenceItem" => await db.EvidenceItems.AsNoTracking().AnyAsync(e => e.TenantId == tenantId && e.Id == id && e.IsUseBlocked, cancellationToken),
            "EvidenceFileVersion" => await db.EvidenceFileVersions.AsNoTracking().AnyAsync(e => e.Id == id && e.EvidenceItem!.TenantId == tenantId && e.IsUseBlocked, cancellationToken),
            "ClassifiedNote" => await db.Set<ClassifiedNoteEntity>().AsNoTracking().AnyAsync(e => e.Id == id && e.TenantId == tenantId && e.IsUseBlocked, cancellationToken),
            "ExtractionJob" => await db.Set<ExtractionJobEntity>().AsNoTracking().AnyAsync(e => e.Id == id && e.TenantId == tenantId && e.IsUseBlocked, cancellationToken),
            "ContractDocument" => await db.Set<ContractDocumentEntity>().AsNoTracking().AnyAsync(e => e.Id == id && e.Contract!.TenantId == tenantId && e.IsUseBlocked, cancellationToken),
            "Report" => await db.Reports.AsNoTracking().AnyAsync(e => e.Id == id && e.TenantId == tenantId && e.IsUseBlocked, cancellationToken),
            _ => false
        };
    }
}
