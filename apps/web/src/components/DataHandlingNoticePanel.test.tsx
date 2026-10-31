import { act, cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { DataHandlingNoticePanel } from "./DataHandlingNoticePanel";
import { acknowledgeDataHandlingNotice, getDataHandlingNoticeAcknowledgements, getPublishedDataHandlingNotice } from "@/lib/api";

vi.mock("@/lib/api", () => ({
  acknowledgeDataHandlingNotice: vi.fn(),
  getDataHandlingNoticeAcknowledgements: vi.fn(),
  getPublishedDataHandlingNotice: vi.fn()
}));

beforeEach(() => vi.resetAllMocks());
afterEach(cleanup);

test("a renewal response reopens the notice and never carries forward consent", async () => {
  const notice = {
    noticeId: "no-cui-general", version: "old", mode: "NoCui", title: "Data handling notice",
    body: "Synthetic test notice", workflowContexts: ["EvidenceUpload", "ReportGeneration"],
    state: "Published", owner: "Test owner", reviewer: "Test reviewer", reviewedAt: "2026-06-18",
    effectiveAt: "2026-06-18", sourceReference: "test"
  };
  vi.mocked(getPublishedDataHandlingNotice).mockResolvedValue(notice);
  vi.mocked(getDataHandlingNoticeAcknowledgements).mockResolvedValue([]);
  vi.mocked(acknowledgeDataHandlingNotice).mockResolvedValue({ data: null, error: "Synthetic rejected acknowledgement" });
  render(<DataHandlingNoticePanel tenantId="synthetic-tenant" mode="NoCui" workflowContext="ReportGeneration" />);
  await screen.findByText("Data handling notice");
  fireEvent.click(screen.getByText("Report generation data handling notice"));
  fireEvent.click(screen.getByRole("checkbox"));
  expect(screen.getByRole("checkbox")).toBeChecked();

  vi.mocked(getPublishedDataHandlingNotice).mockResolvedValue({ ...notice, version: "renewed" });
  act(() => window.dispatchEvent(new CustomEvent("fedril:notice-required", { detail: { workflowContext: "ReportGeneration" } })));
  await screen.findByText("Version renewed · NoCui");
  await waitFor(() => expect(screen.getByRole("checkbox")).not.toBeChecked());
  expect(screen.getByRole("button", { name: "Acknowledge current notice" })).toBeDisabled();
  expect(acknowledgeDataHandlingNotice).not.toHaveBeenCalled();
  expect(screen.getByText("Report generation data handling notice").closest("details")).toHaveAttribute("open");
});

test("each placement loads only its fixed workflow and ignores unrelated precondition events", async () => {
  const notice = {
    noticeId: "no-cui-general", version: "current", mode: "NoCui", title: "Evidence notice",
    body: "Synthetic test notice", workflowContexts: ["EvidenceUpload"], state: "Published",
    owner: "Test owner", reviewer: "Test reviewer", reviewedAt: "2026-06-18",
    effectiveAt: "2026-06-18", sourceReference: "test"
  };
  vi.mocked(getPublishedDataHandlingNotice).mockResolvedValue(notice);
  vi.mocked(getDataHandlingNoticeAcknowledgements).mockResolvedValue([]);

  render(<DataHandlingNoticePanel tenantId="synthetic-tenant" mode="NoCui" workflowContext="EvidenceUpload" />);
  await screen.findByText("Evidence notice");
  expect(getPublishedDataHandlingNotice).toHaveBeenCalledWith("NoCui", "EvidenceUpload");
  expect(getDataHandlingNoticeAcknowledgements).toHaveBeenCalledWith("synthetic-tenant", "NoCui", "EvidenceUpload");

  act(() => window.dispatchEvent(new CustomEvent("fedril:notice-required", { detail: { workflowContext: "Support" } })));
  expect(screen.getByText("Evidence upload data handling notice").closest("details")).not.toHaveAttribute("open");
});

test("a notice for a different tenant mode is not presented for acknowledgement", async () => {
  vi.mocked(getPublishedDataHandlingNotice).mockResolvedValue({
    noticeId: "demo", version: "current", mode: "DemoSandbox", title: "Wrong mode",
    body: "Synthetic test notice", workflowContexts: ["Support"], state: "Published",
    owner: "Test owner", reviewer: "Test reviewer", reviewedAt: "2026-06-18",
    effectiveAt: "2026-06-18", sourceReference: "test"
  });
  vi.mocked(getDataHandlingNoticeAcknowledgements).mockResolvedValue([]);

  render(<DataHandlingNoticePanel tenantId="synthetic-tenant" mode="NoCui" workflowContext="Support" />);

  await screen.findByText("The current mode-specific notice could not be loaded. Refresh before continuing.");
  expect(screen.queryByRole("checkbox")).not.toBeInTheDocument();
});
