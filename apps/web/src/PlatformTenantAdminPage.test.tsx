import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { PlatformTenantAdminPage } from "./PlatformTenantAdminPage";

const {
  cancelPlatformTenantOnboardingMock,
  extendPlatformPilotSubscriptionMock,
  getPlatformAccessMock,
  getPlatformTenantOnboardingsMock,
  provisionPlatformTenantMock,
  resendPlatformTenantInvitationMock
} = vi.hoisted(() => ({
  cancelPlatformTenantOnboardingMock: vi.fn(),
  extendPlatformPilotSubscriptionMock: vi.fn(),
  getPlatformAccessMock: vi.fn(),
  getPlatformTenantOnboardingsMock: vi.fn(),
  provisionPlatformTenantMock: vi.fn(),
  resendPlatformTenantInvitationMock: vi.fn()
}));

vi.mock("./lib/api", async () => {
  const actual = await vi.importActual<typeof import("./lib/api")>("./lib/api");
  return {
    ...actual,
    cancelPlatformTenantOnboarding: cancelPlatformTenantOnboardingMock,
    extendPlatformPilotSubscription: extendPlatformPilotSubscriptionMock,
    getPlatformAccess: getPlatformAccessMock,
    getPlatformTenantOnboardings: getPlatformTenantOnboardingsMock,
    provisionPlatformTenant: provisionPlatformTenantMock,
    resendPlatformTenantInvitation: resendPlatformTenantInvitationMock
  };
});

