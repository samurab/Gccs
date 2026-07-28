import { defineConfig, devices } from "@playwright/test";

const baseURL = process.env.PLAYWRIGHT_BASE_URL ?? "http://127.0.0.1:5174";
const apiURL = process.env.PLAYWRIGHT_API_URL ?? "http://127.0.0.1:5063";

export default defineConfig({
  testDir: "./apps/web/e2e-real",
  fullyParallel: false,
  forbidOnly: Boolean(process.env.CI),
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  reporter: process.env.CI ? [["list"], ["html", { open: "never", outputFolder: "output/playwright/report" }]] : "list",
  outputDir: "output/playwright/results",
  use: {
    baseURL,
    trace: "retain-on-failure",
    screenshot: "only-on-failure"
  },
  webServer: [
    {
      command: "dotnet run --project apps/api --configuration Release --no-build --no-launch-profile",
      port: Number(new URL(apiURL).port),
      reuseExistingServer: !process.env.CI,
      timeout: 120_000,
      env: {
        ASPNETCORE_ENVIRONMENT: "Development",
        ASPNETCORE_URLS: apiURL,
        Cors__AllowedOrigins__0: baseURL,
        LocalDependencies__Enabled: "false",
        LocalDevelopment__SeedData__Enabled: "true"
      }
    },
    {
      command: "npm --workspace apps/web run dev -- --host 127.0.0.1 --port 5174",
      url: baseURL,
      reuseExistingServer: !process.env.CI,
      timeout: 120_000,
      env: {
        VITE_API_BASE_URL: apiURL
      }
    }
  ],
  projects: [
    {
      name: "chromium-real-stack",
      use: { ...devices["Desktop Chrome"] }
    }
  ]
});
