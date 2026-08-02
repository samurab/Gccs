import { lazy, Suspense } from "react";
import { shouldRenderDemoPage, shouldRenderLandingPage } from "./routing";

const LandingPage = lazy(() =>
  import("./LandingPage").then((module) => ({ default: module.LandingPage }))
);
const DemoPage = lazy(() =>
  import("./DemoPage").then((module) => ({ default: module.DemoPage }))
);
const AuthenticatedWorkspace = lazy(() =>
  import("./AuthenticatedWorkspace").then((module) => ({ default: module.AuthenticatedWorkspace }))
);

export function Root() {
  return (
    <Suspense fallback={<main role="status">Loading FeDril…</main>}>
      {shouldRenderDemoPage() ? <DemoPage /> : shouldRenderLandingPage() ? <LandingPage /> : <AuthenticatedWorkspace />}
    </Suspense>
  );
}
