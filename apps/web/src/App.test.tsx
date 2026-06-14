import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const {
  createTenantInvitationMock,
  getCurrentUserAccessMock,
  accessByRole,
  fallbackOverview,
  getComplianceOverviewMock,
  getTenantInvitationsMock,
  getTenantMembersMock,
  adminAccess,
  fallbackAccess,
  invitations,
  members,
  overview
} = vi.hoisted(() => ({
  createTenantInvitationMock: vi.fn(),
  getCurrentUserAccessMock: vi.fn(),
  adminAccess: {
    tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
    userId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
    userEmail: "admin@example.com",
    roles: ["Admin"],
    permissions: ["ManageUsers", "ViewObligations", "ViewReports"],
    rolePermissionMatrix: {}
  },
  fallbackAccess: {
    tenantId: null,
    userId: null,
    userEmail: null,
    roles: [],
    permissions: [],
    rolePermissionMatrix: {}
  },
  fallbackOverview: {
    productPromise:
      "Connect to the GCCS API to load source-backed modules, obligations, review metadata, and tenant-scoped compliance workflow state.",
    mvpDataPosture: "No-CUI / compliance management only",
    modules: [],
    priorityObligations: []
  },
  getComplianceOverviewMock: vi.fn(),
  getTenantInvitationsMock: vi.fn(),
  getTenantMembersMock: vi.fn(),
  accessByRole: {
    Owner: {
      tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
      userId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
      userEmail: "owner@example.com",
      roles: ["Owner"],
      permissions: ["ManageUsers", "ViewObligations", "ViewReports"],
      rolePermissionMatrix: {}
    },
    Admin: {
      tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
      userId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
      userEmail: "admin@example.com",
      roles: ["Admin"],
      permissions: ["ManageUsers", "ViewObligations", "ViewReports"],
      rolePermissionMatrix: {}
    },
    "Compliance Manager": {
      tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
      userId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
      userEmail: "compliance.manager@example.com",
      roles: ["Compliance Manager"],
      permissions: ["ViewObligations", "ViewReports"],
      rolePermissionMatrix: {}
    },
    Contributor: {
      tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
      userId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
      userEmail: "contributor@example.com",
      roles: ["Contributor"],
      permissions: ["ViewObligations", "ViewReports"],
      rolePermissionMatrix: {}
    },
    Auditor: {
      tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
      userId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
      userEmail: "auditor@example.com",
      roles: ["Auditor"],
      permissions: ["AuditorReadOnly", "ViewObligations", "ViewReports"],
      rolePermissionMatrix: {}
    },
    Advisor: {
      tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
      userId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
      userEmail: "advisor@example.com",
      roles: ["Advisor"],
      permissions: ["ViewObligations", "ViewReports"],
      rolePermissionMatrix: {}
    }
  },
  invitations: [
    {
      invitationId: "dddddddd-dddd-dddd-dddd-ddddddddddd1",
      tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
      email: "pending@example.com",
      roleName: "Contributor",
      invitationToken: "pending-token",
      status: "Pending",
      expiresAt: "2026-06-20T12:00:00Z",
      acceptedAt: null,
      acceptedByUserId: null,
      revokedAt: null,
      revokedByUserId: null,
      notificationSentAt: "2026-06-13T12:00:00Z",
      notificationPlaceholder: "Local invitation notification queued for pending@example.com with token pending-token.",
      createdAt: "2026-06-13T12:00:00Z",
      updatedAt: null
    },
    {
      invitationId: "dddddddd-dddd-dddd-dddd-ddddddddddd2",
      tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
      email: "accepted@example.com",
      roleName: "Auditor",
      invitationToken: "accepted-token",
      status: "Accepted",
      expiresAt: "2026-06-20T12:00:00Z",
      acceptedAt: "2026-06-14T12:00:00Z",
      acceptedByUserId: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee1",
      revokedAt: null,
      revokedByUserId: null,
      notificationSentAt: "2026-06-13T12:00:00Z",
      notificationPlaceholder: "Local invitation notification queued for accepted@example.com with token accepted-token.",
      createdAt: "2026-06-13T12:00:00Z",
      updatedAt: "2026-06-14T12:00:00Z"
    },
    {
      invitationId: "dddddddd-dddd-dddd-dddd-ddddddddddd3",
      tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
      email: "expired@example.com",
      roleName: "Advisor",
      invitationToken: "expired-token",
      status: "Expired",
      expiresAt: "2026-06-12T12:00:00Z",
      acceptedAt: null,
      acceptedByUserId: null,
      revokedAt: null,
      revokedByUserId: null,
      notificationSentAt: "2026-06-10T12:00:00Z",
      notificationPlaceholder: "Local invitation notification queued for expired@example.com with token expired-token.",
      createdAt: "2026-06-10T12:00:00Z",
      updatedAt: "2026-06-12T12:00:00Z"
    },
    {
      invitationId: "dddddddd-dddd-dddd-dddd-ddddddddddd4",
      tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
      email: "revoked@example.com",
      roleName: "Admin",
      invitationToken: "revoked-token",
      status: "Revoked",
      expiresAt: "2026-06-20T12:00:00Z",
      acceptedAt: null,
      acceptedByUserId: null,
      revokedAt: "2026-06-14T12:00:00Z",
      revokedByUserId: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee2",
      notificationSentAt: "2026-06-13T12:00:00Z",
      notificationPlaceholder: "Local invitation notification queued for revoked@example.com with token revoked-token.",
      createdAt: "2026-06-13T12:00:00Z",
      updatedAt: "2026-06-14T12:00:00Z"
    }
  ],
  members: [
    {
      membershipId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1",
      tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
      userId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
      email: "admin@example.com",
      displayName: "Avery Admin",
      userStatus: "Active",
      membershipStatus: "Active",
      roleName: "Admin",
      mfaEnabled: true,
      lastSignedInAt: null,
      lastAccessedAt: null,
      createdAt: "2026-06-13T12:00:00Z",
      updatedAt: null
    }
  ],
  overview: {
    productPromise: "Keep every govcon obligation tied to evidence and review status.",
    mvpDataPosture: "No-CUI / compliance management only",
    modules: [
      {
        key: "company-profile",
        name: "Company compliance profile",
        purpose: "Capture entity, SAM, NAICS, certification, and data posture details.",
        status: "planned"
      },
      {
        key: "obligations",
        name: "Obligation dashboard",
        purpose: "Map clauses to actions, owners, evidence, deadlines, and source links.",
        status: "seeded"
      }
    ],
    priorityObligations: [
      {
        id: "far-52-204-21",
        source: "FAR 52.204-21",
        title: "Basic Safeguarding of Covered Contractor Information Systems",
        ownerFunction: "IT/security",
        riskLevel: "High",
        sourceUrl: "https://www.acquisition.gov/far/52.204-21",
        lastReviewedAt: "2026-06-03"
      }
    ]
  }
}));

