using Gccs.Application.Common;
using Gccs.Application.Compliance;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Compliance;

public sealed class EfSspSectionRepository(GccsDbContext dbContext) : ISspSectionRepository
{
    public async Task<IReadOnlyList<SspSectionDto>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        (await Query(tenantId).OrderBy(x => x.SectionType).ThenBy(x => x.Title).ToArrayAsync(cancellationToken))
            .Select(ToDto).ToArray();

    public async Task<SspSectionDto?> GetAsync(Guid tenantId, Guid sectionId, CancellationToken cancellationToken = default)
    {
        var entity = await Query(tenantId).SingleOrDefaultAsync(x => x.Id == sectionId, cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<SspSectionDto> CreateAsync(Guid tenantId, CreateSspSectionRequest request, Guid actorUserId, string actorName, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var sectionId = Guid.NewGuid();
        var entity = new SspSectionEntity
        {
            Id = sectionId, TenantId = tenantId, SectionType = request.SectionType, Title = request.Title.Trim(),
            Owner = request.Owner.Trim(), Status = SspSectionStatus.Draft, IsRequired = true, Version = 1,
            CreatedAt = now, CreatedByUserId = actorUserId,
            LinkedRecords = Links(tenantId, sectionId, request.LinkedRecords),
            SourceReferences = Sources(tenantId, sectionId, request.SourceReferences),
            History = [new SspSectionHistoryEntity
            {
                Id = Guid.NewGuid(), TenantId = tenantId, SectionId = sectionId, Status = SspSectionStatus.Draft,
                ActorUserId = actorUserId, ActorName = actorName, ChangedAt = now, Notes = "Section created."
            }]
        };
        dbContext.SspSections.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<SspSectionDto?> UpdateAsync(Guid tenantId, Guid sectionId, UpdateSspSectionRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var entity = await Tracked(tenantId, sectionId, cancellationToken);
        if (entity is null) return null;
        EnsureVersion(entity, request.ExpectedVersion);
        entity.SectionType = request.SectionType;
        entity.Title = request.Title.Trim();
        entity.Owner = request.Owner.Trim();
        SynchronizeLinks(entity, request.LinkedRecords);
        SynchronizeSources(entity, request.SourceReferences);
        Touch(entity, actorUserId);
        await SaveAsync(cancellationToken);
        return await GetAsync(tenantId, sectionId, cancellationToken);
    }

    public async Task<SspSectionDto?> ChangeStatusAsync(Guid tenantId, Guid sectionId, SspSectionStatusRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var entity = await Tracked(tenantId, sectionId, cancellationToken);
        if (entity is null) return null;
        EnsureVersion(entity, request.ExpectedVersion);
        var now = DateTimeOffset.UtcNow;
        entity.Status = request.Status;
        entity.Reviewer = request.Reviewer?.Trim();
        entity.ReviewDate = request.ReviewDate;
        entity.ApprovalRationale = request.ApprovalRationale?.Trim();
        entity.History.Add(new SspSectionHistoryEntity
        {
            Id = Guid.NewGuid(), TenantId = tenantId, SectionId = sectionId, Status = request.Status,
            ActorUserId = actorUserId, ActorName = request.ActorName.Trim(), ChangedAt = now,
            Notes = request.ApprovalRationale?.Trim()
        });
        Touch(entity, actorUserId, now);
        await SaveAsync(cancellationToken);
        return ToDto(entity);
    }

    private IQueryable<SspSectionEntity> Query(Guid tenantId) => dbContext.SspSections.AsNoTracking()
        .Where(x => x.TenantId == tenantId)
        .Include(x => x.LinkedRecords).Include(x => x.SourceReferences).Include(x => x.History).AsSplitQuery();

    private Task<SspSectionEntity?> Tracked(Guid tenantId, Guid id, CancellationToken token) => dbContext.SspSections
        .Where(x => x.TenantId == tenantId && x.Id == id)
        .Include(x => x.LinkedRecords).Include(x => x.SourceReferences).Include(x => x.History).AsSplitQuery()
        .SingleOrDefaultAsync(token);

    private static List<SspSectionLinkEntity> Links(Guid tenantId, Guid sectionId, IEnumerable<SspLinkedRecordDto> records) => records.Select(x => new SspSectionLinkEntity
    {
        Id = Guid.NewGuid(), TenantId = tenantId, SectionId = sectionId, RecordType = x.RecordType, RecordId = x.RecordId.Trim(), Relationship = x.Relationship.Trim()
    }).ToList();

    private static List<SspSectionSourceReferenceEntity> Sources(Guid tenantId, Guid sectionId, IEnumerable<SspSourceReferenceDto> sources) => sources.Select(x => new SspSectionSourceReferenceEntity
    {
        Id = Guid.NewGuid(), TenantId = tenantId, SectionId = sectionId, Source = x.Source.Trim(), SourceUrl = x.SourceUrl.Trim(), LastReviewedAt = x.LastReviewedAt
    }).ToList();

    private void SynchronizeLinks(SspSectionEntity entity, IReadOnlyList<SspLinkedRecordDto> requested)
    {
        var existing = entity.LinkedRecords.OrderBy(x => x.Id).ToArray();
        for (var index = 0; index < requested.Count; index++)
        {
            var source = requested[index];
            if (index < existing.Length)
            {
                existing[index].RecordType = source.RecordType;
                existing[index].RecordId = source.RecordId.Trim();
                existing[index].Relationship = source.Relationship.Trim();
            }
            else entity.LinkedRecords.Add(Links(entity.TenantId, entity.Id, [source]).Single());
        }
        if (existing.Length > requested.Count) dbContext.SspSectionLinks.RemoveRange(existing[requested.Count..]);
    }

    private void SynchronizeSources(SspSectionEntity entity, IReadOnlyList<SspSourceReferenceDto> requested)
    {
        var existing = entity.SourceReferences.OrderBy(x => x.Id).ToArray();
        for (var index = 0; index < requested.Count; index++)
        {
            var source = requested[index];
            if (index < existing.Length)
            {
                existing[index].Source = source.Source.Trim();
                existing[index].SourceUrl = source.SourceUrl.Trim();
                existing[index].LastReviewedAt = source.LastReviewedAt;
            }
            else entity.SourceReferences.Add(Sources(entity.TenantId, entity.Id, [source]).Single());
        }
        if (existing.Length > requested.Count) dbContext.SspSectionSourceReferences.RemoveRange(existing[requested.Count..]);
    }

    private static void EnsureVersion(SspSectionEntity entity, long expected)
    {
        if (entity.Version != expected) throw new ContentRevisionConflictException();
    }

    private static void Touch(SspSectionEntity entity, Guid actor, DateTimeOffset? now = null)
    {
        entity.Version++;
        entity.UpdatedAt = now ?? DateTimeOffset.UtcNow;
        entity.UpdatedByUserId = actor;
    }

    private async Task SaveAsync(CancellationToken token)
    {
        try { await dbContext.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException) { throw new ContentRevisionConflictException(); }
    }

    private static SspSectionDto ToDto(SspSectionEntity x) => new(
        x.Id, x.TenantId, x.SectionType, x.Title, x.Owner, x.Status, x.Reviewer, x.ReviewDate,
        x.ApprovalRationale, x.IsRequired, x.Version,
        x.LinkedRecords.OrderBy(y => y.RecordType).ThenBy(y => y.RecordId).Select(y => new SspLinkedRecordDto(y.RecordType, y.RecordId, y.Relationship)).ToArray(),
        x.SourceReferences.OrderBy(y => y.Source).Select(y => new SspSourceReferenceDto(y.Source, y.SourceUrl, y.LastReviewedAt)).ToArray(),
        x.History.OrderBy(y => y.ChangedAt).Select(y => new SspSectionHistoryDto(y.Status, y.ActorUserId, y.ActorName, y.ChangedAt, y.Notes)).ToArray(),
        x.CreatedAt, x.UpdatedAt ?? x.CreatedAt);
}

public sealed class EfSspSectionLinkValidator(GccsDbContext dbContext) : ISspSectionLinkValidator
{
    public async Task ValidateAsync(Guid tenantId, IReadOnlyCollection<SspLinkedRecordDto> records, CancellationToken cancellationToken = default)
    {
        var duplicates = records.GroupBy(x => new { x.RecordType, Id = x.RecordId.Trim() }).FirstOrDefault(x => x.Count() > 1);
        if (duplicates is not null) throw new SspSectionValidationException("Duplicate linked records are not allowed.");
        foreach (var record in records)
            if (!await ExistsAsync(tenantId, record, cancellationToken))
                throw new SspSectionValidationException($"Linked {record.RecordType} record was not found in the current tenant scope or is not eligible for SSP use.");
    }

    private async Task<bool> ExistsAsync(Guid tenantId, SspLinkedRecordDto record, CancellationToken token)
    {
        var id = record.RecordId.Trim();
        return record.RecordType switch
        {
            SspLinkedRecordType.CompanyProfile => Guid.TryParse(id, out var companyId) && await dbContext.CompanyProfiles.AnyAsync(x => x.TenantId == tenantId && x.Id == companyId, token),
            SspLinkedRecordType.SystemBoundary => Guid.TryParse(id, out var boundaryId) && await dbContext.SystemBoundaries.AnyAsync(x => x.TenantId == tenantId && x.Id == boundaryId, token),
            SspLinkedRecordType.Asset => Guid.TryParse(id, out var assetId) && await dbContext.Assets.AnyAsync(x => x.TenantId == tenantId && x.Id == assetId, token),
            SspLinkedRecordType.CmmcControl => await dbContext.Controls.AnyAsync(x => x.Id == id, token),
            SspLinkedRecordType.ResponsibilityMatrix => await ResponsibilityMatrixExists(tenantId, id, token),
            SspLinkedRecordType.Policy => Guid.TryParse(id, out var policyId) && (await dbContext.GeneratedPolicies.AnyAsync(x => x.TenantId == tenantId && x.Id == policyId, token) || await dbContext.PolicyTemplates.AnyAsync(x => x.TenantId == tenantId && x.Id == policyId, token)),
            SspLinkedRecordType.PoamItem => Guid.TryParse(id, out var poamId) && await dbContext.PoamItems.AnyAsync(x => x.TenantId == tenantId && x.Id == poamId, token),
            SspLinkedRecordType.Evidence => Guid.TryParse(id, out var evidenceId) && await EvidenceEligible(tenantId, evidenceId, token),
            _ => false
        };
    }

    private async Task<bool> ResponsibilityMatrixExists(Guid tenantId, string id, CancellationToken token)
    {
        var separator = id.LastIndexOf('@');
        if (separator <= 0 || separator == id.Length - 1) return false;
        var matrixId = id[..separator]; var version = id[(separator + 1)..];
        return await dbContext.SharedResponsibilityMatrixAcknowledgements.AnyAsync(x => x.TenantId == tenantId && x.MatrixId == matrixId && x.MatrixVersion == version, token);
    }

    private async Task<bool> EvidenceEligible(Guid tenantId, Guid id, CancellationToken token)
    {
        var evidence = await dbContext.EvidenceItems.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, token);
        if (evidence is null || evidence.IsUseBlocked || evidence.Classification is ContentClassification.Prohibited or ContentClassification.Unknown) return false;
        if (evidence.Classification == ContentClassification.Cui)
            return await dbContext.Tenants.AnyAsync(x => x.Id == tenantId && x.DataPosture == TenantDataPosture.CuiReady, token);
        if (evidence.Classification == ContentClassification.SyntheticCui)
            return evidence.ClassificationIsApprovedDemoContent;
        return true;
    }
}
