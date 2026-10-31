import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { LaborClassificationPanel } from "./LaborClassificationPanel";

const mocks = vi.hoisted(() => ({
  categories: vi.fn(), assignments: vi.fn(), employees: vi.fn(), createCategory: vi.fn(),
  createAssignment: vi.fn(), deactivateCategory: vi.fn(), deactivateAssignment: vi.fn(),
  reclassify: vi.fn(), review: vi.fn()
}));
vi.mock("@/lib/api", () => ({
  getContractLaborCategories: mocks.categories, getContractLaborAssignments: mocks.assignments,
  getLaborClassificationEmployees: mocks.employees, createLaborCategory: mocks.createCategory,
  createLaborAssignment: mocks.createAssignment, deactivateLaborCategory: mocks.deactivateCategory,
  deactivateLaborAssignment: mocks.deactivateAssignment, reclassifyLaborAssignment: mocks.reclassify,
  reviewLaborAssignment: mocks.review
}));

const contractId = "32232232-2232-2322-3223-2232232232bb";
const category = {
  id: "category-1", tenantId: "tenant-1", contractId, title: "Help Desk Technician II",
  wageDeterminationClassification: "Computer Operator IV", hourlyWage: 34.12, fringeRate: 4.98,
  fringeDescription: "Health and welfare", effectiveStart: "2026-01-01", effectiveEnd: "2026-12-31",
  sourceReference: "WD-2015-4341 Rev 24", isActive: true, createdAt: "2026-01-01T00:00:00Z", updatedAt: null
};
const assignment = {
  id: "assignment-1", tenantId: "tenant-1", contractId, employeeId: "employee-1",
  employeeName: "Taylor Employee", employeeEmail: "taylor@example.test", categoryId: category.id,
  laborCategoryTitle: category.title, workLocation: "Norfolk, VA", effectiveStart: "2026-01-01",
  effectiveEnd: "2026-06-30", status: "Active", sourceReference: "HR review 2026-01",
  evidenceItemIds: [], history: [], reviewStatus: "PendingReview", reviewNotes: null,
  reviewedByUserId: null, reviewedAt: null
};

describe("LaborClassificationPanel", () => {
  afterEach(cleanup);
  beforeEach(() => {
    vi.clearAllMocks(); mocks.categories.mockResolvedValue([]); mocks.assignments.mockResolvedValue([]);
    mocks.employees.mockResolvedValue([{ id: "employee-1", tenantId: "tenant-1", employeeNumber: "E-100", name: "Taylor Employee", email: "taylor@example.test" }]);
  });

  it("TC-32.2.1 creates a source-backed category and authoritative employee assignment", async () => {
    mocks.createCategory.mockResolvedValue({ data: category }); mocks.createAssignment.mockResolvedValue({ data: assignment });
    const user = userEvent.setup(); render(<LaborClassificationPanel contractId={contractId} canManage canViewSensitive />);
    await user.type(await screen.findByLabelText("Category title"), category.title);
    await user.type(screen.getByLabelText("Wage determination classification"), category.wageDeterminationClassification);
    await user.clear(screen.getByLabelText("Hourly wage")); await user.type(screen.getByLabelText("Hourly wage"), "34.12");
    await user.clear(screen.getByLabelText("Fringe rate")); await user.type(screen.getByLabelText("Fringe rate"), "4.98");
    await user.type(screen.getAllByLabelText("Effective start", { selector: "input" })[0], "2026-01-01");
    await user.type(screen.getAllByLabelText("Source reference")[0], category.sourceReference);
    await user.click(screen.getByRole("button", { name: "Create labor category" }));
    expect(mocks.createCategory).toHaveBeenCalledWith(contractId, expect.objectContaining({ title: category.title, sourceReference: category.sourceReference }));
    await user.selectOptions(screen.getByLabelText("Employee"), "employee-1");
    await user.selectOptions(screen.getByLabelText("Labor category"), category.id);
    await user.type(screen.getByLabelText("Work location"), "Norfolk, VA");
    await user.type(screen.getAllByLabelText("Effective start", { selector: "input" })[1], "2026-01-01");
    await user.type(screen.getAllByLabelText("Source reference")[1], "HR review 2026-01");
    await user.click(screen.getByRole("button", { name: "Assign employee" }));
    expect(mocks.createAssignment).toHaveBeenCalledWith(contractId, expect.objectContaining({ employeeId: "employee-1", categoryId: category.id }));
  });

  it("TC-32.2.2 surfaces server validation without adding a conflicting assignment", async () => {
    mocks.categories.mockResolvedValue([category]); mocks.createAssignment.mockResolvedValue({ error: "Assignment effective dates conflict with an existing assignment." });
    const user = userEvent.setup(); render(<LaborClassificationPanel contractId={contractId} canManage canViewSensitive />);
    await user.selectOptions(await screen.findByLabelText("Employee"), "employee-1");
    await user.selectOptions(screen.getByLabelText("Labor category"), category.id);
    await user.type(screen.getByLabelText("Work location"), "Norfolk, VA");
    await user.type(screen.getAllByLabelText("Effective start", { selector: "input" })[1], "2026-01-01");
    await user.type(screen.getAllByLabelText("Source reference")[1], "HR review");
    await user.click(screen.getByRole("button", { name: "Assign employee" }));
    expect(await screen.findByRole("alert")).toHaveTextContent(/effective dates conflict/i);
    expect(screen.getByText(/No employee classifications recorded/i)).toBeInTheDocument();
  });

  it("TC-32.2.3 fails closed when sensitive employee permission is absent", async () => {
    mocks.categories.mockResolvedValue([category]); mocks.assignments.mockResolvedValue([{ ...assignment, employeeName: null, employeeEmail: null }]);
    render(<LaborClassificationPanel contractId={contractId} canManage canViewSensitive={false} />);
    expect(await screen.findByText(/Sensitive employee fields are restricted/i)).toBeInTheDocument();
    expect(screen.getByText(/requires sensitive-employee-data permission/i)).toBeInTheDocument();
    expect(mocks.employees).not.toHaveBeenCalled();
    expect(screen.queryByText("Taylor Employee")).not.toBeInTheDocument();
  });
});
