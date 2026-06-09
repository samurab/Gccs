export type ModuleStatus = {
  key: string;
  name: string;
  purpose: string;
  status: string;
};

export type ObligationSummary = {
  id: string;
  source: string;
  title: string;
  ownerFunction: string;
  riskLevel: string;
  sourceUrl: string;
  lastReviewedAt: string;
};

export type ComplianceOverview = {
  productPromise: string;
  mvpDataPosture: string;
  modules: ModuleStatus[];
  priorityObligations: ObligationSummary[];
};

const fallbackOverview: ComplianceOverview = {
  productPromise:
    "Help small government contractors know what applies, prove what they did, and stay ready for audits, renewals, bids, and certifications.",
  mvpDataPosture: "No-CUI / compliance management only",
  modules: [
    {
      key: "company-profile",
      name: "Company compliance profile",
      purpose: "Capture UEI, CAGE, SAM, NAICS, certifications, roles, and data posture.",
      status: "planned"
    },
    {
      key: "contract-intake",
      name: "Contract and clause intake",
      purpose: "Collect solicitations, contracts, flow-downs, wage determinations, and CUI guides.",
      status: "planned"
    },
    {
      key: "obligations",
      name: "Obligation dashboard",
      purpose: "Map clauses to actions, owners, evidence, deadlines, and source links.",
      status: "seeded"
    },
    {
      key: "evidence-vault",
      name: "Evidence vault",
      purpose: "Tag evidence by obligation, contract, control, vendor, employee, and expiration date.",
      status: "planned"
    }
  ],
  priorityObligations: [
    {
      id: "cmmc-32-cfr-170",
      source: "32 CFR Part 170",
      title: "Cybersecurity Maturity Model Certification Program",
      ownerFunction: "security/compliance",
      riskLevel: "Critical",
      sourceUrl: "https://www.ecfr.gov/current/title-32/subtitle-A/chapter-I/subchapter-G/part-170",
      lastReviewedAt: "2026-06-03"
    },
    {
      id: "far-52-204-21",
      source: "FAR 52.204-21",
      title: "Basic Safeguarding of Covered Contractor Information Systems",
      ownerFunction: "IT/security",
      riskLevel: "High",
      sourceUrl: "https://www.acquisition.gov/far/52.204-21",
      lastReviewedAt: "2026-06-03"
    }
  ]
};

export async function getComplianceOverview(): Promise<ComplianceOverview> {
  const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5062";

  try {
    const response = await fetch(`${apiBaseUrl}/api/compliance/overview`, {
      next: { revalidate: 30 }
    });

    if (!response.ok) {
      return fallbackOverview;
    }

    return response.json();
  } catch {
    return fallbackOverview;
  }
}
