import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { ApplicabilityFactsPanel } from "./ApplicabilityFactsPanel";
import { getApplicabilityFacts, type ApplicabilityFact } from "@/lib/api";

vi.mock("@/lib/api", () => ({
  getApplicabilityFacts: vi.fn()
}));

const facts: ApplicabilityFact[] = [
  {
    tenantId: "tenant-a",
    key: "contract.data_type",
    value: "FciOnly",
    isUnknown: false,
    sourceType: "Contract",
    sourceId: "contract-1",
    lastUpdatedAt: "2026-09-17T14:00:00Z"
  },
  {
    tenantId: "tenant-a",
    key: "company.agency_customer",
    value: "unknown",
    isUnknown: true,
    sourceType: "CompanyProfile",
    sourceId: "profile-1",
    lastUpdatedAt: null
  }
];

beforeEach(() => {
  vi.resetAllMocks();
  vi.mocked(getApplicabilityFacts).mockResolvedValue(facts);
});

afterEach(cleanup);

it("shows derived facts, provenance, and unknown values without edit controls", async () => {
  render(<ApplicabilityFactsPanel contractId="contract-1" canView />);

  expect(await screen.findByText("contract.data_type")).toBeVisible();
  expect(screen.getByText("FciOnly")).toBeVisible();
  expect(screen.getByText("CompanyProfile")).toBeVisible();
  expect(screen.getByText("Unknown")).toBeVisible();
  expect(screen.queryByRole("button", { name: /save|edit/i })).not.toBeInTheDocument();
});

it("refreshes the derived facts on demand", async () => {
  render(<ApplicabilityFactsPanel contractId="contract-1" canView />);
  await screen.findByText("contract.data_type");

  await userEvent.setup().click(screen.getByRole("button", { name: "Refresh facts" }));

  expect(getApplicabilityFacts).toHaveBeenCalledTimes(2);
  expect(getApplicabilityFacts).toHaveBeenLastCalledWith("contract-1");
});

it("shows load failures and does not fetch without permission", async () => {
  vi.mocked(getApplicabilityFacts).mockRejectedValueOnce(new Error("Facts unavailable"));
  render(<ApplicabilityFactsPanel contractId="contract-1" canView />);
  expect(await screen.findByRole("alert")).toHaveTextContent("Facts unavailable");

  vi.mocked(getApplicabilityFacts).mockClear();
  render(<ApplicabilityFactsPanel contractId="contract-2" canView={false} />);
  expect(getApplicabilityFacts).not.toHaveBeenCalled();
});
