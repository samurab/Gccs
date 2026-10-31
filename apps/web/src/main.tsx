import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { Root } from "./Root";
import { getRuntimeReleaseInfo } from "./runtimeConfig";
import "../styles/globals.css";

const releaseInfo = getRuntimeReleaseInfo();
document.documentElement.dataset.appVersion = releaseInfo.version;
if (releaseInfo.commitSha) {
  document.documentElement.dataset.commitSha = releaseInfo.commitSha;
}

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <Root />
  </StrictMode>
);
