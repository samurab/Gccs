import test from "node:test";
import assert from "node:assert/strict";
import { validateRollbackAuth, prepareRollbackAuth } from "./validate-rollback-auth.mjs";

const id = "11111111-2222-3333-4444-555555555555";
const workforce = {
  PRODUCTION_RESOURCE_GROUP: "production-rg", PRODUCTION_API_APP_NAME: "production-api",
  PRODUCTION_WEB_BASE_URL: "https://production.example.test",
  PRODUCTION_API_BASE_URL: "https://api.example.test",
  FEDRIL_WORKFORCE_CLIENT_ID: id, FEDRIL_WORKFORCE_TENANT_ID: id,
  FEDRIL_WORKFORCE_API_SCOPE: `api://${id}/access_as_user`
};
const customer = {
  ...workforce, FEDRIL_CUSTOMER_CLIENT_ID: id, FEDRIL_CUSTOMER_TENANT_ID: id,
  FEDRIL_CUSTOMER_TENANT_SUBDOMAIN: "customers", FEDRIL_CUSTOMER_API_SCOPE: `api://${id}/access_as_customer`,
  FEDRIL_CUSTOMER_AUTHORITY: `https://customers.ciamlogin.com/${id}/v2.0`, FEDRIL_CUSTOMER_AUDIENCE: `api://${id}`
};
const settings = [
  { name: "Authentication__Customer__Authority", value: customer.FEDRIL_CUSTOMER_AUTHORITY },
  { name: "Authentication__Customer__Audience", value: customer.FEDRIL_CUSTOMER_AUDIENCE }
];
const azure = values => (command, args, options) => {
  assert.equal(command, "az");
  assert.deepEqual(args.slice(0, 4), ["webapp", "config", "appsettings", "list"]);
  assert.ok(args.includes("production-api"));
  assert.match(args[args.indexOf("--query") + 1], /Authentication__Customer__Authority/);
  assert.deepEqual(options.stdio, ["ignore", "pipe", "pipe"]);
  assert.equal(options.timeout, 30000);
  return JSON.stringify(values);
};

const runtime = active => ({ apiBaseUrl: "https://api.example.test", workforceClientId: id,
  workforceTenantId: id, workforceApiScope: workforce.FEDRIL_WORKFORCE_API_SCOPE,
  ...(active ? { customerClientId: id, customerTenantId: id, customerTenantSubdomain: "customers", customerApiScope: customer.FEDRIL_CUSTOMER_API_SCOPE } : {}) });
const frontend = config => async (url, options) => {
  assert.equal(url.href, "https://production.example.test/runtime-config.js");
  assert.equal(options.redirect, "error");
  assert.ok(options.signal instanceof AbortSignal);
  return new Response(`window.__FEDRIL_CONFIG__ = Object.freeze(${JSON.stringify(config)});`);
};

test("workforce-only rollback remains available when live customer realm is absent", async () => {
  assert.deepEqual(await validateRollbackAuth(workforce, azure([]), frontend(runtime(false))), { customerConfigured: false });
});
test("rollback preserves a matching live customer realm", async () => {
  assert.deepEqual(await validateRollbackAuth(customer, azure(settings), frontend(runtime(true))), { customerConfigured: true });
});
test("rollback cannot remove a live customer realm", async () => {
  await assert.rejects(validateRollbackAuth(workforce, azure(settings), frontend(runtime(true))), /required/);
});
test("pending complete GitHub values are masked only when both live surfaces prove absence", async () => {
  const writes = [];
  const env = { ...customer, GITHUB_ENV: "/runner/effective-env" };
  await prepareRollbackAuth(env, azure([]), frontend(runtime(false)), async (...args) => writes.push(args));
  assert.equal(writes.length, 1);
  assert.equal(writes[0][0], env.GITHUB_ENV);
  assert.equal(writes[0][1], ["CLIENT_ID", "TENANT_ID", "TENANT_SUBDOMAIN", "API_SCOPE", "AUTHORITY", "AUDIENCE"].map(suffix => `FEDRIL_CUSTOMER_${suffix}=\n`).join(""));
  assert.equal(env.FEDRIL_CUSTOMER_CLIENT_ID, id);
  await prepareRollbackAuth(env, azure(settings), frontend(runtime(true)), async () => assert.fail("Active customer realm must not be masked"));
});
test("rollback cannot replace a live customer realm", async () => {
  await assert.rejects(validateRollbackAuth(customer, azure(settings.map(item => item.name.endsWith("Authority") ? { ...item, value: item.value.replace("customers.", "different.") } : item)), frontend(runtime(true))), /differs/);
  await assert.rejects(validateRollbackAuth(customer, azure(settings), frontend({ ...runtime(true), customerClientId: "different" })), /differs/);
});
test("rollback rejects partial desired and live authentication", async () => {
  await assert.rejects(validateRollbackAuth({ ...workforce, FEDRIL_CUSTOMER_CLIENT_ID: id }, azure([]), frontend(runtime(false))), /required/);
  await assert.rejects(validateRollbackAuth(customer, azure(settings.slice(0, 1)), frontend(runtime(true))), /incomplete/);
  await assert.rejects(validateRollbackAuth(customer, azure(settings), frontend(runtime(false))), /inconsistent/);
  await assert.rejects(validateRollbackAuth(customer, azure([]), frontend(runtime(true))), /inconsistent/);
  await assert.rejects(validateRollbackAuth(customer, azure(settings), frontend({ ...runtime(false), customerClientId: id })), /incomplete/);
});
test("provider failures are sanitized and stop rollback", async () => {
  await assert.rejects(validateRollbackAuth(workforce, () => { throw new Error("sensitive-provider-output"); }), error => /Unable to verify/.test(error.message) && !error.message.includes("sensitive"));
  await assert.rejects(validateRollbackAuth(workforce, () => "not-json"), /Unable to verify/);
  await assert.rejects(validateRollbackAuth(workforce, azure({ error: "provider failure" })), /Unable to verify/);
});
test("ambiguous live values stop rollback", async () => {
  await assert.rejects(validateRollbackAuth(customer, azure([...settings, settings[0]])), /Ambiguous/);
});

