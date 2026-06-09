using Gccs.Application.Repositories;
using Gccs.Domain.Compliance;

namespace Gccs.Application.Compliance;

public sealed class ComplianceOverviewService(IObligationRepository obligationRepository)
{
    private static readonly MvpModule[] Modules =
    [
        new("company-profile", "Company compliance profile", "Capture UEI, CAGE, SAM, NAICS, certifications, roles, and data posture.", "planned"),
        new("contract-intake", "Contract and clause intake", "Collect solicitations, contracts, flow-downs, wage determinations, and CUI guides.", "planned"),
        new("obligations", "Obligation dashboard", "Map clauses to required actions, owners, evidence, deadlines, and source links.", "seeded"),
        new("calendar", "Compliance calendar", "Track renewals, reports, training, affirmations, deliverables, and policy reviews.", "planned"),
        new("evidence-vault", "Evidence vault", "Tag evidence by obligation, contract, control, vendor, employee, and expiration date.", "planned"),
        new("cmmc", "CMMC readiness tracker", "Track Level 1 and Level 2 controls, evidence, SSP, POA&M, assets, and affirmations.", "planned"),
        new("subcontractors", "Subcontractor flow-down tracker", "Track flow-down clauses, CMMC status, insurance, NDAs, CUI access, and workshare.", "planned"),
        new("reports", "Basic reports", "Generate obligation matrices, readiness reports, evidence packages, and risk dashboards.", "planned")
    ];

    public async Task<ComplianceOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var obligations = await obligationRepository.ListAsync(cancellationToken);
        var priorityObligations = obligations
            .OrderByDescending(obligation => obligation.RiskLevel)
            .ThenBy(obligation => obligation.Source)
            .Take(5)
            .Select(ToSummary)
            .ToArray();

        return new ComplianceOverviewDto(
            "Help small government contractors know what applies, prove what they did, and stay ready for audits, renewals, bids, and certifications.",
            "No-CUI / compliance management only",
            Modules.Select(module => new ModuleStatusDto(module.Key, module.Name, module.Purpose, module.Status)).ToArray(),
            priorityObligations);
    }

    private static ObligationSummaryDto ToSummary(Obligation obligation) =>
        new(
            obligation.Id,
            obligation.Source,
            obligation.Title,
            obligation.OwnerFunction,
            obligation.RiskLevel.ToString(),
            obligation.SourceReference.Url.ToString(),
            obligation.SourceReference.LastReviewedAt);
}
