const workspacePath = "/app";
const landingPath = "/landing";
const demoPath = "/demo";
const demoRequestDetailsPath = "/demo-request-details";
const platformAdminPath = "/platform";
const platformTenantAdminPath = "/platform/tenants/new";
const platformDemoRequestsPath = "/platform/demo-requests";
const platformCustomersPath = "/platform/customers";
const invitationAcceptancePath = "/invitations/accept";

export function getWorkspaceUrl(origin = window.location.origin) {
  return `${origin}${workspacePath}`;
}

export function getNotificationOpenUrl(linkUrl: string) {
  const normalizedLinkUrl = linkUrl.startsWith("/#/")
    ? `${workspacePath}${linkUrl.slice(1)}`
    : linkUrl;

  return normalizedLinkUrl.startsWith(`${workspacePath}#/`)
    ? normalizedLinkUrl
    : `/api${normalizedLinkUrl}`;
}

export function shouldRenderLandingPage(location: Pick<Location, "pathname" | "search" | "hash"> = window.location) {
  const searchParams = new URLSearchParams(location.search);

  if (searchParams.get("view") === "landing") {
    return true;
  }

  if (location.pathname === landingPath) {
    return true;
  }

  if (location.pathname === "/") {
    return true;
  }

  return false;
}

export function shouldRenderDemoPage(location: Pick<Location, "pathname"> = window.location) {
  return location.pathname === demoPath;
}

export function shouldRenderDemoRequestDetailsPage(location: Pick<Location, "pathname"> = window.location) {
  return location.pathname === demoRequestDetailsPath;
}

export function shouldRenderPlatformAdminPage(location: Pick<Location, "pathname"> = window.location) {
  return location.pathname === platformAdminPath;
}

export function shouldRenderPlatformTenantAdminPage(
  location: Pick<Location, "pathname"> = window.location
) {
  return location.pathname === platformTenantAdminPath;
}

export function shouldRenderPlatformDemoRequestsPage(location: Pick<Location, "pathname"> = window.location) {
  return location.pathname === platformDemoRequestsPath;
}

export function shouldRenderPlatformCustomersPage(location: Pick<Location, "pathname"> = window.location) {
  return location.pathname === platformCustomersPath;
}

export function getPlatformCustomerTenantId(location: Pick<Location, "pathname"> = window.location): string | null {
  const match = location.pathname.match(/^\/platform\/customers\/([0-9a-f-]{36})$/i);
  return match?.[1] ?? null;
}

export function shouldRenderPlatformCustomerDetailPage(location: Pick<Location, "pathname"> = window.location) {
  return getPlatformCustomerTenantId(location) !== null;
}

export function shouldRenderInvitationAcceptancePage(
  location: Pick<Location, "pathname"> = window.location
) {
  return location.pathname === invitationAcceptancePath;
}
