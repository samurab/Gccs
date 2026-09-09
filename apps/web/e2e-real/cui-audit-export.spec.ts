import { expect, test, type Page } from "@playwright/test";

const apiURL = process.env.PLAYWRIGHT_API_URL ?? "http://127.0.0.1:5063";
const tenantId = "11111111-1111-1111-1111-111111111111";
const adminUserId = "22222222-2222-2222-2222-222222222222";

function headers(role: string, userId: string, email: string) {
  return {
    "X-Gccs-Dev-Auth": "true",
    "X-Gccs-Dev-Tenant": tenantId,
    "X-Gccs-Tenant": tenantId,
    "X-Gccs-Dev-User": userId,
    "X-Gccs-Dev-Email": email,
    "X-Gccs-Dev-Role": role
  };
}

async function selectPersona(page: Page, role: string, userId: string, email: string) {
  await page.addInitScript(
    ({ selectedTenantId, selectedRole, selectedUserId, selectedEmail }: Record<string, string>) => {
      window.localStorage.setItem("gccs.selectedTenantId", selectedTenantId);
      window.localStorage.setItem("gccs.developmentRole", selectedRole);
      window.localStorage.setItem("gccs.developmentUserId", selectedUserId);
      window.localStorage.setItem("gccs.developmentUserEmail", selectedEmail);
    },
    { selectedTenantId: tenantId, selectedRole: role, selectedUserId: userId, selectedEmail: email }
  );
}

test("authorized admin filters and downloads a persisted full-match CUI audit export", async ({ page, request }) => {
  const adminHeaders = headers("Admin", adminUserId, "alpha.admin@gccs.local");
  const seedResponse = await request.get(`${apiURL}/api/audit-logs?page=1&pageSize=1`, { headers: adminHeaders });
  expect(seedResponse.status()).toBe(200);

  await selectPersona(page, "Admin", adminUserId, "alpha.admin@gccs.local");
  await page.goto("/app");
  await page.getByRole("link", { name: /Settings/ }).click();

  const auditViewer = page.getByRole("region", { name: "Audit log viewer" });
  await expect(auditViewer.getByRole("heading", { name: "Audit log" })).toBeVisible();
  await expect(auditViewer.getByRole("combobox", { name: "Event type" })).toBeVisible();
  await expect(auditViewer.getByRole("combobox", { name: "Classification" })).toBeVisible();
  await expect(auditViewer.getByRole("combobox", { name: "Mode" })).toBeVisible();
  await expect(auditViewer.getByRole("combobox", { name: "Result" })).toBeVisible();

  const download = page.waitForEvent("download");
  await auditViewer.getByRole("button", { name: "Export matching events" }).click();
  const artifact = await download;
  expect(artifact.suggestedFilename()).toMatch(/^fedril-cui-audit-.*\.json$/);
  await expect(page.getByText(/matching audit events exported\./)).toBeVisible();

  const persistedResponse = await request.get(
    `${apiURL}/api/audit-logs?eventType=export&entityType=CuiAuditExport&actorUserId=${adminUserId}`,
    { headers: adminHeaders }
  );
  expect(persistedResponse.status()).toBe(200);
  const persistedPage = await persistedResponse.json();
  expect(persistedPage.items.length).toBeGreaterThan(0);
  expect(persistedPage.items[0]).toMatchObject({ eventType: "export", result: "succeeded", entityType: "CuiAuditExport" });
});

test("unauthorized auditor cannot reach the audit UI or export endpoint", async ({ page, request }) => {
  const auditorUserId = "22222222-2222-2222-2222-222222222225";
  const auditorHeaders = headers("Auditor", auditorUserId, "alpha.readonlyauditor@gccs.local");
  await selectPersona(page, "Auditor", auditorUserId, "alpha.readonlyauditor@gccs.local");
  await page.goto("/app");

  await expect(page.getByRole("heading", { name: "Dashboard", exact: true })).toBeVisible();
  await expect(page.getByRole("link", { name: /Settings/ })).toHaveCount(0);
  await expect(page.getByRole("button", { name: "Export matching events" })).toHaveCount(0);

  const response = await request.post(`${apiURL}/api/audit-logs/cui-export`, {
    headers: auditorHeaders,
    data: { eventType: "blocked-upload", result: "blocked" }
  });
  expect(response.status()).toBe(403);
  expect(await response.text()).toContain("permission_denied");
});
