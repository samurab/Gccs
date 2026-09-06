import { describe, expect, it } from "vitest";
import { getNotificationOpenUrl, getPlatformAuthenticationUrl, getPlatformCustomerTenantId, getWorkspaceUrl, shouldRenderDemoPage, shouldRenderDemoRequestDetailsPage, shouldRenderLandingPage, shouldRenderPlatformAdminPage, shouldRenderPlatformCustomerDetailPage, shouldRenderPlatformCustomersPage, shouldRenderPlatformDemoRequestsPage } from "./routing";

function locationStub(pathname: string, search = "", hash = ""): Pick<Location, "pathname" | "search" | "hash"> {
  return { pathname, search, hash };
}

describe("routing", () => {
  it("renders the public landing page at root, /landing, and explicit landing query", () => {
    expect(shouldRenderLandingPage(locationStub("/"))).toBe(true);
    expect(shouldRenderLandingPage(locationStub("/landing"))).toBe(true);
    expect(shouldRenderLandingPage(locationStub("/app", "?view=landing"))).toBe(true);
  });

  it("renders root as public landing even when a legacy workspace hash is present", () => {
    expect(shouldRenderLandingPage(locationStub("/", "", "#/dashboard"))).toBe(true);
    expect(shouldRenderLandingPage(locationStub("/", "", "#/reports"))).toBe(true);
  });

  it("keeps /app authenticated workspace routes out of the public landing page", () => {
    expect(shouldRenderLandingPage(locationStub("/app"))).toBe(false);
    expect(shouldRenderLandingPage(locationStub("/app", "", "#/dashboard"))).toBe(false);
    expect(shouldRenderLandingPage(locationStub("/app", "", "#/reports"))).toBe(false);
  });

  it("keeps customer and workforce authentication callbacks on their respective route planes", () => {
    expect(getWorkspaceUrl("https://gccs.example")).toBe("https://gccs.example/app");
    expect(getPlatformAuthenticationUrl("https://gccs.example")).toBe("https://gccs.example/platform");
  });

  it("opens current and legacy workspace notifications inside the authenticated shell", () => {
    expect(getNotificationOpenUrl("/app#/obligations")).toBe("/app#/obligations");
    expect(getNotificationOpenUrl("/#/obligations")).toBe("/app#/obligations");
    expect(getNotificationOpenUrl("/tasks/task-id")).toBe("/api/tasks/task-id");
  });

  it("renders the dedicated public demo only at /demo", () => {
    expect(shouldRenderDemoPage(locationStub("/demo"))).toBe(true);
    expect(shouldRenderDemoPage(locationStub("/"))).toBe(false);
    expect(shouldRenderDemoPage(locationStub("/app"))).toBe(false);
  });

  it("renders the public demo-detail form only at its dedicated path", () => {
    expect(shouldRenderDemoRequestDetailsPage(locationStub("/demo-request-details"))).toBe(true);
    expect(shouldRenderDemoRequestDetailsPage(locationStub("/demo"))).toBe(false);
    expect(shouldRenderDemoRequestDetailsPage(locationStub("/app"))).toBe(false);
  });

  it("routes the protected platform demo-request inbox explicitly", () => {
    expect(shouldRenderPlatformDemoRequestsPage(locationStub("/platform/demo-requests"))).toBe(true);
    expect(shouldRenderPlatformDemoRequestsPage(locationStub("/app"))).toBe(false);
  });

  it("routes the protected platform overview only at the exact platform root", () => {
    expect(shouldRenderPlatformAdminPage(locationStub("/platform"))).toBe(true);
    expect(shouldRenderPlatformAdminPage(locationStub("/platform/demo-requests"))).toBe(false);
    expect(shouldRenderPlatformAdminPage(locationStub("/app"))).toBe(false);
  });

  it("routes the customer directory and UUID detail paths without matching arbitrary segments", () => {
    const tenantId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2";
    expect(shouldRenderPlatformCustomersPage(locationStub("/platform/customers"))).toBe(true);
    expect(shouldRenderPlatformCustomerDetailPage(locationStub(`/platform/customers/${tenantId}`))).toBe(true);
    expect(getPlatformCustomerTenantId(locationStub(`/platform/customers/${tenantId}`))).toBe(tenantId);
    expect(shouldRenderPlatformCustomerDetailPage(locationStub("/platform/customers/not-a-guid"))).toBe(false);
  });
});
