using Gccs.Domain.Common;

namespace Gccs.Domain.Tenancy;

public sealed record Tenant(
    Guid Id,
    string Name,
    TenantStatus Status,
    TenantDataPosture DataPosture,
    DateOnly? TrialEndsAt,
    EntityAudit Audit);

public enum TenantStatus
{
    Active,
    Trialing,
    Suspended,
    Archived
}

public enum TenantDataPosture
{
    NoCui,
    CuiReady,
    GovCloud
}
