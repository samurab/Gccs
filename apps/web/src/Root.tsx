import { lazy, Suspense } from "react";
import { shouldRenderLandingPage } from "./routing";

const LandingPage = lazy(() =>
  import("./LandingPage").then((module) => ({ default: module.LandingPage }))
);
const AuthenticatedWorkspace = lazy(() =>
  import("./AuthenticatedWorkspace").then((module) => ({ default: module.AuthenticatedWorkspace }))
);

export function Root() {
  return (
    <Suspense fallback={<main role="status">Loading FeDril…</main>}>
      {shouldRenderLandingPage() ? <LandingPage /> : <AuthenticatedWorkspace />}
    </Suspense>
  );
}
