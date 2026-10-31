using System.Collections.Concurrent;
using Gccs.Application.Compliance;
using Gccs.Application.Common;
using Gccs.Domain.Compliance;

namespace Gccs.Infrastructure.Compliance;

public sealed class InMemorySspSectionRepository : ISspSectionRepository, ISspNarrativeRepository
{
    private readonly ConcurrentDictionary<Guid, List<SspSectionDto>> _sections = new();
    private readonly ConcurrentDictionary<Guid, List<SspNarrativeDto>> _narratives = new();

    public Task<IReadOnlyList<SspSectionDto>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SspSectionDto>>(_sections.GetOrAdd(tenantId, _ => []).OrderBy(section => section.SectionType).ThenBy(section => section.Title).ToArray());

    public Task<SspSectionDto?> GetAsync(Guid tenantId, Guid sectionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_sections.GetOrAdd(tenantId, _ => []).SingleOrDefault(section => section.Id == sectionId));

    public Task<SspSectionDto> CreateAsync(Guid tenantId, CreateSspSectionRequest request, Guid actorUserId, string actorName, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var section = new SspSectionDto(
            Guid.NewGuid(),
            tenantId,
            request.SectionType,
            request.Title.Trim(),
            request.Owner.Trim(),
            SspSectionStatus.Draft,
            null,
            null,
            null,
            true,
            1,
            Normalize(request.LinkedRecords),
            Normalize(request.SourceReferences),
            [new SspSectionHistoryDto(SspSectionStatus.Draft, actorUserId, actorName, now, "Section created.")],
            now,
            now);

        _sections.GetOrAdd(tenantId, _ => []).Add(section);
        return Task.FromResult(section);
    }

    public Task<SspSectionDto?> UpdateAsync(Guid tenantId, Guid sectionId, UpdateSspSectionRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var current = _sections.GetOrAdd(tenantId, _ => []).SingleOrDefault(section => section.Id == sectionId);
        if (current is not null && current.Version != request.ExpectedVersion)
            throw new Gccs.Application.Common.ContentRevisionConflictException();
        return UpdateAsync(tenantId, sectionId, section => section with
        {
            SectionType = request.SectionType,
            Title = request.Title.Trim(),
            Owner = request.Owner.Trim(),
            LinkedRecords = Normalize(request.LinkedRecords),
            SourceReferences = Normalize(request.SourceReferences),
            Version = section.Version + 1,
            UpdatedAt = DateTimeOffset.UtcNow
        });
    }

    public Task<SspSectionDto?> ChangeStatusAsync(Guid tenantId, Guid sectionId, SspSectionStatusRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var current = _sections.GetOrAdd(tenantId, _ => []).SingleOrDefault(section => section.Id == sectionId);
        if (current is not null && current.Version != request.ExpectedVersion)
            throw new Gccs.Application.Common.ContentRevisionConflictException();
        return UpdateAsync(tenantId, sectionId, section => section with
        {
            Status = request.Status,
            Reviewer = request.Reviewer?.Trim(),
            ReviewDate = request.ReviewDate,
            ApprovalRationale = request.ApprovalRationale?.Trim(),
            Version = section.Version + 1,
            UpdatedAt = now,
            History = section.History.Append(new SspSectionHistoryDto(request.Status, actorUserId, request.ActorName.Trim(), now, request.ApprovalRationale?.Trim())).ToArray()
        });
    }

    private Task<SspSectionDto?> UpdateAsync(Guid tenantId, Guid sectionId, Func<SspSectionDto, SspSectionDto> update)
    {
        var records = _sections.GetOrAdd(tenantId, _ => []);
        var index = records.FindIndex(section => section.Id == sectionId);
        if (index < 0)
        {
            return Task.FromResult<SspSectionDto?>(null);
        }

        records[index] = update(records[index]);
        return Task.FromResult<SspSectionDto?>(records[index]);
    }

    private static SspLinkedRecordDto[] Normalize(SspLinkedRecordDto[] records) =>
        records
            .Select(record => new SspLinkedRecordDto(record.RecordType, record.RecordId.Trim(), record.Relationship.Trim()))
            .ToArray();

    private static SspSourceReferenceDto[] Normalize(SspSourceReferenceDto[] references) =>
        references
            .Select(reference => new SspSourceReferenceDto(reference.Source.Trim(), reference.SourceUrl.Trim(), reference.LastReviewedAt))
            .ToArray();

    public Task<IReadOnlyList<SspNarrativeDto>> ListNarrativesAsync(Guid tenantId, Guid sectionId, CancellationToken cancellationToken = default)
    {
        var records = _narratives.GetOrAdd(tenantId, _ => []);
        lock (records) return Task.FromResult<IReadOnlyList<SspNarrativeDto>>(records.Where(x => x.SectionId == sectionId).OrderByDescending(x => x.UpdatedAt).ToArray());
    }

    public Task<SspNarrativeDto?> GetNarrativeAsync(Guid tenantId, Guid sectionId, Guid narrativeId, CancellationToken cancellationToken = default)
    {
        var records = _narratives.GetOrAdd(tenantId, _ => []);
        lock (records) return Task.FromResult(records.SingleOrDefault(narrative => narrative.SectionId == sectionId && narrative.Id == narrativeId));
    }

