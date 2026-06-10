using Gccs.Domain.Common;
using Gccs.Domain.Compliance;

namespace Gccs.Domain.Contracts;

public sealed record Contract(
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
    IReadOnlyList<ContractDocument> Documents,
    IReadOnlyList<ContractClause> Clauses,
    IReadOnlyList<Deliverable> Deliverables,
    IReadOnlyList<ReportingDeadline> ReportingDeadlines,
    IReadOnlyList<Guid> SubcontractorIds,
    EntityAudit Audit);

public sealed record Solicitation(
    Guid Id,
    Guid TenantId,
    string SolicitationNumber,
    string Title,
    string Agency,
    DateOnly? ResponseDueAt,
    ContractKind ExpectedContractKind,
    string SetAside,
    IReadOnlyList<ContractDocument> Documents,
    IReadOnlyList<ContractClause> Clauses,
    EntityAudit Audit);

public sealed record ContractDocument(
    Guid Id,
    ContractDocumentType Type,
    string FileName,
    Uri? StorageUri,
    string? ExtractedTextHash,
    DateTimeOffset UploadedAt,
    Guid UploadedByUserId,
    bool ContainsPotentialCui);

public sealed record ContractClause(
    Guid Id,
    string ClauseNumber,
    string Title,
    string? Alternate,
    string? FullText,
    ClauseSource Source,
    string? SourceHash,
    IReadOnlyList<string> ObligationIds,
    bool RequiresFlowDown,
    ReviewMetadata Review);

public sealed record Deliverable(
    Guid Id,
    string Name,
    string Description,
    DateOnly? DueAt,
    string OwnerFunction,
    DeliverableStatus Status);

public sealed record ReportingDeadline(
    Guid Id,
    string Name,
    string Description,
    DateOnly DueAt,
    RecurrencePattern Recurrence,
    string OwnerFunction,
    IReadOnlyList<string> SourceClauseNumbers);

public enum ContractorRelationship
{
    Prime,
    Subcontractor,
    Supplier,
    Consultant
}

public enum ContractKind
{
    Unknown,
    FixedPrice,
    TimeAndMaterials,
    CostReimbursement,
    IndefiniteDelivery,
    PurchaseOrder,
    Other
}

public enum ContractStatus
{
    Draft,
    Intake,
    Active,
    OptionPending,
    Closed,
    Archived
}

public enum ContractDocumentType
{
    Solicitation,
    Contract,
    Subcontract,
    PurchaseOrder,
    StatementOfWork,
    FlowDownAttachment,
    WageDetermination,
    Dd254,
    CuiMarkingGuide,
    Other
}

public enum ClauseSource
{
    Far,
    Dfars,
    AgencySupplement,
    PrimeFlowDown,
    Local
}

public enum DeliverableStatus
{
    NotStarted,
    InProgress,
    Submitted,
    Accepted,
    Late,
    Waived
}

public enum RecurrencePattern
{
    None,
    Weekly,
    Monthly,
    Quarterly,
    SemiAnnual,
    Annual
}
