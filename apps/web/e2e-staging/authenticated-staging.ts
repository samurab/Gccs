import { readFileSync } from "node:fs";
import { expect, type Page, type TestInfo } from "@playwright/test";

type CurrentAccess = {
  tenantId: string;
  roles: string[];
  permissions: string[];
};

type AuthenticatedWorkspace = CurrentAccess & {
  apiOrigin: string;
};

export async function openAuthenticatedWorkspace(
  page: Page,
  testInfo: TestInfo,
  requiredPermissions: string[],
  forbiddenPermissions: string[] = []
) {
  const authStatePath = String(testInfo.project.metadata.authStatePath ?? "");
  if (!authStatePath) throw new Error("The staging project auth state path is missing.");

  const state = JSON.parse(readFileSync(authStatePath, "utf8"));
  const baseURL = String(testInfo.project.use.baseURL);
  const origin = new URL(baseURL).origin;
  const entries = state.sessionStorage?.[origin];
  if (!Array.isArray(entries)) throw new Error(`No session storage was captured for ${origin}.`);

  await page.addInitScript(
    ({ expectedOrigin, values }) => {
      if (window.location.origin !== expectedOrigin) return;
      for (const [name, value] of values) window.sessionStorage.setItem(name, value);
    },
    { expectedOrigin: origin, values: entries }
  );

  const accessResponsePromise = page.waitForResponse(
    response => new URL(response.url()).pathname === "/api/me/access" && response.request().method() === "GET"
  );
  await page.goto("/app");
  const accessResponse = await accessResponsePromise;
  expect(accessResponse.status()).toBe(200);
  expect(accessResponse.request().headers()["authorization"]).toMatch(/^Bearer /);
  expect(accessResponse.request().headers()["x-gccs-dev-auth"]).toBeUndefined();

  const access = await accessResponse.json() as CurrentAccess;
  expect(access.tenantId).toBeTruthy();
  for (const permission of requiredPermissions) expect(access.permissions).toContain(permission);
  for (const permission of forbiddenPermissions) expect(access.permissions).not.toContain(permission);
  await expect.poll(() => page.evaluate(() => window.localStorage.getItem("gccs.selectedTenantId")))
    .toBe(access.tenantId);
  await expect(page.getByText(/^Signed in as /)).toBeVisible();
  return { ...access, apiOrigin: new URL(accessResponse.url()).origin } satisfies AuthenticatedWorkspace;
}

export async function acknowledgeWorkflowNotice(page: Page, workflowName: string) {
  const notice = page.locator("details.data-handling-notice").filter({ hasText: `${workflowName} data handling notice` });
  await notice.waitFor({ state: "visible" });
  const accepted = notice.getByText("Current notice acknowledged for this workflow.");
  const acknowledgement = notice.getByLabel("I have read and acknowledge this notice.");
  await expect.poll(async () => await accepted.count() + await acknowledgement.count()).toBeGreaterThan(0);
  if (await accepted.count()) return;
  if (await notice.getAttribute("open") === null) {
    await notice.getByText(`${workflowName} data handling notice`, { exact: true }).click();
  }
  await expect(acknowledgement).toBeVisible();
  await acknowledgement.check();
  await notice.getByRole("button", { name: "Acknowledge current notice" }).click();
  await expect(notice.getByText("Current notice acknowledged for this workflow.")).toBeVisible();
}

export async function authenticatedApiRequest(
  page: Page,
  apiOrigin: string,
  method: "GET" | "POST",
  path: string,
  body?: unknown
) {
  return page.evaluate(async ({ target, requestMethod, payload }) => {
    const token = window.localStorage.getItem("access_token") ?? window.sessionStorage.getItem("access_token");
    if (!token) throw new Error("Authenticated access token is unavailable in the browser session.");
    const tenantId = window.localStorage.getItem("gccs.selectedTenantId");
    const response = await fetch(target, {
      method: requestMethod,
      headers: {
        Authorization: `Bearer ${token}`,
        ...(payload === undefined ? {} : { "Content-Type": "application/json" }),
        ...(tenantId ? { "X-Gccs-Tenant": tenantId } : {})
      },
      ...(payload === undefined ? {} : { body: JSON.stringify(payload) })
    });
    return { status: response.status, body: await response.text() };
  }, { target: new URL(path, apiOrigin).toString(), requestMethod: method, payload: body });
}

export async function authenticatedCollectionIds(page: Page, apiOrigin: string, path: string) {
  const response = await authenticatedApiRequest(page, apiOrigin, "GET", path);
  expect(response.status).toBe(200);
  const records = JSON.parse(response.body) as Array<{ id: string }>;
  if (!Array.isArray(records)) throw new Error(`${path} did not return a collection.`);
  return records.map(record => record.id).sort();
}
