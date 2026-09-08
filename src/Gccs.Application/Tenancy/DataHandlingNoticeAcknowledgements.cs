using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Domain.Audit;
using Gccs.Domain.Tenancy;

namespace Gccs.Application.Tenancy;

public sealed class DataHandlingNoticeAcknowledgementService(
    IDataHandlingNoticeAcknowledgementRepository repository,
    IAuditEventWriter auditEventWriter,
    IApplicationTransaction transaction)
{
    public async Task<IReadOnlyList<DataHandlingNoticeAcknowledgementDto>> ListAsync(
        Guid tenantId,
        Guid userId,
        DataHandlingNoticeDto currentNotice,
        string workflowContext,
        CancellationToken cancellationToken = default)
    {
        var acknowledgements = await repository.ListAsync(tenantId, userId, cancellationToken);
        return acknowledgements.Select(acknowledgement => WithStatus(acknowledgement, currentNotice, workflowContext)).ToArray();
    }

    public async Task<DataHandlingNoticeAcknowledgementDto> AcknowledgeAsync(
        Guid tenantId,
        Guid userId,
        DataHandlingNoticeDto currentNotice,
        AcknowledgeDataHandlingNoticeRequest request,
        CancellationToken cancellationToken = default)
        => await AcknowledgeCoreAsync(tenantId, userId, currentNotice, request,
            CurrentDataHandlingNoticeService.PublishedNoticeWorkflow(request.WorkflowContext), false, cancellationToken);

    // Only the explicit tenant-admin readiness endpoint uses this path. It acknowledges
    // the proposed mode's notice without granting that mode or bypassing its approval gate.
    public Task<DataHandlingNoticeAcknowledgementDto> AcknowledgeReadinessAsync(Guid tenantId, Guid userId,
        DataHandlingNoticeDto notice, AcknowledgeDataHandlingNoticeRequest request, CancellationToken ct)
    {
        if (notice.Mode != TenantDataPosture.CuiReady || request.WorkflowContext != "Onboarding")
            throw new DataHandlingNoticeAcknowledgementRequiredException("Readiness acknowledgement must cover CuiReady onboarding.");
        return AcknowledgeCoreAsync(tenantId, userId, notice, request, request.WorkflowContext, true, ct);
    }

    private async Task<DataHandlingNoticeAcknowledgementDto> AcknowledgeCoreAsync(Guid tenantId, Guid userId,
        DataHandlingNoticeDto currentNotice, AcknowledgeDataHandlingNoticeRequest request,
        string publishedWorkflowContext, bool readiness, CancellationToken cancellationToken)
    {
        ValidateRequest(currentNotice, request, publishedWorkflowContext);
        request = request with { WorkflowContext = request.WorkflowContext.Trim() };
        return await transaction.ExecuteAsync(async cancellationToken =>
        {
            if (!readiness) await repository.EnsureTenantModeAsync(tenantId, currentNotice.Mode, cancellationToken);
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
                return WithStatus(existing, currentNotice, request.WorkflowContext);
            }

            var isRenewal = (await repository.ListAsync(tenantId, userId, cancellationToken)).Any(previous =>
                previous.Mode == currentNotice.Mode && previous.WorkflowContext == request.WorkflowContext &&
                (previous.NoticeId != currentNotice.NoticeId || previous.NoticeVersion != currentNotice.Version));
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
                isRenewal ? AuditAction.Updated : AuditAction.Created,
                "DataHandlingNoticeAcknowledgement",
                acknowledgement.Id.ToString(),
                isRenewal ? "Data handling notice acknowledgement was renewed for a CUI-relevant workflow."
                    : "Data handling notice was acknowledged for a CUI-relevant workflow.",
                new Dictionary<string, string>
                {
                    ["tenantId"] = tenantId.ToString(),
                    ["userId"] = userId.ToString(),
                    ["mode"] = currentNotice.Mode.ToString(),
                    ["workflowContext"] = request.WorkflowContext.Trim(),
                    ["noticeId"] = currentNotice.NoticeId,
                    ["noticeVersion"] = currentNotice.Version,
                    ["acknowledgedAt"] = acknowledgedAt.ToString("O"),
                    ["result"] = isRenewal ? "renewed" : "acknowledged"
                },
                cancellationToken);

            return WithStatus(acknowledgement, currentNotice, request.WorkflowContext);
        }, cancellationToken);
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
            $"Data handling notice acknowledgement is required for {currentNotice.Mode} {workflowContext} before continuing.", workflowContext);
    }

    private static void ValidateRequest(DataHandlingNoticeDto currentNotice, AcknowledgeDataHandlingNoticeRequest request,
        string publishedWorkflowContext)
    {
        if (currentNotice.State != "Published" || currentNotice.EffectiveAt > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new DataHandlingNoticeAcknowledgementRequiredException("Only a currently effective published notice can be acknowledged.");
        if (!request.Acknowledged)
        {
            throw new DataHandlingNoticeAcknowledgementRequiredException("Data handling notice acknowledgement is required.");
        }

        if (string.IsNullOrWhiteSpace(request.WorkflowContext))
        {
            throw new DataHandlingNoticeAcknowledgementRequiredException("Workflow context is required.");
        }

        if (!currentNotice.WorkflowContexts.Contains(publishedWorkflowContext.Trim(), StringComparer.Ordinal))
            throw new DataHandlingNoticeAcknowledgementRequiredException("The current notice does not cover this workflow.");

        if (request.Mode != currentNotice.Mode ||
            !string.Equals(request.NoticeId, currentNotice.NoticeId, StringComparison.Ordinal) ||
            !string.Equals(request.NoticeVersion, currentNotice.Version, StringComparison.Ordinal))
        {
            throw new DataHandlingNoticeAcknowledgementRequiredException("Data handling notice acknowledgement must reference the current published notice.");
        }
    }

    private static DataHandlingNoticeAcknowledgementDto WithStatus(
        DataHandlingNoticeAcknowledgementDto acknowledgement,
        DataHandlingNoticeDto currentNotice,
        string workflowContext)
    {
        var status = acknowledgement.Mode == currentNotice.Mode &&
            acknowledgement.WorkflowContext == workflowContext.Trim() &&
            acknowledgement.NoticeId == currentNotice.NoticeId &&
            acknowledgement.NoticeVersion == currentNotice.Version
                ? DataHandlingNoticeAcknowledgementStatus.Current
                : DataHandlingNoticeAcknowledgementStatus.Outdated;

        return acknowledgement with { Status = status };
    }
}

public interface IDataHandlingNoticeAcknowledgementRepository
{
    Task EnsureTenantModeAsync(Guid tenantId, TenantDataPosture mode, CancellationToken cancellationToken = default);

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

public sealed class DataHandlingNoticeAcknowledgementRequiredException(string message, string? workflowContext = null) : InvalidOperationException(message)
{
    public string? WorkflowContext { get; } = workflowContext;
}

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
