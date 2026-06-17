using Gccs.Domain.Companies;
using Gccs.Domain.Contracts;

namespace Gccs.Application.Contracts;

public sealed record ContractDto(
    Guid Id,
    Guid TenantId,
    string ContractNumber,
    string Title,
    string AgencyOrPrimeName,
    ContractorRelationship Relationship,
    ContractKind Kind,
    ContractStatus Status,
    DateOnly? AwardedAt,
    DateOnly PeriodOfPerformanceStart,
    DateOnly PeriodOfPerformanceEnd,
    string PlaceOfPerformance,
    string Description,
    DataHandlingPosture DataHandlingPosture,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record UpsertContractRequest(
    string ContractNumber,
    string Title,
    string AgencyOrPrimeName,
    ContractorRelationship Relationship,
    ContractKind Kind,
    ContractStatus Status,
    DateOnly? AwardedAt,
    DateOnly PeriodOfPerformanceStart,
    DateOnly PeriodOfPerformanceEnd,
    string PlaceOfPerformance,
    string Description,
    DataHandlingPosture DataHandlingPosture);

public sealed record ContractDocumentDto(
    Guid Id,
    Guid ContractId,
    ContractDocumentType Type,
    string FileName,
    string ContentType,
    long SizeBytes,
    string? StorageUri,
    string? ExtractedTextHash,
    string ValidationStatus,
    string MalwareScanStatus,
    string NoticeVersion,
    DateTimeOffset UploadedAt,
    Guid UploadedByUserId,
    bool ContainsPotentialCui);

public sealed record ContractDocumentUploadRequest(
    ContractDocumentType Type,
    string FileName,
    string ContentType,
    long SizeBytes,
    bool ContainsPotentialCui);

public sealed record ExtractionJobDto(
    Guid Id,
    Guid TenantId,
    Guid SourceDocumentId,
    Guid RequestedByUserId,
    ExtractionJobStatus Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    string? FailureReason);

public sealed record ExtractionJobProcessingInputDto(
    ExtractionJobDto Job,
    ContractDocumentDto SourceDocument);

public sealed record ClauseCandidateDto(
    Guid Id,
    Guid TenantId,
    Guid ExtractionJobId,
    Guid SourceDocumentId,
    string NormalizedCitation,
    string RawExtractedText,
    string? DetectedTitle,
    decimal Confidence,
    string LocationMetadata,
    string MatchMethod,
    string? ClauseLibraryId,
    string ReviewStatus,
    DateTimeOffset CreatedAt);

public sealed record ClauseCandidateCreateDto(
    string NormalizedCitation,
    string RawExtractedText,
    string? DetectedTitle,
    decimal Confidence,
    string LocationMetadata,
    string MatchMethod,
    string? ClauseLibraryId);

public sealed record ExtractionJobProcessResultDto(
    ExtractionJobDto Job,
    IReadOnlyList<ClauseCandidateDto> Candidates);

public sealed record ContractDeliverableDto(
    Guid Id,
    Guid ContractId,
    string Name,
    string Description,
    DateOnly? DueAt,
    string OwnerFunction,
    DeliverableStatus Status,
    bool IsOverdue);

public sealed record UpsertContractDeliverableRequest(
    string Name,
    string Description,
    DateOnly? DueAt,
    string OwnerFunction,
    DeliverableStatus Status);

public sealed record ContractClauseDto(
    Guid Id,
    Guid ContractId,
    string ClauseLibraryId,
    string ClauseNumber,
    string Title,
    ClauseSource Source,
    string SourceUrl,
    DateOnly LastReviewedAt,
    string AttachmentReason,
    string? SourceDocumentReference,
    DateTimeOffset AttachedAt,
    Guid AttachedByUserId);

public sealed record AttachContractClauseRequest(
    string ClauseLibraryId,
    string AttachmentReason,
    string? SourceDocumentReference);

public sealed record RemoveContractClauseRequest(string Reason);

public sealed record GeneratedContractObligationsDto(
    Guid ContractClauseId,
    IReadOnlyList<string> ObligationIds,
    int TasksCreated);
