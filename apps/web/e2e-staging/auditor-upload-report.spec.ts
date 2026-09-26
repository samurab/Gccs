import { expect, test } from "@playwright/test";
import { authenticatedApiRequest, authenticatedCollectionIds, openAuthenticatedWorkspace } from "./authenticated-staging";

test("real staging auditor sign-in is read-only for uploads and reports", async ({ page }, testInfo) => {
  const workspace = await openAuthenticatedWorkspace(
    page,
    testInfo,
    ["ViewEvidence", "ViewReports"],
    ["ManageEvidence", "ManageReports"]
  );

  await page.getByRole("link", { name: /Evidence/ }).click();
  await expect(page.getByRole("heading", { name: "No-CUI evidence management" })).toBeVisible();
  const upload = page.getByRole("form", { name: "Upload area" });
  await expect(upload.getByLabel("Evidence file")).toBeDisabled();
  await expect(upload.getByRole("button", { name: "Upload evidence" })).toBeDisabled();

  const evidenceBefore = await authenticatedCollectionIds(page, workspace.apiOrigin, "/api/evidence-items");
  const deniedEvidence = await authenticatedApiRequest(page, workspace.apiOrigin, "POST", "/api/evidence-items", {
    title: `Denied staging auditor evidence ${Date.now()}`,
    description: "This request must not create evidence.",
    type: "Other",
    ownerFunction: "Compliance",
    status: "Draft",
    classification: { classification: "Unclassified" }
  });
  expect(deniedEvidence.status).toBe(403);
  expect(deniedEvidence.body).toContain("permission_denied");
  expect(await authenticatedCollectionIds(page, workspace.apiOrigin, "/api/evidence-items")).toEqual(evidenceBefore);

  await page.getByRole("link", { name: /Reports/ }).click();
  await expect(page.getByText("Your role can view existing reports but cannot generate new reports or evidence packages.")).toBeVisible();
  await expect(page.getByRole("button", { name: "Generate status" })).toHaveCount(0);

  const reportsBefore = await authenticatedCollectionIds(page, workspace.apiOrigin, "/api/reports/recent?limit=25");
  const deniedReport = await authenticatedApiRequest(page, workspace.apiOrigin, "POST", "/api/reports/compliance-status", {
    classification: { classification: "Unclassified" }
  });
  expect(deniedReport.status).toBe(403);
  expect(deniedReport.body).toContain("permission_denied");
  expect(await authenticatedCollectionIds(page, workspace.apiOrigin, "/api/reports/recent?limit=25")).toEqual(reportsBefore);
});
