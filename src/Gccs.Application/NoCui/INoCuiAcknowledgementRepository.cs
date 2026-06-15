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

    Task RecordAcceptedEvidenceUploadIntentAsync(
        EvidenceUploadIntentDto uploadIntent,
        CancellationToken cancellationToken = default);
}
