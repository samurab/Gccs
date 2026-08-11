import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { PlatformDemoRequestsPage } from "./PlatformDemoRequestsPage";
import * as api from "./lib/api";

vi.mock("./lib/api", async importOriginal => ({ ...(await importOriginal<typeof api>()), getPlatformAccess: vi.fn(), getPlatformDemoRequestCalendar: vi.fn(), getPlatformDemoRequests: vi.fn(), queuePlatformDemoRequestResponse: vi.fn() }));
beforeEach(() => {
  vi.mocked(api.getPlatformDemoRequestCalendar).mockResolvedValue({ items: [], from: "2026-08-01T04:00:00Z", to: "2026-09-01T04:00:00Z" });
});
afterEach(() => { cleanup(); vi.resetAllMocks(); vi.useRealTimers(); });

describe("PlatformDemoRequestsPage", () => {
  it("fails closed without the dedicated permission", async () => {
    vi.mocked(api.getPlatformAccess).mockResolvedValue({ userId: "1", userEmail: "operator@example.com", canProvisionTenants: true, canManageDemoRequests: false, permissions: ["ProvisionTenants"] });
    render(<PlatformDemoRequestsPage />);
    expect(await screen.findByRole("heading", { name: /access denied/i })).toBeInTheDocument();
    expect(api.getPlatformDemoRequests).not.toHaveBeenCalled();
  });

  it("shows captured requests and delivery status to authorized operators", async () => {
    vi.mocked(api.getPlatformAccess).mockResolvedValue({ userId: "1", userEmail: "operator@example.com", canProvisionTenants: false, canManageDemoRequests: true, demoRequestDeliveryMode: "ExternalEmail", permissions: ["ManageDemoRequests"] });
    vi.mocked(api.getPlatformDemoRequests).mockResolvedValue({ page: 1, pageSize: 25, totalCount: 1, hasNextPage: false, hasPreviousPage: false, items: [{ id: "r1", firstName: "Avery", lastName: "Ng", email: "avery@example.com", phone: null, company: "Northstar Systems", referralSource: null, employeeCount: "11-50", message: "Readiness workflow", preferredStartAt: "2026-08-04T18:00:00Z", preferredTimeZone: "America/New_York", receivedAt: "2026-08-02T22:00:00Z", deliveryStatus: "Captured", deliveryAttemptCount: 1, nextDeliveryAttemptAt: null, sentAt: null, deliveryFailureCode: null, acknowledgementStatus: "Captured" }] });
    render(<PlatformDemoRequestsPage />);
    expect(await screen.findByRole("heading", { name: "Northstar Systems" })).toBeInTheDocument();
    expect(screen.getAllByText("Captured")).toHaveLength(2);
    expect(screen.getByText("Readiness workflow")).toBeInTheDocument();
  });

  it("confirms and queues only a selected server-owned response template", async () => {
    vi.spyOn(window, "confirm").mockReturnValue(true);
    vi.mocked(api.getPlatformAccess).mockResolvedValue({ userId: "1", userEmail: "operator@example.com", canProvisionTenants: false, canManageDemoRequests: true, demoRequestDeliveryMode: "ExternalEmail", permissions: ["ManageDemoRequests"] });
    vi.mocked(api.getPlatformDemoRequests).mockResolvedValue({ page: 1, pageSize: 25, totalCount: 1, hasNextPage: false, hasPreviousPage: false, items: [{ id: "r1", firstName: "Avery", lastName: "Ng", email: "avery@example.com", phone: null, company: "Northstar Systems", referralSource: null, employeeCount: "11-50", message: null, preferredStartAt: "2026-08-04T18:00:00Z", preferredTimeZone: "America/New_York", receivedAt: "2026-08-02T22:00:00Z", deliveryStatus: "Queued", deliveryAttemptCount: 0, nextDeliveryAttemptAt: null, sentAt: null, deliveryFailureCode: null, acknowledgementStatus: "Queued" }] });
    vi.mocked(api.queuePlatformDemoRequestResponse).mockResolvedValue({ data: { status: "Queued", templateKey: "RequestMoreDetails", queuedAt: "2026-08-02T23:00:00Z" }, error: null });
    render(<PlatformDemoRequestsPage />);
    const select = await screen.findByLabelText(/response template/i);
    fireEvent.change(select, { target: { value: "RequestMoreDetails" } });
    fireEvent.click(screen.getByRole("button", { name: /queue response/i }));
    expect(api.queuePlatformDemoRequestResponse).toHaveBeenCalledWith("r1", "RequestMoreDetails");
    expect(await screen.findByRole("status")).toHaveTextContent(/response queued/i);
  });

  it("labels development responses as captured and does not claim an email was sent", async () => {
    vi.spyOn(window, "confirm").mockReturnValue(true);
    vi.mocked(api.getPlatformAccess).mockResolvedValue({ userId: "1", userEmail: "operator@example.com", canProvisionTenants: false, canManageDemoRequests: true, demoRequestDeliveryMode: "DevelopmentCapture", permissions: ["ManageDemoRequests"] });
    vi.mocked(api.getPlatformDemoRequests).mockResolvedValue({ page: 1, pageSize: 25, totalCount: 1, hasNextPage: false, hasPreviousPage: false, items: [{ id: "r1", firstName: "Avery", lastName: "Ng", email: "avery@example.com", phone: null, company: "Northstar Systems", referralSource: null, employeeCount: "11-50", message: null, preferredStartAt: "2026-08-04T18:00:00Z", preferredTimeZone: "America/New_York", receivedAt: "2026-08-02T22:00:00Z", deliveryStatus: "Captured", deliveryAttemptCount: 1, nextDeliveryAttemptAt: null, sentAt: null, deliveryFailureCode: null, acknowledgementStatus: "Captured" }] });
    vi.mocked(api.queuePlatformDemoRequestResponse).mockResolvedValue({ data: { status: "Queued", templateKey: "ReviewingRequestedTime", queuedAt: "2026-08-02T23:00:00Z" }, error: null });

    render(<PlatformDemoRequestsPage />);
    expect(await screen.findByText(/without sending email/i)).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: /capture response/i }));

    expect(await screen.findByRole("status")).toHaveTextContent(/no email will be sent/i);
    expect(screen.queryByText(/queued for email delivery/i)).not.toBeInTheDocument();
  });

  it("shows requested-time counts and a daily agenda without claiming confirmation", async () => {
    const now = new Date();
    const requestedAt = new Date(now.getFullYear(), now.getMonth(), 11, 10, 0, 0);
    const rangeFrom = new Date(now.getFullYear(), now.getMonth(), 1).toISOString();
    const rangeTo = new Date(now.getFullYear(), now.getMonth() + 1, 1).toISOString();
    vi.mocked(api.getPlatformAccess).mockResolvedValue({ userId: "1", userEmail: "operator@example.com", canProvisionTenants: false, canManageDemoRequests: true, permissions: ["ManageDemoRequests"] });
    vi.mocked(api.getPlatformDemoRequests).mockResolvedValue({ page: 1, pageSize: 25, totalCount: 0, hasNextPage: false, hasPreviousPage: false, items: [] });
    vi.mocked(api.getPlatformDemoRequestCalendar).mockResolvedValue({
      from: rangeFrom,
      to: rangeTo,
      items: [{ id: "r-calendar", firstName: "Avery", lastName: "Ng", company: "Calendar Company", preferredStartAt: requestedAt.toISOString(), preferredTimeZone: "America/New_York", receivedAt: rangeFrom, deliveryStatus: "Sent", schedulingStatus: "Requested" }],
    });

    render(<PlatformDemoRequestsPage />);
    const requestedDayLabel = `${requestedAt.toLocaleDateString(undefined, { month: "long", day: "numeric" })}: 1 requested demo`;
    const requestedDay = await screen.findByRole("button", { name: requestedDayLabel });
    expect(screen.getByText(/customer-requested times, not confirmed appointments/i)).toBeInTheDocument();
    fireEvent.click(requestedDay);

    expect(screen.getByRole("heading", { name: requestedAt.toLocaleDateString(undefined, { weekday: "long", month: "long", day: "numeric" }) })).toBeInTheDocument();
    expect(screen.getByText("Calendar Company")).toBeInTheDocument();
    expect(screen.getByText("Requested")).toBeInTheDocument();
    expect(screen.queryByText("Confirmed")).not.toBeInTheDocument();
  });

  it("loads a new calendar month without reloading the paginated inbox", async () => {
    vi.mocked(api.getPlatformAccess).mockResolvedValue({ userId: "1", userEmail: "operator@example.com", canProvisionTenants: false, canManageDemoRequests: true, permissions: ["ManageDemoRequests"] });
    vi.mocked(api.getPlatformDemoRequests).mockResolvedValue({ page: 1, pageSize: 25, totalCount: 0, hasNextPage: false, hasPreviousPage: false, items: [] });

    render(<PlatformDemoRequestsPage />);
    await screen.findByRole("heading", { name: /no demo requests/i });
    expect(api.getPlatformDemoRequests).toHaveBeenCalledTimes(1);
    expect(api.getPlatformDemoRequestCalendar).toHaveBeenCalledTimes(1);

    fireEvent.click(screen.getByRole("button", { name: /next month/i }));
    await waitFor(() => expect(api.getPlatformDemoRequestCalendar).toHaveBeenCalledTimes(2));
    expect(api.getPlatformDemoRequests).toHaveBeenCalledTimes(1);
    expect(api.getPlatformAccess).toHaveBeenCalledTimes(1);
  });
});
