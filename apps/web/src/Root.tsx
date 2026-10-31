import { lazy, Suspense } from "react";
import { HubSpotChat } from "./HubSpotChat";
import { shouldOfferHubSpotChat, shouldRenderDemoPage, shouldRenderDemoRequestDetailsPage, shouldRenderLandingPage } from "./routing";

const LandingPage = lazy(() =>
  import("./LandingPage").then((module) => ({ default: module.LandingPage }))
);
const DemoPage = lazy(() =>
  import("./DemoPage").then((module) => ({ default: module.DemoPage }))
);
const DemoRequestDetailsPage = lazy(() =>
  import("./DemoRequestDetailsPage").then((module) => ({ default: module.DemoRequestDetailsPage }))
);
const AuthenticatedWorkspace = lazy(() =>
  import("./AuthenticatedWorkspace").then((module) => ({ default: module.AuthenticatedWorkspace }))
);

export function Root() {
  return (
    <>
      <Suspense fallback={<main role="status">Loading FeDril…</main>}>
        {shouldRenderDemoRequestDetailsPage() ? <DemoRequestDetailsPage /> : shouldRenderDemoPage() ? <DemoPage /> : shouldRenderLandingPage() ? <LandingPage /> : <AuthenticatedWorkspace />}
      </Suspense>
      {shouldOfferHubSpotChat() ? <HubSpotChat /> : null}
    </>
  );
}
