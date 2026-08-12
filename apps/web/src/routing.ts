const workspacePath = "/app";
const landingPath = "/landing";
const demoPath = "/demo";
const demoRequestDetailsPath = "/demo-request-details";
const platformAdminPath = "/platform";
const platformTenantAdminPath = "/platform/tenants/new";
const platformDemoRequestsPath = "/platform/demo-requests";
const invitationAcceptancePath = "/invitations/accept";

export function getWorkspaceUrl(origin = window.location.origin) {
  return `${origin}${workspacePath}`;
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

export function shouldRenderInvitationAcceptancePage(
  location: Pick<Location, "pathname"> = window.location
) {
  return location.pathname === invitationAcceptancePath;
}
