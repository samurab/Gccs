using Gccs.Application.Audit;
using Gccs.Application.Security;
using Gccs.Domain.Audit;

namespace Gccs.Application.Compliance;

public sealed class SspSectionService(
    ISspSectionRepository repository,
    ISspNarrativeRepository narrativeRepository,
    ISspExportPackageRepository exportPackageRepository,
    ICurrentTenantContext tenantContext,
    IAuditEventWriter auditEventWriter)
{
    public async Task<IReadOnlyList<SspSectionDto>> ListAsync(CancellationToken cancellationToken = default) =>
        await repository.ListAsync(tenantContext.TenantId, cancellationToken);

    public async Task<SspSectionDto?> GetAsync(Guid sectionId, CancellationToken cancellationToken = default) =>
        await repository.GetAsync(tenantContext.TenantId, sectionId, cancellationToken);

    public async Task<SspSectionDto> CreateAsync(CreateSspSectionRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        ValidateSection(request.SectionType, request.Title, request.Owner, request.SourceReferences, request.LinkedRecords);
        var section = await repository.CreateAsync(tenantContext.TenantId, request, actorUserId, cancellationToken);
        await WriteAuditAsync(section, actorUserId, AuditAction.Created, "SSP section was created.", cancellationToken);
        return section;
    }

    public async Task<SspSectionDto?> UpdateAsync(Guid sectionId, UpdateSspSectionRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        ValidateSection(request.SectionType, request.Title, request.Owner, request.SourceReferences, request.LinkedRecords);
        var section = await repository.UpdateAsync(tenantContext.TenantId, sectionId, request, actorUserId, cancellationToken);
        if (section is not null)
        {
            await WriteAuditAsync(section, actorUserId, AuditAction.Updated, "SSP section was updated.", cancellationToken);
        }

        return section;
    }

    public async Task<SspSectionDto?> ChangeStatusAsync(Guid sectionId, SspSectionStatusRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ActorName))
        {
            throw new SspSectionValidationException("Actor name is required.");
        }

        var current = await repository.GetAsync(tenantContext.TenantId, sectionId, cancellationToken);
        if (current is null)
        {
            return null;
        }

        ValidateStatusChange(current, request);
        var section = await repository.ChangeStatusAsync(tenantContext.TenantId, sectionId, request, actorUserId, cancellationToken);
        if (section is not null)
        {
            var action = request.Status switch
            {
                SspSectionStatus.Approved => AuditAction.Approved,
                SspSectionStatus.Archived or SspSectionStatus.Superseded => AuditAction.Archived,
                _ => AuditAction.Updated
            };
            await WriteAuditAsync(section, actorUserId, action, $"SSP section moved to {section.Status}.", cancellationToken);
        }

        return section;
    }

    private static void ValidateSection(SspSectionType sectionType, string title, string owner, SspSourceReferenceDto[] sourceReferences, SspLinkedRecordDto[] linkedRecords)
    {
        if (!Enum.IsDefined(sectionType))
        {
            throw new SspSectionValidationException("A valid SSP section type is required.");
        }

        ValidateText(title, "Title", 200);
        ValidateText(owner, "Owner", 200);
        if (sourceReferences.Length == 0 && linkedRecords.Length == 0)
        {
            throw new SspSectionValidationException("At least one source reference or linked source record is required.");
        }

        foreach (var source in sourceReferences)
        {
            ValidateText(source.Source, "Source", 200);
            ValidateText(source.SourceUrl, "Source URL", 1000);
            if (!Uri.TryCreate(source.SourceUrl, UriKind.Absolute, out _))
            {
                throw new SspSectionValidationException("Source URL must be absolute.");
            }
        }

        foreach (var record in linkedRecords)
        {
            ValidateText(record.RecordType, "Linked record type", 100);
            ValidateText(record.RecordId, "Linked record ID", 120);
            ValidateText(record.Relationship, "Linked record relationship", 200);
        }
    }

    private static void ValidateStatusChange(SspSectionDto current, SspSectionStatusRequest request)
    {
        if (!Enum.IsDefined(request.Status))
        {
            throw new SspSectionValidationException("A valid SSP section status is required.");
        }

        if (request.Status is SspSectionStatus.Approved)
        {
            if (string.IsNullOrWhiteSpace(request.Reviewer) || !request.ReviewDate.HasValue)
            {
                throw new SspSectionValidationException("Approval requires reviewer and review date.");
            }

            if (current.SourceReferences.Length == 0 && string.IsNullOrWhiteSpace(request.ApprovalRationale))
            {
                throw new SspSectionValidationException("Approval requires source references or approval rationale.");
            }
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

    public async Task<SspNarrativeDto?> GenerateNarrativeDraftAsync(Guid sectionId, GenerateSspNarrativeDraftRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var section = await repository.GetAsync(tenantContext.TenantId, sectionId, cancellationToken);
        if (section is null)
        {
            return null;
        }

        ValidateNarrativeSources(request.SourceRecords, tenantContext.TenantId, requireApproved: true);
        var narrative = await narrativeRepository.CreateDraftAsync(tenantContext.TenantId, sectionId, request, actorUserId, cancellationToken);
        await WriteNarrativeAuditAsync(narrative, actorUserId, AuditAction.Created, "SSP narrative draft was generated.", cancellationToken);
        return narrative;
    }

    public async Task<SspNarrativeDto?> EditNarrativeDraftAsync(Guid sectionId, Guid narrativeId, EditSspNarrativeDraftRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        ValidateText(request.EditedText, "Narrative text", 8000);
        var narrative = await narrativeRepository.UpdateDraftAsync(tenantContext.TenantId, sectionId, narrativeId, request, actorUserId, cancellationToken);
        if (narrative is not null)
        {
            await WriteNarrativeAuditAsync(narrative, actorUserId, AuditAction.Updated, "SSP narrative draft was edited.", cancellationToken);
        }

        return narrative;
    }

    public async Task<SspNarrativeDto?> ApproveNarrativeAsync(Guid sectionId, Guid narrativeId, ApproveSspNarrativeRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reviewer) || !request.ReviewDate.HasValue)
        {
            throw new SspNarrativeValidationException("Narrative approval requires reviewer and review date.");
        }

        var narrative = await narrativeRepository.GetNarrativeAsync(tenantContext.TenantId, sectionId, narrativeId, cancellationToken);
        if (narrative is null)
        {
            return null;
        }

        ValidateNarrativeApproval(narrative);
        var approved = await narrativeRepository.ApproveAsync(tenantContext.TenantId, sectionId, narrativeId, request, actorUserId, cancellationToken);
        if (approved is not null)
        {
            await WriteNarrativeAuditAsync(approved, actorUserId, AuditAction.Approved, "SSP narrative was approved.", cancellationToken);
        }

        return approved;
    }

    public async Task<SspNarrativeComparisonDto?> CompareNarrativeAsync(Guid sectionId, Guid narrativeId, CancellationToken cancellationToken = default)
    {
        var draft = await narrativeRepository.GetNarrativeAsync(tenantContext.TenantId, sectionId, narrativeId, cancellationToken);
        if (draft is null)
        {
            return null;
        }

        var approved = await narrativeRepository.GetCurrentApprovedNarrativeAsync(tenantContext.TenantId, sectionId, cancellationToken);
        return new SspNarrativeComparisonDto(
            sectionId,
            approved?.Id,
            draft.Id,
            approved?.ApprovedText,
            draft.EditedText ?? draft.GeneratedText,
            approved?.ReviewDate,
            draft.SourceRecords,
            approved?.SourceRecords ?? []);
    }

    private static void ValidateNarrativeSources(SspNarrativeSourceRecordDto[] sourceRecords, Guid tenantId, bool requireApproved)
    {
        if (sourceRecords.Length == 0)
        {
            throw new SspNarrativeValidationException("At least one source record is required.");
        }

        foreach (var record in sourceRecords)
        {
            ValidateText(record.RecordType, "Source record type", 100);
            ValidateText(record.RecordId, "Source record ID", 120);
            ValidateText(record.Summary, "Source summary", 1000);
            ValidateText(record.SourceUrl, "Source URL", 1000);
            if (!Uri.TryCreate(record.SourceUrl, UriKind.Absolute, out _))
            {
                throw new SspNarrativeValidationException("Source URL must be absolute.");
            }

            if (requireApproved && !record.Approved)
            {
                throw new SspNarrativeValidationException("Narrative drafts can only be generated from approved tenant records and approved compliance content.");
            }

            if (record.TenantId != tenantId)
            {
                throw new SspNarrativeValidationException("Narrative drafts can only use source records from the current tenant.");
            }
        }
    }

    private static void ValidateNarrativeApproval(SspNarrativeDto narrative)
    {
        if (narrative.SourceRecords.Length == 0)
        {
            throw new SspNarrativeValidationException("Narrative approval requires source links.");
        }

        if (narrative.SourceRecords.Any(source => source.Outdated))
        {
            throw new SspNarrativeValidationException("Narrative approval is blocked when source records are outdated.");
        }

        var text = narrative.EditedText ?? narrative.GeneratedText;
        if (text.Contains("{{", StringComparison.Ordinal) || text.Contains("}}", StringComparison.Ordinal))
        {
            throw new SspNarrativeValidationException("Narrative approval is blocked while unresolved placeholders remain.");
        }
    }

    private Task WriteNarrativeAuditAsync(SspNarrativeDto narrative, Guid actorUserId, AuditAction action, string summary, CancellationToken cancellationToken) =>
        auditEventWriter.WriteAsync(
            tenantContext.TenantId,
            actorUserId,
            action,
            "SspNarrative",
            narrative.Id.ToString(),
            summary,
            new Dictionary<string, string>
            {
                ["sectionId"] = narrative.SectionId.ToString(),
                ["status"] = narrative.Status.ToString(),
                ["draftOnly"] = narrative.DraftOnly.ToString()
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
    Task<SspSectionDto> CreateAsync(Guid tenantId, CreateSspSectionRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<SspSectionDto?> UpdateAsync(Guid tenantId, Guid sectionId, UpdateSspSectionRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<SspSectionDto?> ChangeStatusAsync(Guid tenantId, Guid sectionId, SspSectionStatusRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
}

public interface ISspNarrativeRepository
{
    Task<SspNarrativeDto?> GetNarrativeAsync(Guid tenantId, Guid sectionId, Guid narrativeId, CancellationToken cancellationToken = default);
    Task<SspNarrativeDto?> GetCurrentApprovedNarrativeAsync(Guid tenantId, Guid sectionId, CancellationToken cancellationToken = default);
    Task<SspNarrativeDto> CreateDraftAsync(Guid tenantId, Guid sectionId, GenerateSspNarrativeDraftRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<SspNarrativeDto?> UpdateDraftAsync(Guid tenantId, Guid sectionId, Guid narrativeId, EditSspNarrativeDraftRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<SspNarrativeDto?> ApproveAsync(Guid tenantId, Guid sectionId, Guid narrativeId, ApproveSspNarrativeRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
}

public interface ISspExportPackageRepository
{
    Task<IReadOnlyList<SspExportPackageDto>> ListExportPackagesAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<SspExportPackageDto> CreateAsync(Guid tenantId, CreateSspExportPackageRequest request, SspExportSectionDto[] sections, SspExportRecordDto[] includedEvidence, Guid actorUserId, CancellationToken cancellationToken = default);
}

public sealed record CreateSspSectionRequest(SspSectionType SectionType, string Title, string Owner, SspLinkedRecordDto[] LinkedRecords, SspSourceReferenceDto[] SourceReferences);
public sealed record UpdateSspSectionRequest(SspSectionType SectionType, string Title, string Owner, SspLinkedRecordDto[] LinkedRecords, SspSourceReferenceDto[] SourceReferences);
public sealed record SspSectionStatusRequest(SspSectionStatus Status, string ActorName, DateOnly? ReviewDate = null, string? Reviewer = null, string? ApprovalRationale = null);
public sealed record SspLinkedRecordDto(string RecordType, string RecordId, string Relationship);
public sealed record SspSourceReferenceDto(string Source, string SourceUrl, DateOnly LastReviewedAt);
public sealed record SspSectionHistoryDto(SspSectionStatus Status, Guid ActorUserId, string ActorName, DateTimeOffset ChangedAt, string? Notes);
public sealed record SspSectionDto(Guid Id, Guid TenantId, SspSectionType SectionType, string Title, string Owner, SspSectionStatus Status, string? Reviewer, DateOnly? ReviewDate, SspLinkedRecordDto[] LinkedRecords, SspSourceReferenceDto[] SourceReferences, SspSectionHistoryDto[] History, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record GenerateSspNarrativeDraftRequest(SspNarrativeSourceRecordDto[] SourceRecords, bool AiAssisted, string? ReviewerNotes = null);
public sealed record EditSspNarrativeDraftRequest(string EditedText, string? ReviewerNotes = null);
public sealed record ApproveSspNarrativeRequest(string Reviewer, DateOnly? ReviewDate);
public sealed record SspNarrativeSourceRecordDto(string RecordType, string RecordId, Guid TenantId, string Summary, string SourceUrl, bool Approved, bool Outdated);
public sealed record SspNarrativeDto(Guid Id, Guid TenantId, Guid SectionId, string GeneratedText, string? EditedText, string? ApprovedText, SspNarrativeStatus Status, bool AiAssisted, bool DraftOnly, string? ReviewerNotes, string? Reviewer, DateOnly? ReviewDate, SspNarrativeSourceRecordDto[] SourceRecords, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record SspNarrativeComparisonDto(Guid SectionId, Guid? ApprovedNarrativeId, Guid DraftNarrativeId, string? CurrentApprovedText, string ProposedText, DateOnly? CurrentApprovedReviewDate, SspNarrativeSourceRecordDto[] ProposedSources, SspNarrativeSourceRecordDto[] CurrentApprovedSources);
public sealed record CreateSspExportPackageRequest(string PackageVersion, string SystemBoundary, string Reviewer, SspExportFormat Format, bool ExternalShareRequested, bool ApprovedForExternalSharing, string[] PoamReferences, SspExportRecordDto[] EvidenceRecords);
public sealed record SspExportRecordDto(string RecordType, string RecordId, Guid TenantId, string Title, SspExportRecordStatus Status, SspExportRecordClassification Classification);
public sealed record SspExportSectionDto(Guid SectionId, SspSectionType SectionType, string Title, SspSectionStatus Status, string Owner, string? Reviewer, DateOnly? ReviewDate, SspSourceReferenceDto[] SourceReferences, string? ApprovedNarrativeText, Guid? ApprovedNarrativeId, DateOnly? NarrativeReviewDate);
public sealed record SspExportHistoryDto(string PackageVersion, Guid ActorUserId, DateTimeOffset GeneratedAt, string Action);
public sealed record SspExportPackageDto(Guid Id, Guid TenantId, DateTimeOffset GeneratedAt, string PackageVersion, string SystemBoundary, string Reviewer, SspExportFormat Format, string AuthorizationLanguage, string HumanReadableReport, SspExportSectionDto[] Sections, SspExportRecordDto[] IncludedEvidence, string[] PoamReferences, SspExportHistoryDto[] History);

public enum SspSectionType
{
    SystemDescription,
    AuthorizationBoundary,
    Environment,
    Interconnections,
    Users,
    Roles,
    DataTypes,
    CuiHandlingPosture,
    ControlImplementationNarratives,
    InheritedResponsibilities,
    ExternalServiceProviders,
    EvidenceReferences
}

public enum SspSectionStatus { Draft, InReview, Approved, Superseded, Archived }
public enum SspNarrativeStatus { Draft, Approved, Superseded, Archived }
public enum SspExportFormat { HumanReadable, MachineReadable, Both }
public enum SspExportRecordStatus { Draft, InReview, Approved, Superseded, Archived }
public enum SspExportRecordClassification { Public, Fci, Cui, Unknown, Prohibited }

public sealed class SspSectionValidationException(string message) : InvalidOperationException(message);
public sealed class SspNarrativeValidationException(string message) : InvalidOperationException(message);
public sealed class SspExportPackageValidationException(string message) : InvalidOperationException(message);
