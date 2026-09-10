using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Security;
using Gccs.Domain.Audit;
using Gccs.Domain.Compliance;
using Gccs.Domain.Common;

namespace Gccs.Application.Compliance;

public sealed class SspSectionService(
    ISspSectionRepository repository,
    ISspNarrativeRepository narrativeRepository,
    ISspExportPackageRepository exportPackageRepository,
    ISspSectionLinkValidator linkValidator,
    ICurrentTenantContext tenantContext,
    IAuditEventWriter auditEventWriter,
    IApplicationTransaction transaction)
{
    public async Task<IReadOnlyList<SspSectionDto>> ListAsync(CancellationToken cancellationToken = default) =>
        await repository.ListAsync(tenantContext.TenantId, cancellationToken);

    public async Task<SspSectionDto?> GetAsync(Guid sectionId, CancellationToken cancellationToken = default) =>
        await repository.GetAsync(tenantContext.TenantId, sectionId, cancellationToken);

    public async Task<SspSectionDto> CreateAsync(CreateSspSectionRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        ValidateSection(request.SectionType, request.Title, request.Owner, request.SourceReferences, request.LinkedRecords);
        ValidateText(tenantContext.UserEmail, "Authenticated actor", 320);
        return await transaction.ExecuteAsync(async token =>
        {
            await linkValidator.ValidateAsync(tenantContext.TenantId, request.LinkedRecords, token);
            var section = await repository.CreateAsync(tenantContext.TenantId, request, actorUserId, tenantContext.UserEmail.Trim(), token);
            await WriteAuditAsync(section, actorUserId, AuditAction.Created, "SSP section was created.", token);
            return section;
        }, cancellationToken);
    }

    public async Task<SspSectionDto?> UpdateAsync(Guid sectionId, UpdateSspSectionRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        ValidateSection(request.SectionType, request.Title, request.Owner, request.SourceReferences, request.LinkedRecords);
        return await transaction.ExecuteAsync(async token =>
        {
            var current = await repository.GetAsync(tenantContext.TenantId, sectionId, token);
            if (current is null) return null;
            if (!SspSectionLifecycle.CanEdit(current.Status))
                throw new SspSectionValidationException("Only draft or in-review SSP sections can be edited.");
            await linkValidator.ValidateAsync(tenantContext.TenantId, request.LinkedRecords, token);
            var section = await repository.UpdateAsync(tenantContext.TenantId, sectionId, request, actorUserId, token);
            if (section is not null)
                await WriteAuditAsync(section, actorUserId, AuditAction.Updated, "SSP section was updated.", token);
            return section;
        }, cancellationToken);
    }

    public async Task<SspSectionDto?> ChangeStatusAsync(Guid sectionId, SspSectionStatusRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        ValidateText(tenantContext.UserEmail, "Authenticated actor", 320);

        return await transaction.ExecuteAsync(async token =>
        {
            var current = await repository.GetAsync(tenantContext.TenantId, sectionId, token);
            if (current is null) return null;
            ValidateStatusChange(current, request);
            var authenticatedRequest = request with { ActorName = tenantContext.UserEmail.Trim() };
            var section = await repository.ChangeStatusAsync(tenantContext.TenantId, sectionId, authenticatedRequest, actorUserId, token);
            if (section is null) return null;
            var action = request.Status switch
            {
                SspSectionStatus.Approved => AuditAction.Approved,
                SspSectionStatus.Archived or SspSectionStatus.Superseded => AuditAction.Archived,
                _ => AuditAction.Updated
            };
            await WriteAuditAsync(section, actorUserId, action, $"SSP section moved to {section.Status}.", token);
            return section;
        }, cancellationToken);
    }

    private static void ValidateSection(SspSectionType sectionType, string title, string owner, SspSourceReferenceDto[] sourceReferences, SspLinkedRecordDto[] linkedRecords)
    {
        if (!Enum.IsDefined(sectionType))
        {
            throw new SspSectionValidationException("A valid SSP section type is required.");
        }

        if (sourceReferences is null || linkedRecords is null)
            throw new SspSectionValidationException("Source references and linked records are required arrays.");
        ValidateText(title, "Title", 200);
        ValidateText(owner, "Owner", 200);
        if (sourceReferences.Length == 0 && linkedRecords.Length == 0)
        {
            throw new SspSectionValidationException("At least one source reference or linked source record is required.");
        }

        foreach (var source in sourceReferences)
        {
            if (source is null) throw new SspSectionValidationException("Source references cannot contain null items.");
            ValidateText(source.Source, "Source", 200);
            ValidateText(source.SourceUrl, "Source URL", 1000);
            if (!Uri.TryCreate(source.SourceUrl, UriKind.Absolute, out var sourceUri) || sourceUri.Scheme is not ("http" or "https"))
            {
                throw new SspSectionValidationException("Source URL must be absolute.");
            }
            if (source.LastReviewedAt == default || source.LastReviewedAt > DateOnly.FromDateTime(DateTime.UtcNow))
                throw new SspSectionValidationException("Source last-reviewed date is required and cannot be in the future.");
        }

        foreach (var record in linkedRecords)
        {
            if (record is null) throw new SspSectionValidationException("Linked records cannot contain null items.");
            if (!Enum.IsDefined(record.RecordType))
                throw new SspSectionValidationException("A valid linked record type is required.");
            ValidateText(record.RecordId, "Linked record ID", 120);
            ValidateText(record.Relationship, "Linked record relationship", 200);
        }
        if (linkedRecords.GroupBy(record => new { record.RecordType, Id = record.RecordId.Trim() }).Any(group => group.Count() > 1))
            throw new SspSectionValidationException("Duplicate linked records are not allowed.");
    }

    private static void ValidateStatusChange(SspSectionDto current, SspSectionStatusRequest request)
    {
        if (!Enum.IsDefined(request.Status))
        {
            throw new SspSectionValidationException("A valid SSP section status is required.");
        }

        if (request.ExpectedVersion != current.Version)
            throw new ContentRevisionConflictException();

        if (!SspSectionLifecycle.CanTransition(current.Status, request.Status))
            throw new SspSectionValidationException($"SSP section cannot move from {current.Status} to {request.Status}.");

        if (request.Status is SspSectionStatus.Approved)
        {
            if (string.IsNullOrWhiteSpace(request.Reviewer) || !request.ReviewDate.HasValue)
            {
                throw new SspSectionValidationException("Approval requires reviewer and review date.");
            }
            ValidateText(request.Reviewer, "Reviewer", 200);
            if (request.ReviewDate > DateOnly.FromDateTime(DateTime.UtcNow))
                throw new SspSectionValidationException("Review date cannot be in the future.");

            if (current.SourceReferences.Length == 0 && current.LinkedRecords.Length == 0 && string.IsNullOrWhiteSpace(request.ApprovalRationale))
            {
                throw new SspSectionValidationException("Approval requires source references or approval rationale.");
            }
            if (!string.IsNullOrWhiteSpace(request.ApprovalRationale))
                ValidateText(request.ApprovalRationale, "Approval rationale", 2000);
        }
    }

    private static void ValidateText(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new SspSectionValidationException($"{fieldName} is required.");
        }

        if (value.Trim().Length > maxLength)
        {
            throw new SspSectionValidationException($"{fieldName} must be {maxLength} characters or fewer.");
        }
    }

    private Task WriteAuditAsync(SspSectionDto section, Guid actorUserId, AuditAction action, string summary, CancellationToken cancellationToken) =>
        auditEventWriter.WriteAsync(
            tenantContext.TenantId,
            actorUserId,
            action,
            "SspSection",
            section.Id.ToString(),
            summary,
            new Dictionary<string, string>
            {
                ["sectionType"] = section.SectionType.ToString(),
                ["status"] = section.Status.ToString(),
                ["owner"] = section.Owner
            },
            cancellationToken);

    public async Task<IReadOnlyList<SspExportPackageDto>> ListExportPackagesAsync(CancellationToken cancellationToken = default) =>
        await exportPackageRepository.ListExportPackagesAsync(tenantContext.TenantId, cancellationToken);

    public async Task<SspExportPackageDto> GenerateExportPackageAsync(CreateSspExportPackageRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        ValidateText(request.PackageVersion, "Package version", 80);
        ValidateText(request.SystemBoundary, "System boundary", 500);
        ValidateText(request.Reviewer, "Reviewer", 200);
        if (request.ExternalShareRequested && !request.ApprovedForExternalSharing)
        {
            throw new SspExportPackageValidationException("External SSP package sharing requires explicit approval.");
        }

        var sections = await repository.ListAsync(tenantContext.TenantId, cancellationToken);
        if (sections.Count == 0)
        {
            throw new SspExportPackageValidationException("At least one SSP section is required before export.");
        }

        var sectionExports = new List<SspExportSectionDto>();
        foreach (var section in sections)
        {
            var narrative = await narrativeRepository.GetCurrentApprovedNarrativeAsync(tenantContext.TenantId, section.Id, cancellationToken);
            sectionExports.Add(new SspExportSectionDto(
                section.Id,
                section.SectionType,
                section.Title,
                section.Status,
                section.Owner,
                section.Reviewer,
                section.ReviewDate,
                section.SourceReferences,
                narrative?.ApprovedText,
                narrative?.Id,
                narrative?.ReviewDate));
        }

        var includedEvidence = request.EvidenceRecords
            .Where(record => record.TenantId == tenantContext.TenantId &&
                record.Status == SspExportRecordStatus.Approved &&
                record.Classification is not SspExportRecordClassification.Unknown and not SspExportRecordClassification.Prohibited)
            .Select(record => record with
            {
                RecordType = record.RecordType.Trim(),
                RecordId = record.RecordId.Trim(),
                Title = record.Title.Trim()
            })
            .ToArray();

        var package = await exportPackageRepository.CreateAsync(
            tenantContext.TenantId,
            request,
            sectionExports.ToArray(),
            includedEvidence,
            actorUserId,
            cancellationToken);
        await auditEventWriter.WriteAsync(
            tenantContext.TenantId,
            actorUserId,
            AuditAction.Exported,
            "SspExportPackage",
            package.Id.ToString(),
            "SSP review package was exported.",
            new Dictionary<string, string>
            {
                ["packageVersion"] = package.PackageVersion,
                ["includedSections"] = package.Sections.Length.ToString(),
                ["includedEvidence"] = package.IncludedEvidence.Length.ToString()
            },
            cancellationToken);
        return package;
    }
}