describe("PlatformTenantAdminPage", () => {
  beforeEach(() => {
    cancelPlatformTenantOnboardingMock.mockReset();
    extendPlatformPilotSubscriptionMock.mockReset();
    getPlatformAccessMock.mockReset();
    getPlatformTenantOnboardingsMock.mockReset();
    provisionPlatformTenantMock.mockReset();
    resendPlatformTenantInvitationMock.mockReset();
    getPlatformAccessMock.mockResolvedValue({
      userId: "22222222-2222-2222-2222-222222222222",
      userEmail: "operator@gccs.local",
      canProvisionTenants: true,
      canManageDemoRequests: false,
      permissions: ["ProvisionTenants"]
    });
    getPlatformTenantOnboardingsMock.mockResolvedValue({
      items: [],
      page: 1,
      pageSize: 25,
      totalCount: 0,
      hasNextPage: false,
      hasPreviousPage: false
    });
  });

  afterEach(() => cleanup());

  it("shows paid-only fields and commercial confirmation when Paid is selected", async () => {
    const user = userEvent.setup();
    render(<PlatformTenantAdminPage />);

    await screen.findByRole("heading", { name: "Tenant onboarding" });
    await user.click(screen.getByRole("radio", { name: /Paid/ }));

    expect(screen.getByLabelText("Plan code")).toBeInTheDocument();
    expect(screen.getByLabelText("Subscription reference")).toBeInTheDocument();
    expect(screen.getByRole("checkbox", { name: /Commercial approval confirmed/ })).toBeInTheDocument();
    expect(screen.queryByLabelText("Pilot end date")).not.toBeInTheDocument();
  });

  it("submits a pilot as a pending No-CUI onboarding without an owner user ID", async () => {
    const user = userEvent.setup();
    provisionPlatformTenantMock.mockResolvedValue({
      data: {
        onboardingId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1",
        tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
        displayName: "Aegis Pilot Workspace",
        onboardingType: "Pilot",
        onboardingStatus: "PendingOwnerAcceptance",
        tenantStatus: "PendingActivation",
        dataHandlingMode: "NoCui",
        customerReference: "PILOT-003",
        ownerEmail: "owner@example.com",
        ownerDisplayName: "Pilot Owner",
        ownerRoleName: "Owner",
        invitationId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
        invitationStatus: "Pending",
        invitationDeliveryStatus: "Queued",
        invitationNotificationSentAt: null,
        invitationExpiresAt: "2026-08-05T12:00:00Z",
        trialEndsAt: "2026-08-31",
        planCode: null,
        subscriptionReference: null,
        setupReason: "Provision approved No-CUI pilot PILOT-003.",
        createdAt: "2026-07-22T12:00:00Z",
        isReplay: false
      },
      error: null
    });

    render(<PlatformTenantAdminPage />);
    await screen.findByRole("heading", { name: "Tenant onboarding" });

    await user.type(screen.getByLabelText("Customer reference"), "PILOT-003");
    await user.type(screen.getByLabelText("Tenant display name"), "Aegis Pilot Workspace");
    await user.type(screen.getByLabelText("Pilot end date"), "2026-08-31");
    await user.type(screen.getByLabelText("Setup reason"), "Provision approved No-CUI pilot PILOT-003.");
    await user.type(screen.getByLabelText("Owner email"), "owner@example.com");
    await user.type(screen.getByLabelText("Owner display name"), "Pilot Owner");
    await user.click(screen.getByRole("checkbox", { name: /No-CUI boundary confirmed/ }));
    await user.click(screen.getByRole("button", { name: "Create pending tenant" }));

    expect(provisionPlatformTenantMock).toHaveBeenCalledOnce();
    const [request, idempotencyKey] = provisionPlatformTenantMock.mock.calls[0];
    expect(request).toEqual({
      onboardingType: "Pilot",
      customerReference: "PILOT-003",
      displayName: "Aegis Pilot Workspace",
      ownerEmail: "owner@example.com",
      ownerDisplayName: "Pilot Owner",
      trialEndsAt: "2026-08-31",
      planCode: null,
      subscriptionReference: null,
      setupReason: "Provision approved No-CUI pilot PILOT-003.",
      confirmsNoCui: true,
      commercialApprovalConfirmed: false
    });
    expect(idempotencyKey).toMatch(/^[0-9a-f-]{36}$/i);
    expect(await screen.findByText("PendingActivation")).toBeInTheDocument();
    expect(screen.getByText("Queued")).toBeInTheDocument();
    expect(screen.queryByText(/invitation token/i)).not.toBeInTheDocument();
  });

  it("blocks the form when the account lacks the platform permission", async () => {
    getPlatformAccessMock.mockResolvedValue({
      userId: "dddddddd-dddd-dddd-dddd-ddddddddddd1",
      userEmail: "owner@example.com",
      canProvisionTenants: false,
      canManageDemoRequests: false,
      permissions: []
    });

    render(<PlatformTenantAdminPage />);

    expect(await screen.findByRole("heading", { name: "Provisioning access denied" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Create pending tenant" })).not.toBeInTheDocument();
  });

  it("requires a reason and cancels a previously created pending onboarding", async () => {
    const user = userEvent.setup();
    const pending = {
      onboardingId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1",
      tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
      displayName: "Duplicate Pilot",
      onboardingType: "Pilot",
      onboardingStatus: "PendingOwnerAcceptance",
      tenantStatus: "PendingActivation",
      dataHandlingMode: "NoCui",
      customerReference: "PILOT-DUPLICATE",
      ownerEmail: "owner@example.com",
      ownerDisplayName: "Pilot Owner",
      ownerRoleName: "Owner",
      invitationId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
      invitationStatus: "Pending",
      invitationDeliveryStatus: "Queued",
      invitationNotificationSentAt: null,
      invitationExpiresAt: "2026-08-05T12:00:00Z",
      trialEndsAt: "2026-08-31",
      planCode: null,
      subscriptionReference: null,
      setupReason: "Duplicate test.",
      createdAt: "2026-07-22T12:00:00Z",
      cancelledAt: null,
      cancelledByUserId: null,
      cancellationReason: null,
      subscription: null,
      isReplay: false
    };
    getPlatformTenantOnboardingsMock.mockResolvedValue({
      items: [pending],
      page: 1,
      pageSize: 25,
      totalCount: 1,
      hasNextPage: false,
      hasPreviousPage: false
    });
    cancelPlatformTenantOnboardingMock.mockResolvedValue({
      data: {
        ...pending,
        onboardingStatus: "Cancelled",
        tenantStatus: "Archived",
        invitationStatus: "Revoked",
        invitationDeliveryStatus: "Cancelled",
        cancelledAt: "2026-07-23T12:00:00Z",
        cancelledByUserId: "22222222-2222-2222-2222-222222222222",
        cancellationReason: "Duplicate pilot onboarding.",
        subscription: null
      },
      error: null
    });

    render(<PlatformTenantAdminPage />);
    await screen.findByText("Duplicate Pilot");
    await user.click(screen.getByRole("button", { name: "Cancel onboarding for Duplicate Pilot" }));

    const confirm = screen.getByRole("button", { name: "Confirm cancellation" });
    expect(confirm).toBeDisabled();
    await user.type(screen.getByLabelText("Cancellation reason"), "Duplicate pilot onboarding.");
    expect(confirm).toBeEnabled();
    await user.click(confirm);

    expect(cancelPlatformTenantOnboardingMock).toHaveBeenCalledWith(
      pending.onboardingId,
      "Duplicate pilot onboarding."
    );
  });

  it("extends an active pilot using the current subscription version", async () => {
    const user = userEvent.setup();
    const activePilot = {
      onboardingId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2",
      tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2",
      displayName: "Lifecycle Pilot",
      onboardingType: "Pilot",
      onboardingStatus: "Active",
      tenantStatus: "Trialing",
      dataHandlingMode: "NoCui",
      customerReference: "PILOT-LIFECYCLE",
      ownerEmail: "owner@example.com",
      ownerDisplayName: "Pilot Owner",
      ownerRoleName: "Owner",
      invitationId: "cccccccc-cccc-cccc-cccc-ccccccccccc2",
      invitationStatus: "Accepted",
      invitationDeliveryStatus: "Sent",
      invitationNotificationSentAt: "2026-08-01T12:00:00Z",
      invitationExpiresAt: "2026-08-08T12:00:00Z",
      trialEndsAt: "2026-08-31",
      planCode: null,
      subscriptionReference: null,
      setupReason: "Approved pilot.",
      createdAt: "2026-08-01T12:00:00Z",
      cancelledAt: null,
      cancelledByUserId: null,
      cancellationReason: null,
      subscription: {
        id: "dddddddd-dddd-dddd-dddd-ddddddddddd2",
        tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2",
        tenantKind: "ContractorWorkspace",
        plan: "PilotEvaluation",
        planCode: "PILOT-EVALUATION",
        status: "Active",
        effectiveStatus: "Active",
        accessLevel: "Full",
        startsAt: "2026-08-01T12:00:00Z",
        endsAt: "2026-09-01T00:00:00Z",
        graceEndsAt: "2026-09-08T00:00:00Z",
        externalCustomerReference: "PILOT-LIFECYCLE",
        externalSubscriptionReference: null,
        statusReason: "Approved pilot.",
        version: 1
      },
      isReplay: false
    };
    getPlatformTenantOnboardingsMock.mockImplementation((_page, _pageSize, status) => Promise.resolve({
      items: status === "Active" ? [activePilot] : [],
      page: 1,
      pageSize: 25,
      totalCount: status === "Active" ? 1 : 0,
      hasNextPage: false,
      hasPreviousPage: false
    }));
    extendPlatformPilotSubscriptionMock.mockResolvedValue({
      data: { ...activePilot.subscription, endsAt: "2026-09-16T00:00:00Z", graceEndsAt: "2026-09-23T00:00:00Z", version: 2 },
      error: null
    });

    render(<PlatformTenantAdminPage />);
    await user.click(await screen.findByText(/Lifecycle Pilot/));
    await user.type(screen.getByLabelText("New pilot end date"), "2026-09-15");
    await user.type(screen.getByLabelText("Required reason"), "Approved extension.");
    await user.click(screen.getByRole("button", { name: "Extend" }));

    expect(extendPlatformPilotSubscriptionMock).toHaveBeenCalledWith(
      activePilot.tenantId,
      "2026-09-15",
      "Approved extension.",
      1,
      expect.stringMatching(/^[0-9a-f-]{36}$/i)
    );
    expect(await screen.findByText("Subscription extend completed.")).toBeInTheDocument();
  });
});
