import { App } from "./App";
import { AuthGate } from "./auth";
import { InvitationAcceptancePage } from "./InvitationAcceptancePage";
import { PlatformTenantAdminPage } from "./PlatformTenantAdminPage";
import {
  shouldRenderInvitationAcceptancePage,
  shouldRenderPlatformTenantAdminPage
} from "./routing";

export function AuthenticatedWorkspace() {
  const page = shouldRenderInvitationAcceptancePage() ? (
    <InvitationAcceptancePage />
  ) : shouldRenderPlatformTenantAdminPage() ? (
    <PlatformTenantAdminPage />
  ) : (
    <App />
  );

  return <AuthGate>{page}</AuthGate>;
}
