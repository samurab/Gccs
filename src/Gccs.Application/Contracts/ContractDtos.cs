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
