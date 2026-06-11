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

export const fallbackOverview: ComplianceOverview = {
  productPromise:
    "Connect to the GCCS API to load source-backed modules, obligations, review metadata, and tenant-scoped compliance workflow state.",
  mvpDataPosture: "No-CUI / compliance management only",
  modules: [],
  priorityObligations: []
};

export async function getComplianceOverview(): Promise<ComplianceOverview> {
  const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5062";
  const headers =
    import.meta.env.DEV
      ? {
          "X-Gccs-Dev-Auth": "true"
        }
      : undefined;

  try {
    const response = await fetch(`${apiBaseUrl}/api/compliance/overview`, { headers });

    if (!response.ok) {
      return fallbackOverview;
    }

    return response.json();
  } catch {
    return fallbackOverview;
  }
}
