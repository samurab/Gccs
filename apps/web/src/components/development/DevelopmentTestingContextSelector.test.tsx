import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { DevelopmentTestingContextSelector } from "./DevelopmentTestingContextSelector";

const {
  getDevelopmentTestingContextMock,
  getSelectedDevelopmentRoleMock,
  getSelectedDevelopmentUserIdMock,
  getSelectedTenantIdMock,
  selectDevelopmentTestingContextMock
} = vi.hoisted(() => ({
  getDevelopmentTestingContextMock: vi.fn(),
  getSelectedDevelopmentRoleMock: vi.fn(),
  getSelectedDevelopmentUserIdMock: vi.fn(),
  getSelectedTenantIdMock: vi.fn(),
  selectDevelopmentTestingContextMock: vi.fn()
}));

vi.mock("@/lib/api", () => ({
  getDevelopmentTestingContext: getDevelopmentTestingContextMock,
  getSelectedDevelopmentRole: getSelectedDevelopmentRoleMock,
  getSelectedDevelopmentUserId: getSelectedDevelopmentUserIdMock,
  getSelectedTenantId: getSelectedTenantIdMock,
  selectDevelopmentTestingContext: selectDevelopmentTestingContextMock
}));

const tenantId = "tenant-1";
const userId = "user-1";
const context = {
  tenants: [{
    tenantId,
    displayName: "Tenant Alpha",
    tenantStatus: "Active",
    dataHandlingMode: "NoCui",
    isSelectable: true,
    unavailableReason: null
  }],
  personas: [{
    tenantId,
    userId,
    email: "bai.dumbuya.legal@gmail.com",
    displayName: "Bai Dumbuya",
    roleName: "Auditor"
  }],
  roles: ["Auditor", "Contributor"]
};

describe("DevelopmentTestingContextSelector", () => {
  beforeEach(() => {
    getDevelopmentTestingContextMock.mockReset();
    getSelectedDevelopmentRoleMock.mockReset();
    getSelectedDevelopmentUserIdMock.mockReset();
    getSelectedTenantIdMock.mockReset();
    selectDevelopmentTestingContextMock.mockReset();
    getDevelopmentTestingContextMock.mockResolvedValue(context);
    getSelectedTenantIdMock.mockReturnValue(tenantId);
    getSelectedDevelopmentUserIdMock.mockReturnValue(userId);
  });

  afterEach(cleanup);

  it("keeps an explicitly selected role after the context is reloaded", async () => {
    getSelectedDevelopmentRoleMock.mockReturnValue("Contributor");

    render(<DevelopmentTestingContextSelector currentTenantId={tenantId} />);

    expect(await screen.findByRole("combobox", { name: "Switch role" })).toHaveValue("Contributor");
  });

  it("uses the membership role when no role context has been saved", async () => {
    getSelectedDevelopmentRoleMock.mockReturnValue("Owner");
    getSelectedDevelopmentUserIdMock.mockReturnValue(null);

    render(<DevelopmentTestingContextSelector currentTenantId={tenantId} />);

    expect(await screen.findByRole("combobox", { name: "Switch role" })).toHaveValue("Auditor");
  });
});
