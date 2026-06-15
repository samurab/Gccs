using Gccs.Application.Audit;
using Gccs.Domain.Audit;
using Gccs.Domain.Vendors;

namespace Gccs.Application.Subcontractors;

public sealed class SubcontractorService(
    ISubcontractorRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public Task<IReadOnlyList<SubcontractorDto>> ListCurrentTenantAsync(CancellationToken cancellationToken = default) =>
        repository.ListCurrentTenantAsync(cancellationToken);

    public Task<SubcontractorDto?> FindCurrentTenantAsync(Guid subcontractorId, CancellationToken cancellationToken = default) =>
        repository.FindCurrentTenantAsync(subcontractorId, cancellationToken);

    public Task<IReadOnlyList<SubcontractorFlowDownDto>?> ListFlowDownsAsync(
        Guid subcontractorId,
        Guid? contractId,
        CancellationToken cancellationToken = default) =>
        repository.ListFlowDownsAsync(subcontractorId, contractId, cancellationToken);

    public Task<IReadOnlyList<SubcontractorEvidenceRequestDto>?> ListEvidenceRequestsAsync(
        Guid subcontractorId,
        CancellationToken cancellationToken = default) =>
        repository.ListEvidenceRequestsAsync(subcontractorId, cancellationToken);

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

    public async Task<SubcontractorEvidenceRequestDto?> CreateEvidenceRequestAsync(
        Guid subcontractorId,
        UpsertSubcontractorEvidenceRequestRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        Validate(normalized);
        var created = await repository.CreateEvidenceRequestAsync(subcontractorId, normalized, actorUserId, cancellationToken);
        if (created is not null)
        {
            await WriteEvidenceRequestAuditAsync(
                created,
                actorUserId,
                AuditAction.Created,
                "Subcontractor evidence request was created.",
                null,
                cancellationToken);
        }

        return created;
    }

    public async Task<SubcontractorEvidenceRequestDto?> UpdateEvidenceRequestAsync(
        Guid subcontractorId,
        Guid evidenceRequestId,
        UpsertSubcontractorEvidenceRequestRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var before = await repository.ListEvidenceRequestsAsync(subcontractorId, cancellationToken);
        var previous = before?.SingleOrDefault(candidate => candidate.Id == evidenceRequestId);
        var normalized = Normalize(request);
        Validate(normalized);
        var updated = await repository.UpdateEvidenceRequestAsync(subcontractorId, evidenceRequestId, normalized, actorUserId, cancellationToken);
        if (updated is not null)
        {
            await WriteEvidenceRequestAuditAsync(
                updated,
                actorUserId,
                AuditAction.Updated,
                "Subcontractor evidence request was updated.",
                previous?.Status.ToString(),
                cancellationToken);
        }

        return updated;
    }

    public async Task<SubcontractorFlowDownDto?> CreateFlowDownAsync(
        Guid subcontractorId,
        UpsertSubcontractorFlowDownRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        Validate(normalized);
        var created = await repository.CreateFlowDownAsync(subcontractorId, normalized, actorUserId, cancellationToken);
        if (created is not null)
        {
            await WriteFlowDownAuditAsync(
                created,
                actorUserId,
                AuditAction.Created,
                "Subcontractor flow-down clause was assigned.",
                null,
                cancellationToken);
        }

        return created;
    }

    public async Task<SubcontractorFlowDownDto?> UpdateFlowDownAsync(
        Guid subcontractorId,
        Guid flowDownId,
        UpsertSubcontractorFlowDownRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var before = await repository.ListFlowDownsAsync(subcontractorId, null, cancellationToken);
        var previous = before?.SingleOrDefault(candidate => candidate.Id == flowDownId);
        var normalized = Normalize(request);
        Validate(normalized);
        var updated = await repository.UpdateFlowDownAsync(subcontractorId, flowDownId, normalized, actorUserId, cancellationToken);
        if (updated is not null)
        {
            await WriteFlowDownAuditAsync(
                updated,
                actorUserId,
                AuditAction.Updated,
                "Subcontractor flow-down clause was updated.",
                previous?.Status.ToString(),
                cancellationToken);
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

    private async Task WriteFlowDownAuditAsync(
        SubcontractorFlowDownDto flowDown,
        Guid actorUserId,
        AuditAction action,
        string summary,
        string? previousStatus,
        CancellationToken cancellationToken)
    {
        var subcontractor = await repository.FindCurrentTenantAsync(flowDown.SubcontractorId, cancellationToken) ??
            throw new SubcontractorValidationException("Subcontractor was not found.");
        var metadata = new Dictionary<string, string>
        {
            ["subcontractorId"] = flowDown.SubcontractorId.ToString(),
            ["clauseNumber"] = flowDown.ClauseNumber,
            ["status"] = flowDown.Status.ToString()
        };

        if (flowDown.ContractId is not null)
        {
            metadata["contractId"] = flowDown.ContractId.Value.ToString();
        }

        if (flowDown.ContractClauseId is not null)
        {
            metadata["contractClauseId"] = flowDown.ContractClauseId.Value.ToString();
        }

        if (flowDown.ObligationId is not null)
        {
            metadata["obligationId"] = flowDown.ObligationId;
        }

        if (flowDown.SignedEvidenceItemId is not null)
        {
            metadata["signedEvidenceItemId"] = flowDown.SignedEvidenceItemId.Value.ToString();
        }

        if (previousStatus is not null)
        {
            metadata["previousStatus"] = previousStatus;
        }

        await auditEventWriter.WriteAsync(
            subcontractor.TenantId,
            actorUserId,
            action,
            "SubcontractorFlowDown",
            flowDown.Id.ToString(),
            summary,
            metadata,
            cancellationToken);
    }

    private async Task WriteEvidenceRequestAuditAsync(
        SubcontractorEvidenceRequestDto evidenceRequest,
        Guid actorUserId,
        AuditAction action,
        string summary,
        string? previousStatus,
        CancellationToken cancellationToken)
    {
        var metadata = new Dictionary<string, string>
        {
            ["subcontractorId"] = evidenceRequest.SubcontractorId.ToString(),
            ["status"] = evidenceRequest.Status.ToString(),
            ["dueDate"] = evidenceRequest.DueDate.ToString("O"),
            ["isOverdue"] = evidenceRequest.IsOverdue.ToString()
        };

        if (evidenceRequest.ObligationId is not null)
        {
            metadata["obligationId"] = evidenceRequest.ObligationId;
        }

        if (evidenceRequest.RelatedFlowDownClauseId is not null)
        {
            metadata["relatedFlowDownClauseId"] = evidenceRequest.RelatedFlowDownClauseId.Value.ToString();
        }

        if (evidenceRequest.ReceivedEvidenceItemId is not null)
        {
            metadata["receivedEvidenceItemId"] = evidenceRequest.ReceivedEvidenceItemId.Value.ToString();
        }

        if (previousStatus is not null)
        {
            metadata["previousStatus"] = previousStatus;
        }

        await auditEventWriter.WriteAsync(
            evidenceRequest.TenantId,
            actorUserId,
            action,
            "SubcontractorEvidenceRequest",
            evidenceRequest.Id.ToString(),
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

    private static UpsertSubcontractorFlowDownRequest Normalize(UpsertSubcontractorFlowDownRequest request) =>
        request with
        {
            ObligationId = TrimOptional(request.ObligationId),
            ClauseNumber = request.ClauseNumber.Trim(),
            Title = request.Title.Trim()
        };

    private static UpsertSubcontractorEvidenceRequestRequest Normalize(UpsertSubcontractorEvidenceRequestRequest request) =>
        request with
        {
            RequestedItem = request.RequestedItem.Trim(),
            RequestedEvidenceTypes = request.RequestedEvidenceTypes.Distinct().OrderBy(type => type.ToString()).ToArray(),
            RecipientName = TrimOptional(request.RecipientName),
            RecipientEmail = TrimOptional(request.RecipientEmail),
            ObligationId = TrimOptional(request.ObligationId)
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

    private static void Validate(UpsertSubcontractorFlowDownRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ClauseNumber))
        {
            throw new SubcontractorValidationException("Flow-down clause number is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new SubcontractorValidationException("Flow-down title is required.");
        }

        if (request.Status is FlowDownStatus.NotRequired or FlowDownStatus.Expired)
        {
            throw new SubcontractorValidationException("Flow-down status must be required, sent, acknowledged, signed, waived, or not applicable.");
        }
    }

    private static void Validate(UpsertSubcontractorEvidenceRequestRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RequestedItem))
        {
            throw new SubcontractorValidationException("Requested evidence item is required.");
        }

        if (request.RequestedEvidenceTypes.Count == 0)
        {
            throw new SubcontractorValidationException("At least one requested evidence type is required.");
        }

        if (request.Status is SubcontractorEvidenceRequestStatus.Overdue)
        {
            throw new SubcontractorValidationException("Overdue is derived from due date and open status.");
        }

        if (request.Status is SubcontractorEvidenceRequestStatus.Satisfied && request.ReceivedEvidenceItemId is null)
        {
            throw new SubcontractorValidationException("Received evidence is required to satisfy an evidence request.");
        }
    }

    private static string? TrimOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class SubcontractorValidationException(string message) : InvalidOperationException(message);
