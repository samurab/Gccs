import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { SspSectionsPanel } from "./SspSectionsPanel";
import * as api from "@/lib/api";

vi.mock("@/lib/api", () => ({
  getSspSections: vi.fn(), createSspSection: vi.fn(), updateSspSection: vi.fn(), changeSspSectionStatus: vi.fn(),
  getSspNarratives: vi.fn(), generateSspNarrative: vi.fn(), editSspNarrative: vi.fn(),
  approveSspNarrative: vi.fn(), compareSspNarrative: vi.fn()
}));
afterEach(cleanup);
beforeEach(() => { vi.clearAllMocks(); vi.mocked(api.getSspSections).mockResolvedValue([]); vi.mocked(api.getSspNarratives).mockResolvedValue([]); });

it("renders empty and permission-denied states and fails closed", async () => {
  render(<SspSectionsPanel canManage={false} />);
  await screen.findByText("No SSP sections exist for this tenant.");
  expect(screen.getByText(/ManageCmmc permission is required/)).toBeInTheDocument();
  expect(screen.getByRole("button", { name: "Create section" })).toBeDisabled();
});

it("creates a source-backed section without narrative content", async () => {
  vi.mocked(api.createSspSection).mockResolvedValue({ data: { id: "section" } as api.SspSection, error: null });
  render(<SspSectionsPanel canManage />); await screen.findByText("No SSP sections exist for this tenant.");
  await userEvent.type(screen.getByLabelText("Title"), "Authorization boundary");
  await userEvent.type(screen.getByLabelText("Owner"), "Security owner");
  await userEvent.type(screen.getByLabelText("Source name"), "NIST SP 800-171");
  await userEvent.type(screen.getByLabelText("Source URL"), "https://csrc.nist.gov/");
  await userEvent.type(screen.getByLabelText("Source reviewed"), "2026-09-01");
  await userEvent.click(screen.getByRole("button", { name: "Create section" }));
  await waitFor(() => expect(api.createSspSection).toHaveBeenCalledWith(expect.objectContaining({ title: "Authorization boundary", linkedRecords: [], sourceReferences: [expect.objectContaining({ source: "NIST SP 800-171" })] })));
});

it("shows load failures separately from an empty tenant", async () => {
  vi.mocked(api.getSspSections).mockRejectedValueOnce(new Error("SSP API unavailable"));
  render(<SspSectionsPanel canManage />);
  expect(await screen.findByRole("alert")).toHaveTextContent("SSP API unavailable");
  expect(screen.queryByText("No SSP sections exist for this tenant.")).not.toBeInTheDocument();
});

it("shows source-backed narrative drafts, guardrails, and comparison", async () => {
  const section = {
    id: "section-1", tenantId: "tenant-1", sectionType: "ControlImplementationNarratives", title: "Access control",
    owner: "Security", status: "Draft", reviewer: null, reviewDate: null, approvalRationale: null, isRequired: true,
    version: 1, linkedRecords: [], sourceReferences: [{ source: "NIST", sourceUrl: "https://csrc.nist.gov", lastReviewedAt: "2026-09-01" }],
    history: [], createdAt: "2026-09-09T00:00:00Z", updatedAt: "2026-09-09T00:00:00Z"
  } as api.SspSection;
  const narrative = {
    id: "narrative-1", tenantId: "tenant-1", sectionId: section.id, generatedText: "MFA is enforced.", editedText: null,
    approvedText: null, status: "Draft", aiAssisted: false, draftOnly: true, reviewerNotes: "Verify scope.",
    reviewerUserId: null, reviewer: null, reviewDate: null, version: 1,
    classification: { classification: "Unclassified", source: "SystemSuggested", confidence: null, reviewedByUserId: null, reviewedAt: null, reason: null, isApprovedDemoContent: false },
    sourceRecords: [{ sourceType: "Evidence", recordId: "evidence-1", label: "MFA evidence", summary: "MFA is enforced.", sourceUrl: "/evidence?item=evidence-1", fingerprint: "v1", classification: "Unclassified" }],
    createdAt: "2026-09-09T00:00:00Z", updatedAt: "2026-09-09T00:00:00Z"
  } as api.SspNarrative;
  vi.mocked(api.getSspSections).mockResolvedValue([section]);
  vi.mocked(api.getSspNarratives).mockResolvedValue([narrative]);
  vi.mocked(api.generateSspNarrative).mockResolvedValue({ data: narrative, error: null });
  vi.mocked(api.compareSspNarrative).mockResolvedValue({
    sectionId: section.id, approvedNarrativeId: null, draftNarrativeId: narrative.id, currentApprovedText: null,
    proposedText: "MFA is enforced.", currentApprovedReviewerUserId: null, currentApprovedReviewer: null,
    currentApprovedReviewDate: null, proposedReviewerNotes: "Verify scope.", proposedSources: narrative.sourceRecords, currentApprovedSources: []
  });

  render(<SspSectionsPanel canManage />);
  await userEvent.click(await screen.findByRole("button", { name: "Access control" }));
  expect(await screen.findAllByText("Draft—human review required")).not.toHaveLength(0);
  expect(screen.getByRole("note")).toHaveTextContent("Do not paste CUI");
  expect(screen.getByRole("link", { name: "MFA evidence" })).toHaveAttribute("href", "/evidence?item=evidence-1");
  await userEvent.click(screen.getByRole("button", { name: "Compare with current approved" }));
  expect(await screen.findByRole("heading", { name: "Current approved narrative" })).toBeInTheDocument();
  expect(screen.getByText("No approved narrative exists.")).toBeInTheDocument();

  await userEvent.type(screen.getByLabelText("Approved source record ID"), "evidence-2");
  await userEvent.click(screen.getByRole("button", { name: "Add another source" }));
  await userEvent.selectOptions(screen.getByLabelText("Source type"), "GeneratedPolicy");
  await userEvent.type(screen.getByLabelText("Approved source record ID"), "policy-1");
  await userEvent.click(screen.getByRole("button", { name: "Generate draft" }));
  await waitFor(() => expect(api.generateSspNarrative).toHaveBeenCalledWith(section.id, { sources: [
    { sourceType: "Evidence", recordId: "evidence-2" },
    { sourceType: "GeneratedPolicy", recordId: "policy-1" }
  ] }));
});
