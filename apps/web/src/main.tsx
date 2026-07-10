import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { App } from "./App";
import { AuthGate } from "./auth";
import { LandingPage } from "./LandingPage";
import "../styles/globals.css";

const searchParams = new URLSearchParams(window.location.search);
const isLandingPage = window.location.pathname === "/landing" || searchParams.get("view") === "landing";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    {isLandingPage ? (
      <LandingPage />
    ) : (
      <AuthGate>
        <App />
      </AuthGate>
    )}
  </StrictMode>
);
