using Gccs.Application.Tenancy;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Tenancy;

public sealed class EfDataHandlingNoticeAcknowledgementRepository(GccsDbContext dbContext)
    : IDataHandlingNoticeAcknowledgementRepository
{
    public async Task<IReadOnlyList<DataHandlingNoticeAcknowledgementDto>> ListAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var acknowledgements = await dbContext.DataHandlingNoticeAcknowledgements
            .AsNoTracking()
            .Where(acknowledgement => acknowledgement.TenantId == tenantId && acknowledgement.UserId == userId)
            .OrderByDescending(acknowledgement => acknowledgement.AcknowledgedAt)
            .ToArrayAsync(cancellationToken);

        return acknowledgements.Select(ToDto).ToArray();
    }

    public async Task<DataHandlingNoticeAcknowledgementDto?> FindAsync(
        Guid tenantId,
        Guid userId,
        TenantDataPosture mode,
        string workflowContext,
        string noticeId,
        string noticeVersion,
        CancellationToken cancellationToken = default)
    {
        var context = Normalize(workflowContext);
        var acknowledgement = await dbContext.DataHandlingNoticeAcknowledgements
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.TenantId == tenantId &&
                    candidate.UserId == userId &&
                    candidate.Mode == mode &&
                    candidate.WorkflowContext == context &&
                    candidate.NoticeId == noticeId &&
                    candidate.NoticeVersion == noticeVersion,
                cancellationToken);

        return acknowledgement is null ? null : ToDto(acknowledgement);
    }

    public async Task<DataHandlingNoticeAcknowledgementDto> AddAsync(
        Guid tenantId,
        Guid userId,
        TenantDataPosture mode,
        string workflowContext,
        string noticeId,
        string noticeVersion,
        DateTimeOffset acknowledgedAt,
        CancellationToken cancellationToken = default)
    {
        var acknowledgement = new DataHandlingNoticeAcknowledgementEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Mode = mode,
            WorkflowContext = Normalize(workflowContext),
            NoticeId = noticeId,
            NoticeVersion = noticeVersion,
            AcknowledgedAt = acknowledgedAt,
            CreatedAt = acknowledgedAt,
            CreatedByUserId = userId
        };

        dbContext.DataHandlingNoticeAcknowledgements.Add(acknowledgement);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(acknowledgement);
    }

    private static DataHandlingNoticeAcknowledgementDto ToDto(DataHandlingNoticeAcknowledgementEntity entity) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.UserId,
            entity.Mode,
            entity.WorkflowContext,
            entity.NoticeId,
            entity.NoticeVersion,
            entity.AcknowledgedAt,
            DataHandlingNoticeAcknowledgementStatus.Outdated);

    private static string Normalize(string value) => value.Trim();
}