test("frontend failures and invalid payloads never mask pending settings", async () => {
  for (const request of [
    async () => { throw new Error("sensitive-network-error"); },
    async () => new Response("redirect", { status: 302 }),
    async () => ({ ok: true, redirected: true, body: new Response("redirected content").body }),
    async () => new Response("failure", { status: 500 }),
    async () => new Response("window.__FEDRIL_CONFIG__ = Object.freeze({});"),
    async () => new Response("window.__FEDRIL_CONFIG__ = Object.freeze(globalThis.sideEffect = true);"),
    async () => new Response("x".repeat(65537)),
    frontend({ ...runtime(false), customerClientId: null })
  ]) {
    await assert.rejects(prepareRollbackAuth({ ...customer, GITHUB_ENV: "/runner/env" }, azure([]), request, async () => assert.fail("Failure must not write GITHUB_ENV")), error => error.message === "Unable to verify live production frontend authentication; rollback stopped");
  }
});

test("frontend origin must be HTTPS without credentials or URL components", async () => {
  for (const url of ["http://production.example.test", "https://user:password@production.example.test", "https://production.example.test/path", "https://production.example.test?query", "https://production.example.test#fragment", "not-a-url"]) {
    await assert.rejects(validateRollbackAuth({ ...workforce, PRODUCTION_WEB_BASE_URL: url }, azure([]), async () => assert.fail("Invalid origin must not fetch")), /Unable to verify/);
  }
});

test("effective environment write is required and failures are sanitized", async () => {
  await assert.rejects(prepareRollbackAuth(customer, azure([]), frontend(runtime(false))), /GITHUB_ENV/);
  await assert.rejects(prepareRollbackAuth({ ...customer, GITHUB_ENV: "/runner/env" }, azure([]), frontend(runtime(false)), async () => { throw new Error("private path"); }), error => error.message === "Unable to set effective rollback authentication configuration; rollback stopped");
});

test("rollback rejects changed workforce or API destination before masking pending settings", async () => {
  for (const overrides of [
    { apiBaseUrl: "https://different.example.test" },
    { apiBaseUrl: "http://api.example.test" },
    { apiBaseUrl: "https://api.example.test?unexpected=true" },
    { workforceClientId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee" },
    { workforceTenantId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee" },
    { workforceApiScope: `api://${id}/different_scope` },
    { workforceTenantId: undefined }
  ]) {
    await assert.rejects(prepareRollbackAuth({ ...customer, GITHUB_ENV: "/runner/env" }, azure([]), frontend({ ...runtime(false), ...overrides }), async () => assert.fail("Mismatch must not change effective settings")), /Unable to verify live production frontend/);
  }
  await assert.rejects(validateRollbackAuth({ ...customer, PRODUCTION_API_BASE_URL: "http://api.example.test" }, azure([]), frontend(runtime(false))), /Unable to verify/);
  assert.deepEqual(await validateRollbackAuth({ ...customer, PRODUCTION_API_BASE_URL: "https://api.example.test/" }, azure([]), frontend(runtime(false))), { customerConfigured: false });
});
