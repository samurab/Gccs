import { render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { SecurityIncidentReadinessPanel } from "./SecurityIncidentReadinessPanel";

vi.mock("@/lib/api", () => ({
  getSecurityReviewReadiness: vi.fn(async () => null), getTechnicalReadiness: vi.fn(async () => null),
  getIncidentReadiness: vi.fn(async () => null), getSecurityIncidentReadinessHistory: vi.fn(async () => []),
  getCurrentUserAccess: vi.fn(async () => ({ canApproveCuiReadiness: false })),
  saveSecurityReviewReadiness: vi.fn(), approveSecurityReviewReadiness: vi.fn(),
  saveTechnicalReadiness: vi.fn(), approveTechnicalReadiness: vi.fn(), saveIncidentReadiness: vi.fn(), approveIncidentReadiness: vi.fn()
}));

describe("SecurityIncidentReadinessPanel", () => {
  beforeEach(() => vi.clearAllMocks());
  it("renders structured empty-state editors and keeps approval unavailable without platform permission", async () => {
    render(<SecurityIncidentReadinessPanel userId="22222222-2222-2222-2222-222222222222" />);
    await waitFor(() => expect(screen.getByRole("heading", { name: "Security review" })).toBeInTheDocument());
    expect(screen.getAllByRole("combobox")).toHaveLength(14);
    expect(screen.getByRole("heading", { name: "Technical control verification" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Incident readiness" })).toBeInTheDocument();
    expect(screen.getByLabelText("security contact")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Add follow-up" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Approve security review" })).not.toBeInTheDocument();
  });
});
