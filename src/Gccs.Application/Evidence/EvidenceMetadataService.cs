using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;

namespace Gccs.Application.Evidence;

public sealed class EvidenceMetadataService(
    IEvidenceMetadataRepository repository,
    IAuditEventWriter auditEventWriter,
    ContentClassificationPolicy classificationPolicy)
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
        var normalized = Normalize(request);
        Validate(normalized);
        await classificationPolicy.EnsureAllowedAsync(
            normalized.Classification ?? ContentClassificationPolicy.DefaultUnclassified(),
            TenantDataHandlingWorkflow.EvidenceUpload,
            actorUserId,
            "EvidenceItem",
            null,
            cancellationToken);
        var created = await repository.CreateCurrentTenantAsync(normalized, actorUserId, cancellationToken);
        await WriteAuditAsync(created, actorUserId, AuditAction.Created, "Evidence metadata was created.", cancellationToken);
        return created;
    }

    public async Task<EvidenceMetadataDto?> UpdateAsync(
        Guid evidenceItemId,
        UpsertEvidenceMetadataRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        Validate(normalized);
        await classificationPolicy.EnsureAllowedAsync(
            normalized.Classification ?? ContentClassificationPolicy.DefaultUnclassified(),
            TenantDataHandlingWorkflow.EvidenceUpload,
            actorUserId,
            "EvidenceItem",
            evidenceItemId.ToString(),
            cancellationToken);
        var updated = await repository.UpdateCurrentTenantAsync(evidenceItemId, normalized, actorUserId, cancellationToken);

        if (updated is null)
        {
            return null;
        }

        await WriteAuditAsync(updated, actorUserId, AuditAction.Updated, "Evidence metadata was updated.", cancellationToken);
        return updated;
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
    }
}

public sealed class EvidenceMetadataValidationException(string message) : InvalidOperationException(message);
