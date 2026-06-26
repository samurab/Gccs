using Gccs.Application.Audit;
using Gccs.Domain.Audit;
using Gccs.Domain.Cmmc;

namespace Gccs.Application.Cmmc;

public sealed class CmmcPoamService(
    ICmmcPoamRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public Task<IReadOnlyList<CmmcPoamItemDto>> ListCurrentTenantAsync(
        CancellationToken cancellationToken = default) =>
        repository.ListCurrentTenantAsync(cancellationToken);

    public Task<IReadOnlyList<CmmcPoamItemDto>?> ListCurrentTenantAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        repository.ListCurrentTenantAsync(assessmentId, cancellationToken);

    public Task<CmmcPoamItemDto?> FindCurrentTenantAsync(
        Guid poamItemId,
        CancellationToken cancellationToken = default) =>
        repository.FindCurrentTenantAsync(poamItemId, cancellationToken);

    public async Task<CmmcPoamItemDto?> CreateAsync(
        Guid assessmentId,
        UpsertCmmcPoamItemRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        Validate(normalized);
        var created = await repository.CreateAsync(assessmentId, normalized, actorUserId, cancellationToken);
        if (created is not null)
        {
            await WriteAuditAsync(created, actorUserId, AuditAction.Created, "POA&M item was created.", null, cancellationToken);
        }

        return created;
    }

    public async Task<CmmcPoamItemDto?> UpdateAsync(
        Guid assessmentId,
        Guid poamItemId,
        UpsertCmmcPoamItemRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var before = (await repository.ListCurrentTenantAsync(assessmentId, cancellationToken))?
            .FirstOrDefault(item => item.Id == poamItemId);
        var normalized = Normalize(request);
        Validate(normalized);
        var updated = await repository.UpdateAsync(assessmentId, poamItemId, normalized, actorUserId, cancellationToken);
        if (updated is not null)
        {
            var summary = before?.Status != updated.Status
                ? $"POA&M status changed to {updated.Status}."
                : "POA&M item was updated.";
            await WriteAuditAsync(updated, actorUserId, AuditAction.Updated, summary, before?.Status.ToString(), cancellationToken);
        }

        return updated;
    }

    public async Task<CmmcPoamItemDto?> UpdateCurrentTenantAsync(
        Guid poamItemId,
        UpsertCmmcPoamItemRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var before = await repository.FindCurrentTenantAsync(poamItemId, cancellationToken);
        var normalized = Normalize(request);
        Validate(normalized);
        var updated = await repository.UpdateCurrentTenantAsync(poamItemId, normalized, actorUserId, cancellationToken);
        if (updated is not null)
        {
            var summary = before?.Status != updated.Status
                ? $"POA&M status changed to {updated.Status}."
                : "POA&M item was updated.";
            await WriteAuditAsync(updated, actorUserId, AuditAction.Updated, summary, before?.Status.ToString(), cancellationToken);
        }

        return updated;
    }

    public async Task<CmmcPoamItemDto?> CloseCurrentTenantAsync(
        Guid poamItemId,
        Guid actorUserId,
        DateOnly closedAt,
        CancellationToken cancellationToken = default)
    {
        var before = await repository.FindCurrentTenantAsync(poamItemId, cancellationToken);
        var closed = await repository.CloseCurrentTenantAsync(poamItemId, actorUserId, closedAt, cancellationToken);
        if (closed is not null)
        {
            await WriteAuditAsync(closed, actorUserId, AuditAction.Updated, "POA&M item was closed.", before?.Status.ToString(), cancellationToken);
        }

        return closed;
    }

    private async Task WriteAuditAsync(
        CmmcPoamItemDto item,
        Guid actorUserId,
        AuditAction action,
        string summary,
        string? previousStatus,
        CancellationToken cancellationToken)
    {
        var metadata = new Dictionary<string, string>
        {
            ["assessmentId"] = item.AssessmentId.ToString(),
            ["controlId"] = item.ControlId,
            ["riskLevel"] = item.RiskLevel.ToString(),
            ["status"] = item.Status.ToString(),
            ["remediationTaskId"] = item.RemediationTaskId?.ToString() ?? string.Empty
        };

        if (previousStatus is not null)
        {
            metadata["previousStatus"] = previousStatus;
        }

        await auditEventWriter.WriteAsync(
            item.TenantId,
            actorUserId,
            action,
            "CmmcPoamItem",
            item.Id.ToString(),
            summary,
            metadata,
            cancellationToken);
    }

    private static UpsertCmmcPoamItemRequest Normalize(UpsertCmmcPoamItemRequest request) =>
        request with
        {
            ControlId = request.ControlId.Trim(),
            Weakness = request.Weakness.Trim(),
            PlannedRemediation = request.PlannedRemediation.Trim(),
            OwnerFunction = request.OwnerFunction.Trim(),
            EvidenceItemIds = request.EvidenceItemIds.Distinct().OrderBy(id => id).ToArray()
        };

    private static void Validate(UpsertCmmcPoamItemRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ControlId))
        {
            throw new CmmcPoamValidationException("Control id is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Weakness))
        {
            throw new CmmcPoamValidationException("Gap or weakness is required.");
        }

        if (string.IsNullOrWhiteSpace(request.PlannedRemediation))
        {
            throw new CmmcPoamValidationException("Remediation plan is required.");
        }

        if (string.IsNullOrWhiteSpace(request.OwnerFunction))
        {
            throw new CmmcPoamValidationException("Owner function is required.");
        }

        if (request.Status == PoamStatus.Closed && request.CompletedAt is null)
        {
            throw new CmmcPoamValidationException("Closed POA&M items require a completed date.");
        }

        if (request.Status != PoamStatus.Closed && request.CompletedAt.HasValue)
        {
            throw new CmmcPoamValidationException("Completed date is only allowed when the POA&M item is closed.");
        }

        if (request.CompletedAt.HasValue && request.CompletedAt.Value < request.TargetCompletionAt.AddYears(-5))
        {
            throw new CmmcPoamValidationException("Completed date is outside the supported remediation window.");
        }
    }
}

public sealed class CmmcPoamValidationException(string message) : InvalidOperationException(message);
