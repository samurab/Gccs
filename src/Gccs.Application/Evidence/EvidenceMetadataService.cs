using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;

namespace Gccs.Application.Evidence;

public sealed class EvidenceMetadataService(
    IEvidenceMetadataRepository repository,
    IAuditEventWriter auditEventWriter,
    ContentClassificationPolicy classificationPolicy,
    IApplicationTransaction transaction)
{
    public Task<IReadOnlyList<EvidenceMetadataDto>> ListCurrentTenantAsync(
        EvidenceMetadataQuery query,
        CancellationToken cancellationToken = default) =>
        repository.ListCurrentTenantAsync(query, cancellationToken);

    public Task<EvidenceMetadataDto?> FindCurrentTenantAsync(
        Guid evidenceItemId,
        CancellationToken cancellationToken = default) =>
        repository.FindCurrentTenantAsync(evidenceItemId, cancellationToken);

    public async Task<EvidenceMetadataDto> CreateAsync(
        UpsertEvidenceMetadataRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        return await transaction.ExecuteAsync(async transactionToken =>
        {
            var normalized = Normalize(request);
            Validate(normalized);
            await ValidateReferencesAsync(normalized, transactionToken);
            await classificationPolicy.EnsureAllowedAsync(
                normalized.Classification ?? ContentClassificationPolicy.DefaultUnclassified(),
                TenantDataHandlingWorkflow.EvidenceUpload,
                actorUserId,
                "EvidenceItem",
                null,
                transactionToken);
            var created = await repository.CreateCurrentTenantAsync(normalized, actorUserId, transactionToken);
            await WriteAuditAsync(
                created,
                actorUserId,
                AuditAction.Created,
                $"Evidence metadata '{created.Title}' was created.",
                transactionToken);
            return created;
        }, cancellationToken);
    }

    public async Task<EvidenceMetadataDto?> UpdateAsync(
        Guid evidenceItemId,
        UpsertEvidenceMetadataRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        return await transaction.ExecuteAsync(async transactionToken =>
        {
            if (!await repository.ExistsCurrentTenantAsync(evidenceItemId, transactionToken))
            {
                return null;
            }

            var normalized = Normalize(request);
            Validate(normalized);
            await ValidateReferencesAsync(normalized, transactionToken);
            await classificationPolicy.EnsureAllowedAsync(
                normalized.Classification ?? ContentClassificationPolicy.DefaultUnclassified(),
                TenantDataHandlingWorkflow.EvidenceUpload,
                actorUserId,
                "EvidenceItem",
                evidenceItemId.ToString(),
                transactionToken);
            var updated = await repository.UpdateCurrentTenantAsync(evidenceItemId, normalized, actorUserId, transactionToken);

            if (updated is null)
            {
                return null;
            }

            await WriteAuditAsync(
                updated,
                actorUserId,
                AuditAction.Updated,
                $"Evidence metadata '{updated.Title}' was updated.",
                transactionToken);
            return updated;
        }, cancellationToken);
    }

    private async Task WriteAuditAsync(
        EvidenceMetadataDto evidence,
        Guid actorUserId,
        AuditAction action,
        string summary,
        CancellationToken cancellationToken)
    {
        await auditEventWriter.WriteAsync(
            evidence.TenantId,
            actorUserId,
            action,
            "EvidenceItem",
            evidence.Id.ToString(),
            summary,
            new Dictionary<string, string>
            {
                ["title"] = evidence.Title,
                ["status"] = evidence.Status.ToString(),
                ["ownerFunction"] = evidence.OwnerFunction,
                ["tagCount"] = evidence.Tags.Count.ToString(),
                ["tags"] = string.Join(",", evidence.Tags),
                ["expiresAt"] = evidence.ExpiresAt?.ToString("yyyy-MM-dd") ?? string.Empty
            },
            cancellationToken);
    }

    private static UpsertEvidenceMetadataRequest Normalize(UpsertEvidenceMetadataRequest request) =>
        request with
        {
            Title = request.Title.Trim(),
            OwnerFunction = request.OwnerFunction.Trim(),
            Description = request.Description.Trim(),
            Tags = request.Tags
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(tag => tag)
                .ToArray(),
            ObligationIds = request.ObligationIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(id => id)
                .ToArray(),
            ControlIds = request.ControlIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(id => id)
                .ToArray(),
            ContractIds = request.ContractIds.Distinct().OrderBy(id => id).ToArray(),
            VendorIds = request.VendorIds.Distinct().OrderBy(id => id).ToArray(),
            SubcontractorIds = request.SubcontractorIds.Distinct().OrderBy(id => id).ToArray(),
            EmployeeIds = request.EmployeeIds.Distinct().OrderBy(id => id).ToArray(),
            ReportIds = request.ReportIds.Distinct().OrderBy(id => id).ToArray()
        };

    private static void Validate(UpsertEvidenceMetadataRequest request)
    {
        ContentClassificationPolicy.ValidateUserSelection(request.Classification ??
            throw new ContentClassificationValidationException("Explicit evidence classification is required."));
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new EvidenceMetadataValidationException("Evidence title is required.");
        }

        if (string.IsNullOrWhiteSpace(request.OwnerFunction))
        {
            throw new EvidenceMetadataValidationException("Evidence owner is required.");
        }

        if (request.Title.Length > 240)
        {
            throw new EvidenceMetadataValidationException("Evidence title must be 240 characters or fewer.");
        }

        if (request.Tags.Any(tag => tag.Length > 80))
        {
            throw new EvidenceMetadataValidationException("Evidence tags must be 80 characters or fewer.");
        }

        if (request.EffectiveAt is { } effectiveAt &&
            request.ExpiresAt is { } expiresAt &&
            expiresAt < effectiveAt)
        {
            throw new EvidenceMetadataValidationException("Expires date must be on or after Effective date.");
        }
    }

    private async Task ValidateReferencesAsync(
        UpsertEvidenceMetadataRequest request,
        CancellationToken cancellationToken)
    {
        var missingControlIds = await repository.FindMissingControlIdsAsync(request.ControlIds, cancellationToken);
        var invalidTenantReferences = await repository.FindInvalidCurrentTenantReferenceIdsAsync(request, cancellationToken);

        if (missingControlIds.Count == 0 && invalidTenantReferences.Count == 0)
        {
            return;
        }

        var errors = new List<string>();
        foreach (var invalidReference in invalidTenantReferences.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            errors.Add($"{invalidReference.Key}: {string.Join(", ", invalidReference.Value)}");
        }

        if (errors.Count > 0)
        {
            throw new EvidenceMetadataValidationException(
                $"Evidence references must belong to the current tenant. Invalid references: {string.Join("; ", errors)}.");
        }

        throw new EvidenceMetadataValidationException(
            missingControlIds.Count == 1
                ? $"Control '{missingControlIds[0]}' was not found. Select a control from the suggestions or leave Controls blank."
                : $"Controls '{string.Join("', '", missingControlIds)}' were not found. Select controls from the suggestions or leave Controls blank.");
    }
}

public sealed class EvidenceMetadataValidationException(string message) : InvalidOperationException(message);
