using Gccs.Application.NoCui;
using Gccs.Application.Security;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.NoCui;

public sealed class EfNoCuiAcknowledgementRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : INoCuiAcknowledgementRepository
{
    public async Task<NoCuiAcknowledgementStatusDto?> FindCurrentUserAcknowledgementAsync(
        string noticeVersion,
        CancellationToken cancellationToken = default)
    {
        var acknowledgement = await dbContext.NoCuiAcknowledgements
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.TenantId == tenantContext.TenantId &&
                    candidate.UserId == tenantContext.UserId &&
                    candidate.NoticeVersion == noticeVersion,
                cancellationToken);

        return acknowledgement is null ? null : ToDto(acknowledgement);
    }

    public async Task<NoCuiAcknowledgementStatusDto> AddCurrentUserAcknowledgementAsync(
        string noticeVersion,
        string noticeCopy,
        Guid actorUserId,
        DateTimeOffset acknowledgedAt,
        CancellationToken cancellationToken = default)
    {
        var acknowledgement = new NoCuiAcknowledgementEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            UserId = tenantContext.UserId,
            NoticeVersion = noticeVersion,
            NoticeCopy = noticeCopy,
            AcknowledgedAt = acknowledgedAt,
            CreatedAt = acknowledgedAt,
            CreatedByUserId = actorUserId
        };

        dbContext.NoCuiAcknowledgements.Add(acknowledgement);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(acknowledgement);
    }

    private static NoCuiAcknowledgementStatusDto ToDto(NoCuiAcknowledgementEntity acknowledgement) =>
        new(
            true,
            acknowledgement.NoticeVersion,
            acknowledgement.NoticeCopy,
            acknowledgement.TenantId,
            acknowledgement.UserId,
            acknowledgement.AcknowledgedAt);
}

