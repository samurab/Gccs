import path from "node:path";
import { defineConfig, devices } from "@playwright/test";

function requireStagingUrl(name: string) {
  const value = process.env[name];
  if (!value) throw new Error(`${name} is required.`);

  const url = new URL(value);
  if (url.protocol !== "https:" || ["localhost", "127.0.0.1"].includes(url.hostname)) {
    throw new Error(`${name} must be a deployed HTTPS URL.`);
  }

  return value.replace(/\/$/, "");
}

const baseURL = requireStagingUrl("PLAYWRIGHT_STAGING_WEB_URL");
const authDirectory = path.resolve(process.env.PLAYWRIGHT_STAGING_AUTH_DIR ?? ".auth");

export default defineConfig({
  testDir: "./apps/web/e2e-staging",
  fullyParallel: false,
  forbidOnly: true,
  retries: 0,
  workers: 1,
  timeout: 120_000,
  reporter: "list",
  outputDir: "output/playwright/staging-results",
  preserveOutput: "never",
  use: {
    baseURL,
    screenshot: "off",
    trace: "off",
    video: "off"
  },
  projects: [
    {
      name: "staging-manager",
      testMatch: "**/manager-upload-report.spec.ts",
      metadata: { authStatePath: path.join(authDirectory, "staging-manager.json") },
      use: {
        ...devices["Desktop Chrome"],
        storageState: path.join(authDirectory, "staging-manager.json")
      }
    },
    {
      name: "staging-auditor",
      testMatch: "**/auditor-upload-report.spec.ts",
      metadata: { authStatePath: path.join(authDirectory, "staging-auditor.json") },
      use: {
        ...devices["Desktop Chrome"],
        storageState: path.join(authDirectory, "staging-auditor.json")
      }
    }
  ]
});
