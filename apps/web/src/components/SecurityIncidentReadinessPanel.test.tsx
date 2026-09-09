import { render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { SecurityIncidentReadinessPanel } from "./SecurityIncidentReadinessPanel";

vi.mock("@/lib/api", () => ({
  getSecurityReviewReadiness: vi.fn(async () => null), getTechnicalReadiness: vi.fn(async () => null),
  getIncidentReadiness: vi.fn(async () => null), getSecurityIncidentReadinessHistory: vi.fn(async () => []),
  getSecurityIncidentReadinessEvidenceOptions: vi.fn(async () => [{ evidenceFileVersionId:"version-1", evidenceItemId:"evidence-1", versionNumber:2, title:"Backup restore execution", fileName:"restore.pdf", sha256Digest:"a".repeat(64), uploadedAt:"2026-09-08T12:00:00Z" }]),
  getCurrentUserAccess: vi.fn(async () => ({ canApproveCuiReadiness: false })),
  saveSecurityReviewReadiness: vi.fn(), approveSecurityReviewReadiness: vi.fn(),
  saveTechnicalReadiness: vi.fn(), approveTechnicalReadiness: vi.fn(), saveIncidentReadiness: vi.fn(), approveIncidentReadiness: vi.fn()
}));

describe("SecurityIncidentReadinessPanel", () => {
  beforeEach(() => vi.clearAllMocks());
  it("renders structured empty-state editors and keeps approval unavailable without platform permission", async () => {
    render(<SecurityIncidentReadinessPanel userId="22222222-2222-2222-2222-222222222222" />);
    await waitFor(() => expect(screen.getByRole("heading", { name: "Security review" })).toBeInTheDocument());
    expect(screen.getAllByRole("combobox")).toHaveLength(28);
    expect(screen.getByRole("heading", { name: "Technical control verification" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Incident readiness" })).toBeInTheDocument();
    expect(screen.getByLabelText("security contact")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Add follow-up" })).toBeInTheDocument();
    expect(screen.getAllByRole("option", { name: "Backup restore execution · restore.pdf v2" })).toHaveLength(7);
    expect(screen.queryByRole("button", { name: "Approve security review" })).not.toBeInTheDocument();
  });
});
