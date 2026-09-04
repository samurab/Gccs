using Gccs.Application.Compliance;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Compliance;

public sealed class EfFedRampControlMappingRepository(GccsDbContext dbContext) : IFedRampControlMappingRepository
{
    public async Task<IReadOnlyList<FedRampControlMappingDto>> ListAsync(Guid tenantId, FedRampGapReportFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var query = Query(tenantId);
        if (filter is not null)
        {
            query = query.Where(record =>
                (string.IsNullOrWhiteSpace(filter.Family) || record.Family.ToLower() == filter.Family.Trim().ToLower()) &&
                (!filter.Severity.HasValue || record.Gaps.Any(gap => gap.IsOpen && gap.Severity == filter.Severity.Value)) &&
                (string.IsNullOrWhiteSpace(filter.Owner) || record.Gaps.Any(gap => gap.IsOpen && gap.Owner.ToLower() == filter.Owner.Trim().ToLower())) &&
                (!filter.TargetDate.HasValue || record.Gaps.Any(gap => gap.IsOpen && gap.TargetDate <= filter.TargetDate.Value)));
        }

        var records = await query.OrderBy(record => record.ControlId).ToArrayAsync(cancellationToken);
        return records.Select(ToDto).ToArray();
    }

    public async Task<FedRampControlMappingDto?> GetAsync(Guid tenantId, Guid mappingId, CancellationToken cancellationToken = default)
    {
        var record = await Query(tenantId).SingleOrDefaultAsync(candidate => candidate.Id == mappingId, cancellationToken);
        return record is null ? null : ToDto(record);
    }

    public async Task<FedRampControlMappingDto> CreateAsync(Guid tenantId, CreateFedRampControlMappingRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var record = new FedRampControlMappingEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ControlId = request.ControlId.Trim(),
            Family = request.Family.Trim(),
            Baseline = request.Baseline.Trim(),
            Owner = request.Owner.Trim(),
            ImplementationStatus = request.ImplementationStatus,
            ImplementationSummary = request.ImplementationSummary.Trim(),
            InheritedProvider = request.InheritedProvider?.Trim(),
            GapRationale = request.GapRationale?.Trim(),
            SourceReference = request.SourceReference.Trim(),
            ReviewState = FedRampReviewState.Draft,
            Version = 1,
            CreatedAt = now,
            CreatedByUserId = actorUserId,
            EvidenceLinks = request.EvidenceLinks.Select(link => new FedRampEvidenceLinkEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Label = link.Label.Trim(),
                Reference = link.Reference.Trim(),
                EvidenceType = link.EvidenceType,
                CreatedAt = now,
                CreatedByUserId = actorUserId
            }).ToList()
        };

        dbContext.FedRampControlMappings.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(record);
    }

    public async Task<FedRampControlMappingDto?> LinkEvidenceAsync(Guid tenantId, Guid mappingId, FedRampEvidenceLinkRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var record = await Query(tenantId).SingleOrDefaultAsync(candidate => candidate.Id == mappingId, cancellationToken);
        if (record is null)
        {
            return null;
        }

        var evidenceLink = new FedRampEvidenceLinkEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            MappingId = mappingId,
            Label = request.Label.Trim(),
            Reference = request.Reference.Trim(),
            EvidenceType = request.EvidenceType,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = actorUserId
        };
        dbContext.FedRampEvidenceLinks.Add(evidenceLink);
        record.EvidenceLinks.Add(evidenceLink);
        Touch(record, actorUserId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(record);
    }

    public async Task<FedRampControlMappingDto?> AddGapAsync(Guid tenantId, Guid mappingId, FedRampGapRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var record = await Query(tenantId).SingleOrDefaultAsync(candidate => candidate.Id == mappingId, cancellationToken);
        if (record is null)
        {
            return null;
        }

        var gap = new FedRampGapEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            MappingId = mappingId,
            Rationale = request.Rationale.Trim(),
            Severity = request.Severity,
            Owner = request.Owner.Trim(),
            TargetDate = request.TargetDate,
            IsOpen = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = actorUserId
        };
        dbContext.FedRampGaps.Add(gap);
        record.Gaps.Add(gap);
        Touch(record, actorUserId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(record);
    }

    public async Task<FedRampControlMappingDto?> ChangeStateAsync(Guid tenantId, Guid mappingId, FedRampControlReviewRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var record = await Query(tenantId).SingleOrDefaultAsync(candidate => candidate.Id == mappingId, cancellationToken);
        if (record is null)
        {
            return null;
        }

        var previousState = record.ReviewState;
        record.ReviewState = request.State;
        record.Reviewer = request.Reviewer.Trim();
        record.ReviewDate = request.ReviewDate;
        var history = new FedRampControlMappingHistoryEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            MappingId = mappingId,
            PreviousState = previousState,
            NewState = request.State,
            Reviewer = request.Reviewer.Trim(),
            ReviewDate = request.ReviewDate,
            ReviewNotes = request.ReviewNotes.Trim(),
            ChangedAt = DateTimeOffset.UtcNow,
            ChangedByUserId = actorUserId
        };
        dbContext.FedRampControlMappingHistory.Add(history);
        record.History.Add(history);
        Touch(record, actorUserId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(record);
    }

    private IQueryable<FedRampControlMappingEntity> Query(Guid tenantId) =>
        dbContext.FedRampControlMappings
            .Where(record => record.TenantId == tenantId)
            .Include(record => record.EvidenceLinks)
            .Include(record => record.Gaps)
            .AsSplitQuery();

    private static void Touch(FedRampControlMappingEntity record, Guid actorUserId)
    {
        record.Version++;
        record.UpdatedAt = DateTimeOffset.UtcNow;
        record.UpdatedByUserId = actorUserId;
    }

    private static FedRampControlMappingDto ToDto(FedRampControlMappingEntity record) =>
        new(
            record.Id,
            record.TenantId,
            record.ControlId,
            record.Family,
            record.Baseline,
            record.Owner,
            record.ImplementationStatus,
            record.ImplementationSummary,
            record.InheritedProvider,
            record.EvidenceLinks.OrderBy(link => link.CreatedAt).Select(link => new FedRampEvidenceLinkDto(link.Label, link.Reference, link.EvidenceType)).ToArray(),
            record.Gaps.OrderBy(gap => gap.CreatedAt).Select(gap => new FedRampGapDto(gap.Rationale, gap.Severity, gap.Owner, gap.TargetDate, gap.IsOpen)).ToArray(),
            record.GapRationale,
            record.SourceReference,
            record.ReviewState,
            record.Reviewer,
            record.ReviewDate,
            record.CreatedAt,
            record.UpdatedAt);
}
