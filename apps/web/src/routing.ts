const workspacePath = "/app";
const landingPath = "/landing";
const platformTenantAdminPath = "/platform/tenants/new";
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

export function shouldRenderPlatformTenantAdminPage(
  location: Pick<Location, "pathname"> = window.location
) {
  return location.pathname === platformTenantAdminPath;
}

export function shouldRenderInvitationAcceptancePage(
  location: Pick<Location, "pathname"> = window.location
) {
  return location.pathname === invitationAcceptancePath;
}