public interface ISspSectionRepository
{
    Task<IReadOnlyList<SspSectionDto>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<SspSectionDto?> GetAsync(Guid tenantId, Guid sectionId, CancellationToken cancellationToken = default);
    Task<SspSectionDto> CreateAsync(Guid tenantId, CreateSspSectionRequest request, Guid actorUserId, string actorName, CancellationToken cancellationToken = default);
    Task<SspSectionDto?> UpdateAsync(Guid tenantId, Guid sectionId, UpdateSspSectionRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<SspSectionDto?> ChangeStatusAsync(Guid tenantId, Guid sectionId, SspSectionStatusRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
}

public interface ISspSectionLinkValidator
{
    Task ValidateAsync(Guid tenantId, IReadOnlyCollection<SspLinkedRecordDto> records, CancellationToken cancellationToken = default);
}

public sealed class PermissiveSspSectionLinkValidator : ISspSectionLinkValidator
{
    public Task ValidateAsync(Guid tenantId, IReadOnlyCollection<SspLinkedRecordDto> records, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public interface ISspNarrativeRepository
{
    Task<IReadOnlyList<SspNarrativeDto>> ListNarrativesAsync(Guid tenantId, Guid sectionId, CancellationToken cancellationToken = default);
    Task<SspNarrativeDto?> GetNarrativeAsync(Guid tenantId, Guid sectionId, Guid narrativeId, CancellationToken cancellationToken = default);
    Task<SspNarrativeDto?> GetCurrentApprovedNarrativeAsync(Guid tenantId, Guid sectionId, CancellationToken cancellationToken = default);
    Task<SspNarrativeDto> CreateDraftAsync(Guid tenantId, Guid sectionId, string generatedText, bool aiAssisted, string? reviewerNotes, ContentClassificationDto classification, IReadOnlyList<ResolvedSspNarrativeSource> sources, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<SspNarrativeDto?> UpdateDraftAsync(Guid tenantId, Guid sectionId, Guid narrativeId, EditSspNarrativeDraftRequest request, ContentClassificationDto classification, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<SspNarrativeDto?> ApproveAsync(Guid tenantId, Guid sectionId, Guid narrativeId, ApproveSspNarrativeRequest request, Guid reviewerUserId, string reviewerName, CancellationToken cancellationToken = default);
}

public interface ISspNarrativeSourceResolver
{
    Task<IReadOnlyList<ResolvedSspNarrativeSource>> ResolveAsync(Guid tenantId, IReadOnlyList<SspNarrativeSourceLinkRequest> sources, CancellationToken cancellationToken = default);
}

public interface ISspNarrativeAiGenerator
{
    Task<string> GenerateAsync(IReadOnlyList<ResolvedSspNarrativeSource> sources, CancellationToken cancellationToken = default);
}

public interface ISspExportPackageRepository
{
    Task<IReadOnlyList<SspExportPackageDto>> ListExportPackagesAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<SspExportPackageDto> CreateAsync(Guid tenantId, CreateSspExportPackageRequest request, SspExportSectionDto[] sections, SspExportRecordDto[] includedEvidence, Guid actorUserId, CancellationToken cancellationToken = default);
}

public sealed record CreateSspSectionRequest(SspSectionType SectionType, string Title, string Owner, SspLinkedRecordDto[] LinkedRecords, SspSourceReferenceDto[] SourceReferences);
public sealed record UpdateSspSectionRequest(SspSectionType SectionType, string Title, string Owner, SspLinkedRecordDto[] LinkedRecords, SspSourceReferenceDto[] SourceReferences, long ExpectedVersion);
public sealed record SspSectionStatusRequest(SspSectionStatus Status, string ActorName, long ExpectedVersion, DateOnly? ReviewDate = null, string? Reviewer = null, string? ApprovalRationale = null);
public sealed record SspLinkedRecordDto(SspLinkedRecordType RecordType, string RecordId, string Relationship);
public sealed record SspSourceReferenceDto(string Source, string SourceUrl, DateOnly LastReviewedAt);
public sealed record SspSectionHistoryDto(SspSectionStatus Status, Guid ActorUserId, string ActorName, DateTimeOffset ChangedAt, string? Notes);
public sealed record SspSectionDto(Guid Id, Guid TenantId, SspSectionType SectionType, string Title, string Owner, SspSectionStatus Status, string? Reviewer, DateOnly? ReviewDate, string? ApprovalRationale, bool IsRequired, long Version, SspLinkedRecordDto[] LinkedRecords, SspSourceReferenceDto[] SourceReferences, SspSectionHistoryDto[] History, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public enum SspNarrativeGenerationMode { Deterministic, AiAssisted }
public sealed record GenerateSspNarrativeDraftRequest(SspNarrativeSourceLinkRequest[] Sources, SspNarrativeGenerationMode GenerationMode = SspNarrativeGenerationMode.Deterministic);
public sealed record EditSspNarrativeDraftRequest(string EditedText, string? ReviewerNotes, ContentClassificationRequest? Classification, long ExpectedVersion);
public sealed record ApproveSspNarrativeRequest(DateOnly? ReviewDate, long ExpectedVersion);
public sealed record SspNarrativeSourceLinkRequest(SspNarrativeSourceType SourceType, string RecordId);
public sealed record ResolvedSspNarrativeSource(SspNarrativeSourceType SourceType, string RecordId, string Label, string Summary, string SourceUrl, string Fingerprint, ContentClassification Classification);
public sealed record SspNarrativeSourceRecordDto(SspNarrativeSourceType SourceType, string RecordId, string Label, string Summary, string SourceUrl, string Fingerprint, ContentClassification Classification);
public sealed record SspNarrativeDto(Guid Id, Guid TenantId, Guid SectionId, string GeneratedText, string? EditedText, string? ApprovedText, SspNarrativeStatus Status, bool AiAssisted, bool DraftOnly, string? ReviewerNotes, Guid? ReviewerUserId, string? Reviewer, DateOnly? ReviewDate, long Version, ContentClassificationDto Classification, SspNarrativeSourceRecordDto[] SourceRecords, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record SspNarrativeComparisonDto(Guid SectionId, Guid? ApprovedNarrativeId, Guid DraftNarrativeId, string? CurrentApprovedText, string ProposedText, Guid? CurrentApprovedReviewerUserId, string? CurrentApprovedReviewer, DateOnly? CurrentApprovedReviewDate, string? ProposedReviewerNotes, SspNarrativeSourceRecordDto[] ProposedSources, SspNarrativeSourceRecordDto[] CurrentApprovedSources);
public sealed record CreateSspExportPackageRequest(string PackageVersion, string SystemBoundary, string Reviewer, SspExportFormat Format, bool ExternalShareRequested, bool ApprovedForExternalSharing, string[] PoamReferences, SspExportRecordDto[] EvidenceRecords);
public sealed record SspExportRecordDto(string RecordType, string RecordId, Guid TenantId, string Title, SspExportRecordStatus Status, SspExportRecordClassification Classification);
public sealed record SspExportSectionDto(Guid SectionId, SspSectionType SectionType, string Title, SspSectionStatus Status, string Owner, string? Reviewer, DateOnly? ReviewDate, SspSourceReferenceDto[] SourceReferences, string? ApprovedNarrativeText, Guid? ApprovedNarrativeId, DateOnly? NarrativeReviewDate);
public sealed record SspExportHistoryDto(string PackageVersion, Guid ActorUserId, DateTimeOffset GeneratedAt, string Action);
public sealed record SspExportPackageDto(Guid Id, Guid TenantId, DateTimeOffset GeneratedAt, string PackageVersion, string SystemBoundary, string Reviewer, SspExportFormat Format, string AuthorizationLanguage, string HumanReadableReport, SspExportSectionDto[] Sections, SspExportRecordDto[] IncludedEvidence, string[] PoamReferences, SspExportHistoryDto[] History);

public enum SspExportFormat { HumanReadable, MachineReadable, Both }
public enum SspExportRecordStatus { Draft, InReview, Approved, Superseded, Archived }
public enum SspExportRecordClassification { Public, Fci, Cui, Unknown, Prohibited }

public sealed class SspSectionValidationException(string message) : InvalidOperationException(message);
public sealed class SspNarrativeValidationException(string message) : InvalidOperationException(message);
public sealed class SspExportPackageValidationException(string message) : InvalidOperationException(message);
