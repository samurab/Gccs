import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { SprReportPackagesPanel } from "./SprReportPackagesPanel";

const mocks = vi.hoisted(() => ({ packages: vi.fn(), capability: vi.fn(), evidence: vi.fn(), receipts: vi.fn(),
  create: vi.fn(), review: vi.fn(), record: vi.fn(), download: vi.fn() }));
vi.mock("@/lib/api", () => ({
  getSprReportPackages: mocks.packages, getSprSubmissionCapability: mocks.capability, getEvidenceItems: mocks.evidence,
  getSprManualSubmissionReceipts: mocks.receipts, createSprReportPackage: mocks.create,
  reviewSprReportPackage: mocks.review, createSprManualSubmissionReceipt: mocks.record, downloadSprReportPackage: mocks.download
}));

const contractId = "31331331-1331-3313-3133-1331331331bb";
const draft = { id: "package-1", tenantId: "tenant", contractId, reportType: "Isr" as const,
  periodStart: "2026-01-01", periodEnd: "2026-03-31", status: "Draft" as const, version: 1,
  notSubmittedDisclaimer: "FeDril has not submitted this report to SAM.gov.", reviewerName: null, reviewerUserId: null,
  approvedAt: null, reviewNotes: null, generatedAt: "2026-04-01T12:00:00Z", updatedAt: null,
  snapshot: { contractId, reportType: "Isr" as const, periodStart: "2026-01-01", periodEnd: "2026-03-31",
    rowCount: 1, totalSpend: 12500, spendSummaries: [], evidenceReferences: [{ rowId: "row", evidenceItemId: "evidence-1" }],
    exceptions: [], schemaProfiles: [{ id: "schema", version: "1.0", sourceUrl: "https://example.test", definitionSha256: "a".repeat(64) }] } };
const priorReceipt = { id: "receipt-prior", tenantId: "tenant", packageId: draft.id,
  submittedAt: "2026-04-02T12:00:00Z", confirmationReference: "SAM-ORIGINAL", outcome: "Submitted" as const,
  notes: null, evidenceItemId: "evidence-1", supersedesReceiptId: null, recordedByUserId: "user",
  recordedAt: "2026-04-02T12:01:00Z" };

