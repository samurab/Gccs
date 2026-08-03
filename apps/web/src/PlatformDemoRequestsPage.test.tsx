import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { PlatformDemoRequestsPage } from "./PlatformDemoRequestsPage";
import * as api from "./lib/api";

vi.mock("./lib/api", async importOriginal => ({ ...(await importOriginal<typeof api>()), getPlatformAccess: vi.fn(), getPlatformDemoRequests: vi.fn(), queuePlatformDemoRequestResponse: vi.fn() }));
afterEach(() => { cleanup(); vi.resetAllMocks(); });

describe("PlatformDemoRequestsPage", () => {
  it("fails closed without the dedicated permission", async () => {
    vi.mocked(api.getPlatformAccess).mockResolvedValue({ userId: "1", userEmail: "operator@example.com", canProvisionTenants: true, canManageDemoRequests: false, permissions: ["ProvisionTenants"] });
    render(<PlatformDemoRequestsPage />);
    expect(await screen.findByRole("heading", { name: /access denied/i })).toBeInTheDocument();
    expect(api.getPlatformDemoRequests).not.toHaveBeenCalled();
  });

  it("shows captured requests and delivery status to authorized operators", async () => {
    vi.mocked(api.getPlatformAccess).mockResolvedValue({ userId: "1", userEmail: "operator@example.com", canProvisionTenants: false, canManageDemoRequests: true, permissions: ["ManageDemoRequests"] });
    vi.mocked(api.getPlatformDemoRequests).mockResolvedValue({ page: 1, pageSize: 25, totalCount: 1, hasNextPage: false, hasPreviousPage: false, items: [{ id: "r1", firstName: "Avery", lastName: "Ng", email: "avery@example.com", phone: null, company: "Northstar Systems", referralSource: null, employeeCount: "11-50", message: "Readiness workflow", preferredStartAt: "2026-08-04T18:00:00Z", preferredTimeZone: "America/New_York", receivedAt: "2026-08-02T22:00:00Z", deliveryStatus: "Queued", deliveryAttemptCount: 0, nextDeliveryAttemptAt: null, sentAt: null, deliveryFailureCode: null, acknowledgementStatus: "Queued" }] });
    render(<PlatformDemoRequestsPage />);
    expect(await screen.findByRole("heading", { name: "Northstar Systems" })).toBeInTheDocument();
    expect(screen.getAllByText("Queued")).toHaveLength(2);
    expect(screen.getByText("Readiness workflow")).toBeInTheDocument();
  });

  it("confirms and queues only a selected server-owned response template", async () => {
    vi.spyOn(window, "confirm").mockReturnValue(true);
    vi.mocked(api.getPlatformAccess).mockResolvedValue({ userId: "1", userEmail: "operator@example.com", canProvisionTenants: false, canManageDemoRequests: true, permissions: ["ManageDemoRequests"] });
    vi.mocked(api.getPlatformDemoRequests).mockResolvedValue({ page: 1, pageSize: 25, totalCount: 1, hasNextPage: false, hasPreviousPage: false, items: [{ id: "r1", firstName: "Avery", lastName: "Ng", email: "avery@example.com", phone: null, company: "Northstar Systems", referralSource: null, employeeCount: "11-50", message: null, preferredStartAt: "2026-08-04T18:00:00Z", preferredTimeZone: "America/New_York", receivedAt: "2026-08-02T22:00:00Z", deliveryStatus: "Queued", deliveryAttemptCount: 0, nextDeliveryAttemptAt: null, sentAt: null, deliveryFailureCode: null, acknowledgementStatus: "Queued" }] });
    vi.mocked(api.queuePlatformDemoRequestResponse).mockResolvedValue({ data: { status: "Queued", templateKey: "RequestMoreDetails", queuedAt: "2026-08-02T23:00:00Z" }, error: null });
    render(<PlatformDemoRequestsPage />);
    const select = await screen.findByLabelText(/response template/i);
    fireEvent.change(select, { target: { value: "RequestMoreDetails" } });
    fireEvent.click(screen.getByRole("button", { name: /queue response/i }));
    expect(api.queuePlatformDemoRequestResponse).toHaveBeenCalledWith("r1", "RequestMoreDetails");
    expect(await screen.findByRole("status")).toHaveTextContent(/response queued/i);
  });
});
