using System.Collections.Concurrent;
using Gccs.Application.Compliance;

namespace Gccs.Infrastructure.Compliance;

public sealed class InMemorySspSectionRepository : ISspSectionRepository
{
    private readonly ConcurrentDictionary<Guid, List<SspSectionDto>> _sections = new();

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
}