describe("SprReportPackagesPanel", () => {
  afterEach(cleanup);
  beforeEach(() => {
    vi.clearAllMocks(); mocks.packages.mockResolvedValue([]); mocks.capability.mockResolvedValue({ enabled: false, reason: "No provider configured." });
    mocks.evidence.mockResolvedValue([{ id: "evidence-1", title: "SAM confirmation", contractIds: [contractId] }]);
    mocks.receipts.mockResolvedValue([]);
  });

  it("generates an immutable preparation snapshot and displays the no-submission posture", async () => {
    mocks.create.mockResolvedValue({ data: draft, error: null }); const user = userEvent.setup();
    render(<SprReportPackagesPanel contractId={contractId} canManage canExport />);
    expect(await screen.findByText(/Direct submission: unavailable/i)).toBeInTheDocument();
    await user.type(screen.getByLabelText("Period start"), "2026-01-01");
    await user.type(screen.getByLabelText("Period end"), "2026-03-31");
    await user.click(screen.getByRole("button", { name: "Generate package" }));
    await waitFor(() => expect(mocks.create).toHaveBeenCalledWith(contractId, "Isr", "2026-01-01", "2026-03-31"));
    expect(await screen.findByText(/immutable SPR preparation snapshot generated/i)).toBeInTheDocument();
    expect(screen.getByText(/has not submitted this report/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Begin review" })).toBeInTheDocument();
  });

  it("reviews and approves a package before recording a user-reported external receipt", async () => {
    mocks.packages.mockResolvedValue([draft]);
    const inReview = { ...draft, status: "InReview" as const, reviewerName: "Avery" };
    const approved = { ...draft, status: "Approved" as const, reviewerName: "Avery", approvedAt: "2026-04-02T12:00:00Z" };
    mocks.review.mockResolvedValueOnce({ data: inReview, error: null }).mockResolvedValueOnce({ data: approved, error: null });
    mocks.record.mockResolvedValue({ data: { id: "receipt-1", tenantId: "tenant", packageId: draft.id,
      submittedAt: "2026-04-02T12:00:00Z", confirmationReference: "SAM-123", outcome: "Submitted", notes: null,
      evidenceItemId: "evidence-1", supersedesReceiptId: null, recordedByUserId: "user", recordedAt: "2026-04-02T12:01:00Z" }, error: null });
    const user = userEvent.setup(); render(<SprReportPackagesPanel contractId={contractId} canManage canExport />);
    await user.type(await screen.findByLabelText("Reviewer display name"), "Avery");
    expect(screen.queryByRole("button", { name: "Approve package" })).not.toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Begin review" }));
    await waitFor(() => expect(mocks.review).toHaveBeenCalledWith(draft.id, "begin-review", "Avery", expect.any(String)));
    await user.click(screen.getByRole("button", { name: "Approve package" }));
    await waitFor(() => expect(mocks.review).toHaveBeenCalledWith(draft.id, "approve", "Avery", expect.any(String)));
    expect(screen.getByText("Reviewer: Avery")).toBeInTheDocument();
    expect(screen.getByText(/Approved:/)).toBeInTheDocument();
    await user.type(await screen.findByLabelText("Confirmation reference"), "SAM-123");
    await user.selectOptions(screen.getByLabelText("Supporting evidence"), "evidence-1");
    await user.click(screen.getByRole("button", { name: "Record external receipt" }));
    await waitFor(() => expect(mocks.record).toHaveBeenCalledWith(draft.id, expect.objectContaining({ confirmationReference: "SAM-123", evidenceItemId: "evidence-1" })));
    expect(await screen.findByText(/did not verify or perform the submission/i)).toBeInTheDocument();
  });

  it("fails closed to read-only and no-export controls", async () => {
    mocks.packages.mockResolvedValue([draft]);
    render(<SprReportPackagesPanel contractId={contractId} canManage={false} canExport={false} />);
    expect(await screen.findByText(/read-only access/i)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Approve package" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Export HTML" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Generate package" })).not.toBeInTheDocument();
  });

  it("records a correction as an append-only receipt linked to the prior receipt", async () => {
    const approved = { ...draft, status: "Approved" as const, reviewerName: "Avery", approvedAt: "2026-04-02T12:00:00Z" };
    mocks.packages.mockResolvedValue([approved]); mocks.receipts.mockResolvedValue([priorReceipt]);
    mocks.record.mockResolvedValue({ data: { ...priorReceipt, id: "receipt-correction", confirmationReference: "SAM-CORRECTED",
      outcome: "Corrected", notes: "Corrected amount in SAM.gov.", supersedesReceiptId: priorReceipt.id }, error: null });
    const user = userEvent.setup();
    render(<SprReportPackagesPanel contractId={contractId} canManage canExport />);

    await user.selectOptions(await screen.findByLabelText("Outcome"), "Corrected");
    await user.selectOptions(screen.getByLabelText("Receipt being corrected"), priorReceipt.id);
    await user.type(screen.getByLabelText("Confirmation reference"), "SAM-CORRECTED");
    await user.type(screen.getByLabelText("Notes"), "Corrected amount in SAM.gov.");
    await user.click(screen.getByRole("button", { name: "Record external receipt" }));

    await waitFor(() => expect(mocks.record).toHaveBeenCalledWith(draft.id, expect.objectContaining({
      confirmationReference: "SAM-CORRECTED", outcome: "Corrected", supersedesReceiptId: priorReceipt.id
    })));
    const packageHistory = within(screen.getByRole("article", { name: "SPR package version 1" }));
    expect(packageHistory.getByText(/SAM-CORRECTED · Corrected/)).toBeInTheDocument();
    expect(packageHistory.getByText(/SAM-ORIGINAL · Submitted/)).toBeInTheDocument();
  });

  it("shows a fail-closed error state when package history cannot be loaded", async () => {
    mocks.packages.mockRejectedValue(new Error("network unavailable"));
    render(<SprReportPackagesPanel contractId={contractId} canManage canExport />);

    expect(await screen.findByRole("alert")).toHaveTextContent("SPR preparation packages could not be loaded.");
    expect(screen.queryByRole("button", { name: "Generate package" })).not.toBeInTheDocument();
  });
});
