import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { PlatformAdminHomePage } from "./PlatformAdminHomePage";
import * as api from "./lib/api";

vi.mock("./lib/api", async (importOriginal) => ({
  ...(await importOriginal<typeof api>()),
  getPlatformAccess: vi.fn(),
  getPlatformDemoRequests: vi.fn(),
  getPlatformTenantOnboardings: vi.fn()
}));

describe("PlatformAdminHomePage", () => {
  beforeEach(() => vi.resetAllMocks());
  afterEach(() => cleanup());

  it("shows implemented operations and live counts allowed by platform permissions", async () => {
    vi.mocked(api.getPlatformAccess).mockResolvedValue({
      userId: "operator-1",
      userEmail: "operator@example.com",
      canProvisionTenants: true,
      canManageDemoRequests: true,
      permissions: ["ProvisionTenants", "ManageDemoRequests"]
    });
    vi.mocked(api.getPlatformDemoRequests).mockResolvedValue({
      items: [], page: 1, pageSize: 5, totalCount: 12, hasNextPage: true, hasPreviousPage: false
    });
    vi.mocked(api.getPlatformTenantOnboardings).mockResolvedValue({
      items: [], page: 1, pageSize: 5, totalCount: 3, hasNextPage: false, hasPreviousPage: false
    });

    render(<PlatformAdminHomePage />);

    expect(await screen.findByRole("heading", { name: "Admin overview" })).toBeInTheDocument();
    expect(await screen.findByText("12")).toBeInTheDocument();
    expect(await screen.findByText("3")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Open demo requests/i })).toHaveAttribute("href", "/platform/demo-requests");
    expect(screen.getByRole("link", { name: /Open tenant onboarding/i })).toHaveAttribute("href", "/platform/tenants/new");
    expect(api.getPlatformTenantOnboardings).toHaveBeenCalledWith(1, 5, "PendingOwnerAcceptance");
  });

  it("does not call or expose an operation the operator cannot manage", async () => {
    vi.mocked(api.getPlatformAccess).mockResolvedValue({
      userId: "operator-1",
      userEmail: "operator@example.com",
      canProvisionTenants: false,
      canManageDemoRequests: true,
      permissions: ["ManageDemoRequests"]
    });
    vi.mocked(api.getPlatformDemoRequests).mockResolvedValue({
      items: [], page: 1, pageSize: 5, totalCount: 0, hasNextPage: false, hasPreviousPage: false
    });

    render(<PlatformAdminHomePage />);

    expect(await screen.findByRole("heading", { name: "Admin overview" })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: /Open tenant onboarding/i })).not.toBeInTheDocument();
    expect(api.getPlatformTenantOnboardings).not.toHaveBeenCalled();
  });

  it("fails closed when the account has no platform permission", async () => {
    vi.mocked(api.getPlatformAccess).mockResolvedValue({
      userId: "user-1",
      userEmail: "user@example.com",
      canProvisionTenants: false,
      canManageDemoRequests: false,
      permissions: []
    });

    render(<PlatformAdminHomePage />);

    expect(await screen.findByRole("heading", { name: "Platform access denied" })).toBeInTheDocument();
    expect(api.getPlatformDemoRequests).not.toHaveBeenCalled();
    expect(api.getPlatformTenantOnboardings).not.toHaveBeenCalled();
  });
});
