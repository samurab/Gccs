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
  ],
  alerts: []
};

test.beforeEach(async ({ page }) => {
  await mockApi(page);
});

test("loads the compliance workspace dashboard", async ({ page }) => {
  await page.goto("/app");

  await expect(page).toHaveTitle("FeDril | GovCon Compliance Readiness Software");
  await expect(page.getByRole("heading", { name: "Dashboard", exact: true })).toBeVisible();
  await expect(page.getByText("No-CUI workspace")).toBeVisible();
  await expect(page.getByText("Keep every govcon obligation tied to evidence and review status.")).toBeVisible();
  await expect(page.getByRole("navigation", { name: "Primary workspace navigation" })).toContainText("Contracts");
});

test("navigates between key MVP workspaces", async ({ page }) => {
  await page.goto("/app");

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

test("generates, edits, and compares a draft-only SSP narrative", async ({ page }) => {
  const section = {
    id: "11111111-1111-1111-1111-111111111111", tenantId: access.tenantId, sectionType: "ControlImplementationNarratives",
    title: "Access control narrative", owner: "Security", status: "Draft", reviewer: null, reviewDate: null,
    approvalRationale: null, isRequired: true, version: 1, linkedRecords: [],
    sourceReferences: [{ source: "NIST SP 800-171", sourceUrl: "https://csrc.nist.gov", lastReviewedAt: "2026-09-01" }],
    history: [], createdAt: "2026-09-09T00:00:00Z", updatedAt: "2026-09-09T00:00:00Z"
  };
  let narratives: Array<Record<string, unknown> & { id: string; editedText: string | null; reviewerNotes: string | null; sourceRecords: unknown[] }> = [];
  let generationRequest: { sources?: Array<{ sourceType: string; recordId: string }> } = {};
  await page.route("**/api/compliance/ssp/**", async route => {
    const request = route.request(); const path = new URL(request.url()).pathname;
    if (path === "/api/compliance/ssp/sections") return route.fulfill({ json: [section] });
    if (path.endsWith("/narratives") && request.method() === "GET") return route.fulfill({ json: narratives });
    if (path.endsWith("/narratives") && request.method() === "POST") {
      generationRequest = request.postDataJSON();
      narratives = [{ id: "22222222-2222-2222-2222-222222222222", tenantId: access.tenantId, sectionId: section.id,
        generatedText: "MFA is enforced for administrative access.", editedText: null, approvedText: null, status: "Draft",
        aiAssisted: false, draftOnly: true, reviewerNotes: null, reviewerUserId: null, reviewer: null, reviewDate: null, version: 1,
        classification: { classification: "Unclassified", source: "SystemSuggested", confidence: null, reviewedByUserId: null, reviewedAt: null, reason: null, isApprovedDemoContent: false },
        sourceRecords: [
          { sourceType: "Evidence", recordId: "evidence-1", label: "MFA configuration evidence", summary: "MFA is enforced.", sourceUrl: "/evidence?item=evidence-1", fingerprint: "v1", classification: "Unclassified" },
          { sourceType: "GeneratedPolicy", recordId: "policy-1", label: "Access control policy", summary: "Access policy is approved.", sourceUrl: "/policies?policy=policy-1", fingerprint: "v1", classification: "Unclassified" }
        ],
        createdAt: "2026-09-09T00:00:00Z", updatedAt: "2026-09-09T00:00:00Z" }];
      return route.fulfill({ status: 201, json: narratives[0] });
    }
    if (request.method() === "PUT") {
      const body = request.postDataJSON(); narratives[0] = { ...narratives[0], editedText: body.editedText, reviewerNotes: body.reviewerNotes, version: 2 };
      return route.fulfill({ json: narratives[0] });
    }
    if (path.endsWith("/comparison")) return route.fulfill({ json: {
      sectionId: section.id, approvedNarrativeId: null, draftNarrativeId: narratives[0].id, currentApprovedText: null,
      proposedText: narratives[0].editedText, currentApprovedReviewerUserId: null, currentApprovedReviewer: null,
      currentApprovedReviewDate: null, proposedReviewerNotes: narratives[0].reviewerNotes,
      proposedSources: narratives[0].sourceRecords, currentApprovedSources: []
    } });
    return route.fulfill({ status: 404, json: {} });
  });

  await page.goto("/app");
  await page.getByRole("link", { name: /CMMC/ }).click();
  await page.getByRole("button", { name: "Access control narrative" }).click();
  await expect(page.getByRole("region", { name: "SSP narrative builder for Access control narrative" })).toBeVisible();
  await page.getByLabel("Approved source record ID").fill("evidence-1");
  await page.getByRole("button", { name: "Add another source" }).click();
  await page.getByLabel("Source type").selectOption("GeneratedPolicy");
  await page.getByLabel("Approved source record ID").fill("policy-1");
  await page.getByRole("button", { name: "Generate draft" }).click();
  expect(generationRequest.sources).toEqual([
    { sourceType: "Evidence", recordId: "evidence-1" },
    { sourceType: "GeneratedPolicy", recordId: "policy-1" }
  ]);
  await expect(page.getByText("Draft—human review required").first()).toBeVisible();
  await expect(page.getByRole("link", { name: "MFA configuration evidence" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Access control policy" })).toBeVisible();
  await page.getByLabel("Narrative text").fill("MFA is enforced for privileged administrative access.");
  await page.getByLabel("Reviewer notes").fill("Validate the privileged role inventory.");
  await page.getByRole("button", { name: "Save draft" }).click();
  await page.getByRole("button", { name: "Compare with current approved" }).click();
  await expect(page.getByText("No approved narrative exists.")).toBeVisible();
  await expect(page.getByText("Reviewer notes: Validate the privileged role inventory.")).toBeVisible();
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

    if (path === "/api/development/testing-context") {
      await route.fulfill({
        json: {
          tenants: [
            {
              tenantId: access.tenantId,
              displayName: "Playwright tenant",
              tenantStatus: "Active",
              dataHandlingMode: "NoCui",
              isSelectable: true,
              unavailableReason: null
            }
          ],
          personas: [
            {
              tenantId: access.tenantId,
              userId: access.userId,
              email: access.userEmail,
              displayName: "Playwright admin",
              roleName: "Admin"
            }
          ],
          roles: ["Owner", "Admin", "Compliance Manager", "Contributor", "Auditor", "Advisor"]
        }
      });
      return;
    }

    if (path === "/api/no-cui-acknowledgement") {
      await route.fulfill({
        json: {
          isAcknowledged: false,
          noticeVersion: "no-cui-mvp-v1",
          noticeCopy:
            "The FeDril MVP is compliance management only and is not ready to store CUI. Do not upload CUI or other prohibited sensitive content.",
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
