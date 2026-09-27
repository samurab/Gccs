import { act, cleanup, render, screen, waitFor } from "@testing-library/react";
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

function deferred<T>() {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>((promiseResolve) => {
    resolve = promiseResolve;
  });
  return { promise, resolve };
}

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

it("clears the prior contract and ignores an old refresh response after the contract changes", async () => {
  const oldRefresh = deferred<ApplicabilityFact[]>();
  const newContract = deferred<ApplicabilityFact[]>();
  const newFacts = [{ ...facts[0], key: "contract.agency", sourceId: "contract-2", value: "USAF" }];
  vi.mocked(getApplicabilityFacts)
    .mockReset()
    .mockResolvedValueOnce(facts)
    .mockReturnValueOnce(oldRefresh.promise)
    .mockReturnValueOnce(newContract.promise);

  const { rerender } = render(<ApplicabilityFactsPanel contractId="contract-1" canView />);
  expect(await screen.findByText("contract.data_type")).toBeVisible();

  await userEvent.setup().click(screen.getByRole("button", { name: "Refresh facts" }));
  rerender(<ApplicabilityFactsPanel contractId="contract-2" canView />);

  expect(screen.queryByText("contract.data_type")).not.toBeInTheDocument();
  expect(screen.getByText("Loading applicability facts...")).toBeVisible();
  await waitFor(() => expect(getApplicabilityFacts).toHaveBeenLastCalledWith("contract-2"));

  await act(async () => oldRefresh.resolve([{ ...facts[0], value: "Stale refresh" }]));
  expect(screen.queryByText("Stale refresh")).not.toBeInTheDocument();
  expect(screen.getByText("Loading applicability facts...")).toBeVisible();

  await act(async () => newContract.resolve(newFacts));
  expect(await screen.findByText("contract.agency")).toBeVisible();
  expect(screen.getByText("USAF")).toBeVisible();
});

it("shows the empty state", async () => {
  vi.mocked(getApplicabilityFacts).mockResolvedValueOnce([]);
  render(<ApplicabilityFactsPanel contractId="contract-1" canView />);

  expect(await screen.findByText("No derived applicability facts are available for this contract.")).toBeVisible();
});

it("shows load failures", async () => {
  vi.mocked(getApplicabilityFacts).mockRejectedValueOnce(new Error("Facts unavailable"));
  render(<ApplicabilityFactsPanel contractId="contract-1" canView />);

  expect(await screen.findByRole("alert")).toHaveTextContent("Facts unavailable");
  expect(screen.queryByRole("table")).not.toBeInTheDocument();
});

it("does not fetch or render the panel without permission", () => {
  render(<ApplicabilityFactsPanel contractId="contract-2" canView={false} />);

  expect(getApplicabilityFacts).not.toHaveBeenCalled();
  expect(screen.queryByRole("heading", { name: "Applicability facts" })).not.toBeInTheDocument();
});
