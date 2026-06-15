using Gccs.Application.Audit;
using Gccs.Domain.Audit;

namespace Gccs.Application.Cmmc;

public sealed class CmmcPoamService(
    ICmmcPoamRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public Task<IReadOnlyList<CmmcPoamItemDto>?> ListCurrentTenantAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        repository.ListCurrentTenantAsync(assessmentId, cancellationToken);

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

        if (request.CompletedAt.HasValue && request.CompletedAt.Value < request.TargetCompletionAt.AddYears(-5))
        {
            throw new CmmcPoamValidationException("Completed date is outside the supported remediation window.");
        }
    }
}

public sealed class CmmcPoamValidationException(string message) : InvalidOperationException(message);
