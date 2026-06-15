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
