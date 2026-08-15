import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { PlatformCustomerDetailPage } from "./PlatformCustomerDetailPage";
import { PlatformCustomersPage } from "./PlatformCustomersPage";
import * as api from "./lib/api";

vi.mock("./lib/api", async (importOriginal) => ({
  ...(await importOriginal<typeof api>()),
  extendPlatformPilotSubscription: vi.fn(),
  getPlatformAccess: vi.fn(),
  getPlatformCustomer: vi.fn(),
  getPlatformCustomers: vi.fn(),
  resendPlatformTenantInvitation: vi.fn()
}));

const tenantId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2";
const subscription: api.TenantSubscription = {
  id: "dddddddd-dddd-dddd-dddd-ddddddddddd2",
  tenantId,
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
};
const customer: api.PlatformCustomerSummary = {
  tenantId,
  displayName: "Lifecycle Pilot",
  customerReference: "PILOT-LIFECYCLE",
  customerType: "Pilot",
  tenantStatus: "Trialing",
  dataPosture: "NoCui",
  onboardingStatus: "Active",
  ownerEmail: "owner@example.com",
  invitationStatus: "Accepted",
  invitationDeliveryStatus: "Sent",
  subscription,
  attention: ["PilotExpiring"],
  createdAt: "2026-08-01T12:00:00Z",
  updatedAt: "2026-08-02T12:00:00Z"
};

describe("platform customer operations", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    window.history.pushState({}, "", "/platform/customers");
    vi.mocked(api.getPlatformAccess).mockResolvedValue({
      userId: "operator-1",
      userEmail: "operator@example.com",
      canProvisionTenants: false,
      canViewPlatformCustomers: true,
      canManageTenantOnboarding: false,
      canManageTenantSubscriptions: false,
      canManageDemoRequests: false,
      permissions: ["ViewPlatformCustomers"]
    });
    vi.mocked(api.getPlatformCustomers).mockResolvedValue({ items: [customer], page: 1, pageSize: 25, totalCount: 1, hasNextPage: false, hasPreviousPage: false });
    vi.mocked(api.getPlatformCustomer).mockResolvedValue({
      customer,
      ownerDisplayName: "Pilot Owner",
      invitationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      invitationNotificationSentAt: "2026-08-01T12:01:00Z",
      invitationExpiresAt: "2026-08-08T12:00:00Z",
      invitationAcceptedAt: "2026-08-01T12:02:00Z",
      planCode: null,
      subscriptionReference: null,
      setupReason: "Synthetic pilot.",
      cancelledAt: null,
      cancellationReason: null,
      lifecycle: [{ eventType: "OwnerActivated", summary: "The initial Owner accepted the invitation.", occurredAt: "2026-08-01T12:02:00Z", actorUserId: null }]
    });
  });
  afterEach(() => cleanup());

  it("renders a bounded clickable customer grid and applies server filters", async () => {
    const user = userEvent.setup();
    render(<PlatformCustomersPage />);

    expect(await screen.findByRole("heading", { name: "Customers" })).toBeInTheDocument();
    expect(await screen.findByRole("link", { name: "Lifecycle Pilot" })).toHaveAttribute("href", `/platform/customers/${tenantId}`);
    expect(screen.getAllByText("Pilot expiring")).toHaveLength(2);
    await user.selectOptions(screen.getByLabelText("Type"), "Paid");
    expect(api.getPlatformCustomers).toHaveBeenLastCalledWith(expect.objectContaining({ customerType: "Paid", page: 1 }));
    await user.selectOptions(screen.getByLabelText("Onboarding"), "Active");
    expect(api.getPlatformCustomers).toHaveBeenLastCalledWith(expect.objectContaining({ customerType: "Paid", onboardingStatus: "Active", page: 1 }));
  });

  it("fails closed when customer permission is absent", async () => {
    vi.mocked(api.getPlatformAccess).mockResolvedValue({ userId: "user-1", userEmail: "user@example.com", canProvisionTenants: false, canManageDemoRequests: false, permissions: [] });
    render(<PlatformCustomersPage />);
    expect(await screen.findByRole("heading", { name: "Customer access denied" })).toBeInTheDocument();
    expect(api.getPlatformCustomers).not.toHaveBeenCalled();
  });

  it("keeps subscription controls hidden for a read-only customer operator", async () => {
    window.history.pushState({}, "", `/platform/customers/${tenantId}`);
    render(<PlatformCustomerDetailPage />);
    expect(await screen.findByRole("heading", { name: "Lifecycle Pilot" })).toBeInTheDocument();
    expect(screen.getByText("The initial Owner accepted the invitation.")).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Subscription actions" })).not.toBeInTheDocument();
  });

  it("allows a subscription manager to extend a pilot with version and idempotency", async () => {
    const user = userEvent.setup();
    vi.mocked(api.getPlatformAccess).mockResolvedValue({
      userId: "operator-1", userEmail: "operator@example.com", canProvisionTenants: false,
      canViewPlatformCustomers: true, canManageTenantOnboarding: false, canManageTenantSubscriptions: true,
      canManageDemoRequests: false, permissions: ["ManageTenantSubscriptions"]
    });
    vi.mocked(api.extendPlatformPilotSubscription).mockResolvedValue({ data: { ...subscription, version: 2, endsAt: "2026-09-16T00:00:00Z" }, error: null });
    window.history.pushState({}, "", `/platform/customers/${tenantId}`);
    render(<PlatformCustomerDetailPage />);

    await screen.findByRole("heading", { name: "Subscription actions" });
    await user.type(screen.getByLabelText(/New pilot end date/), "2026-09-15");
    await user.type(screen.getByLabelText("Required reason"), "Approved evaluation extension.");
    await user.click(screen.getByRole("button", { name: "Extend pilot" }));

    expect(api.extendPlatformPilotSubscription).toHaveBeenCalledWith(
      tenantId, "2026-09-15", "Approved evaluation extension.", 1, expect.stringMatching(/^[0-9a-f-]{36}$/i)
    );
  });
});
