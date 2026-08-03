import { App } from "./App";
import { AuthGate } from "./auth";
import { InvitationAcceptancePage } from "./InvitationAcceptancePage";
import { PlatformTenantAdminPage } from "./PlatformTenantAdminPage";
import { PlatformDemoRequestsPage } from "./PlatformDemoRequestsPage";
import { PlatformAdminHomePage } from "./PlatformAdminHomePage";
import {
  shouldRenderInvitationAcceptancePage,
  shouldRenderPlatformAdminPage,
  shouldRenderPlatformDemoRequestsPage,
  shouldRenderPlatformTenantAdminPage
} from "./routing";

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

  return <AuthGate>{page}</AuthGate>;
}
