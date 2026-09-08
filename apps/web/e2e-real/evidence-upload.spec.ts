import { expect, test } from "@playwright/test";

const apiURL = process.env.PLAYWRIGHT_API_URL ?? "http://127.0.0.1:5063";
const tenantId = "11111111-1111-1111-1111-111111111111";
const manager = {
  role: "Compliance Manager",
  userId: "22222222-2222-2222-2222-222222222223",
  email: "alpha.compliancemanager@gccs.local"
};

function headers() {
  return {
    "X-Gccs-Dev-Auth": "true",
    "X-Gccs-Dev-Tenant": tenantId,
    "X-Gccs-Tenant": tenantId,
    "X-Gccs-Dev-User": manager.userId,
    "X-Gccs-Dev-Email": manager.email,
    "X-Gccs-Dev-Role": manager.role
  };
}

test("Story 12.2 creates evidence metadata, uploads bytes, and retrieves the PostgreSQL-backed version", async ({
  page,
  request
}) => {
  const noCuiAcknowledgement = await request.post(`${apiURL}/api/no-cui-acknowledgement`, {
    headers: headers(),
    data: {
      acknowledged: true,
      noticeVersion: "no-cui-mvp-v1"
    }
  });
  expect(noCuiAcknowledgement.status()).toBe(200);

  const noticeResponse = await request.get(
    `${apiURL}/api/data-handling-notices/published?workflowContext=EvidenceUpload`,
    { headers: headers() }
  );
  expect(noticeResponse.status()).toBe(200);
  const notice = await noticeResponse.json();
  const noticeAcknowledgement = await request.post(
    `${apiURL}/api/tenants/${tenantId}/data-handling-notice-acknowledgements`,
    {
      headers: headers(),
      data: {
        mode: notice.mode,
        workflowContext: "EvidenceUpload",
        noticeId: notice.noticeId,
        noticeVersion: notice.version,
        acknowledged: true
      }
    }
  );
  expect(noticeAcknowledgement.status()).toBe(201);

  await page.addInitScript(
    ({ selectedTenantId, role, userId, email }) => {
      window.localStorage.setItem("gccs.selectedTenantId", selectedTenantId);
      window.localStorage.setItem("gccs.developmentRole", role);
      window.localStorage.setItem("gccs.developmentUserId", userId);
      window.localStorage.setItem("gccs.developmentUserEmail", email);
    },
    {
      selectedTenantId: tenantId,
      role: manager.role,
      userId: manager.userId,
      email: manager.email
    }
  );

  await page.goto("/app");
  await page.getByRole("link", { name: /Evidence/ }).click();
  await page.getByRole("button", { name: "New evidence" }).click();

  const title = `Synthetic Story 12.2 evidence ${Date.now()}`;
  const metadataForm = page.getByRole("region", { name: "Evidence metadata" }).locator("form");
  await metadataForm.getByLabel("Title").fill(title);
  await metadataForm.getByLabel("Description").fill("Synthetic No-CUI browser-to-API-to-PostgreSQL test evidence.");
  await expect(metadataForm.getByRole("button", { name: "Create metadata" })).toBeDisabled();
  await metadataForm.getByLabel("Classification", { exact: true }).selectOption("Unclassified");

  const createResponsePromise = page.waitForResponse(
    (response) => response.url() === `${apiURL}/api/evidence-items` && response.request().method() === "POST"
  );
  await metadataForm.getByRole("button", { name: "Create metadata" }).click();
  const createResponse = await createResponsePromise;
  expect(createResponse.status()).toBe(201);
  const evidence = await createResponse.json();
  expect(evidence.title).toBe(title);
  await expect(page.getByText("Evidence metadata created.")).toBeVisible();

  const uploadForm = page.getByRole("form", { name: "Upload area" });
  const fileName = `synthetic-story-12-2-${Date.now()}.txt`;
  const fileBytes = Buffer.from("Synthetic Story 12.2 evidence bytes.");
  await uploadForm.getByLabel("Evidence file").setInputFiles({
    name: fileName,
    mimeType: "text/plain",
    buffer: fileBytes
  });
  await uploadForm
    .getByLabel(
      "I confirm this file does not contain CUI, classified information, export-controlled data, ITAR data, or sensitive government-furnished information."
    )
    .check();

  const uploadResponsePromise = page.waitForResponse(
    (response) =>
      response.url() === `${apiURL}/api/evidence-items/${evidence.id}/file` &&
      response.request().method() === "POST"
  );
  await expect(uploadForm.getByRole("button", { name: "Upload evidence" })).toBeDisabled();
  await uploadForm.getByLabel("Upload classification", { exact: true }).selectOption("Unclassified");
  await uploadForm.getByRole("button", { name: "Upload evidence" }).click();
  const uploadResponse = await uploadResponsePromise;
  expect(uploadResponse.status()).toBe(201);
  const uploadedVersion = await uploadResponse.json();
  expect(uploadedVersion.evidenceItemId).toBe(evidence.id);
  expect(uploadedVersion.versionNumber).toBe(1);
  expect(uploadedVersion.fileName).toBe(fileName);
  expect(uploadedVersion.malwareScanStatus).toBe("clean");
  await expect(page.getByText(new RegExp(`Uploaded ${fileName}`))).toBeVisible();

  const versionResponse = await request.get(`${apiURL}/api/evidence-items/${evidence.id}/download`, {
    headers: headers()
  });
  expect(versionResponse.status()).toBe(200);
  const persistedVersion = await versionResponse.json();
  expect(persistedVersion).toMatchObject({
    evidenceItemId: evidence.id,
    versionId: uploadedVersion.versionId,
    versionNumber: 1,
    fileName,
    sizeBytes: fileBytes.length,
    validationStatus: "accepted",
    malwareScanStatus: "clean",
    isUsable: true
  });

  const contentResponse = await request.get(`${apiURL}/api/evidence-items/${evidence.id}/file/content`, {
    headers: headers()
  });
  expect(contentResponse.status()).toBe(200);
  expect(await contentResponse.body()).toEqual(fileBytes);
});
