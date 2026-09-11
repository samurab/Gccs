import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { EsrsApplicabilityPanel } from "./EsrsApplicabilityPanel";

const { createMock, listMock, templatesMock, statusMock, updateMock } = vi.hoisted(() => ({
  createMock: vi.fn(), listMock: vi.fn(), templatesMock: vi.fn(), statusMock: vi.fn(), updateMock: vi.fn()
}));

vi.mock("@/lib/api", () => ({
  createEsrsApplicability: createMock,
  getContractEsrsApplicabilities: listMock,
  getEsrsScheduleTemplates: templatesMock,
  updateEsrsApplicability: updateMock,
  updateEsrsApplicabilityStatus: statusMock
}));

const contractId = "31131131-1131-3113-1131-3113113113bb";
const item = {
  id: "31131131-1131-3113-1131-311311311301", tenantId: "tenant", contractId, taskId: "task",
  contractType: "Prime contract", agency: "Department of Defense", subcontractingPlanType: "Individual",
  primeOrLowerTierRole: "Prime", reportType: "Isr" as const, periodStart: "2026-01-01", periodEnd: "2026-03-31",
  dueDate: "2026-04-30", sourceClause: "FAR 52.219-9", rationale: null, status: "Open" as const,
  ownerFunction: "Contracts", assignedToUserId: null, reviewedByUserId: "reviewer",
  reviewedAt: "2026-04-01T12:00:00Z", createdAt: "2026-04-01T12:00:00Z", updatedAt: null, isOverdue: true
};

describe("EsrsApplicabilityPanel", () => {
  afterEach(cleanup);
  beforeEach(() => {
    vi.clearAllMocks();
    listMock.mockResolvedValue([]);
    templatesMock.mockResolvedValue([{
      key: "isr-first-half", reportType: "Isr", periodStart: "2026-10-01", periodEnd: "2027-03-31",
      dueDate: "2027-04-30", sourceCitation: "FAR 52.219-9", sourceUrl: "https://www.acquisition.gov/far/52.219-9",
      guidance: "Suggested schedule; confirm against the governing plan."
    }]);
  });

  it("TC-31.1.1 creates a source-backed obligation from a reviewed schedule", async () => {
    createMock.mockResolvedValue({ data: item });
    const user = userEvent.setup();
    render(<EsrsApplicabilityPanel contractId={contractId} canManage />);
    await user.click(await screen.findByRole("button", { name: /use isr first half/i }));
    await user.type(screen.getByLabelText("Agency"), "Department of Defense");
    await user.click(screen.getByRole("button", { name: /activate sam.gov spr obligation/i }));
    await waitFor(() => expect(createMock).toHaveBeenCalledWith(contractId, expect.objectContaining({
      reportType: "Isr", periodStart: "2026-10-01", periodEnd: "2027-03-31", dueDate: "2027-04-30",
      sourceClause: "FAR 52.219-9"
    })));
    expect(await screen.findByText(/added to the compliance calendar/i)).toBeInTheDocument();
  });

  it("TC-31.1.2 and TC-31.1.4 displays persisted overdue SPR work and updates status", async () => {
    listMock.mockResolvedValue([item]);
    statusMock.mockResolvedValue({ data: { ...item, status: "Completed", isOverdue: false } });
    const user = userEvent.setup();
    render(<EsrsApplicabilityPanel contractId={contractId} canManage />);
    expect(await screen.findByText(/Open · Overdue/)).toBeInTheDocument();
    await user.selectOptions(screen.getByLabelText(/status for ISR/i), "Completed");
    await waitFor(() => expect(statusMock).toHaveBeenCalledWith(contractId, item.id, "Completed"));
    expect(screen.getByLabelText(/status for ISR/i)).toHaveValue("Completed");
  });

  it("fails closed to a read-only view without management permission", async () => {
    listMock.mockResolvedValue([item]);
    render(<EsrsApplicabilityPanel contractId={contractId} canManage={false} />);
    expect(await screen.findByText(/read-only access/i)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /activate/i })).not.toBeInTheDocument();
  });
});
