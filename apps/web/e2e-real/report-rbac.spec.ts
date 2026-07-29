import { expect, test } from "@playwright/test";

const apiURL = process.env.PLAYWRIGHT_API_URL ?? "http://127.0.0.1:5063";
const tenantId = "11111111-1111-1111-1111-111111111111";

const readOnlyPersonas = [
  {
    role: "Auditor",
    userId: "22222222-2222-2222-2222-222222222225",
    email: "alpha.readonlyauditor@gccs.local"
  },
  {
    role: "Contributor",
    userId: "22222222-2222-2222-2222-222222222224",
    email: "alpha.contributor@gccs.local"
  }
];

for (const persona of readOnlyPersonas) {
  test(`${persona.role} can view report history but cannot generate reports through UI or API`, async ({
    page,
    request
  }) => {
    const setupHeaders = {
      "X-Gccs-Dev-Auth": "true",
      "X-Gccs-Dev-Tenant": tenantId,
      "X-Gccs-Tenant": tenantId,
      "X-Gccs-Dev-User": "22222222-2222-2222-2222-222222222222",
      "X-Gccs-Dev-Email": "alpha.admin@gccs.local",
      "X-Gccs-Dev-Role": "Admin"
    };
    const setupReportResponse = await request.post(`${apiURL}/api/reports/compliance-status`, {
      headers: setupHeaders
    });
    expect(setupReportResponse.status()).toBe(201);

    await page.addInitScript(
      ({ selectedTenantId, role, userId, email }) => {
        window.localStorage.setItem("gccs.selectedTenantId", selectedTenantId);
        window.localStorage.setItem("gccs.developmentRole", role);
        window.localStorage.setItem("gccs.developmentUserId", userId);
        window.localStorage.setItem("gccs.developmentUserEmail", email);
      },
      {
        selectedTenantId: tenantId,
        role: persona.role,
        userId: persona.userId,
        email: persona.email
      }
    );

    await page.goto("/app");
    await page.getByRole("link", { name: /Reports/ }).click();

    await expect(page.getByRole("heading", { name: "Reports and audit packages" })).toBeVisible();
    await expect(
      page.getByText("Your role can view existing reports but cannot generate new reports or evidence packages.")
    ).toBeVisible();
    await expect(page.getByRole("button", { name: "Generate status" })).toHaveCount(0);
    await expect(page.getByRole("button", { name: "Generate readiness" })).toHaveCount(0);
    await expect(page.getByRole("button", { name: "Generate supplier report" })).toHaveCount(0);
    await expect(page.getByRole("button", { name: "Generate package" })).toHaveCount(0);

    const headers = {
      "X-Gccs-Dev-Auth": "true",
      "X-Gccs-Dev-Tenant": tenantId,
      "X-Gccs-Tenant": tenantId,
      "X-Gccs-Dev-User": persona.userId,
      "X-Gccs-Dev-Email": persona.email,
      "X-Gccs-Dev-Role": persona.role
    };
    const reportHistoryBefore = await request.get(`${apiURL}/api/reports/recent`, { headers });
    expect(reportHistoryBefore.status()).toBe(200);
    const reportHistory = await reportHistoryBefore.json();
    const initialReportCount = reportHistory.length;
    expect(initialReportCount).toBeGreaterThan(0);
    const firstReport = reportHistory[0];
    const reportDetailResponse = await request.get(`${apiURL}/api/reports/${firstReport.id}`, { headers });
    expect(reportDetailResponse.status()).toBe(200);
    expect((await reportDetailResponse.json()).id).toBe(firstReport.id);

    const recentReportsSection = page.getByRole("heading", { name: "Recent generated reports" }).locator("..");
    await recentReportsSection.getByRole("button").first().click();
    const reportDetail = page.getByLabel("Generated report detail");
    await expect(reportDetail).toBeVisible();
    await expect(reportDetail).toContainText(firstReport.id);
    await expect(reportDetail.getByRole("button", { name: "Archive report" })).toHaveCount(0);

    const archiveResponse = await request.post(`${apiURL}/api/reports/${firstReport.id}/archive`, {
      headers,
      data: { reason: "Read-only personas cannot archive reports." }
    });
    expect(archiveResponse.status()).toBe(403);
    expect(await archiveResponse.text()).toContain("permission_denied");

    const generationPaths = [
      "/api/reports/compliance-status",
      "/api/reports/cmmc-readiness?assessmentId=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3",
      "/api/reports/subcontractor-compliance",
      "/api/reports/evidence-packages"
    ];

    for (const path of generationPaths) {
      const response = await request.post(`${apiURL}${path}`, {
        headers,
        data: path.endsWith("evidence-packages")
          ? {
              title: "Unauthorized package",
              obligationIds: [],
              contractIds: [],
              controlIds: ["AC.L1-3.1.1"],
              subcontractorIds: [],
              includeDraftOrRejectedEvidence: false
            }
          : undefined
      });
      expect(response.status(), `${persona.role} ${path}`).toBe(403);
      expect(await response.text()).toContain("permission_denied");
    }

    const reportHistoryAfter = await request.get(`${apiURL}/api/reports/recent`, { headers });
    expect(reportHistoryAfter.status()).toBe(200);
    expect((await reportHistoryAfter.json()).length).toBe(initialReportCount);
  });
}
