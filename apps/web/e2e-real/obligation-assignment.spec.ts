import { expect, test } from "@playwright/test";

const apiURL = process.env.PLAYWRIGHT_API_URL ?? "http://127.0.0.1:5063";
const tenantId = "11111111-1111-1111-1111-111111111111";
const manager = {
  role: "Compliance Manager",
  userId: "22222222-2222-2222-2222-222222222223",
  email: "alpha.compliancemanager@gccs.local"
};
const contributor = {
  userId: "22222222-2222-2222-2222-222222222224",
  displayName: "Tenant Alpha Contributor"
};

function headers(role = manager.role) {
  return {
    "X-Gccs-Dev-Auth": "true",
    "X-Gccs-Dev-Tenant": tenantId,
    "X-Gccs-Tenant": tenantId,
    "X-Gccs-Dev-User": manager.userId,
    "X-Gccs-Dev-Email": manager.email,
    "X-Gccs-Dev-Role": role
  };
}

test("UAT-09 Compliance Manager can assign an active tenant member and the assignment persists", async ({
  page,
  request
}) => {
  const importResponse = await request.post(`${apiURL}/api/dev/compliance-content/import`, {
    headers: headers()
  });
  expect(importResponse.status()).toBe(200);

  const contractNumber = `E2E-UAT09-${Date.now()}`;
  const contractResponse = await request.post(`${apiURL}/api/contracts`, {
    headers: headers(),
    data: {
      contractNumber,
      title: "Synthetic UAT-09 assignment contract",
      agencyOrPrimeName: "Synthetic Prime LLC",
      relationship: "Subcontractor",
      kind: "FixedPrice",
      status: "Active",
      awardedAt: "2026-07-01",
      periodOfPerformanceStart: "2026-07-01",
      periodOfPerformanceEnd: "2027-06-30",
      placeOfPerformance: "Remote",
      description: "Synthetic No-CUI contract used only for real-stack regression.",
      dataHandlingPosture: "FciOnly"
    }
  });
  expect(contractResponse.status()).toBe(201);
  const contract = await contractResponse.json();

  const clauseResponse = await request.post(`${apiURL}/api/contracts/${contract.id}/clauses`, {
    headers: headers(),
    data: {
      clauseLibraryId: "far-52-204-21",
      attachmentReason: "Synthetic UAT-09 real-stack verification.",
      sourceDocumentReference: "synthetic-uat-09.txt"
    }
  });
  expect(clauseResponse.status()).toBe(201);
  const clause = await clauseResponse.json();

  const generationResponse = await request.post(
    `${apiURL}/api/contracts/${contract.id}/clauses/${clause.id}/obligations/generate`,
    { headers: headers() }
  );
  expect(generationResponse.status()).toBe(200);

  const obligationsResponse = await request.get(
    `${apiURL}/api/contract-obligations?contractId=${contract.id}`,
    { headers: headers() }
  );
  expect(obligationsResponse.status()).toBe(200);
  const obligations = await obligationsResponse.json();
  expect(obligations.length).toBeGreaterThan(0);
  const obligation = obligations[0];

  const candidatesResponse = await request.get(
    `${apiURL}/api/contract-obligations/assignment-candidates`,
    { headers: headers() }
  );
  expect(candidatesResponse.status()).toBe(200);
  const candidates = await candidatesResponse.json();
  expect(candidates).toContainEqual(contributor);
  expect(JSON.stringify(candidates)).not.toContain("mfaEnabled");
  expect(JSON.stringify(candidates)).not.toContain("lastSignedInAt");

  const viewOnlyResponse = await request.get(
    `${apiURL}/api/contract-obligations/assignment-candidates`,
    { headers: headers("Auditor") }
  );
  expect(viewOnlyResponse.status()).toBe(403);

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
  await page.getByRole("link", { name: /Obligations/ }).click();
  await page.getByRole("combobox", { name: "Contract", exact: true }).selectOption(contract.id);
  await page.getByRole("button", { name: "Apply filters" }).click();
  const obligationCard = page.locator(".ui-task-card").filter({ hasText: contractNumber }).first();
  await expect(obligationCard).toBeVisible();
  await obligationCard.getByRole("button", { name: "View details" }).click();

  const memberSelect = page.getByRole("combobox", { name: "Tenant member", exact: true });
  await expect(memberSelect).toBeVisible();
  await expect(memberSelect.getByRole("option", { name: contributor.displayName })).toHaveCount(1);
  await memberSelect.selectOption(contributor.userId);
  await page.getByRole("button", { name: "Assign owner" }).click();
  await expect(page.getByText("Obligation owner assigned.")).toBeVisible();

  await page.reload();
  await page.getByRole("link", { name: /Obligations/ }).click();
  await page.getByRole("combobox", { name: "Contract", exact: true }).selectOption(contract.id);
  await page.getByRole("button", { name: "Apply filters" }).click();
  await page.locator(".ui-task-card").filter({ hasText: contractNumber }).first().getByRole("button", { name: "View details" }).click();
  await expect(page.getByRole("status")).toContainText(`Currently assigned to: ${contributor.displayName}`);
  await expect(page.getByRole("combobox", { name: "Tenant member", exact: true })).toHaveValue(contributor.userId);

  const detailResponse = await request.get(
    `${apiURL}/api/contract-obligations/${obligation.contractClauseId}/${obligation.obligationId}`,
    { headers: headers() }
  );
  expect(detailResponse.status()).toBe(200);
  expect((await detailResponse.json()).assignedUserId).toBe(contributor.userId);
});
