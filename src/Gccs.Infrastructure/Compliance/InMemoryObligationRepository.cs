using Gccs.Application.Repositories;
using Gccs.Domain.Compliance;

namespace Gccs.Infrastructure.Compliance;

public sealed class InMemoryObligationRepository : IObligationRepository
{
    private static readonly Obligation[] Obligations =
    [
        new(
            "far-52-204-21",
            "FAR 52.204-21",
            "Basic Safeguarding of Covered Contractor Information Systems",
            "Apply baseline safeguards when contractor systems process, store, or transmit Federal Contract Information.",
            "Contract involves Federal Contract Information.",
            "Implement and retain evidence for the basic safeguarding controls required by the clause.",
            "IT/security",
            RiskLevel.High,
            "Include the substance of the clause in subcontracts where the subcontractor may have Federal Contract Information.",
            new ApplicabilityDimension("prime/sub", "federal contract", "FCI", "any", "any", "FCI access"),
            [
                new("Access control policy", "Policy or procedure describing authorized system access.", "IT/security"),
                new("MFA configuration", "Screenshot or export showing MFA enforcement for covered systems.", "IT/security"),
                new("Media disposal record", "Evidence that FCI media is sanitized or destroyed before disposal.", "IT/security")
            ],
            new ComplianceSource("FAR 52.204-21", new Uri("https://www.acquisition.gov/far/52.204-21"), new DateOnly(2026, 6, 3), null, "high", false)),
        new(
            "far-52-204-25",
            "FAR 52.204-25",
            "Prohibition on Certain Telecommunications and Video Surveillance Services or Equipment",
            "Screen covered telecom and video surveillance equipment or services for prohibited sources.",
            "Clause appears in a solicitation, contract, subcontract, or purchase order.",
            "Review internal and supplier technology for covered telecommunications restrictions and retain screening evidence.",
            "contracts/IT/procurement",
            RiskLevel.High,
            "Flow down as required by the clause and prime contract instructions.",
            new ApplicabilityDimension("prime/sub", "federal contract", "IT services/equipment", "any", "any", "supplier technology"),
            [
                new("Supplier attestation", "Vendor response confirming reviewed equipment and service sources.", "procurement"),
                new("Technology inventory review", "Inventory export annotated for covered telecom review.", "IT/security")
            ],
            new ComplianceSource("FAR 52.204-25", new Uri("https://www.acquisition.gov/far/52.204-25"), new DateOnly(2026, 6, 3), null, "high", false)),
        new(
            "far-52-204-27",
            "FAR 52.204-27",
            "Prohibition on a ByteDance Covered Application",
            "Prevent covered ByteDance applications on certain government or contractor information technology.",
            "Contract includes the covered application prohibition.",
            "Maintain device management, policy, and user attestation evidence showing the prohibited application is not used on covered IT.",
            "IT/security",
            RiskLevel.Medium,
            "Flow down according to contract and clause instructions.",
            new ApplicabilityDimension("prime/sub", "federal contract", "contractor IT", "any", "any", "covered IT use"),
            [
                new("MDM application inventory", "Export showing prohibited applications are blocked or absent.", "IT/security"),
                new("Acceptable use policy", "Policy language prohibiting covered applications on covered IT.", "IT/security")
            ],
            new ComplianceSource("FAR 52.204-27", new Uri("https://www.acquisition.gov/far/52.204-27"), new DateOnly(2026, 6, 3), null, "high", false)),
        new(
            "cmmc-32-cfr-170",
            "32 CFR Part 170",
            "Cybersecurity Maturity Model Certification Program",
            "Track CMMC applicability, assessment level, evidence, and annual affirmation for DoD contracts and subcontracts.",
            "DoD contract or subcontract involves FCI or CUI and includes CMMC requirements.",
            "Determine level, maintain assessment evidence, prepare affirmation, and track SSP or POA&M work where applicable.",
            "security/compliance",
            RiskLevel.Critical,
            "Subcontractor CMMC requirements depend on the information and work flowed to the subcontractor.",
            new ApplicabilityDimension("prime/sub", "DoD contract", "FCI/CUI", "DoD", "any", "FCI/CUI access"),
            [
                new("CMMC self-assessment", "Completed Level 1 or Level 2 readiness assessment.", "security/compliance"),
                new("System security plan", "SSP for the assessed environment when applicable.", "security/compliance"),
                new("POA&M tracker", "Tracked remediation items with owners and dates.", "security/compliance")
            ],
            new ComplianceSource("32 CFR Part 170", new Uri("https://www.ecfr.gov/current/title-32/subtitle-A/chapter-I/subchapter-G/part-170"), new DateOnly(2026, 6, 3), null, "high", true)),
        new(
            "far-52-222-41",
            "FAR 52.222-41",
            "Service Contract Labor Standards",
            "Identify covered service work and preserve wage determination, labor category, and payroll evidence.",
            "Service contract labor standards clause appears in a covered service contract.",
            "Map employees to labor categories, apply wage/fringe requirements, and retain payroll and classification evidence.",
            "HR/payroll/contracts",
            RiskLevel.High,
            "Flow down to covered service subcontracts as required.",
            new ApplicabilityDimension("prime/sub", "service contract", "labor records", "any", "place specific", "covered service work"),
            [
                new("Wage determination", "Applicable wage determination attached to contract records.", "contracts"),
                new("Labor category mapping", "Employee-to-labor-category mapping with basis for classification.", "HR/payroll"),
                new("Payroll evidence", "Payroll records showing wage and fringe compliance.", "HR/payroll")
            ],
            new ComplianceSource("FAR 52.222-41", new Uri("https://www.acquisition.gov/far/52.222-41"), new DateOnly(2026, 6, 3), null, "medium", true))
    ];

    public Task<IReadOnlyList<Obligation>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Obligation>>(Obligations);

    public Task<Obligation?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var obligation = Obligations.FirstOrDefault(candidate => string.Equals(candidate.Id, id, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(obligation);
    }
}
