using Gccs.Application.Audit;
using Gccs.Domain.Audit;

namespace Gccs.Application.Subcontractors;

public sealed class SubcontractorService(
    ISubcontractorRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public Task<IReadOnlyList<SubcontractorDto>> ListCurrentTenantAsync(CancellationToken cancellationToken = default) =>
        repository.ListCurrentTenantAsync(cancellationToken);

    public Task<SubcontractorDto?> FindCurrentTenantAsync(Guid subcontractorId, CancellationToken cancellationToken = default) =>
        repository.FindCurrentTenantAsync(subcontractorId, cancellationToken);

    public async Task<SubcontractorDto> CreateAsync(
        UpsertSubcontractorRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        Validate(normalized);
        var created = await repository.CreateAsync(normalized, actorUserId, cancellationToken);
        await WriteAuditAsync(created, actorUserId, AuditAction.Created, "Subcontractor profile was created.", null, cancellationToken);
        return created;
    }

    public async Task<SubcontractorDto?> UpdateAsync(
        Guid subcontractorId,
        UpsertSubcontractorRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var before = await repository.FindCurrentTenantAsync(subcontractorId, cancellationToken);
        var normalized = Normalize(request);
        Validate(normalized);
        var updated = await repository.UpdateAsync(subcontractorId, normalized, actorUserId, cancellationToken);
        if (updated is not null)
        {
            await WriteAuditAsync(updated, actorUserId, AuditAction.Updated, "Subcontractor profile was updated.", before?.Status.ToString(), cancellationToken);
        }

        return updated;
    }

    private async Task WriteAuditAsync(
        SubcontractorDto subcontractor,
        Guid actorUserId,
        AuditAction action,
        string summary,
        string? previousStatus,
        CancellationToken cancellationToken)
    {
        var metadata = new Dictionary<string, string>
        {
            ["status"] = subcontractor.Status.ToString(),
            ["hasCuiAccess"] = subcontractor.HasCuiAccess.ToString(),
            ["hasExportControlledAccess"] = subcontractor.HasExportControlledAccess.ToString(),
            ["contractCount"] = subcontractor.ContractIds.Count.ToString()
        };

        if (previousStatus is not null)
        {
            metadata["previousStatus"] = previousStatus;
        }

        await auditEventWriter.WriteAsync(
            subcontractor.TenantId,
            actorUserId,
            action,
            "Subcontractor",
            subcontractor.Id.ToString(),
            summary,
            metadata,
            cancellationToken);
    }

    private static UpsertSubcontractorRequest Normalize(UpsertSubcontractorRequest request) =>
        request with
        {
            Name = request.Name.Trim(),
            Uei = TrimOptional(request.Uei),
            CageCode = TrimOptional(request.CageCode),
            RoleDescription = request.RoleDescription.Trim(),
            SmallBusinessStatus = request.SmallBusinessStatus.Trim(),
            CmmcStatus = request.CmmcStatus.Trim(),
            NdaStatus = request.NdaStatus.Trim(),
            WorkshareDescription = request.WorkshareDescription.Trim(),
            RequiredCmmcLevel = TrimOptional(request.RequiredCmmcLevel),
            ContactName = TrimOptional(request.ContactName),
            ContactEmail = TrimOptional(request.ContactEmail),
            ContactPhone = TrimOptional(request.ContactPhone),
            ContactTitle = TrimOptional(request.ContactTitle),
            ContractIds = request.ContractIds.Distinct().OrderBy(id => id).ToArray()
        };

    private static void Validate(UpsertSubcontractorRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new SubcontractorValidationException("Subcontractor legal name is required.");
        }

        if (request.WorksharePercentage is < 0 or > 100)
        {
            throw new SubcontractorValidationException("Workshare percentage must be between 0 and 100.");
        }
    }

    private static string? TrimOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class SubcontractorValidationException(string message) : InvalidOperationException(message);
