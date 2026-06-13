import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const { fallbackOverview, getComplianceOverviewMock, getTenantMembersMock, members, overview } = vi.hoisted(() => ({
  fallbackOverview: {
    productPromise:
      "Connect to the GCCS API to load source-backed modules, obligations, review metadata, and tenant-scoped compliance workflow state.",
    mvpDataPosture: "No-CUI / compliance management only",
    modules: [],
    priorityObligations: []
  },
  getComplianceOverviewMock: vi.fn(),
  getTenantMembersMock: vi.fn(),
  members: [
    {
      membershipId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1",
      tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
      userId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
      email: "admin@example.com",
      displayName: "Avery Admin",
      userStatus: "Active",
      membershipStatus: "Active",
      roleName: "Admin",
      mfaEnabled: true,
      lastSignedInAt: null,
      lastAccessedAt: null,
      createdAt: "2026-06-13T12:00:00Z",
      updatedAt: null
    }
  ],
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
  getComplianceOverview: getComplianceOverviewMock,
  getTenantMembers: getTenantMembersMock
}));

import { App } from "@/App";

describe("App", () => {
  beforeEach(() => {
    getComplianceOverviewMock.mockReset();
    getTenantMembersMock.mockReset();
  });

  afterEach(() => {
    cleanup();
  });

  it("renders the compliance workspace from the overview data", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(overview);
    getTenantMembersMock.mockResolvedValueOnce(members);

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
    expect(await screen.findByText("Avery Admin")).toBeInTheDocument();
    expect(screen.getByText("admin@example.com")).toBeInTheDocument();
    expect(screen.getByText("Enabled")).toBeInTheDocument();
    expect(screen.getByText("No-CUI")).toBeInTheDocument();
  });

  it("shows empty states instead of UI-only source-backed content when the API is unavailable", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(fallbackOverview);
    getTenantMembersMock.mockResolvedValueOnce([]);

    render(<App />);

    expect(await screen.findByText("API overview unavailable")).toBeInTheDocument();
    expect(screen.getByText("Source data unavailable")).toBeInTheDocument();
    expect(screen.getByText("No tenant members available")).toBeInTheDocument();
    expect(screen.queryByText("FAR 52.204-21")).not.toBeInTheDocument();
    expect(screen.queryByText("admin@example.com")).not.toBeInTheDocument();
  });
});
