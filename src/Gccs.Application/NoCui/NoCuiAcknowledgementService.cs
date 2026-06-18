using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Security;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;

namespace Gccs.Application.NoCui;

public sealed class NoCuiAcknowledgementService(
    INoCuiAcknowledgementRepository repository,
    ICurrentTenantContext tenantContext,
    IAuditEventWriter auditEventWriter,
    ContentClassificationPolicy classificationPolicy)
{
    public async Task<NoCuiAcknowledgementStatusDto> GetCurrentStatusAsync(
        CancellationToken cancellationToken = default)
    {
        var acknowledgement = await repository.FindCurrentUserAcknowledgementAsync(
            NoCuiNotice.CurrentVersion,
            cancellationToken);

        return acknowledgement ?? new NoCuiAcknowledgementStatusDto(
            false,
            NoCuiNotice.CurrentVersion,
            NoCuiNotice.Copy,
            tenantContext.TenantId,
            null,
            null);
    }

    public async Task<NoCuiAcknowledgementStatusDto> AcknowledgeAsync(
        AcknowledgeNoCuiRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateAcknowledgement(request);

        var existing = await repository.FindCurrentUserAcknowledgementAsync(
            request.NoticeVersion,
            cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var acknowledgedAt = DateTimeOffset.UtcNow;
        var acknowledgement = await repository.AddCurrentUserAcknowledgementAsync(
            request.NoticeVersion,
            NoCuiNotice.Copy,
            actorUserId,
            acknowledgedAt,
            cancellationToken);

        await auditEventWriter.WriteAsync(
            tenantContext.TenantId,
            actorUserId,
            AuditAction.Created,
            "NoCuiAcknowledgement",
            $"{tenantContext.TenantId}:{actorUserId}:{request.NoticeVersion}",
            "No-CUI notice was acknowledged before upload access was enabled.",
            new Dictionary<string, string>
            {
                ["noticeVersion"] = acknowledgement.NoticeVersion,
                ["acknowledgedAt"] = acknowledgement.AcknowledgedAt?.ToString("O") ?? string.Empty,
                ["noticeCopy"] = acknowledgement.NoticeCopy
            },
            cancellationToken);

        return acknowledgement;
    }

    public async Task<EvidenceUploadIntentDto> CreateEvidenceUploadIntentAsync(
        Guid evidenceItemId,
        EvidenceUploadIntentRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
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
        await classificationPolicy.EnsureAllowedAsync(
            classification,
            TenantDataHandlingWorkflow.EvidenceUpload,
            actorUserId,
            "EvidenceItem",
            evidenceItemId.ToString(),
            cancellationToken);

        var uploadIntent = new EvidenceUploadIntentDto(
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
            "Upload metadata passed No-CUI guardrails. The file is not usable until future storage and malware scanning workflows complete.",
            acknowledgement.NoticeVersion,
            DateTimeOffset.UtcNow.AddMinutes(15),
            ToClassificationDto(classification));

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
                ["isUsable"] = version.IsUsable.ToString()
            },
            cancellationToken);

        return uploadIntent;
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

    public async Task<EvidenceFileAccessDto?> DeleteLatestFileAsync(
        Guid evidenceItemId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
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

    private static void ValidateAcknowledgement(AcknowledgeNoCuiRequest request)
    {
        if (!request.Acknowledged)
        {
            throw new ArgumentException("The No-CUI notice must be acknowledged before upload is enabled.", nameof(request));
        }

        if (!string.Equals(request.NoticeVersion, NoCuiNotice.CurrentVersion, StringComparison.Ordinal))
        {
            throw new ArgumentException("The No-CUI notice version is not current.", nameof(request));
        }
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
                ["validationErrors"] = string.Join("; ", validationErrors.SelectMany(error => error.Value))
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
