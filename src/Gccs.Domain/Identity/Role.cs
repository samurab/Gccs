using Gccs.Domain.Common;

namespace Gccs.Domain.Identity;

public sealed record Role(
    Guid Id,
    Guid TenantId,
    string Name,
    IReadOnlyList<Permission> Permissions,
    EntityAudit Audit);

public enum Permission
{
    ManageTenant,
    ManageUsers,
    ManageCompanyProfile,
    ManageContracts,
    ManageObligations,
    ManageEvidence,
    ManageCmmc,
    ManageSubcontractors,
    ManageReports,
    ViewAuditLog,
    AuditorReadOnly
}
