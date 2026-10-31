import { cleanup, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { TenantWorkspaceSelector } from "./TenantWorkspaceSelector";

const {
  getMyTenantWorkspacesMock,
  getSelectedTenantIdMock,
  selectMyTenantWorkspaceMock,
  selectTenantMock
} = vi.hoisted(() => ({
  getMyTenantWorkspacesMock: vi.fn(),
  getSelectedTenantIdMock: vi.fn(),
  selectMyTenantWorkspaceMock: vi.fn(),
  selectTenantMock: vi.fn()
}));

vi.mock("@/lib/api", () => ({
  getMyTenantWorkspaces: getMyTenantWorkspacesMock,
  getSelectedTenantId: getSelectedTenantIdMock,
  selectMyTenantWorkspace: selectMyTenantWorkspaceMock,
  selectTenant: selectTenantMock
}));

const workspace = {
  membershipId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1",
  tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
  displayName: "Primary workspace",
  tenantStatus: "Active",
  dataHandlingMode: "NoCui",
  membershipStatus: "Active",
  roleName: "Admin",
  lastAccessedAt: null,
  isSelectable: true,
  unavailableReason: null
};

describe("TenantWorkspaceSelector", () => {
  beforeEach(() => {
    getMyTenantWorkspacesMock.mockReset();
    getSelectedTenantIdMock.mockReset();
    selectMyTenantWorkspaceMock.mockReset();
    selectTenantMock.mockReset();
  });

  afterEach(() => {
    cleanup();
  });

  it("initializes tenant-scoped loading only after a stored workspace is validated", async () => {
    const onInitialized = vi.fn();
    getSelectedTenantIdMock.mockReturnValue(workspace.tenantId);
    getMyTenantWorkspacesMock.mockResolvedValue({
      preferredTenantId: workspace.tenantId,
      tenants: [workspace]
    });

    render(<TenantWorkspaceSelector currentTenantId={null} onInitialized={onInitialized} />);

    expect(await screen.findByRole("combobox", { name: "Workspace" })).toHaveValue(workspace.tenantId);
    expect(onInitialized).toHaveBeenCalledOnce();
    expect(selectMyTenantWorkspaceMock).not.toHaveBeenCalled();
  });

  it("persists an automatic workspace before initializing tenant-scoped loading", async () => {
    const onInitialized = vi.fn();
    getSelectedTenantIdMock.mockReturnValue(null);
    getMyTenantWorkspacesMock.mockResolvedValue({
      preferredTenantId: workspace.tenantId,
      tenants: [workspace]
    });
    selectMyTenantWorkspaceMock.mockResolvedValue({
      data: {
        tenantId: workspace.tenantId,
        displayName: workspace.displayName,
        roleName: workspace.roleName,
        dataHandlingMode: workspace.dataHandlingMode
      },
      error: null
    });

    render(<TenantWorkspaceSelector currentTenantId={null} onInitialized={onInitialized} />);

    await waitFor(() => expect(selectMyTenantWorkspaceMock).toHaveBeenCalledWith(workspace.tenantId));
    expect(selectTenantMock).toHaveBeenCalledWith(workspace.tenantId);
    expect(onInitialized).toHaveBeenCalledOnce();
  });

  it("does not initialize tenant-scoped loading when multiple workspaces require a user choice", async () => {
    const onInitialized = vi.fn();
    getSelectedTenantIdMock.mockReturnValue(null);
    getMyTenantWorkspacesMock.mockResolvedValue({
      preferredTenantId: null,
      tenants: [
        workspace,
        {
          ...workspace,
          membershipId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2",
          tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2",
          displayName: "Secondary workspace"
        }
      ]
    });

    render(<TenantWorkspaceSelector currentTenantId={null} onInitialized={onInitialized} />);

    expect(await screen.findByRole("combobox", { name: "Workspace" })).toHaveValue("");
    expect(onInitialized).not.toHaveBeenCalled();
    expect(selectMyTenantWorkspaceMock).not.toHaveBeenCalled();
  });
});
