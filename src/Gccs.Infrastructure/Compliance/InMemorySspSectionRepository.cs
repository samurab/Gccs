using System.Collections.Concurrent;
using Gccs.Application.Compliance;

namespace Gccs.Infrastructure.Compliance;

public sealed class InMemorySspSectionRepository : ISspSectionRepository, ISspNarrativeRepository
{
    private readonly ConcurrentDictionary<Guid, List<SspSectionDto>> _sections = new();
    private readonly ConcurrentDictionary<Guid, List<SspNarrativeDto>> _narratives = new();

    public Task<IReadOnlyList<SspSectionDto>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SspSectionDto>>(_sections.GetOrAdd(tenantId, _ => []).OrderBy(section => section.SectionType).ThenBy(section => section.Title).ToArray());

    public Task<SspSectionDto?> GetAsync(Guid tenantId, Guid sectionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_sections.GetOrAdd(tenantId, _ => []).SingleOrDefault(section => section.Id == sectionId));

    public Task<SspSectionDto> CreateAsync(Guid tenantId, CreateSspSectionRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
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
            Normalize(request.LinkedRecords),
            Normalize(request.SourceReferences),
            [new SspSectionHistoryDto(SspSectionStatus.Draft, actorUserId, "system", now, "Section created.")],
            now,
            now);

        _sections.GetOrAdd(tenantId, _ => []).Add(section);
        return Task.FromResult(section);
    }

    public Task<SspSectionDto?> UpdateAsync(Guid tenantId, Guid sectionId, UpdateSspSectionRequest request, Guid actorUserId, CancellationToken cancellationToken = default) =>
        UpdateAsync(tenantId, sectionId, section => section with
        {
            SectionType = request.SectionType,
            Title = request.Title.Trim(),
            Owner = request.Owner.Trim(),
            LinkedRecords = Normalize(request.LinkedRecords),
            SourceReferences = Normalize(request.SourceReferences),
            UpdatedAt = DateTimeOffset.UtcNow
        });

    public Task<SspSectionDto?> ChangeStatusAsync(Guid tenantId, Guid sectionId, SspSectionStatusRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        return UpdateAsync(tenantId, sectionId, section => section with
        {
            Status = request.Status,
            Reviewer = request.Reviewer?.Trim(),
            ReviewDate = request.ReviewDate,
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
            .Select(record => new SspLinkedRecordDto(record.RecordType.Trim(), record.RecordId.Trim(), record.Relationship.Trim()))
            .ToArray();

    private static SspSourceReferenceDto[] Normalize(SspSourceReferenceDto[] references) =>
        references
            .Select(reference => new SspSourceReferenceDto(reference.Source.Trim(), reference.SourceUrl.Trim(), reference.LastReviewedAt))
            .ToArray();

    public Task<SspNarrativeDto?> GetNarrativeAsync(Guid tenantId, Guid sectionId, Guid narrativeId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_narratives.GetOrAdd(tenantId, _ => []).SingleOrDefault(narrative => narrative.SectionId == sectionId && narrative.Id == narrativeId));

    public Task<SspNarrativeDto?> GetCurrentApprovedNarrativeAsync(Guid tenantId, Guid sectionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_narratives.GetOrAdd(tenantId, _ => [])
            .Where(narrative => narrative.SectionId == sectionId && narrative.Status == SspNarrativeStatus.Approved)
            .OrderByDescending(narrative => narrative.UpdatedAt)
            .FirstOrDefault());

    public Task<SspNarrativeDto> CreateDraftAsync(Guid tenantId, Guid sectionId, GenerateSspNarrativeDraftRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var generatedText = string.Join(Environment.NewLine, request.SourceRecords.Select(record => $"{record.RecordType} {record.RecordId}: {record.Summary.Trim()}"));
        if (request.AiAssisted)
        {
            generatedText = $"Draft AI-assisted SSP narrative. {generatedText}";
        }

        var narrative = new SspNarrativeDto(
            Guid.NewGuid(),
            tenantId,
            sectionId,
            generatedText,
            null,
            null,
            SspNarrativeStatus.Draft,
            request.AiAssisted,
            true,
            request.ReviewerNotes?.Trim(),
            null,
            null,
            Normalize(request.SourceRecords),
            now,
            now);

        _narratives.GetOrAdd(tenantId, _ => []).Add(narrative);
        return Task.FromResult(narrative);
    }

    public Task<SspNarrativeDto?> UpdateDraftAsync(Guid tenantId, Guid sectionId, Guid narrativeId, EditSspNarrativeDraftRequest request, Guid actorUserId, CancellationToken cancellationToken = default) =>
        UpdateNarrativeAsync(tenantId, sectionId, narrativeId, narrative =>
            narrative.Status == SspNarrativeStatus.Draft
                ? narrative with { EditedText = request.EditedText.Trim(), ReviewerNotes = request.ReviewerNotes?.Trim(), DraftOnly = true, UpdatedAt = DateTimeOffset.UtcNow }
                : narrative);

    public async Task<SspNarrativeDto?> ApproveAsync(Guid tenantId, Guid sectionId, Guid narrativeId, ApproveSspNarrativeRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var approved = await UpdateNarrativeAsync(tenantId, sectionId, narrativeId, narrative =>
        {
            var now = DateTimeOffset.UtcNow;
            return narrative with
            {
                ApprovedText = narrative.EditedText ?? narrative.GeneratedText,
                Status = SspNarrativeStatus.Approved,
                DraftOnly = false,
                Reviewer = request.Reviewer.Trim(),
                ReviewDate = request.ReviewDate,
                UpdatedAt = now
            };
        });

        if (approved is not null)
        {
            var records = _narratives.GetOrAdd(tenantId, _ => []);
            for (var index = 0; index < records.Count; index++)
            {
                var narrative = records[index];
                if (narrative.SectionId == sectionId && narrative.Id != narrativeId && narrative.Status == SspNarrativeStatus.Approved)
                {
                    records[index] = narrative with { Status = SspNarrativeStatus.Superseded, DraftOnly = false, UpdatedAt = DateTimeOffset.UtcNow };
                }
            }
        }

        return approved;
    }

    private Task<SspNarrativeDto?> UpdateNarrativeAsync(Guid tenantId, Guid sectionId, Guid narrativeId, Func<SspNarrativeDto, SspNarrativeDto> update)
    {
        var records = _narratives.GetOrAdd(tenantId, _ => []);
        var index = records.FindIndex(narrative => narrative.SectionId == sectionId && narrative.Id == narrativeId);
        if (index < 0)
        {
            return Task.FromResult<SspNarrativeDto?>(null);
        }

        records[index] = update(records[index]);
        return Task.FromResult<SspNarrativeDto?>(records[index]);
    }

    private static SspNarrativeSourceRecordDto[] Normalize(SspNarrativeSourceRecordDto[] records) =>
        records
            .Select(record => new SspNarrativeSourceRecordDto(record.RecordType.Trim(), record.RecordId.Trim(), record.TenantId, record.Summary.Trim(), record.SourceUrl.Trim(), record.Approved, record.Outdated))
            .ToArray();
}
