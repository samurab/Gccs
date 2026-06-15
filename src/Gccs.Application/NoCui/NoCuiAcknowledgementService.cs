using Gccs.Application.Audit;
using Gccs.Application.Security;
using Gccs.Domain.Audit;

namespace Gccs.Application.NoCui;

public sealed class NoCuiAcknowledgementService(
    INoCuiAcknowledgementRepository repository,
    ICurrentTenantContext tenantContext,
    IAuditEventWriter auditEventWriter)
{
    public async Task<NoCuiAcknowledgementStatusDto> GetCurrentStatusAsync(
        CancellationToken cancellationToken = default)
    {
        var acknowledgement = await repository.FindCurrentUserAcknowledgementAsync(
            NoCuiNotice.CurrentVersion,
            cancellationToken);

        return acknowledgement ?? new NoCuiAcknowledgementStatusDto(
            false,
            NoCuiNotice.CurrentVersion,
            NoCuiNotice.Copy,
            tenantContext.TenantId,
            null,
            null);
    }

    public async Task<NoCuiAcknowledgementStatusDto> AcknowledgeAsync(
        AcknowledgeNoCuiRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateAcknowledgement(request);

        var existing = await repository.FindCurrentUserAcknowledgementAsync(
            request.NoticeVersion,
            cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var acknowledgedAt = DateTimeOffset.UtcNow;
        var acknowledgement = await repository.AddCurrentUserAcknowledgementAsync(
            request.NoticeVersion,
            NoCuiNotice.Copy,
            actorUserId,
            acknowledgedAt,
            cancellationToken);

        await auditEventWriter.WriteAsync(
            tenantContext.TenantId,
            actorUserId,
            AuditAction.Created,
            "NoCuiAcknowledgement",
            $"{tenantContext.TenantId}:{actorUserId}:{request.NoticeVersion}",
            "No-CUI notice was acknowledged before upload access was enabled.",
            new Dictionary<string, string>
            {
                ["noticeVersion"] = acknowledgement.NoticeVersion,
                ["acknowledgedAt"] = acknowledgement.AcknowledgedAt?.ToString("O") ?? string.Empty,
                ["noticeCopy"] = acknowledgement.NoticeCopy
            },
            cancellationToken);

        return acknowledgement;
    }

    public async Task<EvidenceUploadIntentDto> CreateEvidenceUploadIntentAsync(
        Guid evidenceItemId,
        EvidenceUploadIntentRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FileName) || request.FileName.Trim().Length > 240)
        {
            throw new ArgumentException("A file name is required and must be 240 characters or fewer.", nameof(request));
        }

        var acknowledgement = await repository.FindCurrentUserAcknowledgementAsync(
            NoCuiNotice.CurrentVersion,
            cancellationToken);

        if (acknowledgement is null)
        {
            throw new NoCuiAcknowledgementRequiredException(
                "No-CUI acknowledgement is required before evidence upload is enabled.");
        }

        return new EvidenceUploadIntentDto(
            Guid.NewGuid(),
            evidenceItemId,
            tenantContext.TenantId,
            actorUserId,
            "upload-pending",
            "No-CUI acknowledgement is on record. Evidence upload storage will be enabled by the upload guardrails story.",
            acknowledgement.NoticeVersion,
            DateTimeOffset.UtcNow.AddMinutes(15));
    }

    private static void ValidateAcknowledgement(AcknowledgeNoCuiRequest request)
    {
        if (!request.Acknowledged)
        {
            throw new ArgumentException("The No-CUI notice must be acknowledged before upload is enabled.", nameof(request));
        }

        if (!string.Equals(request.NoticeVersion, NoCuiNotice.CurrentVersion, StringComparison.Ordinal))
        {
            throw new ArgumentException("The No-CUI notice version is not current.", nameof(request));
        }
    }
}

public sealed class NoCuiAcknowledgementRequiredException(string message) : InvalidOperationException(message);

