import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { App } from "./App";
import { AuthGate } from "./auth";
import { LandingPage } from "./LandingPage";
import { shouldRenderLandingPage } from "./routing";
import "../styles/globals.css";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    {shouldRenderLandingPage() ? (
      <LandingPage />
    ) : (
      <AuthGate>
        <App />
      </AuthGate>
    )}
  </StrictMode>
);
