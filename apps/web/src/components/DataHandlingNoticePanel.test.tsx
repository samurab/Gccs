import { act, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, expect, test, vi } from "vitest";
import { DataHandlingNoticePanel } from "./DataHandlingNoticePanel";
import { acknowledgeDataHandlingNotice, getDataHandlingNoticeAcknowledgements, getPublishedDataHandlingNotice } from "@/lib/api";

vi.mock("@/lib/api", () => ({
  acknowledgeDataHandlingNotice: vi.fn(),
  getDataHandlingNoticeAcknowledgements: vi.fn(),
  getPublishedDataHandlingNotice: vi.fn()
}));

beforeEach(() => vi.resetAllMocks());

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
  render(<DataHandlingNoticePanel tenantId="synthetic-tenant" mode="NoCui" />);
  await screen.findByText("Data handling notice");
  fireEvent.click(screen.getByText("Current data handling notices"));
  fireEvent.click(screen.getByRole("checkbox"));
  expect(screen.getByRole("checkbox")).toBeChecked();

  vi.mocked(getPublishedDataHandlingNotice).mockResolvedValue({ ...notice, version: "renewed" });
  act(() => window.dispatchEvent(new CustomEvent("fedril:notice-required", { detail: { workflowContext: "ReportGeneration" } })));
  await screen.findByText("Version renewed · NoCui");
  await waitFor(() => expect(screen.getByRole("checkbox")).not.toBeChecked());
  expect(screen.getByRole("button", { name: "Acknowledge current notice" })).toBeDisabled();
  expect(acknowledgeDataHandlingNotice).not.toHaveBeenCalled();
  expect(screen.getByText("Current data handling notices").closest("details")).toHaveAttribute("open");
});
