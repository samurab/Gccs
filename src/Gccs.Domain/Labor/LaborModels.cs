using Gccs.Domain.Common;

namespace Gccs.Domain.Labor;

public sealed record WageDetermination(
    Guid Id,
    Guid TenantId,
    string DeterminationNumber,
    string Revision,
    string PlaceOfPerformance,
    DateOnly EffectiveAt,
    DateOnly? ExpiresAt,
    Uri? SourceUrl,
    IReadOnlyList<LaborCategoryRate> Rates,
    EntityAudit Audit);

public sealed record LaborCategoryRate(
    string LaborCategory,
    decimal HourlyWage,
    decimal FringeBenefitRate,
    string Currency);

public sealed record LaborClassification(
    Guid Id,
    Guid TenantId,
    Guid EmployeeId,
    Guid ContractId,
    string LaborCategory,
    string BasisForClassification,
    Guid? WageDeterminationId,
    Guid? EvidenceItemId,
    EntityAudit Audit);

public sealed record PayrollRecord(
    Guid Id,
    Guid TenantId,
    Guid EmployeeId,
    Guid ContractId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal HoursWorked,
    decimal WagePaid,
    decimal FringePaid,
    Guid? EvidenceItemId,
    EntityAudit Audit);
