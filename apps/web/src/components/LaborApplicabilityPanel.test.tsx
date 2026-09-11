import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { LaborApplicabilityPanel } from "./LaborApplicabilityPanel";
import type { ContractClause, EvidenceMetadata } from "@/lib/api";

const mocks = vi.hoisted(() => ({ create: vi.fn(), list: vi.fn(), status: vi.fn(), update: vi.fn(), upload: vi.fn() }));
vi.mock("@/lib/api", () => ({
  createLaborApplicability: mocks.create, getContractLaborApplicabilities: mocks.list,
  updateLaborApplicability: mocks.update, updateLaborApplicabilityStatus: mocks.status,
  uploadLaborWageDetermination: mocks.upload
}));

const contractId = "32132132-2132-1321-3213-2132132132bb";
const item = {
  id: "labor-1", tenantId: "tenant", contractId, taskId: null, scaApplicable: true, dbaApplicable: false,
  otherFarPart22Obligations: null, placeOfPerformance: "Norfolk, VA", contractPeriodStart: "2026-01-01",
  contractPeriodEnd: "2026-12-31", wageDeterminationReference: "WD-2015-4341 Rev 24",
  wageDeterminationEvidenceItemId: "evidence-1", sourceContractClauseId: "clause-1", sourceClause: "FAR 52.222-41",
  rationale: "Contract review", ownerFunction: "Contracts/HR", status: "Draft" as const, reviewStatus: "PendingReview" as const,
  reviewNotes: "Pending HR review", reviewedByUserId: null, reviewedAt: null, reviewTask: null,
  createdAt: "2026-01-01T00:00:00Z", updatedAt: null, laborStandard: "SCA"
};
const clauses = [{ id: "clause-1", contractId, clauseNumber: "FAR 52.222-41", title: "Service Contract Labor Standards" }] as ContractClause[];
const evidence = [{ id: "evidence-1", title: "Wage determination", contractIds: [contractId] }] as EvidenceMetadata[];

describe("LaborApplicabilityPanel", () => {
  afterEach(cleanup);
  beforeEach(() => {
    vi.clearAllMocks(); mocks.list.mockResolvedValue([]);
  });

  it("TC-32.1.1 records explicit SCA applicability with source, period, location, and wage reference", async () => {
    mocks.create.mockResolvedValue({ data: item }); const user = userEvent.setup();
    render(<LaborApplicabilityPanel contractId={contractId} clauses={clauses} evidence={evidence} canManage canUpload />);
    await user.click(await screen.findByLabelText("SCA applies"));
    await user.type(screen.getByLabelText("Place of performance"), "Norfolk, VA");
    await user.type(screen.getByLabelText("Contract period start"), "2026-01-01");
    await user.type(screen.getByLabelText("Contract period end"), "2026-12-31");
    await user.type(screen.getByLabelText("Wage determination reference"), "WD-2015-4341 Rev 24");
    await user.selectOptions(screen.getByLabelText("Attached source clause"), "clause-1");
    await user.selectOptions(screen.getByLabelText("Wage determination evidence"), "evidence-1");
    await user.click(screen.getByRole("button", { name: "Record draft" }));
    await waitFor(() => expect(mocks.create).toHaveBeenCalledWith(contractId, expect.objectContaining({
      scaApplicable: true, sourceContractClauseId: "clause-1", sourceClause: "FAR 52.222-41",
      wageDeterminationEvidenceItemId: "evidence-1"
    })));
    expect(await screen.findByText(/recorded as a draft/i)).toBeInTheDocument();
  });

  it("TC-32.1.3 surfaces source-backed activation validation and TC-32.1.4 task synchronization", async () => {
    mocks.list.mockResolvedValue([item]); mocks.status.mockResolvedValueOnce({ error: "Labor obligation activation requires an attached source clause, source citation, or documented rationale." });
    const user = userEvent.setup(); render(<LaborApplicabilityPanel contractId={contractId} clauses={clauses} evidence={evidence} canManage canUpload />);
    await user.click(await screen.findByRole("button", { name: "Activate" }));
    expect(await screen.findByRole("alert")).toHaveTextContent(/requires an attached source clause/i);
    mocks.status.mockResolvedValue({ data: { ...item, status: "Active", taskId: "task-1", reviewTask: { id: "task-1", tenantId: "tenant", contractId, title: "Review", description: "Review", status: "WaitingForReview", dueAt: "2026-12-31" } } });
    await user.click(screen.getByRole("button", { name: "Activate" }));
    expect(await screen.findByText(/task synchronized/i)).toBeInTheDocument();
    expect(screen.getByText(/Task: WaitingForReview/i)).toBeInTheDocument();
  });

  it("TC-32.1.2 disables upload without permission or acknowledgement", async () => {
    mocks.list.mockResolvedValue([item]); render(<LaborApplicabilityPanel contractId={contractId} clauses={clauses} evidence={evidence} canManage canUpload={false} />);
    expect(await screen.findByText(/requires evidence-management permission/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Upload wage determination" })).toBeDisabled();
  });

  it("fails closed to read-only applicability controls", async () => {
    mocks.list.mockResolvedValue([item]); render(<LaborApplicabilityPanel contractId={contractId} clauses={clauses} evidence={evidence} canManage={false} canUpload={false} />);
    expect(await screen.findByText(/read-only access/i)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Activate" })).not.toBeInTheDocument();
  });
});
