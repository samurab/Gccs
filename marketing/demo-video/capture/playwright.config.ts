import {defineConfig} from "@playwright/test";
import {dirname, resolve} from "node:path";
import {fileURLToPath} from "node:url";

const projectRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");

export default defineConfig({
  testDir: projectRoot,
  testMatch: "capture/walkthrough.spec.ts",
  timeout: 15 * 60 * 1000,
  expect: {timeout: 15_000},
  fullyParallel: false,
  workers: 1,
  forbidOnly: true,
  globalTeardown: resolve(projectRoot, "capture/global-teardown.ts"),
  reporter: [["line"]],
  outputDir: resolve(projectRoot, "capture/results"),
  use: {
    baseURL: "http://127.0.0.1:5175",
    viewport: {width: 1920, height: 1080},
    colorScheme: "light",
    locale: "en-US",
    headless: true,
    actionTimeout: 15_000,
    navigationTimeout: 30_000,
    screenshot: "off",
    trace: "off",
    video: "off",
    launchOptions: {
      args: ["--disable-notifications", "--hide-scrollbars"]
    }
  },
  webServer: {
    command: "bash ./scripts/start-demo.sh",
    cwd: projectRoot,
    url: "http://127.0.0.1:5175/",
    reuseExistingServer: false,
    timeout: 10 * 60 * 1000,
    stdout: "ignore",
    stderr: "ignore"
  }
});
