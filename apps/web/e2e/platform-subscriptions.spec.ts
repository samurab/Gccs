import { expect, test } from "@playwright/test";

const tenantId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2";
const subscription = {
  id: "dddddddd-dddd-dddd-dddd-ddddddddddd2",
  tenantId,
  tenantKind: "ContractorWorkspace",
  plan: "PilotEvaluation",
  planCode: "PILOT-EVALUATION",
  status: "Active",
  effectiveStatus: "Active",
  accessLevel: "Full",
  startsAt: "2026-08-01T12:00:00Z",
  endsAt: "2026-09-01T00:00:00Z",
  graceEndsAt: "2026-09-08T00:00:00Z",
  externalCustomerReference: "PILOT-E2E",
  externalSubscriptionReference: null,
  statusReason: "Approved pilot.",
  version: 1,
  isReplay: false
};

test("platform operator extends an active pilot with version and idempotency controls", async ({ page }) => {
  let transitionRequest: { body: Record<string, unknown>; idempotencyKey: string | undefined } | null = null;
  await page.route("**/api/platform/**", async (route) => {
    const request = route.request();
    const url = new URL(request.url());
    if (url.pathname === "/api/platform/me/access") {
      await route.fulfill({ json: {
        userId: "22222222-2222-2222-2222-222222222222",
        userEmail: "operator@fedril.local",
        canProvisionTenants: true,
        canManageDemoRequests: false,
        permissions: ["ProvisionTenants"]
      } });
      return;
    }

    if (url.pathname === "/api/platform/tenant-onboardings") {
      const active = url.searchParams.get("status") === "Active";
      await route.fulfill({ json: {
        items: active ? [{
          onboardingId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2",
          tenantId,
          displayName: "Browser Pilot",
          onboardingType: "Pilot",
          onboardingStatus: "Active",
          tenantStatus: "Trialing",
          dataHandlingMode: "NoCui",
          customerReference: "PILOT-E2E",
          ownerEmail: "owner@example.com",
          ownerDisplayName: "Pilot Owner",
          ownerRoleName: "Owner",
          invitationId: "cccccccc-cccc-cccc-cccc-ccccccccccc2",
          invitationStatus: "Accepted",
          invitationDeliveryStatus: "Sent",
          invitationNotificationSentAt: "2026-08-01T12:00:00Z",
          invitationExpiresAt: "2026-08-08T12:00:00Z",
          trialEndsAt: "2026-08-31",
          planCode: null,
          subscriptionReference: null,
          setupReason: "Approved pilot.",
          createdAt: "2026-08-01T12:00:00Z",
          cancelledAt: null,
          cancelledByUserId: null,
          cancellationReason: null,
          subscription,
          isReplay: false
        }] : [],
        page: 1,
        pageSize: active ? 100 : 25,
        totalCount: active ? 1 : 0,
        hasNextPage: false,
        hasPreviousPage: false
      } });
      return;
    }

    if (url.pathname === `/api/platform/tenant-subscriptions/${tenantId}/extend`) {
      transitionRequest = {
        body: request.postDataJSON(),
        idempotencyKey: request.headers()["idempotency-key"]
      };
      await route.fulfill({ json: {
        ...subscription,
        endsAt: "2026-09-16T00:00:00Z",
        graceEndsAt: "2026-09-23T00:00:00Z",
        version: 2
      } });
      return;
    }

    await route.fulfill({ status: 404, json: {} });
  });

  await page.goto("/platform/tenants/new");
  await page.getByText(/Browser Pilot/).click();
  await page.getByLabel("New pilot end date").fill("2026-09-15");
  await page.getByLabel("Required reason").fill("Browser-approved extension.");
  await page.getByRole("button", { name: "Extend" }).click();

  await expect(page.getByText("Subscription extend completed.")).toBeVisible();
  expect(transitionRequest).not.toBeNull();
  expect(transitionRequest!.body).toEqual({
    newEndsOn: "2026-09-15",
    reason: "Browser-approved extension.",
    expectedVersion: 1
  });
  expect(transitionRequest!.idempotencyKey).toMatch(/^[0-9a-f-]{36}$/i);
});