    public Task<SspNarrativeDto?> GetCurrentApprovedNarrativeAsync(Guid tenantId, Guid sectionId, CancellationToken cancellationToken = default)
    {
        var records = _narratives.GetOrAdd(tenantId, _ => []);
        lock (records) return Task.FromResult(records.Where(narrative => narrative.SectionId == sectionId && narrative.Status == SspNarrativeStatus.Approved).OrderByDescending(narrative => narrative.UpdatedAt).FirstOrDefault());
    }

    public Task<IReadOnlyDictionary<Guid, SspNarrativeDto>> ListCurrentApprovedNarrativesAsync(Guid tenantId, IReadOnlyCollection<Guid> sectionIds, CancellationToken cancellationToken = default)
    {
        var included = sectionIds.ToHashSet();
        var records = _narratives.GetOrAdd(tenantId, _ => []);
        lock (records)
            return Task.FromResult<IReadOnlyDictionary<Guid, SspNarrativeDto>>(records
                .Where(item => included.Contains(item.SectionId) && item.Status == SspNarrativeStatus.Approved)
                .GroupBy(item => item.SectionId)
                .ToDictionary(group => group.Key, group => group.OrderByDescending(item => item.UpdatedAt).First()));
    }

    public Task<SspNarrativeDto> CreateDraftAsync(Guid tenantId, Guid sectionId, string generatedText, bool aiAssisted, string? reviewerNotes, ContentClassificationDto classification, IReadOnlyList<ResolvedSspNarrativeSource> sources, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var narrative = new SspNarrativeDto(
            Guid.NewGuid(),
            tenantId,
            sectionId,
            generatedText,
            null,
            null,
            SspNarrativeStatus.Draft,
            aiAssisted,
            true,
            reviewerNotes?.Trim(),
            null,
            null,
            null,
            1,
            classification,
            sources.Select(source => new SspNarrativeSourceRecordDto(source.SourceType, source.RecordId, source.Label, source.Summary, source.SourceUrl, source.Fingerprint, source.Classification)).ToArray(),
            now,
            now);

        var records = _narratives.GetOrAdd(tenantId, _ => []);
        lock (records) records.Add(narrative);
        return Task.FromResult(narrative);
    }

    public Task<SspNarrativeDto?> UpdateDraftAsync(Guid tenantId, Guid sectionId, Guid narrativeId, EditSspNarrativeDraftRequest request, ContentClassificationDto classification, Guid actorUserId, CancellationToken cancellationToken = default) =>
        UpdateNarrativeAsync(tenantId, sectionId, narrativeId, narrative =>
        {
            if (narrative.Status != SspNarrativeStatus.Draft) throw new SspNarrativeValidationException("Only draft SSP narratives can be changed.");
            if (narrative.Version != request.ExpectedVersion) throw new Gccs.Application.Common.ContentRevisionConflictException();
            return narrative with { EditedText = request.EditedText.Trim(), ReviewerNotes = request.ReviewerNotes?.Trim(), Classification = classification, DraftOnly = true, Version = narrative.Version + 1, UpdatedAt = DateTimeOffset.UtcNow };
        });

    public async Task<SspNarrativeDto?> ApproveAsync(Guid tenantId, Guid sectionId, Guid narrativeId, ApproveSspNarrativeRequest request, Guid reviewerUserId, string reviewerName, CancellationToken cancellationToken = default)
    {
        var approved = await UpdateNarrativeAsync(tenantId, sectionId, narrativeId, narrative =>
        {
            if (narrative.Status != SspNarrativeStatus.Draft) throw new SspNarrativeValidationException("Only draft SSP narratives can be changed.");
            if (narrative.Version != request.ExpectedVersion) throw new Gccs.Application.Common.ContentRevisionConflictException();
            var now = DateTimeOffset.UtcNow;
            return narrative with
            {
                ApprovedText = narrative.EditedText ?? narrative.GeneratedText,
                Status = SspNarrativeStatus.Approved,
                DraftOnly = false,
                ReviewerUserId = reviewerUserId,
                Reviewer = reviewerName,
                ReviewDate = request.ReviewDate,
                Version = narrative.Version + 1,
                UpdatedAt = now
            };
        });

        if (approved is not null)
        {
            var records = _narratives.GetOrAdd(tenantId, _ => []);
            lock (records)
            {
                for (var index = 0; index < records.Count; index++)
                {
                    var narrative = records[index];
                    if (narrative.SectionId == sectionId && narrative.Id != narrativeId && narrative.Status == SspNarrativeStatus.Approved)
                        records[index] = narrative with { Status = SspNarrativeStatus.Superseded, DraftOnly = false, Version = narrative.Version + 1, UpdatedAt = DateTimeOffset.UtcNow };
                }
            }
        }

        return approved;
    }

    private Task<SspNarrativeDto?> UpdateNarrativeAsync(Guid tenantId, Guid sectionId, Guid narrativeId, Func<SspNarrativeDto, SspNarrativeDto> update)
    {
        var records = _narratives.GetOrAdd(tenantId, _ => []);
        lock (records)
        {
            var index = records.FindIndex(narrative => narrative.SectionId == sectionId && narrative.Id == narrativeId);
            if (index < 0) return Task.FromResult<SspNarrativeDto?>(null);
            records[index] = update(records[index]);
            return Task.FromResult<SspNarrativeDto?>(records[index]);
        }
    }

}
