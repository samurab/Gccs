import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

const { overview } = vi.hoisted(() => ({
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
  fallbackOverview: overview,
  getComplianceOverview: vi.fn(() => Promise.resolve(overview))
}));

import { App } from "@/App";

describe("App", () => {
  it("renders the compliance workspace from the overview data", async () => {
    render(<App />);

    expect(
      screen.getByRole("heading", {
        name: /govcon obligations, evidence, and readiness in one operating view/i
      })
    ).toBeInTheDocument();
    expect(screen.getByText(overview.productPromise)).toBeInTheDocument();
    expect(screen.getByText("Company compliance profile")).toBeInTheDocument();
    expect(screen.getByText("Obligation dashboard")).toBeInTheDocument();
    expect(await screen.findByText("FAR 52.204-21")).toBeInTheDocument();
    expect(screen.getByText("No-CUI")).toBeInTheDocument();
  });
});
