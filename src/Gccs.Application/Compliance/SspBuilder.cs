using Gccs.Application.Audit;
using Gccs.Application.Security;
using Gccs.Domain.Audit;

namespace Gccs.Application.Compliance;

public sealed class SspSectionService(
    ISspSectionRepository repository,
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
}

public interface ISspSectionRepository
{
    Task<IReadOnlyList<SspSectionDto>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<SspSectionDto?> GetAsync(Guid tenantId, Guid sectionId, CancellationToken cancellationToken = default);
    Task<SspSectionDto> CreateAsync(Guid tenantId, CreateSspSectionRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<SspSectionDto?> UpdateAsync(Guid tenantId, Guid sectionId, UpdateSspSectionRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<SspSectionDto?> ChangeStatusAsync(Guid tenantId, Guid sectionId, SspSectionStatusRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
}

public sealed record CreateSspSectionRequest(SspSectionType SectionType, string Title, string Owner, SspLinkedRecordDto[] LinkedRecords, SspSourceReferenceDto[] SourceReferences);
public sealed record UpdateSspSectionRequest(SspSectionType SectionType, string Title, string Owner, SspLinkedRecordDto[] LinkedRecords, SspSourceReferenceDto[] SourceReferences);
public sealed record SspSectionStatusRequest(SspSectionStatus Status, string ActorName, DateOnly? ReviewDate = null, string? Reviewer = null, string? ApprovalRationale = null);
public sealed record SspLinkedRecordDto(string RecordType, string RecordId, string Relationship);
public sealed record SspSourceReferenceDto(string Source, string SourceUrl, DateOnly LastReviewedAt);
public sealed record SspSectionHistoryDto(SspSectionStatus Status, Guid ActorUserId, string ActorName, DateTimeOffset ChangedAt, string? Notes);
public sealed record SspSectionDto(Guid Id, Guid TenantId, SspSectionType SectionType, string Title, string Owner, SspSectionStatus Status, string? Reviewer, DateOnly? ReviewDate, SspLinkedRecordDto[] LinkedRecords, SspSourceReferenceDto[] SourceReferences, SspSectionHistoryDto[] History, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

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

public sealed class SspSectionValidationException(string message) : InvalidOperationException(message);
