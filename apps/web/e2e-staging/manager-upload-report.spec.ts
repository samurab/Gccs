import { expect, test } from "@playwright/test";
import { acknowledgeWorkflowNotice, openAuthenticatedWorkspace } from "./authenticated-staging";

test("real staging manager sign-in can upload synthetic evidence and generate a report", async ({ page }, testInfo) => {
  const workspace = await openAuthenticatedWorkspace(page, testInfo, ["ManageEvidence", "ManageReports", "ViewReports"]);

  await page.getByRole("link", { name: /Evidence/ }).click();
  await expect(page.getByRole("heading", { name: "No-CUI evidence management" })).toBeVisible();

  const noCuiButton = page.getByRole("button", { name: "I acknowledge the No-CUI upload limitation" });
  if (await noCuiButton.count()) {
    const checklist = page.getByRole("group", { name: "Required user acknowledgement" });
    for (const checkbox of await checklist.getByRole("checkbox").all()) await checkbox.check();
    await noCuiButton.click();
    await expect(page.getByText("Acknowledgement saved.")).toBeVisible();
  }
  await acknowledgeWorkflowNotice(page, "Evidence upload");

  await page.getByRole("button", { name: "New evidence" }).click();
  const metadata = page.getByRole("region", { name: "Evidence metadata" });
  const unique = Date.now();
  await metadata.getByLabel("Title").fill(`Synthetic staging auth evidence ${unique}`);
  await metadata.getByLabel("Description").fill("Synthetic No-CUI staging authentication and permission smoke evidence.");
  await metadata.getByLabel("Classification", { exact: true }).selectOption("Unclassified");
  await metadata.getByRole("button", { name: "Create metadata" }).click();
  await expect(page.getByText("Evidence metadata created.")).toBeVisible();

  const upload = page.getByRole("form", { name: "Upload area" });
  await upload.getByLabel("Evidence file").setInputFiles({
    name: `synthetic-staging-auth-${unique}.txt`,
    mimeType: "text/plain",
    buffer: Buffer.from("Synthetic No-CUI staging authentication test data.")
  });
  await upload.getByLabel("Upload classification", { exact: true }).selectOption("Unclassified");
  await upload.getByLabel(/I confirm this file does not contain CUI/).check();
  const uploadResponsePromise = page.waitForResponse(
    response => response.url().startsWith(`${workspace.apiOrigin}/api/evidence-items/`) && response.url().endsWith("/file") && response.request().method() === "POST"
  );
  await upload.getByRole("button", { name: "Upload evidence" }).click();
  const uploadResponse = await uploadResponsePromise;
  const uploadedVersion = await uploadResponse.json();
  const uploadErrorCode = typeof uploadedVersion?.errorCode === "string" ? uploadedVersion.errorCode : "unexpected_response";
  expect(uploadResponse.status(), `staging evidence upload must return 201 (${uploadErrorCode})`).toBe(201);
  expect(uploadedVersion).toMatchObject({
    fileName: `synthetic-staging-auth-${unique}.txt`,
    validationStatus: "accepted",
    malwareScanStatus: "clean",
    isUsable: true
  });

  await page.getByRole("link", { name: /Reports/ }).click();
  await expect(page.getByRole("heading", { name: "Reports and audit packages" })).toBeVisible();
  await acknowledgeWorkflowNotice(page, "Report generation");
  await page.getByLabel("Workflow classification").selectOption("Unclassified");
  const reportResponsePromise = page.waitForResponse(
    response => response.url() === `${workspace.apiOrigin}/api/reports/compliance-status` && response.request().method() === "POST"
  );
  await page.getByRole("button", { name: "Generate status" }).click();
  const reportResponse = await reportResponsePromise;
  expect(reportResponse.status(), "staging compliance-status generation must return 201").toBe(201);
  await expect(page.getByText("Compliance status report generated.")).toBeVisible();
});
