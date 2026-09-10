import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { SspSectionsPanel } from "./SspSectionsPanel";
import * as api from "@/lib/api";

vi.mock("@/lib/api", () => ({
  getSspSections: vi.fn(), createSspSection: vi.fn(), updateSspSection: vi.fn(), changeSspSectionStatus: vi.fn(),
  getSspNarratives: vi.fn(), generateSspNarrative: vi.fn(), editSspNarrative: vi.fn(),
  approveSspNarrative: vi.fn(), compareSspNarrative: vi.fn(), getSspExportPackages: vi.fn(), createSspExportPackage: vi.fn(),
  getSspExportPolicy: vi.fn(), updateSspExportPolicy: vi.fn()
}));
afterEach(cleanup);
beforeEach(() => {
  vi.clearAllMocks(); vi.mocked(api.getSspSections).mockResolvedValue([]); vi.mocked(api.getSspNarratives).mockResolvedValue([]);
  vi.mocked(api.getSspExportPackages).mockResolvedValue([]);
  vi.mocked(api.getSspExportPolicy).mockResolvedValue({ requireIndependentApproval: true, version: 0, updatedAt: null, updatedByUserId: null });
});

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
  expect(screen.getByText(/Do not paste CUI, classified information/)).toBeInTheDocument();
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

it("fails closed without ExportReports and generates an internal SSP review package from record IDs", async () => {
  const packageRecord = {
    id: "package-1", tenantId: "tenant-1", tenantName: "Tenant Alpha", generatedAt: "2026-09-10T12:00:00Z",
    packageVersion: "ssp-1", systemBoundary: "Boundary A", reviewer: "Security reviewer", format: "Both", languagePolicyVersion: "2026-09-10.1",
    disclaimer: "Draft SSP review package for human review only.", humanReadableReport: "Review package content",
    machineReadableMetadata: { draftOnly: true }, sections: [], includedEvidence: [], poamReferences: [],
    status: "InternalReview", externalShareApprovedByUserId: null, externalShareApprovedAt: null,
    externalShareApprovalReason: null, sharedByUserId: null, sharedAt: null, sharedRecipient: null, sharedPurpose: null,
    history: [{ id: "history-1", action: "Generated", actorUserId: "user-1", actorName: "owner@example.invalid", occurredAt: "2026-09-10T12:00:00Z", notes: null }]
  } as api.SspExportPackage;

  const denied = render(<SspSectionsPanel canManage canExport={false} />);
  await screen.findByText(/ExportReports permission is required/);
  expect(api.getSspExportPackages).not.toHaveBeenCalled();
  denied.unmount();

  vi.mocked(api.createSspExportPackage).mockResolvedValue({ data: packageRecord, error: null });
  vi.mocked(api.getSspExportPackages).mockResolvedValueOnce([]).mockResolvedValueOnce([packageRecord]);
  render(<SspSectionsPanel canManage canExport />);
  await screen.findByText("No SSP review packages exist for this tenant.");
  await userEvent.type(screen.getByLabelText("Package version"), "ssp-1");
  await userEvent.type(screen.getByLabelText("System boundary"), "Boundary A");
  await userEvent.type(screen.getByLabelText("Package reviewer"), "Security reviewer");
  await userEvent.type(screen.getByLabelText("Approved evidence IDs"), "10000000-0000-0000-0000-000000000001");
  await userEvent.type(screen.getByLabelText("POA&M item IDs"), "20000000-0000-0000-0000-000000000001");
  await userEvent.click(screen.getByRole("button", { name: "Generate internal review package" }));
  await waitFor(() => expect(api.createSspExportPackage).toHaveBeenCalledWith({
    packageVersion: "ssp-1", systemBoundary: "Boundary A", reviewer: "Security reviewer", format: "Both", externalShareRequested: false,
    evidenceItemIds: ["10000000-0000-0000-0000-000000000001"], poamItemIds: ["20000000-0000-0000-0000-000000000001"]
  }));
  expect(await screen.findByText(/External sharing requires separate approval/)).toBeInTheDocument();
  expect(screen.getByText("Review package content")).toBeInTheDocument();
});

it("shows record-only sharing posture and updates independent approval policy for tenant managers", async () => {
  vi.mocked(api.updateSspExportPolicy).mockResolvedValue({
    data: { requireIndependentApproval: false, version: 1, updatedAt: "2026-09-10T13:00:00Z", updatedByUserId: "user-2" }, error: null
  });
  render(<SspSectionsPanel canManage canExport canManageExportPolicy />);
  expect(await screen.findByText(/different authorized user must approve/)).toBeInTheDocument();
  expect(screen.getByText(/does not deliver the package/)).toBeInTheDocument();
  await userEvent.click(screen.getByRole("checkbox", { name: /Require independent approval/ }));
  await userEvent.type(screen.getByLabelText("Policy change reason"), "Tenant workflow exception");
  await userEvent.click(screen.getByRole("button", { name: "Save external-share policy" }));
  await waitFor(() => expect(api.updateSspExportPolicy).toHaveBeenCalledWith({
    requireIndependentApproval: false, expectedVersion: 0, reason: "Tenant workflow exception"
  }));
  expect(await screen.findByText(/policy updated and audit logged/)).toBeInTheDocument();
});

it("allows a tenant manager to administer export policy without package-export access", async () => {
  render(<SspSectionsPanel canManage={false} canExport={false} canManageExportPolicy />);

  expect(await screen.findByLabelText("SSP external-share policy")).toBeInTheDocument();
  expect(screen.getByRole("checkbox", { name: /Require independent approval/ })).toBeChecked();
  expect(screen.queryByRole("form", { name: "Generate SSP review package" })).not.toBeInTheDocument();
  expect(api.getSspExportPackages).not.toHaveBeenCalled();
  expect(api.getSspExportPolicy).toHaveBeenCalledTimes(1);
});
