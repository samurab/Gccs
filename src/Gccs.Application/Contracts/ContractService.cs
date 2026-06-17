using Gccs.Application.Audit;
using Gccs.Application.NoCui;
using Gccs.Domain.Audit;
using Gccs.Domain.Companies;
using Gccs.Domain.Contracts;

namespace Gccs.Application.Contracts;

public sealed class ContractService(
    IContractRepository repository,
    INoCuiAcknowledgementRepository noCuiAcknowledgementRepository,
    IAuditEventWriter auditEventWriter,
    IExtractionJobQueue extractionJobQueue)
{
    public Task<IReadOnlyList<ContractDto>> ListCurrentTenantAsync(CancellationToken cancellationToken = default) =>
        repository.ListCurrentTenantAsync(cancellationToken);

    public Task<ContractDto?> FindCurrentTenantAsync(Guid contractId, CancellationToken cancellationToken = default) =>
        repository.FindCurrentTenantAsync(contractId, cancellationToken);

    public Task<IReadOnlyList<ContractDocumentDto>?> ListDocumentsAsync(
        Guid contractId,
        CancellationToken cancellationToken = default) =>
        repository.ListDocumentsAsync(contractId, cancellationToken);

    public Task<IReadOnlyList<ContractDeliverableDto>?> ListDeliverablesAsync(
        Guid contractId,
        CancellationToken cancellationToken = default) =>
        repository.ListDeliverablesAsync(contractId, cancellationToken);

    public Task<IReadOnlyList<ContractClauseDto>?> ListClausesAsync(
        Guid contractId,
        CancellationToken cancellationToken = default) =>
        repository.ListClausesAsync(contractId, cancellationToken);

    public async Task<ContractDto> CreateCurrentTenantAsync(
        UpsertContractRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        Validate(normalized);
        var created = await repository.CreateCurrentTenantAsync(normalized, actorUserId, cancellationToken);
        await WriteAuditAsync(created, actorUserId, AuditAction.Created, cancellationToken);
        return created;
    }

    public async Task<ContractDto?> UpdateCurrentTenantAsync(
        Guid contractId,
        UpsertContractRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        Validate(normalized);
        var updated = await repository.UpdateCurrentTenantAsync(contractId, normalized, actorUserId, cancellationToken);

        if (updated is not null)
        {
            await WriteAuditAsync(updated, actorUserId, AuditAction.Updated, cancellationToken);
        }

        return updated;
    }

    public async Task<ContractDocumentDto?> CreateDocumentMetadataAsync(
        Guid contractId,
        ContractDocumentUploadRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var acknowledgement = await noCuiAcknowledgementRepository.FindCurrentUserAcknowledgementAsync(
            NoCuiNotice.CurrentVersion,
            cancellationToken);

        if (acknowledgement is null)
        {
            throw new NoCuiAcknowledgementRequiredException(
                "No-CUI acknowledgement is required before contract document upload is enabled.");
        }

        var normalized = NormalizeDocument(request);
        var validationErrors = ValidateDocumentUpload(normalized);
        if (validationErrors.Count > 0)
        {
            await WriteDocumentAuditAsync(
                contractId,
                null,
                normalized,
                actorUserId,
                AuditAction.Rejected,
                "Contract document metadata was rejected by No-CUI upload guardrails.",
                validationErrors,
                cancellationToken);
            throw new UploadGuardrailValidationException(validationErrors);
        }

        var document = await repository.CreateDocumentMetadataAsync(
            contractId,
            normalized,
            actorUserId,
            acknowledgement.NoticeVersion,
            cancellationToken);

        if (document is not null)
        {
            await WriteDocumentAuditAsync(
                contractId,
                document.Id,
                normalized,
                actorUserId,
                AuditAction.Uploaded,
                $"Contract document '{document.FileName}' was uploaded as metadata.",
                null,
                cancellationToken);
        }

        return document;
    }

    public async Task<bool> DeleteDocumentAsync(
        Guid contractId,
        Guid documentId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var deleted = await repository.DeleteDocumentAsync(contractId, documentId, actorUserId, cancellationToken);
        if (deleted is not null)
        {
            await WriteDocumentAuditAsync(
                contractId,
                documentId,
                new ContractDocumentUploadRequest(deleted.Type, deleted.FileName, deleted.ContentType, deleted.SizeBytes, deleted.ContainsPotentialCui),
                actorUserId,
                AuditAction.Deleted,
                $"Contract document '{deleted.FileName}' was deleted.",
                null,
                cancellationToken);
        }

        return deleted is not null;
    }

    public async Task<ExtractionJobDto?> StartExtractionJobAsync(
        Guid contractId,
        Guid documentId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var job = await repository.CreateExtractionJobAsync(contractId, documentId, actorUserId, cancellationToken);
        if (job is null)
        {
            return null;
        }

        await WriteExtractionJobAuditAsync(
            job,
            actorUserId,
            AuditAction.Created,
            "Clause extraction job was queued for contract document analysis.",
            cancellationToken);
        await extractionJobQueue.EnqueueAsync(job.Id, cancellationToken);
        return job;
    }

    public async Task<ExtractionJobDto?> MarkExtractionJobCompletedAsync(
        Guid extractionJobId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var job = await repository.MarkExtractionJobCompletedAsync(extractionJobId, cancellationToken);
        if (job is not null)
        {
            await WriteExtractionJobAuditAsync(
                job,
                actorUserId,
                AuditAction.Updated,
                "Clause extraction job completed.",
                cancellationToken);
        }

        return job;
    }

    public async Task<ExtractionJobDto?> MarkExtractionJobFailedAsync(
        Guid extractionJobId,
        Guid actorUserId,
        string failureReason,
        CancellationToken cancellationToken = default)
    {
        var job = await repository.MarkExtractionJobFailedAsync(extractionJobId, failureReason, cancellationToken);
        if (job is not null)
        {
            await WriteExtractionJobAuditAsync(
                job,
                actorUserId,
                AuditAction.Rejected,
                "Clause extraction job failed.",
                cancellationToken);
        }

        return job;
    }

    public async Task<ContractDeliverableDto?> CreateDeliverableAsync(
        Guid contractId,
        UpsertContractDeliverableRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeDeliverable(request);
        ValidateDeliverable(normalized);
        var deliverable = await repository.CreateDeliverableAsync(contractId, normalized, actorUserId, cancellationToken);

        if (deliverable is not null)
        {
            await WriteDeliverableAuditAsync(deliverable, actorUserId, AuditAction.Created, cancellationToken);
        }

        return deliverable;
    }

    public async Task<ContractDeliverableDto?> UpdateDeliverableAsync(
        Guid contractId,
        Guid deliverableId,
        UpsertContractDeliverableRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeDeliverable(request);
        ValidateDeliverable(normalized);
        var deliverable = await repository.UpdateDeliverableAsync(contractId, deliverableId, normalized, actorUserId, cancellationToken);

        if (deliverable is not null)
        {
            await WriteDeliverableAuditAsync(deliverable, actorUserId, AuditAction.Updated, cancellationToken);
        }

        return deliverable;
    }

    public async Task<ContractClauseDto?> AttachClauseAsync(
        Guid contractId,
        AttachContractClauseRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeClauseAttachment(request);
        ValidateClauseAttachment(normalized);
        var clause = await repository.AttachClauseAsync(contractId, normalized, actorUserId, cancellationToken);

        if (clause is not null)
        {
            await repository.GenerateObligationsForClauseAsync(contractId, clause.Id, actorUserId, cancellationToken);
            await WriteClauseAuditAsync(clause, actorUserId, AuditAction.Created, normalized.AttachmentReason, cancellationToken);
        }

        return clause;
    }

    public async Task<GeneratedContractObligationsDto?> GenerateObligationsForClauseAsync(
        Guid contractId,
        Guid contractClauseId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var generated = await repository.GenerateObligationsForClauseAsync(contractId, contractClauseId, actorUserId, cancellationToken);
        if (generated is not null)
        {
            var contract = await repository.FindCurrentTenantAsync(contractId, cancellationToken);
            if (contract is not null)
            {
                await auditEventWriter.WriteAsync(
                    contract.TenantId,
                    actorUserId,
                    AuditAction.Updated,
                    "ContractClause",
                    contractClauseId.ToString(),
                    $"Generated {generated.ObligationIds.Count} obligation mappings for contract clause.",
                    new Dictionary<string, string>
                    {
                        ["contractId"] = contractId.ToString(),
                        ["obligationIds"] = string.Join(", ", generated.ObligationIds),
                        ["tasksCreated"] = generated.TasksCreated.ToString()
                    },
                    cancellationToken);
            }
        }

        return generated;
    }

    public async Task<ContractClauseDto?> RemoveClauseAsync(
        Guid contractId,
        Guid contractClauseId,
        RemoveContractClauseRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeClauseRemoval(request);
        ValidateClauseRemoval(normalized);
        var clause = await repository.RemoveClauseAsync(contractId, contractClauseId, normalized, actorUserId, cancellationToken);

        if (clause is not null)
        {
            await WriteClauseAuditAsync(clause, actorUserId, AuditAction.Deleted, normalized.Reason, cancellationToken);
        }

        return clause;
    }

    private async Task WriteAuditAsync(
        ContractDto contract,
        Guid actorUserId,
        AuditAction action,
        CancellationToken cancellationToken)
    {
        await auditEventWriter.WriteAsync(
            contract.TenantId,
            actorUserId,
            action,
            "Contract",
            contract.Id.ToString(),
            action == AuditAction.Created
                ? $"Contract '{contract.ContractNumber}' was created."
                : $"Contract '{contract.ContractNumber}' was updated.",
            new Dictionary<string, string>
            {
                ["contractNumber"] = contract.ContractNumber,
                ["status"] = contract.Status.ToString(),
                ["relationship"] = contract.Relationship.ToString(),
                ["kind"] = contract.Kind.ToString(),
                ["dataHandlingPosture"] = contract.DataHandlingPosture.ToString()
            },
            cancellationToken);
    }

    private static UpsertContractRequest Normalize(UpsertContractRequest request) =>
        request with
        {
            ContractNumber = request.ContractNumber.Trim(),
            Title = request.Title.Trim(),
            AgencyOrPrimeName = request.AgencyOrPrimeName.Trim(),
            PlaceOfPerformance = request.PlaceOfPerformance.Trim(),
            Description = request.Description.Trim()
        };

    private static ContractDocumentUploadRequest NormalizeDocument(ContractDocumentUploadRequest request) =>
        request with
        {
            FileName = request.FileName.Trim(),
            ContentType = request.ContentType.Trim().ToLowerInvariant()
        };

    private static UpsertContractDeliverableRequest NormalizeDeliverable(UpsertContractDeliverableRequest request) =>
        request with
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            OwnerFunction = request.OwnerFunction.Trim()
        };

    private static AttachContractClauseRequest NormalizeClauseAttachment(AttachContractClauseRequest request) =>
        request with
        {
            ClauseLibraryId = request.ClauseLibraryId.Trim(),
            AttachmentReason = request.AttachmentReason.Trim(),
            SourceDocumentReference = string.IsNullOrWhiteSpace(request.SourceDocumentReference)
                ? null
                : request.SourceDocumentReference.Trim()
        };

    private static RemoveContractClauseRequest NormalizeClauseRemoval(RemoveContractClauseRequest request) =>
        request with
        {
            Reason = request.Reason.Trim()
        };

    private async Task WriteDocumentAuditAsync(
        Guid contractId,
        Guid? documentId,
        ContractDocumentUploadRequest request,
        Guid actorUserId,
        AuditAction action,
        string summary,
        IReadOnlyDictionary<string, string[]>? validationErrors,
        CancellationToken cancellationToken)
    {
        var metadata = new Dictionary<string, string>
        {
            ["contractId"] = contractId.ToString(),
            ["documentType"] = request.Type.ToString(),
            ["fileName"] = request.FileName,
            ["contentType"] = request.ContentType,
            ["sizeBytes"] = request.SizeBytes.ToString(),
            ["containsPotentialCui"] = request.ContainsPotentialCui.ToString()
        };

        if (validationErrors is not null)
        {
            metadata["validationErrors"] = string.Join("; ", validationErrors.SelectMany(error => error.Value));
            metadata["allowedExtensions"] = string.Join(", ", EvidenceUploadGuardrails.AllowedExtensions);
        }

        await auditEventWriter.WriteAsync(
            contractId == Guid.Empty ? Guid.Empty : (await repository.FindCurrentTenantAsync(contractId, cancellationToken))?.TenantId ?? Guid.Empty,
            actorUserId,
            action,
            "ContractDocument",
            (documentId ?? contractId).ToString(),
            summary,
            metadata,
            cancellationToken);
    }

    private static void Validate(UpsertContractRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        AddIf(errors, string.IsNullOrWhiteSpace(request.ContractNumber), "contractNumber", "Contract number is required.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.Title), "title", "Contract title is required.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.AgencyOrPrimeName), "agencyOrPrimeName", "Agency or prime is required.");
        AddIf(errors, request.Kind is ContractKind.Unknown, "kind", "Contract type is required.");
        AddIf(errors, request.Status is not (ContractStatus.Draft or ContractStatus.Active), "status", "Story 8.1 supports Draft and Active contract records.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.PlaceOfPerformance), "placeOfPerformance", "Place of performance is required.");
        AddIf(errors, request.PeriodOfPerformanceEnd < request.PeriodOfPerformanceStart, "periodOfPerformanceEnd", "Period of performance end must be on or after the start date.");
        AddIf(errors, request.DataHandlingPosture is DataHandlingPosture.Unknown, "dataHandlingPosture", "Data handling posture is required.");

        if (errors.Count > 0)
        {
            throw new ContractValidationException(errors);
        }
    }

    private static void ValidateDeliverable(UpsertContractDeliverableRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        AddIf(errors, string.IsNullOrWhiteSpace(request.Name), "name", "Deliverable name is required.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.OwnerFunction), "ownerFunction", "Deliverable owner function is required.");

        if (errors.Count > 0)
        {
            throw new ContractValidationException(errors);
        }
    }

    private static void ValidateClauseAttachment(AttachContractClauseRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        AddIf(errors, string.IsNullOrWhiteSpace(request.ClauseLibraryId), "clauseLibraryId", "Published clause id is required.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.AttachmentReason), "attachmentReason", "Attachment reason is required.");

        if (errors.Count > 0)
        {
            throw new ContractValidationException(errors);
        }
    }

    private static void ValidateClauseRemoval(RemoveContractClauseRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        AddIf(errors, string.IsNullOrWhiteSpace(request.Reason), "reason", "Removal reason is required.");

        if (errors.Count > 0)
        {
            throw new ContractValidationException(errors);
        }
    }

    private async Task WriteDeliverableAuditAsync(
        ContractDeliverableDto deliverable,
        Guid actorUserId,
        AuditAction action,
        CancellationToken cancellationToken)
    {
        var contract = await repository.FindCurrentTenantAsync(deliverable.ContractId, cancellationToken);
        if (contract is null)
        {
            return;
        }

        await auditEventWriter.WriteAsync(
            contract.TenantId,
            actorUserId,
            action,
            "ContractDeliverable",
            deliverable.Id.ToString(),
            action == AuditAction.Created
                ? $"Contract deliverable '{deliverable.Name}' was created."
                : $"Contract deliverable '{deliverable.Name}' was updated.",
            new Dictionary<string, string>
            {
                ["contractId"] = deliverable.ContractId.ToString(),
                ["name"] = deliverable.Name,
                ["status"] = deliverable.Status.ToString(),
                ["dueAt"] = deliverable.DueAt?.ToString("yyyy-MM-dd") ?? string.Empty,
                ["ownerFunction"] = deliverable.OwnerFunction,
                ["isOverdue"] = deliverable.IsOverdue.ToString()
            },
            cancellationToken);
    }

    private async Task WriteClauseAuditAsync(
        ContractClauseDto clause,
        Guid actorUserId,
        AuditAction action,
        string reason,
        CancellationToken cancellationToken)
    {
        var contract = await repository.FindCurrentTenantAsync(clause.ContractId, cancellationToken);
        if (contract is null)
        {
            return;
        }

        await auditEventWriter.WriteAsync(
            contract.TenantId,
            actorUserId,
            action,
            "ContractClause",
            clause.Id.ToString(),
            action == AuditAction.Created
                ? $"Clause '{clause.ClauseNumber}' was attached to contract '{contract.ContractNumber}'."
                : $"Clause '{clause.ClauseNumber}' was removed from contract '{contract.ContractNumber}'.",
            new Dictionary<string, string>
            {
                ["contractId"] = clause.ContractId.ToString(),
                ["contractNumber"] = contract.ContractNumber,
                ["clauseLibraryId"] = clause.ClauseLibraryId,
                ["clauseNumber"] = clause.ClauseNumber,
                ["reason"] = reason,
                ["sourceDocumentReference"] = clause.SourceDocumentReference ?? string.Empty
            },
            cancellationToken);
    }

    private async Task WriteExtractionJobAuditAsync(
        ExtractionJobDto job,
        Guid actorUserId,
        AuditAction action,
        string summary,
        CancellationToken cancellationToken)
    {
        var metadata = new Dictionary<string, string>
        {
            ["sourceDocumentId"] = job.SourceDocumentId.ToString(),
            ["status"] = job.Status.ToString()
        };

        if (!string.IsNullOrWhiteSpace(job.FailureReason))
        {
            metadata["failureReason"] = job.FailureReason;
        }

        await auditEventWriter.WriteAsync(
            job.TenantId,
            actorUserId,
            action,
            "ExtractionJob",
            job.Id.ToString(),
            summary,
            metadata,
            cancellationToken);
    }

    private static Dictionary<string, string[]> ValidateDocumentUpload(ContractDocumentUploadRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (request.ContainsPotentialCui)
        {
            errors["containsPotentialCui"] = ["Contract document metadata cannot be accepted when the file is marked as potential CUI in the No-CUI MVP."];
        }

        if (string.IsNullOrWhiteSpace(request.FileName) || request.FileName.Length > 300)
        {
            errors["fileName"] = ["A file name is required and must be 300 characters or fewer."];
        }

        var extension = Path.GetExtension(request.FileName);
        if (string.IsNullOrWhiteSpace(extension) ||
            !EvidenceUploadGuardrails.AllowedContentTypesByExtension.TryGetValue(extension, out var allowedContentTypes))
        {
            errors["fileType"] =
            [
                $"File type '{extension}' is not allowed. Allowed extensions: {string.Join(", ", EvidenceUploadGuardrails.AllowedExtensions)}."
            ];
        }
        else if (string.IsNullOrWhiteSpace(request.ContentType) ||
                 !allowedContentTypes.Contains(request.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            errors["contentType"] =
            [
                $"Content type '{request.ContentType}' is not allowed for {extension} contract document uploads."
            ];
        }

        if (request.SizeBytes <= 0)
        {
            errors["sizeBytes"] = ["File size must be greater than zero bytes."];
        }
        else if (request.SizeBytes > EvidenceUploadGuardrails.MaxSizeBytes)
        {
            errors["sizeBytes"] =
            [
                $"File size exceeds the {EvidenceUploadGuardrails.MaxSizeBytes} byte No-CUI MVP upload limit."
            ];
        }

        return errors;
    }

    private static void AddIf(IDictionary<string, string[]> errors, bool condition, string field, string message)
    {
        if (condition)
        {
            errors[field] = [message];
        }
    }
}

public interface IContractRepository
{
    Task<IReadOnlyList<ContractDto>> ListCurrentTenantAsync(CancellationToken cancellationToken = default);

    Task<ContractDto?> FindCurrentTenantAsync(Guid contractId, CancellationToken cancellationToken = default);

    Task<ContractDto> CreateCurrentTenantAsync(
        UpsertContractRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<ContractDto?> UpdateCurrentTenantAsync(
        Guid contractId,
        UpsertContractRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ContractDocumentDto>?> ListDocumentsAsync(
        Guid contractId,
        CancellationToken cancellationToken = default);

    Task<ContractDocumentDto?> CreateDocumentMetadataAsync(
        Guid contractId,
        ContractDocumentUploadRequest request,
        Guid actorUserId,
        string noticeVersion,
        CancellationToken cancellationToken = default);

    Task<ContractDocumentDto?> DeleteDocumentAsync(
        Guid contractId,
        Guid documentId,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<ExtractionJobDto?> CreateExtractionJobAsync(
        Guid contractId,
        Guid documentId,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<ExtractionJobDto?> MarkExtractionJobCompletedAsync(
        Guid extractionJobId,
        CancellationToken cancellationToken = default);

    Task<ExtractionJobDto?> MarkExtractionJobFailedAsync(
        Guid extractionJobId,
        string failureReason,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ContractDeliverableDto>?> ListDeliverablesAsync(
        Guid contractId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ContractClauseDto>?> ListClausesAsync(
        Guid contractId,
        CancellationToken cancellationToken = default);

    Task<ContractDeliverableDto?> CreateDeliverableAsync(
        Guid contractId,
        UpsertContractDeliverableRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<ContractDeliverableDto?> UpdateDeliverableAsync(
        Guid contractId,
        Guid deliverableId,
        UpsertContractDeliverableRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<ContractClauseDto?> AttachClauseAsync(
        Guid contractId,
        AttachContractClauseRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<ContractClauseDto?> RemoveClauseAsync(
        Guid contractId,
        Guid contractClauseId,
        RemoveContractClauseRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<GeneratedContractObligationsDto?> GenerateObligationsForClauseAsync(
        Guid contractId,
        Guid contractClauseId,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed class ContractValidationException(IReadOnlyDictionary<string, string[]> errors)
    : InvalidOperationException("Contract record is missing required fields.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}

public interface IExtractionJobQueue
{
    Task EnqueueAsync(Guid extractionJobId, CancellationToken cancellationToken = default);
}
