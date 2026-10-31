import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { DemoRequestDetailsPage } from "./DemoRequestDetailsPage";
import * as api from "./demoRequestApi";

vi.mock("./demoRequestApi", async importOriginal => ({
  ...(await importOriginal<typeof api>()),
  getDemoFollowUpContext: vi.fn(),
  submitDemoFollowUpResponse: vi.fn(),
}));

beforeEach(() => {
  window.history.replaceState(null, "", "/demo-request-details#token=test-token");
  vi.mocked(api.getDemoFollowUpContext).mockResolvedValue({
    data: {
      status: "Pending",
      requestedAt: "2026-08-12T12:00:00Z",
      expiresAt: "2099-08-15T12:00:00Z",
      noCuiNoticeVersion: "demo-follow-up-no-cui-2026-08-12",
    },
    error: null,
    fieldErrors: null,
  });
});

afterEach(() => {
  cleanup();
  vi.resetAllMocks();
});

describe("DemoRequestDetailsPage", () => {
  it("submits bounded structured details with the No-CUI acknowledgement and removes the token", async () => {
    vi.mocked(api.submitDemoFollowUpResponse).mockResolvedValue({
      data: { status: "Received", submittedAt: "2026-08-12T13:00:00Z" },
      error: null,
      fieldErrors: null,
    });
    render(<DemoRequestDetailsPage />);

    expect(await screen.findByRole("heading", { name: /help us tailor/i })).toBeInTheDocument();
    fireEvent.click(screen.getByLabelText("Evidence organization"));
    fireEvent.click(screen.getByLabelText("CMMC readiness workflows"));
    fireEvent.change(screen.getByLabelText(/what should the demonstration help/i), { target: { value: "Show an evidence workflow." } });
    fireEvent.change(screen.getByLabelText(/what process or readiness challenges/i), { target: { value: "Evidence is fragmented." } });
    fireEvent.change(screen.getByLabelText(/how do you manage this work today/i), { target: { value: "Spreadsheets" } });
    fireEvent.click(screen.getByLabelText(/i confirm this response contains no cui/i));
    fireEvent.click(screen.getByRole("button", { name: /send demo details/i }));

    await waitFor(() => expect(api.submitDemoFollowUpResponse).toHaveBeenCalledWith(expect.objectContaining({
      token: "test-token",
      workflows: ["EvidenceManagement", "CmmcReadiness"],
      goals: "Show an evidence workflow.",
      challenges: "Evidence is fragmented.",
      currentProcess: "Spreadsheets",
      noCuiConfirmed: true,
    })));
    expect(await screen.findByRole("heading", { name: /details received/i })).toBeInTheDocument();
    expect(window.location.hash).toBe("");
  });

  it("does not render the form for an expired single-use link", async () => {
    vi.mocked(api.getDemoFollowUpContext).mockResolvedValue({
      data: {
        status: "Expired",
        requestedAt: "2026-08-10T12:00:00Z",
        expiresAt: "2026-08-11T12:00:00Z",
        noCuiNoticeVersion: "demo-follow-up-no-cui-2026-08-12",
      },
      error: null,
      fieldErrors: null,
    });

    render(<DemoRequestDetailsPage />);

    expect(await screen.findByText(/link has expired/i)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /send demo details/i })).not.toBeInTheDocument();
  });

  it("keeps the form available when the API returns correctable field validation", async () => {
    vi.mocked(api.submitDemoFollowUpResponse).mockResolvedValue({
      data: null,
      error: "The demo follow-up response is invalid.",
      fieldErrors: { challenges: ["This field is required."] },
    });
    render(<DemoRequestDetailsPage />);
    await screen.findByRole("button", { name: /send demo details/i });
    fireEvent.click(screen.getByLabelText("Evidence organization"));
    fireEvent.change(screen.getByLabelText(/what should the demonstration help/i), { target: { value: "Show an evidence workflow." } });
    fireEvent.change(screen.getByLabelText(/what process or readiness challenges/i), { target: { value: "Temporary" } });
    fireEvent.click(screen.getByLabelText(/i confirm this response contains no cui/i));
    fireEvent.click(screen.getByRole("button", { name: /send demo details/i }));

    expect(await screen.findByText("This field is required.")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /send demo details/i })).toBeInTheDocument();
  });
});
