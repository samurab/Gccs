import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { PortalPackageLifecyclePanel } from "./PortalPackageLifecyclePanel";

const mocks = vi.hoisted(() => ({
  list: vi.fn(),
  report: vi.fn(),
  expire: vi.fn(),
  revoke: vi.fn(),
  supersede: vi.fn(),
  reissue: vi.fn(),
  archive: vi.fn()
}));

vi.mock("@/lib/api", () => ({
  getSharedPortalPackages: mocks.list,
  getPortalPackageActivityReport: mocks.report,
  expireSharedPortalPackage: mocks.expire,
  revokeSharedPortalPackage: mocks.revoke,
  supersedeSharedPortalPackage: mocks.supersede,
  reissueSharedPortalPackage: mocks.reissue,
  archiveSharedPortalPackage: mocks.archive
}));

const active = {
  id: "34334334-4334-3343-3433-433433433401",
  tenantId: "34334334-4334-3343-3433-433433433402",
  packageId: "34334334-4334-3343-3433-433433433403",
  invitationId: "34334334-4334-3343-3433-433433433404",
  version: 1,
  state: "Active" as const,
  expiresAt: "2026-10-10T12:00:00Z",
  reminderAt: "2026-10-03T12:00:00Z",
  reminderSentAt: null,
  supersedesSharedPackageId: null,
  replacementSharedPackageId: null,
  replacementPackageId: null,
  revocationReason: null,
  revokedAt: null,
  createdAt: "2026-09-10T12:00:00Z",
  updatedAt: null
};

describe("PortalPackageLifecyclePanel", () => {
  afterEach(cleanup);
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.list.mockResolvedValue([active]);
    mocks.report.mockResolvedValue({
      tenantId: active.tenantId,
      activities: [{
        id: "activity-1",
        sharedPackageId: active.id,
        tenantId: active.tenantId,
        activityType: "Download",
        actorUserId: "portal-user",
        occurredAt: "2026-09-10T13:00:00Z",
        detail: null
      }]
    });
  });

  it("shows lifecycle state, expiration reminder, and portal activity", async () => {
    render(<PortalPackageLifecyclePanel canManage canViewActivity />);

    expect(await screen.findByText("Version 1 · Active")).toBeInTheDocument();
    expect(screen.getByText(/reminder scheduled/i)).toBeInTheDocument();
    expect(screen.getByText(/Download/)).toBeInTheDocument();
    expect(screen.getByText(/does not authorize CUI sharing/i)).toBeInTheDocument();
  });

  it("requires a reason and updates the package after revocation", async () => {
    const revoked = {
      ...active,
      state: "Revoked" as const,
      revocationReason: "Review scope was reduced.",
      revokedAt: "2026-09-10T14:00:00Z"
    };
    mocks.revoke.mockResolvedValue({ data: revoked, error: null });
    const user = userEvent.setup();
    render(<PortalPackageLifecyclePanel canManage canViewActivity />);

    await user.click(await screen.findByRole("button", { name: "Revoke" }));
    await user.type(screen.getByLabelText("Revocation reason"), "Review scope was reduced.");
    await user.click(screen.getByRole("button", { name: "Confirm revoke" }));

    await waitFor(() => expect(mocks.revoke).toHaveBeenCalledWith(active.id, "Review scope was reduced."));
    expect(await screen.findByText("Version 1 · Revoked")).toBeInTheDocument();
    expect(screen.getByText(/access was revoked immediately/i)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Expire" })).not.toBeInTheDocument();
  });

  it("fails closed when the package list cannot be loaded", async () => {
    mocks.list.mockRejectedValue(new Error("network unavailable"));
    render(<PortalPackageLifecyclePanel canManage canViewActivity />);

    expect(await screen.findByRole("alert")).toHaveTextContent("Shared portal packages could not be loaded.");
    expect(screen.queryByRole("button", { name: "Revoke" })).not.toBeInTheDocument();
  });

  it("hides lifecycle controls without tenant administrator permission", async () => {
    render(<PortalPackageLifecyclePanel canManage={false} canViewActivity={false} />);

    expect(await screen.findByText(/requires tenant administrator access/i)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Revoke" })).not.toBeInTheDocument();
    expect(mocks.report).not.toHaveBeenCalled();
  });
});
