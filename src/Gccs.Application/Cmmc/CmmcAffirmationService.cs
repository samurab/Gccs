using Gccs.Application.Audit;
using Gccs.Domain.Audit;

namespace Gccs.Application.Cmmc;

public sealed class CmmcAffirmationService(
    ICmmcAffirmationRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public Task<IReadOnlyList<CmmcAffirmationDto>> ListCurrentTenantAsync(CancellationToken cancellationToken = default) =>
        repository.ListCurrentTenantAsync(cancellationToken);

    public async Task<CmmcAffirmationDto> CreateAsync(
        UpsertCmmcAffirmationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        Validate(normalized);
        var created = await repository.CreateAsync(normalized, actorUserId, cancellationToken);
        await WriteAuditAsync(created, actorUserId, AuditAction.Created, "CMMC affirmation was created.", null, cancellationToken);
        return created;
    }

    public async Task<CmmcAffirmationDto?> UpdateAsync(
        Guid affirmationId,
        UpsertCmmcAffirmationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var before = (await repository.ListCurrentTenantAsync(cancellationToken)).FirstOrDefault(item => item.Id == affirmationId);
        var normalized = Normalize(request);
        Validate(normalized);
        var updated = await repository.UpdateAsync(affirmationId, normalized, actorUserId, cancellationToken);
        if (updated is not null)
        {
            var summary = before?.Status != updated.Status
                ? $"CMMC affirmation status changed to {updated.Status}."
                : "CMMC affirmation was updated.";
            await WriteAuditAsync(updated, actorUserId, AuditAction.Updated, summary, before?.Status.ToString(), cancellationToken);
        }

        return updated;
    }

    private async Task WriteAuditAsync(
        CmmcAffirmationDto affirmation,
        Guid actorUserId,
        AuditAction action,
        string summary,
        string? previousStatus,
        CancellationToken cancellationToken)
    {
        var metadata = new Dictionary<string, string>
        {
            ["level"] = affirmation.Level.ToString(),
            ["dueAt"] = affirmation.DueAt.ToString("yyyy-MM-dd"),
            ["status"] = affirmation.Status.ToString(),
            ["evidenceCount"] = affirmation.EvidenceItemIds.Count.ToString()
        };

        if (previousStatus is not null)
        {
            metadata["previousStatus"] = previousStatus;
        }

        await auditEventWriter.WriteAsync(
            affirmation.TenantId,
            actorUserId,
            action,
            "CmmcAffirmation",
            affirmation.Id.ToString(),
            summary,
            metadata,
            cancellationToken);
    }

    private static UpsertCmmcAffirmationRequest Normalize(UpsertCmmcAffirmationRequest request) =>
        request with
        {
            ConfirmationReference = string.IsNullOrWhiteSpace(request.ConfirmationReference) ? null : request.ConfirmationReference.Trim(),
            EvidenceItemIds = request.EvidenceItemIds.Distinct().OrderBy(id => id).ToArray()
        };

    private static void Validate(UpsertCmmcAffirmationRequest request)
    {
        if (request.SubmittedAt.HasValue && request.SubmittedAt.Value > request.DueAt.AddYears(1))
        {
            throw new CmmcAffirmationValidationException("Submitted date is outside the supported affirmation window.");
        }
    }
}

public sealed class CmmcAffirmationValidationException(string message) : InvalidOperationException(message);
