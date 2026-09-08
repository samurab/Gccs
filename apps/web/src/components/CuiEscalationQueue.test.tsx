import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { CuiEscalationQueue } from "./CuiEscalationQueue";
import { getCuiSupportEscalationReport, getCuiSupportEscalations } from "@/lib/api";

vi.mock("@/lib/api", () => ({
  getCuiSupportEscalations: vi.fn(), getCuiSupportEscalationReport: vi.fn(),
  updateCuiSupportEscalation: vi.fn(), changeCuiSupportEscalationStatus: vi.fn(), resolveCuiSupportEscalation: vi.fn()
}));

beforeEach(() => {
  vi.mocked(getCuiSupportEscalations).mockResolvedValue([]);
  vi.mocked(getCuiSupportEscalationReport).mockResolvedValue({ openCount: 0, resolvedCount: 0, overdueCount: 0, byStatus: {}, bySeverity: {} });
});
afterEach(cleanup);

it("does not request or render the restricted queue without tenant management permission", () => {
  render(<CuiEscalationQueue tenantId="tenant-1" permissions={["ViewEvidence"]} />);
  expect(screen.queryByText("CUI escalation work queue")).not.toBeInTheDocument();
  expect(getCuiSupportEscalations).not.toHaveBeenCalled();
});

it("shows restricted escalation reporting to tenant managers", async () => {
  vi.mocked(getCuiSupportEscalationReport).mockResolvedValue({ openCount: 2, resolvedCount: 3, overdueCount: 1, byStatus: {}, bySeverity: {} });
  render(<CuiEscalationQueue tenantId="tenant-1" permissions={["ManageTenant"]} />);
  await userEvent.click(screen.getByText("CUI escalation work queue"));
  expect(await screen.findByText("Open 2 · resolved 3 · SLA overdue 1")).toBeVisible();
  expect(screen.getByText("No escalations are recorded.")).toBeVisible();
});
