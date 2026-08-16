using Gccs.Infrastructure.Persistence.Models;

namespace Gccs.Infrastructure.Compliance;

public static class MvpModuleCatalog
{
    public static IReadOnlyList<MvpModuleDefinition> Definitions { get; } =
    [
        new("company-profile", "Company compliance profile", "Capture UEI, CAGE, SAM, NAICS, certifications, roles, and data posture.", "active"),
        new("contract-intake", "Contract and clause intake", "Collect solicitations, contracts, flow-downs, wage determinations, and CUI guides.", "active"),
        new("obligations", "Obligation dashboard", "Map clauses to required actions, owners, evidence, deadlines, and source links.", "seeded"),
        new("calendar", "Compliance calendar", "Track renewals, reports, training, affirmations, deliverables, and policy reviews.", "active"),
        new("evidence-vault", "Evidence vault", "Tag evidence by obligation, contract, control, vendor, employee, and expiration date.", "active"),
        new("cmmc", "CMMC readiness tracker", "Track Level 1 and Level 2 controls, evidence, SSP, POA&M, assets, and affirmations.", "active"),
        new("subcontractors", "Subcontractor flow-down tracker", "Track flow-down clauses, CMMC status, insurance, NDAs, CUI access, and workshare.", "active"),
        new("reports", "Basic reports", "Generate obligation matrices, readiness reports, evidence packages, and risk dashboards.", "active")
    ];

    public static IReadOnlyList<MvpModuleEntity> CreateEntities() =>
        Definitions
            .Select(definition => new MvpModuleEntity
            {
                Key = definition.Key,
                Name = definition.Name,
                Purpose = definition.Purpose,
                Status = definition.Status
            })
            .ToArray();

    public sealed record MvpModuleDefinition(
        string Key,
        string Name,
        string Purpose,
        string Status);
}
