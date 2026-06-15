import { cleanup, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const {
  acknowledgeNoCuiNoticeMock,
  allWorkflowAccess,
  createEvidenceUploadIntentMock,
  createTenantInvitationMock,
  fallbackOverview,
  getNoCuiAcknowledgementStatusMock,
  getComplianceOverviewMock,
  getCurrentUserAccessMock,
  getTenantInvitationsMock,
  getTenantMembersMock,
  invitations,
  members,
  overview,
  restrictedAccess
} = vi.hoisted(() => ({
  acknowledgeNoCuiNoticeMock: vi.fn(),
  createEvidenceUploadIntentMock: vi.fn(),
  createTenantInvitationMock: vi.fn(),
  getComplianceOverviewMock: vi.fn(),
  getCurrentUserAccessMock: vi.fn(),
  getNoCuiAcknowledgementStatusMock: vi.fn(),
  getTenantInvitationsMock: vi.fn(),
  getTenantMembersMock: vi.fn(),
  allWorkflowAccess: {
    tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
    userId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
    userEmail: "admin@example.com",
    roles: ["Admin"],
    permissions: [
      "ManageUsers",
      "ViewCompanyProfile",
      "ViewContracts",
      "ViewObligations",
      "ViewTasks",
      "ViewEvidence",
      "ManageEvidence",
      "ViewCmmc",
      "ViewSubcontractors",
      "ViewReports"
    ],
    rolePermissionMatrix: {}
  },
  restrictedAccess: {
    tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
    userId: "cccccccc-cccc-cccc-cccc-ccccccccccc2",
    userEmail: "auditor@example.com",
    roles: ["Auditor"],
    permissions: ["ViewObligations", "ViewReports"],
    rolePermissionMatrix: {}
  },
  fallbackOverview: {
    productPromise:
      "Connect to the GCCS API to load source-backed modules, obligations, review metadata, and tenant-scoped compliance workflow state.",
    mvpDataPosture: "No-CUI / compliance management only",
    modules: [],
    priorityObligations: []
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
  acknowledgeNoCuiNotice: acknowledgeNoCuiNoticeMock,
  createEvidenceUploadIntent: createEvidenceUploadIntentMock,
  fallbackAccess: {
    tenantId: null,
    userId: null,
    userEmail: null,
    roles: [],
    permissions: [],
    rolePermissionMatrix: {}
  },
  fallbackNoCuiAcknowledgementStatus: {
    isAcknowledged: false,
    noticeVersion: "no-cui-mvp-v1",
    noticeCopy:
      "The GCCS MVP is compliance management only and is not ready to store CUI. Do not upload CUI, classified information, ITAR/export-controlled technical data, SSNs, payroll, bank or tax details, protected medical or disability data, passwords, secrets, private keys, unrestricted security logs, or other prohibited sensitive content.",
    tenantId: null,
    acknowledgedByUserId: null,
    acknowledgedAt: null
  },
  fallbackOverview,
  getComplianceOverview: getComplianceOverviewMock,
  getCurrentUserAccess: getCurrentUserAccessMock,
  getNoCuiAcknowledgementStatus: getNoCuiAcknowledgementStatusMock,
  getTenantInvitations: getTenantInvitationsMock,
  getTenantMembers: getTenantMembersMock
}));

import { App } from "@/App";

describe("App", () => {
  beforeEach(() => {
    window.location.hash = "";
    acknowledgeNoCuiNoticeMock.mockReset();
    createEvidenceUploadIntentMock.mockReset();
    createTenantInvitationMock.mockReset();
    getComplianceOverviewMock.mockReset();
    getCurrentUserAccessMock.mockReset();
    getNoCuiAcknowledgementStatusMock.mockReset();
    getTenantInvitationsMock.mockReset();
    getTenantMembersMock.mockReset();
    getNoCuiAcknowledgementStatusMock.mockResolvedValue({
      isAcknowledged: false,
      noticeVersion: "no-cui-mvp-v1",
      noticeCopy:
        "The GCCS MVP is compliance management only and is not ready to store CUI. Do not upload CUI, classified information, ITAR/export-controlled technical data, SSNs, payroll, bank or tax details, protected medical or disability data, passwords, secrets, private keys, unrestricted security logs, or other prohibited sensitive content.",
      tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
      acknowledgedByUserId: null,
      acknowledgedAt: null
    });
  });

  afterEach(() => {
    cleanup();
  });

  it("TC-3.2.1 lands authenticated users in the workspace dashboard", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(overview);
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);
    getTenantInvitationsMock.mockResolvedValueOnce(invitations);
    getTenantMembersMock.mockResolvedValueOnce(members);

    render(<App />);

    expect(await screen.findByRole("heading", { name: "Dashboard" })).toBeInTheDocument();
    expect(screen.getByText(overview.productPromise)).toBeInTheDocument();
    expect(screen.queryByText(/marketing/i)).not.toBeInTheDocument();
    expect(screen.getByRole("navigation", { name: /primary workspace navigation/i })).toBeInTheDocument();
  });

  it("TC-3.2.2 supports keyboard navigation across each visible primary route", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(overview);
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);
    getTenantInvitationsMock.mockResolvedValueOnce(invitations);
    getTenantMembersMock.mockResolvedValueOnce(members);
    const user = userEvent.setup();

    render(<App />);

    await screen.findByRole("heading", { name: "Dashboard" });
    await user.tab();
    expect(screen.getByRole("link", { name: /skip to workspace content/i })).toHaveFocus();

    const routeChecks = [
      ["Profile", "No company profile has been created yet"],
      ["Contracts", "No contracts have been added yet"],
      ["Obligations", "No tenant-specific obligation matrix yet"],
      ["Calendar", "No calendar items yet"],
      ["Evidence", "No-CUI acknowledgement"],
      ["CMMC", "No CMMC assessment has started yet"],
      ["Subcontractors", "No subcontractors have been added yet"],
      ["Reports", "No reports have been generated yet"],
      ["Settings", "Team members"]
    ];

    for (const [linkName, expectedText] of routeChecks) {
      const link = screen.getByRole("link", { name: new RegExp(linkName, "i") });
      link.focus();
      expect(link).toHaveFocus();
      await user.keyboard("{Enter}");
      expect(await screen.findByText(expectedText)).toBeInTheDocument();
      expect(link).toHaveAttribute("aria-current", "page");
    }
  });

  it("TC-2.4.2 renders workspace actions and TC-3.2.3 hides restricted navigation", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(overview);
    getCurrentUserAccessMock.mockResolvedValueOnce(restrictedAccess);

    render(<App />);

    await screen.findByRole("heading", { name: "Dashboard" });
    const navigation = screen.getByRole("navigation", { name: /primary workspace navigation/i });

    expect(within(navigation).getByRole("link", { name: /dashboard/i })).toBeInTheDocument();
    expect(within(navigation).getByRole("link", { name: /obligations/i })).toBeInTheDocument();
    expect(within(navigation).getByRole("link", { name: /reports/i })).toBeInTheDocument();
    expect(within(navigation).queryByRole("link", { name: /profile/i })).not.toBeInTheDocument();
    expect(within(navigation).queryByRole("link", { name: /contracts/i })).not.toBeInTheDocument();
    expect(within(navigation).queryByRole("link", { name: /settings/i })).not.toBeInTheDocument();
    expect(getTenantMembersMock).not.toHaveBeenCalled();
    expect(getTenantInvitationsMock).not.toHaveBeenCalled();
    expect(getNoCuiAcknowledgementStatusMock).not.toHaveBeenCalled();
  });

  it("TC-3.2.4 shows loading, empty, and error states", async () => {
    let resolveOverview: (value: typeof fallbackOverview) => void = () => undefined;
    getComplianceOverviewMock.mockReturnValueOnce(
      new Promise((resolve) => {
        resolveOverview = resolve;
      })
    );
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);
    getTenantInvitationsMock.mockResolvedValueOnce([]);
    getTenantMembersMock.mockResolvedValueOnce([]);

    const { unmount } = render(<App />);
    expect(screen.getByText("Loading workspace data")).toBeInTheDocument();
    resolveOverview(fallbackOverview);
    expect(await screen.findByText("API overview unavailable")).toBeInTheDocument();
    expect(screen.getByText("Source data unavailable")).toBeInTheDocument();

    unmount();
    cleanup();
    getComplianceOverviewMock.mockReset();
    getCurrentUserAccessMock.mockReset();
    getComplianceOverviewMock.mockRejectedValueOnce(new Error("API unavailable"));
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);

    render(<App />);
    expect(await screen.findByRole("alert")).toHaveTextContent("Workspace data could not be loaded");
  });

  it("keeps user invitation actions in the role-aware settings route", async () => {
    const createdInvitation = {
      ...invitations[0],
      invitationId: "dddddddd-dddd-dddd-dddd-ddddddddddd5",
      email: "new.invite@example.com",
      invitationToken: "new-token",
      notificationPlaceholder: "Local invitation notification queued for new.invite@example.com with token new-token."
    };
    getComplianceOverviewMock.mockResolvedValueOnce(fallbackOverview);
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);
    getTenantInvitationsMock.mockResolvedValueOnce([]);
    getTenantMembersMock.mockResolvedValueOnce([]);
    createTenantInvitationMock.mockResolvedValueOnce(createdInvitation);
    const user = userEvent.setup();

    render(<App />);

    await user.click(await screen.findByRole("link", { name: /settings/i }));
    await user.type(screen.getByLabelText("Email"), "new.invite@example.com");
    await user.selectOptions(screen.getByLabelText("Role"), "Auditor");
    await user.click(screen.getByRole("button", { name: /invite/i }));

    expect(createTenantInvitationMock).toHaveBeenCalledWith({
      email: "new.invite@example.com",
      roleName: "Auditor",
      expiresInDays: 7
    });
    expect(await screen.findByText("Invitation created.")).toBeInTheDocument();
    expect(screen.getByText("new.invite@example.com")).toBeInTheDocument();
  });

  it("TC-4.1.1 and TC-4.1.2 shows the No-CUI notice before upload and disables upload controls", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(overview);
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);
    getTenantInvitationsMock.mockResolvedValueOnce(invitations);
    getTenantMembersMock.mockResolvedValueOnce(members);
    const user = userEvent.setup();

    render(<App />);

    await user.click(await screen.findByRole("link", { name: /evidence/i }));

    expect(screen.getByText("No-CUI acknowledgement")).toBeInTheDocument();
    expect(screen.getByText(/compliance management only and is not ready to store CUI/i)).toBeInTheDocument();
    expect(screen.getByLabelText("Evidence file")).toBeDisabled();
    expect(screen.getByRole("button", { name: /upload evidence/i })).toBeDisabled();
    expect(screen.getByText(/upload is disabled until the No-CUI notice is acknowledged/i)).toBeInTheDocument();
  });

  it("TC-4.1.3 and TC-4.1.4 saves acknowledgement before enabling upload intent creation", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(overview);
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);
    getTenantInvitationsMock.mockResolvedValueOnce(invitations);
    getTenantMembersMock.mockResolvedValueOnce(members);
    acknowledgeNoCuiNoticeMock.mockResolvedValueOnce({
      isAcknowledged: true,
      noticeVersion: "no-cui-mvp-v1",
      noticeCopy:
        "The GCCS MVP is compliance management only and is not ready to store CUI. Do not upload CUI, classified information, ITAR/export-controlled technical data, SSNs, payroll, bank or tax details, protected medical or disability data, passwords, secrets, private keys, unrestricted security logs, or other prohibited sensitive content.",
      tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
      acknowledgedByUserId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
      acknowledgedAt: "2026-06-14T12:00:00Z"
    });
    createEvidenceUploadIntentMock.mockResolvedValueOnce({
      data: {
        id: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee1",
        evidenceItemId: "00000000-0000-0000-0000-000000000041",
        tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
        createdByUserId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
        fileName: "policy.pdf",
        contentType: "application/pdf",
        sizeBytes: 6,
        status: "upload-pending",
        validationStatus: "accepted",
        malwareScanStatus: "scan-pending",
        message: "No-CUI acknowledgement is on record.",
        noticeVersion: "no-cui-mvp-v1",
        expiresAt: "2026-06-14T12:15:00Z"
      },
      error: null
    });
    const user = userEvent.setup();

    render(<App />);

    await user.click(await screen.findByRole("link", { name: /evidence/i }));
    await user.click(screen.getByRole("button", { name: /i acknowledge the no-cui upload limitation/i }));

    expect(acknowledgeNoCuiNoticeMock).toHaveBeenCalledWith("no-cui-mvp-v1");
    expect(await screen.findByText("Acknowledgement saved.")).toBeInTheDocument();
    expect(screen.getAllByText("Acknowledged").length).toBeGreaterThan(0);
    const fileInput = screen.getByLabelText("Evidence file");
    expect(fileInput).toBeEnabled();

    await user.upload(fileInput, new File(["policy"], "policy.pdf", { type: "application/pdf" }));
    await user.click(screen.getByRole("button", { name: /upload evidence/i }));

    expect(createEvidenceUploadIntentMock).toHaveBeenCalledWith(expect.objectContaining({ name: "policy.pdf", type: "application/pdf" }));
    expect(await screen.findByText(/Upload intent created for policy.pdf/i)).toBeInTheDocument();
    expect(screen.getByText(/malware scan scan-pending/i)).toBeInTheDocument();
  });

  it("TC-4.2.1 and TC-4.2.2 shows server-side upload guardrail rejection messages", async () => {
    getComplianceOverviewMock.mockResolvedValueOnce(overview);
    getCurrentUserAccessMock.mockResolvedValueOnce(allWorkflowAccess);
    getTenantInvitationsMock.mockResolvedValueOnce(invitations);
    getTenantMembersMock.mockResolvedValueOnce(members);
    getNoCuiAcknowledgementStatusMock.mockResolvedValueOnce({
      isAcknowledged: true,
      noticeVersion: "no-cui-mvp-v1",
      noticeCopy:
        "The GCCS MVP is compliance management only and is not ready to store CUI. Do not upload CUI, classified information, ITAR/export-controlled technical data, SSNs, payroll, bank or tax details, protected medical or disability data, passwords, secrets, private keys, unrestricted security logs, or other prohibited sensitive content.",
      tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
      acknowledgedByUserId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
      acknowledgedAt: "2026-06-14T12:00:00Z"
    });
    createEvidenceUploadIntentMock.mockResolvedValueOnce({
      data: null,
      error: "File size exceeds the No-CUI MVP upload limit."
    });
    const user = userEvent.setup();

    render(<App />);

    await user.click(await screen.findByRole("link", { name: /evidence/i }));
    expect(screen.getByText(/Allowed file types: PDF, PNG, JPG, TXT, CSV, DOCX, and XLSX/i)).toBeInTheDocument();

    const fileInput = screen.getByLabelText("Evidence file");
    await user.upload(fileInput, new File(["policy"], "policy.pdf", { type: "application/pdf" }));
    await user.click(screen.getByRole("button", { name: /upload evidence/i }));

    expect(createEvidenceUploadIntentMock).toHaveBeenCalledWith(
      expect.objectContaining({ name: "policy.pdf", type: "application/pdf" })
    );
    expect(await screen.findByText("File size exceeds the No-CUI MVP upload limit.")).toBeInTheDocument();
  });
});
