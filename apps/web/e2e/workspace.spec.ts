import { expect, test, type Page } from "@playwright/test";

const access = {
  tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
  userId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
  userEmail: "admin@example.com",
  roles: ["Admin"],
  permissions: [
    "ManageUsers",
    "ManageCompanyProfile",
    "ManageContracts",
    "ManageObligations",
    "ManageTasks",
    "ManageEvidence",
    "ManageCmmc",
    "ManageSubcontractors",
    "ManageReports",
    "ManageTenant",
    "ViewAuditLog"
  ],
  rolePermissionMatrix: {}
};

const overview = {
  productPromise: "Keep every govcon obligation tied to evidence and review status.",
  mvpDataPosture: "No-CUI / compliance management only",
  modules: [
    {
      key: "company-profile",
      name: "Company compliance profile",
      purpose: "Capture entity, SAM, NAICS, certification, and data posture details.",
      status: "seeded"
    },
    {
      key: "obligations",
      name: "Obligation dashboard",
      purpose: "Map clauses to actions, owners, evidence, deadlines, and source links.",
      status: "seeded"
    }
  ],
  priorityObligations: [
    {
      id: "far-52-204-21",
      source: "FAR 52.204-21",
      title: "Basic Safeguarding of Covered Contractor Information Systems",
      ownerFunction: "IT/security",
      riskLevel: "High",
      sourceUrl: "https://www.acquisition.gov/far/52.204-21",
      lastReviewedAt: "2026-06-03"
    }
  ]
};

test.beforeEach(async ({ page }) => {
  await mockApi(page);
});

test("loads the compliance workspace dashboard", async ({ page }) => {
  await page.goto("/");

  await expect(page).toHaveTitle("GCCS Compliance Workspace");
  await expect(page.getByRole("heading", { name: "Dashboard", exact: true })).toBeVisible();
  await expect(page.getByText("No-CUI workspace")).toBeVisible();
  await expect(page.getByText("Keep every govcon obligation tied to evidence and review status.")).toBeVisible();
  await expect(page.getByRole("navigation", { name: "Primary workspace navigation" })).toContainText("Contracts");
});

test("navigates between key MVP workspaces", async ({ page }) => {
  await page.goto("/");

  await page.getByRole("link", { name: /Contracts/ }).click();
  await expect(page.getByRole("heading", { name: "Contracts", exact: true })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Create contract record" })).toBeVisible();

  await page.getByRole("link", { name: /Evidence/ }).click();
  await expect(page.getByRole("heading", { name: "Evidence", exact: true })).toBeVisible();
  await expect(page.getByRole("heading", { name: "No-CUI evidence management" })).toBeVisible();

  await page.getByRole("link", { name: /CMMC/ }).click();
  await expect(page.getByRole("heading", { name: "CMMC", exact: true })).toBeVisible();
  await expect(page.getByRole("region", { name: "CMMC readiness workspace" })).toBeVisible();
});

async function mockApi(page: Page) {
  await page.route("**/api/**", async (route) => {
    const url = new URL(route.request().url());
    const path = url.pathname;

    if (path === "/api/compliance/overview") {
      await route.fulfill({ json: overview });
      return;
    }

    if (path === "/api/me/access") {
      await route.fulfill({ json: access });
      return;
    }

    if (path === "/api/no-cui-acknowledgement") {
      await route.fulfill({
        json: {
          isAcknowledged: false,
          noticeVersion: "no-cui-mvp-v1",
          noticeCopy:
            "The GCCS MVP is compliance management only and is not ready to store CUI. Do not upload CUI or other prohibited sensitive content.",
          tenantId: access.tenantId,
          acknowledgedByUserId: null,
          acknowledgedAt: null
        }
      });
      return;
    }

    if (path === "/api/audit-logs") {
      await route.fulfill({
        json: {
          items: [],
          page: 1,
          pageSize: 5,
          totalCount: 0,
          hasNextPage: false,
          hasPreviousPage: false
        }
      });
      return;
    }

    if (path === "/api/notification-preferences") {
      await route.fulfill({ json: null });
      return;
    }

    await route.fulfill({ json: [] });
  });
}
