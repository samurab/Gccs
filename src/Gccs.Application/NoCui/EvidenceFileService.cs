using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Security;
using Gccs.Application.Storage;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;

namespace Gccs.Application.NoCui;

public sealed class EvidenceFileService(
    INoCuiAcknowledgementRepository repository,
    ICurrentTenantContext tenantContext,
    IAuditEventWriter auditEventWriter,
    ContentClassificationPolicy classificationPolicy,
    IObjectStorageService objectStorageService,
    IMalwareScanner malwareScanner)
{
    public async Task<EvidenceUploadIntentDto> CreateEvidenceUploadIntentAsync(
        Guid evidenceItemId,
        EvidenceUploadIntentRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var uploadIntent = await ValidateAndBuildUploadIntentAsync(evidenceItemId, request, actorUserId, cancellationToken);
        var version = await repository.RecordAcceptedEvidenceUploadIntentAsync(uploadIntent, cancellationToken);
        await auditEventWriter.WriteAsync(
            tenantContext.TenantId,
            actorUserId,
            AuditAction.Uploaded,
            "EvidenceFileVersion",
            version.Id.ToString(),
            "Evidence file upload metadata was accepted and versioned.",
            new Dictionary<string, string>
            {
                ["evidenceItemId"] = evidenceItemId.ToString(),
                ["versionNumber"] = version.VersionNumber.ToString(),
                ["fileName"] = version.FileName,
                ["validationStatus"] = version.ValidationStatus,
                ["malwareScanStatus"] = version.MalwareScanStatus,
                ["isUsable"] = version.IsUsable.ToString(),
                ["noCuiAttestation"] = request.NoCuiAttestation.ToString()
            },
            cancellationToken);

        return uploadIntent;
    }

    public async Task<EvidenceFileAccessDto> UploadEvidenceFileAsync(
        Guid evidenceItemId,
        EvidenceUploadFileRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request.Content);

        var uploadIntent = await ValidateAndBuildUploadIntentAsync(
            evidenceItemId,
            new EvidenceUploadIntentRequest(
                request.FileName,
                request.ContentType,
                request.SizeBytes,
                request.NoCuiAttestation,
                request.ContainsPotentialCui,
                request.Classification),
            actorUserId,
            cancellationToken);

        await using var content = await BufferUploadContentAsync(request.Content, cancellationToken);
        var scanResult = await ScanUploadAsync(evidenceItemId, uploadIntent, content, actorUserId, cancellationToken);
        var scannedIntent = uploadIntent with
        {
            MalwareScanStatus = EvidenceUploadGuardrails.CleanMalwareScanStatus,
            Message = $"Upload passed configured malware scanner '{scanResult.ScannerName}' and is usable after storage persistence."
        };

        var objectName = BuildEvidenceObjectName(evidenceItemId, scannedIntent.Id, scannedIntent.FileName);
        content.Position = 0;
        await objectStorageService.UploadAsync(
            new ObjectStorageWriteRequest(
                tenantContext.TenantId,
                ObjectStorageContainer.Evidence,
                objectName,
                content,
                scannedIntent.ContentType,
                new Dictionary<string, string>
                {
                    ["evidenceItemId"] = evidenceItemId.ToString("D"),
                    ["evidenceFileVersionId"] = scannedIntent.Id.ToString("D"),
                    ["uploadedByUserId"] = actorUserId.ToString("D"),
                    ["classification"] = scannedIntent.Classification.Classification.ToString(),
                    ["malwareScanStatus"] = scannedIntent.MalwareScanStatus,
                    ["malwareScanner"] = scanResult.ScannerName
                }),
            cancellationToken);

        EvidenceFileVersionDto version;
        var storedIntent = scannedIntent with { StorageObjectName = objectName };
        try
        {
            version = await repository.RecordAcceptedEvidenceUploadIntentAsync(storedIntent, cancellationToken);
        }
        catch
        {
            await objectStorageService.DeleteAsync(
                new ObjectStorageReadRequest(tenantContext.TenantId, ObjectStorageContainer.Evidence, objectName),
                cancellationToken);
            throw;
        }
        await auditEventWriter.WriteAsync(
            tenantContext.TenantId,
            actorUserId,
            AuditAction.Uploaded,
            "EvidenceFileVersion",
            version.Id.ToString(),
            "Evidence file bytes were uploaded to object storage and versioned.",
            ToAuditMetadata(version),
            cancellationToken);

        return ToAccessDto(version, "Evidence file was uploaded to private object storage.");
    }

    private async Task<MalwareScanResult> ScanUploadAsync(
        Guid evidenceItemId,
        EvidenceUploadIntentDto uploadIntent,
        MemoryStream content,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        content.Position = 0;
        var scanResult = await malwareScanner.ScanAsync(
            new MalwareScanRequest(
                content,
                uploadIntent.FileName,
                uploadIntent.ContentType,
                uploadIntent.SizeBytes),
            cancellationToken);
        content.Position = 0;

        if (scanResult.Verdict == MalwareScanVerdict.Clean)
        {
            return scanResult;
        }

        await AuditRejectedMalwareScanAsync(evidenceItemId, uploadIntent, actorUserId, scanResult, cancellationToken);
        if (scanResult.Verdict == MalwareScanVerdict.Malicious)
        {
            throw new MalwareScanRejectedException("Evidence upload was rejected because malware scanning detected unsafe content.");
        }

        throw new MalwareScanUnavailableException("Evidence upload is unavailable because malware scanning did not produce a clean verdict.");
    }

    private async Task<EvidenceUploadIntentDto> ValidateAndBuildUploadIntentAsync(
        Guid evidenceItemId,
        EvidenceUploadIntentRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var acknowledgement = await repository.FindCurrentUserAcknowledgementAsync(
            NoCuiNotice.CurrentVersion,
            cancellationToken);

        if (acknowledgement is null)
        {
            throw new NoCuiAcknowledgementRequiredException(
                "No-CUI acknowledgement is required before evidence upload is enabled.");
        }

        var validationErrors = ValidateUploadRequest(request);
        if (validationErrors.Count > 0)
        {
            await AuditRejectedUploadIntentAsync(evidenceItemId, request, actorUserId, validationErrors, cancellationToken);
            throw new UploadGuardrailValidationException(validationErrors);
        }

        var classification = request.Classification ??
            ContentClassificationPolicy.FromLegacyCuiFlag(request.ContainsPotentialCui);
        try
        {
            await classificationPolicy.EnsureAllowedAsync(
                classification,
                TenantDataHandlingWorkflow.EvidenceUpload,
                actorUserId,
                "EvidenceItem",
                evidenceItemId.ToString(),
                cancellationToken);
        }
        catch (Exception exception) when (exception is TenantDataHandlingModeRestrictedException or ContentClassificationValidationException)
        {
            await AuditRejectedUploadIntentAsync(
                evidenceItemId,
                request,
                actorUserId,
                new Dictionary<string, string[]>
                {
                    ["classification"] = [exception.Message]
                },
                cancellationToken);
            throw;
        }

        return new EvidenceUploadIntentDto(
            Guid.NewGuid(),
            evidenceItemId,
            tenantContext.TenantId,
            actorUserId,
            request.FileName.Trim(),
            request.ContentType.Trim().ToLowerInvariant(),
            request.SizeBytes,
            "upload-pending",
            EvidenceUploadGuardrails.AcceptedValidationStatus,
            EvidenceUploadGuardrails.PendingMalwareScanStatus,
            "Upload metadata passed No-CUI guardrails. The file is not usable until future malware scanning workflows complete.",
            acknowledgement.NoticeVersion,
            NoCuiNotice.RequiredUploadAttestationText,
            DateTimeOffset.UtcNow.AddMinutes(15),
            ToClassificationDto(classification));
    }

    public async Task<EvidenceFileAccessDto?> GetLatestFileForDownloadAsync(
        Guid evidenceItemId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var version = await repository.FindLatestCurrentTenantFileVersionAsync(evidenceItemId, cancellationToken);
        if (version is null)
        {
            return null;
        }

        await auditEventWriter.WriteAsync(
            tenantContext.TenantId,
            actorUserId,
            AuditAction.Downloaded,
            "EvidenceFileVersion",
            version.Id.ToString(),
            "Evidence file download metadata was requested.",
            ToAuditMetadata(version),
            cancellationToken);

        return ToAccessDto(version, "File storage is represented as metadata in the No-CUI MVP.");
    }

    public async Task<EvidenceFileDownloadDto?> OpenLatestFileForDownloadAsync(
        Guid evidenceItemId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var version = await repository.FindLatestCurrentTenantFileVersionAsync(evidenceItemId, cancellationToken);
        if (version is null || string.IsNullOrWhiteSpace(version.StorageObjectName))
        {
            return null;
        }

        if (!version.IsUsable)
        {
            throw new EvidenceFileDownloadUnavailableException(
                "Evidence file content is not available until validation and malware scanning allow it.");
        }

        var storedFile = await objectStorageService.OpenReadAsync(
            new ObjectStorageReadRequest(
                tenantContext.TenantId,
                ObjectStorageContainer.Evidence,
                version.StorageObjectName),
            cancellationToken);

        if (storedFile is null)
        {
            return null;
        }

        await auditEventWriter.WriteAsync(
            tenantContext.TenantId,
            actorUserId,
            AuditAction.Downloaded,
            "EvidenceFileVersion",
            version.Id.ToString(),
            "Evidence file bytes were streamed from object storage.",
            ToAuditMetadata(version),
            cancellationToken);

        return new EvidenceFileDownloadDto(version, storedFile);
    }

    public async Task<EvidenceFileAccessDto?> DeleteLatestFileAsync(
        Guid evidenceItemId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = await repository.FindLatestCurrentTenantFileVersionAsync(evidenceItemId, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(existing.StorageObjectName))
        {
            await objectStorageService.DeleteAsync(
                new ObjectStorageReadRequest(
                    tenantContext.TenantId,
                    ObjectStorageContainer.Evidence,
                    existing.StorageObjectName),
                cancellationToken);
        }

        var version = await repository.MarkLatestCurrentTenantFileVersionDeletedAsync(evidenceItemId, actorUserId, cancellationToken);
        if (version is null)
        {
            return null;
        }

        await auditEventWriter.WriteAsync(
            tenantContext.TenantId,
            actorUserId,
            AuditAction.Deleted,
            "EvidenceFileVersion",
            version.Id.ToString(),
            "Evidence file version was deleted.",
            ToAuditMetadata(version),
            cancellationToken);

        return ToAccessDto(version, "Evidence file version was deleted.");
    }

    private static string BuildEvidenceObjectName(Guid evidenceItemId, Guid versionId, string fileName)
    {
        var safeFileName = Path.GetFileName(fileName.Trim());
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            safeFileName = "evidence-file";
        }

        return $"evidence/{evidenceItemId:D}/{versionId:D}/{safeFileName}";
    }

    private static async Task<MemoryStream> BufferUploadContentAsync(
        Stream content,
        CancellationToken cancellationToken)
    {
        var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        return buffer;
    }

    private static Dictionary<string, string[]> ValidateUploadRequest(EvidenceUploadIntentRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var fileName = request.FileName?.Trim() ?? string.Empty;
        var contentType = request.ContentType?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > 240)
        {
            errors["fileName"] = ["A file name is required and must be 240 characters or fewer."];
        }

        if (!request.NoCuiAttestation)
        {
            errors["noCuiAttestation"] = [NoCuiNotice.RequiredUploadAttestationText];
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension) ||
            !EvidenceUploadGuardrails.AllowedContentTypesByExtension.TryGetValue(extension, out var allowedContentTypes))
        {
            errors["fileType"] =
            [
                $"File type '{extension}' is not allowed. Allowed extensions: {string.Join(", ", EvidenceUploadGuardrails.AllowedExtensions)}."
            ];
        }
        else if (string.IsNullOrWhiteSpace(contentType) ||
                 !allowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            errors["contentType"] =
            [
                $"Content type '{contentType}' is not allowed for {extension} evidence uploads."
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

    private async Task AuditRejectedUploadIntentAsync(
        Guid evidenceItemId,
        EvidenceUploadIntentRequest request,
        Guid actorUserId,
        IReadOnlyDictionary<string, string[]> validationErrors,
        CancellationToken cancellationToken)
    {
        await auditEventWriter.WriteAsync(
            tenantContext.TenantId,
            actorUserId,
            AuditAction.Rejected,
            "EvidenceUploadIntent",
            evidenceItemId.ToString(),
            "Evidence upload metadata was rejected by No-CUI upload guardrails.",
            new Dictionary<string, string>
            {
                ["fileName"] = request.FileName ?? string.Empty,
                ["contentType"] = request.ContentType ?? string.Empty,
                ["sizeBytes"] = request.SizeBytes.ToString(),
                ["maxSizeBytes"] = EvidenceUploadGuardrails.MaxSizeBytes.ToString(),
                ["allowedExtensions"] = string.Join(", ", EvidenceUploadGuardrails.AllowedExtensions),
                ["noCuiAttestation"] = request.NoCuiAttestation.ToString(),
                ["validationErrors"] = string.Join("; ", validationErrors.SelectMany(error => error.Value))
            },
            cancellationToken);
    }

    private async Task AuditRejectedMalwareScanAsync(
        Guid evidenceItemId,
        EvidenceUploadIntentDto uploadIntent,
        Guid actorUserId,
        MalwareScanResult scanResult,
        CancellationToken cancellationToken)
    {
        var scanStatus = scanResult.Verdict == MalwareScanVerdict.Malicious
            ? EvidenceUploadGuardrails.MalwareDetectedScanStatus
            : EvidenceUploadGuardrails.ScannerUnavailableScanStatus;

        await auditEventWriter.WriteAsync(
            tenantContext.TenantId,
            actorUserId,
            AuditAction.Rejected,
            "EvidenceUploadIntent",
            evidenceItemId.ToString(),
            "Evidence upload was rejected by malware scanning.",
            new Dictionary<string, string>
            {
                ["fileName"] = uploadIntent.FileName,
                ["contentType"] = uploadIntent.ContentType,
                ["sizeBytes"] = uploadIntent.SizeBytes.ToString(),
                ["validationStatus"] = uploadIntent.ValidationStatus,
                ["malwareScanStatus"] = scanStatus,
                ["malwareScanVerdict"] = scanResult.Verdict.ToString(),
                ["malwareScanner"] = scanResult.ScannerName,
                ["scannerDetail"] = scanResult.Detail,
                ["noFileContentLogged"] = bool.TrueString
            },
            cancellationToken);
    }

    private static EvidenceFileAccessDto ToAccessDto(EvidenceFileVersionDto version, string message) =>
        new(
            version.EvidenceItemId,
            version.Id,
            version.VersionNumber,
            version.FileName,
            version.ContentType,
            version.SizeBytes,
            version.ValidationStatus,
            version.MalwareScanStatus,
            version.IsUsable,
            version.Classification,
            message);

    private static Dictionary<string, string> ToAuditMetadata(EvidenceFileVersionDto version) =>
        new()
        {
            ["evidenceItemId"] = version.EvidenceItemId.ToString(),
            ["versionNumber"] = version.VersionNumber.ToString(),
            ["fileName"] = version.FileName,
            ["validationStatus"] = version.ValidationStatus,
            ["malwareScanStatus"] = version.MalwareScanStatus,
            ["isUsable"] = version.IsUsable.ToString()
        };

    private static ContentClassificationDto ToClassificationDto(ContentClassificationRequest classification) =>
        new(
            classification.Classification,
            classification.Source,
            classification.Confidence,
            classification.ReviewedByUserId,
            classification.ReviewedAt,
            classification.Reason,
            classification.IsApprovedDemoContent);
}

public sealed class NoCuiAcknowledgementRequiredException(string message) : InvalidOperationException(message);

public sealed class UploadGuardrailValidationException(IReadOnlyDictionary<string, string[]> errors)
    : InvalidOperationException("Evidence upload metadata did not pass No-CUI upload guardrails.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}

public sealed class EvidenceFileDownloadUnavailableException(string message) : InvalidOperationException(message);

public sealed class MalwareScanRejectedException(string message) : InvalidOperationException(message);

public sealed class MalwareScanUnavailableException(string message) : InvalidOperationException(message);
