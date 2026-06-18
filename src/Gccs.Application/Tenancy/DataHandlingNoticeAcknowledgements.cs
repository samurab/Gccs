using Gccs.Application.Audit;
using Gccs.Domain.Audit;
using Gccs.Domain.Tenancy;

namespace Gccs.Application.Tenancy;

public sealed class DataHandlingNoticeAcknowledgementService(
    IDataHandlingNoticeAcknowledgementRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public async Task<IReadOnlyList<DataHandlingNoticeAcknowledgementDto>> ListAsync(
        Guid tenantId,
        Guid userId,
        DataHandlingNoticeDto currentNotice,
        CancellationToken cancellationToken = default)
    {
        var acknowledgements = await repository.ListAsync(tenantId, userId, cancellationToken);
        return acknowledgements.Select(acknowledgement => WithStatus(acknowledgement, currentNotice)).ToArray();
    }

    public async Task<DataHandlingNoticeAcknowledgementDto> AcknowledgeAsync(
        Guid tenantId,
        Guid userId,
        DataHandlingNoticeDto currentNotice,
        AcknowledgeDataHandlingNoticeRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(currentNotice, request);
        var existing = await repository.FindAsync(
            tenantId,
            userId,
            currentNotice.Mode,
            request.WorkflowContext,
            currentNotice.NoticeId,
            currentNotice.Version,
            cancellationToken);

        if (existing is not null)
        {
            return WithStatus(existing, currentNotice);
        }

        var acknowledgedAt = DateTimeOffset.UtcNow;
        var acknowledgement = await repository.AddAsync(
            tenantId,
            userId,
            currentNotice.Mode,
            request.WorkflowContext.Trim(),
            currentNotice.NoticeId,
            currentNotice.Version,
            acknowledgedAt,
            cancellationToken);

        await auditEventWriter.WriteAsync(
            tenantId,
            userId,
            AuditAction.Created,
            "DataHandlingNoticeAcknowledgement",
            acknowledgement.Id.ToString(),
            "Data handling notice was acknowledged for a CUI-relevant workflow.",
            new Dictionary<string, string>
            {
                ["tenantId"] = tenantId.ToString(),
                ["userId"] = userId.ToString(),
                ["mode"] = currentNotice.Mode.ToString(),
                ["workflowContext"] = request.WorkflowContext.Trim(),
                ["noticeId"] = currentNotice.NoticeId,
                ["noticeVersion"] = currentNotice.Version,
                ["acknowledgedAt"] = acknowledgedAt.ToString("O"),
                ["result"] = "acknowledged"
            },
            cancellationToken);

        return WithStatus(acknowledgement, currentNotice);
    }

    public async Task EnsureAcknowledgedAsync(
        Guid tenantId,
        Guid userId,
        DataHandlingNoticeDto currentNotice,
        string workflowContext,
        CancellationToken cancellationToken = default)
    {
        var acknowledgement = await repository.FindAsync(
            tenantId,
            userId,
            currentNotice.Mode,
            workflowContext,
            currentNotice.NoticeId,
            currentNotice.Version,
            cancellationToken);

        if (acknowledgement is not null)
        {
            return;
        }

        throw new DataHandlingNoticeAcknowledgementRequiredException(
            $"Data handling notice acknowledgement is required for {currentNotice.Mode} {workflowContext} before continuing.");
    }

    private static void ValidateRequest(DataHandlingNoticeDto currentNotice, AcknowledgeDataHandlingNoticeRequest request)
    {
        if (!request.Acknowledged)
        {
            throw new DataHandlingNoticeAcknowledgementRequiredException("Data handling notice acknowledgement is required.");
        }

        if (string.IsNullOrWhiteSpace(request.WorkflowContext))
        {
            throw new DataHandlingNoticeAcknowledgementRequiredException("Workflow context is required.");
        }

        if (request.Mode != currentNotice.Mode ||
            !string.Equals(request.NoticeId, currentNotice.NoticeId, StringComparison.Ordinal) ||
            !string.Equals(request.NoticeVersion, currentNotice.Version, StringComparison.Ordinal))
        {
            throw new DataHandlingNoticeAcknowledgementRequiredException("Data handling notice acknowledgement must reference the current published notice.");
        }
    }

    private static DataHandlingNoticeAcknowledgementDto WithStatus(
        DataHandlingNoticeAcknowledgementDto acknowledgement,
        DataHandlingNoticeDto currentNotice)
    {
        var status = acknowledgement.Mode == currentNotice.Mode &&
            acknowledgement.NoticeId == currentNotice.NoticeId &&
            acknowledgement.NoticeVersion == currentNotice.Version
                ? DataHandlingNoticeAcknowledgementStatus.Current
                : DataHandlingNoticeAcknowledgementStatus.Outdated;

        return acknowledgement with { Status = status };
    }
}

public interface IDataHandlingNoticeAcknowledgementRepository
{
    Task<IReadOnlyList<DataHandlingNoticeAcknowledgementDto>> ListAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<DataHandlingNoticeAcknowledgementDto?> FindAsync(
        Guid tenantId,
        Guid userId,
        TenantDataPosture mode,
        string workflowContext,
        string noticeId,
        string noticeVersion,
        CancellationToken cancellationToken = default);

    Task<DataHandlingNoticeAcknowledgementDto> AddAsync(
        Guid tenantId,
        Guid userId,
        TenantDataPosture mode,
        string workflowContext,
        string noticeId,
        string noticeVersion,
        DateTimeOffset acknowledgedAt,
        CancellationToken cancellationToken = default);
}

public sealed class DataHandlingNoticeAcknowledgementRequiredException(string message) : InvalidOperationException(message);

public sealed record AcknowledgeDataHandlingNoticeRequest(
    TenantDataPosture Mode,
    string WorkflowContext,
    string NoticeId,
    string NoticeVersion,
    bool Acknowledged);

public sealed record DataHandlingNoticeAcknowledgementDto(
    Guid Id,
    Guid TenantId,
    Guid UserId,
    TenantDataPosture Mode,
    string WorkflowContext,
    string NoticeId,
    string NoticeVersion,
    DateTimeOffset AcknowledgedAt,
    DataHandlingNoticeAcknowledgementStatus Status);

public enum DataHandlingNoticeAcknowledgementStatus
{
    Current,
    Outdated
}
