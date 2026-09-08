import { expect, test } from "@playwright/test";
import { acknowledgeCurrentNotice } from "./notices";

const apiURL = process.env.PLAYWRIGHT_API_URL ?? "http://127.0.0.1:5063";
const tenantId = "11111111-1111-1111-1111-111111111111";
const owner = { role: "Owner", userId: "22222222-2222-2222-2222-222222222222", email: "alpha.admin@gccs.local" };
const headers = {
  "X-Gccs-Dev-Auth": "true", "X-Gccs-Dev-Tenant": tenantId, "X-Gccs-Tenant": tenantId,
  "X-Gccs-Dev-User": owner.userId, "X-Gccs-Dev-Email": owner.email, "X-Gccs-Dev-Role": owner.role
};

test("classification review persists note history and keeps escalation release separate", async ({ page, request }) => {
  await acknowledgeCurrentNotice(request, apiURL, headers, "ClassifiedNote");
  await acknowledgeCurrentNotice(request, apiURL, headers, "Support");
  await page.addInitScript(({ tenantId, owner }) => {
    localStorage.setItem("gccs.selectedTenantId", tenantId);
    localStorage.setItem("gccs.developmentRole", owner.role);
    localStorage.setItem("gccs.developmentUserId", owner.userId);
    localStorage.setItem("gccs.developmentUserEmail", owner.email);
  }, { tenantId, owner });
  await page.goto("/app");
  await page.getByRole("link", { name: /Evidence/ }).click();
  const title = `Synthetic classification note ${Date.now()}`;
  const notes = page.getByRole("region", { name: "Classified notes", exact: true });
  await notes.getByLabel("Note title").fill(title);
  await notes.getByLabel("Note text").fill("Synthetic No-CUI note text for real-stack classification verification.");
  await expect(notes.getByRole("button", { name: "Save note" })).toBeDisabled();
  await notes.getByLabel("Note classification").selectOption("Unknown");
  const creating = page.waitForResponse(r => r.url() === `${apiURL}/api/classified-notes` && r.request().method() === "POST");
  await notes.getByRole("button", { name: "Save note" }).click();
  const created = await creating; expect(created.status()).toBe(201);
  const note = await created.json();
  expect((await request.get(`${apiURL}/api/classified-notes/${note.id}`, { headers })).status()).toBe(400);
  await page.locator("summary").filter({ hasText: /^Classification review and history$/ }).click();
  const panel = page.getByRole("region", { name: "Classification review and history", exact: true });
  await panel.getByLabel("Content type").selectOption("notes");
  await panel.getByRole("button", { name: `Inspect ${title}`, exact: true }).click();
  await expect(panel.getByRole("button", { name: "Save classification review" })).toBeDisabled();
  async function review(classification: string, reason: string) {
    await panel.getByLabel("Reviewed classification", { exact: true }).selectOption(classification);
    await panel.getByLabel("Review reason", { exact: true }).fill(reason);
    const pending = page.waitForResponse(r => r.url() === `${apiURL}/api/classified-content/notes/${note.id}/classification` && r.request().method() === "PATCH");
    await panel.getByRole("button", { name: "Save classification review" }).click();
    expect((await pending).status()).toBe(200);
    await expect(panel.getByRole("button", { name: "Reload current item" })).toBeEnabled();
  }
  await review("Fci", "Synthetic reviewer confirmed safe FCI metadata.");
  expect((await request.get(`${apiURL}/api/classified-notes/${note.id}`, { headers })).status()).toBe(200);
  await review("Prohibited", "Synthetic quarantine test; no actual prohibited content.");
  expect((await request.get(`${apiURL}/api/classified-notes/${note.id}`, { headers })).status()).toBe(400);
  await panel.getByLabel("Escalation or resolution reason").fill("Synthetic metadata-only escalation.");
  const escalating = page.waitForResponse(r => r.url().endsWith("/cui-support-escalations") && r.request().method() === "POST");
  await panel.getByRole("button", { name: "Escalate restricted content" }).click();
  expect((await escalating).status()).toBe(201);
  await review("Fci", "Synthetic post-escalation review confirmed false positive.");
  expect((await request.get(`${apiURL}/api/classified-notes/${note.id}`, { headers })).status()).toBe(403);
  await panel.getByLabel("Escalation or resolution reason").fill("Safe review occurred after escalation; synthetic false positive.");
  const resolving = page.waitForResponse(r => r.url().includes("/cui-support-escalations/") && r.url().endsWith("/resolve"));
  await panel.getByRole("button", { name: "Resolve reviewed false positive" }).click();
  expect((await resolving).status()).toBe(200);
  expect((await request.get(`${apiURL}/api/classified-notes/${note.id}`, { headers })).status()).toBe(200);
  const historyResponse = await request.get(`${apiURL}/api/classified-content/notes/${note.id}/history`, { headers });
  const history = await historyResponse.json();
  expect(history).toHaveLength(4);
  expect(history[0].revision).toBe(3);
  expect(history[0].previousMetadata.classification).toBe("Prohibited");
  expect(history[0].reviewedByUserId).toBe(owner.userId);
  await page.reload();
  await page.locator("summary").filter({ hasText: /^Classification review and history$/ }).click();
  await panel.getByLabel("Content type").selectOption("notes");
  await panel.getByLabel("Needs classification review only").uncheck();
  await panel.getByRole("button", { name: `Inspect ${title}`, exact: true }).click();
  await expect(panel.getByText("Revision 3: Prohibited → Fci")).toBeVisible();
});
