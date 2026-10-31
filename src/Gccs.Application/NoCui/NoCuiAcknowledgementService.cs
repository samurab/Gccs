using Gccs.Application.Common;
using Gccs.Application.Audit;
using Gccs.Application.Security;
using Gccs.Domain.Audit;

namespace Gccs.Application.NoCui;

public sealed class NoCuiAcknowledgementService(
    INoCuiAcknowledgementRepository repository,
    ICurrentTenantContext tenantContext,
    IAuditEventWriter auditEventWriter,
    IApplicationTransaction transaction)
{
    public async Task<NoCuiAcknowledgementStatusDto> AcknowledgeAsync(
        AcknowledgeNoCuiRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        return await transaction.ExecuteAsync(async transactionToken =>
        {
            ValidateAcknowledgement(request);

            var existing = await repository.FindCurrentUserAcknowledgementAsync(
                request.NoticeVersion,
                transactionToken);

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
                transactionToken);

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
                transactionToken);

            return acknowledgement;
        }, cancellationToken);
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
