import test from "node:test";
import assert from "node:assert/strict";
import { mkdtempSync, writeFileSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { spawnSync } from "node:child_process";

const expected = {
  ASPNETCORE_ENVIRONMENT: "Staging", AllowedHosts: "api.example.test",
  Cors__AllowedOrigins__0: "https://web.example.test",
  Authentication__Authority: "https://login.microsoftonline.com/workforce/v2.0",
  Authentication__Audience: "api://workforce-api",
  Authentication__Customer__Authority: "https://customer.ciamlogin.com/customer/v2.0",
  Authentication__Customer__Audience: "api://customer-api"
};
const environment = {
  STAGING_API_BASE_URL: "https://api.example.test", STAGING_WEB_BASE_URL: "https://web.example.test",
  STAGING_MSAL_TENANT_ID: "workforce", STAGING_MSAL_API_SCOPE: "api://workforce-api/access_as_user",
  STAGING_CUSTOMER_AUTHORITY: expected.Authentication__Customer__Authority,
  STAGING_CUSTOMER_AUDIENCE: expected.Authentication__Customer__Audience,
  STAGING_RESOURCE_GROUP: "test-rg", STAGING_API_APP_NAME: "test-api"
};
function run({ values = expected, failure = false, missing = false, raw } = {}) {
  const dir = mkdtempSync(join(tmpdir(), "fedril-settings-test-"));
  try {
    writeFileSync(join(dir, "az"), `#!${process.execPath}\nif (${failure}) { console.error('secret-provider-detail'); process.exit(1); }\nconsole.log(${JSON.stringify(raw ?? JSON.stringify(Object.entries(values).map(([name, value]) => ({ name, value }))))});\n`, { mode: 0o700 });
    return spawnSync(process.execPath, [new URL("./check-azure-settings.mjs", import.meta.url).pathname, "staging"], {
      encoding: "utf8", env: { ...process.env, ...environment, PATH: `${dir}:${process.env.PATH}`, ...(missing ? { STAGING_CUSTOMER_AUDIENCE: "" } : {}) }
    });
  } finally { rmSync(dir, { recursive: true, force: true }); }
}
test("matches public explicit Azure configuration", () => {
  const result = run(); assert.equal(result.status, 0); assert.match(result.stdout, /7 keys/);
});
test("missing required identity variable fails before lookup", () => {
  const result = run({ missing: true }); assert.equal(result.status, 1); assert.match(result.stderr, /STAGING_CUSTOMER_AUDIENCE is missing/);
});
test("mismatch reports key only, never provider setting value", () => {
  const result = run({ values: { ...expected, Authentication__Audience: "unexpected-sensitive-value" } });
  assert.equal(result.status, 1); assert.match(result.stderr, /Authentication__Audience/);
  assert.doesNotMatch(result.stderr, /unexpected-sensitive-value/);
});
test("provider failure does not print provider diagnostics", () => {
  const result = run({ failure: true }); assert.equal(result.status, 1);
  assert.match(result.stderr, /Azure settings lookup failed/); assert.doesNotMatch(result.stderr, /secret-provider-detail/);
});
for (const [name, raw] of [
  ["malformed JSON", "secret-provider-detail is not JSON"],
  ["non-array JSON", JSON.stringify({ value: "secret-provider-detail" })],
  ["duplicate settings", JSON.stringify([{ name: "AllowedHosts", value: "secret-provider-detail" }, { name: "AllowedHosts", value: "api.example.test" }])],
  ["invalid entry shape", JSON.stringify([{ name: "AllowedHosts", value: { content: "secret-provider-detail" } }])]
]) test(`${name} fails without provider output`, () => {
  const result = run({ raw }); assert.equal(result.status, 1);
  assert.match(result.stderr, /response was malformed/);
  assert.doesNotMatch(result.stderr, /secret-provider-detail/);
});
