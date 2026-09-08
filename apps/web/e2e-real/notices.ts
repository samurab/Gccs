import { expect, type APIRequestContext } from "@playwright/test";

export async function acknowledgeCurrentNotice(
  request: APIRequestContext,
  apiURL: string,
  headers: Record<string, string>,
  workflowContext: string
) {
  const response = await request.get(`${apiURL}/api/data-handling-notices/published`, {
    headers, params: { workflowContext }
  });
  expect(response.status()).toBe(200);
  const notice = await response.json();
  const accepted = await request.post(
    `${apiURL}/api/tenants/${headers["X-Gccs-Dev-Tenant"]}/data-handling-notice-acknowledgements`,
    { headers, data: {
      mode: notice.mode, workflowContext, noticeId: notice.noticeId,
      noticeVersion: notice.version, acknowledged: true
    } }
  );
  expect(accepted.status()).toBe(201);
}
