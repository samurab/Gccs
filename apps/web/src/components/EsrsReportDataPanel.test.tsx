import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { EsrsReportDataPanel } from "./EsrsReportDataPanel";

const mocks = vi.hoisted(() => ({ list: vi.fn(), subcontractors: vi.fn(), evidence: vi.fn(), create: vi.fn(),
  update: vi.fn(), review: vi.fn(), importCsv: vi.fn(), template: vi.fn(), schema: vi.fn() }));
vi.mock("@/lib/api", () => ({
  getContractSubcontractingPlanReportData: mocks.list, getSubcontractors: mocks.subcontractors, getEvidenceItems: mocks.evidence,
  createContractSubcontractingPlanReportData: mocks.create, updateContractSubcontractingPlanReportData: mocks.update,
  reviewContractSubcontractingPlanReportData: mocks.review, importSubcontractingPlanReportDataCsv: mocks.importCsv,
  downloadSubcontractingPlanReportDataTemplate: mocks.template, getCurrentSprSchemaProfile: mocks.schema
}));

const contractId = "31231231-1231-2312-3123-1231231231bb";
const subcontractorId = "31231231-1231-2312-3123-1231231231cc";
const contractNumber = "FA-TEST-312";
const companyUei = "TESTUEI12345";
const row = { id: "row-1", tenantId: "tenant", contractId, subcontractorId, reportType: "Isr" as const,
  reportPeriodStart: "2026-01-01", reportPeriodEnd: "2026-03-31", rowPeriodStart: "2026-01-01", rowPeriodEnd: "2026-03-31",
  socioeconomicCategory: "Small Disadvantaged Business", planCategory: "Direct subcontract spend", amount: 12500,
  supportingEvidenceItemIds: ["evidence-1"], sourceReference: "FAR 52.219-9", reviewStatus: "Draft" as const,
  reportingRole: "PrimeContractor" as const, reportingFiscalYear: 2026, reportingPeriod: "March31" as const,
  reportingEntityUei: companyUei, primeContractPiid: contractNumber, subcontractNumber: null,
  sprEligibilityConfirmed: true, sprEligibilityBasis: "Qualifying individual plan", sprReadinessStatus: "Ready" as const,
  sprSchemaProfileId: "gsa-spr-fdd-2026-03-06", sprSchemaVersion: "1.0", sprSchemaSourceUrl: "https://example.test",
  sprSchemaDefinitionSha256: "a".repeat(64), sprReadinessBlockers: [],
  reviewedByUserId: null, reviewedAt: null, reviewerNotes: null, version: 1,
  createdAt: "2026-04-01T12:00:00Z", updatedAt: null, isPackageEligible: false };

describe("EsrsReportDataPanel", () => {
  afterEach(cleanup);
  beforeEach(() => {
    vi.clearAllMocks(); mocks.list.mockResolvedValue([]);
    mocks.subcontractors.mockResolvedValue([{ id: subcontractorId, name: "Atlas Subcontracting", contractIds: [contractId] }]);
    mocks.evidence.mockResolvedValue([{ id: "evidence-1", title: "Paid invoice", contractIds: [contractId] }]);
    mocks.schema.mockResolvedValue({ version: "1.0", priorFiscalYearsAllowed: 9,
      categories: ["Small Disadvantaged Business (SDB)"] });
  });

  it("TC-31.2.1 and TC-31.2.3 creates a linked draft row", async () => {
    mocks.create.mockResolvedValue({ data: row, error: null }); const user = userEvent.setup();
    render(<EsrsReportDataPanel contractId={contractId} contractNumber={contractNumber} companyUei={companyUei} canManage />);
    await user.selectOptions(await screen.findByLabelText("Subcontractor"), subcontractorId);
    await user.type(screen.getByLabelText("Report period start"), "2026-01-01");
    await user.type(screen.getByLabelText("Report period end"), "2026-03-31");
    await user.type(screen.getByLabelText("Row period start"), "2026-01-01");
    await user.type(screen.getByLabelText("Row period end"), "2026-03-31");
    await user.selectOptions(screen.getByLabelText("Socioeconomic category"), "Small Disadvantaged Business (SDB)");
    await user.type(screen.getByLabelText("Plan category"), "Direct subcontract spend");
    await user.clear(screen.getByLabelText("Amount (whole dollars)")); await user.type(screen.getByLabelText("Amount (whole dollars)"), "12500");
    await user.selectOptions(screen.getByLabelText("Supporting evidence"), "evidence-1");
    await user.type(screen.getByLabelText("Source reference"), "FAR 52.219-9");
    await user.type(screen.getByLabelText("SPR eligibility basis"), "Qualifying individual plan");
    await user.click(screen.getByLabelText(/confirmed the external SAM.gov SPR eligibility basis/i));
    await user.click(screen.getByRole("button", { name: "Create report data row" }));
    await waitFor(() => expect(mocks.create).toHaveBeenCalledWith(contractId, expect.objectContaining({
      subcontractorId, amount: 12500, supportingEvidenceItemIds: ["evidence-1"], reportingEntityUei: companyUei,
      primeContractPiid: contractNumber, sprEligibilityConfirmed: true
    })));
    expect(await screen.findByText(/created as a draft/i)).toBeInTheDocument();
  });

  it("TC-31.2.4 exposes package gating and explicit acceptance", async () => {
    mocks.list.mockResolvedValue([row]); mocks.review.mockResolvedValue({ data: { ...row, reviewStatus: "Accepted", isPackageEligible: true }, error: null });
    const user = userEvent.setup(); render(<EsrsReportDataPanel contractId={contractId} contractNumber={contractNumber} companyUei={companyUei} canManage />);
    expect(await screen.findByText(/blocked from final package/i)).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Accept" }));
    await waitFor(() => expect(mocks.review).toHaveBeenCalledWith(contractId, row.id, "Accepted", null, 1));
    expect(await screen.findByText(/marked accepted/i)).toBeInTheDocument();
  });

  it("fails closed to read-only controls without ManageReports", async () => {
    mocks.list.mockResolvedValue([row]); render(<EsrsReportDataPanel contractId={contractId} contractNumber={contractNumber} companyUei={companyUei} canManage={false} />);
    expect(await screen.findByText(/read-only access/i)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Accept" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Create report data row" })).not.toBeInTheDocument();
  });
});
