const workspacePath = "/app";
const landingPath = "/landing";

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
