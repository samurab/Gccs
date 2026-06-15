namespace Gccs.Application.NoCui;

public interface INoCuiAcknowledgementRepository
{
    Task<NoCuiAcknowledgementStatusDto?> FindCurrentUserAcknowledgementAsync(
        string noticeVersion,
        CancellationToken cancellationToken = default);

    Task<NoCuiAcknowledgementStatusDto> AddCurrentUserAcknowledgementAsync(
        string noticeVersion,
        string noticeCopy,
        Guid actorUserId,
        DateTimeOffset acknowledgedAt,
        CancellationToken cancellationToken = default);

    Task<EvidenceFileVersionDto> RecordAcceptedEvidenceUploadIntentAsync(
        EvidenceUploadIntentDto uploadIntent,
        CancellationToken cancellationToken = default);

    Task<EvidenceFileVersionDto?> FindLatestCurrentTenantFileVersionAsync(
        Guid evidenceItemId,
        CancellationToken cancellationToken = default);

    Task<EvidenceFileVersionDto?> MarkLatestCurrentTenantFileVersionDeletedAsync(
        Guid evidenceItemId,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}
