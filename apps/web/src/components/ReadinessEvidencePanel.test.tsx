import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { ReadinessEvidencePanel, ReadinessItemEditor } from "./ReadinessEvidencePanel";
import * as api from "@/lib/api";

vi.mock("@/lib/api", () => ({ getCurrentUserAccess: vi.fn(), getReadinessSources: vi.fn(), getReadinessEvidence: vi.fn(),
  getReadinessNotice: vi.fn(), acknowledgeReadinessNotice: vi.fn(), recordReadinessEvidence: vi.fn() }));
afterEach(cleanup);
beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(api.getCurrentUserAccess).mockResolvedValue({ tenantId: "t", userId: "u", userEmail: "synthetic@example.invalid", roles: [], permissions: ["ManageTenant"], rolePermissionMatrix: {} });
  vi.mocked(api.getReadinessSources).mockResolvedValue([]);
  vi.mocked(api.getReadinessEvidence).mockResolvedValue([]);
  vi.mocked(api.getReadinessNotice).mockResolvedValue({ noticeId: "proposed", version: "v1", title: "Proposed notice", body: "Synthetic notice body", mode: "CuiReady" } as api.DataHandlingNotice);
});

it("fails closed when platform approval permission is absent", async () => {
  render(<ReadinessEvidencePanel>{(_, canApprove) => <button disabled={!canApprove}>Final approval</button>}</ReadinessEvidencePanel>);
  await screen.findByText(/Final approval and recording readiness evidence require/);
  expect(screen.getByRole("button", { name: "Final approval" })).toBeDisabled();
  expect(screen.queryByRole("button", { name: "Record reviewed version" })).not.toBeInTheDocument();
});

it("distinguishes loading failures from empty sources and supports retry", async () => {
  vi.mocked(api.getReadinessSources).mockRejectedValueOnce(new Error("Evidence API unavailable"));
  render(<ReadinessEvidencePanel>{() => null}</ReadinessEvidencePanel>);
  expect(await screen.findByRole("alert")).toHaveTextContent("Evidence API unavailable");
  await userEvent.click(screen.getByRole("button", { name: "Refresh supporting records" }));
  await screen.findByText(/No current supporting records are available/);
  expect(screen.queryByRole("alert")).not.toBeInTheDocument();
});

it("requires explicit proposed-mode consent without changing mode", async () => {
  vi.mocked(api.acknowledgeReadinessNotice).mockResolvedValue({ data: {} as api.DataHandlingNoticeAcknowledgement, error: null });
  render(<ReadinessEvidencePanel>{() => null}</ReadinessEvidencePanel>);
  await screen.findByText(/Proposed CuiReady onboarding notice/);
  await userEvent.click(screen.getByText(/Proposed CuiReady onboarding notice/));
  const button = screen.getByRole("button", { name: "Acknowledge proposed-mode notice" });
  expect(button).toBeDisabled();
  await userEvent.click(screen.getByLabelText("I acknowledge this proposed-mode notice."));
  await userEvent.click(button);
  await waitFor(() => expect(api.acknowledgeReadinessNotice).toHaveBeenCalledWith({ mode: "CuiReady", workflowContext: "Onboarding", noticeId: "proposed", noticeVersion: "v1", acknowledged: true }));
  await screen.findByText("Readiness notice acknowledged. Tenant mode is unchanged.");
});

it("requires a real current link and never manufactures evidence URLs", async () => {
  const save = vi.fn();
  const item = { id: "item", itemKey: "security-review", notes: null, owner: null, evidenceLink: null } as api.CuiReadyApprovalChecklistItem;
  const { rerender } = render(<ReadinessItemEditor item={item} sources={[]} userId="actor" disabled={false} onSave={save} />);
  expect(screen.getByRole("button", { name: "Save reviewed item" })).toBeDisabled();
  rerender(<ReadinessItemEditor item={item} sources={[{ id: "source", kind: "security-review", version: "3", title: "Security review v3" }]} userId="actor" disabled={false} onSave={save} />);
  await userEvent.type(screen.getByLabelText("Owner"), "Reviewer");
  await userEvent.type(screen.getByLabelText("Review notes"), "Verified source");
  await userEvent.selectOptions(screen.getByLabelText("Current supporting record"), "source");
  await userEvent.click(screen.getByRole("button", { name: "Save reviewed item" }));
  expect(save).toHaveBeenCalledWith(expect.objectContaining({ evidenceLink: null, supportingRecordId: "source", supportingVersion: "3", reviewerUserId: "actor" }));
});
