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
    ViewCompanyProfile,
    ManageCompanyProfile,
    ViewContracts,
    ManageContracts,
    ReviewClauses,
    ViewObligations,
    ManageObligations,
    ViewTasks,
    ManageTasks,
    ViewEvidence,
    ManageEvidence,
    ApproveEvidence,
    ViewCmmc,
    ManageCmmc,
    ViewSubcontractors,
    ManageSubcontractors,
    ViewReports,
    ManageReports,
    ViewAuditLog,
    AuditorReadOnly,
    ViewEnclave,
    UploadEnclave,
    DownloadEnclave,
    ExportEnclave,
    ApproveEnclave,
    SupportEnclave,
    EmergencyEnclave
}

public static class RoleCatalog
{
    public const string Owner = "Owner";
    public const string Admin = "Admin";
    public const string ComplianceManager = "Compliance Manager";
    public const string Contributor = "Contributor";
    public const string Auditor = "Auditor";
    public const string Advisor = "Advisor";

    private static readonly IReadOnlyDictionary<string, string> CanonicalRoleNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [Owner] = Owner,
            [Admin] = Admin,
            [ComplianceManager] = ComplianceManager,
            [Contributor] = Contributor,
            [Auditor] = Auditor,
            [Advisor] = Advisor
        };

    private static readonly Permission[] AllWorkflowPermissions =
    [
        Permission.ViewCompanyProfile,
        Permission.ManageCompanyProfile,
        Permission.ViewContracts,
        Permission.ManageContracts,
        Permission.ReviewClauses,
        Permission.ViewObligations,
        Permission.ManageObligations,
        Permission.ViewTasks,
        Permission.ManageTasks,
        Permission.ViewEvidence,
        Permission.ManageEvidence,
        Permission.ApproveEvidence,
        Permission.ViewCmmc,
        Permission.ManageCmmc,
        Permission.ViewSubcontractors,
        Permission.ManageSubcontractors,
        Permission.ViewReports,
        Permission.ManageReports,
        Permission.ViewEnclave,
        Permission.UploadEnclave,
        Permission.DownloadEnclave,
        Permission.ExportEnclave,
        Permission.ApproveEnclave,
        Permission.SupportEnclave,
        Permission.EmergencyEnclave
    ];

    public static readonly IReadOnlyDictionary<string, IReadOnlySet<Permission>> PermissionsByRole =
        new Dictionary<string, IReadOnlySet<Permission>>(StringComparer.OrdinalIgnoreCase)
        {
            [Owner] = AllWorkflowPermissions
                .Concat([
                    Permission.ManageTenant,
                    Permission.ManageUsers,
                    Permission.ViewAuditLog,
                    Permission.AuditorReadOnly
                ])
                .ToHashSet(),
            [Admin] = AllWorkflowPermissions
                .Concat([
                    Permission.ManageUsers,
                    Permission.ViewAuditLog,
                    Permission.AuditorReadOnly
                ])
                .ToHashSet(),
            [ComplianceManager] = new HashSet<Permission>
            {
                Permission.ViewCompanyProfile,
                Permission.ManageCompanyProfile,
                Permission.ViewContracts,
                Permission.ManageContracts,
                Permission.ReviewClauses,
                Permission.ViewObligations,
                Permission.ManageObligations,
                Permission.ViewTasks,
                Permission.ManageTasks,
                Permission.ViewEvidence,
                Permission.ManageEvidence,
                Permission.ApproveEvidence,
                Permission.ViewCmmc,
                Permission.ManageCmmc,
                Permission.ViewSubcontractors,
                Permission.ManageSubcontractors,
                Permission.ViewReports,
                Permission.ManageReports,
                Permission.AuditorReadOnly
            },
            [Contributor] = new HashSet<Permission>
            {
                Permission.ViewCompanyProfile,
                Permission.ViewContracts,
                Permission.ViewObligations,
                Permission.ViewTasks,
                Permission.ManageTasks,
                Permission.ViewEvidence,
                Permission.ManageEvidence,
                Permission.ViewCmmc,
                Permission.ViewSubcontractors,
                Permission.ViewReports
            },
            [Auditor] = new HashSet<Permission>
            {
                Permission.ViewCompanyProfile,
                Permission.ViewContracts,
                Permission.ViewObligations,
                Permission.ViewTasks,
                Permission.ViewEvidence,
                Permission.ViewCmmc,
                Permission.ViewSubcontractors,
                Permission.ViewReports,
                Permission.AuditorReadOnly
            },
            [Advisor] = new HashSet<Permission>
            {
                Permission.ViewCompanyProfile,
                Permission.ViewContracts,
                Permission.ManageContracts,
                Permission.ReviewClauses,
                Permission.ViewObligations,
                Permission.ManageObligations,
                Permission.ViewTasks,
                Permission.ManageTasks,
                Permission.ViewEvidence,
                Permission.ManageEvidence,
                Permission.ApproveEvidence,
                Permission.ViewCmmc,
                Permission.ManageCmmc,
                Permission.ViewSubcontractors,
                Permission.ManageSubcontractors,
                Permission.ViewReports,
                Permission.ManageReports,
                Permission.ViewAuditLog,
                Permission.AuditorReadOnly
            }
        };

    public static IReadOnlyList<string> Roles => CanonicalRoleNames.Values.ToArray();

    public static bool TryNormalizeRoleName(string? roleName, out string canonicalRoleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            canonicalRoleName = string.Empty;
            return false;
        }

        return CanonicalRoleNames.TryGetValue(roleName.Trim(), out canonicalRoleName!);
    }

    public static IReadOnlySet<Permission> GetPermissions(string roleName) =>
        TryNormalizeRoleName(roleName, out var canonicalRoleName) &&
        PermissionsByRole.TryGetValue(canonicalRoleName, out var permissions)
            ? permissions
            : new HashSet<Permission>();
}
