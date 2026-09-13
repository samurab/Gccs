import { expect, test } from "@playwright/test";

const invitationId = "34234234-4234-4234-8234-4234234234aa";
const sharedPackageId = "34234234-4234-4234-8234-4234234234bb";

test("external reviewer reads an approved package, asks a question, and downloads without workspace controls", async ({ page }) => {
  let messageBody: unknown;
  let downloadRequested = false;
  await page.route("**/api/external-portal/**", async route => {
    const request = route.request();
    const path = new URL(request.url()).pathname;
    if (path.endsWith("/messages")) {
      messageBody = request.postDataJSON();
      await route.fulfill({ status: 201, json: {
        id: "34234234-4234-4234-8234-4234234234ee", kind: "Question",
        body: "Which source supports this status?", createdAt: "2026-09-13T13:00:00Z"
      } });
      return;
    }
    if (path.endsWith("/download")) {
      downloadRequested = true;
      await route.fulfill({ status: 200, contentType: "text/html", body: "<main>Watermarked package</main>",
        headers: { "content-disposition": "attachment; filename=package.html" } });
      return;
    }
    await route.fulfill({ json: [{
      sharedPackageId, packageId: "34234234-4234-4234-8234-4234234234cc",
      sourceKind: "Report:CmmcReadiness", title: "Approved CMMC readiness package", version: 2,
      status: "Approved", classification: "Fci", contractId: null,
      evidenceItemIds: ["34234234-4234-4234-8234-4234234234dd"],
      evidenceReferences: [{ id: "34234234-4234-4234-8234-4234234234dd",
        name: "Approved access-control policy", type: "Policy", classification: "Fci",
        approvedAt: "2026-09-12T12:00:00Z", expiresAt: null }],
      generatedAt: "2026-09-12T12:00:00Z", reviewDueAt: "2026-10-01T12:00:00Z",
      externalReviewApprovedAt: "2026-09-12T12:30:00Z", approvedSourceVersion: 2,
      approvedSourceFingerprint: "a".repeat(64), shareState: "Active", downloadAvailable: true,
      reviewerMessages: []
    }] });
  });

  await page.goto(`/portal/review/${invitationId}`);
  await expect(page.getByRole("heading", { name: "Approved CMMC readiness package" })).toBeVisible();
  await page.locator("summary", { hasText: "Evidence references" }).click();
  await expect(page.getByText("Approved access-control policy")).toBeVisible();
  await expect(page.getByRole("button", { name: /edit|delete|approve/i })).toHaveCount(0);

  await page.getByRole("textbox", { name: "Message" }).fill("Which source supports this status?");
  await page.getByRole("button", { name: "Add reviewer message" }).click();
  await expect(page.getByText("Reviewer message saved.")).toBeVisible();
  expect(messageBody).toEqual({ kind: "Question", body: "Which source supports this status?" });

  const download = page.waitForEvent("download");
  await page.getByRole("button", { name: "Download controlled package" }).click();
  await download;
  expect(downloadRequested).toBe(true);
});

test("external reviewer sees controlled empty and denied states", async ({ page }) => {
  await page.route("**/api/external-portal/**", route => route.fulfill({ json: [] }));
  await page.goto(`/portal/review/${invitationId}`);
  await expect(page.getByText("No approved packages are currently assigned to this invitation.")).toBeVisible();

  await page.unroute("**/api/external-portal/**");
  await page.route("**/api/external-portal/**", route => route.fulfill({ status: 403,
    contentType: "application/problem+json", body: JSON.stringify({ detail: "The requested package is unavailable." }) }));
  await page.reload();
  await expect(page.getByRole("alert")).toContainText("unavailable");
});
