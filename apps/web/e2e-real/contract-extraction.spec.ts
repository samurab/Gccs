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

function adminHeaders() {
  return {
    ...headers(),
    "X-Gccs-Dev-User": "22222222-2222-2222-2222-222222222222",
    "X-Gccs-Dev-Email": "alpha.admin@gccs.local",
    "X-Gccs-Dev-Role": "Admin"
  };
}

test("UAT-04 Start extraction processes uploaded text and displays clause candidates", async ({
  page,
  request
}) => {
  const acknowledgementResponse = await request.post(`${apiURL}/api/no-cui-acknowledgement`, {
    headers: headers(),
    data: {
      acknowledged: true,
      noticeVersion: "no-cui-mvp-v1"
    }
  });
  expect(acknowledgementResponse.status()).toBe(200);

  const contractNumber = `E2E-UAT04-${Date.now()}`;
  const contractResponse = await request.post(`${apiURL}/api/contracts`, {
    headers: headers(),
    data: {
      contractNumber,
      title: "Synthetic UAT-04 extraction contract",
      agencyOrPrimeName: "Synthetic Prime LLC",
      relationship: "Prime",
      kind: "PurchaseOrder",
      status: "Active",
      awardedAt: "2026-07-01",
      periodOfPerformanceStart: "2026-07-01",
      periodOfPerformanceEnd: "2027-06-30",
      placeOfPerformance: "Remote",
      description: "Synthetic No-CUI text used only for real-stack extraction regression.",
      dataHandlingPosture: "NoFciOrCui"
    }
  });
  expect(contractResponse.status()).toBe(201);

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
  await page.getByRole("link", { name: /Contracts/ }).click();
  await page.getByRole("button", { name: new RegExp(contractNumber) }).click();

  const fileName = `synthetic-uat04-${Date.now()}.txt`;
  await page.locator('input[type="file"][aria-label="Contract document"]').setInputFiles({
    name: fileName,
    mimeType: "text/plain",
    buffer: Buffer.from("FAR 52.204-21 - Basic Safeguarding of Covered Contractor Information Systems.")
  });
  await page
    .getByLabel(
      "I confirm this file does not contain CUI, classified information, export-controlled data, ITAR data, or sensitive government-furnished information."
    )
    .check();
  await page.getByRole("button", { name: "Upload document" }).click();

  const documentCard = page.locator(".contract-document-item").filter({ hasText: fileName });
  await expect(documentCard).toContainText("accepted · clean");
  await documentCard.getByRole("button", { name: "Start extraction" }).click();

  await expect(page.getByText("Extraction completed with 1 clause candidate.")).toBeVisible({
    timeout: 30_000
  });
  await expect(documentCard).toContainText("Results Completed · 1 candidates");
  await expect(documentCard).toContainText("FAR 52.204-21");

  const auditResponse = await request.get(
    `${apiURL}/api/audit-logs?entityType=ExtractionJob`,
    { headers: adminHeaders() }
  );
  expect(auditResponse.status()).toBe(200);
  const auditEvents = await auditResponse.json();
  expect(auditEvents.items.some((entry: { action: string }) => entry.action === "Created")).toBe(true);
  expect(auditEvents.items.some((entry: { action: string }) => entry.action === "Updated")).toBe(true);
});
