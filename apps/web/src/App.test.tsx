import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const { fallbackOverview, getComplianceOverviewMock, overview } = vi.hoisted(() => ({
  fallbackOverview: {
    productPromise:
      "Connect to the GCCS API to load source-backed modules, obligations, review metadata, and tenant-scoped compliance workflow state.",
    mvpDataPosture: "No-CUI / compliance management only",
    modules: [],
    priorityObligations: []
  },
  getComplianceOverviewMock: vi.fn(),
  overview: {
    productPromise: "Keep every govcon obligation tied to evidence and review status.",
    mvpDataPosture: "No-CUI / compliance management only",
    modules: [
      {
        key: "company-profile",
        name: "Company compliance profile",
        purpose: "Capture entity, SAM, NAICS, certification, and data posture details.",
        status: "planned"
      },
      {
        key: "obligations",
        name: "Obligation dashboard",
        purpose: "Map clauses to actions, owners, evidence, deadlines, and source links.",
        status: "seeded"
      }
    ],
    priorityObligations: [
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
  }
}));

vi.mock("@/lib/api", () => ({
  fallbackOverview,
  getComplianceOverview: getComplianceOverviewMock
}));

import { App } from "@/App";

describe("App", () => {
  beforeEach(() => {
    getComplianceOverviewMock.mockReset();
  });

  afterEach(() => {
    cleanup();
  });

  it("renders the compliance workspace from the overview data", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(overview);

    render(<App />);

    expect(
      screen.getByRole("heading", {
        name: /govcon obligations, evidence, and readiness in one operating view/i
      })
    ).toBeInTheDocument();
    expect(await screen.findByText(overview.productPromise)).toBeInTheDocument();
    expect(screen.getByText("Company compliance profile")).toBeInTheDocument();
    expect(screen.getByText("Obligation dashboard")).toBeInTheDocument();
    expect(await screen.findByText("FAR 52.204-21")).toBeInTheDocument();
    expect(screen.getByText("No-CUI")).toBeInTheDocument();
  });

  it("shows empty states instead of UI-only source-backed content when the API is unavailable", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(fallbackOverview);

    render(<App />);

    expect(await screen.findByText("API overview unavailable")).toBeInTheDocument();
    expect(screen.getByText("Source data unavailable")).toBeInTheDocument();
    expect(screen.queryByText("FAR 52.204-21")).not.toBeInTheDocument();
  });
});
