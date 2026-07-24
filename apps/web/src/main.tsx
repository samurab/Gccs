import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { App } from "./App";
import { AuthGate } from "./auth";
import { LandingPage } from "./LandingPage";
import { PlatformTenantAdminPage } from "./PlatformTenantAdminPage";
import { InvitationAcceptancePage } from "./InvitationAcceptancePage";
import { shouldRenderInvitationAcceptancePage, shouldRenderLandingPage, shouldRenderPlatformTenantAdminPage } from "./routing";
import "../styles/globals.css";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    {shouldRenderLandingPage() ? (
      <LandingPage />
    ) : shouldRenderInvitationAcceptancePage() ? (
      <AuthGate>
        <InvitationAcceptancePage />
      </AuthGate>
    ) : shouldRenderPlatformTenantAdminPage() ? (
      <AuthGate>
        <PlatformTenantAdminPage />
      </AuthGate>
    ) : (
      <AuthGate>
        <App />
      </AuthGate>
    )}
  </StrictMode>
);
