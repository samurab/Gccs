import { lazy, Suspense } from "react";
import { AuthGate } from "./auth";
import {
  shouldRenderInvitationAcceptancePage,
  shouldRenderPlatformAdminPage,
  shouldRenderPlatformDemoRequestsPage,
  shouldRenderPlatformTenantAdminPage
} from "./routing";

const App = lazy(() => import("./App").then((module) => ({ default: module.App })));
const InvitationAcceptancePage = lazy(() => import("./InvitationAcceptancePage").then((module) => ({ default: module.InvitationAcceptancePage })));
const PlatformAdminHomePage = lazy(() => import("./PlatformAdminHomePage").then((module) => ({ default: module.PlatformAdminHomePage })));
const PlatformDemoRequestsPage = lazy(() => import("./PlatformDemoRequestsPage").then((module) => ({ default: module.PlatformDemoRequestsPage })));
const PlatformTenantAdminPage = lazy(() => import("./PlatformTenantAdminPage").then((module) => ({ default: module.PlatformTenantAdminPage })));

export function AuthenticatedWorkspace() {
  const page = shouldRenderInvitationAcceptancePage() ? (
    <InvitationAcceptancePage />
  ) : shouldRenderPlatformAdminPage() ? (
    <PlatformAdminHomePage />
  ) : shouldRenderPlatformDemoRequestsPage() ? (
    <PlatformDemoRequestsPage />
  ) : shouldRenderPlatformTenantAdminPage() ? (
    <PlatformTenantAdminPage />
  ) : (
    <App />
  );

  return <AuthGate><Suspense fallback={<main className="platform-console-state">Loading FeDril…</main>}>{page}</Suspense></AuthGate>;
}
