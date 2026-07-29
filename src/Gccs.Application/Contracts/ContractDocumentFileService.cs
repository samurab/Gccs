using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.NoCui;
using Gccs.Application.Security;
using Gccs.Application.Storage;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;
using Gccs.Domain.Contracts;

namespace Gccs.Application.Contracts;

public sealed class ContractDocumentFileService(
    IContractRepository repository,
    INoCuiAcknowledgementRepository noCuiAcknowledgementRepository,
    ICurrentTenantContext tenantContext,
    IAuditEventWriter auditEventWriter,
    ContentClassificationPolicy classificationPolicy,
    IObjectStorageService objectStorageService,
    IMalwareScanner malwareScanner,
    IApplicationTransaction transaction)
{
    public async Task<bool> DeleteAsync(
        Guid contractId,
        Guid documentId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = await repository.FindDocumentInCurrentTenantAsync(contractId, documentId, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        var deleted = await transaction.ExecuteAsync(async transactionToken =>
        {
            var removed = await repository.DeleteDocumentAsync(contractId, documentId, actorUserId, transactionToken);
            if (removed is null)
            {
                return false;
            }

            await auditEventWriter.WriteAsync(
                tenantContext.TenantId,
                actorUserId,
                AuditAction.Deleted,
                "ContractDocument",
                documentId.ToString(),
                $"Contract document '{removed.FileName}' was deleted.",
                new Dictionary<string, string>
                {
                    ["contractId"] = contractId.ToString(),
                    ["fileName"] = removed.FileName,
                    ["classification"] = removed.Classification.Classification.ToString(),
                    ["noFileContentLogged"] = bool.TrueString
                },
                transactionToken);
            return true;
        }, cancellationToken);

        if (deleted &&
            !string.IsNullOrWhiteSpace(existing.StorageUri) &&
            existing.StorageUri.StartsWith("contracts/", StringComparison.Ordinal))
        {
            await DeleteStoredObjectAsync(existing.StorageUri, cancellationToken);
        }

        return deleted;
    }

    public async Task<ContractDocumentDto?> UploadAsync(
        Guid contractId,
        ContractDocumentFileUploadRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request.Content);

        if (await repository.FindCurrentTenantAsync(contractId, cancellationToken) is null)
        {
            return null;
        }

        var acknowledgement = await noCuiAcknowledgementRepository.FindCurrentUserAcknowledgementAsync(
            NoCuiNotice.CurrentVersion,
            cancellationToken);
        if (acknowledgement is null)
        {
            throw new NoCuiAcknowledgementRequiredException(
                "No-CUI acknowledgement is required before contract document upload is enabled.");
        }

        var metadata = new ContractDocumentUploadRequest(
            request.Type,
            request.FileName.Trim(),
            request.ContentType.Trim().ToLowerInvariant(),
            request.SizeBytes,
            request.ContainsPotentialCui,
            request.Classification);
        var validationErrors = ContractDocumentUploadValidation.Validate(metadata, request.NoCuiAttestation);
        if (validationErrors.Count > 0)
        {
            await WriteRejectedAuditAsync(contractId, metadata, actorUserId, validationErrors, "upload-validation", cancellationToken);
            throw new UploadGuardrailValidationException(validationErrors);
        }

        var classification = metadata.Classification ??
            throw new ContentClassificationValidationException(
                "Classification metadata is required before a contract document can be uploaded.");
        await classificationPolicy.EnsureAllowedAsync(
            classification,
            TenantDataHandlingWorkflow.ContractDocumentUpload,
            actorUserId,
            "ContractDocument",
            contractId.ToString(),
            cancellationToken);

        await using var content = await BufferAndValidateSizeAsync(request.Content, request.SizeBytes, cancellationToken);
        var scanResult = await malwareScanner.ScanAsync(
            new MalwareScanRequest(content, metadata.FileName, metadata.ContentType, metadata.SizeBytes),
            cancellationToken);
        content.Position = 0;

        if (scanResult.Verdict is not MalwareScanVerdict.Clean)
        {
            var failureCode = scanResult.Verdict is MalwareScanVerdict.Malicious
                ? "malware-detected"
                : "scan-unavailable";
            await WriteRejectedAuditAsync(
                contractId,
                metadata,
                actorUserId,
                new Dictionary<string, string[]>
                {
                    ["malwareScan"] =
                    [
                        scanResult.Verdict is MalwareScanVerdict.Malicious
                            ? "Contract document upload was rejected because malware was detected."
                            : "Contract document upload was rejected because malware scanning was unavailable."
                    ]
                },
                failureCode,
                cancellationToken);

            if (scanResult.Verdict is MalwareScanVerdict.Malicious)
            {
                throw new MalwareScanRejectedException(
                    "Contract document upload was rejected because malware scanning detected unsafe content.");
            }

            throw new MalwareScanUnavailableException(
                "Contract document upload is unavailable because malware scanning did not produce a clean verdict.");
        }

        var uploadId = Guid.NewGuid();
        var objectName = BuildObjectName(contractId, uploadId, metadata.FileName);
        await objectStorageService.UploadAsync(
            new ObjectStorageWriteRequest(
                tenantContext.TenantId,
                ObjectStorageContainer.ContractDocuments,
                objectName,
                content,
                metadata.ContentType,
                new Dictionary<string, string>
                {
                    ["contractId"] = contractId.ToString("D"),
                    ["uploadedByUserId"] = actorUserId.ToString("D"),
                    ["classification"] = classification.Classification.ToString(),
                    ["malwareScanStatus"] = EvidenceUploadGuardrails.CleanMalwareScanStatus,
                    ["malwareScanner"] = scanResult.ScannerName
                }),
            cancellationToken);

        try
        {
            var document = await transaction.ExecuteAsync(async transactionToken =>
            {
                var created = await repository.CreateDocumentMetadataAsync(
                    contractId,
                    metadata,
                    actorUserId,
                    acknowledgement.NoticeVersion,
                    transactionToken,
                    storageObjectName: objectName,
                    malwareScanStatus: EvidenceUploadGuardrails.CleanMalwareScanStatus);
                if (created is null)
                {
                    return null;
                }

                await auditEventWriter.WriteAsync(
                    tenantContext.TenantId,
                    actorUserId,
                    AuditAction.Uploaded,
                    "ContractDocument",
                    created.Id.ToString(),
                    "Contract document bytes passed No-CUI validation and malware scanning and were stored.",
                    new Dictionary<string, string>
                    {
                        ["contractId"] = contractId.ToString(),
                        ["documentType"] = created.Type.ToString(),
                        ["fileName"] = created.FileName,
                        ["contentType"] = created.ContentType,
                        ["sizeBytes"] = created.SizeBytes.ToString(),
                        ["classification"] = created.Classification.Classification.ToString(),
                        ["validationStatus"] = created.ValidationStatus,
                        ["malwareScanStatus"] = created.MalwareScanStatus,
                        ["noFileContentLogged"] = bool.TrueString
                    },
                    transactionToken);
                return created;
            }, cancellationToken);

            if (document is null)
            {
                await DeleteStoredObjectAsync(objectName, cancellationToken);
            }

            return document;
        }
        catch
        {
            await DeleteStoredObjectAsync(objectName, CancellationToken.None);
            throw;
        }
    }

    private async Task WriteRejectedAuditAsync(
        Guid contractId,
        ContractDocumentUploadRequest request,
        Guid actorUserId,
        IReadOnlyDictionary<string, string[]> errors,
        string failureCode,
        CancellationToken cancellationToken)
    {
        await auditEventWriter.WriteAsync(
            tenantContext.TenantId,
            actorUserId,
            AuditAction.Rejected,
            "ContractDocument",
            contractId.ToString(),
            "Contract document upload was rejected before storage persistence.",
            new Dictionary<string, string>
            {
                ["contractId"] = contractId.ToString(),
                ["fileName"] = request.FileName,
                ["contentType"] = request.ContentType,
                ["sizeBytes"] = request.SizeBytes.ToString(),
                ["failureCode"] = failureCode,
                ["validationErrors"] = string.Join("; ", errors.SelectMany(error => error.Value)),
                ["noFileContentLogged"] = bool.TrueString
            },
            cancellationToken);
    }

    private async Task DeleteStoredObjectAsync(string objectName, CancellationToken cancellationToken)
    {
        try
        {
            await objectStorageService.DeleteAsync(
                new ObjectStorageReadRequest(
                    tenantContext.TenantId,
                    ObjectStorageContainer.ContractDocuments,
                    objectName),
                cancellationToken);
        }
        catch
        {
            // Preserve the original persistence/audit failure. Orphan detection can remove this tenant-scoped object.
        }
    }

    private static async Task<MemoryStream> BufferAndValidateSizeAsync(
        Stream source,
        long declaredSize,
        CancellationToken cancellationToken)
    {
        var buffer = new MemoryStream();
        var chunk = new byte[81920];
        long total = 0;
        while (true)
        {
            var read = await source.ReadAsync(chunk, cancellationToken);
            if (read == 0)
            {
                break;
            }

            total += read;
            if (total > EvidenceUploadGuardrails.MaxSizeBytes)
            {
                await buffer.DisposeAsync();
                throw new UploadGuardrailValidationException(new Dictionary<string, string[]>
                {
                    ["sizeBytes"] =
                    [
                        $"File size exceeds the {EvidenceUploadGuardrails.MaxSizeBytes} byte No-CUI MVP upload limit."
                    ]
                });
            }

            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }

        if (total != declaredSize)
        {
            await buffer.DisposeAsync();
            throw new UploadGuardrailValidationException(new Dictionary<string, string[]>
            {
                ["sizeBytes"] = ["The uploaded byte count does not match the declared file size."]
            });
        }

        buffer.Position = 0;
        return buffer;
    }

    private static string BuildObjectName(Guid contractId, Guid uploadId, string fileName)
    {
        var safeFileName = Path.GetFileName(fileName.Trim());
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            safeFileName = "contract-document";
        }

        return $"contracts/{contractId:D}/{uploadId:D}/{safeFileName}";
    }
}

public sealed record ContractDocumentFileUploadRequest(
    ContractDocumentType Type,
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content,
    bool NoCuiAttestation,
    bool ContainsPotentialCui,
    ContentClassificationRequest? Classification);

public static class ContractDocumentUploadValidation
{
    public static Dictionary<string, string[]> Validate(
        ContractDocumentUploadRequest request,
        bool? noCuiAttestation = null)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(request.FileName) || request.FileName.Length > 300)
        {
            errors["fileName"] = ["A file name is required and must be 300 characters or fewer."];
        }

        if (noCuiAttestation is false)
        {
            errors["noCuiAttestation"] = [NoCuiNotice.RequiredUploadAttestationText];
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
}