vi.mock("@/lib/api", () => ({
  createTenantInvitation: createTenantInvitationMock,
  fallbackAccess,
  fallbackOverview,
  getComplianceOverview: getComplianceOverviewMock,
  getCurrentUserAccess: getCurrentUserAccessMock,
  getTenantInvitations: getTenantInvitationsMock,
  getTenantMembers: getTenantMembersMock
}));

import { App } from "@/App";

describe("App", () => {
  beforeEach(() => {
    createTenantInvitationMock.mockReset();
    getCurrentUserAccessMock.mockReset();
    getComplianceOverviewMock.mockReset();
    getTenantInvitationsMock.mockReset();
    getTenantMembersMock.mockReset();
  });

  afterEach(() => {
    cleanup();
  });

  it("renders the compliance workspace from the overview data", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(overview);
    getCurrentUserAccessMock.mockResolvedValueOnce(adminAccess);
    getTenantInvitationsMock.mockResolvedValueOnce(invitations);
    getTenantMembersMock.mockResolvedValueOnce(members);

    render(<App />);

    expect(
      screen.getByRole("heading", {
        name: /govcon obligations, evidence, and readiness in one operating view/i
      })
    ).toBeInTheDocument();
    expect(await screen.findByText(overview.productPromise)).toBeInTheDocument();
    expect(screen.getByText("Company compliance profile")).toBeInTheDocument();
    expect(screen.getByText("Obligation dashboard")).toBeInTheDocument();
    expect(await screen.findByText("FAR 52.204-21")).toBeInTheDocument();
    expect(await screen.findByText("Avery Admin")).toBeInTheDocument();
    expect(screen.getByText("admin@example.com")).toBeInTheDocument();
    expect(screen.getByText("Enabled")).toBeInTheDocument();
    expect(await screen.findByText("pending@example.com")).toBeInTheDocument();
    expect(screen.getByText("accepted@example.com")).toBeInTheDocument();
    expect(screen.getByText("expired@example.com")).toBeInTheDocument();
    expect(screen.getByText("revoked@example.com")).toBeInTheDocument();
    expect(screen.getByText("Pending")).toBeInTheDocument();
    expect(screen.getByText("Accepted")).toBeInTheDocument();
    expect(screen.getByText("Expired")).toBeInTheDocument();
    expect(screen.getByText("Revoked")).toBeInTheDocument();
    expect(screen.getByText("No-CUI")).toBeInTheDocument();
  });

  it("shows empty states instead of UI-only source-backed content when the API is unavailable", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(fallbackOverview);
    getCurrentUserAccessMock.mockResolvedValueOnce(adminAccess);
    getTenantInvitationsMock.mockResolvedValueOnce([]);
    getTenantMembersMock.mockResolvedValueOnce([]);

    render(<App />);

    expect(await screen.findByText("API overview unavailable")).toBeInTheDocument();
    expect(screen.getByText("Source data unavailable")).toBeInTheDocument();
    expect(screen.getByText("No tenant members available")).toBeInTheDocument();
    expect(screen.getByText("No invitations available")).toBeInTheDocument();
    expect(screen.queryByText("FAR 52.204-21")).not.toBeInTheDocument();
    expect(screen.queryByText("admin@example.com")).not.toBeInTheDocument();
  });

  it("creates an invitation and appends the pending state", async () => {
    const createdInvitation = {
      ...invitations[0],
      invitationId: "dddddddd-dddd-dddd-dddd-ddddddddddd5",
      email: "new.invite@example.com",
      invitationToken: "new-token",
      notificationPlaceholder: "Local invitation notification queued for new.invite@example.com with token new-token."
    };
    getComplianceOverviewMock.mockResolvedValueOnce(fallbackOverview);
    getCurrentUserAccessMock.mockResolvedValueOnce(adminAccess);
    getTenantInvitationsMock.mockResolvedValueOnce([]);
    getTenantMembersMock.mockResolvedValueOnce([]);
    createTenantInvitationMock.mockResolvedValueOnce(createdInvitation);
    const user = userEvent.setup();

    render(<App />);

    await user.type(await screen.findByLabelText("Email"), "new.invite@example.com");
    await user.selectOptions(screen.getByLabelText("Role"), "Auditor");
    await user.click(screen.getByRole("button", { name: /invite/i }));

    expect(createTenantInvitationMock).toHaveBeenCalledWith({
      email: "new.invite@example.com",
      roleName: "Auditor",
      expiresInDays: 7
    });
    expect(await screen.findByText("Invitation created.")).toBeInTheDocument();
    expect(screen.getByText("new.invite@example.com")).toBeInTheDocument();
    expect(screen.getByText("Pending")).toBeInTheDocument();
  });

  it.each([
    ["Owner", true],
    ["Admin", true],
    ["Compliance Manager", false],
    ["Contributor", false],
    ["Auditor", false],
    ["Advisor", false]
  ])("TC-2.4.2 renders workspace actions for %s from the role permission matrix", async (roleName, canManageUsers) => {
    getComplianceOverviewMock.mockResolvedValueOnce(overview);
    getCurrentUserAccessMock.mockResolvedValueOnce(accessByRole[roleName as keyof typeof accessByRole]);
    if (canManageUsers) {
      getTenantInvitationsMock.mockResolvedValueOnce(invitations);
      getTenantMembersMock.mockResolvedValueOnce(members);
    }

    render(<App />);

    expect(await screen.findByText(overview.productPromise)).toBeInTheDocument();

    if (canManageUsers) {
      expect(await screen.findByRole("heading", { name: /team members/i })).toBeInTheDocument();
      expect(screen.getByRole("button", { name: /invite/i })).toBeInTheDocument();
      expect(screen.getByLabelText("Email")).toBeInTheDocument();
      expect(getTenantMembersMock).toHaveBeenCalledOnce();
      expect(getTenantInvitationsMock).toHaveBeenCalledOnce();
      return;
    }

    expect(screen.queryByRole("button", { name: /invite/i })).not.toBeInTheDocument();
    expect(screen.queryByLabelText("Email")).not.toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: /team members/i })).not.toBeInTheDocument();
    expect(getTenantMembersMock).not.toHaveBeenCalled();
    expect(getTenantInvitationsMock).not.toHaveBeenCalled();
  });
});
